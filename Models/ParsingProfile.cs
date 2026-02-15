namespace PlmXmlSyntax.Models;

public enum RootSearchStrategy
{
    Attribute, // Use rootInstanceRef only
    Heuristic, // Find "orphan" top-level nodes
    Flat,      // Collect all leaves without structure
    Auto       // Try Attribute, fallback to Heuristic, fallback to Flat
}

public enum MergeStrategy
{
    PrefixAll,      // id1 -> F1_id1 (Maximum isolation)
    UnifyByName,    // Parts with same Name share one ID
    UnifyByPartNo,  // Parts with same UserValue:PartNo share one ID
    FlattenHierarchy // All leaves under one root, no sub-assemblies
}

public class ParsingProfile
{
    public string Name { get; set; } = "Default";
    
    // Detection
    public string AuthorPattern { get; set; } = ".*"; 
    
    // Parser Logic
    public string PartNumberUserDataTitle { get; set; } = "Mehrnummern";
    public RootSearchStrategy RootStrategy { get; set; } = RootSearchStrategy.Auto;
    public bool ResolveIndirectLinks { get; set; } = true; 
    
    // Merge Logic
    public MergeStrategy ActiveMergeStrategy { get; set; } = MergeStrategy.PrefixAll;
    public string MergePrefixPattern { get; set; } = "F{0}_"; // {0} is file index
    
    // Guard Logic (IP Protection)
    public bool ScrubUserNames { get; set; } = false;
    public bool ScrubCosts { get; set; } = false;

    public static ParsingProfile Default => new();
}
