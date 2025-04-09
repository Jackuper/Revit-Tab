using System;
using System.IO;
using System.Windows.Forms;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.Attributes;

namespace Revit_Tab
{
    [Transaction(TransactionMode.Manual)]
    public class BulkLoadFamiliesCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Get the active document.
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            // Open a FolderBrowserDialog for the user to select a folder.
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Select the folder containing family files (.rfa)";
                folderDialog.RootFolder = Environment.SpecialFolder.MyComputer;

                if (folderDialog.ShowDialog() != DialogResult.OK)
                {
                    return Result.Cancelled;
                }

                string folderPath = folderDialog.SelectedPath;
                // Search for Revit family files in the selected folder.
                string[] familyFiles = Directory.GetFiles(folderPath, "*.rfa", SearchOption.TopDirectoryOnly);

                if (familyFiles.Length == 0)
                {
                    TaskDialog.Show("Bulk Load", "No family files (.rfa) found in the selected folder.");
                    return Result.Succeeded;
                }

                int countLoaded = 0;
                using (Transaction trans = new Transaction(doc, "Bulk Load Families"))
                {
                    trans.Start();
                    foreach (string familyFile in familyFiles)
                    {
                        Family family = null;
                        // Attempt to load the family.
                        if (doc.LoadFamily(familyFile, out family))
                        {
                            countLoaded++;
                        }
                        else
                        {
                            // If needed, you can log or show a warning for family files that failed to load.
                        }
                    }
                    trans.Commit();
                }

                TaskDialog.Show("Bulk Load Complete", $"{countLoaded} family file(s) loaded from:\n{folderPath}");
            }

            return Result.Succeeded;
        }
    }
}

