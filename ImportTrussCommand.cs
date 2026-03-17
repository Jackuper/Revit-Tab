using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;

namespace Revit_Tab
{
    /// <summary>
    /// Revit Add-in Command: Import 3D Trusses from DWG
    /// Ribbon button label: "Import Trusses"
    /// Add this to your existing ribbon tab in App.cs
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ImportTrussCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;

            try
            {
                // Collect all levels in the model
                var levels = new FilteredElementCollector(doc)
                    .OfClass(typeof(Level))
                    .Cast<Level>()
                    .OrderBy(l => l.Elevation)
                    .ToList();

                if (!levels.Any())
                {
                    TaskDialog.Show("Error", "No levels found in the current Revit model.");
                    return Result.Failed;
                }

                // Collect all structural framing family names loaded in the model
                var familyNames = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilySymbol))
                    .OfCategory(BuiltInCategory.OST_StructuralFraming)
                    .Cast<FamilySymbol>()
                    .Select(fs => fs.FamilyName)
                    .Distinct()
                    .OrderBy(n => n)
                    .ToList();

                // Show the dialog
                var dialog = new TrussImportDialog(doc)
                {
                    AvailableLevels = levels,
                    AvailableFamilyNames = familyNames
                };

                dialog.Populate();
                dialog.ShowDialog();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }

    /// <summary>
    /// Handles all Revit API calls to place structural framing members.
    /// Separated so it can be called from the dialog or unit tests.
    /// </summary>
    public static class TrussPlacementHelper
    {
        /// <summary>
        /// Places all framing members for a list of truss instances.
        /// Each instance uses the config for its DetectedType (or fallback to overrideType).
        /// Returns total count of placed elements.
        /// </summary>
        public static int PlaceAllTrusses(
            Document doc,
            List<TrussInstance> instances,
            TrussConfig config,
            string overrideType,       // used if instance.DetectedType is null or not in config
            double baseElevationFeet,
            string levelName)
        {
            Level level = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .FirstOrDefault(l => l.Name == levelName);

            if (level == null)
                throw new Exception($"Level '{levelName}' not found in the model.");

            int totalPlaced = 0;

            using (var tx = new Transaction(doc, "Import Trusses from DWG"))
            {
                tx.Start();

                foreach (var inst in instances)
                {
                    // Resolve truss type
                    string typeKey = inst.DetectedType;
                    if (typeKey == null || !config.TrussTypes.ContainsKey(typeKey))
                        typeKey = overrideType;
                    if (typeKey == null || !config.TrussTypes.ContainsKey(typeKey))
                        continue; // skip if no type resolved

                    var typeCfg = config.TrussTypes[typeKey];

                    // Resolve family symbols
                    var topSymbol    = GetFamilySymbol(doc, typeCfg.TopChordFamily,    typeCfg.TopChordType);
                    var bottomSymbol = GetFamilySymbol(doc, typeCfg.BottomChordFamily, typeCfg.BottomChordType);
                    var webSymbol    = GetFamilySymbol(doc, typeCfg.WebFamily,         typeCfg.WebType);

                    EnsureActive(doc, topSymbol);
                    EnsureActive(doc, bottomSymbol);
                    EnsureActive(doc, webSymbol);

                    var members = TrussBuilder.BuildMembers(inst, typeCfg, baseElevationFeet);

                    foreach (var m in members)
                    {
                        FamilySymbol symbol = m.Type == MemberType.TopChord    ? topSymbol
                                           : m.Type == MemberType.BottomChord ? bottomSymbol
                                                                               : webSymbol;
                        PlaceMember(doc, m, symbol, level, baseElevationFeet);
                        totalPlaced++;
                    }
                }

                tx.Commit();
            }

            return totalPlaced;
        }

        /// <summary>
        /// Places a single structural framing element between two 3D points.
        /// </summary>
        private static void PlaceMember(
            Document doc,
            TrussMember m,
            FamilySymbol symbol,
            Level level,
            double baseElevFeet)
        {
            // Revit internal units = decimal feet
            var pt1 = new XYZ(m.X1, m.Y1, m.Z1);
            var pt2 = new XYZ(m.X2, m.Y2, m.Z2);

            // Skip zero-length members (coincident points)
            if (pt1.DistanceTo(pt2) < 0.001) return;

            var line = Line.CreateBound(pt1, pt2);

            // Create structural framing beam between the two points
            // StructuralType.Beam works for all linear framing (chords, webs)
            var fi = doc.Create.NewFamilyInstance(
                line,
                symbol,
                level,
                StructuralType.Beam);

            // Set start/end offsets so the member sits at the correct absolute elevation
            // Revit places beams relative to the level elevation, so we offset by
            // the Z value minus the level elevation.
            double levelElev = level.Elevation;

            // For non-horizontal members (webs), Revit handles the 3D line directly.
            // For horizontal chords, confirm the z-offset parameter:
            var startOffsetParam = fi.get_Parameter(BuiltInParameter.STRUCTURAL_BEAM_END0_ELEVATION);
            var endOffsetParam   = fi.get_Parameter(BuiltInParameter.STRUCTURAL_BEAM_END1_ELEVATION);

            if (startOffsetParam != null && !startOffsetParam.IsReadOnly)
                startOffsetParam.Set(m.Z1 - levelElev);

            if (endOffsetParam != null && !endOffsetParam.IsReadOnly)
                endOffsetParam.Set(m.Z2 - levelElev);
        }

        /// <summary>
        /// Finds a FamilySymbol by family name + type name.
        /// Throws a descriptive error if not found so the user knows what to load.
        /// </summary>
        private static FamilySymbol GetFamilySymbol(Document doc, string familyName, string typeName)
        {
            var symbol = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_StructuralFraming)
                .Cast<FamilySymbol>()
                .FirstOrDefault(fs =>
                    string.Equals(fs.FamilyName, familyName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(fs.Name,        typeName,   StringComparison.OrdinalIgnoreCase));

            if (symbol == null)
                throw new Exception(
                    $"Structural framing type '{familyName} : {typeName}' is not loaded in this Revit model.\n\n" +
                    $"Please load the family first via Insert > Load Family.");

            return symbol;
        }

        private static void EnsureActive(Document doc, FamilySymbol symbol)
        {
            if (!symbol.IsActive)
                symbol.Activate();
        }
    }
}
