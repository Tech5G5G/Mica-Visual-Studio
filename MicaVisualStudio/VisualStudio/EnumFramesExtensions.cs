namespace MicaVisualStudio.VisualStudio;

public static class EnumFramesExtensions
{
    public static IEnumerable<IVsWindowFrame> ToEnumerable(this IEnumWindowFrames @enum)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        const uint Amount = 1;
        var frames = new IVsWindowFrame[Amount];

        while (ErrorHandler.Succeeded(@enum.Next(Amount, frames, out uint fetched)) && fetched == Amount)
            yield return frames[0];
    }
}
