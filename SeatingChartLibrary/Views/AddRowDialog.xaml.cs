using System.Windows;

namespace SeatingChartLibrary.Views
{
    public partial class AddRowDialog : Window
    {
        public string RowName { get; private set; }

        public AddRowDialog()
        {
            InitializeComponent();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            RowName = RowNameTextBox.Text;
            DialogResult = true;
        }
    }
}