namespace System.Windows.Media;

public static class ColorExtensions
{
    extension(Color color)
    {
        public bool IsGray => color.R == color.G && color.G == color.B;
    }
}
