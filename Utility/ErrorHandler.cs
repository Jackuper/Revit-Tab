using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Autodesk.Revit.UI;

namespace Revit_Tab.Utility
{
    /// <summary>
    /// Centralized error handling and logging utility for Revit Tab commands
    /// </summary>
    public static class ErrorHandler
    {
        private static readonly string LogDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Revit Tab", "Logs");

        private static readonly string ScreenshotDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Revit Tab", "Logs", "Screenshots");

        private static readonly object LogLock = new object();

        static ErrorHandler()
        {
            try
            {
                if (!Directory.Exists(LogDirectory))
                {
                    Directory.CreateDirectory(LogDirectory);
                }
                if (!Directory.Exists(ScreenshotDirectory))
                {
                    Directory.CreateDirectory(ScreenshotDirectory);
                }
            }
            catch
            {
                // If we can't create log directory, we'll just skip logging
            }
        }

        /// <summary>
        /// Logs an error and shows a user-friendly dialog
        /// </summary>
        public static void HandleError(string commandName, Exception ex, bool showDialog = true, bool captureScreenshot = true)
        {
            string screenshotPath = null;

            // Capture screenshot before showing dialog
            if (captureScreenshot)
            {
                screenshotPath = CaptureScreenshot(commandName);
            }

            string errorMessage = FormatErrorMessage(commandName, ex, screenshotPath);
            LogError(errorMessage);

            if (showDialog)
            {
                ShowErrorDialog(commandName, ex, screenshotPath);
            }
        }

        /// <summary>
        /// Shows a detailed error dialog to the user
        /// </summary>
        public static void ShowErrorDialog(string commandName, Exception ex, string screenshotPath = null)
        {
            string userMessage = GetUserFriendlyMessage(ex);
            string technicalDetails = GetTechnicalDetails(ex);

            string footerText = "Error details have been logged to:\n" + GetCurrentLogFilePath();
            if (!string.IsNullOrEmpty(screenshotPath))
            {
                footerText += "\n\nScreenshot saved to:\n" + screenshotPath;
            }

            TaskDialog dialog = new TaskDialog(commandName + " - Error")
            {
                MainIcon = TaskDialogIcon.TaskDialogIconWarning,
                MainInstruction = "An error occurred while executing " + commandName,
                MainContent = userMessage,
                ExpandedContent = "Technical Details:\n\n" + technicalDetails,
                CommonButtons = TaskDialogCommonButtons.Close,
                DefaultButton = TaskDialogResult.Close,
                FooterText = footerText
            };

            dialog.Show();
        }

        /// <summary>
        /// Shows a validation error dialog
        /// </summary>
        public static void ShowValidationError(string commandName, string validationMessage)
        {
            TaskDialog dialog = new TaskDialog(commandName + " - Validation Error")
            {
                MainIcon = TaskDialogIcon.TaskDialogIconWarning,
                MainInstruction = "Input Validation Failed",
                MainContent = validationMessage,
                CommonButtons = TaskDialogCommonButtons.Close,
                DefaultButton = TaskDialogResult.Close
            };

            dialog.Show();
        }

        /// <summary>
        /// Shows a warning dialog with continue/cancel options
        /// </summary>
        public static TaskDialogResult ShowWarningDialog(string commandName, string warningMessage, string details = null)
        {
            TaskDialog dialog = new TaskDialog(commandName + " - Warning")
            {
                MainIcon = TaskDialogIcon.TaskDialogIconWarning,
                MainInstruction = warningMessage,
                CommonButtons = TaskDialogCommonButtons.Ok | TaskDialogCommonButtons.Cancel,
                DefaultButton = TaskDialogResult.Ok
            };

            if (!string.IsNullOrEmpty(details))
            {
                dialog.ExpandedContent = details;
            }

            return dialog.Show();
        }

        /// <summary>
        /// Logs an error message to file
        /// </summary>
        public static void LogError(string message)
        {
            try
            {
                lock (LogLock)
                {
                    string logFile = GetCurrentLogFilePath();
                    File.AppendAllText(logFile, message + Environment.NewLine + Environment.NewLine);
                }
            }
            catch
            {
                // If logging fails, don't crash the application
            }
        }

