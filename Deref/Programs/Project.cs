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
        public async Task<string> Handle(Request request, CancellationToken cancellationToken)
        {
            var settings = await settingsBuilder.BuildAsync(cancellationToken);
            var database = await databaseProvider.GetDatabaseAsync(settings.BranchName, cancellationToken);
            var lookup = database.Projects.ToDictionary(p => p.Name);

            // single project
            if (!request.IsList)
            {
                if (string.IsNullOrEmpty(request.ProjectName))
                    return "Must specify a project name or use the '--list' option";

                if (!lookup.TryGetValue(request.ProjectName, out var project))
                    return "Project not found: " + request.ProjectName;

                // show header
                ShowProjectDetails(project);

                if (request.IsListReferencedBy)
                {
                    logger.LogInformation("Referenced by:");
                    var projects = project.ReferencedBy.Select(p => lookup[p]).ToArray();
                    ShowProjectsList(projects, request);
                    return $"{projects.Length} projects reference {project.Name}";
                }

                if (request.IsListReferences)
                {
                    logger.LogInformation("References:");
                    var projects = project.References.Select(p => lookup[p]).ToArray();
                    ShowProjectsList(projects, request);
                    return $"{project.Name} references {projects.Length} projects";
                }

                logger.LogInformation($"References: {project.References.Count} projects");
                logger.LogInformation($"Referenced by: {project.ReferencedBy.Count} projects");
                return string.Empty;
            }

            // filtered
            if (!string.IsNullOrEmpty(request.ProjectName))
            {
                // filtered
                var projects = database.Projects.Where(p => p.Name.Contains(request.ProjectName)).ToArray();
                ShowProjectsList(projects, request);
                return $"Found {projects.Length} project matching '{request.ProjectName}'";
            }

            // all
            var all = database.Projects.ToArray();
            ShowProjectsList(all, request);
            return $"Found {all.Length} projects";
        }

        private void ShowProjectDetails(BranchDatabase.Project project)
        {
            var isGood = project is {IsNetStandard2: true, IsPackageRef: true, IsSdk: true};
            var status = isGood ? "✔️" : "Needs Work!";
            logger.LogInformation($"Project: {project.Name} (status: {status})");
            logger.LogInformation($"Path: {project.Path}");
            logger.LogInformation($"Exists: {project.Exists}");
            logger.LogInformation($"Is SDK: {project.IsSdk}");
            logger.LogInformation($"Is NetStandard2: {project.IsNetStandard2}");
            logger.LogInformation($"Is PackageRef: {project.IsPackageRef}");
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
            stringBuilder.Append($" - {project.Name}");

            if (request.ShowListCounts) 
                stringBuilder.Append($" (uses: {project.References.Count}, used by: {project.ReferencedBy.Count})");

            if (request.ShowListTodos)
            {
                var todos = new List<string>();
                if (!project.IsNetStandard2) todos.Add("NETSTANDARD2");
                if (!project.IsSdk) todos.Add("SDK upgrade");
                if (!project.IsPackageRef) todos.Add("PackageReferences");

                var todoList = string.Join(", ", todos);
                if (todoList.Length > 0)
                    stringBuilder.Append($" (todos: {todoList})");
                else
                    stringBuilder.Append(" (✔️)");
            }

            logger.LogInformation(stringBuilder.ToString());
        }
    }
}