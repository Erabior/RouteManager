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
    public class Logger : MonoBehaviour
    {

        public static logLevel currentLogLevel = logLevel.Information;

        public enum logLevel
        {
            Trace,
            Debug,
            Information,
            Warning,
            Error
        }

        public static void LogToConsole(string message)
        {
            Console.Log(String.Format("{0}: {1}",ModLoader.getModName(), message));
            LogToDebug("[CONSOLE OUTPUT] " + message);
        }

        public static void LogToDebug(string message, logLevel messageLevel = logLevel.Information)
        {
            if(messageLevel>=currentLogLevel)
                Debug.Log(String.Format("{0} - {1}_V{2}: {3}", DateTime.Now.ToString("u"), ModLoader.getModName(), ModLoader.getModVersion(), message));
        }

        public static void LogToError(string message)
        {
            Debug.LogError(String.Format("{0} - {1}_V{2}: {3}", DateTime.Now.ToString("u"), ModLoader.getModName(), ModLoader.getModVersion(), message));
        }
    }
}
