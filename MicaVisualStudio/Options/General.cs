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
            protected override UIElement Child => new Options.Controls.DialogPage();
        }
    }

    /// <summary>
    /// Represents the general options used by Mica Visual Studio.
    /// </summary>
    public class General : BaseOptionModel<General>
    {
        /// <summary>
        /// Gets or sets the <see cref="Options.Theme"/> used for all windows.
        /// </summary>
        /// <remarks>Used in <see cref="WindowHelper.SetDarkMode(IntPtr, bool)"/>.</remarks>
        public int Theme { get; set; } = 0; //Theme.VisualStudio
        /// <summary>
        /// Gets or sets the <see cref="BackdropType"/> used for all windows.
        /// </summary>
        /// <remarks>Used in <see cref="WindowHelper.SetBackdropType(IntPtr, BackdropType)"/>.</remarks>
        public int Backdrop { get; set; } = 2; //BackdropType.Mica
        /// <summary>
        /// Gets or sets the <see cref="Interop.CornerPreference"/> used for all windows.
        /// </summary>
        /// <remarks>Used in <see cref="WindowHelper.SetCornerPreference(IntPtr, Interop.CornerPreference)"/>.</remarks>
        public int CornerPreference { get; set; } = 0; //CornerPreference.Default

        /// <summary>
        /// Gets or sets whether to use separate options for <see cref="WindowType.Tool"/> windows.
        /// </summary>
        public bool ToolWindows { get; set; } = false;
        /// <summary>
        /// Gets or sets the <see cref="Options.Theme"/> used for <see cref="WindowType.Tool"/> windows.
        /// </summary>
        /// <remarks>Used in <see cref="WindowHelper.SetDarkMode(IntPtr, bool)"/>.</remarks>
        public int ToolTheme { get; set; } = 0; //Theme.VisualStudio
        /// <summary>
        /// Gets or sets the <see cref="BackdropType"/> used for <see cref="WindowType.Tool"/> windows.
        /// </summary>
        /// <remarks>Used in <see cref="WindowHelper.SetBackdropType(IntPtr, BackdropType)"/>.</remarks>
        public int ToolBackdrop { get; set; } = 2; //BackdropType.Mica
        /// <summary>
        /// Gets or sets the <see cref="Interop.CornerPreference"/> used for <see cref="WindowType.Tool"/> windows.
        /// </summary>
        /// <remarks>Used in <see cref="WindowHelper.SetCornerPreference(IntPtr, Interop.CornerPreference)"/>.</remarks>
        public int ToolCornerPreference { get; set; } = 0; //CornerPreference.Default

        /// <summary>
        /// Gets or sets whether to use separate options for <see cref="WindowType.Dialog"/> windows.
        /// </summary>
        public bool DialogWindows { get; set; } = false;
        /// <summary>
        /// Gets or sets the <see cref="Options.Theme"/> used for <see cref="WindowType.Dialog"/> windows.
        /// </summary>
        /// <remarks>Used in <see cref="WindowHelper.SetDarkMode(IntPtr, bool)"/>.</remarks>
        public int DialogTheme { get; set; } = 0; //Theme.VisualStudio
        /// <summary>
        /// Gets or sets the <see cref="BackdropType"/> used for <see cref="WindowType.Dialog"/> windows.
        /// </summary>
        /// <remarks>Used in <see cref="WindowHelper.SetBackdropType(IntPtr, BackdropType)"/>.</remarks>
        public int DialogBackdrop { get; set; } = 2; //BackdropType.Mica
        /// <summary>
        /// Gets or sets the <see cref="Interop.CornerPreference"/> used for <see cref="WindowType.Dialog"/> windows.
        /// </summary>
        /// <remarks>Used in <see cref="WindowHelper.SetCornerPreference(IntPtr, Interop.CornerPreference)"/>.</remarks>
        public int DialogCornerPreference { get; set; } = 0; //CornerPreference.Default

        /// <summary>
        /// Gets or sets the <see cref="Options.Theme"/> used by the app.
        /// </summary>
        /// <remarks>Used in <see cref="ThemeHelper.SetAppTheme(Options.Theme)"/>.</remarks>
        public int AppTheme { get; set; } = 0; //Theme.VisualStudio

        /// <summary>
        /// Gets or sets whether or not to invoke <see cref="VsWindowStyler.Listen"/>.
        /// </summary>
        public bool ForceTransparency { get; set; } = true;
        /// <summary>
        /// Gets or sets whether child windows recieve the <see cref="WindowStylesEx.Layered"/> style via <see cref="WindowHelper.MakeLayered(IntPtr)"/>.
        /// </summary>
        /// <remarks>Used by <see cref="VsWindowStyler"/>.</remarks>
        public bool LayeredWindows { get; set; } = true;
    }
}
