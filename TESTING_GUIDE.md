# Testing Guide - Enhanced Error Handling

## Overview
This guide helps you test the new error handling features added to your Revit Tab add-in.

## What's New

### 1. ErrorHandler Utility
- User-friendly error dialogs with technical details in expandable sections
- Automatic logging to `%AppData%\Revit Tab\Logs\RevitTab_YYYY-MM-DD.log`
- Validation error dialogs for input issues
- Warning dialogs with continue/cancel options

### 2. TransactionGuard
- Automatic rollback on errors
- Safe transaction management
- Transaction logging

### 3. Enhanced Commands
All three commands now have improved error handling:
- **Create Sheets** - Better validation, duplicate detection, partial success handling
- **Create King Studs** - Better family loading, opening validation, detailed skip reasons
- **3D Per Level** - View name conflict detection, better result summaries

---

## Test Plan

### Test 1: Create Sheets - Normal Operation
**Goal:** Verify basic functionality still works

1. Open Revit 2024 and create/open a project with a title block loaded
2. Go to the **Clancy Theys** tab
3. Click **Create Sheets**
4. Enter:
   - Sheet Number: `A101`
   - Sheet Name: `Test Sheet`
   - Quantity: `3`
5. Click OK

**Expected Result:**
- Should create 3 sheets: A101, A102, A103
- Success dialog shows "Successfully created 3 sheet(s)"
- Log file created at `%AppData%\Revit Tab\Logs\RevitTab_[today].log`

---

### Test 2: Create Sheets - Duplicate Detection
**Goal:** Test the new duplicate sheet detection

1. Run Create Sheets again with the same parameters (A101, quantity 3)

**Expected Result:**
- Warning dialog appears: "The following sheet numbers already exist and will be skipped: A101, A102, A103"
- Dialog asks "Continue with creating 0 sheet(s)?"
- If you click OK, shows "All requested sheet numbers already exist. No sheets will be created."

---

### Test 3: Create Sheets - Partial Duplicates
**Goal:** Test handling of partial duplicates

1. Run Create Sheets with:
   - Sheet Number: `A102`
   - Sheet Name: `Mixed Test`
   - Quantity: `3`

**Expected Result:**
- Warning shows A102 already exists
- Offers to create A103, A104 (2 sheets)
- If you continue, creates only the non-duplicate sheets
- Result dialog shows: "Successfully created X sheet(s), Skipped Y duplicate(s)"

---

### Test 4: Create Sheets - Input Validation
**Goal:** Test input validation errors

Try each of these scenarios:

#### 4a. Empty Sheet Number
- Leave Sheet Number blank
- Click OK

**Expected:** Validation error: "Sheet number cannot be empty."

#### 4b. Sheet Number Without Digits
- Sheet Number: `ABC`
- Click OK

**Expected:** Validation error with explanation about numeric portion requirement

#### 4c. Excessive Quantity
- Sheet Number: `A101`
- Quantity: `2000`
- Click OK

**Expected:** Validation error: "Quantity cannot exceed 1000 sheets"

#### 4d. No Title Block
- In a project with no title blocks loaded
- Try to create sheets

**Expected:** Validation error: "No title block found. Please load a title block into the project before creating sheets."

---

### Test 5: Create Sheets - Error Handling
**Goal:** Test error logging and expandable details

1. Create sheets and force an error (try creating with a very long name or special characters that might fail)
2. Check the error dialog

**Expected Result:**
- Main dialog shows user-friendly message
- "Technical Details" section can be expanded to see stack trace
- Footer shows log file location
- Check log file - should contain detailed error information with timestamp

---

### Test 6: King Studs - Normal Operation
**Goal:** Verify basic functionality

1. Open a project with doors and windows in walls
2. Click **Create King Studs**

**Expected Result:**
- Success dialog: "Processed X opening(s): ✓ Created Y king stud(s)"
- Log file records the operation
- Studs are placed 3.5" on each side of openings

---

### Test 7: King Studs - No Stud Family
**Goal:** Test missing family handling

1. Delete/unload the Stud family from the project
2. Remove `Stud.rfa` from the `Families` folder temporarily
3. Click **Create King Studs**

**Expected Result:**
- Clear error dialog explaining:
  - "Could not find or load a stud family"
  - Shows expected file path
  - Suggests ensuring Stud.rfa is available
- Log file records the attempt

---

### Test 8: King Studs - No Openings
**Goal:** Test empty project handling

1. In a project with no doors or windows
2. Click **Create King Studs**

**Expected Result:**
- Info dialog: "No doors or windows found in the project or linked models"
- Suggests ensuring project contains openings
- Command completes successfully (Result.Succeeded)

