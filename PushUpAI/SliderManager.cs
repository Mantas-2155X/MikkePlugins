using System.Reflection;
using CharaCustom;
using UnityEngine;

namespace PushUpAI {
    public class SliderManager {
        public void InitSliders(PushUpController pushUpController) {
            //use reflection to find the sliders for the chest area
            var boobCont = Object.FindObjectOfType(typeof(CvsB_ShapeBreast));

            var shapesField = typeof(CvsB_ShapeBreast).GetField("ssShape", BindingFlags.NonPublic | BindingFlags.Instance);
            var softField = typeof(CvsB_ShapeBreast).GetField("ssBustSoftness", BindingFlags.NonPublic | BindingFlags.Instance);
            var weightField = typeof(CvsB_ShapeBreast).GetField("ssBustWeight", BindingFlags.NonPublic | BindingFlags.Instance);

            var ssShape = (CustomSliderSet[]) shapesField.GetValue(boobCont);
            var ssSoft = (CustomSliderSet) softField.GetValue(boobCont);
            var ssWeight = (CustomSliderSet) weightField.GetValue(boobCont);

            SetUpSlider(ssSoft, pushUpController);
            SetUpSlider(ssWeight, pushUpController);

            SetUpSlider(ssShape[0], pushUpController);
            SetUpSlider(ssShape[1], pushUpController);
            SetUpSlider(ssShape[2], pushUpController);
            SetUpSlider(ssShape[3], pushUpController);
            SetUpSlider(ssShape[4], pushUpController);

            SetUpSlider(ssShape[5], pushUpController);

            SetUpSlider(ssShape[6], pushUpController);
            SetUpSlider(ssShape[7], pushUpController);
            SetUpSlider(ssShape[8], pushUpController);
            
            //for corset
            var waistCont = Object.FindObjectOfType(typeof(CvsB_ShapeUpper));
            var waistShapesField = typeof(CvsB_ShapeUpper).GetField("ssShape", BindingFlags.NonPublic | BindingFlags.Instance);
            var waistShape = (CustomSliderSet[]) waistShapesField.GetValue(waistCont);
            
            SetUpSlider(waistShape[6], pushUpController);
            SetUpSlider(waistShape[7], pushUpController);
        }

        private void SetUpSlider(CustomSliderSet slider, PushUpController pushUpController) {
            var action = slider.onChange;
            slider.onChange = f => {
                action(f);
                pushUpController.RecalculateBody();
            };
        }
    }
}