using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Revit_Tab
{
    [Transaction(TransactionMode.Manual)]
    public class Create3DViewsPerLevelCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            ViewFamilyType view3DType = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .FirstOrDefault(vft => vft.ViewFamily == ViewFamily.ThreeDimensional);

            if (view3DType == null)
            {
                TaskDialog.Show("Error", "No 3D ViewFamilyType found.");
                return Result.Failed;
            }

            var levels = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .OrderBy(l => l.Elevation)
                .ToList();

            var linkInstances = new FilteredElementCollector(doc)
                .OfClass(typeof(RevitLinkInstance))
                .Cast<RevitLinkInstance>()
                .ToList();

            // Compute overall XY bounding box of model + links
            BoundingBoxXYZ projectBounds = GetFullXYBounds(doc, linkInstances);
            if (projectBounds == null)
            {
                TaskDialog.Show("Error", "Could not determine project extents.");
                return Result.Failed;
            }

            using (Transaction tx = new Transaction(doc, "Create Level 3D Views"))
            {
                tx.Start();

                for (int i = 0; i < levels.Count; i++)
                {
                    var level = levels[i];
                    double baseZ = level.Elevation;
                    double topZ = (i < levels.Count - 1)
                        ? levels[i + 1].Elevation
                        : baseZ + 15; // default level height

                    var sectionBox = new BoundingBoxXYZ();
                    sectionBox.Min = new XYZ(projectBounds.Min.X, projectBounds.Min.Y, baseZ);
                    sectionBox.Max = new XYZ(projectBounds.Max.X, projectBounds.Max.Y, topZ);

                    var view = View3D.CreateIsometric(doc, view3DType.Id);
                    if (view == null) continue;

                    view.Name = $"3D - {level.Name}";
                    view.SetSectionBox(sectionBox);
                    view.CropBoxActive = true;
                    view.CropBoxVisible = true;
                }

                tx.Commit();
            }

            TaskDialog.Show("Done", $"Created {levels.Count} level-cut 3D views.");
            return Result.Succeeded;
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