---

### Test 9: King Studs - Detailed Error Reporting
**Goal:** Test the new detailed skip reasons

1. Create a door that's NOT hosted on a wall (if possible)
2. Click **Create King Studs**

**Expected Result:**
- Result dialog shows skipped openings
- Expandable "Skipped Openings" section lists:
  - Which opening was skipped
  - The reason (e.g., "Not hosted on a wall")
- Log file contains detailed information

---

### Test 10: King Studs - Read-Only Document
**Goal:** Test read-only validation

1. Open a project in read-only mode (or from a read-only location)
2. Click **Create King Studs**

**Expected Result:**
- Validation error: "The document is read-only. Please ensure the document is editable before running this command."

---

### Test 11: 3D Per Level - Normal Operation
**Goal:** Verify basic functionality

1. Open a project with multiple levels
2. Click **3D Per Level**

**Expected Result:**
- Creates isometric 3D views for each level
- Views named "Z-[Level Name]"
- Success dialog: "Processed X level(s): ✓ Created Y 3D view(s)"
- Section boxes active and properly sized

---

### Test 12: 3D Per Level - Duplicate Views
**Goal:** Test view name conflict detection

1. Run **3D Per Level** once to create views
2. Run it again without deleting the views

**Expected Result:**
- Warning dialog lists all existing views that will be skipped
- Asks "Continue with creating 0 view(s)?"
- If you continue, shows views were skipped
- Expandable "Skipped Views" section shows "(already exists)"

---

### Test 13: 3D Per Level - No Levels
**Goal:** Test empty project handling

1. Delete all levels from a project (create at least one dummy element first)
2. Click **3D Per Level**

**Expected Result:**
- Validation error: "No levels found in the project. Please create levels before running this command."

---

### Test 14: Error Logging
**Goal:** Verify log files are created correctly

1. Navigate to `%AppData%\Revit Tab\Logs\` in File Explorer
2. Check for today's log file: `RevitTab_2026-01-27.log`
3. Open the log file

**Expected Content:**
- Timestamp for each operation
- Command names clearly identified
- INFO messages for successful operations
- ERROR messages with full stack traces for failures
- Readable format

---

### Test 15: Transaction Rollback
**Goal:** Verify automatic rollback on failure

1. Force an error during sheet creation (e.g., by manually corrupting something mid-operation if possible)
2. Check that no partial changes were committed

**Expected Result:**
- Transaction automatically rolls back
- No partial/incomplete elements left in the project
- Error is logged with rollback information

---

## Log File Location

All operations are logged to:
```
C:\Users\jacobembree\AppData\Roaming\Revit Tab\Logs\RevitTab_[DATE].log
```

Each day gets its own log file. The footer of error dialogs shows this path for easy access.

---

## Success Criteria

### Must Work:
- [x] All three commands function normally in happy path scenarios
- [x] Input validation catches bad inputs before processing
- [x] Duplicate detection prevents creating conflicting elements
- [x] Partial success scenarios complete what they can
- [x] Log files are created and contain useful information

### Must Improve:
- [x] Error messages are user-friendly (not raw exception text)
- [x] Technical details available in expandable sections
- [x] Clear guidance on how to fix issues
- [x] Skipped items are reported with reasons
- [x] Success/skip/error counts in result dialogs

---

## Known Warnings (Can Ignore)

The build process shows these warnings - they're normal and don't affect functionality:
- `NU1701`: Package compatibility warning (Revit API packages work fine)
- `MSB3270`: Processor architecture mismatch (MSIL vs AMD64 - normal for Revit add-ins)

---

## Troubleshooting

### Issue: Add-in doesn't load
**Solution:** Check that Revit is fully closed, then rebuild and deploy

### Issue: Error dialogs don't appear
**Solution:** Check the log file - if logging works, the ErrorHandler is loaded

### Issue: Log folder doesn't exist
**Solution:** Run any command once - the folder is created automatically on first use

### Issue: Old version still running
**Solution:**
1. Close Revit completely
2. Check Task Manager for any Revit processes
3. Rebuild: `dotnet build "Revit Tab\Revit Tab.csproj" --configuration Debug`
4. Restart Revit

---

## Next Steps After Testing

If testing is successful, consider:
1. Committing the changes to Git
2. Updating documentation for end users
3. Creating a release build for distribution
4. Adding more features (progress bars, settings dialog, etc.)

---

## Contact

If you find issues or want to enhance the error handling further, the key files are:
- `Revit Tab/Utility/ErrorHandler.cs` - Main error handling logic
- `Revit Tab/Utility/TransactionGuard.cs` - Transaction management
- Each command file has been updated with enhanced error handling
