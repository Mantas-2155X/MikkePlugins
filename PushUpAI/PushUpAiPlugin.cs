using AIChara;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using KKABMX.Core;
using KKAPI.Chara;

namespace PushUpAI {
    [BepInDependency("marco.kkapi", "1.9.2")]
    [BepInDependency(KKABMX_Core.GUID, "4.2")]
    [BepInPlugin(GUID, "PushUp plugin", VERSION)]
    public class PushUpAiPlugin : BaseUnityPlugin {
        public const string GUID = "mikke.pushUpAI";
        internal const string VERSION = "2.1.1";
        private const string PushUpBraDefaultSectionName = "Push-Up bra default settings";

        internal static ManualLogSource Log;

        private static ConfigFile Conf;
        public static DefaultPushUp BraDefault;
        
        public static AnimationKeyInfo anmKeyInfo = new AnimationKeyInfo();

        private void Start() {
            Log = Logger;
            Conf = Config;

            var aiabmxPlugin = FindObjectOfType(typeof(KKABMX_Core));
            if (aiabmxPlugin != null) {
                CharacterApi.RegisterExtraBehaviour<PushUpBoneController>(GUID);
            } else {
                Log.LogError("Could not find KKABMX_Core");
            }

            BraDefault = new DefaultPushUp();

            var settingEnablePushUp = Conf.Wrap(PushUpBraDefaultSectionName, "Enable pushUp by default", "Is the push-up bra effect enabled by default for all inner tops.", true);
            BraDefault.EnablePushUp = settingEnablePushUp.Value;
            settingEnablePushUp.SettingChanged += (sender, args) => BraDefault.EnablePushUp = settingEnablePushUp.Value;

            var settingFirmness = Config.AddSetting(PushUpBraDefaultSectionName, "Firmness", 90,
                new ConfigDescription("The firmer the bra holds the breasts, the less they will bounce",
                    new AcceptableValueRange<int>(0, 100)));
            BraDefault.Firmness = settingFirmness.Value / 100f;
            settingFirmness.SettingChanged += (sender, args) => BraDefault.Firmness = settingFirmness.Value / 100f;

            var settingLift = Config.AddSetting(PushUpBraDefaultSectionName, "Lift", 60, new
                ConfigDescription("Lift is the minimum height position of the breasts while the bra is on",
                    new AcceptableValueRange<int>(0, 100)));
            BraDefault.Lift = settingLift.Value / 100f;
            settingLift.SettingChanged += (sender, args) => BraDefault.Lift = settingLift.Value / 100f;

            var settingPuT = Config.AddSetting(PushUpBraDefaultSectionName, "Push Together", 65,
                new ConfigDescription("If the breasts are wide apart, the bra will push them together.",
                    new AcceptableValueRange<int>(0, 100)));
            BraDefault.PushTogether = settingPuT.Value / 100f;
            settingPuT.SettingChanged += (sender, args) => BraDefault.PushTogether = settingPuT.Value / 100f;

            var settingSqueeze = Config.AddSetting(PushUpBraDefaultSectionName, "Squeeze", 60,
                new ConfigDescription("Long breasts will be squeezed flat by this amount. Breast volume stays roughly the same, so the breasts may look bigger",
                    new AcceptableValueRange<int>(0, 100)));
            BraDefault.Squeeze = settingSqueeze.Value / 100f;
            settingSqueeze.SettingChanged += (sender, args) => BraDefault.Squeeze = settingSqueeze.Value / 100f;

            var settingCentering = Config.AddSetting(PushUpBraDefaultSectionName, "Nipple Centering", 100,
                new ConfigDescription("If the nipples point up or down, wearing a bra will make them point forwards.",
                    new AcceptableValueRange<int>(0, 100)));
            BraDefault.CenterNipples = settingCentering.Value / 100f;
            settingCentering.SettingChanged += (sender, args) => BraDefault.CenterNipples = settingCentering.Value / 100f;

           // var settingFlattened = Conf.Wrap(PushUpBraDefaultSectionName, "Flatten Nipples",
          //      "Flatten the areola and prevent nipple erection while the bra is worn.", true);
            var settingFlattened = Conf.Bind<bool>(PushUpBraDefaultSectionName, "Flatten Nipples", true,
                "Flatten the areola and prevent nipple erection while the bra is worn.");
            BraDefault.FlattenNipples = settingFlattened.Value;
            settingFlattened.SettingChanged += (sender, args) => BraDefault.FlattenNipples = settingFlattened.Value;

            var settingHide = Conf.Wrap(PushUpBraDefaultSectionName, "Hide Nipples",
                "Apply a bone modifier to make the nipples nearly disappear. This can make bra and top textures look weird around the nipple area. ",
                false);
            BraDefault.HideNipples = settingHide.Value;
            settingHide.SettingChanged += (sender, args) => BraDefault.HideNipples = settingHide.Value;

            CharacterApi.RegisterExtraBehaviour<PushUpController>(GUID);
        }
        
        private void Awake() {
            Harmony.CreateAndPatchAll(typeof(PushUpAiPlugin));
        }

        public static AnimationKeyInfo getAnimKeyInfo() {
            if (anmKeyInfo.GetKeyCount() == 0) {
                anmKeyInfo.LoadInfo("abdata","list/customshape.unity3d","cf_anmShapeBody");//,new Action<string, string>(Singleton<Character>.Instance.AddLoadAssetBundle));
            }
            return anmKeyInfo;
        }
        
        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl),nameof(ChaControl.UpdateBustGravity))]
        internal static void UpdateBustGravityPostfix(ChaControl __instance)
        {
            GetPushUpController(__instance)?.ApplyBreastSoftness();
        }
        
        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl),nameof(ChaControl.UpdateBustSoftness))]
        internal static void UpdateBustSoftnessPostfix(ChaControl __instance)
        {
            GetPushUpController(__instance)?.ApplyBreastSoftness();
        }
        
        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.SetClothesState))]
        internal static void SetClothesStatePostfix(ChaControl __instance, int clothesKind)
        {
            if (clothesKind == 0 || clothesKind == 2) //tops and bras
                GetPushUpController(__instance)?.RecalculateBody();
        }
        
        
        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeClothes), new[] {typeof(int), typeof(int), typeof(bool)} )]
        public static void ChangeClothes(ChaControl __instance, int kind) {
            if (kind == 0 || kind == 2) //tops and bras
                GetPushUpController(__instance)?.RecalculateBody();
        }

        private static PushUpController GetPushUpController(ChaControl character) => character?.gameObject?.GetComponent<PushUpController>();
    }
}