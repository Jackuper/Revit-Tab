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

            // Get a 3D view type
            ViewFamilyType view3DType = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .FirstOrDefault(vft => vft.ViewFamily == ViewFamily.ThreeDimensional);

            if (view3DType == null)
            {
                TaskDialog.Show("Error", "No 3D ViewFamilyType found.");
                return Result.Failed;
            }

            // Get all levels
            IList<Level> levels = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .OrderBy(l => l.Elevation)
                .ToList();

            using (Transaction tx = new Transaction(doc, "Create 3D Views Per Level"))
            {
                tx.Start();

                foreach (Level level in levels)
                {
                    View3D view = View3D.CreateIsometric(doc, view3DType.Id);
                    if (view == null) continue;

                    view.Name = $"3D - {level.Name}";

                    // Apply section box
                    BoundingBoxXYZ sectionBox = new BoundingBoxXYZ();

                    double baseElevation = level.Elevation;
                    double height = 10; // feet

                    XYZ min = new XYZ(-100, -100, baseElevation);
                    XYZ max = new XYZ(100, 100, baseElevation + height);

                    sectionBox.Min = min;
                    sectionBox.Max = max;

                    view.SetSectionBox(sectionBox);
                    view.CropBoxActive = true;
                    view.CropBoxVisible = true;
                }

                tx.Commit();
            }

            TaskDialog.Show("Done", $"{levels.Count} 3D views created.");
            return Result.Succeeded;
        }
    }
}