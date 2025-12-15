namespace MicaVisualStudio.Options.Controls
{
    /// <summary>
    /// Interaction logic for GeneralPage.xaml
    /// </summary>
    public partial class GeneralPage : UserControl
    {
        public GeneralPage()
        {
            InitializeComponent();

            General general = General.Instance;
            windowOptions.SetOptionModel(general);

            WindowOptions.InitializeComboBox(theme, nameof(General.AppTheme), general);
            WindowOptions.InitializeCheckBox(force, nameof(General.ForceTransparency), general);
            WindowOptions.InitializeCheckBox(layered, nameof(General.LayeredWindows), general);

            Force_Click(this, new());
        }

        private void Force_Click(object sender, RoutedEventArgs args) => layered.IsEnabled = force.IsChecked == true;
    }
}
