using System;
using MicaVisualStudio.Options;

namespace MicaVisualStudio.Contracts;

public interface IThemeService
{
    Theme SystemTheme { get; }

    event EventHandler<Theme> SystemThemeChanged;
}
