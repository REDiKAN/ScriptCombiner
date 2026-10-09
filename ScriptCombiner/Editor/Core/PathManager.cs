using System.IO;
using UnityEngine;

public static class PathManager
{
    public static string ConvertToFullPath(string path)
    {
        if (path.StartsWith("Assets/") || path.StartsWith("Assets\\"))
            return Path.Combine(Application.dataPath, path.Substring(7));
        return path;
    }

    public static string ConvertToRelativePath(string absolutePath)
    {
        if (absolutePath.StartsWith(Application.dataPath))
            return "Assets" + absolutePath.Substring(Application.dataPath.Length);
        return null;
    }

    public static bool IsCSharpFile(string path)
    {
        return Path.GetExtension(path)?.ToLower() == ".cs";
    }
}