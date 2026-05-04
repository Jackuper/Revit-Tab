using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.Attributes;
using System.Windows;
using Revit_Tab.Utility;

namespace Revit_Tab
{
    [Transaction(TransactionMode.Manual)]
    public class CreateSheetCommand : IExternalCommand
    {
        private const string COMMAND_NAME = "Create Sheets";

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                ErrorHandler.LogInfo(COMMAND_NAME, "Command started");

                // Show input dialog
                SheetInputWindow inputWindow = new SheetInputWindow();
                bool? dialogResult = inputWindow.ShowDialog();

                if (dialogResult != true)
                {
                    ErrorHandler.LogInfo(COMMAND_NAME, "Command cancelled by user");
                    return Result.Cancelled;
                }

                // Retrieve user inputs
                string inputSheetNumber = inputWindow.SheetNumber;
                string sheetName = inputWindow.SheetName;
                int quantity = inputWindow.Quantity;

                ErrorHandler.LogInfo(COMMAND_NAME, $"Creating {quantity} sheets starting with {inputSheetNumber}");

                // Validate inputs
                if (!ValidateInputs(inputSheetNumber, sheetName, quantity, out string validationError))
                {
                    ErrorHandler.ShowValidationError(COMMAND_NAME, validationError);
                    message = validationError;
                    return Result.Failed;
                }

                // Parse sheet number format
                if (!ParseSheetNumber(inputSheetNumber, out string prefix, out int startingNumber, out int numberLength, out string parseError))
                {
                    ErrorHandler.ShowValidationError(COMMAND_NAME, parseError);
                    message = parseError;
                    return Result.Failed;
                }

                UIDocument uidoc = commandData.Application.ActiveUIDocument;
                Document doc = uidoc.Document;

                // Check for existing sheets
                var existingSheetNumbers = GetExistingSheetNumbers(doc);
                var sheetsToCreate = new List<string>();
                var duplicateSheets = new List<string>();

                for (int i = 0; i < quantity; i++)
                {
                    string sheetNumber = prefix + (startingNumber + i).ToString().PadLeft(numberLength, '0');
                    if (existingSheetNumbers.Contains(sheetNumber))
                    {
                        duplicateSheets.Add(sheetNumber);
                    }
                    else
                    {
                        sheetsToCreate.Add(sheetNumber);
                    }
                }

                // Warn about duplicates
                if (duplicateSheets.Count > 0)
                {
                    string warningMessage = $"The following sheet numbers already exist and will be skipped:\n\n{string.Join(", ", duplicateSheets.Take(10))}";
                    if (duplicateSheets.Count > 10)
                    {
                        warningMessage += $"\n...and {duplicateSheets.Count - 10} more";
                    }

                    if (sheetsToCreate.Count == 0)
                    {
                        ErrorHandler.ShowValidationError(COMMAND_NAME, "All requested sheet numbers already exist. No sheets will be created.");
                        return Result.Cancelled;
                    }

                    warningMessage += $"\n\nContinue with creating {sheetsToCreate.Count} sheet(s)?";

                    var result = ErrorHandler.ShowWarningDialog(COMMAND_NAME, warningMessage);
                    if (result != TaskDialogResult.Ok)
                    {
                        ErrorHandler.LogInfo(COMMAND_NAME, "User cancelled due to duplicate sheets");
                        return Result.Cancelled;
                    }
                }

                // Create sheets
                int createdCount = 0;
                StringBuilder errorLog = new StringBuilder();

                using (var guard = new TransactionGuard(doc, "Create New Sheets", COMMAND_NAME))
                {
                    // Find and activate title block
                    FamilySymbol titleBlock = GetTitleBlock(doc);
                    if (titleBlock == null)
                    {
                        string errorMsg = "No title block found. Please load a title block into the project before creating sheets.";
                        ErrorHandler.ShowValidationError(COMMAND_NAME, errorMsg);
                        message = errorMsg;
                        return Result.Failed;
                    }

                    if (!titleBlock.IsActive)
                    {
                        titleBlock.Activate();
                        doc.Regenerate();
                    }

                    // Create each sheet
                    foreach (string sheetNumber in sheetsToCreate)
                    {
                        try
                        {
                            ViewSheet newSheet = ViewSheet.Create(doc, titleBlock.Id);
                            if (newSheet == null)
                            {
                                errorLog.AppendLine($"Failed to create sheet {sheetNumber}");
                                continue;
                            }

                            Parameter sheetNumberParam = newSheet.get_Parameter(BuiltInParameter.SHEET_NUMBER);
                            Parameter sheetNameParam = newSheet.get_Parameter(BuiltInParameter.SHEET_NAME);

                            if (sheetNumberParam != null && sheetNameParam != null)
                            {
                                sheetNumberParam.Set(sheetNumber);
                                sheetNameParam.Set(sheetName);
                                createdCount++;
                            }
                            else
                            {
                                errorLog.AppendLine($"Could not set parameters for sheet {sheetNumber}");
                            }
                        }
                        catch (Exception ex)
                        {
                            errorLog.AppendLine($"{sheetNumber}: {ex.Message}");
                            ErrorHandler.LogError($"Error creating sheet {sheetNumber}: {ex.Message}");
                        }
                    }

                    guard.Commit();
                }

