using Microsoft.VisualStudio.PlatformUI;
using MicaVisualStudio.Options;

namespace MicaVisualStudio.ViewModels;

public class OptionPageViewModel(IGeneral general) : ObservableObject
{
    public IGeneral General { get; } = general;
}
