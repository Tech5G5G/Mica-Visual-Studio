namespace MicaVisualStudio.Services.Resourcing;

public readonly struct ResourceConfiguration(bool transparentIfGray = true, bool translucent = false, byte opacity = 0x38)
{
    public static readonly ResourceConfiguration Default = new(),
                                                 Layered = new(transparentIfGray: false, translucent: true, opacity: 0x7F);

    public readonly bool TransparentIfGray { get; } = transparentIfGray;

    public readonly bool IsTranslucent { get; } = translucent;

    public readonly byte Opacity { get; } = opacity;
}
