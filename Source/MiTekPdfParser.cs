using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace Revit_Tab
{
    /// <summary>
    /// Extracts truss specs from MiTek-generated shop drawing PDFs.
    /// One page = one truss type. Returns one ExtractedTrussType per page.
    /// </summary>
    public static class MiTekPdfParser
    {
        /// <summary>
        /// Parses all pages of a MiTek PDF and returns extracted truss data.
        /// Pages that cannot be parsed are skipped (no exception thrown).
        /// </summary>
        public static List<ExtractedTrussType> ParsePdf(string pdfPath)
        {
            var results = new List<ExtractedTrussType>();

            using (var doc = PdfDocument.Open(pdfPath))
            {
                foreach (var page in doc.GetPages())
                {
                    try
                    {
                        var extracted = ParsePage(page);
                        if (extracted != null)
                            results.Add(extracted);
                    }
                    catch { /* skip unparseable pages */ }
                }
            }

            return results;
        }

        private static ExtractedTrussType ParsePage(Page page)
        {
            // Extract all text from the page as one string (words joined by spaces)
            var words = page.GetWords().Select(w => w.Text).ToList();
            string fullText = string.Join(" ", words);

            string typeKey  = ExtractTrussType(fullText);
            string topChord = ExtractLumberSize(fullText, "TOP CHORD");
            string botChord = ExtractLumberSize(fullText, "BOT CHORD");
            string webs     = ExtractLumberSize(fullText, "WEBS");
            double spacing  = ExtractSpacingInches(fullText);
            double depth    = ExtractDepthInches(fullText);

            // Skip page if we couldn't find a truss type
            if (string.IsNullOrWhiteSpace(typeKey)) return null;

            return new ExtractedTrussType
            {
                TypeKey          = typeKey.ToUpperInvariant(),
                DepthInches      = depth,
                TopChordSize     = NormalizeLumberSize(topChord),
                BotChordSize     = NormalizeLumberSize(botChord),
                WebSize          = NormalizeLumberSize(webs),
                WebSpacingInches = spacing,
                WebPattern       = "vertical"
            };
        }

        /// <summary>
        /// Finds the truss type label. MiTek puts "Truss Type" as a column header
        /// with the value in the next cell. We look for text after "Truss Type".
        /// </summary>
        private static string ExtractTrussType(string text)
        {
            var match = Regex.Match(text,
                @"Truss\s+Type\s+([A-Za-z0-9\-]+)",
                RegexOptions.IgnoreCase);
            if (match.Success)
                return match.Groups[1].Value.Trim();

            return null;
        }

        /// <summary>
        /// Extracts lumber size for a given member type from the LUMBER section.
        /// e.g. "TOP CHORD 2x4 SP No.2" → "2x4"
        /// </summary>
        private static string ExtractLumberSize(string text, string memberLabel)
        {
            string escaped = Regex.Escape(memberLabel);
            var match = Regex.Match(text,
                escaped + @"\s+([\dx]+)\s+",
                RegexOptions.IgnoreCase);
            if (match.Success)
                return match.Groups[1].Value.Trim();

            return "2x4"; // safe default
        }

        /// <summary>
        /// Extracts truss spacing in inches from the SPACING section.
        /// MiTek format: "2-0-0" = 2ft 0in 0/16in = 24"
        /// </summary>
        private static double ExtractSpacingInches(string text)
        {
            var match = Regex.Match(text,
                @"SPACING[-\s]+([\d]+-[\d]+-[\d]+)",
                RegexOptions.IgnoreCase);
            if (match.Success)
                return MiTekDimToInches(match.Groups[1].Value);

            return 24.0; // default 24" OC
        }

        /// <summary>
        /// Extracts overall truss depth in inches.
        /// Strategy: find all feet-inches-sixteenths values on the page,
        /// return the largest (which is the overall height dimension).
        /// </summary>
        private static double ExtractDepthInches(string text)
        {
            var matches = Regex.Matches(text, @"\b(\d+)-(\d+)-(\d+)\b");
            double max = 0;

            foreach (Match m in matches)
            {
                double val = MiTekDimToInches(m.Value);
                if (val > max) max = val;
            }

            return max > 0 ? Math.Round(max, 4) : 60.0; // fallback 60"
        }

        /// <summary>
        /// Converts MiTek feet-inches-sixteenths string to decimal inches.
        /// e.g. "4-5-11" → 4*12 + 5 + 11/16 = 53.6875
        /// </summary>
        public static double MiTekDimToInches(string dim)
        {
            var parts = dim.Split('-');
            if (parts.Length != 3) return 0;

            if (!double.TryParse(parts[0], out double feet))       return 0;
            if (!double.TryParse(parts[1], out double inches))     return 0;
            if (!double.TryParse(parts[2], out double sixteenths)) return 0;

            return feet * 12.0 + inches + sixteenths / 16.0;
        }

        /// <summary>
        /// Normalizes lumber sizes to simple format: "2x4", "2x3", etc.
        /// Strips species/grade info (e.g. "2x4 SP No.2" → "2x4").
        /// </summary>
        private static string NormalizeLumberSize(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "2x4";
            var match = Regex.Match(raw.Trim(), @"(\dx\d+)");
            return match.Success ? match.Groups[1].Value : raw.Trim();
        }
    }
}
