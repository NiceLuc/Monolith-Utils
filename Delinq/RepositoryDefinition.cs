namespace Delinq;

public class ConnectionStrings
{
    public string InCode { get; set; }
}

public enum SprocQueryType
{
    Unknown = 0,
    NonQuery = 1, // insert, update, delete
    Query = 2, // select
    Scalar = 3, // select count(*)
    ReturnValue = 4 // select @ReturnValue
}

public enum RepositoryMethodStatus
{
    Unknown = 0,
    OK = 1,
    NotAllParametersAreBeingUsed = 2,
    SprocNotFound = 3,
    MissingSprocParameters = 4,
    TooManySprocParameters = 5,
    InvalidQueryType = 6,
}

public class RepositoryDefinition
{
    public string FilePath { get; set; }
    public List<RepositoryMethod> Methods { get; set; } = new();
}

public class RepositoryMethod
{
    public RepositoryMethodStatus Status { get; set; }

    public string Name { get; set; }
    public string ReturnType { get; set; }
    public bool IsEnumerable => ReturnType.StartsWith("IEnumerable<");

    public List<RepositoryParameter> Parameters { get; set; } = new();

    public int NumberOfOutParameters { get; set; }
    public bool HasReturnParameter { get; set; }
    public SprocQueryType CurrentQueryType { get; set; }

    public SprocDefinition StoredProcedure { get; set; }
}

public class RepositoryParameter
{
    public string Type { get; set; }
    public string Name { get; set; }
    public string Modifier { get; set; }
    public bool IsNullable => Type.EndsWith("?");
}

public class SprocDefinition
{
    public string Name { get; set; }
    public SprocQueryType QueryType { get; set; }
    public List<SprocParameter> Parameters { get; set; } = new();
}

public class SprocParameter
{
    public string Name { get; set; }
    public string Type { get; set; }
    public string Modifier { get; set; }
}
