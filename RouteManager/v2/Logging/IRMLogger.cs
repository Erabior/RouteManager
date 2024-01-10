using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UI.Console;
using System.ComponentModel;

namespace RouteManager.v2.Logging
{
    [Serializable]
    public enum LogLevel
    {
        Trace,
        Verbose,
        Debug,
        Info,
        Warning,
        Error
    }

    public interface IRMLogger 
    {

        //Initial default state
        public LogLevel currentLogLevel { get; set; }

        public void LogToConsole(string message);

        public void LogToDebug(string message, LogLevel messageLevel = LogLevel.Info);

        public void LogToError(string message);
    }
}
