using System;
using UnityEngine;
using RouteManager.v2.Logging;

namespace RouteManager.BepInEx.Util
{
    public class BepInExLogger : IRMLogger
    {

        //Initial default state
        private LogLevel level = LogLevel.Debug;
        
        public LogLevel currentLogLevel
        {
            get
            {
                return level;
            }

            set
            {
                level = value;
            }
        }

        public void LogToConsole(string message)
        {
            Console.Log(String.Format("{0}: {1}",RouteManager.getModName(), message));
            LogToDebug("[CONSOLE OUTPUT] " + message);
        }

        public void LogToDebug(string message, LogLevel messageLevel = LogLevel.Info)
        {
            if(messageLevel>=currentLogLevel)
                Debug.Log(String.Format("{0} - {1}_V{2} - {3}: {4}", DateTime.Now.ToString("u"), RouteManager.getModName(), RouteManager.getModVersion(),messageLevel.ToString().ToUpper().Substring(0,3), message));
        }

        public void LogToError(string message)
        {
            Debug.LogError(String.Format("{0} - {1}_V{2} - ERR:   {3}", DateTime.Now.ToString("u"), RouteManager.getModName(), RouteManager.getModVersion(), message));
        }
    }
}
