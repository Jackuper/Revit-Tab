using System.Collections.ObjectModel;
using System.Windows;

namespace Revit_Tab
{
    public partial class FamilyCatalogWindow : Window
    {
        public ObservableCollection<FamilyCatalogItem> CatalogItems { get; set; }

        public FamilyCatalogWindow(ObservableCollection<FamilyCatalogItem> items)
        {
            InitializeComponent();
            CatalogItems = items;
            lvFamilies.ItemsSource = CatalogItems;
        }

        // When the user clicks "Import Selected Families"
        private void btnImport_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
