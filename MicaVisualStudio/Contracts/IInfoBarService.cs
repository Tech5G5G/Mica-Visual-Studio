using Microsoft.VisualStudio.Shell;

namespace MicaVisualStudio.Contracts;

public interface IInfoBarService
{
    void EnqueueModel(InfoBarModel bar);
}
