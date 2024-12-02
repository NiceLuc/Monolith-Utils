using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using MediatR;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Options;
using MonoUtils.Domain;

namespace Delinq.Programs;

public sealed class VerifyRepositoryMethods
{
    public record Request : IRequest<string>
    {
        public string ContextName { get; set; }
        public string BranchName { get; set; }
        public string RepositoryFilePath { get; set; }
        public string ConnectionString { get; set; }
        public string ValidationFilePath { get; set; }
        public string MethodName { get; set; }
        public bool IsGenerateReport { get; set; }
        public bool IsOpenReport { get; set; }
    }

    public class Handler(
        IDefinitionSerializer<RepositoryDefinition> serializer,
        IConfigSettingsBuilder settingsBuilder,
        IOptions<ConnectionStrings> connectionStrings,
        IFileStorage fileStorage,
        IMediator mediatr) : IRequestHandler<Request, string>
    {
        private readonly ConnectionStrings _connectionStrings = connectionStrings.Value;

        public async Task<string> Handle(Request request, CancellationToken cancellationToken)
        {
            // configure defaults and support file name overrides
            await ConfigureRequestAsync(request, cancellationToken);
            await ValidateRequestAsync(request, cancellationToken);

            // read repository file to extract all details using Roslyn (respecting method name filter)
            var methods = await GetRepositoryMethodsAsync(request.RepositoryFilePath, request.MethodName, cancellationToken);

            var definition = new RepositoryDefinition {FilePath = request.RepositoryFilePath};
            foreach (var method in methods)
            {
                var repositoryMethod = CreateRepositoryMethod(method, out var code);

                // extract the stored procedure details from database
                await FillSprocDetailsAsync(repositoryMethod, request.ConnectionString, cancellationToken);

                if (repositoryMethod.Status == RepositoryMethodStatus.Unknown)
                    FinalizeMethodStatus(repositoryMethod, code);

                definition.Methods.Add(repositoryMethod);
            }

            await serializer.SerializeAsync(request.ValidationFilePath, definition, cancellationToken);

            if (!request.IsGenerateReport)
                return request.ValidationFilePath;

            // generate the report
            var reportRequest = new VerificationReport.Request
            {
                ContextName = request.ContextName,
                ValidationFilePath = request.ValidationFilePath,
                IsOpenReport = request.IsOpenReport
            };

            return await mediatr.Send(reportRequest, cancellationToken);
        }

        #region Private Methods

        private async Task ConfigureRequestAsync(Request request, CancellationToken cancellationToken)
        {
            // get a custom settings object with all file and directory paths resolved from config files
            var settings = await settingsBuilder.BuildAsync(request.ContextName, request.BranchName, cancellationToken);

            if (string.IsNullOrEmpty(request.RepositoryFilePath))
                request.RepositoryFilePath = settings.TfsRepositoryFilePath;

            if (string.IsNullOrEmpty(request.ValidationFilePath))
                request.ValidationFilePath = settings.TempValidationFilePath;
        }

        private async Task ValidateRequestAsync(Request request, CancellationToken cancellationToken)
        {
            // note: repository file must exist
            if (!fileStorage.FileExists(request.RepositoryFilePath))
                throw new FileNotFoundException($"File does not exist: {request.RepositoryFilePath}");

            // if the connection string is a secret, replace it with the actual connection string
            ResolveConnectionString(request);

            // before doing anything more, ensure we have a valid connection string!
            await ValidateConnectionAsync(request.ConnectionString, cancellationToken);
        }

        private void ResolveConnectionString(Request request)
        {
            if (string.IsNullOrEmpty(request.ConnectionString))
            {
                request.ConnectionString = _connectionStrings.InCode;
                return;
            }

            if (request.ConnectionString.StartsWith("SECRET:"))
            {
                if (request.ConnectionString != "SECRET:ConnectionStrings:InCode")
                    throw new InvalidOperationException("Must specify 'SECRET:ConnectionStrings:InCode'");

                request.ConnectionString = _connectionStrings.InCode;
            }
        }

        private static async Task ValidateConnectionAsync(string connectionString, CancellationToken cancellationToken)
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            connection.Close();
        }

