using System;
using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using System.IO;

namespace Revit_Tab
{
    public class RevitApp : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            // PROOF OF LIFE: Confirm new code is running
            TaskDialog.Show("Startup Check", "The updated add-in code is running! Checking images next...");

            string tabName = "Clancy Theys";
            try
            {
                application.CreateRibbonTab(tabName);
            }
            catch (Exception) { }

            RibbonPanel panel = application.CreateRibbonPanel(tabName, "Project Setup");

            // Uncomment to debug resource names if images are missing
            // DebugResources();

            string assemblyPath = Assembly.GetExecutingAssembly().Location;

            // Button 1 - Create Sheets
            PushButtonData createSheetBtnData = new PushButtonData(
                "CreateSheetBtn",
                "Create Sheets",
                assemblyPath,
                "Revit_Tab.CreateSheetCommand"
            );
            PushButton createSheetBtn = panel.AddItem(createSheetBtnData) as PushButton;
            createSheetBtn.ToolTip = "Click to create sheets";
            var sheetImage = GetImageSource("Revit_Tab.Images.CreatePage.png");
            createSheetBtn.LargeImage = sheetImage;
            createSheetBtn.Image = sheetImage;

            // Button 2 - King Studs
            PushButtonData kingStudsBtnData = new PushButtonData(
                "CreateKingStuds",
                "Create\nKing Studs",
                assemblyPath,
                "Revit_Tab.CreateKingStudsCommand"
            );
            PushButton btnKingStuds = panel.AddItem(kingStudsBtnData) as PushButton;
            btnKingStuds.ToolTip = "Create King Studs";
            var kingImage = GetImageSource("Revit_Tab.Images.KingStuds.png");
            btnKingStuds.LargeImage = kingImage;
            btnKingStuds.Image = kingImage;

            // Button 3 - 3D Views Per Level
            PushButtonData view3DButtonData = new PushButtonData(
                "Create3DViewsPerLevel",
                "3D Per Level",
                assemblyPath,
                "Revit_Tab.Create3DViewsPerLevelCommand"
            );
            PushButton view3DButton = panel.AddItem(view3DButtonData) as PushButton;
            view3DButton.ToolTip = "Automatically create 3D views for each level.";

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        private System.Windows.Media.ImageSource GetImageSource(string embeddedPath)
        {
            try
            {
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(embeddedPath))
                {
                    if (stream == null)
                    {
                        TaskDialog.Show("Error", $"Resource not found: {embeddedPath}");
                        return null;
                    }

                    // Copy to memory stream to ensure we have seekable access
                    var memoryStream = new MemoryStream();
                    stream.CopyTo(memoryStream);
                    memoryStream.Position = 0;

                    // Create BitmapImage with resizing
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.StreamSource = memoryStream;
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.DecodePixelWidth = 32; // Force resize to 32px width (standard icon size)
                    image.EndInit();
                    image.Freeze(); // Crucial for Revit

                    // TaskDialog.Show("Success", $"Loaded: {embeddedPath}");
                    return image;
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Exception", $"Failed to load {embeddedPath}: {ex.Message}");
                return null;
            }
        }

        // Debug method to inspect resource names if images fail
        private void DebugResources()
        {
            string[] resources = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            Autodesk.Revit.UI.TaskDialog.Show("Debug Resources", "Found resources:\n" + string.Join("\n", resources));
        }
    }
}
