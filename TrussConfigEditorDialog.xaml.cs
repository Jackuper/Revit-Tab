using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;
using Newtonsoft.Json;

namespace Revit_Tab
{
    public partial class TrussConfigEditorDialog : Window
    {
        private readonly List<ExtractedTrussType> _rows;

        public TrussConfigEditorDialog(List<ExtractedTrussType> extracted)
        {
            InitializeComponent();
            _rows = extracted;
            GridTrusses.ItemsSource = _rows;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Commit any in-progress cell edits
            GridTrusses.CommitEdit();

            string defaultFamily = TxtDefaultFamily.Text?.Trim() ?? "Wood Timber-Lumber";

            // Apply default family to all rows that still have the placeholder
            foreach (var row in _rows)
                if (string.IsNullOrWhiteSpace(row.FamilyName) ||
                    row.FamilyName == "Wood Timber-Lumber")
                    row.FamilyName = defaultFamily;

            // Load existing trusses.json (or start fresh)
            string dllDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string path   = Path.Combine(dllDir, "trusses.json");

            TrussConfig config;
            if (File.Exists(path))
            {
                try
                {
                    config = JsonConvert.DeserializeObject<TrussConfig>(
                        File.ReadAllText(path)) ?? new TrussConfig();
                }
                catch { config = new TrussConfig(); }
            }
            else
            {
                config = new TrussConfig();
            }

            // Merge: add new types, update existing
            foreach (var row in _rows)
            {
                if (string.IsNullOrWhiteSpace(row.TypeKey)) continue;

                config.TrussTypes[row.TypeKey] = new TrussTypeConfig
                {
                    DepthInches       = row.DepthInches,
                    TopChordFamily    = row.FamilyName,
                    TopChordType      = row.TopChordSize,
                    BottomChordFamily = row.FamilyName,
                    BottomChordType   = row.BotChordSize,
                    WebFamily         = row.FamilyName,
                    WebType           = row.WebSize,
                    WebSpacingInches  = row.WebSpacingInches,
                    WebPattern        = row.WebPattern
                };
            }

            File.WriteAllText(path,
                JsonConvert.SerializeObject(config, Formatting.Indented));

            TxtStatus.Text = $"Saved {_rows.Count} truss type(s).";
            TxtStatus.Foreground = System.Windows.Media.Brushes.Green;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
    }
}
