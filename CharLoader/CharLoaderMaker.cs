using System.Collections.Generic;
using System.IO;
using System.Reflection;
using AIChara;
using BepInEx.Configuration;
using CharaCustom;
using HarmonyLib;
using KKAPI.Maker;
using UnityEngine;
using UnityEngine.UI;

namespace CharLoader {
    public class CharLoaderMaker : MonoBehaviour {
        private static Canvas GUI;
        private static CustomCharaWindow CustomCharaSave;
        private static CustomCharaWindow CustomCharaLoad;
        private static GameObject CvsOCharaSave;
        private static GameObject CvsOCharaLoad;

        private static MethodInfo CharaCustomInfoAssistAddList;

        private static DirectoryInfo CurrentDir;

        private Button saveVariantButton;
        private Button overWriteButton;

        private static string VariantName = "";
        private static bool IsLoad;
        private static bool IsActive;

        private static DirectoryInfo BaseDir;
        private static ConfigEntry<bool> ShowMakerFolders;

        internal void SpawnGui(ConfigEntry<bool> showMakerFolders) {
            var saveDel = GameObject.Find("O_SaveDelete");
            if (saveDel == null) {
                return;
            }

            ShowMakerFolders = showMakerFolders;
            CurrentDir = null;
            CvsOCharaSave = saveDel;
            CvsOCharaLoad = GameObject.Find("O_Load");

            CustomCharaSave = CvsOCharaSave.GetComponent<CustomCharaWindow>();
            CustomCharaLoad = CvsOCharaLoad.GetComponent<CustomCharaWindow>();

            AssetBundle bundle = AssetBundle.LoadFromMemory(CharLoaderRes.loaderres);
            GUI = Instantiate(bundle.LoadAsset<GameObject>("MakerCanvas")).GetComponent<Canvas>();
            GUI.gameObject.SetActive(false);

            bundle.Unload(false);

            HackyStuff();
            SetUpVariantButton(CvsOCharaSave);
            SetUpFolderButtons();
        }

        private void SetUpFolderButtons() {
            var variantButton = GUI.transform.Find("MainPanel/PanelHeader/VariantButton");
            variantButton.GetComponent<Button>().onClick.AddListener(() => LoadCurrentVariantDir());
            var rootButton = GUI.transform.Find("MainPanel/PanelHeader/RootButton");
            rootButton.GetComponent<Button>().onClick.AddListener(() => {
                CurrentDir = BaseDir;
                LoadDirList(CurrentDir);
                ShowCharacters(CurrentDir);
            });
        }

