// ─────────────────────────────────────────────────────────────────────────────
// ADD THIS to your existing App.cs  CreateRibbonPanel() / OnStartup() method
// inside the section where you register your other buttons.
// ─────────────────────────────────────────────────────────────────────────────

// Find your existing panel (or create a new one):
//   RibbonPanel panel = application.CreateRibbonPanel(tabName, "Trusses");

PushButtonData importTrussBtn = new PushButtonData(
    "ImportTrussCommand",               // internal name (must be unique)
    "Import\nTrusses",                  // button label (use \n for two lines)
    assemblyPath,                       // path to your Jcup.dll (same as other buttons)
    "Revit_Tab.ImportTrussCommand"      // full namespace.classname
)
{
    ToolTip = "Import 3D trusses from a 2D DWG plan file.",
    // LargeImage = ... (optional: add a 32x32 icon)
};

panel.AddItem(importTrussBtn);

// ─────────────────────────────────────────────────────────────────────────────
// ADD TO YOUR .csproj — NuGet packages needed:
//
//   No new NuGet needed for the core command.
//   AutoCAD COM reference: add via VS > References > COM tab:
//     "AutoCAD Type Library" (acax##enu.tlb)
//
//   In your .csproj confirm you have:
//     <COMReference Include="AutoCAD.Interop">
//       <Guid>{...</Guid>
//       <VersionMajor>1</VersionMajor>
//       ...
//     </COMReference>
//
//   OR use late-bound dynamic (already done in DwgParser.cs — no COM ref needed).
// ─────────────────────────────────────────────────────────────────────────────
