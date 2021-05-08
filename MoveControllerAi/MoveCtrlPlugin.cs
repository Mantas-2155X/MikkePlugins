using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;

namespace MoveController {
    [BepInPlugin(GUID, "Move Controller AI", VERSION)]
    [BepInProcess("StudioNEOV2")]
    public class MoveCtrlPlugin : BaseUnityPlugin {
        public const string GUID = "mikke.MoveControllerAI";
        public const string VERSION = "1.5.2";

        public static ConfigFile ConfigFile;
        public const string MoveCtrlConfigName = "Move Controller settings";

        public static ManualLogSource Log;


        public void OnLevelWasLoaded(int level) {
            Log = Logger;

            StartMod();
        }

        public void StartMod() {
            ConfigFile = Config;
            new GameObject(GUID).AddComponent<MoveCtrlWindow>();
        }
    }
}