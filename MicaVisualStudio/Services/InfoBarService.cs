using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Community.VisualStudio.Toolkit;
using MicaVisualStudio.Contracts;

namespace MicaVisualStudio.Services;

public sealed class InfoBarService : IInfoBarService, IDisposable
{
    private readonly Queue<InfoBarModel> _models = [];

    private bool _isAvailable = Application.Current.MainWindow?.Visibility == Visibility.Visible;

    public InfoBarService()
    {
        VS.Events.ShellEvents.MainWindowVisibilityChanged += OnMainWindowVisibilityChanged;
    }

    private void OnMainWindowVisibilityChanged(bool e)
    {
        if (_isAvailable = e)
        {
            while (_models.Count > 0)
            {
                ShowModel(_models.Dequeue());
            }
        }
    }

    private void ShowModel(InfoBarModel model)
    {
        VS.InfoBar.CreateAsync(model)
                  .ContinueWith(t => t.Result?.TryShowInfoBarUIAsync().Forget(), TaskScheduler.Default)
                  .Forget();
    }

    public void EnqueueModel(InfoBarModel model)
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

    #region Dispose

    private bool _disposed;

    void IDisposable.Dispose()
    {
        if (!_disposed)
        {
            VS.Events.ShellEvents.MainWindowVisibilityChanged -= OnMainWindowVisibilityChanged;
            _disposed = true;
        }
    }

    #endregion
}
