using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Community.VisualStudio.Toolkit;
using MicaVisualStudio.Contracts;

namespace MicaVisualStudio.Services;

public class InfoBarService : IInfoBarService
{
    private readonly Queue<InfoBarModel> _models = [];

    private bool _isAvailable;

    public InfoBarService() =>
        VS.Events.ShellEvents.MainWindowVisibilityChanged += OnMainWindowVisibilityChanged;

    private void OnMainWindowVisibilityChanged(bool args)
    {
        _isAvailable = args;

        while (_models.Count > 0)
        {
            ShowModel(_models.Dequeue());
        }
    }

    private void ShowModel(InfoBarModel model) =>
        VS.InfoBar.CreateAsync(model)
                  .ContinueWith(t => t.Result?.TryShowInfoBarUIAsync().Forget(), TaskScheduler.Default)
                  .Forget();

    public void EnqueueInfoBarModel(InfoBarModel model)
    {
        if (_isAvailable)
        {
            ShowModel(model);
        }
        else
        {
            _models.Enqueue(model);
        }
    }
}
