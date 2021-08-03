using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Studio;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MoveController 
{
    [BepInPlugin(GUID, "Move Controller KOI", VERSION)]
    [BepInProcess("CharaStudio")]
    public class MoveCtrlPlugin : BaseUnityPlugin 
    {
        private const string GUID = "mikke.MoveControllerKOI";
        public const string VERSION = "1.7.0";
        
        public static Camera camera;
        public static Studio.CameraControl cameraControl;
        public static TreeNodeCtrl treeNodeController;
        
        public static MoveCtrlWindow window;
        
        public static bool neverHideObjectHandle { get; private set; }
        
        public static ConfigEntry<float> guiScale { get; private set; }
        private static ConfigEntry<bool> hideGuideobjectDuringFk { get; set; }
        
        private void Awake()
        {
            guiScale = Config.Bind("Move Controller settings", "GUI Scale", 1f, "The scale of the MoveController Window. Takes effect the next time the window is opened.");
            hideGuideobjectDuringFk = Config.Bind("Move Controller settings", "Hide object handle when FK active", true, "This setting will hide an object's selection handle when an FK node is selected, so the handle doesn't cover up the FK nodes. Takes effect after restart");
            neverHideObjectHandle = !hideGuideobjectDuringFk.Value;
            
            Harmony.CreateAndPatchAll(typeof(MoveCtrlPlugin));
        }
        
        private static void SceneChanged(Scene Scene, LoadSceneMode mode)
        {
            if (Scene.name != "Studio")
                return;
            
            camera = Camera.main;
            cameraControl = FindObjectOfType<Studio.CameraControl>();
            treeNodeController = Studio.Studio.Instance.treeNodeCtrl;
            
            new GameObject(GUID).AddComponent<MoveCtrlWindow>();
        }
        
        private void OnEnable() => SceneManager.sceneLoaded += SceneChanged;
        
        private void OnDisable() => SceneManager.sceneLoaded -= SceneChanged;
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(AddObjectFemale), "Add", new[] {typeof(ChaControl), typeof(OICharInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject), typeof(bool), typeof(int)})]
        [HarmonyPatch(typeof(AddObjectMale), "Add", new[] {typeof(ChaControl), typeof(OICharInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject), typeof(bool), typeof(int)})]
        [HarmonyPatch(typeof(ChaControl), "ChangeCoordinateType", new[] {typeof(ChaFileDefine.CoordinateType), typeof(bool)})]
        public static void UpdateAccessoryNodes() 
        {
            AccessoryCtrlService.UpdateNodes();
        }
    }
}