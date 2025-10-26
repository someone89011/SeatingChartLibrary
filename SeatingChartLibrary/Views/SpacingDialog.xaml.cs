using System.Windows;

namespace SeatingChartLibrary.Views
{
    public partial class SpacingDialog : Window
    {
        public double Spacing { get; private set; }

        public SpacingDialog()
        {
            InitializeComponent();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(SpacingTextBox.Text, out double spacing) && spacing > 0)
            {
                Spacing = spacing;
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("請輸入有效的正數間隔值。");
            }
        }
    }
}