using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using Autodesk.Revit.DB;

namespace Revit_Tab
{
    public partial class TrussImportDialog : Window
    {
        private readonly Document _doc;
        private string _dwgPath;
        private TrussConfig _config;
        private List<TrussInstance> _parsedInstances;

        public List<Level> AvailableLevels { get; set; } = new List<Level>();

        public TrussImportDialog(Document doc, TrussConfig config)
        {
            InitializeComponent();
            _doc    = doc;
            _config = config;
        }

        public void Populate()
        {
            // Levels
            CboLevel.Items.Clear();
            foreach (var lv in AvailableLevels)
                CboLevel.Items.Add(lv.Name);
            if (CboLevel.Items.Count > 0)
                CboLevel.SelectedIndex = 0;

            // Truss types from config
            CboTrussType.Items.Clear();
            CboTrussType.Items.Add("(auto-detect)");
            foreach (var key in _config.TrussTypes.Keys.OrderBy(k => k))
                CboTrussType.Items.Add(key);
            CboTrussType.SelectedIndex = 0;
        }

        private void BrowseDwg_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title  = "Select Truss Layout DWG",
                Filter = "DWG Files (*.dwg)|*.dwg|All Files (*.*)|*.*"
            };
            if (dlg.ShowDialog() == true)
            {
                _dwgPath = dlg.FileName;
                TxtDwgPath.Text = _dwgPath;
                TxtDwgPath.Foreground = System.Windows.Media.Brushes.Black;
                TxtStatus.Text = "File selected. Click 'Parse DWG' to read truss locations.";
                BtnPlace.IsEnabled = false;
                _parsedInstances = null;
            }
        }

        private void ParseDwg_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_dwgPath))
            {
                MessageBox.Show("Select a DWG file first.", "No File",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            TxtStatus.Text = "Parsing DWG...";
            BtnPlace.IsEnabled = false;
            _parsedInstances = null;

            try
            {
                DwgParser.DwgUnitsAreInches = RbInches.IsChecked == true;
                _parsedInstances = DwgParser.ParseTrussInstances(_dwgPath);

                if (_parsedInstances.Count == 0)
                {
                    TxtStatus.Text = "No closed polylines found on the TRUSS layer.";
                    return;
                }

                // Summarize detected types
                var typeCounts = _parsedInstances
                    .GroupBy(i => i.DetectedType ?? "(unknown)")
                    .OrderBy(g => g.Key)
                    .Select(g => $"{g.Count()}x {g.Key}");

                TxtStatus.Text =
                    $"Found {_parsedInstances.Count} trusses: {string.Join(", ", typeCounts)}.\n" +
                    "Review above then click 'Place Trusses'.";

                BtnPlace.IsEnabled = true;
            }
            catch (Exception ex)
            {
                TxtStatus.Text = $"Parse error: {ex.Message}";
                MessageBox.Show(ex.ToString(), "Parse Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PlaceTrusses_Click(object sender, RoutedEventArgs e)
        {
            if (_parsedInstances == null || _parsedInstances.Count == 0)
            {
                MessageBox.Show("Parse the DWG first.", "No Data",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!double.TryParse(TxtBaseElev.Text, out double baseIn))
            {
                MessageBox.Show("Enter a valid base elevation in inches.", "Invalid Input");
                return;
            }

            // Resolve override type (null = use auto-detected per instance)
            string overrideType = CboTrussType.SelectedIndex == 0
                ? null
                : CboTrussType.SelectedItem as string;

            TxtStatus.Text = "Placing in Revit...";

            try
            {
                int placed = TrussPlacementHelper.PlaceAllTrusses(
                    _doc,
                    _parsedInstances,
                    _config,
                    overrideType,
                    baseIn / 12.0,
                    CboLevel.Text);

                TxtStatus.Text = $"Done! Placed {placed} framing elements " +
                                 $"for {_parsedInstances.Count} trusses.";
                BtnPlace.IsEnabled = false;
            }
            catch (Exception ex)
            {
                TxtStatus.Text = $"Error: {ex.Message}";
                MessageBox.Show(ex.ToString(), "Placement Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
    }
}
