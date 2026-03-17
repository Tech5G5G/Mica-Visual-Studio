using Microsoft.VisualStudio.PlatformUI;
using MicaVisualStudio.Options;

namespace MicaVisualStudio.UI.ViewModels;

public sealed class OptionPageViewModel(IGeneral general) : ObservableObject
{
    public IGeneral General { get; } = general;
}
