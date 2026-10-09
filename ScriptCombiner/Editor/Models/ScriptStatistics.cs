public class ScriptStatistics
{
    public int TotalFiles { get; private set; }
    public long TotalSizeBytes { get; private set; }
    public int TotalLines { get; private set; }
    public int TotalClasses { get; private set; }
    public int TotalMethods { get; private set; }
    public int TotalComments { get; private set; }
    public float TotalSizeKB => TotalSizeBytes / 1024f;
    public int CodeLines { get; private set; }
    public int BlankLines { get; private set; }
    public int CommentLines { get; private set; }

    public void Clear()
    {
        TotalFiles = 0;
        TotalSizeBytes = 0;
        TotalLines = 0;
        TotalClasses = 0;
        TotalMethods = 0;
        TotalComments = 0;
        CodeLines = 0;
        BlankLines = 0;
        CommentLines = 0;
    }

    public void Add(FileStatistics fileStats)
    {
        TotalFiles++;
        TotalSizeBytes += fileStats.SizeBytes;
        TotalLines += fileStats.LineCount;
        TotalClasses += fileStats.ClassCount;
        TotalMethods += fileStats.MethodCount;
        TotalComments += fileStats.CommentCount;
        CodeLines += fileStats.CodeLines;
        BlankLines += fileStats.BlankLines;
        CommentLines += fileStats.CommentLines;
    }
}