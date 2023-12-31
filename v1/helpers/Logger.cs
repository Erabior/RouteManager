﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UI.Console;

namespace RouteManager.v1.helpers
{
    public class Logger : MonoBehaviour
    {
        public static void LogToConsole(string message)
        {
            Console.Log(String.Format("{0}: {1}",ModLoader.getModName(), message));
            LogToDebug("[CONSOLE OUTPUT] " + message);
        }

        public static void LogToDebug(string message)
        {
            Debug.Log(String.Format("{0} - {1}_V{2}: {3}", DateTime.Now.ToString("u"), ModLoader.getModName(), ModLoader.getModVersion(), message));
        }

        public static void LogToError(string message)
        {
            Debug.LogError(String.Format("{0} - {1}_V{2}: {3}", DateTime.Now.ToString("u"), ModLoader.getModName(), ModLoader.getModVersion(), message));
        }
    }
}
