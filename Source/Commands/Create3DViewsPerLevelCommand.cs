using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Revit_Tab.Utility;

namespace Revit_Tab
{
    [Transaction(TransactionMode.Manual)]
    public class Create3DViewsPerLevelCommand : IExternalCommand
    {
        private const string COMMAND_NAME = "3D Per Level";

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

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

                // Find 3D view family type
                ViewFamilyType view3DType = new FilteredElementCollector(doc)
                    .OfClass(typeof(ViewFamilyType))
                    .Cast<ViewFamilyType>()
                    .FirstOrDefault(vft => vft.ViewFamily == ViewFamily.ThreeDimensional);

                if (view3DType == null)
                {
                    string errorMsg = "No 3D View Family Type found in the project.\n\nThis is required to create 3D views. Please ensure your project template includes 3D view types.";
                    ErrorHandler.ShowValidationError(COMMAND_NAME, errorMsg);
                    message = "No 3D ViewFamilyType found";
                    return Result.Failed;
                }

                // Collect levels
                var levels = new FilteredElementCollector(doc)
                    .OfClass(typeof(Level))
                    .Cast<Level>()
                    .OrderBy(l => l.Elevation)
                    .ToList();

                if (levels.Count == 0)
                {
                    string errorMsg = "No levels found in the project.\n\nPlease create levels before running this command.";
                    ErrorHandler.ShowValidationError(COMMAND_NAME, errorMsg);
                    message = "No levels found";
                    return Result.Failed;
                }

                ErrorHandler.LogInfo(COMMAND_NAME, $"Found {levels.Count} level(s) in project");

                // Collect linked instances
                var linkInstances = new FilteredElementCollector(doc)
                    .OfClass(typeof(RevitLinkInstance))
                    .Cast<RevitLinkInstance>()
                    .ToList();

                if (linkInstances.Count > 0)
                {
                    ErrorHandler.LogInfo(COMMAND_NAME, $"Found {linkInstances.Count} linked model(s)");
                }

                // Compute overall XY bounding box of model + links
                BoundingBoxXYZ projectBounds = GetFullXYBounds(doc, linkInstances);
                if (projectBounds == null)
                {
                    string errorMsg = "Could not determine project extents.\n\nPlease ensure your project contains model elements with valid geometry.";
                    ErrorHandler.ShowValidationError(COMMAND_NAME, errorMsg);
                    message = "Could not determine project extents";
                    return Result.Failed;
                }

                // Check for existing views with same naming convention
                var existingViewNames = GetExistingViewNames(doc);
                var conflictingViews = new List<string>();

                foreach (var level in levels)
                {
                    string viewName = $"Z-{level.Name}";
                    if (existingViewNames.Contains(viewName))
                    {
                        conflictingViews.Add(viewName);
                    }
                }

                if (conflictingViews.Count > 0)
                {
                    string warningMsg = $"The following 3D view names already exist and will be skipped:\n\n{string.Join("\n", conflictingViews.Take(10))}";
                    if (conflictingViews.Count > 10)
                    {
                        warningMsg += $"\n...and {conflictingViews.Count - 10} more";
                    }

                    warningMsg += $"\n\nContinue with creating {levels.Count - conflictingViews.Count} view(s)?";

                    var result = ErrorHandler.ShowWarningDialog(COMMAND_NAME, warningMsg);
                    if (result != TaskDialogResult.Ok)
                    {
                        ErrorHandler.LogInfo(COMMAND_NAME, "User cancelled due to existing views");
                        return Result.Cancelled;
                    }
                }

                // Create views
                int createdCount = 0;
                int skippedCount = 0;
                StringBuilder errorLog = new StringBuilder();
                StringBuilder skippedLog = new StringBuilder();

