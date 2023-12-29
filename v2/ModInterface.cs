using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.Builder;
using UnityEngine.UI;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using Logger = RouteManager.v2.Logging.Logger;

namespace RouteManager.v2.UI
{
    public class ModInterface : MonoBehaviour
    {
        CoreInterface coreUserInterface;

        void Awake()
        {

            //Log status
            Logger.LogToDebug("--------------------------------------------------------------------------------------------------");
            Logger.LogToDebug("Dispatcher UI Initializing");
            Logger.LogToDebug("--------------------------------------------------------------------------------------------------");

            coreUserInterface = new CoreInterface();

            //Log status
            Logger.LogToDebug("--------------------------------------------------------------------------------------------------");
            Logger.LogToDebug("Dispatcher UI Ready!");
            Logger.LogToDebug("--------------------------------------------------------------------------------------------------");
        }

        void Update()
        {
            if (Input.GetKeyUp(KeyCode.Insert))
            {
                Logger.LogToDebug("Dispatcher UI Toggled!");
                coreUserInterface.togglePanel();
            }
        }


    }
}
