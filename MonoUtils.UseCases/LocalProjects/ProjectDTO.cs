namespace MonoUtils.UseCases.LocalProjects;

public record ProjectDTO(string ProjectName, string ProjectPath, bool DoesExist)
{
    public string[] Todos { get; init; } = [];
    public string ProjectStatus { get; init; }
    public int ReferencedByCount { get; init; }
    public int ReferencesCount { get; init; }
    public int WixProjectsCount { get; init; }
    public int SolutionsCount { get; init; }
    public int BuildDefinitionCount { get; set; }

    public ProjectReferenceDTO[] References { get; set; }
    public ProjectReferenceDTO[] ReferencedBy { get; set; }
    public ProjectWixReferenceDTO[] WixProjects { get; set; }
    public ProjectSolutionDTO[] Solutions { get; set; }
    public string[] BuildNames { get; set; }
}