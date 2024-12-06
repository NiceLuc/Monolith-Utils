namespace MonoUtils.Domain.Data;

/// <summary>
/// When querying our database for assets, we can filter the results by the type of asset.
/// </summary>
public enum FilterType
{
    All,
    OnlyRequired,
    OnlyNonRequired
}