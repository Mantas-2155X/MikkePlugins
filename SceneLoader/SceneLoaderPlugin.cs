using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using KKAPI.Utilities;
using Manager;
using Studio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SceneLoader {
    [BepInPlugin(GUID, "Sceneloader plugin", VERSION)]
    [BepInProcess("StudioNEOV2")]
    public class SceneLoaderPlugin : BaseUnityPlugin {
        public const string GUID = "mikke.SceneLoader";
        internal const string VERSION = "1.1";
        
        private Canvas GUI;
        private string currentDir;

        public void OnLevelWasLoaded() {
            currentDir = UserData.Path + Config.Bind<String>("", "Initial folder", "studio/scene", "Folder to open when SceneLoader starts up. Takes effect after restart").Value;
            DirectoryInfo directoryInfo = new DirectoryInfo(currentDir);
            if (!directoryInfo.Exists) {
                directoryInfo.Create();
            }
            
            SpawnGui();
            CreateMenuButton();
        }

        private GameObject menuButton;

        private void CreateMenuButton() {
            GameObject original = GameObject.Find("StudioScene/Canvas Main Menu/04_System/Viewport/Content/End");
            if (original == null) return;
            menuButton = Instantiate(original, original.transform.parent);
            Button component = menuButton.GetComponent<Button>();
            component.onClick.ActuallyRemoveAllListeners();
            component.onClick.AddListener(() => GUI.gameObject.SetActive(!GUI.gameObject.activeInHierarchy));
            menuButton.GetComponentInChildren<TMP_Text>().text = "SceneLoader";
        }

        private void SpawnGui() {
            AssetBundle bundle = AssetBundle.LoadFromMemory(Resources.loaderres);
            GUI = Instantiate(bundle.LoadAsset<GameObject>("SceneLoaderCanvas")).GetComponent<Canvas>();
            GUI.gameObject.SetActive(false);
            bundle.Unload(false);

            LoadDirList();

            var saveButton = GUI.transform.Find("MainPanel/PanelHeader/SaveButton").GetComponent<Button>();
            saveButton.onClick.AddListener(() => {
                SaveScene(generateFilename(currentDir));
                ListScenesInDir(currentDir);
            });
            var folderButton = GUI.transform.Find("MainPanel/PanelHeader/FolderButton").GetComponent<Button>();
            folderButton.onClick.AddListener(() => { Process.Start(UserData.Path + "studio/scene/"); });

            var refreshButton = GUI.transform.Find("MainPanel/PanelHeader/RefreshButton").GetComponent<Button>();
            refreshButton.onClick.AddListener(LoadDirList);
            
            var dirUpButton = GUI.transform.Find("MainPanel/PanelHeader/DirUpButton").GetComponent<Button>();
            dirUpButton.onClick.AddListener(() => ListScenesInDir(new DirectoryInfo(currentDir).Parent.FullName));
        }

        private void LoadDirList() {
            var dirbutton = GUI.transform.Find("DirButton").GetComponent<Button>();

            var content = GUI.transform.Find("MainPanel/Body/DirView/Viewport/Content").GetComponent<Transform>();
            foreach (Transform child in content) {
                Destroy(child.gameObject);
            }

            var dirInfo = new DirectoryInfo(UserData.Path + "studio/scene");

            foreach (var dir in dirInfo.GetDirectories()) {
                Button button = Instantiate(dirbutton.gameObject).GetComponent<Button>();
                var tom = button.transform.Find("Text").GetComponent<Text>();
                tom.text = dir.Name;

                button.onClick.AddListener(() => ListScenesInDir(dir.FullName));

                button.gameObject.SetActive(true);
                button.transform.SetParent(content.gameObject.transform, false);
            }

            ListScenesInDir(currentDir);
        }

        private List<GameObject> buttonPanels = new List<GameObject>();

        private void ListScenesInDir(string path) {
            currentDir = path;
            var content = GUI.transform.Find("MainPanel/Body/SceneView/Viewport/Content").GetComponent<Transform>();
            foreach (Transform child in content) {
                Destroy(child.gameObject);
            }

            buttonPanels.Clear();

            var scenebutton = GUI.transform.Find("SceneButton").GetComponent<Button>();
            var dirInfo = new DirectoryInfo(path);

            foreach (var dir in dirInfo.GetDirectories().OrderByDescending(f => f.Name)) {
                Button folder = Instantiate(scenebutton.gameObject).GetComponent<Button>();
                var text = folder.transform.Find("Text").GetComponent<Text>();
                text.text = dir.Name;
                folder.onClick.AddListener(() => ListScenesInDir(dir.FullName));
                
                folder.gameObject.SetActive(true);
                folder.transform.SetParent(content.gameObject.transform, false);
            }


            foreach (var file in dirInfo.GetFiles().OrderByDescending(f => f.Name)) {
                if (!file.Extension.ToLower().Equals(".png")) {
                    continue;
                }

                Button scene = Instantiate(scenebutton.gameObject).GetComponent<Button>();
                var fileData = File.ReadAllBytes(file.FullName);
                var img = scene.GetComponent<RawImage>();
                var tex = new Texture2D(2, 2);
                tex.LoadImage(fileData);
                img.texture = tex;

                scene.gameObject.SetActive(true);
                scene.transform.SetParent(content.gameObject.transform, false);

                buttonPanels.Add(scene.transform.Find("ButtonPanel").gameObject);

                var loadButton = scene.transform.Find("ButtonPanel/LoadButton").GetComponent<Button>();
                var importButton = scene.transform.Find("ButtonPanel/ImportButton").GetComponent<Button>();
                var deleteButton = scene.transform.Find("ConfirmationPanel/DeleteButton").GetComponent<Button>();
                var overwriteButton = scene.transform.Find("OwerwritePanel/OverwriteButton").GetComponent<Button>();

                loadButton.onClick.AddListener(() => { LoadScene(file.FullName); });
                importButton.onClick.AddListener(() => { Studio.Studio.Instance.ImportScene(file.FullName); });
                deleteButton.onClick.AddListener(() => {
                    File.Delete(file.FullName);
                    ListScenesInDir(path);
                });
                overwriteButton.onClick.AddListener(() => {
                    SaveScene(file.FullName);
                    ListScenesInDir(path);
                });

                scene.onClick.AddListener(() => {
                    foreach (var lp in buttonPanels) {
                        if (lp != null)
                            lp.SetActive(false);
                    }

                    var panel = scene.transform.Find("ButtonPanel").gameObject;
                    panel.SetActive(true);
                });
            }
        }

        public void LoadScene(string path) {
            StartCoroutine(LoadSceneCo(path));
        }

        private IEnumerator LoadSceneCo(string _path) {
            yield return Singleton<Studio.Studio>.Instance.LoadSceneCoroutine(_path);
            yield return null;
           
            #if HS2
            Scene.LoadReserve(new Scene.Data {
                levelName = "StudioNotification",
                isAdd = true
            }, false);
         
            #elif AI
            Singleton<Scene>.Instance.LoadReserve(new Scene.Data {
                levelName = "StudioNotification",
                isAdd = true
            }, false);
            #endif
            yield break;
        }

        public void SaveScene(string filename) {
            foreach (KeyValuePair<int, ObjectCtrlInfo> keyValuePair in Studio.Studio.Instance.dicObjectCtrl)
                keyValuePair.Value.OnSavePreprocessing();

            var cam = (Studio.CameraControl) typeof(Studio.Studio).GetField("m_CameraCtrl", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(Studio.Studio.Instance);
            Studio.Studio.Instance.sceneInfo.cameraSaveData = cam.Export();

           
            Studio.Studio.Instance.sceneInfo.Save(filename);
        }

        private string generateFilename(string path) {
            var now = DateTime.Now;
            return path + string.Format("/{0}_{1:00}{2:00}_{3:00}{4:00}_{5:00}_{6:000}.png", (object) now.Year, (object) now.Month, (object) now.Day, (object) now.Hour, (object) now.Minute,
                (object) now.Second, (object) now.Millisecond);
        }
    }
}