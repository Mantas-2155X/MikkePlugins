using System;
using BepInEx;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using UniRx;

namespace PushUpAI {
    [BepInPlugin(PushUpAiPlugin.GUID + "_GUI", "PushUp plugin GUI", PushUpAiPlugin.VERSION)]
    [BepInDependency(PushUpAiPlugin.GUID)]
    [BepInProcess(PROCESS)]
    public class PushUpGui : BaseUnityPlugin {
#if HS2
        public const string PROCESS = "HoneySelect2";
#elif AI
        public const string PROCESS = "AI-Syoujyo";
#endif
        //Sliders and toggles

        private static MakerToggle EnablePushUpToggle;

        private static PushUpSlider FirmnessSlider;
        private static PushUpSlider LiftSlider;
        private static PushUpSlider PushTogetherSlider;
        private static PushUpSlider SqueezeSlider;
        private static PushUpSlider CenterSlider;
        private static PushUpSlider CorsetSlider;

        private static MakerToggle FlattenNippleToggle;
        private static MakerToggle HideAccessoryToggle;

        private static MakerToggle HideNippleToggle;
        private static MakerToggle CorsetHalfOffToggle;

        private static MakerRadioButtons SelectButtons;

        private PushUpInfo pushUpInfo;

        private PushUpController pushUpController;
        private SliderManager sliderManager;

        private ClothData activeClothData;

        private void Start() {
            MakerAPI.RegisterCustomSubCategories += RegisterCustomSubCategories;
            MakerAPI.ReloadCustomInterface += ReLoadPushUp;
            MakerAPI.MakerExiting += MakerExiting;
            MakerAPI.MakerFinishedLoading += ReLoadPushUp;
        }

        private void ReLoadPushUp(object sender, EventArgs args) {
            ReLoadPushUp();
        }

        private void ReLoadPushUp() {
            sliderManager = new SliderManager();

            pushUpController = GetMakerController();
            pushUpInfo = pushUpController.Info;
            activeClothData = SelectButtons.Value == 0 ? pushUpInfo.Bra : pushUpInfo.Top;

            sliderManager.InitSliders(pushUpController);

            UpdateToggleSubscription(EnablePushUpToggle, activeClothData.EnablePushUp, b => { activeClothData.EnablePushUp = b; });

            UpdateSliderSubscription(FirmnessSlider, activeClothData.Firmness, f => { activeClothData.Firmness = f; });
            UpdateSliderSubscription(LiftSlider, activeClothData.Lift, f => { activeClothData.Lift = f; });
            UpdateSliderSubscription(PushTogetherSlider, activeClothData.PushTogether, f => { activeClothData.PushTogether = f; });
            UpdateSliderSubscription(SqueezeSlider, activeClothData.Squeeze, f => { activeClothData.Squeeze = f; });
            UpdateSliderSubscription(CenterSlider, activeClothData.CenterNipples, f => { activeClothData.CenterNipples = f; });

            UpdateToggleSubscription(FlattenNippleToggle, activeClothData.FlattenNipples, b => { activeClothData.FlattenNipples = b; });
            UpdateToggleSubscription(HideAccessoryToggle, activeClothData.HideAccessories, b => { activeClothData.HideAccessories = b; });

            UpdateToggleSubscription(HideNippleToggle, activeClothData.HideNipples, b => { activeClothData.HideNipples = b; });
            
            UpdateSliderSubscription(CorsetSlider, activeClothData.Corset, f => { activeClothData.Corset = f; });
            UpdateToggleSubscription(CorsetHalfOffToggle, activeClothData.CorsetHalf, b => { activeClothData.CorsetHalf = b; });
        }

        private void UpdateToggleSubscription(MakerToggle toggle, bool value, Action<bool> action) {
            var pushObserver = Observer.Create<bool>(b => {
                action(b);
                pushUpController.RecalculateBody();
            });

            toggle.ValueChanged.Subscribe(pushObserver);
            toggle.SetValue(value);
        }

        private void UpdateSliderSubscription(PushUpSlider slider, float value, Action<float> action) {
            slider.OnUpdate = f => {
                action(f);
                pushUpController.RecalculateBody();
            };

            var pushObserver = Observer.Create<float>(slider.Update);

            slider.MakerSlider.ValueChanged.Subscribe(pushObserver);
            slider.MakerSlider.SetValue(value);
        }

        private void MakerExiting(object sender, EventArgs e) {
            pushUpInfo = null;
            pushUpController = null;
            sliderManager = null;
        }

        private static PushUpController GetMakerController() {
            return MakerAPI.GetCharacterControl().gameObject.GetComponent<PushUpController>();
        }

        private void RegisterCustomSubCategories(object sender, RegisterSubCategoriesEvent ev) {
            MakerCategory category = new MakerCategory(MakerConstants.Clothes.CategoryName, "PU", 1, "Push Up");

            //Bra or top
            SelectButtons = ev.AddControl(new MakerRadioButtons(category, this, "Type", "Bra", "Top"));
            SelectButtons.ValueChanged.Subscribe(i => ReLoadPushUp());
            
            EnablePushUpToggle = new MakerToggle(category, "Enabled", true, this);
            ev.AddControl(EnablePushUpToggle);

            float min = -1.0f;
            float max = 2f;
            
            FirmnessSlider = MakeSlider(category, "Firmness", ev, min, max, PushUpAiPlugin.BraDefault.Firmness);
            LiftSlider = MakeSlider(category, "Lift", ev, min, max, PushUpAiPlugin.BraDefault.Lift);
            PushTogetherSlider = MakeSlider(category, "Push Together", ev, min, max, PushUpAiPlugin.BraDefault.PushTogether);
            SqueezeSlider = MakeSlider(category, "Squeeze", ev, min, max, PushUpAiPlugin.BraDefault.Squeeze);
            CenterSlider = MakeSlider(category, "Center Nipples", ev, min, max, PushUpAiPlugin.BraDefault.CenterNipples);

            FlattenNippleToggle = new MakerToggle(category, "Flatten Nipples", true, this);
            ev.AddControl(FlattenNippleToggle);

            HideAccessoryToggle = new MakerToggle(category, "Hide Accessories", true, this);
            ev.AddControl(HideAccessoryToggle);

            ev.AddControl(new MakerSeparator(category, this));

            HideNippleToggle = new MakerToggle(category, "Hide Nipples", false, this);
            ev.AddControl(HideNippleToggle);

            ev.AddControl(new MakerSeparator(category, this));
            
            CorsetSlider = MakeSlider(category, "Corset", ev, min, 1.2f,0f);
            CorsetHalfOffToggle =  new MakerToggle(category, "Corset active for Half-Off", false, this);
            ev.AddControl(CorsetHalfOffToggle);
            
            ev.AddSubCategory(category);
        }

        private PushUpSlider MakeSlider(MakerCategory category, string sliderName, RegisterSubCategoriesEvent e, float minValue, float maxValue, float defaultValue) {
            var slider = new MakerSlider(category, sliderName, minValue, maxValue, defaultValue, this);
            e.AddControl(slider);
            var pushUpSlider = new PushUpSlider {MakerSlider = slider};
            return pushUpSlider;
        }
    }

    public class PushUpSlider {
        public MakerSlider MakerSlider;
        public Action<float> OnUpdate;

        public void Update(float f) {
            OnUpdate(f);
        }
    }
}