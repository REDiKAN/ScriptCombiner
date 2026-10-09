using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

public class ScriptProcessor
{
    private readonly CSharpCommentRemover _commentRemover = new CSharpCommentRemover();

    public async Task<FileStatistics> ProcessFileAsync(string filePath, bool detailed)
    {
        string content = await FileReader.ReadFileWithAutoEncodingAsync(filePath);
        FileInfo fileInfo = new FileInfo(filePath);
        var stats = new FileStatistics
        {
            FilePath = filePath,
            SizeBytes = fileInfo.Length,
            LineCount = content.Split('\n').Length,
            ClassCount = CountOccurrences(content, "class "),
            MethodCount = CountMethods(content),
            CommentCount = CountComments(content)
        };

        if (detailed)
        {
            AnalyzeLines(content, out int code, out int blank, out int comments);
            stats.CodeLines = code;
            stats.BlankLines = blank;
            stats.CommentLines = comments;
        }
        return stats;
    }

    public async Task<string> GenerateCombinedTextAsync(List<string> paths, ScriptStatistics statistics, Encoding encoding, ProcessorOptions options)
    {
        var allScriptPaths = CollectScriptPaths(paths, options.ExclusionCheck);
        if (allScriptPaths.Count == 0) return "// No .cs files found to combine.";

        HashSet<string> allUsings = new HashSet<string>();
        List<FileContent> processedFiles = new List<FileContent>();

        foreach (string scriptPath in allScriptPaths)
        {
            string content = await FileReader.ReadFileWithAutoEncodingAsync(scriptPath);

            if (options.RemoveComments) content = _commentRemover.Execute(content);
            if (options.RemoveRegions) content = RemoveRegions(content);
            if (options.ConsolidateUsings) content = ExtractUsings(content, allUsings);
            if (options.RemoveEmptyLines) content = Regex.Replace(content, @"^\s*$[\r\n]*", string.Empty, RegexOptions.Multiline);

            processedFiles.Add(new FileContent { Path = scriptPath, Content = content });
        }

        return BuildCombinedText(processedFiles, statistics, encoding, options.ConsolidateUsings, allUsings);
    }

    private string BuildCombinedText(List<FileContent> files, ScriptStatistics stats, Encoding encoding, bool consolidateUsings, HashSet<string> usings)
    {
        StringBuilder combinedText = new StringBuilder();
        combinedText.AppendLine("// ===== Combined Scripts Header =====");
        combinedText.AppendLine($"// Generation Time: {System.DateTime.Now}");
        combinedText.AppendLine($"// Encoding: {encoding.EncodingName}");
        combinedText.AppendLine($"// Total Files: {files.Count}");
        combinedText.AppendLine("// =================================");
        combinedText.AppendLine();

        if (consolidateUsings && usings.Count > 0)
        {
            combinedText.AppendLine("// ===== Consolidated Usings =====");
            foreach (var u in usings.OrderBy(x => x)) combinedText.AppendLine(u);
            combinedText.AppendLine("// =================================");
            combinedText.AppendLine();
        }

        for (int i = 0; i < files.Count; i++)
        {
            combinedText.AppendLine($"//==== File {i + 1} of {files.Count}: {files[i].Path} ====");
            combinedText.AppendLine(files[i].Content);
            combinedText.AppendLine();
        }

        combinedText.AppendLine("// ============ Statistics =============");
        combinedText.AppendLine($"// Total Files: {stats.TotalFiles}");
        combinedText.AppendLine($"// Total Size: {stats.TotalSizeKB:F2} KB");
        if (stats.CodeLines > 0)
        {
            combinedText.AppendLine($"// Code Lines: {stats.CodeLines}");
            combinedText.AppendLine($"// Comment Lines: {stats.CommentLines}");
            combinedText.AppendLine($"// Blank Lines: {stats.BlankLines}");
        }
        else
        {
            combinedText.AppendLine($"// Total Lines: {stats.TotalLines}");
        }
        combinedText.AppendLine($"// Classes: {stats.TotalClasses}");
        combinedText.AppendLine($"// Methods: {stats.TotalMethods}");
        combinedText.AppendLine($"// Comments (Blocks): {stats.TotalComments}");
        combinedText.AppendLine("// =====================================");

        return combinedText.ToString();
    }

    private string RemoveRegions(string content)
    {
        content = Regex.Replace(content, @"#region\s.*", "", RegexOptions.Multiline);
        return Regex.Replace(content, @"#endregion", "", RegexOptions.Multiline);
    }

    private string ExtractUsings(string content, HashSet<string> usingsSet)
    {
        StringBuilder sb = new StringBuilder();
        bool inHeader = true;
        bool hasNamespace = false;
        using (var reader = new StringReader(content))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                string trimmed = line.Trim();
                if (trimmed.StartsWith("using ") && trimmed.EndsWith(";"))
                {
                    usingsSet.Add(trimmed);
                    if (!inHeader) sb.AppendLine(line);
                    continue;
                }
                if (trimmed.StartsWith("namespace ")) hasNamespace = true;
                if (inHeader && !string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith("//") && !trimmed.StartsWith("using ") && !trimmed.StartsWith("namespace")) inHeader = false;
                if (!inHeader || trimmed.StartsWith("//") || (hasNamespace && trimmed.StartsWith("namespace"))) sb.AppendLine(line);
            }
        }
        return sb.ToString();
    }

    private void AnalyzeLines(string content, out int code, out int blank, out int comments)
    {
        code = 0; blank = 0; comments = 0;
        var lines = content.Split('\n');
        bool inBlockComment = false;
        foreach (var line in lines)
        {
            string trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed)) { blank++; continue; }
            if (trimmed.StartsWith("/*") || trimmed.Contains("/*")) inBlockComment = true;
            if (trimmed.Contains("*/")) inBlockComment = false;
            if (inBlockComment || trimmed.StartsWith("//") || trimmed.StartsWith("/*")) { comments++; continue; }
            code++;
        }
    }

    private int CountOccurrences(string text, string pattern)
    {
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(pattern, index, System.StringComparison.Ordinal)) != -1)
        {
            index += pattern.Length;
            count++;
        }
        return count;
    }

    private int CountMethods(string content)
    {
        string[] returnTypes = { "void", "int", "string", "float", "bool", "double", "object" };
        string[] excludedKeywords = { "class", "struct", "interface", "enum" };
        return content.Split('\n').Count(line => line.Trim().Contains("(") && line.Trim().Contains(")") && returnTypes.Any(rt => line.Contains(rt)) && !excludedKeywords.Any(ek => line.Contains(ek)));
    }

    private int CountComments(string content)
    {
        return content.Split('\n').Count(line => line.Trim().StartsWith("//") || line.Trim().Contains("/*") || line.Trim().Contains("*/"));
    }

    private List<string> CollectScriptPaths(List<string> paths, System.Func<string, bool> exclusionCheck)
    {
        var allScriptPaths = new List<string>();
        foreach (string path in paths)
        {
            string fullPath = PathManager.ConvertToFullPath(path);
            if (Directory.Exists(fullPath))
            {
                allScriptPaths.AddRange(Directory.GetFiles(fullPath, "*.cs", System.IO.SearchOption.AllDirectories).Where(p => !exclusionCheck(p)));
            }
            else if (File.Exists(fullPath) && PathManager.IsCSharpFile(fullPath))
            {
                if (!exclusionCheck(fullPath)) allScriptPaths.Add(fullPath);
            }
        }
        return allScriptPaths.Distinct().ToList();
    }

    private class FileContent { public string Path; public string Content; }
}