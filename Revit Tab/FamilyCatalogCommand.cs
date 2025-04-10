using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.Attributes;
using System.Windows.Media.Imaging;

namespace Revit_Tab
{
    [Transaction(TransactionMode.Manual)]
    public class FamilyCatalogCommand : IExternalCommand
    {
        // You might store your families in a known folder.
        private const string FamiliesFolder = @"C:\MyFamilyLibrary\";

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Get the active document.
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            // Get list of family files from the folder.
            string[] files = Directory.GetFiles(FamiliesFolder, "*.rfa", SearchOption.TopDirectoryOnly);
            if (files == null || files.Length == 0)
            {
                TaskDialog.Show("Family Catalog", "No family files found in the catalog folder.");
                return Result.Succeeded;
            }

            // Build a collection of FamilyCatalogItem items.
            var catalogItems = new ObservableCollection<FamilyCatalogItem>(
                files.Select(f => new FamilyCatalogItem
                {
                    FilePath = f,
                    FamilyName = Path.GetFileNameWithoutExtension(f),
                    Description = "Description for " + Path.GetFileNameWithoutExtension(f),  // Optionally customize
                    // You can set a default thumbnail here. For example, if you have a default image.
                    Thumbnail = new BitmapImage(new Uri(@"C:\Path\To\DefaultThumbnail.png", UriKind.Absolute)),
                    IsSelected = false
                })
            );

            // Show the WPF catalog window.
            FamilyCatalogWindow catalogWindow = new FamilyCatalogWindow(catalogItems);
            bool? dialogResult = catalogWindow.ShowDialog();
            if (dialogResult != true)
                return Result.Cancelled;

            // Filter to get only the selected items.
            var selectedItems = catalogItems.Where(item => item.IsSelected).ToList();
            if (selectedItems.Count == 0)
            {
                TaskDialog.Show("Family Catalog", "No families were selected.");
                return Result.Succeeded;
            }

            // Load each selected family.
            int loadedCount = 0;
            using (Transaction trans = new Transaction(doc, "Import Selected Families"))
            {
                trans.Start();
                foreach (var item in selectedItems)
                {
                    Family family;
                    if (doc.LoadFamily(item.FilePath, out family))
                    {
                        loadedCount++;
                    }
                    else
                    {
                        // Optionally log which families failed.
                    }
                }
                trans.Commit();
            }

            TaskDialog.Show("Family Catalog", $"{loadedCount} families imported successfully.");
            return Result.Succeeded;
        }
    }
}
