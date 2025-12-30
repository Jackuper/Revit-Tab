using System;
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
            try
            {
                application.CreateRibbonTab(tabName);
            }
            catch (Exception) { }

            RibbonPanel panel = application.CreateRibbonPanel(tabName, "Project Setup");

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
            // Optional image: 
            // createSheetBtn.LargeImage = new BitmapImage(new Uri(@"C:\Path\To\Your\Image.png", UriKind.Absolute));

            // Button 2 - Family Catalog
            PushButtonData catalogBtnData = new PushButtonData(
                "FamilyCatalogBtn",
                "Family Catalog",
                assemblyPath,
                "Revit_Tab.FamilyCatalogCommand"
            );
            PushButton catalogBtn = panel.AddItem(catalogBtnData) as PushButton;
            catalogBtn.ToolTip = "Click to load in all C&T families";
            


            // Button 3 - 3D View creation
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
