using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;

namespace MicaVisualStudio.Composition
{
    public class MicaController(Compositor compositor) : IDisposable
    {
        public Color TintColor
        {
            get => tintColor;
            set
            {

            }
        }
        Color tintColor = Colors.Teal;

        public float TintOpacity
        {
            get => tintOpacity;
            set
            {

            }
        }
        float tintOpacity = 0.05f;

        public float LuminosityOpacity
        {
            get => luminosityOpacity;
            set
            {

            }
        }
        float luminosityOpacity = 0.15f;

        public Color FallbackColor
        {
            get => fallbackColor;
            set
            {

            }
        }
        Color fallbackColor = Colors.Teal;

        public Compositor Compositor { get; } = compositor;

        Visual visual;
        CompositionBrush brush;

        DesktopWindowTarget target;
        ICompositionSupportsSystemBackdrop composition;

        [DllImport("User32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        public void SetTarget(nint hWnd)
        {
            Compositor.CreateDesktopWindowTarget(hWnd, true, out target);
            target.Root = visual;

            visual = Compositor.CreateContainerVisual();
            brush = Compositor.TryCreateBlurredWallpaperBackdropBrush/*CreateHostBackdropBrush*/();

            //Create Mica brush
            //Or create Acrylic (using AcrylicBrush source code on WinUI 2 branch)

            composition = (ICompositionSupportsSystemBackdrop)(object)target;
            composition.SystemBackdrop = brush;
        }

        private void Crossfade()
        {

        }

        public void Dispose()
        {
            target.Dispose();
            visual.Dispose();
            brush.Dispose();
        }
    }
}
