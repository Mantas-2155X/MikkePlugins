using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Studio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Resources = MoveController.Properties.Resources;

namespace MoveController 
{
    public sealed class MoveCtrlWindow : MonoBehaviour 
    {
        private bool IsVisible;

        private Canvas GUI;

        private Button AnimControlButton;
        private Button ResetFkButton;

        private Image MoveCtrlButtonImage;

        public readonly List<ObjectCtrlInfo> AllSelected = new List<ObjectCtrlInfo>();
        
        private const float GuiFactor = 0.8f;
        
        private void Start()
        {
            SpawnGUI();
        }

        private void Update() 
        {
            //all selected
            AllSelected.Clear();

            if (MoveCtrlPlugin.treeNodeController == null)
                return;
            
            var treeNodeObjects = MoveCtrlPlugin.treeNodeController.selectNodes;
            if (treeNodeObjects == null)
                return;

            foreach (var node in treeNodeObjects)
                if (Studio.Studio.Instance.dicInfo.TryGetValue(node, out var info))
                    AllSelected.Add(info);
            
            if (AllSelected.Count > 0) 
                AllSelected[0].guideObject.visible = true;

            toggleButton(AnimControlButton, AllSelected.Any() && (AllSelected[0] is OCIChar || (AllSelected[0] is OCIItem item && item.isAnime)));
            
            //FK target
            var fkActive = MoveCtrlPlugin.fkManagerService != null && MoveCtrlPlugin.fkManagerService.checkIfFkNodeSelected() && AllSelected.Any();
            if (fkActive)
                AllSelected[0].guideObject.visible = false | MoveCtrlPlugin.neverHideObjectHandle;
            
            toggleButton(ResetFkButton, fkActive);
        }

        internal static Vector2 getMouseInput() 
        {
            var xm = Input.GetAxis("Mouse X");
            var ym = Input.GetAxis("Mouse Y");

            return new Vector2(xm, ym);
        }

        internal static Quaternion getCameraQuaternion() 
        {
            var tc = MoveCtrlPlugin.camera.transform;
            var camAngle = tc.rotation.eulerAngles;

            camAngle.x = 0;
            camAngle.z = 0;

            var cameraAngle = Quaternion.Euler(camAngle);

            return cameraAngle;
        }

        private static void toggleButton(Button button, bool state)
        {
            if (button == null) 
                return;

            button.interactable = state;
            button.GetComponentInChildren<Text>().color = state ? Color.black : Color.gray;
        }

        public void HackTheWorld(Texture2D icon) 
        {
            var studioScene = FindObjectOfType<StudioScene>();
            if (studioScene == null)
                return;

            var buttonTrav = Traverse.Create(studioScene).Field("inputInfo").Field("button");
            
            var controllerButton = buttonTrav.GetValue<Button>();
            if (controllerButton == null)
                return;
            
            controllerButton.interactable = true;
            controllerButton.onClick = new Button.ButtonClickedEvent();
            controllerButton.onClick.AddListener(() => 
            {
                IsVisible = !IsVisible;
                
                GUI.gameObject.SetActive(IsVisible);
                controllerButton.image.color = IsVisible ? Color.green : Color.white;
                
                var scale = MoveCtrlPlugin.guiScale.Value;
                GUI.scaleFactor = scale * GuiFactor;
                //TODO: check if window is off screen and move back    
            });

            MoveCtrlButtonImage = controllerButton.targetGraphic as Image;
            
            if (MoveCtrlButtonImage != null)
                MoveCtrlButtonImage.sprite = Sprite.Create(icon, new Rect(0f, 0f, 32, 32), new Vector2(16, 16));

            buttonTrav.SetValue(Instantiate(controllerButton));
        }

        private void SpawnGUI() 
        {
            var bundle = AssetBundle.LoadFromMemory(Resources.objmoveresources);

            //Load icon
            var icon = bundle.LoadAsset<Texture2D>("Icon-DXT1");

            GUI = Instantiate(bundle.LoadAsset<GameObject>("MoveCanvas")).GetComponent<Canvas>();
            GUI.gameObject.SetActive(IsVisible);
            bundle.Unload(false);

            GUI.scaleFactor = GuiFactor;

            var bg = (RectTransform) GUI.transform.Find("MovePanel");
            var mw = bg.gameObject.AddComponent<MovableWindow>();
            mw.toDrag = bg;
            mw.preventCameraControl = true;

            var buttonManager = new ButtonManager(MoveCtrlPlugin.fkManagerService);
            var buttonActionManager = new ButtonActionManager(MoveCtrlPlugin.moveObjectService, MoveCtrlPlugin.fkManagerService, this, MoveCtrlPlugin.undoRedoService);

            buttonManager.DragButton(GUI.transform.Find("MovePanel/MoveXZ").GetComponent<Button>(), buttonActionManager.MoveXZ(new Vector3(1, 0, 1)));
            buttonManager.DragButton(GUI.transform.Find("MovePanel/MoveY").GetComponent<Button>(), buttonActionManager.MoveY(new Vector3(0, 1, 0)));

            buttonManager.DragButton(GUI.transform.Find("MovePanel/RotateX").GetComponent<Button>(), buttonActionManager.RotateX());
            buttonManager.DragButton(GUI.transform.Find("MovePanel/RotateY").GetComponent<Button>(), buttonActionManager.RotateY());
            buttonManager.DragButton(GUI.transform.Find("MovePanel/RotateZ").GetComponent<Button>(), buttonActionManager.RotateZ());

            buttonManager.ClickButton(GUI.transform.Find("MovePanel/Move2Cam").GetComponent<Button>(), buttonActionManager.Move2Camera());

            buttonManager.DragButton(GUI.transform.Find("MovePanel/FkX").GetComponent<Button>(), buttonActionManager.RotateFk(new Vector3(-1, 0, 0)));
            buttonManager.DragButton(GUI.transform.Find("MovePanel/FkY").GetComponent<Button>(), buttonActionManager.RotateFk(new Vector3(0, -1, 0)));
            buttonManager.DragButton(GUI.transform.Find("MovePanel/FkZ").GetComponent<Button>(), buttonActionManager.RotateFk(new Vector3(0, 0, -1)));

            AnimControlButton = buttonManager.DragButton(GUI.transform.Find("MovePanel/AnimControl").GetComponent<Button>(), buttonActionManager.Animation());
            toggleButton(AnimControlButton, false);

            ResetFkButton = buttonManager.ClickButton(GUI.transform.Find("MovePanel/ResetFk").GetComponent<Button>(), buttonActionManager.ResetFk());
            toggleButton(ResetFkButton, false);

            buttonManager.slider(GUI.transform.Find("MovePanel/FactorSlider").GetComponent<Slider>(), buttonActionManager.UpdateSpeedFactors());
            buttonManager.slider(GUI.transform.Find("MovePanel/FKSizeSlider").GetComponent<Slider>(), buttonActionManager.UpdateFkScale());

            GUI.gameObject.AddComponent<EventTrigger>();
            var trigger = GUI.gameObject.GetComponent<EventTrigger>();
            trigger.triggers.Add(buttonManager.getScrollTrigger());

            //use reflection to hack the button
            HackTheWorld(icon);
        }
    }
}