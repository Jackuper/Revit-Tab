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
            Uri imageUri = new Uri(@"C:\Code\Revit-Tab\Revit Tab\Images\Create page.jpg", UriKind.Absolute);
            button.LargeImage = new BitmapImage(imageUri);

            //Button #2 Testing!
            PushButtonData FamilyLoader = new PushButtonData(
                "FamilyLoader",
                "Load Families",
                assemblyPath,
                "Revit_Tab.FamilyLoader"
            );
            PushButton button2 = panel.AddItem(FamilyLoader) as PushButton;
            button.ToolTip = "Click to load in all C&T families";
            // Button Image (Not working)
            Uri imageUri2 = new Uri(@"C:\Code\Revit-Tab\Revit Tab\Images\Create page.jpg", UriKind.Absolute);
            button.LargeImage = new BitmapImage(imageUri2);


            //Checking if it worked
            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}