        private void LoadCurrentVariantDir() {
            var chaInfo = (IsLoad ? CustomCharaLoad : CustomCharaSave).cscChara.selectInfo?.info;
            if (chaInfo == null) return;
            var variantName = chaInfo?.name;
            var variantDir = new DirectoryInfo(BaseDir.FullName + @"\" + CharLoaderPlugin.VariantsDirName + @"\" + variantName);
            if (!variantDir.Exists) return;
            CurrentDir = variantDir;
            LoadDirList(CurrentDir);
            ShowCharacters(CurrentDir);
        }


        private void SetUpVariantButton(GameObject saveDel) {
            var sb = saveDel.transform.Find("buttons/btnDelete");

            var ob = saveDel.transform.Find("buttons/btnOverwrite");
            overWriteButton = ob.GetComponent<Button>();

            var newBut = Instantiate(sb, sb.parent);
            var position = newBut.position;
            position = new Vector3(position.x, position.y - 110, position.z);
            newBut.position = position;
            saveVariantButton = newBut.GetComponent<Button>();
            var text = saveVariantButton.transform.Find("Text").GetComponent<Text>();
            text.text = "Save Variant";

            var rect = newBut.GetComponent<RectTransform>();
            var sizeDelta = rect.sizeDelta;
            sizeDelta = new Vector2(sizeDelta.x + 70, sizeDelta.y);
            rect.sizeDelta = sizeDelta;

            newBut.gameObject.SetActive(true);

            saveVariantButton.onClick.AddListener(() => {
                var chaInfo = CustomCharaSave.cscChara.selectInfo?.info;
                VariantName = chaInfo?.name;
                CustomCharaSave.onClick02.Invoke(new CustomCharaFileInfo());
            });
            saveVariantButton.interactable = true;
        }

        private void HackyStuff() {
            CharaCustomInfoAssistAddList = typeof(CustomCharaFileInfoAssist).GetMethod("AddList", BindingFlags.NonPublic | BindingFlags.Static);

            var tglOption = GameObject.Find("tglOption").GetComponent<Toggle>();
            tglOption.onValueChanged.AddListener(arg0 => GuiSetActive(arg0 && IsActive));

            var fusionButton = GameObject.Find("Fusion").GetComponent<UI_ButtonEx>();
            fusionButton.onClick.AddListener(() => {
                IsActive = false;
                GuiSetActive(false);
            });
        }

        private static void GuiSetActive(bool isActive) {
            GUI?.gameObject.SetActive(isActive && ShowMakerFolders.Value);
        }

        private static void LoadDirList(DirectoryInfo dirInfo) {
            CurrentDir = dirInfo;
            var dirButtonPrefab = GUI.transform.Find("DirButton").GetComponent<Button>();

            var content = GUI.transform.Find("MainPanel/Body/DirView/Viewport/Content").GetComponent<Transform>();
            foreach (Transform child in content) {
                Destroy(child.gameObject);
            }

            CustomCharaSave.SelectInfoClear();
            CustomCharaLoad.SelectInfoClear();

            if (!dirInfo.FullName.ToLower().Equals(BaseDir.FullName.ToLower())) {
                Button parentDirButton = Instantiate(dirButtonPrefab.gameObject).GetComponent<Button>();
                parentDirButton.transform.Find("Text").GetComponent<Text>().text = "..";

                parentDirButton.onClick.AddListener((() => {
                    LoadDirList(dirInfo.Parent);
                    ShowCharacters(dirInfo.Parent);
                }));
                parentDirButton.gameObject.SetActive(true);
                parentDirButton.transform.SetParent(content.gameObject.transform, false);
            }

            foreach (var subdir in dirInfo.GetDirectories()) {
                Button dirButton = Instantiate(dirButtonPrefab.gameObject).GetComponent<Button>();
                var text = dirButton.transform.Find("Text").GetComponent<Text>();
                text.text = subdir.Name;

                dirButton.onClick.AddListener(() => {
                    LoadDirList(subdir);
                    ShowCharacters(subdir);
                });

                dirButton.gameObject.SetActive(true);
                dirButton.transform.SetParent(content.gameObject.transform, false);
            }
        }

        private static void ShowCharacters(DirectoryInfo dir) {
            var charaList = new List<CustomCharaFileInfo>();
            int idx = 0;
            byte sex = (byte) MakerAPI.GetMakerSex();
            var prms = new object[] {charaList, dir.FullName, sex, true, true, false, true, idx};

            CharaCustomInfoAssistAddList.Invoke(null, prms);
            CustomCharaSave.UpdateWindow(Singleton<CustomBase>.Instance.modeNew, sex, true, charaList);
            
            charaList = new List<CustomCharaFileInfo>();
            prms = new object[] {charaList, dir.FullName, sex, true, true, false, false, idx};
            CharaCustomInfoAssistAddList.Invoke(null, prms);
            CustomCharaLoad.UpdateWindow( Singleton<CustomBase>.Instance.modeNew, sex, false, charaList);
            //TODO: fusion?
        }

        public void ToggleActive() {
            GuiSetActive(!GUI.gameObject.activeInHierarchy);
        }

        private void Update() {
            if (MakerAPI.InsideAndLoaded) {
                var interactable = overWriteButton?.interactable;
                if (interactable != null) {
                    saveVariantButton.interactable = interactable.Value;
                } // TODO: find a smarter solution for this
            }
        }

        private static string GetTargetDir() {
            if (string.IsNullOrEmpty(VariantName)) {
                if (CurrentDir == null) {
                    return "";
                }
                return CurrentDir.FullName + "\\";
            }

            string path = BaseDir.FullName + @"\" + CharLoaderPlugin.VariantsDirName;
            DirectoryInfo directoryInfo = new DirectoryInfo(path);
            if (!directoryInfo.Exists) {
                directoryInfo.Create();
            }

            path = path + @"\" + VariantName;
            directoryInfo = new DirectoryInfo(path);
            if (!directoryInfo.Exists) {
                directoryInfo.Create();
            }

            VariantName = null;
            return directoryInfo.FullName + "\\";
        }

        private void Awake() {
            Harmony.CreateAndPatchAll(typeof(CharLoaderMaker));
        }

        [HarmonyPostfix, HarmonyPatch(typeof(CvsO_CharaLoad), "UpdateCharasList")]
        public static void Charaload_Update_Patch() {
            BaseDir = MakerAPI.GetMakerSex() == 1 ? CharLoaderPlugin.FemaleBaseDir : CharLoaderPlugin.MaleBaseDir;
            if (CurrentDir == null) {
                CurrentDir = BaseDir;
            }

            LoadDirList(CurrentDir);
            ShowCharacters(CurrentDir);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(CvsO_CharaLoad), "ChangeMenuFunc")]
        public static void CharaLoad_ChangeWin_Patch() {
            IsActive = true;
            IsLoad = true;
            GuiSetActive(true);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(CvsO_CharaSave), "ChangeMenuFunc")]
        public static void CharaSave_ChangeWin_Patch() {
            GuiSetActive(true);
            IsActive = true;
            IsLoad = false;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(CvsBase), nameof(CvsBase.ChangeMenuFunc))]
        public static void CharaBase_ChangeWin_Patch() {
            if (GUI?.gameObject.activeSelf == true) {
                GuiSetActive(false);
                IsActive = false;
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(ChaFileControl),
             nameof(ChaFileControl.SaveCharaFile),
             new[] {typeof(string), typeof(byte), typeof(bool)})]
        public static void SaveCharaFile_Patch(ref string filename, bool newFile) {
            if (MakerAPI.InsideAndLoaded&&!filename.Contains(":")) {
                var bob = GetTargetDir();
                filename = bob + filename;
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(CvsCaptureMenu), nameof(CvsCaptureMenu.BeginCapture))]
        public static void CvsCaptureMenu_BeginCapture_Patch() {
            GuiSetActive(false);
            IsActive = false;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(CvsCaptureMenu), nameof(CvsCaptureMenu.EndCapture))]
        public static void CvsCaptureMenu_EndCapture_Patch() {
            GuiSetActive(true);
            IsActive = true;
        }
    }
}