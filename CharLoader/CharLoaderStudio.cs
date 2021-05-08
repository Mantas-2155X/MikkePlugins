using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using AIChara;
#if HS2
using HS2WearCustom;
#elif AI
using AIWearCustom;
#endif
using KKAPI.Utilities;
using Studio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CharLoader {
    public class CharLoaderStudio : MonoBehaviour {
        private Canvas GUI;

        private GameObject charButton;
        private GameObject menuButton;
        private Toggle closeToggle;
        private Text genderButtonText;

        private bool wearCustomActive;
        private Type studioCharaListUtilType = null;

        internal void SpawnGui(bool replaceStudioButtons, int rows) {
            CheckWearCustom();

            AssetBundle bundle = AssetBundle.LoadFromMemory(CharLoaderRes.loaderres);
            var tull = Instantiate(bundle.LoadAsset<GameObject>("CharLoaderCanvas"));
            GUI = tull.GetComponent<Canvas>();
            GUI.gameObject.SetActive(false);
            charButton = bundle.LoadAsset<GameObject>("CharButton2");
            bundle.Unload(false);

            float rowCount = rows;
            float scalefactor = 1;
            scalefactor = (4f / rowCount);

            var viewport = GUI.transform.Find("MainPanel/Body/CharView/Viewport/Content");
            viewport.GetComponent<GridLayoutGroup>().constraintCount = rows;
            viewport.transform.localScale = new Vector3(scalefactor, scalefactor, 1);


            var folderButton = GUI.transform.Find("MainPanel/PanelHeader/FolderButton").GetComponent<Button>();
            folderButton.onClick.AddListener(() => { Process.Start(currentDir.FullName); });

            var genderButton = GUI.transform.Find("MainPanel/PanelHeader/GenderButton").GetComponent<Button>();
            genderButtonText = genderButton.transform.Find("Text").GetComponent<Text>();
            genderButtonText.text = "Male";
            genderButton.onClick.AddListener(() => ToggleGender());

            var exitButton = GUI.transform.Find("MainPanel/PanelHeader/ExitButton").GetComponent<Button>();
            exitButton.onClick.AddListener(() => { closeAction.Invoke(); });
            closeToggle = GUI.transform.Find("MainPanel/CloseToggle").GetComponent<Toggle>();
            CreateMenuButton(replaceStudioButtons);
            LoadDirList(CharLoaderPlugin.FemaleBaseDir);
        }

        private void CheckWearCustom() {
          

#if HS2 
            studioCharaListUtilType = Type.GetType("HS2WearCustom.StudioCharaListUtil, HS2WearCustom, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
#elif AI
            studioCharaListUtilType = Type.GetType("AIWearCustom.StudioCharaListUtil, AIWearCustom, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
#endif

            if (studioCharaListUtilType == null) {
                wearCustomActive = false;
            } else {
                wearCustomActive = true;
            }
        }

        private bool femaleActive = true;

        private void ToggleGender() {
            if (femaleActive) {
                genderButtonText.text = "Female";
                femaleActive = false;
                LoadDirList(CharLoaderPlugin.MaleBaseDir);
            } else {
                genderButtonText.text = "Male";
                femaleActive = true;
                LoadDirList(CharLoaderPlugin.FemaleBaseDir);
            }
        }

        private void CreateMenuButton(bool replaceStudioButtons) {
            GameObject original = GameObject.Find("StudioScene/Canvas Main Menu/01_Add/Scroll View Add Group/Viewport/Content/Chara Female");
            if (original == null) return;
            menuButton = Instantiate(original, original.transform.parent);
            menuButton.name = "Characters";
            Button component = menuButton.GetComponent<Button>();

            if (replaceStudioButtons) {
                menuButton.transform.SetSiblingIndex(0);
                original.transform.SetParent(null);
                GameObject.Find("StudioScene/Canvas Main Menu/01_Add/Scroll View Add Group/Viewport/Content/Chara Male").transform.SetParent(null);
            }

            var addCtrlType = typeof(AddButtonCtrl);
            var commonInfoType = addCtrlType.GetNestedType("CommonInfo", BindingFlags.NonPublic);

            var commonInfo = Activator.CreateInstance(commonInfoType);
            commonInfoType.GetField("obj").SetValue(commonInfo, GUI.gameObject);
            commonInfoType.GetField("button").SetValue(commonInfo, component);

            AddButtonCtrl addButtonCtrl = GameObject.Find("StudioScene/Canvas Main Menu/01_Add").GetComponent<AddButtonCtrl>();

            var ciField = addCtrlType.GetField("commonInfo", BindingFlags.NonPublic | BindingFlags.Instance);
            var commonInfoList = (Array) ciField.GetValue(addButtonCtrl);

            int lenght = commonInfoList.Length;
            var newList = (Array) Activator.CreateInstance(commonInfoList.GetType(), lenght + 1);
            for (int index = 0; index < lenght; index++) {
                newList.SetValue(commonInfoList.GetValue(index), index);
            }

            newList.SetValue(commonInfo, lenght);
            ciField.SetValue(addButtonCtrl, newList);

            component.onClick.ActuallyRemoveAllListeners();
            component.onClick.AddListener(() => addButtonCtrl.OnClick(lenght));
            menuButton.GetComponentInChildren<TMP_Text>().text = "Characters";

            closeAction = () => { addButtonCtrl.OnClick(lenght); };
        }

        private Action closeAction = () => { };

        private DirectoryInfo currentDir;

        private void LoadDirList(DirectoryInfo dirInfo) {
            currentDir = dirInfo;
            var dirButtonPrefab = GUI.transform.Find("DirButton").GetComponent<Button>();

            var content = GUI.transform.Find("MainPanel/Body/DirView/Viewport/Content").GetComponent<Transform>();
            foreach (Transform child in content) {
                Destroy(child.gameObject);
            }

            Button parentDirButton = Instantiate(dirButtonPrefab.gameObject).GetComponent<Button>();
            parentDirButton.transform.Find("Text").GetComponent<Text>().text = "..";

            parentDirButton.onClick.AddListener((() => LoadDirList(dirInfo.Parent)));
            parentDirButton.gameObject.SetActive(true);
            parentDirButton.transform.SetParent(content.gameObject.transform, false);

            foreach (var subdir in dirInfo.GetDirectories()) {
                Button dirButton = Instantiate(dirButtonPrefab.gameObject).GetComponent<Button>();
                var text = dirButton.transform.Find("Text").GetComponent<Text>();
                text.text = subdir.Name;

                dirButton.onClick.AddListener(() => { LoadDirList(subdir); });

                dirButton.gameObject.SetActive(true);
                dirButton.transform.SetParent(content.gameObject.transform, false);
            }

            ListCharsInDir(dirInfo, false);
        }

        private List<GameObject> buttonPanels = new List<GameObject>();
        private List<GameObject> variantPanels = new List<GameObject>();

        private void ListCharsInDir(DirectoryInfo dirInfo, bool variant) {
            Transform content;
            List<GameObject> panels;

            if (!variant) {
                content = GUI.transform.Find("MainPanel/Body/CharView/Viewport/Content").GetComponent<Transform>();
                panels = buttonPanels;
            } else {
                content = GUI.transform.Find("MainPanel/Body/VarView/Viewport/Content").GetComponent<Transform>();
                panels = variantPanels;
            }

            foreach (Transform child in content) {
                Destroy(child.gameObject);
            }

            panels.Clear();
            if (!dirInfo.Exists) {
                return;
            }

            foreach (var file in dirInfo.GetFiles().OrderByDescending(f => f.LastWriteTime)) {
                if (!file.Extension.ToLower().Equals(".png")) {
                    continue;
                }

                var fullname = "";
                int sex = 1;
                ChaFileControl chaFileControl = new ChaFileControl();
                if (chaFileControl.LoadCharaFile(file.FullName, 1, true)) {
                    fullname = chaFileControl.parameter.fullname;
                    sex = chaFileControl.parameter.sex;
                }

                Button character = Instantiate(charButton).GetComponent<Button>();
                var fileData = File.ReadAllBytes(file.FullName);
                var img = character.GetComponent<RawImage>();
                var tex = new Texture2D(2, 2);
                tex.LoadImage(fileData);
                img.texture = tex;

                var txt = character.transform.Find("ProText").GetComponent<TextMeshProUGUI>();
                txt.text = fullname;

                character.gameObject.SetActive(true);
                character.transform.SetParent(content.gameObject.transform, false);

                panels.Add(character.transform.Find("ButtonPanel").gameObject);

                var loadButton = character.transform.Find("ButtonPanel/LoadButton").GetComponent<Button>();
                var replaceButton = character.transform.Find("ButtonPanel/ReplaceButton").GetComponent<Button>();
                loadButton.onClick.AddListener(() => LoadChara(file.FullName));
                replaceButton.onClick.AddListener(() => ReplaceChara(file.FullName, sex));

                if (wearCustomActive) {
                    var anatomyButton = character.transform.Find("ButtonPanel/AnatomyButton").GetComponent<Button>();
                    anatomyButton.gameObject.SetActive(true);
                    anatomyButton.onClick.AddListener(() => CallWearCustom(file.FullName, anatomy));
                    var outfitBUtton = character.transform.Find("ButtonPanel/OutfitButton").GetComponent<Button>();
                    outfitBUtton.gameObject.SetActive(true);
                    outfitBUtton.onClick.AddListener(() => CallWearCustom(file.FullName, outfit));
                    var clothesButton = character.transform.Find("ButtonPanel/ClothesButton").GetComponent<Button>();
                    clothesButton.gameObject.SetActive(true);
                    clothesButton.onClick.AddListener(() => CallWearCustom(file.FullName, clothes));
                    var accButton = character.transform.Find("ButtonPanel/AccessoriesButton").GetComponent<Button>();
                    accButton.gameObject.SetActive(true);
                    accButton.onClick.AddListener(() => CallWearCustom(file.FullName, accessories));
                    var bodyButton = character.transform.Find("ButtonPanel/BodyButton").GetComponent<Button>();
                    bodyButton.gameObject.SetActive(true);
                    bodyButton.onClick.AddListener(() => CallWearCustom(file.FullName, body));
                    var faceButton = character.transform.Find("ButtonPanel/FaceButton").GetComponent<Button>();
                    faceButton.gameObject.SetActive(true);
                    faceButton.onClick.AddListener(() => CallWearCustom(file.FullName, face));
                    var hairButton = character.transform.Find("ButtonPanel/HairButton").GetComponent<Button>();
                    hairButton.gameObject.SetActive(true);
                    hairButton.onClick.AddListener(() => CallWearCustom(file.FullName, hair));
                }

                character.onClick.AddListener(() => {
                    foreach (var lp in buttonPanels.Concat(variantPanels)) {
                        if (lp != null)
                            lp.SetActive(false);
                    }

                    var panel = character.transform.Find("ButtonPanel").gameObject;
                    panel.SetActive(true);
                    if (!variant)
                        ListCharsInDir(new DirectoryInfo((femaleActive ? CharLoaderPlugin.FemaleBaseDir : CharLoaderPlugin.MaleBaseDir).FullName + @"\" + CharLoaderPlugin.VariantsDirName + @"\" + fullname), true);
                });
            }
        }

        private readonly bool[] anatomy = {true, true, true, false, false};
        private readonly bool[] outfit = {false, false, false, true, true};
        private readonly bool[] body = {true, false, false, false, false};
        private readonly bool[] face = {false, true, false, false, false};
        private readonly bool[] hair = {false, false, true, false, false};
        private readonly bool[] clothes = {false, false, false, true, false};
        private readonly bool[] accessories = {false, false, false, false, true};

        private void CallWearCustom(string fileFullName, bool[] loadState) {
            var chara00 = GameObject.Find("StudioScene/Canvas Main Menu/02_Manipulate/00_Chara");

            var studioCharaListUtil = chara00.GetComponent<StudioCharaListUtil>();
            if (studioCharaListUtil == null) {
                StudioCharaListUtil.Install();
                studioCharaListUtil = chara00.GetComponent<StudioCharaListUtil>();
            }

            var replaceCharaHairOnly = studioCharaListUtilType.GetField("replaceCharaHairOnly", BindingFlags.NonPublic | BindingFlags.Instance);
            var replaceCharaHeadOnly = studioCharaListUtilType.GetField("replaceCharaHeadOnly", BindingFlags.NonPublic | BindingFlags.Instance);
            var replaceCharaBodyOnly = studioCharaListUtilType.GetField("replaceCharaBodyOnly", BindingFlags.NonPublic | BindingFlags.Instance);
            var replaceCharaClothesOnly = studioCharaListUtilType.GetField("replaceCharaClothesOnly", BindingFlags.NonPublic | BindingFlags.Instance);
            var replaceCharaAccOnly = studioCharaListUtilType.GetField("replaceCharaAccOnly", BindingFlags.NonPublic | BindingFlags.Instance);

            var replaceFields = new[] {replaceCharaBodyOnly, replaceCharaHeadOnly, replaceCharaHairOnly, replaceCharaClothesOnly, replaceCharaAccOnly};
            for (int i = 0; i < replaceFields.Length; i++) {
                replaceFields[i].SetValue(studioCharaListUtil, loadState[i]);
            }

            var charaFileSortField = studioCharaListUtilType.GetField("charaFileSort", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo changeMetod = studioCharaListUtilType.GetMethod("ChangeChara", BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { }, null);

            CharaFileSort charaFileSort = charaFileSortField.GetValue(studioCharaListUtil) as CharaFileSort;
            charaFileSort.cfiList.Clear();
            var charaFileInfo = new CharaFileInfo(fileFullName, "Bobby");
            charaFileInfo.node = new ListNode();
            charaFileInfo.select = true;

            charaFileSort.cfiList.Add(charaFileInfo);
            charaFileSort.select = 0;
            changeMetod.Invoke(studioCharaListUtil, new object[] { });
        }

        private void ReplaceChara(string fileFullName, int sex) {
            OCIChar[] array = Singleton<GuideObjectManager>.Instance.selectObjectKey.Select(v => Studio.Studio.GetCtrlInfo(v) as OCIChar)
                .Where(v => v != null).Where(v => v.oiCharInfo.sex == sex).ToArray();
            int length = array.Length;
            for (int index = 0; index < length; ++index)
                array[index].ChangeChara(fileFullName);
            if (length > 0) {
                CloseWindowIfToggle();
            }
        }

        private void CloseWindowIfToggle() {
            if (closeToggle.isOn)
                closeAction.Invoke();
        }

        private void LoadChara(string fileFullName) {
            Singleton<Studio.Studio>.Instance.AddFemale(fileFullName);
            CloseWindowIfToggle();
        }

        public void ToggleActive() {
            GUI?.gameObject.SetActive(!GUI.gameObject.activeInHierarchy);
        }
    }
}