        private async Task<IEnumerable<MethodDeclarationSyntax>> GetRepositoryMethodsAsync(
            string repositoryFilePath, string? methodName, CancellationToken cancellationToken)
        {
            // read the repository file
            var code = await fileStorage.ReadAllTextAsync(repositoryFilePath, cancellationToken);

            // get the methods from the code
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = (CompilationUnitSyntax) await tree.GetRootAsync(cancellationToken);
            var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>()
                .Where(m => m.Modifiers.ToString() == "public");

            // return one or all methods
            return !string.IsNullOrEmpty(methodName)
                ? methods.Where(m => m.Identifier.Text == methodName)
                : methods;
        }

        private static RepositoryMethod CreateRepositoryMethod(MethodDeclarationSyntax method, out string code)
        {
            var parameters = method.ParameterList.Parameters.Select(p => new RepositoryParameter
            {
                Name = p.Identifier.Text,
                Type = p.Type?.ToString() ?? "UNKNOWN",
                Modifier = p.Modifiers.ToString()
            }).ToList();

            var result = new RepositoryMethod
            {
                Name = method.Identifier.Text,
                ReturnType = method.ReturnType.ToString(),
                Status = RepositoryMethodStatus.Unknown,
                Parameters = parameters,
            };

            code = method.Body?.ToString() ?? string.Empty;
            if (string.IsNullOrEmpty(code))
                throw new InvalidOperationException("Method body is empty!");

            // extend our method definition with details from the code
            result.QueryType = GetCurrentQueryType(code);

            // every repository should have a stored procedure name
            var match = Regex.Match(code, "SQL = \"(.*?)\"");
            var sprocName = match.Success ? match.Groups[1].Value : string.Empty;
            result.StoredProcedure = new SprocDefinition {Name = sprocName, QueryType = SprocQueryType.Unknown};

            return result;
        }

        private static async Task FillSprocDetailsAsync(RepositoryMethod method, string connectionString, CancellationToken cancellationToken)
        {
            var sproc = method.StoredProcedure;
            var sprocName = sproc.Name.Replace("dbo.", "");
            var code = await GetStoredProcedureCodeAsync(connectionString, sprocName, cancellationToken);

            if (string.IsNullOrEmpty(code))
            {
                method.Status = RepositoryMethodStatus.SprocNotFound;
                sproc.QueryType = method.QueryType;
                return;
            }

            // ignore all comments above the definition
            var match = Regex.Match(code, @"CREATE\s+PROCEDURE", RegexOptions.IgnoreCase);
            if (!match.Success)
                throw new InvalidOperationException("Sproc is missing [CREATE PROCEDURE]: " + sprocName);

            code = code[match.Index..];

            var splitIndex = code.IndexOf("BEGIN", StringComparison.Ordinal);
            if (splitIndex < 0)
            {
                splitIndex = code.IndexOf("AS", StringComparison.Ordinal);
                if (splitIndex < 0)
                    throw new InvalidOperationException("Sproc is not valid: " + sprocName);
            }

            var header = code[..splitIndex];
            var body = code[splitIndex..];

            sproc.Parameters = ExtractSprocParameters(header);
            sproc.QueryType = GetSprocQueryType(body, out var confidence);
            sproc.Confidence = confidence;
        }

        private static void FinalizeMethodStatus(RepositoryMethod method, string code)
        {
            var validators = new List<MethodValidator>
            {
                new ConfidenceValidator(),
                new QueryTypeValidator(),
                new ParameterValidator(),
                new OutputParameterValidator()
            };

            foreach (var validator in validators)
                validator.Validate(method, code);

            // if we've made it here, we have done the best we can to determine the status
            if (method.Status == RepositoryMethodStatus.Unknown)
                method.Status = RepositoryMethodStatus.OK;
        }


        private static SprocQueryType GetCurrentQueryType(string? body)
        {
            if (string.IsNullOrEmpty(body))
                return SprocQueryType.Unknown;

            if (body.Contains("ReturnValue"))
                return SprocQueryType.ReturnValue;

            if (body.Contains("query.Read()"))
                return SprocQueryType.Query;

            if (body.Contains("ExecuteNonQuery"))
                return SprocQueryType.NonQuery;

            // note: we don't currently have code for Scalar in our query methods

            return SprocQueryType.Unknown;
        }

