using System.Collections.Generic;

namespace Microsoft.VisualStudio.Shell.Interop;

/// <summary>
/// Contains extensions for dealing with <see cref="IEnumWindowFrames"/>.
/// </summary>
public static class EnumFramesExtensions
{
    /// <summary>
    /// Enumerates the specified <see cref="IEnumWindowFrames"/> into an <see cref="IEnumerable{T}"/> of <see cref="IVsWindowFrame"/>.
    /// </summary>
    /// <param name="enum">An <see cref="IEnumWindowFrames"/> to enumerate.</param>
    /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="IVsWindowFrame"/> of the frames from the specified <see cref="IEnumWindowFrames"/>.</returns>
    public static IEnumerable<IVsWindowFrame> ToEnumerable(this IEnumWindowFrames @enum)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        const uint Amount = 1;
        var frames = new IVsWindowFrame[Amount];

        while (ErrorHandler.Succeeded(@enum.Next(Amount, frames, out uint fetched)) && fetched == Amount)
            yield return frames[0];
    }
}
