using System.Data;
using System.Data.SqlClient;
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

    public class Handler : IRequestHandler<Request, string>
    {
        private readonly IFileStorage _fileStorage;
        private readonly ConnectionStrings _connectionStrings;

        public Handler(IOptions<ConnectionStrings> connectionStrings, IFileStorage fileStorage)
        {
            _fileStorage = fileStorage;
            _connectionStrings = connectionStrings.Value;
        }

        public Task<string> Handle(Request request, CancellationToken cancellationToken)
        {
            if (request.ConnectionString.StartsWith("SECRET:"))
            {
                if (request.ConnectionString != "SECRET:ConnectionStrings:InCode")
                    throw new InvalidOperationException("Must specify 'SECRET:ConnectionStrings:InCode'");

                request.ConnectionString = _connectionStrings.InCode;
            }

            var code = GetStoredProcedureCode(request.ConnectionString, request.MethodName);
            Console.WriteLine($"Request: {code}");
            return Task.FromResult("Implement me!!");
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