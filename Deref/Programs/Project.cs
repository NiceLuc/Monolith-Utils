using System.Text;
using MediatR;
using Microsoft.Extensions.Logging;

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
        public bool IsRecursive { get; set; }
    }

    public class Handler(ILogger<Handler> logger,
        IProgramSettingsBuilder settingsBuilder,
        IBranchDatabaseProvider databaseProvider) : IRequestHandler<Request, string>
    {
        private static readonly string _separator = new('-', 50);
        private const string _termPattern = "{0,4}"; // right align terms
        private const string _todoPattern = "        - {0}"; // 8 leading spaces!

        public async Task<string> Handle(Request request, CancellationToken cancellationToken)
        {
            if (!ValidateRequest(request, out var message))
                return message;

            var settings = await settingsBuilder.BuildAsync(cancellationToken);
            var database = await databaseProvider.GetDatabaseAsync(settings.BranchName, cancellationToken);
            var lookup = database.Projects.ToDictionary(p => p.Name, StringComparer.InvariantCultureIgnoreCase);

            // single project
            if (!request.IsList)
            {
                if (!lookup.TryGetValue(request.ProjectName!, out var project))
                    return "Project not found: " + request.ProjectName;

                // show header
                ShowProjectDetails(project);

                if (request.IsListReferences)
                {
                    var projects = GetProjectsReferencing(project, lookup, request.IsRecursive);
                    ShowProjectsList("References", projects, request);
                }

                if (request.IsListReferencedBy)
                {
                    var projects = GetProjectsReferencedBy(project, lookup, request.IsRecursive);
                    ShowProjectsList("Referenced by", projects, request);
                }

                // always show solutions
                var solutions = database.Solutions.ToDictionary(s => s.Name, StringComparer.InvariantCultureIgnoreCase);
                logger.LogInformation($"Found in {project.Solutions.Count} solution(s):");
                ShowSolutionsList(project, solutions);
                return string.Empty;
            }

            // filtered
            if (!string.IsNullOrEmpty(request.ProjectName))
            {
                var projects = database.Projects.Where(p 
                    => p.Name.Contains(request.ProjectName, StringComparison.InvariantCultureIgnoreCase)).ToArray();
                ShowProjectsList("Search found", projects, request);
                return string.Empty;
            }

            // all
            var all = database.Projects.ToArray();
            ShowProjectsList("Listing", all, request);
            return string.Empty;
        }

        private void ShowProjectDetails(BranchDatabase.Project project)
        {
            var status = project.Exists
                ? GetProjectStatus(project) ? "Done" : "Needs Work"
                : "Missing";

            logger.LogInformation(_separator);
            LogInformation("Project", project.Name);
            LogInformation("Path", project.Path);
            LogInformation("Status",status);

            if (project.Exists)
            {
                var todos = GetTodos(project);
                LogInformation("Todos", string.Join(", ", todos));
            }
            else
            {
                LogInformation("Exists", project.Exists);
            }

            logger.LogInformation(_separator);
            return;

            void LogInformation(string fieldName, object fieldValue)
                => logger.LogInformation($"{fieldName,8}: {fieldValue}");
        }

        private void ShowSolutionsList(BranchDatabase.Project project, Dictionary<string, BranchDatabase.Solution> solutions)
        {
            var maxNameWidth = project.Solutions.Max(s => s.Length) + 2;
            foreach(var solutionName in project.Solutions)
            {
                var solution = solutions[solutionName];
                var spaces = new string(' ', maxNameWidth - solutionName.Length);
                logger.LogInformation($"{spaces}{solutionName}: {solution.Path}");
            }

            logger.LogInformation(_separator);
        }

        private void ShowProjectsList(string prefix, BranchDatabase.Project[] projects, Request request)
        {
            if (projects.Length == 0)
            {
                logger.LogInformation($"{prefix} 0 projects");
            }
            else
            {
                logger.LogInformation($"{prefix} {projects.Length} projects{(request.IsRecursive ? " (RECURSIVELY)" : "")}: {IncludeCountsHeader(request)}");

                foreach (var project in projects.OrderBy(p => p.Name)) 
                    ShowProjectDetailsRow(project, request);
            }

            logger.LogInformation(_separator);
        }

        private void ShowProjectDetailsRow(BranchDatabase.Project project, Request request)
        {
            var stringBuilder = new StringBuilder();
            var glyph = GetProjectStatusTerm(project);
            stringBuilder.Append($" {glyph} - {project.Name}");

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
                if (!project.IsPackageRef)
                    logger.LogInformation(string.Format(_todoPattern, "Migrate to PackageReferences"));

                if (!project.IsSdk)
                    logger.LogInformation(string.Format(_todoPattern, "Upgrade to SDK style formatting"));

                if (!project.IsNetStandard2)
                    logger.LogInformation(string.Format(_todoPattern, "Support NETSTANDARD2"));
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

        private static BranchDatabase.Project[] GetProjectsReferencedBy(BranchDatabase.Project project, Dictionary<string, BranchDatabase.Project> lookup, bool isRecursive)
        {
            if (!isRecursive)
                return project.ReferencedBy.Select(p => lookup[p]).ToArray();

            var result = new Dictionary<string, BranchDatabase.Project>();
            CaptureProjectNames(project);
            return result.Values.ToArray();

            void CaptureProjectNames(BranchDatabase.Project current)
            {
                foreach(var name in current.ReferencedBy)
                {
                    if (result.ContainsKey(name))
                        continue;

                    var next = lookup[name];
                    result.Add(name, next);

                    // note: recursion!
                    CaptureProjectNames(next);
                }
            }
        }

        private static BranchDatabase.Project[] GetProjectsReferencing(BranchDatabase.Project project, Dictionary<string, BranchDatabase.Project> lookup, bool isRecursive)
        {
            if (!isRecursive)
                return project.References.Select(p => lookup[p]).ToArray();

            var result = new Dictionary<string, BranchDatabase.Project>();
            CaptureProjectNames(project);
            return result.Values.ToArray();

            void CaptureProjectNames(BranchDatabase.Project current)
            {
                foreach(var name in current.References)
                {
                    if (result.ContainsKey(name))
                        continue;

                    var next = lookup[name];
                    result.Add(name, next);

                    // note: recursion!
                    CaptureProjectNames(next);
                }
            }
        }

        private static string IncludeCountsHeader(Request request) 
            => request.ShowListCounts ? "(uses / used by)" : "";

        private static string GetProjectStatusTerm(BranchDatabase.Project project)
        {
            if (!project.Exists)
                return string.Format(_termPattern, "x");

            var status = GetProjectStatus(project);
            var term = status ? "ok" : "todo";
            return string.Format(_termPattern, term);
        }

        private static bool GetProjectStatus(BranchDatabase.Project project) 
            => project is {IsSdk: true, IsNetStandard2: true, IsPackageRef: true};

        private static string[] GetTodos(BranchDatabase.Project project)
        {
            var todos = new List<string>();
            if (!project.IsPackageRef) todos.Add("PackageRef");
            if (!project.IsSdk) todos.Add("SDK upgrade");
            if (!project.IsNetStandard2) todos.Add("NETSTANDARD2");
            if (todos.Count == 0) todos.Add("None");
            return todos.ToArray();
        }
    }
}