using System.Diagnostics;
using ClosedXML.Excel;
using MediatR;

namespace Delinq.Programs;

public sealed class VerificationReport
{
    public record Request : IRequest<string>
    {
        public string ContextName { get; set; }
        public string ValidationFilePath { get; set; }
        public string ReportFilePath { get; set; }
        public bool IsOpenReport { get; set; }
    }

    public class Handler(
        IDefinitionSerializer<RepositoryDefinition> serializer,
        IConfigSettingsBuilder settingsBuilder) : IRequestHandler<Request, string>
    {
        public async Task<string> Handle(Request request, CancellationToken cancellationToken)
        {
            await ConfigureRequestAsync(request, cancellationToken);
            ValidateRequest(request);

            var definition = await serializer.DeserializeAsync(request.ValidationFilePath, cancellationToken);

            using (var workbook = new XLWorkbook())
            {
                var report = workbook.Worksheets.Add("Report");
                var details = workbook.Worksheets.Add("Details");

                // report header
                details.Cell(1, 1).Value = "FileName: " + definition.FilePath;
                details.Cell(2, 1).Value = "Errors: " + definition.TotalErrorCount;
                details.Cell(2, 1).Style.Font.Bold = true;

                // table headers
                report.Cell(1, 1).Value = "Confidence";
                report.Cell(1, 2).Value = "Status";
                report.Cell(1, 3).Value = "Method";
                report.Cell(1, 4).Value = "Errors";

                // table data
                var row = 2;
                foreach (var method in definition.Methods)
                {
                    report.Cell(row, 1).Value = method.StoredProcedure.Confidence;
                    report.Cell(row, 2).Value = method.Status.ToString();
                    report.Cell(row, 3).Value = method.Name;
                    report.Cell(row, 4).Value = string.Join(" | ", method.Errors);

                    row++;
                }

                // confidence as percent
                report.Range(report.Cell(1, 1), report.Cell(row - 1, 1)).Style.NumberFormat.Format = "0%";

                // format and save
                var nearCell = report.Cell(1, 1);
                var farCell = report.Cell(row - 1, 4);
                report.Range(nearCell, farCell).CreateTable();
                report.Columns().AdjustToContents();
                report.Rows().AdjustToContents();

                workbook.SaveAs(request.ReportFilePath);
            }

            if (request.IsOpenReport)
                Process.Start(new ProcessStartInfo(request.ReportFilePath) { UseShellExecute = true });

            return request.ReportFilePath;
        }

        #region Private Methods

        private async Task ConfigureRequestAsync(Request request, CancellationToken cancellationToken)
        {
            // get a custom settings object with all file and directory paths resolved from config files
            var settings = await settingsBuilder.BuildAsync(request.ContextName, string.Empty, cancellationToken);

            if (string.IsNullOrEmpty(request.ValidationFilePath))
                request.ValidationFilePath = settings.TempValidationFilePath;

            if (string.IsNullOrEmpty(request.ReportFilePath)) 
                request.ReportFilePath = settings.TempValidationReportFilePath;
        }

        private static void ValidateRequest(Request request)
        {
            // note: repository file must exist
            if (!File.Exists(request.ValidationFilePath))
                throw new FileNotFoundException($"File does not exist: {request.ValidationFilePath}");

            if (!request.ReportFilePath.EndsWith(".xlsx"))
                request.ReportFilePath += ".xlsx";
        }

        #endregion
    }
}