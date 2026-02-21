using System;
using System.Collections.Generic;
using MicaVisualStudio.Options;

namespace MicaVisualStudio.Resourcing;

public interface IResourceManager
{
    IDictionary<string, ResourceConfiguration> Configurations { get; }
    IDictionary<object, CustomResource> CustomResources { get; }

    Theme VisualStudioTheme { get; }
    event EventHandler<Theme> VisualStudioThemeChanged;

    void ConfigureResources();
    void AddCustomResources();
}