                using (var guard = new TransactionGuard(doc, "Create Level 3D Views", COMMAND_NAME))
                {
                    for (int i = 0; i < levels.Count; i++)
                    {
                        var level = levels[i];
                        string viewName = $"Z-{level.Name}";

                        try
                        {
                            // Skip if view name already exists
                            if (existingViewNames.Contains(viewName))
                            {
                                skippedLog.AppendLine($"• {viewName} (already exists)");
                                skippedCount++;
                                continue;
                            }

                            double baseZ = level.Elevation;
                            double topZ = (i < levels.Count - 1)
                                ? levels[i + 1].Elevation
                                : baseZ + UnitUtils.ConvertToInternalUnits(15, UnitTypeId.Feet);

                            var sectionBox = new BoundingBoxXYZ();
                            sectionBox.Min = new XYZ(projectBounds.Min.X, projectBounds.Min.Y, baseZ);
                            sectionBox.Max = new XYZ(projectBounds.Max.X, projectBounds.Max.Y, topZ);

                            var view = View3D.CreateIsometric(doc, view3DType.Id);
                            if (view == null)
                            {
                                errorLog.AppendLine($"• {viewName}: Failed to create view");
                                skippedCount++;
                                continue;
                            }

                            view.Name = viewName;
                            view.SetSectionBox(sectionBox);
                            view.IsSectionBoxActive = true;
                            view.DetailLevel = ViewDetailLevel.Fine;

                            createdCount++;
                            ErrorHandler.LogInfo(COMMAND_NAME, $"Created view: {viewName}");
                        }
                        catch (Exception ex)
                        {
                            errorLog.AppendLine($"• {viewName}: {ex.Message}");
                            ErrorHandler.LogError($"Error creating view {viewName}: {ex.Message}");
                            skippedCount++;
                        }
                    }

                    guard.Commit();
                }

                // Show results
                ShowResults(createdCount, skippedCount, levels.Count, errorLog, skippedLog);

                ErrorHandler.LogInfo(COMMAND_NAME, $"Command completed: {createdCount} views created, {skippedCount} skipped");
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleError(COMMAND_NAME, ex);
                message = ex.Message;
                return Result.Failed;
            }
        }

        private void ShowResults(int createdCount, int skippedCount, int totalLevels, StringBuilder errorLog, StringBuilder skippedLog)
        {
            StringBuilder resultMessage = new StringBuilder();
            resultMessage.AppendLine($"Processed {totalLevels} level(s):");
            resultMessage.AppendLine($"✓ Created {createdCount} 3D view(s)");

            if (skippedCount > 0)
            {
                resultMessage.AppendLine($"⚠ Skipped {skippedCount} view(s)");
            }

            StringBuilder expandedContent = new StringBuilder();

            if (skippedLog.Length > 0)
            {
                expandedContent.AppendLine("Skipped Views:");
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
                MainInstruction = createdCount > 0 ? "3D Views Created Successfully" : "No 3D Views Created",
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

        private HashSet<string> GetExistingViewNames(Document doc)
        {
            var existingNames = new HashSet<string>();

            try
            {
                var views = new FilteredElementCollector(doc)
                    .OfClass(typeof(View3D))
                    .Cast<View3D>()
                    .Where(v => !v.IsTemplate);

                foreach (var view in views)
                {
                    if (!string.IsNullOrEmpty(view.Name))
                    {
                        existingNames.Add(view.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError($"Error collecting existing views: {ex.Message}");
            }

            return existingNames;
        }

        private BoundingBoxXYZ GetFullXYBounds(Document doc, List<RevitLinkInstance> linkInstances)
        {
            List<XYZ> corners = new List<XYZ>();

            // Host model
            var elements = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .Where(e => e.get_BoundingBox(null) != null)
                .ToList();

            foreach (var e in elements)
            {
                var bb = e.get_BoundingBox(null);
                corners.Add(bb.Min);
                corners.Add(bb.Max);
            }

            // Linked models
            foreach (var link in linkInstances)
            {
                Document linkDoc = link.GetLinkDocument();
                if (linkDoc == null) continue;

                var linkElements = new FilteredElementCollector(linkDoc)
                    .WhereElementIsNotElementType()
                    .Where(e => e.get_BoundingBox(null) != null)
                    .ToList();

                foreach (var e in linkElements)
                {
                    var bb = e.get_BoundingBox(null);
                    if (bb == null) continue;

                    corners.Add(link.GetTransform().OfPoint(bb.Min));
                    corners.Add(link.GetTransform().OfPoint(bb.Max));
                }
            }

            if (corners.Count == 0) return null;

            double minX = corners.Min(p => p.X);
            double minY = corners.Min(p => p.Y);
            double maxX = corners.Max(p => p.X);
            double maxY = corners.Max(p => p.Y);

            return new BoundingBoxXYZ
            {
                Min = new XYZ(minX, minY, 0),
                Max = new XYZ(maxX, maxY, 0) // Z will be set per-level
            };
        }
    }
}