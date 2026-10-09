using System;

public class ProcessorOptions
{
    public bool ConsolidateUsings { get; set; }
    public bool RemoveComments { get; set; }
    public bool RemoveEmptyLines { get; set; }
    public bool RemoveRegions { get; set; }
    public bool IsDetailed { get; set; }
    public Func<string, bool> ExclusionCheck { get; set; }
}