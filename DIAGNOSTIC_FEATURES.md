# Diagnostic Features Guide

## Overview
Your Revit Tab add-in now includes comprehensive diagnostic and troubleshooting features that help identify and resolve issues quickly. These features allow you (and me, Claude) to see exactly what's happening when errors occur.

## ✨ New Features

### 1. Automatic Screenshot Capture
When errors occur, the system automatically captures a screenshot of your entire screen so you can see the exact state of Revit when the error happened.

**Features:**
- Captures all monitors
- Saves to `%AppData%\Revit Tab\Logs\Screenshots\`
- Filename includes command name and timestamp
- Referenced in error logs for easy correlation

### 2. Detailed Error Reports
Every error now generates a comprehensive diagnostic report with:
- System information (OS, hardware, user)
- Revit information (version, build, language)
- Add-in information (version, build date, size)
- Document information (sheets, levels, elements, linked models)
- Full exception details with stack traces
- Screenshot reference

### 3. Real-Time Log Viewer
A beautiful, dark-themed log viewer window that monitors logs as they're written.

**Features:**
- Real-time updates as operations run
- Filter by log level (All, INFO, ERROR)
- Auto-scroll to latest entries
- Syntax highlighting with different colors
- Quick access to log and screenshot folders
- File size monitoring

---

## 🚀 How to Use

### Opening the Log Viewer

1. Open Revit 2024 (or 2023)
2. Go to the **Clancy Theys** tab
3. Click the **Log Viewer** button
4. A new window will open showing real-time logs

### Using the Log Viewer

#### Toolbar Options:
- **Log Level dropdown**: Filter by All, INFO, or ERROR
- **Auto-scroll checkbox**: Keeps view scrolled to newest entries
- **Refresh button**: Manually reload the log file
- **Clear Display button**: Clears the display (doesn't delete the file)
- **Open Log Folder**: Opens Windows Explorer to the logs folder
- **Open Screenshots**: Opens the screenshots folder

#### Status Bar:
- Left side: Shows current status and last update time
- Right side: Shows the current log file being monitored

### Testing the Features

#### Test 1: View Real-Time Logs
1. Open the Log Viewer
2. Go back to Revit (keep Log Viewer open)
3. Run any command (Create Sheets, King Studs, 3D Per Level)
4. Watch the Log Viewer update in real-time
5. You'll see:
   - `[INFO]` entries for successful operations
   - `[ERROR]` entries for failures
   - Timestamps for each operation

#### Test 2: Screenshot Capture
1. Close the Log Viewer
2. Force an error (try creating sheets with no title block)
3. When the error dialog appears, note the footer
4. Click "Open Log Folder" from the Log Viewer
5. Navigate to the `Screenshots` subfolder
6. You'll see a PNG screenshot with timestamp

#### Test 3: Detailed Error Report
1. In the Log Viewer, click "Open Log Folder"
2. Find today's log file: `RevitTab_2026-01-27.log`
3. Open it in a text editor
4. You'll see:
   - Section headers like `=====ERROR LOG=====`
   - Command name
   - Error type and message
   - Screenshot path
   - Full stack trace

---

## 📁 File Locations

### Log Files
```
C:\Users\jacobembree\AppData\Roaming\Revit Tab\Logs\
```

Daily log files named: `RevitTab_YYYY-MM-DD.log`

### Screenshots
```
C:\Users\jacobembree\AppData\Roaming\Revit Tab\Logs\Screenshots\
```

Screenshot files named: `CommandName_YYYY-MM-DD_HH-mm-ss-fff.png`

### Diagnostic Reports
Saved in the same Logs folder with detailed system and project information.

---

## 🔍 Sharing Diagnostics with Claude

When you encounter an issue and want my help:

### Quick Method:
1. Open the Log Viewer
2. Click "Open Log Folder"
3. Share today's log file

### Detailed Method (for complex issues):
1. After encountering an error:
   - A screenshot is automatically captured
   - Error is logged with full details
2. Open Log Folder (from Log Viewer or error dialog)
3. Share with me:
   - Today's log file (`RevitTab_YYYY-MM-DD.log`)
   - The screenshot from the Screenshots folder
   - Optionally: Any diagnostic report files

### What to Share:
Just paste the contents of the log file into our conversation. I can then see:
- Exact error messages
- Stack traces showing where it failed
- System configuration
- Project state when error occurred
- Sequence of operations leading to the error

---

## 📊 Log Format

### INFO Entries
```
[2026-01-27 10:04:15] [Create Sheets] INFO: Command started
[2026-01-27 10:04:17] [Create Sheets] INFO: Creating 3 sheets starting with A101
[2026-01-27 10:04:18] [Create Sheets] INFO: Command completed: 3 sheets created
```

### ERROR Entries
```
============================================
ERROR LOG - 2026-01-27 10:05:22
============================================
Command: Create Sheets
Error Type: InvalidOperationException
Message: No title block found...
Screenshot: C:\Users\...\Screenshots\Create_Sheets_2026-01-27_10-05-22-123.png
Stack Trace:
   at Revit_Tab.CreateSheetCommand.Execute(...)
   ...
