using System.Text;
using MediatR;

namespace Delinq.Programs;

public sealed class VerificationReport
{
    public record Request : IRequest<string>
    {
        public string ValidationFilePath { get; set; }
        public string ReportName { get; set; }
    }

    public class Handler(IDefinitionSerializer<RepositoryDefinition> serializer, IFileStorage fileStorage) : IRequestHandler<Request, string>
    {
        public async Task<string> Handle(Request request, CancellationToken cancellationToken)
        {
            var definition = await serializer.DeserializeAsync(request.ValidationFilePath, cancellationToken);

            var reportName = string.IsNullOrEmpty(request.ReportName) ? "VerificationReport.csv" : $"{request.ReportName}.csv";
            var reportFilePath = Path.Combine(Path.GetDirectoryName(request.ValidationFilePath), reportName);

            // header
            var report = new StringBuilder();
            report.AppendLine($"\"FileName: {definition.FilePath}\"");
            report.AppendLine($"\"Errors: {definition.TotalErrorCount}\"");
            report.AppendLine();

            // results
            report.AppendLine("Status,Method,Errors");
            foreach (var method in definition.Methods) 
                report.AppendLine($"{method.Status},{method.Name},\"{string.Join(',', method.Errors)}\"");

            // write to file
            await fileStorage.WriteAllTextAsync(reportFilePath, report.ToString(), cancellationToken);
            return reportFilePath;
        }
    }
}