using System;
using System.Linq;
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
            // Show version check to confirm new code is loaded
            string assemblyLocation = Assembly.GetExecutingAssembly().Location;
            string version = System.Diagnostics.FileVersionInfo.GetVersionInfo(assemblyLocation).FileVersion ?? "Unknown";
            string buildTime = System.IO.File.GetLastWriteTime(assemblyLocation).ToString("yyyy-MM-dd HH:mm:ss");

            TaskDialog.Show("Add-in Loading",
                $"Assembly: {assemblyLocation}\nBuild Time: {buildTime}\nThis is the FIXED version with panel checking.");

            string tabName = "Clancy Theys";
            string panelName = "Project Setup";

            try
            {
                application.CreateRibbonTab(tabName);
            }
            catch (Exception) { }

            // Check if panel already exists
            RibbonPanel panel = application.GetRibbonPanels(tabName)
                .FirstOrDefault(p => p.Name == panelName);

            // Create panel only if it doesn't exist
            if (panel == null)
            {
                panel = application.CreateRibbonPanel(tabName, panelName);
            }
            else
            {
                // Panel already exists, check if it already has buttons
                if (panel.GetItems().Count > 0)
                {
                    // Buttons already added, no need to add them again
                    return Result.Succeeded;
                }
            }

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

            // Add image for 3D Views button (you'll need to add a 3DViews.png image to the Images folder)
            var view3DImage = GetImageSource("Revit_Tab.Images.3DViews.png");
            if (view3DImage != null)
            {
                view3DButton.LargeImage = view3DImage;
                view3DButton.Image = view3DImage;
            }

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
