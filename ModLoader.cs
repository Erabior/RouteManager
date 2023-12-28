using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UI.Menu;
using RouteManager.v2;


namespace RouteManager
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class ModLoader : BaseUnityPlugin
    {
        private const string modGUID = "Erabior.Dispatcher";
        private const string modName = "Dispatcher";
        private const string modVersion = "2.0.0.0";
        private readonly Harmony harmony = new Harmony(modGUID);
        public static ManualLogSource mls;

        void Awake()
        {
            harmony.PatchAll();
            mls = Logger;
        }

        public static string getModGuid()
        {
            return modGUID;
        }

        public static string getModName()
        {
            return modName;
        }

        public static string getModVersion()
        {
            return modVersion;
        }
    }

    public class ModInjector : MonoBehaviour
    {
        [HarmonyPatch(typeof(PersistentLoader), nameof(PersistentLoader.ShowLoadingScreen))]
        public static class ShowLoadingScreen
        {
            public static void Postfix(bool show)
            {
                //I am sure that there is probably a better way of ensuring that only one GameObject is generated.
                //Multiple instances can and will cause issues with multiple actions triggering and more.
                if (GameObject.FindObjectsOfType(typeof(Dispatcher)).Length == 0)
                {
                    //Create Parent Game object to bind the instance of Route AI To.
                    GameObject erabiorDispatcher = new GameObject("Erabior.Dispatcher");

                    //Bind the Route AI component to the Game Object instance
                    erabiorDispatcher.AddComponent<Dispatcher>();
                }
            }
        }
    } 
}