        /// <summary>
        /// Logs an informational message
        /// </summary>
        public static void LogInfo(string commandName, string message)
        {
            string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{commandName}] INFO: {message}";
            LogError(logMessage); // Reuse same method since it's just file writing
        }

        private static string FormatErrorMessage(string commandName, Exception ex, string screenshotPath = null)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"============================================");
            sb.AppendLine($"ERROR LOG - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"============================================");
            sb.AppendLine($"Command: {commandName}");
            sb.AppendLine($"Error Type: {ex.GetType().Name}");
            sb.AppendLine($"Message: {ex.Message}");

            if (!string.IsNullOrEmpty(screenshotPath))
            {
                sb.AppendLine($"Screenshot: {screenshotPath}");
            }

            sb.AppendLine($"Stack Trace:");
            sb.AppendLine(ex.StackTrace);

            if (ex.InnerException != null)
            {
                sb.AppendLine($"\nInner Exception: {ex.InnerException.GetType().Name}");
                sb.AppendLine($"Inner Message: {ex.InnerException.Message}");
                sb.AppendLine($"Inner Stack Trace:");
                sb.AppendLine(ex.InnerException.StackTrace);
            }

            return sb.ToString();
        }

        private static string GetUserFriendlyMessage(Exception ex)
        {
            // Map common exception types to user-friendly messages
            if (ex is UnauthorizedAccessException)
            {
                return "Access denied. Please check that you have permission to modify this project.";
            }
            else if (ex is InvalidOperationException)
            {
                return "The operation could not be completed. " + ex.Message;
            }
            else if (ex is ArgumentException)
            {
                return "Invalid input provided. " + ex.Message;
            }
            else if (ex is FileNotFoundException || ex is DirectoryNotFoundException)
            {
                return "A required file or resource could not be found. " + ex.Message;
            }
            else if (ex.Message.Contains("not editable") || ex.Message.Contains("workshared"))
            {
                return "The project is not editable. It may be opened in read-only mode or you may need to synchronize with central.";
            }
            else if (ex.Message.Contains("already exists"))
            {
                return "An element with that name already exists. Please use a different name.";
            }
            else
            {
                return ex.Message;
            }
        }

        private static string GetTechnicalDetails(Exception ex)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Exception Type: {ex.GetType().FullName}");
            sb.AppendLine($"Message: {ex.Message}");
            sb.AppendLine($"Source: {ex.Source}");
            sb.AppendLine($"\nStack Trace:");
            sb.AppendLine(ex.StackTrace);

            if (ex.InnerException != null)
            {
                sb.AppendLine($"\n--- Inner Exception ---");
                sb.AppendLine($"Type: {ex.InnerException.GetType().FullName}");
                sb.AppendLine($"Message: {ex.InnerException.Message}");
                sb.AppendLine($"Stack Trace:");
                sb.AppendLine(ex.InnerException.StackTrace);
            }

            return sb.ToString();
        }

        private static string GetCurrentLogFilePath()
        {
            string fileName = $"RevitTab_{DateTime.Now:yyyy-MM-dd}.log";
            return Path.Combine(LogDirectory, fileName);
        }

        /// <summary>
        /// Gets the log directory path for display
        /// </summary>
        public static string GetLogDirectory()
        {
            return LogDirectory;
        }

        /// <summary>
        /// Captures a screenshot of the current screen
        /// </summary>
        /// <param name="commandName">The command name for filename</param>
        /// <returns>Path to the saved screenshot, or null if capture failed</returns>
        public static string CaptureScreenshot(string commandName)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff");
                string safeCommandName = string.Join("_", commandName.Split(Path.GetInvalidFileNameChars()));
                string fileName = $"{safeCommandName}_{timestamp}.png";
                string filePath = Path.Combine(ScreenshotDirectory, fileName);

                // Get the bounds of all screens combined
                Rectangle bounds = SystemInformation.VirtualScreen;

                using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
                {
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        g.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size);
                    }

                    bitmap.Save(filePath, ImageFormat.Png);
                }

                LogInfo("Screenshot", $"Captured screenshot: {fileName}");
                return filePath;
            }
            catch (Exception ex)
            {
                // Don't crash if screenshot fails
                LogError($"Failed to capture screenshot: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets the screenshot directory path
        /// </summary>
        public static string GetScreenshotDirectory()
        {
            return ScreenshotDirectory;
        }
    }
}
