using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;

namespace Revit_Tab.Utility
{
    /// <summary>
    /// Generates detailed diagnostic reports for troubleshooting
    /// </summary>
    public class DiagnosticReport
    {
        private readonly Application _application;
        private readonly Document _document;
        private readonly string _commandName;
        private readonly Exception _exception;
        private readonly string _screenshotPath;

        public DiagnosticReport(Application application, Document document, string commandName, Exception exception = null, string screenshotPath = null)
        {
            _application = application;
            _document = document;
            _commandName = commandName;
            _exception = exception;
            _screenshotPath = screenshotPath;
        }

        /// <summary>
        /// Generates and saves a comprehensive diagnostic report
        /// </summary>
        /// <returns>Path to the saved report</returns>
        public string Generate()
        {
            try
            {
                StringBuilder report = new StringBuilder();

                report.AppendLine("====================================================");
                report.AppendLine("REVIT TAB DIAGNOSTIC REPORT");
                report.AppendLine("====================================================");
                report.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                report.AppendLine($"Command: {_commandName}");
                report.AppendLine();

                // System Information
                AppendSystemInfo(report);

                // Revit Information
                AppendRevitInfo(report);

                // Add-in Information
                AppendAddinInfo(report);

                // Document Information
                if (_document != null)
                {
                    AppendDocumentInfo(report);
                }

                // Exception Information
                if (_exception != null)
                {
                    AppendExceptionInfo(report);
                }

                // Screenshot Information
                if (!string.IsNullOrEmpty(_screenshotPath))
                {
                    AppendScreenshotInfo(report);
                }

                // Save report
                string reportPath = SaveReport(report.ToString());
                return reportPath;
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError($"Failed to generate diagnostic report: {ex.Message}");
                return null;
            }
        }

        private void AppendSystemInfo(StringBuilder report)
        {
            report.AppendLine("SYSTEM INFORMATION");
            report.AppendLine("--------------------------------------------------");
            report.AppendLine($"OS: {Environment.OSVersion}");
            report.AppendLine($"64-bit OS: {Environment.Is64BitOperatingSystem}");
            report.AppendLine($"64-bit Process: {Environment.Is64BitProcess}");
            report.AppendLine($"Machine Name: {Environment.MachineName}");
            report.AppendLine($"User: {Environment.UserName}");
            report.AppendLine($"CLR Version: {Environment.Version}");
            report.AppendLine($"Processors: {Environment.ProcessorCount}");
            report.AppendLine();
        }

        private void AppendRevitInfo(StringBuilder report)
        {
            report.AppendLine("REVIT INFORMATION");
            report.AppendLine("--------------------------------------------------");

            try
            {
                if (_application != null)
                {
                    report.AppendLine($"Revit Version: {_application.VersionName}");
                    report.AppendLine($"Revit Build: {_application.VersionBuild}");
                    report.AppendLine($"Language: {_application.Language}");
                    report.AppendLine($"Username: {_application.Username}");
                }
            }
            catch (Exception ex)
            {
                report.AppendLine($"Error retrieving Revit info: {ex.Message}");
            }

            report.AppendLine();
        }

        private void AppendAddinInfo(StringBuilder report)
        {
            report.AppendLine("ADD-IN INFORMATION");
            report.AppendLine("--------------------------------------------------");

            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                report.AppendLine($"Assembly: {assembly.GetName().Name}");
                report.AppendLine($"Version: {assembly.GetName().Version}");
                report.AppendLine($"Location: {assembly.Location}");

                var fileInfo = new FileInfo(assembly.Location);
                report.AppendLine($"Build Date: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
                report.AppendLine($"File Size: {fileInfo.Length / 1024} KB");
            }
            catch (Exception ex)
            {
                report.AppendLine($"Error retrieving add-in info: {ex.Message}");
            }

            report.AppendLine();
        }

        private void AppendDocumentInfo(StringBuilder report)
        {
            report.AppendLine("DOCUMENT INFORMATION");
            report.AppendLine("--------------------------------------------------");

            try
            {
                report.AppendLine($"Title: {_document.Title}");
                report.AppendLine($"Path: {_document.PathName ?? "(not saved)"}");
                report.AppendLine($"Is Workshared: {_document.IsWorkshared}");
                report.AppendLine($"Is Family: {_document.IsFamilyDocument}");
                report.AppendLine($"Is Modified: {_document.IsModified}");
                report.AppendLine($"Is Read-Only: {_document.IsReadOnly}");

                // Project info
                var projectInfo = _document.ProjectInformation;
                if (projectInfo != null)
                {
                    report.AppendLine($"Project Name: {projectInfo.Name}");
                    report.AppendLine($"Project Number: {projectInfo.Number}");
                }

                // Element counts
                report.AppendLine();
                report.AppendLine("Element Counts:");
                AppendElementCount(report, _document, typeof(ViewSheet), "Sheets");
                AppendElementCount(report, _document, typeof(Level), "Levels");
                AppendElementCount(report, _document, typeof(View3D), "3D Views");
                AppendElementCount(report, _document, typeof(Wall), "Walls");
                AppendElementCount(report, _document, typeof(FamilyInstance), "Family Instances");

                // Linked models
                var links = new FilteredElementCollector(_document)
                    .OfClass(typeof(RevitLinkInstance))
                    .Cast<RevitLinkInstance>()
                    .ToList();

                report.AppendLine($"Linked Models: {links.Count}");
                foreach (var link in links)
                {
                    report.AppendLine($"  - {link.Name}");
                }
            }
            catch (Exception ex)
            {
                report.AppendLine($"Error retrieving document info: {ex.Message}");
            }

            report.AppendLine();
        }

