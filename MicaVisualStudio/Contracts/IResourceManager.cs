using System;
using System.Collections.Generic;
using MicaVisualStudio.Options;
using MicaVisualStudio.Services.Resourcing;

namespace MicaVisualStudio.Contracts;

public interface IResourceManager
{
    IDictionary<string, ResourceConfiguration> Configurations { get; }
    IDictionary<object, CustomResource> CustomResources { get; }

    Theme VisualStudioTheme { get; }
    event EventHandler<Theme> VisualStudioThemeChanged;

    void ConfigureResources();
    void AddCustomResources();
}
