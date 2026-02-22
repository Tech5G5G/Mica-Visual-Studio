using System;
using System.Windows.Media;
using Microsoft.VisualStudio.Shell;
using MicaVisualStudio.Options;

namespace MicaVisualStudio.Resourcing;

public class CustomResource(ThemeResourceKey baseResourceKey, Func<Theme, Color, object> factory)
{
    public ThemeResourceKey BaseResourceKey { get; } = baseResourceKey;

    public Func<Theme, Color, object> Factory { get; } = factory;
}
