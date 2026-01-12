using System;
using System.Linq;
using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Revit_Tab
{
    public class RevitApp : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
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

            // Button 2 - King Studs
            PushButtonData kingStudsBtnData = new PushButtonData(
                "CreateKingStuds",
                "Create\nKing Studs",
                assemblyPath,
                "Revit_Tab.CreateKingStudsCommand"
            );
            PushButton btnKingStuds = panel.AddItem(kingStudsBtnData) as PushButton;
            btnKingStuds.ToolTip = "Create King Studs";

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
    }
}
