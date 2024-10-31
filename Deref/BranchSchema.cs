namespace Deref;

public class BranchSchema
{
    public List<Project> Projects { get; set; }
    public List<Solution> Solutions { get; set; }
    public List<ProjectReference> ProjectReferences { get; set; }

    /*
     * Projects
     *  - Key (string)
     *  - Name (string)
     *  - Path (string)
     * Solutions
     *  - Key (string)
     *  - Name (string)
     *  - Path (string)
     *  - Projects
     *      - Key (string)
     * ProjectReferences
     *  - Key (string)
     *  - Uses
     *      - ProjectKey (string)
     */

    public class Project
    {
        public string Name { get; set; }
        public string Path { get; set; }
    }

    public class Solution
    {
        public string Name { get; set; }
        public string Path { get; set; }

        public List<string> ProjectKeys { get; set; }
    }

    public class ProjectReference
    {
        public string Name { get; set; }
        public List<string> UsedBy { get; set; }
        public List<string> Uses { get; set; }
    }
}