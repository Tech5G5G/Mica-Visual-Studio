namespace System.Windows.Media;

/// <summary>
/// Contains extensions for dealing with <see cref="Color"/>s.
/// </summary>
public static class ColorExtensions
{
    /// <summary>
    /// Determines whether the specified <paramref name="color"/> is a shade of gray.
    /// </summary>
    /// <param name="color">A <see cref="Color"/> to check the grayness of.</param>
    /// <returns>
    /// Whether or not the specified <paramref name="color"/>'s
    /// <see cref="Color.R"/>, <see cref="Color.G"/>, and <see cref="Color.B"/> channels are equal.
    /// </returns>
    public static bool IsGray(this Color color) =>
        color.R == color.G && color.G == color.B;
}
