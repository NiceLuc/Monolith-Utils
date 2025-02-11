namespace MonoUtils.UseCases.LocalProjects;

public record ProjectWixReferenceDTO(string ProjectName, string ProjectPath, bool DoesExist)
{
    public bool IsSdk { get; init; }
    public bool IsHarvested { get; init; }
}