namespace MonoUtils.UseCases.LocalProjects;

public record ProjectReferenceDTO(string ProjectName, string ProjectPath, bool DoesExist)
{
    public bool IsSdk { get; set; }
    public bool IsNetStandard2 { get; set; }
    public bool IsPackageRef { get; set; }
}