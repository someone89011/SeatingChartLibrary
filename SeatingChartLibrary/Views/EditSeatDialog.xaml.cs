using System.Collections.ObjectModel;
using System.Windows;
using SeatingChartLibrary.ViewModels;

namespace SeatingChartLibrary.Views
{
    public partial class EditSeatDialog : Window
    {
        public string RowName { get; private set; }
        public string Number { get; private set; }
        public string PersonName { get; private set; }
        public string DeviceType { get; private set; }
        public string DeviceNumber { get; private set; }

        public EditSeatDialog(Seat seat, ObservableCollection<string> rowNames)
        {
            InitializeComponent();
            RowNameComboBox.ItemsSource = rowNames;
            RowNameComboBox.Text = seat.RowName;
            NumberTextBox.Text = seat.Number;
            PersonNameTextBox.Text = seat.Person?.Name;
            DeviceTypeTextBox.Text = seat.Person?.Device?.Type;
            DeviceNumberTextBox.Text = seat.Person?.Device?.Number;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            RowName = RowNameComboBox.Text;
            Number = NumberTextBox.Text;
            PersonName = PersonNameTextBox.Text;
            DeviceType = DeviceTypeTextBox.Text;
            DeviceNumber = DeviceNumberTextBox.Text;
            DialogResult = true;
        }
    }
}