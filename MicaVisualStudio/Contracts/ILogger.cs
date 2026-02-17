using Microsoft.VisualStudio.Imaging.Interop;
using System;

namespace MicaVisualStudio.Contracts;

public interface ILogger
{
    void Log(string message);
    void Log(Exception exception);

    void Output(string message);
    void Output(Exception exception);

    void InfoBar(string message, ImageMoniker image);
    void InfoBar(Exception exception, ImageMoniker image);
}
