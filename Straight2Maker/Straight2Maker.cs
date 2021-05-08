using System;
using AIChara;
using BepInEx;
using HarmonyLib;
using HS2;
using UnityEngine;

namespace Straight2Maker {
    [BepInPlugin(GUID, "Straight 2 Maker", VERSION)]
    [BepInProcess(PROCESS)]
    public class Straight2MakerPlugin : BaseUnityPlugin {
        public static bool skipTitle = true;
        public const string PROCESS = "HoneySelect2";

        public const string GUID = "mikke.straight2maker";
        internal const string VERSION = "1.0";

        private void Awake() {
            Harmony.CreateAndPatchAll(typeof(Straight2MakerPlugin));
        }

        [HarmonyPostfix, HarmonyPatch(typeof(TitleScene), "Start")]
        public static void TitleScene_Start_Patch(TitleScene __instance) {
            if (skipTitle) {
                skipTitle = false;
                __instance.OnMakeFemale();
            }
        }
        
        [HarmonyPostfix, HarmonyPatch(typeof(BonesFramework.BonesFramework), "LoadAdditionalBonesForCurrent")]
        public static void Fbx_load_path(string assetBundlePath,
            string assetName,
            string manifest) {
            UnityEngine.Debug.LogError("JALLA:"+assetBundlePath);
        }
    }
}