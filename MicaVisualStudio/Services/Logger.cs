using System;
using System.Reflection;
using System.Diagnostics;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Imaging.Interop;
using MicaVisualStudio.Contracts;

namespace MicaVisualStudio.Services;

public class Logger(IVsActivityLog log, IInfoBarService service) : ILogger
{
    private readonly IVsActivityLog _log = log;
    private readonly IInfoBarService _service = service;

    public void Log(string message)
    {
        Debug.WriteLine(message);
        _log.LogEntry((uint)__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR, Assembly.GetCallingAssembly().GetName().Name, message);
    }

    public void Log(Exception exception)
    {
        Log(FormatException(exception));
    }

    public void Output(string message)
    {
        ExceptionExtensions.Log(exception: null, $"[{Assembly.GetCallingAssembly().GetName().Name}] {message}");
        Log(message);
    }

    public void Output(Exception exception)
    {
        Output(FormatException(exception));
    }

    public void InfoBar(string message, ImageMoniker image)
    {
        _service.EnqueueInfoBarModel(new($"[{Assembly.GetCallingAssembly().GetName().Name}] {message}", image));
        Output(message);
    }

    public void InfoBar(Exception exception, ImageMoniker image)
    {
        InfoBar(FormatException(exception), image);
    }

    private string FormatException(Exception exception)
    {
        return exception + Environment.NewLine + exception.StackTrace;
    }
}
