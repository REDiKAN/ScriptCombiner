# Script Combiner: Technical Documentation

## 1. Architectural Overview

The tool is engineered following SOLID principles with strict layer separation. This ensures high testability and allows the business logic to be reused outside the Unity Editor context (e.g., in CI/CD pipelines or CLI utilities).

* **UI Layer (`ScriptCombinerWindow`)**: Handles IMGUI rendering, `Event.current` processing (Drag & Drop), and window state management. Contains zero parsing logic.
* **Core Layer (`ScriptProcessor`, `CSharpCommentRemover`)**: Pure text processing algorithms. Completely decoupled from `UnityEditor` or `UnityEngine` dependencies.
* **I/O Layer (`FileReader`)**: Manages asynchronous file streaming and byte-level BOM (Byte Order Mark) analysis for precise encoding detection.
* **Models (`FileStatistics`, `ProcessorOptions`)**: Strongly typed DTOs (Data Transfer Objects) for safe data transition across layers.

## 2. Deep Dive into Core Features

### 2.1. State Machine Parser (Comment Removal)
Unlike standard Regex-based solutions that break on verbatim strings (`@" /* not a comment */ "`) or escaped quotes (`" \"" "`), the `CSharpCommentRemover` utilizes a custom State Machine. 

The algorithm tracks the following states:
1. `inString` (standard strings, accounting for escape sequences like `\"`).
2. `inVerbatimString` (strings prefixed with `@`, where backslashes are ignored).
3. `inSingleLineComment` (`//`).
4. `inMultiLineComment` (`/* ... */`).

This guarantees 100% accuracy when stripping comments without corrupting string literals containing SQL queries, JSON payloads, or regex patterns.

### 2.2. Auto-Encoding Detection
The `FileReader` does not rely on system locale. The encoding resolution process:
1. Reads the raw byte array (`byte[]`).
2. Scans for BOM signatures: `EF BB BF` (UTF-8), `FF FE` (Unicode), `FE FF` (BigEndianUnicode).
3. If no BOM is present, applies heuristic analysis to detect Cyrillic (Windows-1251) or valid UTF-8 sequences.

## 3. Extensibility Guide

The architecture supports adding new code processors via interfaces. To implement custom cleanup logic (e.g., stripping `#pragma warning` directives or custom tags), follow the Strategy pattern.

### Step 1: Create a Processor
Implement the `ITextProcessor` interface for isolated logic.

```csharp
using System.Text.RegularExpressions;

public interface ITextProcessor
{
    string Execute(string content);
}

public class PragmaWarningProcessor : ITextProcessor
{
    public string Execute(string content)
    {
        return Regex.Replace(content, @"^\s*#pragma\s+warning\s+.*$", string.Empty, RegexOptions.Multiline);
    }
}