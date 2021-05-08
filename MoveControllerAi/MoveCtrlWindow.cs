using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using Studio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Resources = MoveController.Properties.Resources;

namespace MoveController {
    class MoveCtrlWindow : MonoBehaviour {
        MoveObjectService moveObjectService;
        UndoRedoService undoRedoService;
        FkManagerService fkManagerService;

        public static MoveCtrlWindow self { get; private set; }

        private bool IsVisible = false;

        private Canvas GUI;

        private Button AnimControlButton;
        private Button ResetFkButton;

        private Image MoveCtrlButtonImage;

        public bool NeverHideObjectHandle { get; set; }
        public ConfigEntry<float> GuiScale { get; set; }
        private static float GuiFactor = 0.8f;


        public List<ObjectCtrlInfo> AllSelected = new List<ObjectCtrlInfo>();

        private Studio.CameraControl cameraControl;

        protected virtual void Awake() {
            self = this;
            this.cameraControl = FindObjectOfType<Studio.CameraControl>();
        }


        private IEnumerator Start() {
            yield return new WaitUntil(() => Studio.Studio.IsInstance());
            var settingHideGuideobjectDuringFk = MoveCtrlPlugin.ConfigFile.Wrap(MoveCtrlPlugin.MoveCtrlConfigName, "Hide object handle when FK active",
                "This setting will hide an object's selection handle when an FK node is selected, so the handle doesn't cover up the FK nodes. Takes effect after restart",
                true);
            NeverHideObjectHandle = !settingHideGuideobjectDuringFk.Value;
            if (MoveCtrlPlugin.ConfigFile.GetSetting<float>(MoveCtrlPlugin.MoveCtrlConfigName, "GUI Scale") == null) {
                var settingGuiScale = MoveCtrlPlugin.ConfigFile.AddSetting(MoveCtrlPlugin.MoveCtrlConfigName, "GUI Scale", 1.0f,
                    new ConfigDescription("The scale of the MoveController Window. Takes effect the next time the window is opened.",
                        new AcceptableValueRange<float>(0.2f, 2)));
                GuiScale = settingGuiScale;
            } else {
                GuiScale = MoveCtrlPlugin.ConfigFile.GetSetting<float>(MoveCtrlPlugin.MoveCtrlConfigName, "GUI Scale");
            }

            undoRedoService = new UndoRedoService();
            moveObjectService = new MoveObjectService(cameraControl, undoRedoService);
            undoRedoService.setMoveObjectService(moveObjectService);
            fkManagerService = new FkManagerService();

            SpawnGUI();
        }

        protected virtual void Update() {
            if (!Studio.Studio.IsInstance()) {
                return;
            }

            //all selected
            AllSelected.Clear();
            TreeNodeObject[] treeNodeObjects = Studio.Studio.Instance.treeNodeCtrl?.selectNodes;
            if (treeNodeObjects == null) {
                return;
            }
            foreach (TreeNodeObject node in treeNodeObjects) {
                ObjectCtrlInfo info;
                if (Studio.Studio.Instance.dicInfo.TryGetValue(node, out info)) {
                    AllSelected.Add(info);
                }
            }

            if (AllSelected.Count > 0) {
                AllSelected[0].guideObject.visible = true;
            }

            if (AllSelected.Any() && (AllSelected[0] is OCIChar || (AllSelected[0] is OCIItem item && item.isAnime))) {
                enableButton(AnimControlButton);
            } else {
                disableButton(AnimControlButton);
            }

            //FK target
            bool fkActive = fkManagerService != null && fkManagerService.checkIfFkNodeSelected() && AllSelected.Any();
            if (fkActive) {
                enableButton(ResetFkButton);
                AllSelected[0].guideObject.visible = false | NeverHideObjectHandle;
            } else {
                disableButton(ResetFkButton);
            }
        }

        internal Vector2 getMouseInput() {
            float xm = Input.GetAxis("Mouse X");
            float ym = Input.GetAxis("Mouse Y");

            return new Vector2(xm, ym);
        }

        internal Quaternion getCameraQuaternion() {
            Transform tc = Camera.main.transform;
            Vector3 camAngle = tc.rotation.eulerAngles;

            camAngle.x = 0;
            camAngle.z = 0;

            Quaternion cameraAngle = Quaternion.Euler(camAngle);

            return cameraAngle;
        }

