namespace MicaVisualStudio.Options
{
    /// <summary>
    /// Interaction logic for DialogPage.xaml
    /// </summary>
    public partial class DialogPage : UserControl
    {
        public DialogPage()
        {
            InitializeComponent();
            windowOptions.SetOptionModel(General.Instance);
        }
    }
}
