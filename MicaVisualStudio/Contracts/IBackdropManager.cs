using MicaVisualStudio.Interop;

namespace MicaVisualStudio.Contracts;

public interface IBackdropManager
{
    BackdropType Backdrop { get; set; }
}
