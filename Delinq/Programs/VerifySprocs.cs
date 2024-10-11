using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using MediatR;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Options;

namespace Delinq.Programs;

public sealed class VerifySprocs
{
    public record Request : IRequest<string>
    {
        public string RepositoryFilePath { get; set; }
        public string ConnectionString { get; set; }
        public string ValidationFilePath { get; set; }
        public string MethodName { get; set; }
    }

    public class Handler(
        IContextDefinitionSerializer<RepositoryDefinition> serializer,
        IOptions<ConnectionStrings> connectionStrings,
        IFileStorage fileStorage) : IRequestHandler<Request, string>
    {
        private readonly ConnectionStrings _connectionStrings = connectionStrings.Value;

        public async Task<string> Handle(Request request, CancellationToken cancellationToken)
        {
            // if the connection string is a secret, replace it with the actual connection string
            if (request.ConnectionString.StartsWith("SECRET:"))
            {
                if (request.ConnectionString != "SECRET:ConnectionStrings:InCode")
                    throw new InvalidOperationException("Must specify 'SECRET:ConnectionStrings:InCode'");

                request.ConnectionString = _connectionStrings.InCode;
            }

            // before doing anything more, ensure we have a valid connection string!
            await ValidateConnectionAsync(request.ConnectionString);

            // read repository file to extract all details using Roslyn
            var definition = new RepositoryDefinition {FilePath = request.RepositoryFilePath};
            await ExtractRepositoryMethodsAsync(definition, request.MethodName, cancellationToken);

            // read each method and extract the stored procedure details from database
            foreach (var method in definition.Methods)
            {
                await ExtractSprocDetailsAsync(request.ConnectionString, method);
            }

            // todo: set the status of each repository method to help determine what things may need updated

            await serializer.SerializeAsync(request.ValidationFilePath, definition, cancellationToken);
            return request.ValidationFilePath;
        }


        private async Task ExtractRepositoryMethodsAsync(RepositoryDefinition definition, string? methodName, CancellationToken cancellationToken)
        {
            // read the repository file
            var code = await fileStorage.ReadAllTextAsync(definition.FilePath, cancellationToken);

            // get the methods from the code
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = (CompilationUnitSyntax)await tree.GetRootAsync(cancellationToken);
            var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

            // populate our definition with details of each method
            foreach (var method in methods.Where(m => m.Modifiers.ToString() == "public"))
            {
                // are we filtering by method name?
                if (!string.IsNullOrEmpty(methodName) && method.Identifier.Text != methodName)
                    continue;

                var repositoryMethod = ExtractRepositoryMethod(method);
                definition.Methods.Add(repositoryMethod);
            }
        }


        private static async Task ValidateConnectionAsync(string connectionString)
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            connection.Close();
        }

        private static RepositoryMethod ExtractRepositoryMethod(MethodDeclarationSyntax method)
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
                Parameters = parameters,
            };

            var body = method.Body?.ToString();
            if (!string.IsNullOrEmpty(body))
            {
                // every repository should have a stored procedure name
                var match = Regex.Match(body, "SQL = \"(.*?)\"");
                var sprocName = match.Success ? match.Groups[1].Value : string.Empty;

                // extend our method definition with details from the code
                result.CurrentQueryType = GetCurrentQueryType(body);
                result.HasReturnParameter = body.Contains("ReturnValue");
                result.NumberOfOutParameters = Regex.Matches(body, "ParameterDirection\\.Output").Count;
                result.StoredProcedure = new SprocDefinition { Name = sprocName };
            }

            return result;
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

        private static async Task ExtractSprocDetailsAsync(string connectionString, RepositoryMethod method)
        {
            var sproc = method.StoredProcedure;
            var sprocName = sproc.Name.Replace("dbo.", "");
            var code = await GetStoredProcedureCodeAsync(connectionString, sprocName);

            if (string.IsNullOrEmpty(code))
            {
                method.Status = RepositoryMethodStatus.SprocNotFound;
                return;
            }

            var splitIndex = code.IndexOf("BEGIN", StringComparison.Ordinal);
            var header = code[..splitIndex];
            var body = code[splitIndex..];

            sproc.Parameters = ExtractSprocParameters(header);
            sproc.QueryType = GetSprocQueryType(body);
        }

        private static async Task<string?> GetStoredProcedureCodeAsync(string connectionString, string sprocName)
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

            await connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();
            return result?.ToString();
        }

        private static List<SprocParameter> ExtractSprocParameters(string code)
        {
            const string pattern = @"@(?<ParamName>\w+)\s+(?<DataType>[\w\(\)]+)\s*(?<Collation>COLLATE\s+\w+)?\s*(?<Output>OUT|OUTPUT)?\s*(?<ReadOnly>READONLY)?";
            var results = new List<SprocParameter>();

            var regex = new Regex(pattern);
            foreach (Match match in regex.Matches(code))
            {
                var parameter = new SprocParameter
                {
                    Name = match.Groups["ParamName"].Value,
                    Type = match.Groups["DataType"].Value,
                    Modifier = match.Groups["Output"].Value
                };

                results.Add(parameter);
            }

            return results;
        }

        private static SprocQueryType GetSprocQueryType(string code)
        {
            // Check for RETURN statements
            var returnRegex = new Regex(@"\bRETURN\b\s+(@|\d|SCOPE_IDENTITY)\d*", RegexOptions.Multiline);
            if (returnRegex.IsMatch(code))
                return SprocQueryType.ReturnValue;

            // Check for SELECT statements that return result sets
            // note: regex considers very complex SELECT statements 
            var pattern = @"(?<!INSERT\s+INTO[\s\S]+?\)\s*)(?<!EXISTS\s*\()(?<!\()\bSELECT\b\s+(?!\@|\*\s+INTO|\@\w+\s*=)";
            var selectRegex = new Regex(pattern, RegexOptions.Multiline);
            if (selectRegex.IsMatch(code))
                return SprocQueryType.Query;

            return SprocQueryType.NonQuery;
        }
    }
}