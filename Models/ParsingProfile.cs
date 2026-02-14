namespace PlmXmlSyntax.Models;

public enum RootSearchStrategy
{
    Attribute, // Use rootInstanceRef only
    Heuristic, // Find "orphan" top-level nodes
    Flat,      // Collect all leaves without structure
    Auto       // Try Attribute, fallback to Heuristic, fallback to Flat
}

public class ParsingProfile
{
    public string Name { get; set; } = "Universal";
    
    // Detection
    public string AuthorPattern { get; set; } = ".*"; 
    
    // Logic Configuration
    public string PartNumberUserDataTitle { get; set; } = "Mehrnummern";
    public RootSearchStrategy RootStrategy { get; set; } = RootSearchStrategy.Auto;
    public bool ResolveIndirectLinks { get; set; } = true; 
    
    public static ParsingProfile Default => new();
    
    public static ParsingProfile Teamcenter => new()
    {
        Name = "Siemens Teamcenter",
        AuthorPattern = "Teamcenter",
        PartNumberUserDataTitle = "Mehrnummern"
    };

    public static ParsingProfile exentra => new()
    {
        Name = "exentra DPTW",
        AuthorPattern = "exentra",
        RootStrategy = RootSearchStrategy.Auto
    };
}
