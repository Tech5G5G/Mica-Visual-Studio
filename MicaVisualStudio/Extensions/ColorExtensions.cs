namespace System.Windows.Media;

/// <summary>
/// Contains extensions for dealing with <see cref="Color"/>s.
/// </summary>
public static class ColorExtensions
{
    extension(Color color)
    {
        /// <summary>
        /// Gets whether the <see cref="Color"/> is a shade of gray.
        /// </summary>
        public bool IsGray => color.R == color.G && color.G == color.B;
    }
}
