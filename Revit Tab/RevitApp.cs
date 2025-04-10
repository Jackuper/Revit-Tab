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
            //(Storage) Uri imageUri = new Uri(@"C:\Code\Revit-Tab\Revit Tab\Images\Create page.jpg", UriKind.Absolute);
            //(Storage) button.LargeImage = new BitmapImage(imageUri);

            //Button #2 This is a good template youll have to change some names
            PushButtonData FamilyCatalogItem = new PushButtonData(
                "FamilyCatalogItem",
                "Load Families",
                assemblyPath,
                "Revit_Tab.FamilyLoader"
            );
            PushButton button2 = panel.AddItem(FamilyCatalogItem) as PushButton;
            button.ToolTip = "Click to load in all C&T families";


            //Button #3 This is a good template youll have to change some names
            //PushButtonData Command3 = new PushButtonData(
                //"Command3",                // Internal name.
                //"Load Families",           // Button label.
                //assemblyPath,             // Assembly path.
                //"Revit_Tab.Command3" // Command class.
            //);
            //PushButton Button3 = panel.AddItem(Command3) as PushButton;
            //button.ToolTip = "Click to load in all C&T families";

            //Button #4 This is a good template youll have to change some names
            //PushButtonData Command4 = new PushButtonData(
                //"Command4",                // Internal name.
                //"Load Families",           // Button label.
                //assemblyPath,             // Assembly path.
                //"Revit_Tab.Command4" // Command class.
            //);
            //PushButton Button4 = panel.AddItem(Command4) as PushButton;
            //button.ToolTip = "Click to load in all C&T families";





























            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}

