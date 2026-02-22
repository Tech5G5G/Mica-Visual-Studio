using Microsoft.VisualStudio.Shell;

namespace MicaVisualStudio.Contracts;

public interface IInfoBarService
{
    void EnqueueInfoBarModel(InfoBarModel bar);
}
