# ACadScript

ACadScript is an advanced, highly extensible AutoCAD .NET plugin designed to supercharge drafting, structural detailing, and general AutoCAD productivity. At its core, it acts as a **dynamic C# script execution engine** within AutoCAD, allowing users to write, load, compile, and run C# macros on the fly without needing to restart AutoCAD or recompile the main plugin assembly.

## Key Features

### 🚀 Dynamic C# Script Execution
- **Command `ns`**: Running the `ns` command in AutoCAD launches the `AcadForm` user interface.
- **Roslyn Integration**: Powered by `Microsoft.CodeAnalysis.CSharp.Scripting`, ACadScript evaluates and executes C# scripts interactively.
- **Live Editing**: Edit and run scripts directly within the AutoCAD environment.

### 🏗️ Structural Engineering Tools (Struct)
A vast array of automation tools specifically tailored for civil and structural engineers:
- **Beam & Slab Automation**: Tools like `Beam From Plan.cs`, `Structural Beam Editor.cs`, `Draw Slab.cs`, and `Slab Upper/Lower Steel.cs`.
- **Detailing**: Detailed column generation (`Struct Column Detail.cs`), U-Steel dividing (`Structural Divide U Steel.cs`), and rebar text handling.
- **Tekla Integration**: Exporting/reading data relevant to Tekla (`gTekla.cs`, `_Tekla Build Purlin.cs`).

### 📝 Advanced Text & Dimensioning (Text / Dm)
Rich utilities to normalize and manage drafting text and dimensions:
- **Text Formatting**: `Text All To Current Font.cs`, `Text Increase.cs`, `Text Upper.cs`.
- **Annotation & Scaling**: Easy annotative scale removals or setups.
- **Smart Notes**: Tag elements with smart bounding boxes, automatic leaders, and reminders.
- **Dimensions**: Cross dimensions, ordinate dimensions, and automatic group dimensioning.

### 📐 Line, Polyline & Hatch Utilities (Ln)
Powerful algorithms to manipulate base geometric entities:
- **Polyline Processing**: Functions to weld (`Polyline Weld.cs`), simplify (`Polyline Simply.cs`), calculate areas (`Area Sum.cs`), and auto-straighten polylines.
- **Architectural Tools**: Quick generation of stairs, walls, elevators, roads, and ceilings from base polylines.
- **Hatch Processing**: Trace base points and modify hatch scales automatically.

### 📦 Block & Layout Management (Bk / Pl)
- **Block Automation**: Count blocks, batch create new blocks, align blocks with text, and manage furniture.
- **Viewport & Plotting**: Automated viewport extraction, layout clearing, and plotting data info extraction.

### 🔄 API / Integrations
Includes third-party libraries for advanced processing tasks:
- **OpenCVSharp4 & Emgu.CV**: Computer vision operations (e.g., shape recognition, image processing).
- **PDFsharp**: Read and overlay PDF data into AutoCAD environment.
- **Interop**: Excel / CSV data processing for schedule generations.

## Installation / Usage

1. Compile the project using **Visual Studio** (.NET Framework 4.8).
2. The output will be `AcadScript5.dll`.
3. Open AutoCAD and use the **`NETLOAD`** command to load the compiled `AcadScript5.dll`.
4. Type **`ns`** in the AutoCAD command line to bring up the ACadScript editor and execution interface.
5. Select a script from the `Sample`, `Structural`, or `Text` directories and hit Execute to run the automation logic dynamically!

## Project Structure

- `ACadScript.sln / .csproj`: The main .NET Framework 4.8 Class Library project.
- `AcadForm.cs`: The core WinForms editor/runner UI.
- `Cls / Public / Extent`: Core framework wrappers and helper classes standardizing AutoCAD database interactions (`IBlock`, `IDraw`, `DataView`).
- `Sample / Structural / Text / ...`: The repository of hundreds of pre-written C# macros ranging from quick drafting fixes to complex structural drawing automations.
