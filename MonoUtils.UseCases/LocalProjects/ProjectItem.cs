using System.Text;
using MediatR;
using Microsoft.Extensions.Logging;
using MonoUtils.Domain.Data;
using MonoUtils.Domain.Data.Queries;
using SharedKernel;

namespace MonoUtils.UseCases.LocalProjects;

public static class ProjectItem
{
    public class Query : ItemQuery<ProjectRecord>, IRequest<Result>
    {
        public TodoFilterType TodoFilter { get; set; }
        public bool IsListBuildDefinitions { get; set; }
        public bool IsListSolutions { get; set; }
        public bool IsListWixProjects { get; set; }
        public bool IsListReferences { get; set; }
        public bool IsListReferencedBy { get; set; }

        public override bool IsActive(ProjectRecord project)
        {
            if (IsExcludeTests && project.IsTestProject)
                return false;

            if (!MatchesBranchFilter(project))
                return false;

            if (!MatchesTodoFilter(project))
                return false;

            return IsSearchTermMatch(project);
        }

        private bool MatchesBranchFilter(ProjectRecord project) => BranchFilter switch
        {
            FilterType.All => true,
            FilterType.OnlyRequired => project.IsRequired,
            FilterType.OnlyNonRequired => !project.IsRequired,
            _ => throw new InvalidOperationException("Invalid FilterType: " + BranchFilter)
        };

        private bool MatchesTodoFilter(ProjectRecord project) => TodoFilter switch
        {
            TodoFilterType.NoFilter => true,
            TodoFilterType.SdkProjects => !project.IsSdk,
            TodoFilterType.PackageRefs => !project.IsPackageRef,
            TodoFilterType.NetStandard2 => !project.IsNetStandard2,
            TodoFilterType.All => project is not {IsSdk: true, IsPackageRef: true, IsNetStandard2: true},
            _ => throw new InvalidOperationException("Invalid filter: " + TodoFilter)
        };

        private bool IsSearchTermMatch(ProjectRecord project) =>
            string.IsNullOrEmpty(ListSearchTerm) ||
            project.Name.Contains(ListSearchTerm, StringComparison.InvariantCultureIgnoreCase);
    }

