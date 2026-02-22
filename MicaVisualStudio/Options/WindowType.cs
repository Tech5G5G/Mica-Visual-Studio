namespace MicaVisualStudio.Options;

/// <summary>
/// Specifies the type of a <see cref="System.Windows.Window"/>.
/// </summary>
public enum WindowType
{
    /// <summary>
    /// Specifies that the <see cref="System.Windows.Window"/> is the main window of the process.
    /// </summary>
    Main,

    /// <summary>
    /// Specifies that the <see cref="System.Windows.Window"/> is an additional top-level window, meant for tooling and options.
    /// </summary>
    Tool,

    /// <summary>
    /// Specifies that the <see cref="System.Windows.Window"/> is a child window, meant for responding to requests and displaying information.
    /// </summary>
    Dialog
}