                // Show results
                string resultMessage = $"Successfully created {createdCount} sheet(s)";
                if (duplicateSheets.Count > 0)
                {
                    resultMessage += $"\nSkipped {duplicateSheets.Count} duplicate(s)";
                }

                if (errorLog.Length > 0)
                {
                    resultMessage += $"\n\nErrors:\n{errorLog}";
                    TaskDialog resultDialog = new TaskDialog(COMMAND_NAME)
                    {
                        MainIcon = TaskDialogIcon.TaskDialogIconWarning,
                        MainInstruction = "Sheets Created with Warnings",
                        MainContent = resultMessage,
                        CommonButtons = TaskDialogCommonButtons.Close
                    };
                    resultDialog.Show();
                }
                else
                {
                    TaskDialog.Show(COMMAND_NAME, resultMessage);
                }

                ErrorHandler.LogInfo(COMMAND_NAME, $"Command completed: {createdCount} sheets created");
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleError(COMMAND_NAME, ex);
                message = ex.Message;
                return Result.Failed;
            }
        }

        private bool ValidateInputs(string sheetNumber, string sheetName, int quantity, out string error)
        {
            if (string.IsNullOrWhiteSpace(sheetNumber))
            {
                error = "Sheet number cannot be empty.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(sheetName))
            {
                error = "Sheet name cannot be empty.";
                return false;
            }

            if (quantity <= 0)
            {
                error = "Quantity must be greater than zero.";
                return false;
            }

            if (quantity > 1000)
            {
                error = "Quantity cannot exceed 1000 sheets. Please create sheets in smaller batches.";
                return false;
            }

            error = null;
            return true;
        }

        private bool ParseSheetNumber(string inputSheetNumber, out string prefix, out int startingNumber, out int numberLength, out string error)
        {
            prefix = null;
            startingNumber = 0;
            numberLength = 0;
            error = null;

            // Find where digits start from the end
            int index = inputSheetNumber.Length - 1;
            while (index >= 0 && char.IsDigit(inputSheetNumber[index]))
                index--;

            if (index == inputSheetNumber.Length - 1)
            {
                error = "Sheet number must end with digits (e.g., 'A101', 'S-001', 'Z.100').\n\nThe numeric portion will be automatically incremented.";
                return false;
            }

            prefix = inputSheetNumber.Substring(0, index + 1);
            string numericPart = inputSheetNumber.Substring(index + 1);

            if (!int.TryParse(numericPart, out startingNumber))
            {
                error = "Invalid numeric portion in sheet number.";
                return false;
            }

            numberLength = numericPart.Length;
            return true;
        }

        private HashSet<string> GetExistingSheetNumbers(Document doc)
        {
            var existingNumbers = new HashSet<string>();

            try
            {
                var sheets = new FilteredElementCollector(doc)
                    .OfClass(typeof(ViewSheet))
                    .Cast<ViewSheet>()
                    .Where(s => !s.IsTemplate);

                foreach (var sheet in sheets)
                {
                    string sheetNumber = sheet.SheetNumber;
                    if (!string.IsNullOrEmpty(sheetNumber))
                    {
                        existingNumbers.Add(sheetNumber);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError($"Error collecting existing sheets: {ex.Message}");
            }

            return existingNumbers;
        }

        private FamilySymbol GetTitleBlock(Document doc)
        {
            try
            {
                return new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilySymbol))
                    .OfCategory(BuiltInCategory.OST_TitleBlocks)
                    .Cast<FamilySymbol>()
                    .FirstOrDefault();
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError($"Error finding title block: {ex.Message}");
                return null;
            }
        }
    }
}
