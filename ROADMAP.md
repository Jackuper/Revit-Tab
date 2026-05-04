# Revit-Tab Roadmap

A living document — update this every time you sit down to work. When you switch computers, this is your first read.

---

## How to use this file

1. **Before you start coding:** Read "Current Session" and "Up Next" so you know exactly where you left off.
2. **While you work:** Move tasks between sections as you go.
3. **Before you stop:** Write a quick note in "Last Session Notes" — future-you will thank you.
4. **Commit this file** along with your code changes every session.

---

## Last Session Notes

> _(Write a note here before you stop working. What were you in the middle of? What was the next small step? Any error you were chasing?)_
>
> Example: "Got King Studs working on linked models. Left off trying to figure out why diagonal walls aren't detecting openings correctly. Next step: test with a simple diagonal wall in a fresh model."

---

## Status Key

- ✅ Done — working and deployed
- 🔧 In Progress — actively being worked on
- 📋 Up Next — clearly defined, ready to start
- 💡 Ideas — not defined yet, just captured so you don't forget
- ❌ Blocked — can't move forward until something else is resolved

---

## Tools / Features

### 🔧 Create Sheets
Batch creates sheets with auto-incrementing numbers.
- Input: Sheet Number, Name, Quantity
- Core functionality works
- **Needs work:** Visual/UI improvements — dialog feels unpolished, revisit layout and styling

### 🔧 King Studs
Places structural stud families around door/window openings.
- Supports linked models with coordinate transforms
- **Needs work:** Placing the wrong family — needs to be fixed before this is reliable
- **Known edge case:** diagonal walls — behavior untested

### 🔧 3D Per Level
Creates an isometric 3D view per level with section box cropped to that level.
- **Needs work:** Not behaving as intended — revisit section box logic and view output

---

## Current Session

> _(Move ONE task here when you sit down. Just one. Finish it before picking the next.)_

---

## Up Next (defined and ready)

- [ ] Test King Studs against diagonal walls — create a simple test model
- [ ] Add error message when `Stud.rfa` family is missing instead of silent failure
- [ ] Clean up loose `.cs` files in root (DwgParser.cs, ImportTrussCommand.cs, TrussData.cs) — move into `Source/Commands/` or a `Trusses/` subfolder
- [ ] Review and activate Aspose.Cells usage (currently referenced but unused)
- [ ] Add a button icon for any tools missing one (check Images/ folder)

---

## Ideas / Future Tools

> _(Dump ideas here. Don't filter them. You can decide if they're worth building later.)_

- [ ] Truss import tool — import truss geometry from DWG/external source (files already started: DwgParser.cs, ImportTrussCommand.cs)
- [ ] View naming / renaming tool
- [ ] Sheet index / transmittal generator
- [ ] Parameter batch editor (fill in shared parameters across multiple elements)
- [ ] Room/space tagging automation
- [ ] _(add yours here)_

---

## Blocked

> _(Anything you can't move forward on right now, and why)_

---

## Done Archive

> _(Move completed items here from "Up Next" once fully done and stable — keeps the active list clean)_

- ✅ Basic ribbon tab and panel setup (RevitApp.cs)
- ✅ Multi-version targeting (.NET 4.8 for 2023/2024, .NET 8 for 2025)
- ✅ GitHub Actions CI/CD on push to main
- ✅ Local deploy scripts (Deploy-Local.ps1, Clean-And-Deploy.ps1)
- ✅ CLAUDE.md context file for AI-assisted development

---

## Distribution Checklist

> _(When you're ready to hand this off to coworkers)_

- [ ] Confirm DLL deploys correctly to `%AppData%\Autodesk\Revit\Addins\{YEAR}\Jcup\`
- [ ] Write simple install instructions (drop files here, restart Revit)
- [ ] Test on a clean machine that has never had this add-in installed
- [ ] Decide which Revit versions to support in the release zip
- [ ] Create a GitHub Release with the built DLL attached

