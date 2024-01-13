using System;
using UnityEngine;
using RouteManager.v2.Logging;
using static UnityModManagerNet.UnityModManager;
using dnlib.DotNet;
using Game;


namespace RouteManager.UMM.Util
{
    public class UMMLogger : IRMLogger
    {
        private LogLevel level = LogLevel.Debug;

        public LogLevel currentLogLevel
        {
            get {return level;}
            set {level = value;}
        }

        public void LogToConsole(string message)
        {
            string messagePrefix = RouteManager.getModName();

            if (RMUMM.settingsData.showTimestamp)
                messagePrefix = String.Format("{0} | {1}", TimeWeather.Now.TimeString(), messagePrefix);

            if (RMUMM.settingsData.showDaystamp)
                messagePrefix = String.Format("{0} | {1}", TimeWeather.Now.DayString(), messagePrefix);

            Console.Log(String.Format("{0}: {1}", messagePrefix, message));
            LogToDebug("[CONSOLE OUTPUT] " + message);
        }

        public void LogToDebug(string message, LogLevel messageLevel = LogLevel.Info)
        {
            if (messageLevel >= currentLogLevel)
                WriteLog($"[{messageLevel.ToString().ToUpper().Substring(0, 3)}] {message}");
        }

        public void LogToError(string message)
        {
            WriteLog($"[Error] {message}");
        }

        private static void WriteLog(string msg)
        {
            string str = $"[{DateTime.Now:HH:mm:ss.fff}] {msg}";
            RMUMM.ModEntry.Logger.Log(str);
        }
    }
}
