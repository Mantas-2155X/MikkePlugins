using System.IO;
using BepInEx;
using BepInEx.Configuration;
using KKAPI.Studio;
using UnityEngine.SceneManagement;

namespace CharLoader {
    [BepInPlugin(GUID, "Character Loader", VERSION)]
    [BepInDependency("GarryWu.HS2WearCustom", BepInDependency.DependencyFlags.SoftDependency)]
    public class CharLoaderPlugin : BaseUnityPlugin {
        public const string GUID = "mikke.Charloader";
        internal const string VERSION = "1.2";

        internal static DirectoryInfo FemaleBaseDir = new DirectoryInfo(UserData.Path + "chara/female");
        internal static DirectoryInfo MaleBaseDir = new DirectoryInfo(UserData.Path + "chara/male");

        internal static readonly string VariantsDirName = "Variants";

        private bool replaceStudioButtons;
        private int numRows;
        private ConfigEntry<bool> showMakerFolders;

        public void Start() {
            SceneManager.sceneLoaded += FinishedLoading;

            var replaceButtons = Config.AddSetting("Config", "Replace menu buttons", false,
                "Replace the standard buttons to add female and male characters to the game with a single Characters button");
            replaceStudioButtons = replaceButtons.Value;
            
            var numRowsSetting = Config.AddSetting("Config", "Chars per row.", 4, new
                ConfigDescription("Number of character cards per row in the Studio Character Loader. Takes effect on restart",
                    new AcceptableValueRange<int>(2, 6)));
            numRows = numRowsSetting.Value;

            showMakerFolders = Config.AddSetting("Config", "Maker folder list",
                true, "Show or hide folder list in the character maker when saving or loading characters");
        }

        private CharLoaderStudio charLoaderStudio;
        private CharLoaderMaker charLoaderMaker;

        public void FinishedLoading(Scene scene, LoadSceneMode loadSceneMode) {
            if (StudioAPI.InsideStudio) {
               
                charLoaderStudio = gameObject.AddComponent<CharLoaderStudio>();
                charLoaderStudio.SpawnGui(replaceStudioButtons, numRows);
            } else {
                charLoaderMaker = gameObject.AddComponent<CharLoaderMaker>();
                charLoaderMaker.SpawnGui(showMakerFolders);
            }
        }
    }
}