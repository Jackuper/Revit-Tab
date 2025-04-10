using System.ComponentModel;
using System.Windows.Media.Imaging;

namespace Revit_Tab
{
    public class FamilyCatalogItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }

        public string FamilyName { get; set; }
        public string FilePath { get; set; }
        public string Description { get; set; }
        public BitmapImage Thumbnail { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
