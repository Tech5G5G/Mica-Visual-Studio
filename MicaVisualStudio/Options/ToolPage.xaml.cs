namespace MicaVisualStudio.Options
{
    /// <summary>
    /// Interaction logic for ToolPage.xaml
    /// </summary>
    public partial class ToolPage : UserControl
    {
        public ToolPage()
        {
            InitializeComponent();
            windowOptions.SetOptionModel(General.Instance);
        }
    }
}
