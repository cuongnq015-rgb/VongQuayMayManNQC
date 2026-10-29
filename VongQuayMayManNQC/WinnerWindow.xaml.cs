using System.Windows;

namespace VongQuayMayManNQC
{
    public partial class WinnerWindow : Window
    {
        public WinnerWindow(string winnerName, string prizeName)
        {
            InitializeComponent();
            txtWinner.Text = winnerName;
            txtPrize.Text = prizeName.ToUpper();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}