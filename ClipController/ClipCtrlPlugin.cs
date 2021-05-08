using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using Cinemachine;
using ExtensibleSaveFormat;
using UnityEngine;
using UnityEngine.UI;

namespace ClipController {
    [BepInPlugin(GUID, "Clip Controller plugin", VERSION)]
    [BepInProcess("StudioNEOV2")]
    public class ClipCtrlPlugin : BaseUnityPlugin {
        public const string GUID = "mikke.ClipController";
        internal const string VERSION = "1.0";
        private const string CONF_NAME = "Camera Clipping Controller";

        private Canvas GUI;
        private Slider slider;
        private bool isActive = false;
        private float sliderValue = 0.1f;
        private readonly string CLIPVAL = "CLIP_VAL";


        public static ConfigEntry<KeyboardShortcut> ShortKey { get; private set; }

        public void OnLevelWasLoaded() {
            SpawnGui();
        }

        public void Start() {
            ExtendedSave.SceneBeingLoaded += ExtendedSaveOnSceneBeingLoaded;
            ExtendedSave.SceneBeingSaved += ExtendedSaveOnSceneBeingSaved;

            ShortKey = Config.Bind("General", "Show clipcontroller", new KeyboardShortcut(KeyCode.N));
        }

        private void ExtendedSaveOnSceneBeingSaved(string path) {
            PluginData data = new PluginData();
            if (sliderValue != 0.1f) {
                data.data[CLIPVAL] = sliderValue;
            }

            ExtendedSave.SetSceneExtendedDataById(GUID, data);
        }

        private void ExtendedSaveOnSceneBeingLoaded(string path) {
            PluginData data = ExtendedSave.GetSceneExtendedDataById(GUID);
            
            if (data != null && data.data.TryGetValue(CLIPVAL, out var val)) {
                sliderValue = (float) val;
            } else {
                sliderValue = 0.1f;
            }

            slider.value = sliderValue;
        }


        private void SpawnGui() {
            AssetBundle bundle = AssetBundle.LoadFromMemory(Resources.clipctrlres);
            GUI = Instantiate(bundle.LoadAsset<GameObject>("ClipCtrlCanvas")).GetComponent<Canvas>();

            GUI.gameObject.SetActive(isActive);
            bundle.Unload(false);
            var slider1 = GUI.transform.Find("ClipCtrlPanel/ClipCtrlSlider");
            slider = slider1.GetComponent<Slider>();

            slider.value = sliderValue;
            slider.onValueChanged.AddListener(x => AiSpispopd(x));
        }


        private bool AiSpispopd(float sliderValue) {
            this.sliderValue = sliderValue;
            var cameraControl = Studio.Studio.Instance.cameraCtrl;

            var lensSettingsField = typeof(Studio.CameraControl).GetField("lensSettings", BindingFlags.NonPublic | BindingFlags.Instance);
            var lensSettings = (LensSettings) lensSettingsField.GetValue(cameraControl);
            lensSettings.NearClipPlane = sliderValue;
            lensSettingsField.SetValue(cameraControl, lensSettings);
            cameraControl.fieldOfView = cameraControl.fieldOfView;
            return true;
        }

        private void Update() {
            if (ShortKey.Value.IsDown()) {
                isActive = !isActive;
                GUI?.gameObject.SetActive(isActive);
                AiSpispopd(sliderValue);
            }
        }
    }
}