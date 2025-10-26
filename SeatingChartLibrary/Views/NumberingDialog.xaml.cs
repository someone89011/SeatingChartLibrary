using System.Windows;
using System.Windows.Controls;

namespace SeatingChartLibrary.Views
{
    public partial class NumberingDialog : Window
    {
        public int StartNumber { get; private set; }
        public bool IsIncrement { get; private set; }

        public NumberingDialog()
        {
            InitializeComponent();
            NumberingModeComboBox.SelectedIndex = 0; // 預設遞增
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(StartNumberTextBox.Text, out int startNumber))
            {
                StartNumber = startNumber;
                IsIncrement = NumberingModeComboBox.SelectedIndex == 0;
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("請輸入有效的整數起始編號。");
            }
        }
    }
}