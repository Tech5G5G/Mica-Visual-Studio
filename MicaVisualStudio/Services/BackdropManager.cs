using MicaVisualStudio.Interop;
using MicaVisualStudio.Contracts;

namespace MicaVisualStudio.Services;

public class BackdropManager : IBackdropManager
{
    public BackdropType Backdrop
    {
        get => _backdrop;
        set
        {
            var general = General.Instance;
            general.Backdrop = (int)(_backdrop = value);
            general.Save();
        }
    }

    private BackdropType _backdrop = (BackdropType)General.Instance.Backdrop;
}
