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

            RibbonPanel panel = application.CreateRibbonPanel(tabName, "Project Setup");

            string assemblyPath = Assembly.GetExecutingAssembly().Location;

            PushButtonData buttonData = new PushButtonData(
                "CreateSheetBtn",                 // Internal name.
                "Create Sheets",                  // Button label.
                assemblyPath,                     // Assembly path.
                "Revit_Tab.CreateSheetCommand"    // Command class (from CreateSheetCommand.cs).
            );
            PushButton button = panel.AddItem(buttonData) as PushButton;
            button.ToolTip = "Click to create sheets";

            Uri imageUri = new Uri(@"C:\Code\Revit Tab\Revit Tab\Images\Create page.jpg", UriKind.Absolute);
            button.LargeImage = new BitmapImage(imageUri);



            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}

