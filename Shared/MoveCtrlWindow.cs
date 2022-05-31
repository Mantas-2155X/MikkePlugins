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
    public class MoveCtrlWindow : MonoBehaviour 
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
            MoveCtrlPlugin.window = this;
            
            SpawnGUI();
        }

        private void Update() 
        {
            //all selected
            AllSelected.Clear();

            if (MoveCtrlPlugin.treeNodeController == null)
                return;
            
            var treeNodeObjects = MoveCtrlPlugin.treeNodeController.selectNodes;
            if (treeNodeObjects.IsNullOrEmpty())
                return;
            
            var selectNode = treeNodeObjects[0];
            
            if (AccessoryCtrlService.AccMoveInfos.TryGetValue(selectNode, out var value)) 
            {
                AccessoryCtrlService.Current = value;
                return;
            }
            AccessoryCtrlService.Current = null;
            
            foreach (var node in treeNodeObjects)
                if (Studio.Studio.Instance.dicInfo.TryGetValue(node, out var info))
                    AllSelected.Add(info);
            
            if (AllSelected.Count > 0) 
                AllSelected[0].guideObject.visible = true;

            toggleButton(AnimControlButton, AllSelected.Any() && (AllSelected[0] is OCIChar || (AllSelected[0] is OCIItem item && item.isAnime)));
            
            //FK target
            var fkActive = FkManagerService.checkIfFkNodeSelected() && AllSelected.Any();
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

        public void HackTheWorld(Texture2D moveCtrlIcon, Texture2D accsCtrlIcon) 
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
                MoveCtrlButtonImage.sprite = Sprite.Create(moveCtrlIcon, new Rect(0f, 0f, 32, 32), new Vector2(16, 16));

            buttonTrav.SetValue(Instantiate(controllerButton));
            
            var original = GameObject.Find("StudioScene").transform.Find("Canvas Object List/Image Bar/Button Remove").GetComponent<RectTransform>();
            var accCtrlButton = Instantiate(original.gameObject).GetComponent<Button>();
            
            accCtrlButton.name = "Button Accessories";
            
            var accCtrlButtonRectTransform = accCtrlButton.transform as RectTransform;
            if (accCtrlButtonRectTransform == null)
                return;
            
            accCtrlButtonRectTransform.SetParent(original.parent, true);
            accCtrlButtonRectTransform.localScale = original.localScale;
            
            accCtrlButtonRectTransform.anchoredPosition = original.anchoredPosition + new Vector2(-48f, 0f);
            
            var accCtrlButtonImg = accCtrlButton.targetGraphic as Image;
            if (accCtrlButtonImg == null)
                return;
            
            accCtrlButtonImg.sprite = Sprite.Create(accsCtrlIcon, new Rect(0f, 0f, 32, 32), new Vector2(16, 16));

            accCtrlButton.interactable = true;
            accCtrlButton.onClick.AddListener(() => AccessoryCtrlService.ToggleNodes(accCtrlButton));
        }

        private void SpawnGUI() 
        {
            var bundle = AssetBundle.LoadFromMemory(Resources.objmoveresources);

            //Load icons
            var moveCtrlIcon = bundle.LoadAsset<Texture2D>("Icon-DXT1");
            var accsCtrlIcon = bundle.LoadAsset<Texture2D>("IconAccsMove");

            GUI = Instantiate(bundle.LoadAsset<GameObject>("MoveCanvas")).GetComponent<Canvas>();
            GUI.gameObject.SetActive(IsVisible);
            bundle.Unload(false);

            GUI.scaleFactor = GuiFactor;

            var bg = (RectTransform) GUI.transform.Find("MovePanel");
            var mw = bg.gameObject.AddComponent<MovableWindow>();
            mw.toDrag = bg;
            mw.preventCameraControl = true;
            
            ButtonManager.DragButton(GUI.transform.Find("MovePanel/MoveXZ").GetComponent<Button>(), ButtonActionManager.MoveXZ(new Vector3(1, 0, 1)));
            ButtonManager.DragButton(GUI.transform.Find("MovePanel/MoveY").GetComponent<Button>(), ButtonActionManager.MoveY(new Vector3(0, 1, 0)));

            ButtonManager.DragButton(GUI.transform.Find("MovePanel/RotateX").GetComponent<Button>(), ButtonActionManager.RotateX());
            ButtonManager.DragButton(GUI.transform.Find("MovePanel/RotateY").GetComponent<Button>(), ButtonActionManager.RotateY());
            ButtonManager.DragButton(GUI.transform.Find("MovePanel/RotateZ").GetComponent<Button>(), ButtonActionManager.RotateZ());

            ButtonManager.ClickButton(GUI.transform.Find("MovePanel/Move2Cam").GetComponent<Button>(), ButtonActionManager.Move2Camera());

            ButtonManager.DragButton(GUI.transform.Find("MovePanel/FkX").GetComponent<Button>(), ButtonActionManager.RotateFk(new Vector3(-1, 0, 0)));
            ButtonManager.DragButton(GUI.transform.Find("MovePanel/FkY").GetComponent<Button>(), ButtonActionManager.RotateFk(new Vector3(0, -1, 0)));
            ButtonManager.DragButton(GUI.transform.Find("MovePanel/FkZ").GetComponent<Button>(), ButtonActionManager.RotateFk(new Vector3(0, 0, -1)));

            AnimControlButton = ButtonManager.DragButton(GUI.transform.Find("MovePanel/AnimControl").GetComponent<Button>(), ButtonActionManager.Animation());
            toggleButton(AnimControlButton, false);

            ResetFkButton = ButtonManager.ClickButton(GUI.transform.Find("MovePanel/ResetFk").GetComponent<Button>(), ButtonActionManager.ResetFk());
            toggleButton(ResetFkButton, false);

            ButtonManager.slider(GUI.transform.Find("MovePanel/FactorSlider").GetComponent<Slider>(), ButtonActionManager.UpdateSpeedFactors());
            ButtonManager.slider(GUI.transform.Find("MovePanel/FKSizeSlider").GetComponent<Slider>(), ButtonActionManager.UpdateFkScale());

            GUI.gameObject.AddComponent<EventTrigger>();
            var trigger = GUI.gameObject.GetComponent<EventTrigger>();
            trigger.triggers.Add(ButtonManager.getScrollTrigger());

            //use reflection to hack the button
            HackTheWorld(moveCtrlIcon, accsCtrlIcon);
        }
    }
}