        private EventTrigger.Entry getScrollTrigger() {
            //TODO: move to buttonmanager
            EventTrigger.Entry scroll = new EventTrigger.Entry();
            scroll.eventID = EventTriggerType.Scroll;
            scroll.callback.AddListener((data) => {
                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) {
                    return; //CTRL just messes up selection, better to do nothing
                }

                float scrollRate = ((PointerEventData) data).scrollDelta.y;

                bool shiftDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                bool altDown = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
                if (fkManagerService.ActiveBone != null) {
                    if (scrollRate > 0) {
                        if (shiftDown)
                            fkManagerService.multiUp();
                        else if (altDown)
                            fkManagerService.up();
                        else
                            fkManagerService.slideUp();
                    }

                    if (scrollRate < 0) {
                        if (shiftDown)
                            fkManagerService.multiDown();
                        else if (altDown)
                            fkManagerService.down();
                        else
                            fkManagerService.slideDown();
                    }
                }
            });
            return scroll;
        }

        private void disableButton(Button button) {
            if (button == null) {
                return;
            }

            button.interactable = false;
            button.GetComponentInChildren<Text>().color = Color.gray;
        }

        private void enableButton(Button button) {
            button.interactable = true;
            button.GetComponentInChildren<Text>().color = Color.black;
        }

        public static void lg(string logEntry) {
            MoveCtrlPlugin.Log.LogError(DateTime.Now + ": " + logEntry);
        }

        public void HackTheWorld(Texture2D icon) {
            var studioScene = FindObjectOfType<StudioScene>();
            if (studioScene == null) {
                return;
            }

            var inputInfo = typeof(StudioScene).GetField("inputInfo", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(studioScene);
            var assembly = typeof(StudioScene).Assembly;
            Type type = assembly.GetType("StudioScene+InputInfo");
            var field = type.GetField("button", BindingFlags.Public | BindingFlags.Instance);

            var controllerButton = (Button) field?.GetValue(inputInfo);

            controllerButton.interactable = true;
            controllerButton.onClick = new Button.ButtonClickedEvent();
            controllerButton.onClick.AddListener(() => {
                IsVisible = !IsVisible;
                GUI.gameObject.SetActive(IsVisible);
                controllerButton.image.color = IsVisible ? Color.green : Color.white;
                float scale = GuiScale.Value;
                GUI.scaleFactor = scale * GuiFactor;
                //TODO: check if window is off screen and move back    
            });

            MoveCtrlButtonImage = controllerButton.targetGraphic as Image;
            if (MoveCtrlButtonImage != null) {
                MoveCtrlButtonImage.sprite = Sprite.Create(icon, new Rect(0f, 0f, 32, 32), new Vector2(16, 16));
            }

            field.SetValue(inputInfo, Instantiate(controllerButton));
        }

        private void SpawnGUI() {
            AssetBundle bundle = AssetBundle.LoadFromMemory(Resources.objmoveresources);

            //Load icon
            Texture2D icon = bundle.LoadAsset<Texture2D>("Icon-DXT1");

            GUI = Instantiate(bundle.LoadAsset<GameObject>("MoveCanvas")).GetComponent<Canvas>();
            GUI.gameObject.SetActive(IsVisible);
            bundle.Unload(false);

            GUI.scaleFactor = GuiFactor;

            RectTransform bg = (RectTransform) GUI.transform.Find("MovePanel");
            MovableWindow mw = bg.gameObject.AddComponent<MovableWindow>();
            mw.toDrag = bg;
            mw.preventCameraControl = true;

            ButtonManager buttonManager = new ButtonManager(fkManagerService);
            ButtonActionManager buttonActionManager = new ButtonActionManager(moveObjectService, fkManagerService, this, undoRedoService);

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
            disableButton(AnimControlButton);

            ResetFkButton = buttonManager.ClickButton(GUI.transform.Find("MovePanel/ResetFk").GetComponent<Button>(), buttonActionManager.ResetFk());
            disableButton(ResetFkButton);

            buttonManager.slider(GUI.transform.Find("MovePanel/FactorSlider").GetComponent<Slider>(), buttonActionManager.updateSpeedFactors());
            buttonManager.slider(GUI.transform.Find("MovePanel/FKSizeSlider").GetComponent<Slider>(), buttonActionManager.updateFkScale());

            GUI.gameObject.AddComponent<EventTrigger>();
            EventTrigger trigger = GUI.gameObject.GetComponent<EventTrigger>();
            trigger.triggers.Add(buttonManager.getScrollTrigger());

            //use reflection to hack the button
            HackTheWorld(icon);
        }
    }
}