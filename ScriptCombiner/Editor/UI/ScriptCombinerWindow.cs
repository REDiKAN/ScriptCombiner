using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public class ScriptCombinerWindow : EditorWindow
{
    private Vector2 scrollPosition;
    private Vector2 previewScrollPosition;
    private List<string> selectedPaths = new List<string>();
    private Encoding selectedEncoding = Encoding.UTF8;
    private ScriptStatistics statistics = new ScriptStatistics();
    private string previewContent = "";
    private int selectedTab = 0;
    private string[] tabTitles = { "Configuration", "Preview" };
    private bool consolidateUsings = true;
    private bool enableExclusions = false;
    private string exclusionPatterns = "Test, Temp, AssemblyInfo";
    private bool cleanupCode = false;
    private bool removeComments = false;
    private bool removeEmptyLines = false;
    private bool removeRegions = false;
    private bool detailedStats = false;

    [MenuItem("Tools/Combine Scripts")]
    public static void ShowWindow()
    {
        var window = GetWindow<ScriptCombinerWindow>("Script Combiner");
        window.minSize = new Vector2(450, 600);
    }

    private void OnGUI()
    {
        selectedTab = GUILayout.Toolbar(selectedTab, tabTitles);
        if (selectedTab == 0) RenderConfigTab();
        else RenderPreviewTab();
    }

    private void RenderConfigTab()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        GUILayout.Space(5);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        RenderSectionHeader("Generation Settings");
        GUILayout.Space(5);
        RenderEncodingSelection();
        EditorGUILayout.Space(5);
        RenderAdvancedOptions();
        EditorGUILayout.EndVertical();
        GUILayout.Space(10);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        RenderSectionHeader("File Selection");
        RenderSelectedPaths();
        GUILayout.Space(5);
        RenderActionButtons();
        EditorGUILayout.EndVertical();
        GUILayout.Space(10);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        RenderSectionHeader("Statistics & Output");
        RenderStatistics();
        GUILayout.Space(5);
        RenderOutputButtons();
        EditorGUILayout.EndVertical();
        GUILayout.Space(10);
        EditorGUILayout.EndScrollView();
    }

    private void RenderPreviewTab()
    {
        EditorGUILayout.HelpBox("Preview of the generated text.", MessageType.Info);
        GUILayout.Space(5);
        if (GUILayout.Button("Regenerate Preview", GUILayout.Height(30))) _ = GeneratePreviewContentAsync();
        GUILayout.Space(5);
        previewScrollPosition = EditorGUILayout.BeginScrollView(previewScrollPosition, GUILayout.ExpandHeight(true));
        EditorGUI.BeginDisabledGroup(true);
        var textStyle = new GUIStyle(EditorStyles.textArea) { fontSize = 12, font = EditorStyles.label.font };
        EditorGUILayout.TextArea(previewContent, textStyle, GUILayout.ExpandHeight(true));
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndScrollView();
    }

    private void RenderSectionHeader(string title)
    {
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        Rect rect = GUILayoutUtility.GetLastRect();
        rect.y += rect.height - 2;
        rect.height = 1;
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
    }

    private void RenderAdvancedOptions()
    {
        consolidateUsings = EditorGUILayout.Toggle(new GUIContent("Consolidate Usings"), consolidateUsings);
        enableExclusions = EditorGUILayout.Toggle(new GUIContent("Enable Exclusions"), enableExclusions);
        EditorGUI.BeginDisabledGroup(!enableExclusions);
        exclusionPatterns = EditorGUILayout.TextField("Exclude Patterns (comma sep):", exclusionPatterns);
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.Space(5);
        cleanupCode = EditorGUILayout.Foldout(cleanupCode, "Code Cleanup Options", true);
        if (cleanupCode)
        {
            EditorGUI.indentLevel++;
            removeComments = EditorGUILayout.Toggle("Remove Comments", removeComments);
            removeEmptyLines = EditorGUILayout.Toggle("Remove Empty Lines", removeEmptyLines);
            removeRegions = EditorGUILayout.Toggle("Remove Regions", removeRegions);
            EditorGUI.indentLevel--;
        }
        detailedStats = EditorGUILayout.Toggle(new GUIContent("Detailed Statistics"), detailedStats);
    }

    private void RenderEncodingSelection()
    {
        GUILayout.Label("Target Encoding:", EditorStyles.miniLabel);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("UTF-8", GUILayout.Height(25))) selectedEncoding = Encoding.UTF8;
        if (GUILayout.Button("ANSI", GUILayout.Height(25))) selectedEncoding = Encoding.Default;
        if (GUILayout.Button("Win-1251", GUILayout.Height(25))) selectedEncoding = Encoding.GetEncoding(1251);
        GUILayout.EndHorizontal();
    }

    private void RenderSelectedPaths()
    {
        Rect dropArea = GUILayoutUtility.GetRect(0.0f, 45.0f, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "Drag & Drop Files/Folders Here", EditorStyles.centeredGreyMiniLabel);
        HandleDragAndDrop(dropArea);
        var listRect = GUILayoutUtility.GetRect(0.0f, 100.0f, GUILayout.ExpandWidth(true));
        GUI.Box(listRect, "");
        var listScroll = EditorGUILayout.BeginScrollView(Vector2.zero, GUILayout.Height(100));
        GUILayout.BeginHorizontal();
        GUILayout.Space(5);
        EditorGUILayout.BeginVertical();
        for (int i = 0; i < selectedPaths.Count; i++)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(selectedPaths.Count > 0 && Directory.Exists(PathManager.ConvertToFullPath(selectedPaths[i])) ? "F " : "f ", GUILayout.Width(20));
            EditorGUILayout.LabelField(selectedPaths[i], EditorStyles.miniLabel, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                selectedPaths.RemoveAt(i);
                _ = UpdateStatisticsAsync();
                EditorGUILayout.EndScrollView();
                GUIUtility.ExitGUI();
                return;
            }
            GUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
        GUILayout.EndHorizontal();
        EditorGUILayout.EndScrollView();
    }

    private void RenderActionButtons()
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Selected", GUILayout.Height(25))) AddSelectedInProject();
        if (GUILayout.Button("Add Folder", GUILayout.Height(25))) AddFolder();
        if (GUILayout.Button("Clear", GUILayout.Height(25)))
        {
            selectedPaths.Clear();
            statistics.Clear();
            previewContent = "";
        }
        GUILayout.EndHorizontal();
    }

    private void RenderStatistics()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.BeginVertical(GUILayout.Width(150));
        EditorGUILayout.LabelField("Files: " + statistics.TotalFiles, EditorStyles.miniBoldLabel);
        EditorGUILayout.LabelField("Size: " + statistics.TotalSizeKB.ToString("F2") + " KB", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();
        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
        if (detailedStats)
        {
            EditorGUILayout.LabelField($"Code: {statistics.CodeLines} | Comment: {statistics.CommentLines} | Empty: {statistics.BlankLines}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Classes: {statistics.TotalClasses} | Methods: {statistics.TotalMethods}", EditorStyles.miniLabel);
        }
        else
        {
            EditorGUILayout.LabelField($"Lines: {statistics.TotalLines}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Classes: {statistics.TotalClasses} | Methods: {statistics.TotalMethods}", EditorStyles.miniLabel);
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
    }

    private void RenderOutputButtons()
    {
        EditorGUI.BeginDisabledGroup(selectedPaths.Count == 0);
        if (GUILayout.Button("Save Combined Scripts To File...", GUILayout.Height(30))) _ = SaveCombinedScriptsAsync();
        GUILayout.Space(5);
        if (GUILayout.Button("Copy to Clipboard", GUILayout.Height(30))) _ = CopyCombinedScriptsAsync();
        EditorGUI.EndDisabledGroup();
    }

    private void HandleDragAndDrop(Rect dropArea)
    {
        Event evt = Event.current;
        if (evt.type == EventType.DragUpdated)
        {
            if (dropArea.Contains(evt.mousePosition)) { DragAndDrop.visualMode = DragAndDropVisualMode.Copy; evt.Use(); }
        }
        else if (evt.type == EventType.DragPerform)
        {
            if (dropArea.Contains(evt.mousePosition))
            {
                DragAndDrop.AcceptDrag();
                bool added = false;
                foreach (UnityEngine.Object obj in DragAndDrop.objectReferences)
                {
                    string path = AssetDatabase.GetAssetPath(obj);
                    if (!string.IsNullOrEmpty(path) && !selectedPaths.Contains(path))
                    {
                        if (Directory.Exists(path) || PathManager.IsCSharpFile(path)) { selectedPaths.Add(path); added = true; }
                    }
                }
                foreach (string path in DragAndDrop.paths)
                {
                    if (!string.IsNullOrEmpty(path) && !selectedPaths.Contains(path))
                    {
                        string relativePath = PathManager.ConvertToRelativePath(path);
                        if (!string.IsNullOrEmpty(relativePath)) selectedPaths.Add(relativePath);
                        else selectedPaths.Add(path);
                        added = true;
                    }
                }
                if (added) _ = UpdateStatisticsAsync();
                evt.Use();
            }
        }
    }

    private void AddSelectedInProject()
    {
        bool added = false;
        foreach (UnityEngine.Object obj in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(path) && (Directory.Exists(path) || PathManager.IsCSharpFile(path)))
            {
                if (!selectedPaths.Contains(path)) { selectedPaths.Add(path); added = true; }
            }
        }
        if (added) _ = UpdateStatisticsAsync();
    }

    private void AddFolder()
    {
        string folder = EditorUtility.OpenFolderPanel("Select Folder", Application.dataPath, "");
        if (!string.IsNullOrEmpty(folder))
        {
            string relativePath = folder.StartsWith(Application.dataPath) ? "Assets" + folder.Substring(Application.dataPath.Length) : folder;
            if (!selectedPaths.Contains(relativePath))
            {
                selectedPaths.Add(relativePath);
                _ = UpdateStatisticsAsync();
            }
        }
    }

    private async Task UpdateStatisticsAsync()
    {
        statistics.Clear();
        var processor = new ScriptProcessor();
        var tasks = new List<Task<FileStatistics>>();

        foreach (string path in selectedPaths)
        {
            string fullPath = PathManager.ConvertToFullPath(path);
            if (Directory.Exists(fullPath))
            {
                foreach (string file in Directory.GetFiles(fullPath, "*.cs", System.IO.SearchOption.AllDirectories))
                {
                    if (IsExcluded(file)) continue;
                    tasks.Add(processor.ProcessFileAsync(file, detailedStats));
                }
            }
            else if (File.Exists(fullPath) && PathManager.IsCSharpFile(fullPath))
            {
                if (IsExcluded(fullPath)) continue;
                tasks.Add(processor.ProcessFileAsync(fullPath, detailedStats));
            }
        }

        var results = await Task.WhenAll(tasks);
        foreach (var result in results) statistics.Add(result);
        Repaint();
    }

    private bool IsExcluded(string filePath)
    {
        if (!enableExclusions || string.IsNullOrEmpty(exclusionPatterns)) return false;
        string fileName = Path.GetFileName(filePath);
        var patterns = exclusionPatterns.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
        return patterns.Any(pattern => fileName.Contains(pattern.Trim()));
    }

    private async Task GeneratePreviewContentAsync()
    {
        if (selectedPaths.Count == 0) { previewContent = "// No files selected."; return; }
        var options = new ProcessorOptions
        {
            ConsolidateUsings = consolidateUsings,
            RemoveComments = removeComments && cleanupCode,
            RemoveEmptyLines = removeEmptyLines && cleanupCode,
            RemoveRegions = removeRegions && cleanupCode,
            IsDetailed = detailedStats,
            ExclusionCheck = IsExcluded
        };
        var processor = new ScriptProcessor();
        previewContent = await processor.GenerateCombinedTextAsync(selectedPaths, statistics, selectedEncoding, options);
        Repaint();
    }

    private async Task SaveCombinedScriptsAsync()
    {
        if (selectedPaths.Count == 0) { EditorUtility.DisplayDialog("Info", "Please select files or folders first", "OK"); return; }
        await GeneratePreviewContentAsync();
        string directory = Application.dataPath;
        if (selectedPaths.Count == 1 && !File.Exists(selectedPaths[0]) && Directory.Exists(PathManager.ConvertToFullPath(selectedPaths[0]))) directory = PathManager.ConvertToFullPath(selectedPaths[0]);
        string fileName = $"CombinedScripts_{selectedEncoding.EncodingName}.txt";
        string savePath = EditorUtility.SaveFilePanel("Save Combined Scripts", directory, fileName, "txt");
        if (!string.IsNullOrEmpty(savePath))
        {
            try
            {
                await WriteTextAsync(savePath, previewContent, selectedEncoding);
                EditorUtility.RevealInFinder(savePath);
                EditorUtility.DisplayDialog("Success", "Scripts combined successfully!", "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error writing combined file: {e.Message}");
                EditorUtility.DisplayDialog("Error", $"Error writing file: {e.Message}", "OK");
            }
        }
    }

    private async Task WriteTextAsync(string path, string content, Encoding encoding)
    {
        using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
        {
            byte[] bytes = encoding.GetBytes(content);
            await stream.WriteAsync(bytes, 0, bytes.Length);
        }
    }

    private async Task CopyCombinedScriptsAsync()
    {
        if (selectedPaths.Count == 0) { EditorUtility.DisplayDialog("Info", "Please select files or folders first", "OK"); return; }
        await GeneratePreviewContentAsync();
        GUIUtility.systemCopyBuffer = previewContent;
        Debug.Log($"Combined {statistics.TotalFiles} scripts copied to clipboard.");
    }
}