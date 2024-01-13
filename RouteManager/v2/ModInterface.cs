using UnityEngine;

namespace RouteManager.v2.UI
{
    public class ModInterface : MonoBehaviour
    {
        CoreInterface coreUserInterface;

        void Awake()
        {

            //Log status
            RouteManager.logger.LogToDebug("--------------------------------------------------------------------------------------------------");
            RouteManager.logger.LogToDebug("Dispatcher UI Initializing");
            RouteManager.logger.LogToDebug("--------------------------------------------------------------------------------------------------");

            coreUserInterface = new CoreInterface();

            //Log status
            RouteManager.logger.LogToDebug("--------------------------------------------------------------------------------------------------");
            RouteManager.logger.LogToDebug("Dispatcher UI Ready!");
            RouteManager.logger.LogToDebug("--------------------------------------------------------------------------------------------------");
        }

        void Update()
        {
            if (Input.GetKeyUp(KeyCode.Insert))
            {
                RouteManager.logger.LogToDebug("Dispatcher UI Toggled!");
                coreUserInterface.togglePanel();
            }
        }


    }
}
