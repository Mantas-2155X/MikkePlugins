using System;
using System.Collections.Generic;
using System.Linq;
using AIChara;
using BepInEx;
using BepInEx.Configuration;
using ExtensibleSaveFormat;
using HarmonyLib;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Studio;
using KKAPI.Studio.UI;
using Studio;
using TMPro;
using UniRx;
using UnityEngine;

namespace BeaverAI {
    [BepInDependency("marco.kkapi", "1.9.4")]
    [BepInDependency("com.deathweasel.bepinex.uncensorselector", "3.9.0")]
    [BepInPlugin(GUID, "Beaver plugin", VERSION)]
    public class BeaverPlugin : BaseUnityPlugin {
        public const string GUID = "mikke.BeaverAI";
        internal const string VERSION = "1.2.3";

#if HS2
        public const string PROCESS = "HoneySelect2";
#elif AI
        public const string PROCESS = "AI-Syoujyo";
#endif

        public const int MaxShapeCount = 20;

        internal static ConfigEntry<bool> ConfPanties;
        internal static ConfigEntry<bool> ConfPantyhose;
        internal static ConfigEntry<bool> ConfBottom;

        private static CurrentStateCategorySlider[] StudioSliders;

        private static MPCharCtrl mpCharCtrl;

        internal static MPCharCtrl GetMpCharCtrl() {
            if (mpCharCtrl != null) {
                return mpCharCtrl;
            }

            mpCharCtrl = FindObjectOfType<MPCharCtrl>();
            return mpCharCtrl;
        }

        private void Start() {
            CharacterApi.RegisterExtraBehaviour<BeaverController>(GUID);

            ConfPanties = Config.Bind("Hide Accessories/pussy shape when wearing", "Panties", false, "");
            ConfPantyhose = Config.Bind("Hide Accessories/pussy shape when wearing", "Pantyhose", false, "");
            ConfBottom = Config.Bind("Hide Accessories/pussy shape when wearing", "Bottom", false, "");

            if (StudioAPI.InsideStudio) {
                StudioSliders = new CurrentStateCategorySlider[MaxShapeCount];
                for (int index = 0; index < MaxShapeCount; index++) {
                    var i = index;
                    var beaverName = "Beaver " + i;
                    CurrentStateCategorySlider slider = new CurrentStateCategorySlider(beaverName, c => UpdateStudioSlider(c, i));
                    StudioAPI.GetOrCreateCurrentStateCategory("Uncensor Selector").AddControl(slider);
                    StudioSliders[index] = slider;

                    var observer = Observer.Create<float>(f => UpdateStudioSliderValue(f, i));

                    slider.Value.Subscribe(observer);
                }
            }
        }

        private static void UpdateStudioSliderValue(float value, int index) {
            var beaverController = GetMpCharCtrl()?.ociChar?.charInfo?.GetComponent<BeaverController>();
            if (beaverController == null) return;
            beaverController.SetBeaverShape(index, value);
        }

        private static float UpdateStudioSlider(OCIChar ociChar, int index) {
            var beaverController = ociChar?.charInfo?.GetComponent<BeaverController>();

            var slider = StudioSliders[index];
            if (beaverController == null || !beaverController.HasBeaverShape(index)) {
                slider.Visible.OnNext(false);
                return 0f;
            }

            slider.Visible.OnNext(true);
            slider.RootGameObject.GetComponentInChildren<TextMeshProUGUI>().text = "" + beaverController.GetBeaverName(index);
            return beaverController.GetBeaverShape(index);
        }

        private void Awake() {
            var harmony = Harmony.CreateAndPatchAll(typeof(BeaverPlugin));
            if (StudioAPI.InsideStudio) {
                var ucType = GetUcType();
                var methodInfo = AccessTools.Method(ucType, "UpdateUncensor", null);
                harmony.Patch(methodInfo, null, new HarmonyMethod(GetType(), nameof(UpdateBeaverStudioUIFromUncensor)));
            }
        }

