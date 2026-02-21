using System;
using MicaVisualStudio.Enums;

namespace MicaVisualStudio.Contracts;

public interface IThemeService
{
    Theme SystemTheme { get; }

    event EventHandler<Theme> SystemThemeChanged;
}
