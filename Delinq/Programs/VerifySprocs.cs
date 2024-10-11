using System.Data;
using System.Data.SqlClient;
using Delinq.Parsers;
using MediatR;
using Microsoft.CodeAnalysis;
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
        public string ReportFilePath { get; set; }
        public string MethodName { get; set; }
    }

    public class Handler(
        IEnumerable<IParser<RepositoryDefinition>> parsers,
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
            var methods = await ExtractMethodsFromFile(request.RepositoryFilePath, cancellationToken);

            var definition = new RepositoryDefinition {FilePath = request.RepositoryFilePath};
            foreach (var method in methods)
            {
                var returnType = method.ReturnType.ToString();
                var methodName = method.Identifier.Text;
                var parameters = method.ParameterList.Parameters.Select(p => new ParameterDefinition
                {
                    ParameterName = p.Identifier.Text,
                    ParameterType = p.Type.ToString(),
                }).ToList();

                definition.EnumerableMethods.Add(new EnumerableMethod
                {
                    MethodName = methodName,
                    MethodReturnType = returnType,
                    MethodParameters = parameters
                });
            }

            var sproc = GetStoredProcedureCode(request.ConnectionString, request.MethodName);
            Console.WriteLine($"Request: {sproc}");

            await serializer.SerializeAsync(request.ReportFilePath, definition, cancellationToken);
            return request.ReportFilePath;
        }

        private async Task<IEnumerable<MethodDeclarationSyntax>> ExtractMethodsFromFile(string filePath, CancellationToken cancellationToken)
        {
            var code = await fileStorage.ReadAllTextAsync(filePath, cancellationToken);

            var tree = CSharpSyntaxTree.ParseText(code);
            var root = (CompilationUnitSyntax)await tree.GetRootAsync(cancellationToken);

            return root.DescendantNodes().OfType<MethodDeclarationSyntax>();
        }

        private static async Task ValidateConnectionAsync(string connectionString)
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            connection.Close();
        }

        private string GetStoredProcedureCode(string connectionString, string sprocName)
        {

            using var connection = new SqlConnection(connectionString);
            var query = @"
                    SELECT sm.definition
                    FROM sys.sql_modules AS sm
                    INNER JOIN sys.objects AS o ON sm.object_id = o.object_id
                    WHERE o.type = 'P' AND o.name = @StoredProcedureName";

            using var command = new SqlCommand(query, connection);
            command.Parameters.Add(new SqlParameter("@StoredProcedureName", SqlDbType.NVarChar, 128));
            command.Parameters["@StoredProcedureName"].Value = sprocName;

            connection.Open();
            return (string)command.ExecuteScalar();
        }
    }
}