        private static async Task<string?> GetStoredProcedureCodeAsync(string connectionString, string sprocName, CancellationToken cancellationToken)
        {
            const string query = """
                 SELECT sm.definition
                 FROM sys.sql_modules AS sm
                 INNER JOIN sys.objects AS o ON sm.object_id = o.object_id
                 WHERE o.type = 'P' AND o.name = @StoredProcedureName
            """;

            await using var connection = new SqlConnection(connectionString);
            await using var command = new SqlCommand(query, connection);
            command.Parameters.Add(new SqlParameter("@StoredProcedureName", SqlDbType.NVarChar, 128));
            command.Parameters["@StoredProcedureName"].Value = sprocName;

            await connection.OpenAsync(cancellationToken);
            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result?.ToString();
        }

        private static List<SprocParameter> ExtractSprocParameters(string code)
        {
            const string pattern = @"@(?<ParamName>\w+)\s+(?<DataType>[\w\(\)]+)\s*(?<Collation>COLLATE\s+\w+)?\s*(?<Output>OUT|OUTPUT)?\s*(?<ReadOnly>READONLY)?\s*(?<HasDefault>\=)?";
            var results = new List<SprocParameter>();

            var regex = new Regex(pattern);
            foreach (Match match in regex.Matches(code))
            {
                var parameter = new SprocParameter
                {
                    Name = match.Groups["ParamName"].Value,
                    Type = match.Groups["DataType"].Value,
                    Modifier = match.Groups["Output"].Value,
                    HasDefault = match.Groups["HasDefault"].Success
                };

                results.Add(parameter);
            }

            return results;
        }

