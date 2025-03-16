using System;
using System.Windows.Forms;

namespace MicaVisualStudio.Options
{
    public partial class GeneralPage : UserControl
    {
        public GeneralPage()
        {
            InitializeComponent();

            var general = General.Instance;
            backdrop.SelectedIndex = general.Backdrop;
            theme.SelectedIndex = general.Theme;
            cornerPreference.SelectedIndex = general.CornerPreference;
            toolWindows.Checked = general.ToolWindows;
        }

        private void Backdrop_SelectionChanged(object sender, EventArgs e)
        {
            General.Instance.Backdrop = backdrop.SelectedIndex;
            General.Instance.Save();
        }

        private void Theme_SelectionChanged(object sender, EventArgs e)
        {
            General.Instance.Theme = theme.SelectedIndex;
            General.Instance.Save();
        }

        private void CornerPreference_SelectionChanged(object sender, EventArgs e)
        {
            General.Instance.CornerPreference = cornerPreference.SelectedIndex;
            General.Instance.Save();
        }

        private void ToolWindows_Checked(object sender, EventArgs e)
        {
            General.Instance.ToolWindows = toolWindows.Checked;
            General.Instance.Save();
        }
    }
}