        internal static Type GetUcType() {
#if HS2
            var ucType = Type.GetType("KK_Plugins.UncensorSelector+UncensorSelectorController, HS2_UncensorSelector, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
#elif AI
            var ucType = Type.GetType("KK_Plugins.UncensorSelector+UncensorSelectorController, AI_UncensorSelector, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
#endif
            return ucType;
        }

        internal static void UpdateBeaverStudioUIFromUncensor() {
            OCIChar ociChar = GetMpCharCtrl()?.ociChar;
            var beaverController = ociChar?.charInfo?.GetComponent<BeaverController>();
            if (beaverController == null) {
                return;
            }

            for (int index = 0; index < MaxShapeCount; index++) {
                UpdateStudioSlider(ociChar, index);
            }
        }


        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.SetClothesState))]
        internal static void SetClothesStatePostfix(ChaControl __instance, int clothesKind) {
            if (clothesKind == 1 || clothesKind == 3 || clothesKind == 5) //bottoms, panties, pantyhose
                GetBeaverController(__instance)?.UpdateBeaver();
        }


        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeClothes), new[] {typeof(int), typeof(int), typeof(bool)})]
        public static void ChangeClothes(ChaControl __instance, int kind) {
            if (kind == 1 || kind == 3 || kind == 5) //bottoms, panties, pantyhose
                GetBeaverController(__instance)?.UpdateBeaver();
        }

        private static BeaverController GetBeaverController(ChaControl character) => character?.gameObject?.GetComponent<BeaverController>();
    }

    internal class BeaverController : CharaCustomFunctionController {
        internal const int Reloaded = -1;
        internal const int NoPan = 0;
        internal const int Panties = 1;
        internal const int Pantyhose = 10;
        internal const int Bottom = 100;

        private int wasWearing;
        public BeaverInfo beaverInfo;

        protected override void OnReload(GameMode currentGameMode) {
            base.OnReload(currentGameMode);
            beaverInfo = new BeaverInfo();

            var pluginData = GetExtendedData();
            if (pluginData != null) {
                beaverInfo.Load(pluginData);
            }

            if (currentGameMode == GameMode.Studio && pluginData.data.ContainsKey("K_STUDIO")) {
                wasWearing = IsWearing();
                UpdateShapes(wasWearing == NoPan);
            } else {
                wasWearing = Reloaded;
            }

            UpdateBeaver();
        }

        protected override void OnCardBeingSaved(GameMode currentGameMode) {
            var pluginData = beaverInfo.Save(new PluginData());
            if (currentGameMode == GameMode.Studio) {
                pluginData.data["K_STUDIO"] = true;
            }

            SetExtendedData(pluginData);
        }

        protected override void OnCoordinateBeingSaved(ChaFileCoordinate coordinate) {
            SetCoordinateExtendedData(coordinate, beaverInfo.SaveCoord(new PluginData()));
        }

        protected override void OnCoordinateBeingLoaded(ChaFileCoordinate coordinate) {
            wasWearing = Reloaded;
            var coordinateLoadFlags = MakerAPI.GetCoordinateLoadFlags();
            if (coordinateLoadFlags != null && !coordinateLoadFlags.Clothes) return;
            var pluginData = GetCoordinateExtendedData(coordinate);
            beaverInfo.LoadCoord(pluginData);
            UpdateBeaver();
        }

        private void UpdateAccesories(bool showAccessories) {
            for (int index = 0; index < ChaControl.nowCoordinate.accessory.parts.Length; index++) {
                var acc = ChaControl.nowCoordinate.accessory.parts[index];
                if (acc.parentKey == ChaAccessoryDefine.AccessoryParentKey.N_Dan.ToString() ||
                    acc.parentKey == ChaAccessoryDefine.AccessoryParentKey.N_Kokan.ToString() ||
                    acc.parentKey == ChaAccessoryDefine.AccessoryParentKey.N_Ana.ToString()) {
                    ChaControl.SetAccessoryState(index, showAccessories);
                }
            }

            if (StudioAPI.InsideStudio) {
                var mpCharCtrl = BeaverPlugin.GetMpCharCtrl();
                if (mpCharCtrl == null) return;
                //do some hacky shit

                var tempChar = mpCharCtrl.ociChar;
                var thisChar = ChaControl.GetOCIChar();
                mpCharCtrl.ociChar = thisChar;
                if (tempChar != thisChar) mpCharCtrl.ociChar = tempChar;
            }
        }

        private int IsWearing() {
            if (beaverInfo == null) {
                return 0;
            }

            bool wearPanties = ChaControl.IsClothesStateKind((int) ChaFileDefine.ClothesKind.inner_b) &&
                               ChaControl.fileStatus.clothesState[(int) ChaFileDefine.ClothesKind.inner_b] == 0 && beaverInfo.PantiesHiding;

            bool wearPantyhose = ChaControl.IsClothesStateKind((int) ChaFileDefine.ClothesKind.panst) &&
                                 ChaControl.fileStatus.clothesState[(int) ChaFileDefine.ClothesKind.panst] == 0 && beaverInfo.PantyHoseHiding;

            bool wearBottom = ChaControl.IsClothesStateKind((int) ChaFileDefine.ClothesKind.bot) &&
                              ChaControl.fileStatus.clothesState[(int) ChaFileDefine.ClothesKind.bot] == 0 && beaverInfo.BottomHiding;

            return (wearPanties ? Panties : 0) +
                   (wearPantyhose ? Pantyhose : 0) +
                   (wearBottom ? Bottom : 0);
        }

        public void SetBeaverShape(int index, float value) {
            if (beaverInfo == null) return;
            beaverInfo.BeaverShapes[index] = value;
            if (IsWearing() == NoPan) {
                BlendBeaver(index, value);
            }
        }

        private void BlendBeaver(int index, float value) {
            var skinnedMeshRenderer = FindSkinnedMeshRenderer();
            if (skinnedMeshRenderer!=null && skinnedMeshRenderer.sharedMesh.blendShapeCount > index) {
                skinnedMeshRenderer.SetBlendShapeWeight(index, value * 100);
            }
        }

        private SkinnedMeshRenderer FindSkinnedMeshRenderer() {
            var body = transform.Find("BodyTop/p_cf_body_00/n_o_root/n_body_base/n_body_cf/o_body_cf");
            if (body == null) return null;
            var skinnedMeshRenderer = body.GetComponent<SkinnedMeshRenderer>();
            return skinnedMeshRenderer;
        }

        public void Recalculate() {
            wasWearing = Reloaded;
        }

        public void UpdateBeaver() {
            if (ChaControl == null) return;
            var nowWearing = IsWearing();
            if (nowWearing == wasWearing) return;
            wasWearing = nowWearing;
            UpdateAccesories(nowWearing == NoPan);
            UpdateShapes(nowWearing == NoPan);
        }

        private void UpdateShapes(bool showBeaver) {
            if (ChaControl.sex == 0) {
                return; //No support for males
            }

            foreach (KeyValuePair<int, float> entry in beaverInfo.BeaverShapes) {
                BlendBeaver(entry.Key, showBeaver ? entry.Value : 0f);
            }
        }

        public float GetBeaverShape(int index) {
            if (beaverInfo == null) return 0f;
            beaverInfo.BeaverShapes.TryGetValue(index, out var value);
            return value;
        }

        public bool HasBeaverShape(int index) {
            if (ChaControl.sex == 0) {
                return false; //No support for males
            }
            var mesh = FindSkinnedMeshRenderer();
            return mesh.sharedMesh.blendShapeCount > index;
        }

        public string GetBeaverName(int index) {
            var mesh = FindSkinnedMeshRenderer();
            if (mesh.sharedMesh.blendShapeCount > index) {
                return mesh.sharedMesh.GetBlendShapeName(index).Replace("unknown_blendshape.", "");
            }

            return "Beaver " + index;
        }
    }

    internal class BeaverInfo {
        private static string K_PANTIES = "K_PANTIES";
        private static string K_PANTYHOSE = "K_PANTYHOSE";
        private static string K_BOTTOM = "K_BOTTOM";
        private static string K_SHAPES = "K_SHAPES";

        public bool PantiesHiding { get; set; }
        public bool PantyHoseHiding { get; set; }
        public bool BottomHiding { get; set; }

        public Dictionary<int, float> BeaverShapes { get; private set; } = new Dictionary<int, float>();

        internal void Load(PluginData pluginData) {
            if (LoadCoord(pluginData)) return;
            if (pluginData.data.TryGetValue(K_SHAPES, out var res4))
                BeaverShapes = ((Dictionary<object, object>) res4).ToDictionary(pair => (int) pair.Key, pair => (float) pair.Value);
        }

        public bool LoadCoord(PluginData pluginData) {
            PantiesHiding = BeaverPlugin.ConfPanties.Value;
            PantyHoseHiding = BeaverPlugin.ConfPantyhose.Value;
            BottomHiding = BeaverPlugin.ConfBottom.Value;
            if (pluginData == null) return true;
            if (pluginData.data.TryGetValue(K_PANTIES, out var res1))
                PantiesHiding = (bool) res1;
            if (pluginData.data.TryGetValue(K_PANTYHOSE, out var res2))
                PantyHoseHiding = (bool) res2;
            if (pluginData.data.TryGetValue(K_BOTTOM, out var res3))
                BottomHiding = (bool) res3;
            return false;
        }

        internal PluginData Save(PluginData pluginData) {
            SaveCoord(pluginData);
            pluginData.data.Add(K_SHAPES, BeaverShapes);
            return pluginData;
        }

        public PluginData SaveCoord(PluginData pluginData) {
            pluginData.data.Add(K_PANTIES, PantiesHiding);
            pluginData.data.Add(K_PANTYHOSE, PantyHoseHiding);
            pluginData.data.Add(K_BOTTOM, BottomHiding);
            return pluginData;
        }
    }
}