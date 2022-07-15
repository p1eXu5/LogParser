using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogParser.DesktopClient.ElmishApp.Interfaces;

namespace LogParser.DesktopClient;

public class ErrorMessageQueue : MaterialDesignThemes.Wpf.SnackbarMessageQueue, IErrorMessageQueue
{
    public void EnqueuError(string value)
    {
        Enqueue(value);
    }
}
