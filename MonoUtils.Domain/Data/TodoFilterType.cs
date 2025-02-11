namespace MonoUtils.Domain.Data;

[Flags]
public enum TodoFilterType
{
    NoFilter = 0,
    SdkProjects = 1,
    PackageRefs = 2,
    NetStandard2 = 4,
    All = SdkProjects | PackageRefs | NetStandard2
}