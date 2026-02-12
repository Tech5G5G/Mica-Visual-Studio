using System.Windows.Controls;

namespace MicaVisualStudio.Options.Controls
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
