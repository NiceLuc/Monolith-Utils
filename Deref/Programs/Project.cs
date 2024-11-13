using System.Collections.Immutable;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog;
using SharedKernel;
using ILogger = Serilog.ILogger;

namespace Deref.Programs;

public class Project
{
    public class Request : IRequest<string>
    {
        public string? ProjectName { get; set; }
        public bool IsList { get; set; }
        public bool IsListReferences { get; set; }
        public bool IsListReferencedBy { get; set; }
        public bool ShowListCounts { get; set; }
        public bool ShowListTodos { get; set; }
    }

    public class Handler(ILogger<Handler> logger,
        IProgramSettingsBuilder settingsBuilder,
        IBranchDatabaseProvider databaseProvider) : IRequestHandler<Request, string>
    {
        private const string _todoPattern = "    - todo: {0}";

        public async Task<string> Handle(Request request, CancellationToken cancellationToken)
        {
            if (!ValidateRequest(request, out var message))
                return message;

            ConfigureRequest(request);

            var settings = await settingsBuilder.BuildAsync(cancellationToken);
            var database = await databaseProvider.GetDatabaseAsync(settings.BranchName, cancellationToken);
            var lookup = database.Projects.ToDictionary(p => p.Name, StringComparer.InvariantCultureIgnoreCase);

            // single project
            if (!request.IsList)
            {
                if (!lookup.TryGetValue(request.ProjectName, out var project))
                    return "Project not found: " + request.ProjectName;

                // show header
                ShowProjectDetails(project);

                if (request.IsListReferencedBy)
                {
                    var projects = project.ReferencedBy.Select(p => lookup[p]).ToArray();
                    logger.LogInformation($"Referenced by {projects.Length} projects: {IncludeCountsHeader(request)}");
                    ShowProjectsList(projects, request);
                }

                if (request.IsListReferences)
                {
                    var projects = project.References.Select(p => lookup[p]).ToArray();
                    logger.LogInformation($"References {projects.Length} projects: {IncludeCountsHeader(request)}");
                    ShowProjectsList(projects, request);
                }

                return string.Empty;
            }

            // filtered
            if (!string.IsNullOrEmpty(request.ProjectName))
            {
                var projects = database.Projects.Where(p 
                    => p.Name.Contains(request.ProjectName, StringComparison.InvariantCultureIgnoreCase)).ToArray();
                logger.LogInformation($"Search found {projects.Length} projects: {IncludeCountsHeader(request)}");
                ShowProjectsList(projects, request);
                return string.Empty;
            }

            // all
            var all = database.Projects.ToArray();
            logger.LogInformation($"Listing {all.Length} projects: {IncludeCountsHeader(request)}");
            ShowProjectsList(all, request);
            return string.Empty;
        }

        private void ShowProjectDetails(BranchDatabase.Project project)
        {
            var status = !project.Exists
                ? "Missing"
                : GetProjectStatus(project)
                    ? "Done"
                    : "Needs Work";
            var glyph = GetProjectGlyph(project);
            logger.LogInformation($"Project: {project.Name}");
            logger.LogInformation($"Status: {status} {glyph}");


            if (project.Exists)
            {
                logger.LogInformation($"Is SDK: {project.IsSdk}");
                logger.LogInformation($"Is NetStandard2: {project.IsNetStandard2}");
                logger.LogInformation($"Is PackageRef: {project.IsPackageRef}");
            }
            else
            {
                logger.LogInformation($"Exists: {project.Exists}");
            }
            logger.LogInformation("----------");
        }

        private void ShowProjectsList(BranchDatabase.Project[] projects, Request request)
        {
            foreach (var project in projects) 
                ShowProjectDetailsRow(project, request);
        }

        private void ShowProjectDetailsRow(BranchDatabase.Project project, Request request)
        {
            var stringBuilder = new StringBuilder();
            var glyph = GetProjectGlyph(project);
            stringBuilder.Append($"{glyph} - {project.Name}");

            if (!project.Exists)
            {
                stringBuilder.Append(" (missing)");
                logger.LogInformation(stringBuilder.ToString());
                return;
            }

            if (request.ShowListCounts)
                stringBuilder.Append($" ({project.References.Count} / {project.ReferencedBy.Count})");

            logger.LogInformation(stringBuilder.ToString());

            if (request.ShowListTodos)
            {
                if (!project.IsNetStandard2)
                    logger.LogInformation(string.Format(_todoPattern, "NETSTANDARD2"));

                if (!project.IsSdk)
                    logger.LogInformation(string.Format(_todoPattern, "SDK upgrade"));

                if (!project.IsPackageRef)
                    logger.LogInformation(string.Format(_todoPattern, "PackageReferences"));
            }
        }


        private static bool ValidateRequest(Request request, out string message)
        {
            if (!request.IsList && string.IsNullOrEmpty(request.ProjectName))
            {
                message = "Must specify a project name or use the '--list' option";
                return false;
            }

            message = string.Empty;
            return true;

        }

        private static void ConfigureRequest(Request request)
        {
            if (request is {IsList: false, IsListReferences: false, IsListReferencedBy: false})
            {
                request.IsListReferences = true;
                request.IsListReferencedBy = true;
            }
        }

        private static string IncludeCountsHeader(Request request) 
            => request.ShowListCounts ? "(uses / used by)" : "";

        private static string GetProjectGlyph(BranchDatabase.Project project)
        {
            if (!project.Exists)
                return "❌";

            var status = GetProjectStatus(project);
            return status ? "✔️" : "⚠️";
        }

        private static bool GetProjectStatus(BranchDatabase.Project project) 
            => project is {IsSdk: true, IsNetStandard2: true, IsPackageRef: true};
    }
}