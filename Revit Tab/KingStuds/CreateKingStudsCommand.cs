using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using Aspose.Cells.Charts;

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
                // Prompt user to pick a stud family type
                FamilySymbol studType = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilySymbol))
                    .Cast<FamilySymbol>()
                    .FirstOrDefault(f => f.Name.Contains("2x6") && f.Family.Name.Contains("Stud"));

                if (studType == null)
                {
                    TaskDialog.Show("King Studs", "No 'Stud' family found. Load a stud family before running this command.");
                    return Result.Cancelled;
                }

                // Activate the type if not already active
                if (!studType.IsActive)
                    studType.Activate();

                // Collect all doors and windows
                var openings = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_Doors)
                    .WhereElementIsNotElementType()
                    .ToList();

                openings.AddRange(new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_Windows)
                    .WhereElementIsNotElementType()
                    .ToList());

                using (Transaction t = new Transaction(doc, "Create King Studs"))
                {
                    t.Start();

                    foreach (var opening in openings)
                    {
                        FamilyInstance fi = opening as FamilyInstance;
                        if (fi == null) continue;

                        // Get host wall
                        Wall hostWall = fi.Host as Wall;
                        if (hostWall == null) continue;

                        LocationPoint locPt = fi.Location as LocationPoint;
                        if (locPt == null) continue;

                        // Compute placement
                        XYZ origin = locPt.Point;
                        XYZ wallDir = (hostWall.Orientation).Normalize();  // Wall normal
                        XYZ wallLineDir = (hostWall.Location as LocationCurve).Curve.GetEndPoint(1) -
                                          (hostWall.Location as LocationCurve).Curve.GetEndPoint(0);

                        wallLineDir = wallLineDir.Normalize();

                        double offset = UnitUtils.ConvertToInternalUnits(3.5, UnitTypeId.Inches);


                        // Left king stud
                        XYZ left = origin - wallLineDir * offset;
                        // Right king stud
                        XYZ right = origin + wallLineDir * offset;

                        // Place instances
                        doc.Create.NewFamilyInstance(left, studType, hostWall, StructuralType.NonStructural);
                        doc.Create.NewFamilyInstance(right, studType, hostWall, StructuralType.NonStructural);
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
    }
}

