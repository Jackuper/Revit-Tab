using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Revit_Tab.Utility;

namespace Revit_Tab
{
    [Transaction(TransactionMode.Manual)]
    public class OpenLogViewerCommand : IExternalCommand
    {
        private const string COMMAND_NAME = "Log Viewer";

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                ErrorHandler.LogInfo(COMMAND_NAME, "Opening log viewer");

                // Create and show the log viewer window
                LogViewerWindow logViewer = new LogViewerWindow();
                logViewer.Show();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleError(COMMAND_NAME, ex);
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
