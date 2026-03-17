using System.Collections.Generic;
using Autodesk.Revit.DB;
using Newtonsoft.Json;

namespace Revit_Tab
{
    /// <summary>
    /// Profile definition for one truss type, loaded from trusses.json.
    /// </summary>
    public class TrussTypeConfig
    {
        public double DepthInches { get; set; }
        public string TopChordFamily { get; set; }
        public string TopChordType { get; set; }
        public string BottomChordFamily { get; set; }
        public string BottomChordType { get; set; }
        public string WebFamily { get; set; }
        public string WebType { get; set; }
        public double WebSpacingInches { get; set; }
        /// <summary>"vertical" | "diagonal" | "fink"</summary>
        public string WebPattern { get; set; } = "vertical";
    }

    /// <summary>
    /// Root config object — maps truss type names to their profiles.
    /// </summary>
    public class TrussConfig
    {
        public Dictionary<string, TrussTypeConfig> TrussTypes { get; set; }
            = new Dictionary<string, TrussTypeConfig>();

        /// <summary>
        /// Loads trusses.json from the same folder as the executing DLL.
        /// Throws a descriptive exception if the file is missing or malformed.
        /// </summary>
        public static TrussConfig Load()
        {
            string dllDir = System.IO.Path.GetDirectoryName(
                System.Reflection.Assembly.GetExecutingAssembly().Location);
            string path = System.IO.Path.Combine(dllDir, "trusses.json");

            if (!System.IO.File.Exists(path))
                throw new System.Exception(
                    $"trusses.json not found at:\n{path}\n\n" +
                    "Create this file to define your truss type profiles.");

            string json = System.IO.File.ReadAllText(path);
            var config = JsonConvert.DeserializeObject<TrussConfig>(json);

            if (config?.TrussTypes == null || config.TrussTypes.Count == 0)
                throw new System.Exception("trusses.json loaded but contains no truss types.");

            return config;
        }
    }


    /// <summary>
    /// A single truss placement: real-world plan position + detected type.
    /// X/Y are in Revit internal units (feet). AngleRadians = rotation in plan.
    /// </summary>
    public class TrussInstance
    {
        /// <summary>Plan start point (Z = 0, elevation handled separately).</summary>
        public double StartX { get; set; }
        public double StartY { get; set; }
        /// <summary>Plan end point.</summary>
        public double EndX { get; set; }
        public double EndY { get; set; }
        public double LengthFeet { get; set; }
        /// <summary>Rotation angle in plan (radians, measured from X-axis).</summary>
        public double AngleRadians { get; set; }
        /// <summary>Truss type key from trusses.json (e.g. "F62"). Null if not detected.</summary>
        public string DetectedType { get; set; }
        public string Layer { get; set; }
    }

    /// <summary>
    /// Represents a single truss centerline extracted from the DWG plan.
    /// X/Y come from the 2D plan; Z will be assigned from user input.
    /// </summary>
    public class TrussCenterline
    {
        public double StartX { get; set; }  // inches (converted from DWG units)
        public double StartY { get; set; }
        public double EndX   { get; set; }
        public double EndY   { get; set; }
        public string Layer  { get; set; }  // DWG layer name, useful for filtering
    }

    /// <summary>
    /// User-supplied specs from the shop drawing, applied to all trusses
    /// (or per-truss if you extend the UI later).
    /// </summary>
    public class TrussSpecs
    {
        /// <summary>Overall truss depth in feet (e.g. 2.0 for a 24" truss).</summary>
        public double DepthFeet { get; set; }

        /// <summary>Revit structural framing family name for top chord (e.g. "Wood Timber-Lumber").</summary>
        public string TopChordFamily { get; set; }

        /// <summary>Type name for top chord (e.g. "2x4").</summary>
        public string TopChordType { get; set; }

        /// <summary>Revit structural framing family name for bottom chord.</summary>
        public string BottomChordFamily { get; set; }

        /// <summary>Type name for bottom chord.</summary>
        public string BottomChordType { get; set; }

        /// <summary>Revit structural framing family name for web members.</summary>
        public string WebFamily { get; set; }

        /// <summary>Type name for web members.</summary>
        public string WebType { get; set; }

        /// <summary>
        /// Web spacing along the truss length in feet.
        /// Webs will be placed at this interval between the two chords.
        /// </summary>
        public double WebSpacingFeet { get; set; }

        /// <summary>Base elevation (bottom chord elevation) in feet above the level.</summary>
        public double BaseElevationFeet { get; set; }

        /// <summary>Revit Level name to place trusses on (e.g. "Level 1").</summary>
        public string LevelName { get; set; }
    }

    /// <summary>
    /// A resolved member with full 3D start/end points ready for Revit placement.
    /// All values in Revit internal units (decimal feet).
    /// </summary>
    public class TrussMember
    {
        public double X1 { get; set; }
        public double Y1 { get; set; }
        public double Z1 { get; set; }
        public double X2 { get; set; }
        public double Y2 { get; set; }
        public double Z2 { get; set; }
        public MemberType Type { get; set; }
    }

    public enum MemberType
    {
        TopChord,
        BottomChord,
        Web
    }