        private static SprocQueryType GetSprocQueryType(string code, out float confidence)
        {
            // Check for RETURN statements
            var returnRegex = new Regex(@"\bRETURN\b\s+(@|\d|SCOPE_IDENTITY)\d*", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            if (returnRegex.IsMatch(code))
            {
                confidence = 1f;
                return SprocQueryType.ReturnValue;
            }

            // for both query and non-query, we can calculate confidence
            confidence = CalculateConfidence(code);

            // Check for SELECT statements that return result sets
            // note: regex considers very complex SELECT statements 
            var pattern = @"(?<!INSERT\s+INTO[\s\S]+?\)\s*)(?<!EXISTS\s*\()(?<!\()\bSELECT\b\s+(?!\@|\*\s+INTO|\@\w+\s*=)";
            var selectRegex = new Regex(pattern, RegexOptions.Multiline | RegexOptions.IgnoreCase);
            if (selectRegex.IsMatch(code))
            {
                return SprocQueryType.Query;
            }

            return SprocQueryType.NonQuery;
        }

        private static float CalculateConfidence(string code)
        {
            const int threshold = 3;
            const int tooMany = 10;

            var selectCount = Regex.Matches(code, @"\bSELECT\b").Count;
            if (selectCount <= threshold)
                return 1f; // 100% confident!! 

            if (selectCount >= tooMany) 
                return 0; // 0% confidence!!

            // any count above the threshold reduces 10% confidence
            var notSureLevel = 0.1f * (selectCount - threshold);
            return 1f - notSureLevel;
        }
        #endregion

        #region Private Classes

        private abstract class MethodValidator
        {
            public void Validate(RepositoryMethod method, string code)
            {
                var originalValue = method.Status;

                ValidateImpl(method, code);

                // Warnings should not override errors
                if (WasOriginalStatusMoreSevere(method, originalValue))
                {
                    method.Status = originalValue;
                    return;
                }

                // if there was a previous error status, and it's different, then indicate we have multiple errors
                if (WasOriginalStatusIndicatingAnError(method, originalValue)) 
                    method.Status = RepositoryMethodStatus.MultipleErrors;
            }

            private static bool WasOriginalStatusMoreSevere(RepositoryMethod method, RepositoryMethodStatus originalValue)
            {
                if (originalValue is RepositoryMethodStatus.Unknown or RepositoryMethodStatus.Warning)
                    return false;

                // if our current status is a warning, then the original status is more severe
                return method.Status == RepositoryMethodStatus.Warning;
            }

            private static bool WasOriginalStatusIndicatingAnError(RepositoryMethod method, RepositoryMethodStatus originalValue)
            {
                if (originalValue is RepositoryMethodStatus.Unknown or RepositoryMethodStatus.Warning)
                    return false;

                return method.Status != originalValue;
            }

            protected abstract void ValidateImpl(RepositoryMethod method, string code);
        }

        private class ParameterValidator : MethodValidator
        {
            protected override void ValidateImpl(RepositoryMethod method, string code)
            {
                var sprocParameters = method.StoredProcedure.Parameters;
                var addedParameters = GetParametersAdded(method, code);

                var missing = FindMissingParameters(sprocParameters, addedParameters, true);
                if (missing.Any())
                {
                    var names = string.Join(", ", missing.Select(p => p.Name));
                    method.Errors.Add($"INFO: Optional parameter(s) not set: {names}");
                    method.Status = RepositoryMethodStatus.Warning;
                }

                missing = FindMissingParameters(sprocParameters, addedParameters, false);
                if (missing.Any())
                {
                    var names = string.Join(", ", missing.Select(p => p.Name));
                    method.Errors.Add($"ERROR: Required parameter(s) not set: {names}");
                    method.Status = RepositoryMethodStatus.NotAllParametersAreBeingSet;
                }
            }

            private static List<SprocParameter> GetParametersAdded(RepositoryMethod method, string code) =>
                (from p in method.StoredProcedure.Parameters
                    let pattern = @$"AddParameter\(""@{p.Name}"""
                    where Regex.IsMatch(code, pattern, RegexOptions.Multiline | RegexOptions.IgnoreCase)
                    select p).ToList();

            private static SprocParameter[] FindMissingParameters(
                IEnumerable<SprocParameter> sprocParameters,
                IEnumerable<SprocParameter> addedParameters,
                bool hasDefault)
            {
                var required = sprocParameters.Where(p => p.HasDefault == hasDefault);
                var added = addedParameters.Where(p => p.HasDefault == hasDefault);
                return required.Except(added).ToArray();
            }
        }

        private class QueryTypeValidator : MethodValidator
        {
            protected override void ValidateImpl(RepositoryMethod method, string code)
            {
                var sproc = method.StoredProcedure;
                if (method.QueryType == sproc.QueryType) 
                    return;

                method.Status = RepositoryMethodStatus.InvalidQueryType;
                method.Errors.Add($"C#: {method.QueryType}, DB: {sproc.QueryType}");
            }
        }

        private class ConfidenceValidator : MethodValidator
        {
            protected override void ValidateImpl(RepositoryMethod method, string code)
            {
                var message = method.StoredProcedure.Confidence switch
                {
                    <= 0.7f => "TODO: STORED PROCEDURE REQUIRES MANUAL REVIEW TO DETERMINE QUERY TYPE!",
                    <= 0.8f => "NOTE: Stored procedure is fairly complex and should be reviewed for query type.",
                    <= 0.9f => "INFO: Stored procedure may require manual review for query type.",
                    _ => string.Empty
                };

                if (string.IsNullOrEmpty(message))
                    return;

                method.Status = RepositoryMethodStatus.Warning;
                method.Errors.Add(message);
            }
        }

        private class OutputParameterValidator : MethodValidator
        {
            private static readonly Regex _regex = new(
                @"AddParameter\(""@(?<name>.+?)"".+ParameterDirection\.(?<direction>.+?)\)",
                RegexOptions.Multiline);

            private static readonly HashSet<string> _ignoreList = new(
                ["TotalRows", "RowCount", "TotalRowCount", "RowsReturned"], 
                StringComparer.OrdinalIgnoreCase);

            protected override void ValidateImpl(RepositoryMethod method, string code)
            {
                var matches = _regex.Matches(code);
                if (matches.Count == 0)
                    return;

                var items = new List<string>();
                foreach (Match match in matches)
                {
                    if (match.Groups["direction"].Value != "Output")
                        continue;

                    var parameterName = match.Groups["name"].Value;
                    if (_ignoreList.Contains(parameterName))
                        continue;

                    items.Add(parameterName);
                }

                if (items.Count <= 0)
                    return;

                var names = string.Join(", ", items);
                method.Status = RepositoryMethodStatus.Warning;
                method.Errors.Add($"Note: Output parameter(s) detected: {names}");
            }
        }

        #endregion
    }
}