using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Revit_Tab.Utility;

namespace Revit_Tab
{
    [Transaction(TransactionMode.Manual)]
    public class CreateKingStudsCommand : IExternalCommand
    {
        private const string COMMAND_NAME = "Create King Studs";

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;

            try
            {
                ErrorHandler.LogInfo(COMMAND_NAME, "Command started");

                // Validate document is editable
                if (doc.IsReadOnly)
                {
                    string errorMsg = "The document is read-only. Please ensure the document is editable before running this command.";
                    ErrorHandler.ShowValidationError(COMMAND_NAME, errorMsg);
                    message = errorMsg;
                    return Result.Failed;
                }

                // Find or load the stud family
                FamilySymbol studType = FindStudSymbol(doc);
                if (studType == null)
                {
                    ErrorHandler.LogInfo(COMMAND_NAME, "Stud family not found in project, attempting to load");
                    studType = TryLoadStudFamilyAndFindSymbol(doc);
                }

                if (studType == null)
                {
                    string assemblyPath = Assembly.GetExecutingAssembly().Location;
                    string assemblyDir = Path.GetDirectoryName(assemblyPath);
                    string familyPath = Path.Combine(assemblyDir, "Families", "Stud.rfa");

                    string errorMsg = "Could not find or load a stud family.\n\n" +
                                     "The command requires a family named 'Stud' or containing 'Stud' in its name.\n\n" +
                                     $"Expected location:\n{familyPath}\n\n" +
                                     "Please ensure the Stud.rfa family is available.";

                    ErrorHandler.ShowValidationError(COMMAND_NAME, errorMsg);
                    ErrorHandler.LogError($"Stud family not found at: {familyPath}");
                    message = "Stud family not found";
                    return Result.Cancelled;
                }

                ErrorHandler.LogInfo(COMMAND_NAME, $"Using stud family: {studType.Family.Name} - {studType.Name}");

                // Collect all openings from host and linked models
                var openingSources = CollectOpeningsFromHostAndLinks(doc);

                if (openingSources.Count == 0)
                {
                    string infoMsg = "No doors or windows found in the project or linked models.\n\n" +
                                    "Please ensure your project contains doors or windows where king studs should be placed.";
                    TaskDialog.Show(COMMAND_NAME, infoMsg);
                    ErrorHandler.LogInfo(COMMAND_NAME, "No openings found");
                    return Result.Succeeded;
                }

                ErrorHandler.LogInfo(COMMAND_NAME, $"Found {openingSources.Count} opening(s) to process");

                // Process openings and create studs
                int successCount = 0;
                int skippedCount = 0;
                StringBuilder errorLog = new StringBuilder();
                StringBuilder skippedLog = new StringBuilder();

                using (var guard = new TransactionGuard(doc, "Create King Studs", COMMAND_NAME))
                {
                    // Activate the type if not already active
                    if (!studType.IsActive)
                    {
                        studType.Activate();
                        ErrorHandler.LogInfo(COMMAND_NAME, "Activated stud family type");
                    }

                    foreach (var source in openingSources)
                    {
                        try
                        {
                            if (!ProcessOpening(doc, source, studType, out string skipReason))
                            {
                                skippedCount++;
                                if (!string.IsNullOrEmpty(skipReason))
                                {
                                    skippedLog.AppendLine($"• {skipReason}");
                                }
                                continue;
                            }

                            successCount += 2; // Two studs per opening
                        }
                        catch (Exception ex)
                        {
                            string openingInfo = GetOpeningInfo(source.Opening);
                            errorLog.AppendLine($"• {openingInfo}: {ex.Message}");
                            ErrorHandler.LogError($"Error processing {openingInfo}: {ex.Message}");
                            skippedCount++;
                        }
                    }

                    guard.Commit();
                }

                // Show results
                ShowResults(successCount, skippedCount, openingSources.Count, errorLog, skippedLog);

                ErrorHandler.LogInfo(COMMAND_NAME, $"Command completed: {successCount} studs created, {skippedCount} openings skipped");
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleError(COMMAND_NAME, ex);
                message = ex.Message;
                return Result.Failed;
            }
        }

