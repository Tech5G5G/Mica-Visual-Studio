namespace MicaVisualStudio.Options
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
        }
    }
}
