using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using CharaCustom;
using HarmonyLib;
using KKAPI.Maker;
using KKAPI.Utilities;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GravureAI {
    [BepInDependency("marco.kkapi", "1.9.2")]
    [BepInPlugin(GUID, "Gravure plugin", VERSION)]
    [BepInProcess(PROCESS)]
    public class GravurePlugin : BaseUnityPlugin {
#if HS2
        public const string PROCESS = "HoneySelect2";
#elif AI
        public const string PROCESS = "AI-Syoujyo";
#endif
        public const string GUID = "mikke.gravureAI";
        internal const string VERSION = "1.4.1";

        private CustomCharaWindow saveWindow;
        private Button overWriteButton;
        private RawImage cardImage;

        private Canvas confCanvas;
        private Button keepButton;
        private Button yesButton;
        private Text yesButtonText;
        private static Canvas GravureCanvas;
        private static Vector3 DefaultPosition = Vector3.zero;
        private static Transform Panel;

        private bool started;

        private int gravureIndex = 0;
        private int animeControllerIndex1D = 0; // first dimension index for animaController array
        private string currentLoadedController = "";


        public static ConfigEntry<KeyboardShortcut> ShortKey { get; private set; }

        public void Start() {
            ShortKey = Config.Bind("General", "Show gravure panel", new KeyboardShortcut(KeyCode.G));
        }

        private void Awake() {
            Harmony.CreateAndPatchAll(typeof(GravurePlugin));
        }

        [HarmonyPostfix, HarmonyPatch(typeof(CvsCaptureMenu), nameof(CvsCaptureMenu.BeginCapture))]
        public static void CvsCaptureMenu_BeginCapture_Patch() {
            GravureCanvas?.gameObject.SetActive(true);
            if (DefaultPosition != Vector3.zero) {
                Panel.position = DefaultPosition;
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(CvsCaptureMenu), nameof(CvsCaptureMenu.EndCapture))]
        public static void CvsCaptureMenu_EndCapture_Patch() {
            GravureCanvas?.gameObject.SetActive(false);
        }

        private void SetUpCopyButton() {
            var clothesWin = GameObject.Find("C_Clothes");
            if (clothesWin == null) {
                return;
            }

            var setting1 = clothesWin.transform.Find("Setting/Setting01");
            var setting2 = clothesWin.transform.Find("Setting/Setting02");
            var setting3 = clothesWin.transform.Find("Setting/Setting03");
            var setting4 = clothesWin.transform.Find("Setting/Setting04");

            var defColButton = setting1.transform.Find("DefaultColor");

            var colorSet2 = setting2.transform.Find("Scroll View/Viewport/Content/ColorSet").GetComponent<CustomColorSet>();
            var colorSet3 = setting3.transform.Find("Scroll View/Viewport/Content/ColorSet").GetComponent<CustomColorSet>();
            var colorSet4 = setting4.transform.Find("Scroll View/Viewport/Content/ColorSet").GetComponent<CustomColorSet>();

            FindSliders(setting2, out var glossSet2, out var textuSet2);
            FindSliders(setting3, out var glossSet3, out var textuSet3);
            FindSliders(setting4, out var glossSet4, out var textuSet4);

            var copyColorButtonTransform = NewCopyColorButton(setting2, defColButton);
            var copyColorButton = copyColorButtonTransform.transform.Find("Button").GetComponent<Button>();

            copyColorButton.onClick.AddListener(() => {
                var myColor = colorSet2.image.color;
                colorSet3.SetColor(myColor);
                colorSet3.actUpdateColor.Invoke(myColor);
                CopySliderValue(glossSet2, glossSet3);
                CopySliderValue(textuSet2, textuSet3);

                colorSet4.SetColor(myColor);
                colorSet4.actUpdateColor(myColor);
                CopySliderValue(glossSet2, glossSet4);
                CopySliderValue(textuSet2, textuSet4);
            });
            copyColorButtonTransform.gameObject.SetActive(true);

            copyColorButtonTransform = NewCopyColorButton(setting3, defColButton);
            copyColorButton = copyColorButtonTransform.transform.Find("Button").GetComponent<Button>();
            copyColorButton.onClick.AddListener(() => {
                var myColor = colorSet3.image.color;
                colorSet2.SetColor(myColor);
                colorSet2.actUpdateColor.Invoke(myColor);
                CopySliderValue(glossSet3, glossSet2);
                CopySliderValue(textuSet3, textuSet2);


                colorSet4.SetColor(myColor);
                colorSet4.actUpdateColor(myColor);
                CopySliderValue(glossSet3, glossSet4);
                CopySliderValue(textuSet3, textuSet4);
            });
            copyColorButtonTransform.gameObject.SetActive(true);

            copyColorButtonTransform = NewCopyColorButton(setting4, defColButton);
            copyColorButton = copyColorButtonTransform.transform.Find("Button").GetComponent<Button>();
            copyColorButton.onClick.AddListener(() => {
                var myColor = colorSet4.image.color;

                colorSet3.SetColor(myColor);
                colorSet3.actUpdateColor.Invoke(myColor);
                CopySliderValue(glossSet4, glossSet2);
                CopySliderValue(textuSet4, textuSet2);

                colorSet2.SetColor(myColor);
                colorSet2.actUpdateColor(myColor);
                CopySliderValue(glossSet4, glossSet3);
                CopySliderValue(textuSet4, textuSet3);
            });
            copyColorButtonTransform.gameObject.SetActive(true);
        }

        private void FindSliders(Transform setting2, out CustomSliderSet glossSet, out CustomSliderSet textuSet) {
            var sliders = setting2.transform.Find("Scroll View/Viewport/Content").GetComponentsInChildren<CustomSliderSet>();
            if (sliders == null || sliders.Length < 2) {
                Logger.LogError("Did not find gloss and texture sliders");
            }

            glossSet = sliders[0];
            textuSet = sliders[1];
        }

        private void CopySliderValue(CustomSliderSet source, CustomSliderSet target) {
            float value = source.slider.value;
            target.slider.value = value;
            target.onChange.Invoke(value);
        }

        private static Transform NewCopyColorButton(Transform parentTabTransform, Transform originalButton) {
            var sView = parentTabTransform.transform.Find("Scroll View");
            var rect = sView.GetComponent<RectTransform>();
            var sizeDelta = rect.sizeDelta;
            sizeDelta = new Vector2(sizeDelta.x, sizeDelta.y - 45);
            rect.sizeDelta = sizeDelta;
            var pos = rect.localPosition;
            pos = new Vector3(pos.x, pos.y + 23, pos.z);
            rect.localPosition = pos;
            var newButton = Instantiate(originalButton, parentTabTransform);

            var text = newButton.transform.Find("Button/Text").GetComponent<Text>();
            text.text = "Set all colors";

            return newButton;
        }

        public void OnLevelWasLoaded() {
            started = false;

            var saveDel = GameObject.Find("O_SaveDelete");
            if (saveDel == null) {
                return;
            }

            SetUpCopyButton();

            saveWindow = saveDel.GetComponent<CustomCharaWindow>();
            LoadGui();

            var sb = saveDel.transform.Find("buttons/btnSave");

            var ob = saveDel.transform.Find("buttons/btnOverwrite");
            overWriteButton = ob.GetComponent<Button>();

            var newBut = Instantiate(sb, sb.parent);
            var position = newBut.position;
            position = new Vector3(position.x + 70, position.y - 110, position.z);
            newBut.position = position;
            keepButton = newBut.GetComponent<Button>();
            var text = keepButton.transform.Find("Text").GetComponent<Text>();
            text.text = "Save (Keep Card)";

            var rect = newBut.GetComponent<RectTransform>();
            var sizeDelta = rect.sizeDelta;
            sizeDelta = new Vector2(sizeDelta.x + 100, sizeDelta.y);
            rect.sizeDelta = sizeDelta;

            newBut.gameObject.SetActive(true);

            keepButton.onClick.AddListener(() => ShowConfPanel(() => Save(), "Overwrite"));
            keepButton.interactable = false;
            started = true;
        }


        private void Save() {
            var chaInfo = saveWindow.cscChara.selectInfo?.info;
            if (chaInfo != null) {
                var png = File.ReadAllBytes(chaInfo.FullPath);

                var tex = new Texture2D(2, 2);
                tex.LoadImage(png);

                var chompedPng = tex.EncodeToPNG();

                var chaFile = MakerAPI.GetCharacterControl().chaFile;
                chaFile.pngData = chompedPng;
                chaFile.SaveCharaFile(chaInfo.FullPath, (byte) chaInfo.sex);
            }

            confCanvas.gameObject.SetActive(false);
        }

        private void ShowConfPanel(UnityAction action, string yesText) {
            confCanvas.gameObject.SetActive(true);
            yesButtonText.text = yesText;
            yesButton.onClick.ActuallyRemoveAllListeners();
            yesButton.onClick.AddListener(action);


            var SaveWindow = GameObject.Find("O_SaveDelete").GetComponent<CustomCharaWindow>();
            var chaInfo = SaveWindow.cscChara.selectInfo?.info;
            if (chaInfo != null) {
                var png = File.ReadAllBytes(chaInfo.FullPath);

                var tex = new Texture2D(2, 2);
                tex.LoadImage(png);
                cardImage.texture = tex;
            }
        }

        private void LoadGui() {
            AssetBundle bundle = AssetBundle.LoadFromMemory(GravureRes.gravureres);
            confCanvas = Instantiate(bundle.LoadAsset<GameObject>("ConfCanvas")).GetComponent<Canvas>();
            confCanvas.gameObject.SetActive(false);

            GravureCanvas = Instantiate(bundle.LoadAsset<GameObject>("GravureCanvas")).GetComponent<Canvas>();
            GravureCanvas.gameObject.SetActive(false);
            Panel = GravureCanvas.transform.Find("Panel");

            bundle.Unload(false);

            MovableWindow.makeWindowMovable((RectTransform) Panel);

            for (int i = 0; i < 8; i++) {
                var panelName = "Panel/ClothState (" + i + ")";
                var id = i;

                var text = GravureCanvas.transform.Find(panelName + "/Text").GetComponent<Text>();
                text.text = clothNames[i];
                
                var onButton = GravureCanvas.transform.Find(panelName + "/ButtonOn").GetComponent<Button>();
                onButton.onClick.AddListener(() => MakerAPI.GetCharacterControl().SetClothesState(id, 0));
                var offButton = GravureCanvas.transform.Find(panelName + "/ButtonOff").GetComponent<Button>();
                offButton.onClick.AddListener(() => MakerAPI.GetCharacterControl().SetClothesState(id, 2));
                var halfButton = GravureCanvas.transform.Find(panelName + "/ButtonHalf").GetComponent<Button>();
                halfButton.onClick.AddListener(() => MakerAPI.GetCharacterControl().SetClothesState(id, 1));
            }

            Anims();
            Sliders();
            SetUpAnimateButton();

            var yes = confCanvas.transform.Find("ShadePanel/YesButton");
            yesButton = yes.gameObject.GetComponent<Button>();
            yesButtonText = yes.Find("Text").GetComponent<Text>();

            cardImage = confCanvas.transform.Find("ShadePanel/Card").GetComponent<RawImage>();
        }

        private String[] clothNames = {"Top", "Bottom", "Inner Top", "Inner Bot", "Gloves", "Pantyhose", "Socks", "Shoes"};
        
        private void SetUpAnimateButton() {
            var animCtrl = GravureCanvas.transform.Find("Panel/AnimControl");
            var trigger = animCtrl.gameObject.AddComponent<EventTrigger>();

            EventTrigger.Entry entryDrag = new EventTrigger.Entry();
            entryDrag.eventID = EventTriggerType.Drag;
            entryDrag.callback.AddListener((data) => {
                ((PointerEventData) data).useDragThreshold = false;
                DragAnimation();
            });
            trigger.triggers.Add(entryDrag);
        }

        private void DragAnimation() {
            float input = Input.GetAxis("Mouse X");
            Animator animator = MakerAPI.GetCharacterControl().animBody;

            if (animator == null) return;

            var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            var clipInfo = animator.GetCurrentAnimatorClipInfo(0);

            var normalizedTimeSkip = (input * 0.15f) / clipInfo[0].clip.length;
            var normalizedTime = stateInfo.normalizedTime + normalizedTimeSkip;

            if (stateInfo.loop == false) {
                if (normalizedTime < 0) normalizedTime = 0;
                if (normalizedTime > 1) normalizedTime = 1;
            }

            animator.Play(0, 0, normalizedTime);
        }

        private String[] SliderNames = {"Wetness", "Blush", "Skin Gloss", "Nipples", "Tears"};
        
        private void Sliders() {
            var slider = GravureCanvas.transform.Find("Panel/StateSlider (0)/Slider").GetComponent<Slider>();
            slider.onValueChanged.AddListener(rate => { MakerAPI.GetCharacterControl().wetRate = rate; });

            slider = GravureCanvas.transform.Find("Panel/StateSlider (1)/Slider").GetComponent<Slider>();
            slider.onValueChanged.AddListener(rate => { MakerAPI.GetCharacterControl().ChangeHohoAkaRate(rate); });

            slider = GravureCanvas.transform.Find("Panel/StateSlider (2)/Slider").GetComponent<Slider>();
            slider.onValueChanged.AddListener(rate => { MakerAPI.GetCharacterControl().skinGlossRate = rate; });

            slider = GravureCanvas.transform.Find("Panel/StateSlider (3)/Slider").GetComponent<Slider>();
            slider.onValueChanged.AddListener(rate => { MakerAPI.GetCharacterControl().ChangeNipRate(rate); });

            slider = GravureCanvas.transform.Find("Panel/StateSlider (4)/Slider").GetComponent<Slider>();
            slider.onValueChanged.AddListener(rate => { MakerAPI.GetCharacterControl().ChangeTearsRate(rate); });

            for (int index = 0; index < 5; index++) {
                var text = GravureCanvas.transform.Find("Panel/StateSlider ("+index+")/Text").GetComponent<Text>();
                text.text = SliderNames[index];
            }
            
        }

        private String[] AnimGroups = {
            "Gravure",
            "Action",
            "Dance",
            "Fighting",
            "Walking",
            "Pose",
            "BaseAction1",
            "BaseAction2",
            "Sleeping"
        };

        // Animation controllers
        private string[,] animeController = new[,] {
            {"studio/anime/03.unity3d", "gravure", "abdata", "tachi_00"},
            {"studio/anime/08.unity3d", "action_08", "abdata", "dance_00"},
            {"studio/anime/03.unity3d", "dance", "abdata", "poledance_00"},
            {"studio/anime/08.unity3d", "f_cutin", "abdata", "FightA_cutin_00"},
            {"studio/anime/00.unity3d", "f_studio", "abdata", "mc_f_move_00"},
            {"studio/anime/00.unity3d", "f_studio", "abdata", "mc_f_pose_00_in"},
            {"studio/anime/00.unity3d", "f_studio", "abdata", "action_00"},
            {"studio/anime/00.unity3d", "f_studio", "abdata", "action_38"},
            {"studio/anime/00.unity3d", "f_studio", "abdata", "sleep_01_00"},
        };

        // Animation clips
        private string[][] gravureAnims = new string[][] {
            new[] {
                // studio/anime/03.unity3d gravure
                "tachi_00",
                "tachi_01",
                "tachi_02",
                "tachi_03",
                "tachi_04",
                "tachi_05",
                "tachi_06",
                "tachi_07",
                "tachi_08",
                "tyugosi_00",
                "tyugosi_01",
                "tyugosi_02",
                "tyugosi_03",
                "tyugosi_04",
                "tyugosi_05",
                "tyugosi_06",
                "suwari_00",
                "suwari_01",
                "suwari_02",
                "suwari_03",
                "suwari_04",
                "suwari_05",
                "suwari_06",
                "suwari_07",
                "suwari_08",
                "suwari_09",
                "suwari_10",
                "ne_00",
                "ne_01",
                "ne_02",
                "ne_03",
                "ne_04",
                "ne_05",
                "ne_06",
                "ne_07",
                "isu_00",
                "isu_01",
                "isu_02"
            },
            new[] {
                // studio/anime/08.unity3d action_08
                "dance_00",
                "action_00",
                "action_01",
                "action_02",
                "action_03",
                "action_04",
                "action_05",
                "action_06",
                "action_07",
                "action_08",
                "action_09",
                "action_10",
                "action_11",
                "action_12",
                "action_13",
                "action_14",
                "action_15",
                "action_16",
                "action_17",
                "action_18",
                "action_19",
                "action_20",
                "action_21",
                "action_22",
                "action_23",
                "action_24",
                "action_25",
                "action_26",
                "action_27"
            },
            new[] {
                // studio/anime/03.unity3d dance
                "poledance_00",
                "sexydance_00",
                "dance_00",
                "dance_01",
                "dance_02"
            },
            new[] {
                // studio/anime/08.unity3d f_cutin
                "FightA_cutin_00",
                "FightA_cutin_01",
                "FightA_cutin_02",
                "FightB_cutin_00",
                "FightB_cutin_01",
                "FightB_cutin_02",
                "AuraShot_cutin_00",
                "AuraShot_cutin_01",
                "AuraShot_cutin_02",
                "Sword_cutin_00",
                "Sword_cutin_01",
                "Sword_cutin_02",
                "ShieldSword_cutin_00",
                "ShieldSword_cutin_01",
                "ShieldSword_cutin_02",
                "Dagger1_cutin_00",
                "Dagger1_cutin_01",
                "Dagger1_cutin_02",
                "Dagger2_cutin_00",
                "Dagger2_cutin_01",
                "Dagger2_cutin_02",
                "Gun1_cutin_00",
                "Gun1_cutin_01",
                "Gun1_cutin_02",
                "Gun2_cutin_00",
                "Gun2_cutin_01",
                "Gun2_cutin_02",
                "Throwing_cutin_00",
                "Throwing_cutin_01",
                "Throwing_cutin_02",
                "MagicWand_cutin_00",
                "MagicWand_cutin_01",
                "MagicWand_cutin_02",
                "MagicBook_cutin_00",
                "MagicBook_cutin_01",
                "MagicBook_cutin_02",
                "LargeSword_cutin_00",
                "LargeSword_cutin_01",
                "LargeSword_cutin_02",
                "Rod_Spear_cutin_00",
                "Rod_Spear_cutin_01",
                "Rod_Spear_cutin_02",
                "Hammer_Sickle_cutin_00",
                "Hammer_Sickle_cutin_01",
                "Hammer_Sickle_cutin_02",
                "Rifle_cutin_00",
                "Rifle_cutin_01",
                "Rifle_cutin_02",
                "Bow_cutin_00",
                "Bow_cutin_01",
                "Bow_cutin_02"
            },
            new[] {
                // studio/anime/00.unity3d f_studio walking
                "mc_f_move_00",
                "mc_f_move_01",
                "mc_f_move_02",
                "mc_f_move_03",
                "mc_f_move_04",
                "mc_f_move_05",
                "mc_f_move_06"
            },
            new[] {
                // studio/anime/00.unity3d f_studio pose
                "mc_f_pose_00_in",
                "mc_f_pose_01_in",
                "mc_f_pose_02_in",
                "mc_f_pose_03_in",
                "mc_f_pose_04_in",
                "mc_f_pose_05_in",
                "mc_f_pose_06_in",
                "mc_f_pose_07_in",
            },
            new[] {
                // studio/anime/00.unity3d f_studio base1
                "action_00",
                "action_01",
                "action_02",
                "action_03",
                "action_04",
                "action_05_in",
                "action_06",
                "action_07",
                "action_08",
                "action_09",
                "action_10",
                "action_11_in",
                "action_12",
                "action_13",
                "action_14",
                "action_17",
                "action_18",
                "action_19",
                "action_20",
                "action_21",
                "action_22",
                "action_23",
                "action_25",
                "action_26",
                "action_27",
                "action_30",
                "action_32",
            },
            new[] {
                // studio/anime/00.unity3d f_studio base2
                "action_38",
                "action_39",
                "action_40",
                "action_44",
                "action_45",
                "action_47",
                "action_48",
                "action_52",
                "action_55",
                "action_56_L_loop",
                "action_57",
                "action_58",
                "action_59_in",
                "action_60",
                "action_61",
                "action_62",
                "action_63",
                "action_64",
                "action_66_in",
                "action_70",
                "action_74",
                "adv_action_00_00",
                "adv_action_00_01",
                "adv_action_00_02",
                "adv_action_00_03",
                "adv_action_00_04",
                "adv_action_00_05_in",
                "adv_action_00_06",
                "adv_action_00_08",
            },
            new[] {
                // studio/anime/00.unity3d f_studio sleeping
                "sleep_01_00",
                "sleep_01_01",
                "sleep_02_00",
                "sleep_02_01",
                "sleep_03",
                "sleep_04",
                "sleep_05",
                "sleep_06",
                "sleep_07_in",
                "sleep_08",
                "sleep_09",
                "sleep_10",
                "sleep_11_in",
                "sleep_14_in",
                "sleep_15_in",
            }
        };

        private void Anims() {
            var nextAnimButton = GravureCanvas.transform.Find("Panel/NextAnim").GetComponent<Button>();
            nextAnimButton.onClick.AddListener(() => {
                // set animation clip index to next clip in current set and play clip
                gravureIndex = (gravureIndex + 1) % gravureAnims[animeControllerIndex1D].Length;
                MakerAPI.GetCharacterControl().AnimPlay(gravureAnims[animeControllerIndex1D][gravureIndex]);
            });

            var prevAnimButton = GravureCanvas.transform.Find("Panel/PrevAnim").GetComponent<Button>();
            prevAnimButton.onClick.AddListener(() => {
                gravureIndex = ((gravureIndex - 1) % gravureAnims[animeControllerIndex1D].Length + gravureAnims[animeControllerIndex1D].Length) % gravureAnims[animeControllerIndex1D].Length;
                MakerAPI.GetCharacterControl().AnimPlay(gravureAnims[animeControllerIndex1D][gravureIndex]);
            });

            nextAnimButton.interactable = false;
            prevAnimButton.interactable = false;

            var dropdownTra = GravureCanvas.transform.Find("Panel/Dropdown");
            var dropdown = dropdownTra.GetComponent<Dropdown>();

            
            var options = new List<Dropdown.OptionData>();
            foreach (String animGroup in AnimGroups) {
                options.Add(new Dropdown.OptionData(animGroup));
            }

            dropdown.ClearOptions();
            dropdown.AddOptions(options);
            dropdown.gameObject.SetActive(false);
            dropdown.onValueChanged.AddListener(opt => {
                animeControllerIndex1D = opt;
                gravureIndex = 0;
                PlayStudioAnim();
            });

            var gravureButton = GravureCanvas.transform.Find("Panel/ButtonGravure").GetComponent<Button>();
            gravureButton.onClick.AddListener(() => {
                nextAnimButton.interactable = true;
                prevAnimButton.interactable = true;
                dropdown.gameObject.SetActive(true);
                PlayStudioAnim();
            });
            var defaultAnimButton = GravureCanvas.transform.Find("Panel/ButtonDefaultAnim").GetComponent<Button>();
            defaultAnimButton.onClick.AddListener(() => {
                nextAnimButton.interactable = false;
                prevAnimButton.interactable = false;
                dropdown.gameObject.SetActive(false);

                DefaultAnim();
            });
        }

        private void PlayStudioAnim() {
            MakerAPI.GetCharacterControl().LoadAnimation(
                animeController[animeControllerIndex1D, 0],
                animeController[animeControllerIndex1D, 1],
                animeController[animeControllerIndex1D, 2]
            );
            this.currentLoadedController = animeController[animeControllerIndex1D, 1];
            MakerAPI.GetCharacterControl().AnimPlay(gravureAnims[animeControllerIndex1D][gravureIndex]);
        }

        private void Update() {
            if (started) {
                keepButton.interactable = overWriteButton.interactable;
                if (ShortKey.Value.IsDown()) {
                    GravureCanvas?.gameObject.SetActive(!GravureCanvas.gameObject.activeInHierarchy);
                    if (DefaultPosition == Vector3.zero) {
                        DefaultPosition = Panel.position;
                    }
                }
            }
        }

        private static void DefaultAnim() {
#if HS2
            var custombase = Singleton<CustomBase>.Instance;
            int poseNo = custombase.poseNo;
            custombase.ChangeAnimationNo(20);
            custombase.ChangeAnimationNo(2);
            custombase.ChangeAnimationNo(1);
            custombase.ChangeAnimationNo(poseNo);
#elif AI
            if (MakerAPI.GetCharacterControl().sex != 0) {
                MakerAPI.GetCharacterControl().LoadAnimation("custom/00/anim_f_00.unity3d", "edit_f", "abdata");
            } else {
                MakerAPI.GetCharacterControl().LoadAnimation("custom/00/anim_m_00.unity3d", "edit_m", "abdata");
            }
#endif
        }
    }
}