        private bool ProcessOpening(Document doc, OpeningSource source, FamilySymbol studType, out string skipReason)
        {
            skipReason = null;

            FamilyInstance fi = source.Opening;
            if (fi == null)
            {
                skipReason = "Invalid opening instance";
                return false;
            }

            LocationPoint locPt = fi.Location as LocationPoint;
            if (locPt == null)
            {
                skipReason = $"{GetOpeningInfo(fi)}: No location point";
                return false;
            }

            // Transform location to host coordinates
            XYZ originHost = source.Transform.OfPoint(locPt.Point);

            // Get the host wall
            Wall sourceHostWall = fi.Host as Wall;
            if (sourceHostWall == null)
            {
                skipReason = $"{GetOpeningInfo(fi)}: Not hosted on a wall";
                return false;
            }

            LocationCurve wallLocCurve = sourceHostWall.Location as LocationCurve;
            if (wallLocCurve?.Curve == null)
            {
                skipReason = $"{GetOpeningInfo(fi)}: Wall has no location curve";
                return false;
            }

            // Calculate stud positions
            XYZ p0Host = source.Transform.OfPoint(wallLocCurve.Curve.GetEndPoint(0));
            XYZ p1Host = source.Transform.OfPoint(wallLocCurve.Curve.GetEndPoint(1));
            XYZ wallLineDirHost = (p1Host - p0Host).Normalize();

            double offset = UnitUtils.ConvertToInternalUnits(3.5, UnitTypeId.Inches);

            XYZ leftHost = originHost - wallLineDirHost * offset;
            XYZ rightHost = originHost + wallLineDirHost * offset;

            // Find appropriate level
            Level hostLevel = GetBestHostLevel(doc, source.LevelElevation);
            if (hostLevel == null)
            {
                skipReason = $"{GetOpeningInfo(fi)}: No matching level found";
                return false;
            }

            // Place the studs
            doc.Create.NewFamilyInstance(leftHost, studType, hostLevel, StructuralType.NonStructural);
            doc.Create.NewFamilyInstance(rightHost, studType, hostLevel, StructuralType.NonStructural);

            return true;
        }

        private void ShowResults(int successCount, int skippedCount, int totalOpenings, StringBuilder errorLog, StringBuilder skippedLog)
        {
            StringBuilder resultMessage = new StringBuilder();
            resultMessage.AppendLine($"Processed {totalOpenings} opening(s):");
            resultMessage.AppendLine($"✓ Created {successCount} king stud(s)");

            if (skippedCount > 0)
            {
                resultMessage.AppendLine($"⚠ Skipped {skippedCount} opening(s)");
            }

            StringBuilder expandedContent = new StringBuilder();

            if (skippedLog.Length > 0)
            {
                expandedContent.AppendLine("Skipped Openings:");
                expandedContent.AppendLine(skippedLog.ToString());
            }

            if (errorLog.Length > 0)
            {
                if (expandedContent.Length > 0)
                    expandedContent.AppendLine();
                expandedContent.AppendLine("Errors:");
                expandedContent.AppendLine(errorLog.ToString());
            }

            TaskDialog resultDialog = new TaskDialog(COMMAND_NAME)
            {
                MainIcon = errorLog.Length > 0 ? TaskDialogIcon.TaskDialogIconWarning : TaskDialogIcon.TaskDialogIconInformation,
                MainInstruction = successCount > 0 ? "King Studs Created Successfully" : "No King Studs Created",
                MainContent = resultMessage.ToString(),
                CommonButtons = TaskDialogCommonButtons.Close,
                DefaultButton = TaskDialogResult.Close
            };

            if (expandedContent.Length > 0)
            {
                resultDialog.ExpandedContent = expandedContent.ToString();
            }

            if (errorLog.Length > 0 || skippedLog.Length > 0)
            {
                resultDialog.FooterText = "Logs saved to: " + ErrorHandler.GetLogDirectory();
            }

            resultDialog.Show();
        }

        private string GetOpeningInfo(FamilyInstance opening)
        {
            if (opening == null)
                return "Unknown opening";

            try
            {
                string category = opening.Category?.Name ?? "Unknown";
                string family = opening.Symbol?.Family?.Name ?? "Unknown";
                string id = opening.Id.ToString();
                return $"{category} ({family}) [ID: {id}]";
            }
            catch
            {
                return $"Opening [ID: {opening.Id}]";
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
