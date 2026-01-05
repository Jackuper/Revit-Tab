using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Revit_Tab
{
    [Transaction(TransactionMode.Manual)]
    public class CreateKingStudsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;

            try
            {
                // Find the symbol in the project first; if missing, attempt to load it from the deployed add-in folder.
                FamilySymbol studType = FindStudSymbol(doc);
                if (studType == null)
                {
                    studType = TryLoadStudFamilyAndFindSymbol(doc);
                }

                if (studType == null)
                {
                    string assemblyPath = Assembly.GetExecutingAssembly().Location;
                    string assemblyDir = Path.GetDirectoryName(assemblyPath);
                    string familyPath = Path.Combine(assemblyDir, "Families", "Stud.rfa");
                    TaskDialog.Show("King Studs", "Could not find or load a stud family type.\nExpected family file at:\n" + familyPath);
                    return Result.Cancelled;
                }

                // Collect all doors and windows from linked models (and also from host model, in case they exist there too)
                var openingSources = CollectOpeningsFromHostAndLinks(doc);

                using (Transaction t = new Transaction(doc, "Create King Studs"))
                {
                    t.Start();

                    // Activate the type if not already active
                    if (!studType.IsActive)
                        studType.Activate();

                    foreach (var source in openingSources)
                    {
                        FamilyInstance fi = source.Opening;
                        if (fi == null) continue;

                        LocationPoint locPt = fi.Location as LocationPoint;
                        if (locPt == null) continue;

                        // Linked elements are transformed into host coordinates. Host elements use identity transform.
                        XYZ originHost = source.Transform.OfPoint(locPt.Point);

                        // Direction comes from host wall (in the same document as the opening), then transformed.
                        Wall sourceHostWall = fi.Host as Wall;
                        if (sourceHostWall == null) continue;

                        LocationCurve wallLocCurve = sourceHostWall.Location as LocationCurve;
                        if (wallLocCurve?.Curve == null) continue;

                        XYZ p0Host = source.Transform.OfPoint(wallLocCurve.Curve.GetEndPoint(0));
                        XYZ p1Host = source.Transform.OfPoint(wallLocCurve.Curve.GetEndPoint(1));
                        XYZ wallLineDirHost = (p1Host - p0Host).Normalize();

                        double offset = UnitUtils.ConvertToInternalUnits(3.5, UnitTypeId.Inches);

                        XYZ leftHost = originHost - wallLineDirHost * offset;
                        XYZ rightHost = originHost + wallLineDirHost * offset;

                        // Place instances in the host document only.
                        // When reading from a link, we select the closest host level by elevation.
                        Level hostLevel = GetBestHostLevel(doc, source.LevelElevation);
                        if (hostLevel == null)
                        {
                            continue;
                        }

                        doc.Create.NewFamilyInstance(leftHost, studType, hostLevel, StructuralType.NonStructural);
                        doc.Create.NewFamilyInstance(rightHost, studType, hostLevel, StructuralType.NonStructural);
                    }

                    t.Commit();
                }

                TaskDialog.Show("King Studs", "King studs created successfully.");
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        private sealed class OpeningSource
        {
            public FamilyInstance Opening { get; set; }
            public Transform Transform { get; set; }
            public double LevelElevation { get; set; }
        }

        private List<OpeningSource> CollectOpeningsFromHostAndLinks(Document hostDoc)
        {
            var result = new List<OpeningSource>();

            // Host doc openings
            result.AddRange(CollectOpeningsFromDocument(hostDoc, Transform.Identity));

            // Linked doc openings
            var linkInstances = new FilteredElementCollector(hostDoc)
                .OfClass(typeof(RevitLinkInstance))
                .Cast<RevitLinkInstance>()
                .ToList();

            foreach (var link in linkInstances)
            {
                Document linkedDoc = link.GetLinkDocument();
                if (linkedDoc == null) continue;

                Transform linkTransform = link.GetTotalTransform();
                result.AddRange(CollectOpeningsFromDocument(linkedDoc, linkTransform));
            }

            return result;
        }

        private IEnumerable<OpeningSource> CollectOpeningsFromDocument(Document doc, Transform transformToHost)
        {
            var openings = new List<Element>();
            openings.AddRange(new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Doors)
                .WhereElementIsNotElementType()
                .ToList());

            openings.AddRange(new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Windows)
                .WhereElementIsNotElementType()
                .ToList());

            foreach (var opening in openings)
            {
                var fi = opening as FamilyInstance;
                if (fi == null) continue;

                double elev = 0.0;
                Level lvl = doc.GetElement(fi.LevelId) as Level;
                if (lvl != null)
                {
                    elev = lvl.Elevation;
                }

                yield return new OpeningSource
                {
                    Opening = fi,
                    Transform = transformToHost,
                    LevelElevation = elev
                };
            }
        }

        private Level GetBestHostLevel(Document hostDoc, double targetElevation)
        {
            var levels = new FilteredElementCollector(hostDoc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .ToList();

            if (levels.Count == 0) return null;

            // Match by closest elevation (internal units). This works even when link levels aren't named the same.
            return levels
                .OrderBy(l => Math.Abs(l.Elevation - targetElevation))
                .FirstOrDefault();
        }

        private FamilySymbol FindStudSymbol(Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .FirstOrDefault(f =>
                    (f.Family != null && !string.IsNullOrWhiteSpace(f.Family.Name) && f.Family.Name.Contains("Stud")) ||
                    (!string.IsNullOrWhiteSpace(f.Name) && f.Name.Contains("Stud")));
        }

        private FamilySymbol TryLoadStudFamilyAndFindSymbol(Document doc)
        {
            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            string assemblyDir = Path.GetDirectoryName(assemblyPath);
            string familyPath = Path.Combine(assemblyDir, "Families", "Stud.rfa");

            if (!File.Exists(familyPath))
            {
                return null;
            }

            string expectedFamilyName = Path.GetFileNameWithoutExtension(familyPath);

            using (Transaction t = new Transaction(doc, "Load Stud Family"))
            {
                t.Start();
                bool loaded = doc.LoadFamily(familyPath, new AlwaysLoadFamilyOptions(), out Family loadedFamily);
                t.Commit();

                // If the family is already loaded, 'loaded' may be false and 'loadedFamily' may be null.
                // So we try several fallbacks.
                if (loadedFamily != null)
                {
                    ElementId symId = loadedFamily.GetFamilySymbolIds().FirstOrDefault();
                    if (symId != null) return doc.GetElement(symId) as FamilySymbol;
                }

                // Prefer family name that matches the file name (common convention)
                var byExpectedName = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilySymbol))
                    .Cast<FamilySymbol>()
                    .FirstOrDefault(s => s.Family != null && s.Family.Name == expectedFamilyName);

                if (byExpectedName != null) return byExpectedName;

                // Fallback to previous heuristic
                FamilySymbol symbol = FindStudSymbol(doc);
                if (symbol != null) return symbol;

                return null;
                }
            }

        private sealed class AlwaysLoadFamilyOptions : IFamilyLoadOptions
        {
            public bool OnFamilyFound(bool familyInUse, out bool overwriteParameterValues)
            {
                overwriteParameterValues = true;
                return true;
            }

            public bool OnSharedFamilyFound(Family sharedFamily, bool familyInUse, out FamilySource source, out bool overwriteParameterValues)
            {
                source = FamilySource.Family;
                overwriteParameterValues = true;
                return true;
            }
        }
    }
}