        private void AppendElementCount(StringBuilder report, Document doc, Type elementType, string label)
        {
            try
            {
                int count = new FilteredElementCollector(doc)
                    .OfClass(elementType)
                    .GetElementCount();
                report.AppendLine($"  {label}: {count}");
            }
            catch
            {
                report.AppendLine($"  {label}: (error counting)");
            }
        }

        private void AppendExceptionInfo(StringBuilder report)
        {
            report.AppendLine("EXCEPTION INFORMATION");
            report.AppendLine("--------------------------------------------------");
            report.AppendLine($"Type: {_exception.GetType().FullName}");
            report.AppendLine($"Message: {_exception.Message}");
            report.AppendLine($"Source: {_exception.Source}");
            report.AppendLine();
            report.AppendLine("Stack Trace:");
            report.AppendLine(_exception.StackTrace);

            if (_exception.InnerException != null)
            {
                report.AppendLine();
                report.AppendLine("Inner Exception:");
                report.AppendLine($"Type: {_exception.InnerException.GetType().FullName}");
                report.AppendLine($"Message: {_exception.InnerException.Message}");
                report.AppendLine("Stack Trace:");
                report.AppendLine(_exception.InnerException.StackTrace);
            }

            report.AppendLine();
        }

        private void AppendScreenshotInfo(StringBuilder report)
        {
            report.AppendLine("SCREENSHOT");
            report.AppendLine("--------------------------------------------------");
            report.AppendLine($"Path: {_screenshotPath}");

            try
            {
                var fileInfo = new FileInfo(_screenshotPath);
                if (fileInfo.Exists)
                {
                    report.AppendLine($"Size: {fileInfo.Length / 1024} KB");
                    report.AppendLine($"Created: {fileInfo.CreationTime:yyyy-MM-dd HH:mm:ss}");
                }
            }
            catch (Exception ex)
            {
                report.AppendLine($"Error reading screenshot info: {ex.Message}");
            }

            report.AppendLine();
        }

        private string SaveReport(string content)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string safeCommandName = string.Join("_", _commandName.Split(Path.GetInvalidFileNameChars()));
            string fileName = $"DiagnosticReport_{safeCommandName}_{timestamp}.txt";
            string filePath = Path.Combine(ErrorHandler.GetLogDirectory(), fileName);

            File.WriteAllText(filePath, content);
            ErrorHandler.LogInfo("DiagnosticReport", $"Saved diagnostic report: {fileName}");

            return filePath;
        }

        /// <summary>
        /// Generates a quick diagnostic report without requiring a document
        /// </summary>
        public static string GenerateQuick(string commandName, Exception exception = null, string screenshotPath = null)
        {
            try
            {
                StringBuilder report = new StringBuilder();

                report.AppendLine("====================================================");
                report.AppendLine("REVIT TAB QUICK DIAGNOSTIC REPORT");
                report.AppendLine("====================================================");
                report.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                report.AppendLine($"Command: {commandName}");
                report.AppendLine();

                report.AppendLine("SYSTEM INFORMATION");
                report.AppendLine("--------------------------------------------------");
                report.AppendLine($"OS: {Environment.OSVersion}");
                report.AppendLine($"Machine: {Environment.MachineName}");
                report.AppendLine($"User: {Environment.UserName}");
                report.AppendLine();

                if (exception != null)
                {
                    report.AppendLine("EXCEPTION INFORMATION");
                    report.AppendLine("--------------------------------------------------");
                    report.AppendLine($"Type: {exception.GetType().FullName}");
                    report.AppendLine($"Message: {exception.Message}");
                    report.AppendLine($"Stack Trace:");
                    report.AppendLine(exception.StackTrace);
                    report.AppendLine();
                }

                if (!string.IsNullOrEmpty(screenshotPath))
                {
                    report.AppendLine($"Screenshot: {screenshotPath}");
                    report.AppendLine();
                }

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string safeCommandName = string.Join("_", commandName.Split(Path.GetInvalidFileNameChars()));
                string fileName = $"QuickDiagnostic_{safeCommandName}_{timestamp}.txt";
                string filePath = Path.Combine(ErrorHandler.GetLogDirectory(), fileName);

                File.WriteAllText(filePath, report.ToString());
                return filePath;
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError($"Failed to generate quick diagnostic report: {ex.Message}");
                return null;
            }
        }
    }
}
