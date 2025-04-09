using System;
using System.Linq;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.Attributes;
using System.Windows;

namespace Revit_Tab
{
    [Transaction(TransactionMode.Manual)]
    public class CreateSheetCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            
            SheetInputWindow inputWindow = new SheetInputWindow();
            bool? dialogResult = inputWindow.ShowDialog();

            
            if (dialogResult != true)
                return Result.Cancelled;

            // Retrieve user inputs
            string inputSheetNumber = inputWindow.SheetNumber;
            string sheetName = inputWindow.SheetName;
            int quantity = inputWindow.Quantity;

            int index = inputSheetNumber.Length - 1;
            while (index >= 0 && char.IsDigit(inputSheetNumber[index]))
                index--;

            if (index == inputSheetNumber.Length - 1)
            {
                message = "Sheet number must end with digits representing the numeric part (e.g., 'Z.001').";
                return Result.Failed;
            }

            string prefix = inputSheetNumber.Substring(0, index + 1);
            string numericPart = inputSheetNumber.Substring(index + 1);

            if (!int.TryParse(numericPart, out int startingNumber))
            {
                message = "Invalid numeric portion in sheet number.";
                return Result.Failed;
            }
            int numberLength = numericPart.Length; 

            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            using (Transaction trans = new Transaction(doc, "Create New Sheets"))
            {
                trans.Start();

                FilteredElementCollector collector = new FilteredElementCollector(doc)
                                                        .OfClass(typeof(FamilySymbol))
                                                        .OfCategory(BuiltInCategory.OST_TitleBlocks);
                FamilySymbol titleBlock = collector.Cast<FamilySymbol>().FirstOrDefault();
                if (titleBlock == null)
                {
                    message = "No title block found. Please load a title block into the project.";
                    return Result.Failed;
                }

                if (!titleBlock.IsActive)
                {
                    titleBlock.Activate();
                    doc.Regenerate();
                }

                // Loop through the quantity to create each sheet.
                for (int i = 0; i < quantity; i++)
                {
                    string currentSheetNumber = prefix + (startingNumber + i).ToString().PadLeft(numberLength, '0');

                    ViewSheet newSheet = ViewSheet.Create(doc, titleBlock.Id);
                    if (newSheet == null)
                    {
                        message = "Failed to create sheet.";
                        return Result.Failed;
                    }

                    Parameter sheetNumberParam = newSheet.get_Parameter(BuiltInParameter.SHEET_NUMBER);
                    Parameter sheetNameParam = newSheet.get_Parameter(BuiltInParameter.SHEET_NAME);
                    if (sheetNumberParam != null && sheetNameParam != null)
                    {
                        sheetNumberParam.Set(currentSheetNumber);
                        sheetNameParam.Set(sheetName);
                    }
                }

                trans.Commit();
            }

            return Result.Succeeded;
        }
    }
}
