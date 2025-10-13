namespace MicaVisualStudio
{
    internal partial class OptionsProvider
    {
        [ComVisible(true)]
        public class GeneralOptions : UIElementDialogPage
        {
            protected override UIElement Child => new GeneralPage();
        }
    }
    
    public class General : BaseOptionModel<General>
    {
        public int Backdrop { get; set; } = 2;
        public int CornerPreference { get; set; } = 0;

        public bool ToolWindows { get; set; } = false;
        public int ToolBackdrop { get; set; } = 2;
        public int ToolCornerPreference { get; set; } = 0;

        public bool DialogWindows { get; set; } = false;
        public int DialogBackdrop { get; set; } = 2;
        public int DialogCornerPreference { get; set; } = 0;
    }
}
