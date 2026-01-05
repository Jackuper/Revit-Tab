using System;
using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;

namespace Revit_Tab
{
    public class RevitApp : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            string tabName = "Clancy Theys";
            try
            {
                application.CreateRibbonTab(tabName);
            }
            catch (Exception)
            {
            }
            // Ribbon
            RibbonPanel panel = application.CreateRibbonPanel(tabName, "Project Setup");

            string assemblyPath = Assembly.GetExecutingAssembly().Location;

            //Button #1 "Create Sheets"
            PushButtonData buttonData = new PushButtonData(
                "CreateSheetBtn",                 // Internal name.
                "Create Sheets",                  // Button label.
                assemblyPath,                     // Assembly path.
                "Revit_Tab.CreateSheetCommand"    // Command class (from CreateSheetCommand.cs).
            );
            PushButton button = panel.AddItem(buttonData) as PushButton;
            button.ToolTip = "Click to create sheets";

            // Button Image
            // Uri imageUri = new Uri(@"/Jcup;component/Images/Create page.jpg", UriKind.Relative);
            // button.LargeImage = new BitmapImage(imageUri);

            // Button for Family Catalog
            PushButtonData PushButtonData = new PushButtonData(
                "CreateKingStuds",
                "Create\nKing Studs",
                assemblyPath,
                "Revit_Tab.CreateKingStudsCommand"
            );
            PushButtonData btnKingStuds = PushButtonData;
            PushButton kingStudsButton = panel.AddItem(btnKingStuds) as PushButton;
            kingStudsButton.ToolTip = "Click to place King Studs around doors and windows";

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
