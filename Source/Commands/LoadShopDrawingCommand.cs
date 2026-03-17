using System;
using System.Collections.Generic;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.Win32;

namespace Revit_Tab
{
    [Transaction(TransactionMode.ReadOnly)]
    [Regeneration(RegenerationOption.Manual)]
    public class LoadShopDrawingCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Browse for PDF
                var dlg = new OpenFileDialog
                {
                    Title  = "Select MiTek Shop Drawing PDF",
                    Filter = "PDF Files (*.pdf)|*.pdf|All Files (*.*)|*.*"
                };

                if (dlg.ShowDialog() != true)
                    return Result.Cancelled;

                string pdfPath = dlg.FileName;

                // Parse all pages
                List<ExtractedTrussType> extracted;
                try
                {
                    extracted = MiTekPdfParser.ParsePdf(pdfPath);
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("PDF Parse Error",
                        $"Could not read the PDF:\n{ex.Message}");
                    return Result.Failed;
                }

                if (extracted.Count == 0)
                {
                    TaskDialog.Show("No Data Found",
                        "No truss types could be extracted from this PDF.\n\n" +
                        "Make sure it is a MiTek-generated shop drawing.");
                    return Result.Failed;
                }

                // Show review dialog
                var editor = new TrussConfigEditorDialog(extracted);
                editor.ShowDialog();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