    public class Handler(
        ILogger<Handler> logger,
        IBranchDatabaseContextFactory contextFactory) : IRequestHandler<Query, Result>
    {
        private static readonly string _separator = new('-', 50);
        private const string _todoPattern = "        - {0}"; // 8 leading spaces!

        public async Task<Result> Handle(Query query, CancellationToken cancellationToken)
        {
            var context = await contextFactory.CreateAsync(cancellationToken);
            var project = context.GetProject(query.ItemKey);
            if (project == null)
                return Result.Failure(ProjectErrors.ProjectNotFound(query.ItemKey));

            // show header
            ShowProjectDetails(project);

            if (query.IsListReferences)
            {
                var references = context.GetProjectsReferencing(project, query);
                ShowProjectsList("References", references, query);
            }

            if (query.IsListReferencedBy)
            {
                var referencedBy = context.GetProjectsReferencedBy(project, query);
                ShowProjectsList("Referenced by", referencedBy, query);
            }

            if (query.IsListWixProjects)
            {
                var wixLookup = context.GetWixProjects(project).ToDictionary(x => x.Name);
                ShowWixProjectList(project.WixProjects, wixLookup);
            }

            if (query.IsListSolutions)
            {
                var solutions = context.GetSolutions(project).ToDictionary(x => x.Name);
                ShowSolutionsList(project, solutions);
            }

            if (query.IsListBuildDefinitions)
            {
                var buildDefinitions = context.GetBuildDefinitionNames(project);
                ShowBuildDefinitionList(buildDefinitions);
            }

            return Result.Success();
        }

        private void ShowSolutionsList(ProjectRecord project, Dictionary<string, SolutionRecord> solutions)
        {
            logger.LogInformation($"Found in {project.Solutions.Length} solution(s):");

            var maxNameWidth = project.Solutions.Max(s => s.Length) + 2;
            foreach(var solutionName in project.Solutions)
            {
                var solution = solutions[solutionName];
                var spaces = new string(' ', maxNameWidth - solutionName.Length);
                logger.LogInformation($"{spaces}{solutionName}: {solution.Path}");
            }

            logger.LogInformation(_separator);
        }

        private void ShowWixProjectList(WixProjectReference[] wixProjects, Dictionary<string, WixProjectRecord> lookup)
        {
            if (wixProjects.Length == 0)
            {
                logger.LogInformation($"Not referenced in any wix projects");
            }
            else
            {
                var names = wixProjects
                    .Where(w => !w.IsManuallyHarvested)
                    .Select(w => w.ProjectName)
                    .ToDictionary(w => w, StringComparer.InvariantCultureIgnoreCase);

                foreach (var wixProject in wixProjects.Where(w => w.IsManuallyHarvested))
                {
                    if (names.TryGetValue(wixProject.ProjectName, out var found))
                    {
                        found = "(**) " + found;
                        names[wixProject.ProjectName] = found;
                    }
                    else
                    {
                        names.Add(wixProject.ProjectName, "(*) " + wixProject.ProjectName);
                    }
                }

                logger.LogInformation($"Found in {wixProjects.Length} wix projects: (note: * = IsHarvested)");

                var maxWidth = names.Values.Max(n => n.Length) + 2;
                foreach (var reference in wixProjects.DistinctBy(w => w.ProjectName).OrderBy(w => w.ProjectName))
                {
                    var wixProject = lookup[reference.ProjectName];
                    var name = names[reference.ProjectName];
                    var spaces = new string(' ', maxWidth - name.Length);
                    logger.LogInformation($"{spaces}{name}: {wixProject.Path}");
                }
            }

            logger.LogInformation(_separator);
        }

        private void ShowBuildDefinitionList(string[] buildDefinitions)
        {
            if (buildDefinitions.Length == 0)
            {
                logger.LogInformation($"Not referenced in any build definitions");
            }
            else
            {
                logger.LogInformation($"Found in {buildDefinitions.Length} build definition(s):");
                foreach (var buildRef in buildDefinitions)
                    logger.LogInformation($"  {buildRef}");
            }

            logger.LogInformation(_separator);
        }

        private void ShowProjectsList(string prefix, ProjectRecord[] projects, Query query)
        {
            if (projects.Length == 0)
            {
                logger.LogInformation($"{prefix} 0 projects");
            }
            else
            {
                logger.LogInformation($"{prefix} {projects.Length} projects{(query.IsRecursive ? " (RECURSIVELY)" : "")}: {IncludeCountsHeader(query)}");

                foreach (var project in projects.OrderBy(p => p.Name)) 
                    ShowProjectDetailsRow(project, query);
            }

            logger.LogInformation(_separator);
        }

        private static string IncludeCountsHeader(Query query) 
            => query.ShowListCounts ? "(uses / used by)" : "";

        private void ShowProjectDetailsRow(ProjectRecord project, Query query)
        {
            var stringBuilder = new StringBuilder();
            var glyph = GetProjectStatusTerm(project);
            stringBuilder.Append($" {glyph} - {project.Name}");

            if (!project.DoesExist)
            {
                stringBuilder.Append(" (missing)");
                logger.LogInformation(stringBuilder.ToString());
                return;
            }

            if (query.ShowListCounts)
                stringBuilder.Append($" ({project.References.Length} / {project.ReferencedBy.Length})");

            logger.LogInformation(stringBuilder.ToString());

            if (query.ShowListTodos)
            {
                if (!project.IsPackageRef)
                    logger.LogInformation(string.Format(_todoPattern, "Migrate to PackageReferences"));

                if (!project.IsSdk)
                    logger.LogInformation(string.Format(_todoPattern, "Upgrade to SDK style formatting"));

                if (!project.IsNetStandard2)
                    logger.LogInformation(string.Format(_todoPattern, "Support NETSTANDARD2"));
            }
        }


        private void ShowProjectDetails(ProjectRecord project)
        {
            var status = project.DoesExist
                ? GetProjectStatus(project) ? "Done" : "Needs Work"
                : "Missing";

            logger.LogInformation(_separator);
            LogInformation("Project", project.Name);
            LogInformation("Path", project.Path);
            LogInformation("Status", status);

            if (project.DoesExist)
            {
                var todos = GetTodos(project);
                LogInformation("Todos", string.Join(", ", todos));
            }
            else
            {
                LogInformation("Exists", project.DoesExist);
            }

            LogInformation("References", $"{project.References.Length} project(s)");
            LogInformation("Referenced by", $"{project.ReferencedBy.Length} project(s)");
            LogInformation("Solutions", project.Solutions.Length);

            logger.LogInformation(_separator);
            return;

            void LogInformation(string fieldName, object fieldValue)
                => logger.LogInformation($"{fieldName,15}: {fieldValue}");
        }

        private static bool GetProjectStatus(ProjectRecord project) 
            => project is {IsSdk: true, IsNetStandard2: true, IsPackageRef: true};

        private static string GetProjectStatusTerm(ProjectRecord project)
        {
            return project switch
            {
                {DoesExist: false} => "Missing",
                {IsSdk: true, IsNetStandard2: true, IsPackageRef: true} => "Done",
                _ => "Needs Work"
            };
        }

        private static string[] GetTodos(ProjectRecord project)
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