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

        //Initial default state
        public static logLevel currentLogLevel = logLevel.Debug;

        public enum logLevel
        {
            Trace,
            Verbose,
            Debug,
            Info,
            Warning,
            Error
        }

        public static void LogToConsole(string message)
        {
            Console.Log(String.Format("{0}: {1}",RouteManagerLoader.getModName(), message));
            LogToDebug("[CONSOLE OUTPUT] " + message);
        }

        public static void LogToDebug(string message, logLevel messageLevel = logLevel.Info)
        {
            if(messageLevel>=currentLogLevel)
                Debug.Log(String.Format("{0} - {1}_V{2} - {3}: {4}", DateTime.Now.ToString("u"), RouteManagerLoader.getModName(), RouteManagerLoader.getModVersion(),messageLevel.ToString().ToUpper().Substring(0,3), message));
        }

        public static void LogToError(string message)
        {
            Debug.LogError(String.Format("{0} - {1}_V{2} - ERR:   {3}", DateTime.Now.ToString("u"), RouteManagerLoader.getModName(), RouteManagerLoader.getModVersion(), message));
        }
    }
}
