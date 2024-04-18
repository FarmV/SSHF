using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;

namespace FVH.SSHF.Windows
{
    public partial class UserControl1 : Grid, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private const string _defaultActiveWindow = "1";
        private string _numberActiveWindow;

        public UserControl1()
        {
            InitializeComponent();
            _numberActiveWindow = _defaultActiveWindow;
        }
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        public string NumberActiveWindow
        {
            get => _numberActiveWindow;
            set
            {
                if(value  == _numberActiveWindow) return;
                _numberActiveWindow = value;
                NotifyPropertyChanged();
            }
        }
    }
}