    public static class TrussBuilder
    {
        /// <summary>
        /// Builds all 3D members for one truss.
        /// baseElevFeet = bottom chord elevation above project origin.
        /// </summary>
        public static List<TrussMember> BuildMembers(
            TrussInstance inst,
            TrussTypeConfig config,
            double baseElevFeet)
        {
            var members = new List<TrussMember>();

            double depthFeet = config.DepthInches / 12.0;
            double spacingFeet = config.WebSpacingInches / 12.0;
            double length = inst.LengthFeet;
            double zBot = baseElevFeet;
            double zTop = baseElevFeet + depthFeet;
            double cos = System.Math.Cos(inst.AngleRadians);
            double sin = System.Math.Sin(inst.AngleRadians);

            // Helper: convert local truss coordinate (t = 0..1 along length) to world XYZ
            XYZ WorldPt(double t, double z)
            {
                double d = t * length;
                return new XYZ(
                    inst.StartX + d * cos,
                    inst.StartY + d * sin,
                    z);
            }

            // Bottom chord
            members.Add(new TrussMember
            {
                X1 = inst.StartX, Y1 = inst.StartY, Z1 = zBot,
                X2 = inst.EndX,   Y2 = inst.EndY,   Z2 = zBot,
                Type = MemberType.BottomChord
            });

            // Top chord
            members.Add(new TrussMember
            {
                X1 = inst.StartX, Y1 = inst.StartY, Z1 = zTop,
                X2 = inst.EndX,   Y2 = inst.EndY,   Z2 = zTop,
                Type = MemberType.TopChord
            });

            // Web members
            switch ((config.WebPattern ?? "vertical").ToLowerInvariant())
            {
                case "diagonal":
                    members.AddRange(BuildDiagonalWebs(length, spacingFeet, zBot, zTop, WorldPt));
                    break;
                case "fink":
                    members.AddRange(BuildFinkWebs(length, zBot, zTop, WorldPt));
                    break;
                default: // "vertical"
                    members.AddRange(BuildVerticalWebs(length, spacingFeet, zBot, zTop, WorldPt));
                    break;
            }

            return members;
        }

        // --- vertical: straight posts at spacing intervals ---
        private static List<TrussMember> BuildVerticalWebs(
            double length, double spacing, double zBot, double zTop,
            System.Func<double, double, XYZ> WorldPt)
        {
            var webs = new List<TrussMember>();
            if (spacing <= 0 || length <= 0) return webs;

            int count = (int)(length / spacing);
            for (int i = 0; i <= count; i++)
            {
                double t = (i == count) ? 1.0 : (i * spacing / length);
                var bot = WorldPt(t, zBot);
                var top = WorldPt(t, zTop);
                webs.Add(Member(bot, top, MemberType.Web));
            }
            return webs;
        }

        // --- diagonal (Warren): alternating diagonals ---
        private static List<TrussMember> BuildDiagonalWebs(
            double length, double spacing, double zBot, double zTop,
            System.Func<double, double, XYZ> WorldPt)
        {
            var webs = new List<TrussMember>();
            if (spacing <= 0 || length <= 0) return webs;

            int panels = (int)System.Math.Max(1, System.Math.Round(length / spacing));

            for (int i = 0; i < panels; i++)
            {
                double t0 = (double)i / panels;
                double t1 = (double)(i + 1) / panels;

                if (i % 2 == 0)
                    webs.Add(Member(WorldPt(t0, zBot), WorldPt(t1, zTop), MemberType.Web));
                else
                    webs.Add(Member(WorldPt(t0, zTop), WorldPt(t1, zBot), MemberType.Web));
            }

            // Verticals at panel points (Warren-with-verticals variant)
            for (int i = 1; i < panels; i++)
            {
                double t = (double)i / panels;
                webs.Add(Member(WorldPt(t, zBot), WorldPt(t, zTop), MemberType.Web));
            }

            return webs;
        }

        // --- fink (W): two ridge nodes + center node, mirrored diagonals ---
        private static List<TrussMember> BuildFinkWebs(
            double length, double zBot, double zTop,
            System.Func<double, double, XYZ> WorldPt)
        {
            var webs = new List<TrussMember>();

            // End verticals
            webs.Add(Member(WorldPt(0.0, zBot), WorldPt(0.0, zTop), MemberType.Web));
            webs.Add(Member(WorldPt(1.0, zBot), WorldPt(1.0, zTop), MemberType.Web));

            // Center vertical
            webs.Add(Member(WorldPt(0.5, zBot), WorldPt(0.5, zTop), MemberType.Web));

            // Quarter-point verticals
            webs.Add(Member(WorldPt(0.25, zBot), WorldPt(0.25, zTop), MemberType.Web));
            webs.Add(Member(WorldPt(0.75, zBot), WorldPt(0.75, zTop), MemberType.Web));

            // W diagonals
            webs.Add(Member(WorldPt(0.0,  zBot), WorldPt(0.25, zTop), MemberType.Web));
            webs.Add(Member(WorldPt(0.5,  zBot), WorldPt(0.25, zTop), MemberType.Web));
            webs.Add(Member(WorldPt(0.5,  zBot), WorldPt(0.75, zTop), MemberType.Web));
            webs.Add(Member(WorldPt(1.0,  zBot), WorldPt(0.75, zTop), MemberType.Web));

            return webs;
        }

        private static TrussMember Member(XYZ a, XYZ b, MemberType t) => new TrussMember
        {
            X1 = a.X, Y1 = a.Y, Z1 = a.Z,
            X2 = b.X, Y2 = b.Y, Z2 = b.Z,
            Type = t
        };
    }

    /// <summary>
    /// Data extracted from one page of a MiTek shop drawing PDF.
    /// Used to populate the review grid before saving to trusses.json.
    /// </summary>
    public class ExtractedTrussType
    {
        public string TypeKey          { get; set; }
        public double DepthInches      { get; set; }
        public string TopChordSize     { get; set; }
        public string BotChordSize     { get; set; }
        public string WebSize          { get; set; }
        public double WebSpacingInches { get; set; } = 24;
        public string WebPattern       { get; set; } = "vertical";
        /// <summary>Filled in from the dialog's Default Family field.</summary>
        public string FamilyName       { get; set; } = "Wood Timber-Lumber";
    }
}
