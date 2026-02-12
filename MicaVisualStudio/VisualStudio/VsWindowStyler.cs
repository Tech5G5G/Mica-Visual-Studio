using System;
using System.Windows;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Shell.Interop;
using Community.VisualStudio.Toolkit;

namespace MicaVisualStudio.VisualStudio;

/// <summary>
/// Represents an observer that listens and styles Visual Studio windows.
/// </summary>
// This code is bad, but it works, so...
public sealed partial class VsWindowStyler : IVsWindowFrameEvents, IDisposable
{
    /// <summary>
    /// Gets the singleton instance of <see cref="VsWindowStyler"/>.
    /// </summary>
    public static VsWindowStyler Instance => field ??= new();

    #region Shells

    private readonly IVsUIShell shell = VS.GetRequiredService<SVsUIShell, IVsUIShell>();
    private readonly IVsUIShell5 shell5 = VS.GetRequiredService<SVsUIShell, IVsUIShell5>();
    private readonly IVsUIShell7 shell7 = VS.GetRequiredService<SVsUIShell, IVsUIShell7>();

    #endregion

    private VsWindowStyler() { }

    /// <summary>
    /// Tells the <see cref="VsWindowStyler"/> to start listening to and styling Visual Studio windows.
    /// </summary>
    public void Listen()
    {
        if (disposed)
            return;

        Compile(); //Compile hooks and functions
        AddBrushes(); //Add layered brushes to resources
        SubscribeToEvents(); //Subscribe to window frame, text view, and dock target creation events

        ApplyToAllWindows();
        ApplyToAllWindowPanesAsync().Forget();
    }

    #region IsTrackedProperty

    /// <summary>
    /// Gets the value of the <see cref="IsTrackedProperty"/> attached property from a given <see cref="FrameworkElement"/>.
    /// </summary>
    /// <param name="target">The <see cref="FrameworkElement"/> from which to read the property value.</param>
    /// <returns>The value of the <see cref="IsTrackedProperty"/> attached property.</returns>
    public static bool GetIsTracked(FrameworkElement target) =>
        (bool)target.GetValue(IsTrackedProperty);

    /// <summary>
    /// Sets the value of the <see cref="IsTrackedProperty"/> attached property from a given <see cref="FrameworkElement"/>.
    /// </summary>
    /// <param name="target">The <see cref="FrameworkElement"/> on which to set the attached property.</param>
    /// <param name="value">The property value to set.</param>
    public static void SetIsTracked(FrameworkElement target, bool value) =>
        target.SetValue(IsTrackedProperty, value);

    /// <summary>
    /// Identifies the MicaVisualStudio.VisualStudio.VsWindowStyler.IsTracked dependency property.
    /// </summary>
    public static readonly DependencyProperty IsTrackedProperty =
        DependencyProperty.RegisterAttached("IsTracked", typeof(bool), typeof(VsWindowStyler), new(defaultValue: false));

    #endregion

    #region Dispose

    private bool disposed;

    /// <summary>
    /// Disposes the singleton instance of <see cref="VsWindowStyler"/>.
    /// </summary>
    public void Dispose()
    {
        if (disposed)
            return;

        RevertHooks();
        UnsubscribeFromEvents();

        disposed = true;
    }

    #endregion
}