```

---

## 🎨 Log Viewer UI

The Log Viewer features a modern, dark-themed interface:
- **Dark background (#1E1E1E)** - Easy on the eyes
- **Monospace font (Consolas)** - Aligned, readable logs
- **Color-coded status** - Quick visual feedback
- **Responsive layout** - Resizable and scrollable

---

## 🛠️ Advanced Features

### DiagnosticReport Class
Generates comprehensive reports programmatically:

```csharp
// In any command, you can generate a diagnostic report:
var report = new DiagnosticReport(
    application,
    document,
    "CommandName",
    exception,
    screenshotPath
);
string reportPath = report.Generate();
```

### Custom Screenshot Capture
```csharp
// Manually capture a screenshot:
string screenshotPath = ErrorHandler.CaptureScreenshot("MyOperation");
```

### Manual Logging
```csharp
// Log info messages:
ErrorHandler.LogInfo("CommandName", "Operation successful");

// Log errors without showing dialog:
ErrorHandler.HandleError("CommandName", exception, showDialog: false);
```

---

## 🧪 Testing Checklist

- [ ] Log Viewer opens successfully
- [ ] Real-time updates work when running commands
- [ ] Filter dropdown changes displayed content
- [ ] Auto-scroll keeps view at bottom
- [ ] Open Log Folder button works
- [ ] Open Screenshots button works
- [ ] Screenshots are captured when errors occur
- [ ] Error dialogs show screenshot path in footer
- [ ] Log files contain detailed information
- [ ] Multiple monitors are captured in screenshots

---

## 💡 Tips

1. **Keep Log Viewer Open**: While testing or debugging, keep the Log Viewer open to see real-time feedback

2. **Filter by ERROR**: When troubleshooting, filter by ERROR to see only problems

3. **Clear Display**: Use "Clear Display" to start fresh without deleting the actual log file

4. **Screenshot Folder**: Check the Screenshots folder periodically - you might find helpful captures you forgot about

5. **Share Logs**: When asking for help, always share the log file - it has way more detail than what you see in dialogs

---

## 🚦 What Happens When...

### An Error Occurs:
1. Screenshot is automatically captured
2. Full error details logged to file
3. User-friendly dialog shown
4. Dialog footer shows log and screenshot locations
5. Technical details available in expandable section

### A Command Succeeds:
1. INFO message logged with timestamp
2. Operation details recorded
3. No screenshot taken (only on errors)

### You Open Log Viewer:
1. Today's log file is loaded
2. File watcher starts monitoring
3. Updates appear automatically every 2 seconds
4. Status bar shows file size and update time

---

## ❓ Troubleshooting

### Q: Log Viewer shows "No log entries for today"
**A:** No commands have been run yet. Run any command to generate logs.

### Q: Real-time updates aren't working
**A:** Click "Refresh" button. The timer updates every 2 seconds.

### Q: Can't find Screenshots folder
**A:** It only exists after the first error with screenshot capture. Force an error to create it.

### Q: Logs are too large
**A:** Each day gets a new file. Old files can be deleted manually from the Logs folder.

### Q: Want to clear logs completely
**A:** Close Revit, go to `%AppData%\Revit Tab\Logs\` and delete old files.

---

## 🎯 Benefits

### For You:
- See exactly what's happening in real-time
- Understand why errors occur
- Track command execution
- Debug issues quickly

### For Me (Claude):
- View exact error messages and stack traces
- See system and project configuration
- Understand the sequence of events
- Provide accurate troubleshooting steps
- Identify root causes faster

### For Both:
- Faster issue resolution
- Better communication about problems
- Visual evidence via screenshots
- Complete diagnostic information

---

## 🔄 Next Steps

1. **Test It Out**: Open the Log Viewer and run some commands
2. **Force an Error**: Try creating sheets without a title block to see error capture
3. **Explore Logs**: Open the log folder and check out the file format
4. **Share with Me**: When issues occur, share the log file contents

---

## 📝 Summary

Your Revit Tab now has enterprise-grade diagnostics:
- ✅ Automatic screenshot capture
- ✅ Real-time log monitoring
- ✅ Detailed error reports
- ✅ User-friendly dialogs
- ✅ Easy sharing with Claude

All designed to help us quickly identify and fix any issues that arise!
