# Script Combiner Pro

A production-ready Unity Editor tool designed for consolidating, statically analyzing, and cleaning C# codebases. Engineered to streamline code reviews, prepare context for LLM analysis, and facilitate large-scale project refactoring.

<img width="450" height="722" alt="image_2026-10-09_19-35-01" src="https://github.com/user-attachments/assets/0d6a793b-e3a4-4d43-a1f3-5d0c03a124ab" />

## 🚀 Key Features

* **Asynchronous I/O**: Processes hundreds of files without blocking the Unity Editor UI thread utilizing `async/await` and `Task.WhenAll`.
* **Robust Parsing Engine**: Comment removal is implemented via a custom State Machine rather than Regex. This guarantees accurate parsing of verbatim strings (`@""`) and escaped characters.
* **Smart Consolidation**: Automatically extracts, deduplicates, and groups `using` directives at the top of the generated output.
* **Deep Code Metrics**: Calculates precise statistics, distinguishing between code, comments (including block comments), and blank lines.
* **Flexible Filtering**: Exclude generated code, tests, or temporary files using customizable comma-separated patterns.
* **Auto-Encoding Detection**: Seamlessly handles UTF-8 (with/without BOM), Unicode, and Windows-1251 encodings.

## 🏗 Architecture & Design

The tool is built adhering to SOLID principles, strictly separating concerns to ensure testability and maintainability:

* **UI Layer (`EditorWindow`)**: Handles IMGUI rendering, user input, and window state.
* **Core Layer (`ScriptProcessor`, `CSharpCommentRemover`)**: Contains pure business logic and parsing algorithms. Completely decoupled from `UnityEditor` dependencies.
* **I/O Layer (`FileReader`)**: Manages asynchronous file streaming and byte-level encoding detection.
* **Models (`FileStatistics`, `ProcessorOptions`)**: Strongly typed DTOs for safe data transfer across layers.

## 📦 Installation (Unity Package Manager)

1. Open **Window > Package Manager** in Unity.
2. Click the **`+`** button in the top-left corner and select **Add package from git URL...**
3. Enter the following URL:
   ```text
   https://github.com/REDiKAN/ScriptCombiner
