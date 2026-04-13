using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

// AutoCAD COM references — add these via COM tab in VS References:
//   Autodesk AutoCAD Type Library  (acax##enu.tlb)
//   AutoCAD/ObjectDBX Common       (acdbmgd.dll or via COM)
// The interop types used here are late-bound via dynamic to avoid
// hard version dependencies — works with AutoCAD 2020-2025.

namespace Revit_Tab
{
    /// <summary>
    /// Parses a .dwg file using the AutoCAD COM API (requires AutoCAD installed).
    /// Extracts all 2D polylines and converts their segments into TrussCenterline objects.
    ///
    /// UNIT ASSUMPTION: DWG is in inches. All output is converted to decimal feet
    /// to match Revit internal units.
    /// If your DWG is in feet, set DwgUnitsAreInches = false.
    /// </summary>
    public static class DwgParser
    {
        public static bool DwgUnitsAreInches { get; set; } = true;

        /// <summary>
        /// Opens the DWG in a hidden AutoCAD instance, reads all polylines,
        /// returns them as TrussCenterline segments (one per polyline segment).
        ///
        /// Each polyline becomes ONE centerline (start = first vertex, end = last vertex).
        /// If your trusses are drawn as multi-segment polylines representing the full
        /// truss boundary, set treatPolylineAsOneMember = true (default).
        /// Set to false to break each polyline segment into a separate member.
        /// </summary>
        public static List<TrussCenterline> ParseDwg(
            string dwgFilePath,
            string layerFilter = "",          // leave empty to read all layers
            bool treatPolylineAsOneMember = true)
        {
            var centerlines = new List<TrussCenterline>();

            dynamic acApp = null;
            dynamic acDoc = null;

            try
            {
                // Try to get a running AutoCAD instance first
#if NETFRAMEWORK
                try { acApp = Marshal.GetActiveObject("AutoCAD.Application"); } catch { }
#endif
                if (acApp == null)
                {
                    // No running instance — create a new hidden one
                    Type acType = Type.GetTypeFromProgID("AutoCAD.Application");
                    if (acType == null)
                        throw new Exception(
                            "AutoCAD is not installed or not registered on this machine.");

                    acApp = Activator.CreateInstance(acType);
                    acApp.Visible = false;
                }

                // Open the document
                acDoc = acApp.Documents.Open(dwgFilePath, true); // true = read-only
                dynamic modelSpace = acDoc.ModelSpace;

                foreach (dynamic entity in modelSpace)
                {
                    try
                    {
                        string entityType = entity.EntityName; // e.g. "AcDbPolyline", "AcDb2dPolyline"

                        bool isPoly = entityType.IndexOf("Polyline", StringComparison.OrdinalIgnoreCase) >= 0
                                   || entityType.IndexOf("LWPolyline", StringComparison.OrdinalIgnoreCase) >= 0;

                        if (!isPoly) continue;

                        // Layer filter
                        if (!string.IsNullOrEmpty(layerFilter) &&
                            !string.Equals(entity.Layer, layerFilter, StringComparison.OrdinalIgnoreCase))
                            continue;

                        // Get coordinates array — COM returns a flat double array: [x0,y0,z0, x1,y1,z1, ...]
                        double[] coords = (double[])entity.Coordinates;
                        int dim = 2; // LWPolyline uses 2D coords

                        // Detect if 3D polyline (z values included)
                        // AcDb2dPolyline / AcDb3dPolyline have 3 values per vertex
                        if (entityType.IndexOf("2d", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            entityType.IndexOf("3d", StringComparison.OrdinalIgnoreCase) >= 0)
                            dim = 3;

                        int vertexCount = coords.Length / dim;
                        if (vertexCount < 2) continue;

                        string layer = entity.Layer;

                        if (treatPolylineAsOneMember)
                        {
                            // Use first and last vertex as the truss centerline
                            double x1 = ToFeet(coords[0]);
                            double y1 = ToFeet(coords[1]);
                            double x2 = ToFeet(coords[(vertexCount - 1) * dim]);
                            double y2 = ToFeet(coords[(vertexCount - 1) * dim + 1]);

                            centerlines.Add(new TrussCenterline
                            {
                                StartX = x1,
                                StartY = y1,
                                EndX   = x2,
                                EndY   = y2,
                                Layer  = layer
                            });
                        }
                        else
                        {
                            // Each segment of the polyline becomes its own member
                            for (int i = 0; i < vertexCount - 1; i++)
                            {
                                double x1 = ToFeet(coords[i * dim]);
                                double y1 = ToFeet(coords[i * dim + 1]);
                                double x2 = ToFeet(coords[(i + 1) * dim]);
                                double y2 = ToFeet(coords[(i + 1) * dim + 1]);

                                centerlines.Add(new TrussCenterline
                                {
                                    StartX = x1,
                                    StartY = y1,
                                    EndX   = x2,
                                    EndY   = y2,
                                    Layer  = layer
                                });
                            }
                        }
                    }
                    catch
                    {
                        // Skip entities that don't support Coordinates (hatches, text, etc.)
                        continue;
                    }
                }
            }
            finally
            {
                // Close document without saving
                if (acDoc != null)
                {
                    try { acDoc.Close(false); } catch { }
                    Marshal.ReleaseComObject(acDoc);
                }

                // Only quit AutoCAD if we launched it (not if it was already open)
                // To be safe, we never quit — just release the COM object
                if (acApp != null)
                    Marshal.ReleaseComObject(acApp);
            }

            return centerlines;
        }

        /// <summary>
        /// Reads the DWG and returns one TrussInstance per closed polyline on the TRUSS layer.
        /// Also reads TEXT/MTEXT entities to detect truss type callouts nearby.
        /// Callout format: "{qty} {type}" e.g. "3 F62" or just "F62".
        /// </summary>
        public static List<TrussInstance> ParseTrussInstances(string dwgFilePath)
        {
            var instances = new List<TrussInstance>();
            var callouts  = new List<(double cx, double cy, string typeKey)>();

            dynamic acApp = null;
            dynamic acDoc = null;

            try
            {
#if NETFRAMEWORK
                try { acApp = Marshal.GetActiveObject("AutoCAD.Application"); } catch { }
#endif
                if (acApp == null)
                {
                    Type t = Type.GetTypeFromProgID("AutoCAD.Application");
                    if (t == null) throw new Exception("AutoCAD is not installed or not registered.");
                    acApp = Activator.CreateInstance(t);
                    acApp.Visible = false;
                }

                acDoc = acApp.Documents.Open(dwgFilePath, true);
                dynamic modelSpace = acDoc.ModelSpace;

                // --- Pass 1: collect text callouts ---
                foreach (dynamic entity in modelSpace)
                {
                    try
                    {
                        string etype = (string)entity.EntityName;
                        bool isText = etype.IndexOf("Text", StringComparison.OrdinalIgnoreCase) >= 0;
                        if (!isText) continue;

                        string text = "";
                        try { text = (string)entity.TextString; } catch { continue; }

                        // Parse callout: optional leading qty + type key (e.g. "3 F62" or "F62")
                        text = text.Trim();
                        var match = System.Text.RegularExpressions.Regex.Match(
                            text, @"^(?:\d+\s+)?([A-Za-z]\w*)$");
                        if (!match.Success) continue;

                        string typeKey = match.Groups[1].Value.ToUpperInvariant();

                        // Get text insertion point
                        double[] ins;
                        try { ins = (double[])entity.InsertionPoint; }
                        catch { continue; }

                        double cx = ToFeet(ins[0]);
                        double cy = ToFeet(ins[1]);
                        callouts.Add((cx, cy, typeKey));
                    }
                    catch { continue; }
                }

                // --- Pass 2: collect closed polylines on TRUSS layer ---
                foreach (dynamic entity in modelSpace)
                {
                    try
                    {
                        string etype = (string)entity.EntityName;
                        bool isPoly = etype.IndexOf("Polyline", StringComparison.OrdinalIgnoreCase) >= 0
                                   || etype.IndexOf("LWPolyline", StringComparison.OrdinalIgnoreCase) >= 0;
                        if (!isPoly) continue;

                        string layer = (string)entity.Layer;
                        if (!string.Equals(layer, "TRUSS", StringComparison.OrdinalIgnoreCase)) continue;

                        double[] coords;
                        try { coords = (double[])entity.Coordinates; }
                        catch { continue; }

                        // Determine coordinate dimensions (2D vs 3D polyline)
                        string etypeLower = etype.ToLowerInvariant();
                        int dim = (etypeLower.Contains("2d") || etypeLower.Contains("3d")) ? 3 : 2;
                        int vertCount = coords.Length / dim;
                        if (vertCount < 2) continue;

                        // Build vertex list in feet
                        var verts = new List<(double x, double y)>();
                        for (int i = 0; i < vertCount; i++)
                            verts.Add((ToFeet(coords[i * dim]), ToFeet(coords[i * dim + 1])));

                        // Find longest segment to define the truss axis
                        int bestI = 0;
                        double bestLen = 0;
                        for (int i = 0; i < verts.Count - 1; i++)
                        {
                            double dx = verts[i + 1].x - verts[i].x;
                            double dy = verts[i + 1].y - verts[i].y;
                            double len = System.Math.Sqrt(dx * dx + dy * dy);
                            if (len > bestLen) { bestLen = len; bestI = i; }
                        }

                        double sx = verts[bestI].x,     sy = verts[bestI].y;
                        double ex = verts[bestI + 1].x, ey = verts[bestI + 1].y;
                        double angle = System.Math.Atan2(ey - sy, ex - sx);

                        // Centroid for callout matching
                        double sumX = 0, sumY = 0;
                        foreach (var v in verts) { sumX += v.x; sumY += v.y; }
                        double centX = sumX / verts.Count;
                        double centY = sumY / verts.Count;

                        // Find nearest callout within 50 feet
                        string detected = null;
                        double bestDist = 50.0;
                        foreach (var (cx, cy, key) in callouts)
                        {
                            double d = System.Math.Sqrt(
                                (cx - centX) * (cx - centX) + (cy - centY) * (cy - centY));
                            if (d < bestDist) { bestDist = d; detected = key; }
                        }

                        instances.Add(new TrussInstance
                        {
                            StartX        = sx,
                            StartY        = sy,
                            EndX          = ex,
                            EndY          = ey,
                            LengthFeet    = bestLen,
                            AngleRadians  = angle,
                            DetectedType  = detected,
                            Layer         = layer
                        });
                    }
                    catch { continue; }
                }
            }
            finally
            {
                if (acDoc != null) { try { acDoc.Close(false); } catch { } Marshal.ReleaseComObject(acDoc); }
                if (acApp != null) Marshal.ReleaseComObject(acApp);
            }

            return instances;
        }

        private static double ToFeet(double value)
        {
            return DwgUnitsAreInches ? value / 12.0 : value;
        }

        /// <summary>
        /// Returns a deduplicated list of layer names in the DWG.
        /// Useful for populating a layer filter dropdown in the UI.
        /// </summary>
        public static List<string> GetLayerNames(string dwgFilePath)
        {
            var layers = new List<string>();
            dynamic acApp = null;
            dynamic acDoc = null;

            try
            {
#if NETFRAMEWORK
                try { acApp = Marshal.GetActiveObject("AutoCAD.Application"); } catch { }
#endif
                if (acApp == null)
                {
                    Type t = Type.GetTypeFromProgID("AutoCAD.Application");
                    acApp = Activator.CreateInstance(t);
                    acApp.Visible = false;
                }

                acDoc = acApp.Documents.Open(dwgFilePath, true);

                foreach (dynamic layer in acDoc.Layers)
                {
                    layers.Add(layer.Name);
                }
            }
            finally
            {
                if (acDoc != null) { try { acDoc.Close(false); } catch { } Marshal.ReleaseComObject(acDoc); }
                if (acApp != null) Marshal.ReleaseComObject(acApp);
            }

            return layers;
        }
    }
}
