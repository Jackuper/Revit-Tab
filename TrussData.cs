using System.Collections.Generic;

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
            var config = Newtonsoft.Json.JsonConvert.DeserializeObject<TrussConfig>(json);

            if (config?.TrussTypes == null || config.TrussTypes.Count == 0)
                throw new System.Exception("trusses.json loaded but contains no truss types.");

            return config;
        }
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

    /// <summary>
    /// Generates all 3D members for a single truss from its centerline + specs.
    /// </summary>
    public static class TrussBuilder
    {
        /// <summary>
        /// Builds the full member list for one truss.
        /// The truss stands vertically: bottom chord at BaseElevation,
        /// top chord at BaseElevation + Depth.
        /// Webs run vertically between chords at regular intervals.
        /// </summary>
        public static List<TrussMember> BuildMembers(TrussCenterline cl, TrussSpecs specs)
        {
            var members = new List<TrussMember>();

            double x1 = cl.StartX;
            double y1 = cl.StartY;
            double x2 = cl.EndX;
            double y2 = cl.EndY;

            double zBot = specs.BaseElevationFeet;
            double zTop = specs.BaseElevationFeet + specs.DepthFeet;

            // Bottom chord — runs at base elevation
            members.Add(new TrussMember
            {
                X1 = x1, Y1 = y1, Z1 = zBot,
                X2 = x2, Y2 = y2, Z2 = zBot,
                Type = MemberType.BottomChord
            });

            // Top chord — runs at top elevation
            members.Add(new TrussMember
            {
                X1 = x1, Y1 = y1, Z1 = zTop,
                X2 = x2, Y2 = y2, Z2 = zTop,
                Type = MemberType.TopChord
            });

            // Web members — vertical posts at spacing intervals
            double dx = x2 - x1;
            double dy = y2 - y1;
            double length = System.Math.Sqrt(dx * dx + dy * dy);

            if (specs.WebSpacingFeet > 0 && length > 0)
            {
                double spacing = specs.WebSpacingFeet;
                int webCount = (int)(length / spacing);

                // Always place webs at start and end, plus intermediate
                for (int i = 0; i <= webCount; i++)
                {
                    double t = (i == webCount) ? 1.0 : (i * spacing / length);
                    double wx = x1 + t * dx;
                    double wy = y1 + t * dy;

                    members.Add(new TrussMember
                    {
                        X1 = wx, Y1 = wy, Z1 = zBot,
                        X2 = wx, Y2 = wy, Z2 = zTop,
                        Type = MemberType.Web
                    });
                }
            }

            return members;
        }
    }
}
