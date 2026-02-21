using System;
using System.Windows.Media;
using Microsoft.VisualStudio.Shell;
using MicaVisualStudio.Options;

namespace MicaVisualStudio.Resourcing;

public readonly struct CustomResource(ThemeResourceKey baseResourceKey, Func<Theme, Color, object> factory)
{
    public readonly ThemeResourceKey BaseResourceKey { get; } = baseResourceKey;

    public readonly Func<Theme, Color, object> Factory { get; } = factory;
}
