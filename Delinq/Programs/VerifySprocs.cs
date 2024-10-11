using System.Data;
using System.Data.SqlClient;
using Delinq.Parsers;
using MediatR;
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

            var definition = new RepositoryDefinition {FilePath = request.RepositoryFilePath};

            // read the repository file and extract all the method names that return IEnumerable<T>
            // parse the designer file using the parser implementations created for ContextDefinition
            using (var reader = File.OpenText(definition.FilePath))
            {
                while (await reader.ReadLineAsync(cancellationToken) is { } line)
                {
                    var parser = parsers.FirstOrDefault(p => p.CanParse(line));
                    parser?.Parse(definition, reader);
                }
            }
            var repositoryCode = await fileStorage.ReadAllTextAsync(request.RepositoryFilePath, cancellationToken);
            var methodDetails = await ExtractMethodNamesAndSprocNamesAsync(repositoryCode, cancellationToken);

            var code = GetStoredProcedureCode(request.ConnectionString, request.MethodName);
            Console.WriteLine($"Request: {code}");
            return "Implement me!!";
        }

        private Task<Dictionary<string, string>> ExtractMethodNamesAndSprocNamesAsync(string repositoryCode, CancellationToken cancellationToken)
        {
            var result = new Dictionary<string, string>();
            return Task.FromResult(result);
        }

        private async Task ValidateConnectionAsync(string connectionString)
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