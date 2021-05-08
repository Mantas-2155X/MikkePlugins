using System;
using BepInEx;
using HarmonyLib;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using UniRx;
using UnityEngine.UI;

namespace BeaverAI {
    [BepInPlugin(BeaverPlugin.GUID + "_GUI", "Beaver plugin GUI", BeaverPlugin.VERSION)]
    [BepInDependency(BeaverPlugin.GUID)]
    [BepInDependency("com.deathweasel.bepinex.uncensorselector", "3.9.0")]
    [BepInProcess(BeaverPlugin.PROCESS)]
    public class BeaverGUI : BaseUnityPlugin {
        internal MakerToggle PantiesHiding;
        internal MakerToggle PantyhoseHiding;
        internal MakerToggle BottomHiding;

        private BeaverController beaverController;

        private static MakerSlider[] BeaverSliders;
        private static MakerText BeaverText;


        private void Start() {
            MakerAPI.RegisterCustomSubCategories += RegisterCustomSubCategories;
            MakerAPI.ReloadCustomInterface += (sender, args) => ReLoad(sender, args);
            MakerAPI.MakerFinishedLoading += ReLoad;
        }

        private void Awake() {
            Harmony harmony = new Harmony(nameof(BeaverGUI));
            var ucType = BeaverPlugin.GetUcType();

            var methodInfo = AccessTools.Method(ucType, "UpdateUncensor", null);
            harmony.Patch(methodInfo, null, new HarmonyMethod(GetType(), nameof(UpdateFromUnc)));
        }


        internal static void UpdateFromUnc(object __instance) {
            if (MakerAPI.InsideMaker) {
                FindBeaverShapes(false);
            }
        }

        private void RegisterCustomSubCategories(object sender, RegisterSubCategoriesEvent ev) {
            RegisterClothesGui(ev);
            RegisterBodyGui(ev);
        }

        private void RegisterBodyGui(RegisterSubCategoriesEvent ev) {
            //female only, for now
            if (MakerAPI.GetMakerSex() == 0) {
                return;
            }

            var category = MakerConstants.Body.All;
            BeaverText = new MakerText("Beaver shapes", category, this);
            ev.AddControl(BeaverText);

            BeaverSliders = new MakerSlider[BeaverPlugin.MaxShapeCount];
            for (int index = 0; index < BeaverPlugin.MaxShapeCount; index++) {
                int shapeIndex = index;
                MakerSlider makerSlider = new MakerSlider(category, "Shape" + index, 0f, 1f, 0f, this);
                makerSlider.Visible.OnNext(false);
                ev.AddControl(makerSlider).BindToFunctionController<BeaverController, float>(controller => controller.GetBeaverShape(shapeIndex),
                    (controller, value) => controller.SetBeaverShape(shapeIndex, value));
                BeaverSliders[index] = makerSlider;
            }

            ev.AddSubCategory(category);
        }

        private void RegisterClothesGui(RegisterSubCategoriesEvent ev) {
            MakerCategory category = new MakerCategory(MakerConstants.Clothes.CategoryName, "WZ", 1, "Beaver");

            ev.AddControl(new MakerText("Accessories parented to the lower abdomen will be hidden when wearing:", category, this));

            PantiesHiding = new MakerToggle(category, "Panties", false, this);
            ev.AddControl(PantiesHiding);
            PantyhoseHiding = new MakerToggle(category, "Pantyhose", false, this);
            ev.AddControl(PantyhoseHiding);
            BottomHiding = new MakerToggle(category, "Bottom", false, this);
            ev.AddControl(BottomHiding);

            ev.AddSubCategory(category);
        }

        private void ReLoad(object sender, EventArgs args) {
            beaverController = GetMakerController();
            var beaverInfo = beaverController.beaverInfo;
            FindBeaverShapes(true);

            UpdateToggleSubscription(PantiesHiding, beaverInfo.PantiesHiding, b => { beaverInfo.PantiesHiding = b; });
            UpdateToggleSubscription(PantyhoseHiding, beaverInfo.PantyHoseHiding, b => { beaverInfo.PantyHoseHiding = b; });
            UpdateToggleSubscription(BottomHiding, beaverInfo.BottomHiding, b => { beaverInfo.BottomHiding = b; });
        }

        private void UpdateToggleSubscription(MakerToggle toggle, bool value, Action<bool> action) {
            var beaverObserver = Observer.Create<bool>(b => {
                action(b);
                beaverController.Recalculate();
            });

            toggle.ValueChanged.Subscribe(beaverObserver);
            toggle.SetValue(value);
        }

        public static void FindBeaverShapes(bool init) {
            if (MakerAPI.GetMakerSex() == 0) {
                return;
            }

            var beaverController = GetMakerController();

            for (int index = 0; index < BeaverPlugin.MaxShapeCount; index++) {
                if (!beaverController.HasBeaverShape(index)) {
                    BeaverSliders[index].SetValue(0f);
                    BeaverSliders[index].Visible.OnNext(false);
                } else {
                    MakerSlider slider = BeaverSliders[index];
                    if (slider.Exists) {
                        var controlObjectTransform = slider.ControlObject.transform;
                        var textTrans = controlObjectTransform.Find("Text");
                        textTrans.GetComponent<Text>().text = beaverController.GetBeaverName(index);
                    }

                    float beaverValue = init ? beaverController.GetBeaverShape(index) : 0f;
                    BeaverSliders[index].SetValue(beaverValue);
                    BeaverSliders[index].Visible.OnNext(true);

                    beaverController.SetBeaverShape(index, beaverValue);
                }
            }
        }

        private static BeaverController GetMakerController() {
            return MakerAPI.GetCharacterControl().gameObject.GetComponent<BeaverController>();
        }
    }
}