using System.Windows;

namespace Revit_Tab
{
    public partial class SheetInputWindow : Window
    {
        public string SheetNumber { get; private set; }
        public string SheetName { get; private set; }
        public int Quantity { get; private set; }

        public SheetInputWindow()
        {
            InitializeComponent();
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            SheetNumber = txtSheetNumber.Text;
            SheetName = txtSheetName.Text;

            if (int.TryParse(txtQuantity.Text, out int qty) && qty > 0)
            {
                Quantity = qty;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Please enter a valid positive number for Quantity.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtQuantity.Focus();
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

