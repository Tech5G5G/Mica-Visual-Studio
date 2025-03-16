using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Community.VisualStudio.Toolkit;
using MicaVisualStudio.Options;

namespace MicaVisualStudio
{
    internal partial class OptionsProvider
    {
        [ComVisible(true)]
        public class GeneralOptions : BaseOptionPage<General>
        {
            protected override IWin32Window Window => new GeneralPage();

            public override void SaveSettingsToStorage() { }
        }
    }

    public class General : BaseOptionModel<General>
    {
        [Category("Main window")]
        [DisplayName("Backdrop")]
        [Description("The backdrop to display")]
        [DefaultValue(2)]
        public int Backdrop { get; set; } = 2;

        [Category("Main window")]
        [DisplayName("Corner preference")]
        [Description("The backdrop to display")]
        [DefaultValue(0)]
        public int CornerPreference { get; set; } = 0;

        [Category("Main window")]
        [DisplayName("Theme")]
        [Description("The backdrop to display")]
        [DefaultValue(1)]
        public int Theme { get; set; } = 1;

        [Category("Tool windows")]
        [DisplayName("Enable seperate options for tool windows")]
        [Description("The backdrop to display")]
        [DefaultValue(false)]
        public bool ToolWindows { get; set; } = false;

        [Category("Tool windows")]
        [DisplayName("Backdrop")]
        [Description("The backdrop to display")]
        [DefaultValue(2)]
        public int ToolBackdrop { get; set; } = 2;

        [Category("Tool windows")]
        [DisplayName("Corner preference")]
        [Description("The backdrop to display")]
        [DefaultValue(0)]
        public int ToolCornerPreference { get; set; } = 0;

        [Category("Tool windows")]
        [DisplayName("Theme")]
        [Description("The backdrop to display")]
        [DefaultValue(1)]
        public int ToolTheme { get; set; } = 1;
    }
}
