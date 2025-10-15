namespace MicaVisualStudio
{
    internal partial class OptionsProvider
    {
        [ComVisible(true)]
        public class GeneralOptions : UIElementDialogPage
        {
            protected override UIElement Child => new GeneralPage();
        }

        [ComVisible(true)]
        public class ToolOptions : UIElementDialogPage
        {
            protected override UIElement Child => new ToolPage();
        }

        [ComVisible(true)]
        public class DialogOptions : UIElementDialogPage
        {
            protected override UIElement Child => new Options.DialogPage();
        }
    }

    public class General : BaseOptionModel<General>
    {
        public int Theme { get; set; } = 0; //Theme.VisualStudio
        public int Backdrop { get; set; } = 2; //BackdropType.Mica
        public int CornerPreference { get; set; } = 0; //CornerPreference.Default

        public bool ToolWindows { get; set; } = false;
        public int ToolTheme { get; set; } = 0; //Theme.VisualStudio
        public int ToolBackdrop { get; set; } = 2; //BackdropType.Mica
        public int ToolCornerPreference { get; set; } = 0; //CornerPreference.Default

        public bool DialogWindows { get; set; } = false;
        public int DialogTheme { get; set; } = 0; //Theme.VisualStudio
        public int DialogBackdrop { get; set; } = 2; //BackdropType.Mica
        public int DialogCornerPreference { get; set; } = 0; //CornerPreference.Default
    }
}
