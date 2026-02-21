namespace MicaVisualStudio.Resourcing;

/// <summary>
/// Represents the configuration for a <see cref="System.Windows.Media.Color"/> or
/// <see cref="System.Windows.Media.Brush"/> used by Visual Studio.
/// </summary>
/// <param name="transparentIfGray">
/// Whether to make the <see cref="System.Windows.Media.Color"/> or
/// <see cref="System.Windows.Media.Brush"/> completely transparent if it is gray.
/// </param>
/// <param name="translucent">Whether to allow <paramref name="opacity"/>.</param>
/// <param name="opacity">
/// The opacity to make the
/// <see cref="System.Windows.Media.Color"/> or
/// <see cref="System.Windows.Media.Brush"/>.
/// </param>
public readonly struct ResourceConfiguration(bool transparentIfGray = true, bool translucent = false, byte opacity = 0x38)
{
    /// <summary>
    /// Represents the default <see cref="ResourceConfiguration"/> for a
    /// <see cref="System.Windows.Media.Color"/> or
    /// <see cref="System.Windows.Media.Brush"/>.
    /// </summary>
    /// <remarks>Equivalent to a <see cref="ResourceConfiguration"/> initialized to its default value; that is, with no parameters modified.</remarks>
    public static readonly ResourceConfiguration Default = new();
    /// <summary>
    /// Represents the <see cref="ResourceConfiguration"/> for a layered
    /// <see cref="System.Windows.Media.Color"/> or
    /// <see cref="System.Windows.Media.Brush"/>.
    /// </summary>
    /// <remarks>
    /// Equivalent to a <see cref="ResourceConfiguration"/> with:
    /// <list type="bullet">
    /// <item>
    /// <see cref="TransparentIfGray"/> set to <see langword="false"/>
    /// </item>
    /// <item>
    /// <see cref="IsTranslucent"/> set to <see langword="true"/>.
    /// </item>
    /// <item>
    /// <see cref="Opacity"/> set to <c>0x7F</c>.
    /// </item>
    /// </list>
    /// </remarks>
    public static readonly ResourceConfiguration Layered = new(transparentIfGray: false, translucent: true, opacity: 0x7F);

    public readonly bool TransparentIfGray { get; } = transparentIfGray;

    public readonly bool IsTranslucent { get; } = translucent;

    public readonly byte Opacity { get; } = opacity;
}
