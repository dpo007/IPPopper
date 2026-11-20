using System.Windows;

namespace IPPopper
{
    public partial class QuitConfirmationWindow : Window
    {
        public QuitConfirmationWindow()
        {
            InitializeComponent();
        }

        private void Yes_Click(object sender, RoutedEventArgs e) => DialogResult = true;

        private void No_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
