using System;
using AIChara;
using ExtensibleSaveFormat;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Studio;
using Studio;
using UnityEngine;

namespace PushUpAI {
    internal enum Wearing {
        Topless,
        Bra,
        Top,
        Both
    }

    public class PushUpController : CharaCustomFunctionController {
        public PushUpInfo Info { get; private set; }
        private PushUpBoneController pushUpBoneController;

        protected override void OnReload(GameMode currentGameMode) {
            base.OnReload(currentGameMode);

            var flags = MakerAPI.GetCharacterLoadFlags();
            var clothesFlagged = flags == null || flags.Clothes;
            var bodyFlagged = flags == null || flags.Body;

            var pluginData = GetExtendedData();

            if (bodyFlagged) {
                var newInfo = new PushUpInfo(PushUpAiPlugin.BraDefault);
                if (Info != null && !clothesFlagged) {
                    newInfo.CopyOldInfo(Info);
                }

                Info = newInfo;
            }

            if (clothesFlagged) {
                if (!bodyFlagged) {
                    Info.MapFromCoordinate(pluginData);
                } else {
                    Info.MapFromSave(pluginData);
                }
            }

            try {
                var boneController = ChaControl.gameObject.GetComponent<PushUpBoneController>();
                if (boneController != null) {
                    pushUpBoneController = boneController;
                }
            } catch (Exception e) {
                Lg(e.Message);
            }

            RecalculateBody();
        }

        public void RecalculateBody() {
            if (ChaControl == null || Info == null) return;
            Wearing nowWearing = IsWearing(false, false);
            if ((nowWearing != Wearing.Topless)) {
                CalculatePush(nowWearing, Info);
                UpdateAccessories(nowWearing);
                pushUpBoneController.EnablePushUp = true;
            } else {
                pushUpBoneController.EnablePushUp = false;
                UpdateAccessories(nowWearing);
                SetBreastSoftness(ChaControl.fileBody.bustSoftness);
            }

            Wearing corsetWearing = IsWearing(Info.Bra.CorsetHalf, Info.Top.CorsetHalf);
            if (corsetWearing != Wearing.Topless) {
                CalculateCorset(corsetWearing, Info);
                pushUpBoneController.EnableCorset = true;
            } else {
                pushUpBoneController.EnableCorset = false;
            }
        }

        protected override void OnCardBeingSaved(GameMode currentGameMode) {
            SetExtendedData(Info.MapToSave());
        }

        private void UpdateAccessories(Wearing nowWearing) {
            bool showNippleAccs;
            switch (nowWearing) {
                case Wearing.Topless:
                    showNippleAccs = true;
                    break;
                case Wearing.Both:
                    showNippleAccs = !(Info.Bra.HideAccessories || Info.Top.HideAccessories);
                    break;
                case Wearing.Bra:
                    showNippleAccs = !Info.Bra.HideAccessories;
                    break;
                default:
                    showNippleAccs = !Info.Top.HideAccessories;
                    break;
            }

            for (int index = 0; index < ChaControl.nowCoordinate.accessory.parts.Length; index++) {
                var acc = ChaControl.nowCoordinate.accessory.parts[index];
                if (acc.parentKey == ChaAccessoryDefine.AccessoryParentKey.N_Tikubi_L.ToString() || acc.parentKey == ChaAccessoryDefine.AccessoryParentKey.N_Tikubi_R.ToString()) {
                    ChaControl.SetAccessoryState(index, showNippleAccs);
                }
            }

            if (StudioAPI.InsideStudio) {
                //Update state menu
                var mpCharCtrl = FindObjectOfType<MPCharCtrl>();
                if (mpCharCtrl == null) return;

                //do some hacky shit
                var tempChar = mpCharCtrl.ociChar;
                var thisChar = ChaControl.GetOCIChar();
                mpCharCtrl.ociChar = thisChar;
                if (tempChar != thisChar) mpCharCtrl.ociChar = tempChar;
            }
        }

        private Wearing IsWearing(bool braActiveHalf, bool topActiveHalf) {
            var braIsOnAndEnabled = BraIsOnAndEnabled(braActiveHalf);
            var topIsOnAndEnabled = TopIsOnAndEnabled(topActiveHalf);

            if (topIsOnAndEnabled) {
                return braIsOnAndEnabled ? Wearing.Both : Wearing.Top;
            }

            return braIsOnAndEnabled ? Wearing.Bra : Wearing.Topless;
        }


        protected override void OnCoordinateBeingLoaded(ChaFileCoordinate coordinate) {
            //check if we are loading clothes or accessories
            if (MakerAPI.GetCoordinateLoadFlags()?.Clothes == false) return;

            var pluginData = GetCoordinateExtendedData(coordinate);
            Info.MapFromCoordinate(pluginData);
            RecalculateBody();
        }

        protected override void OnCoordinateBeingSaved(ChaFileCoordinate coordinate) {
            SetCoordinateExtendedData(coordinate, Info.MapToCoordinate());
        }

        private bool BraIsOnAndEnabled(bool braActiveHalf) {
            return ChaControl.IsClothesStateKind((int) ChaFileDefine.ClothesKind.inner_t) &&
                   (ChaControl.fileStatus.clothesState[(int) ChaFileDefine.ClothesKind.inner_t] == 0 
                    ||ChaControl.fileStatus.clothesState[(int) ChaFileDefine.ClothesKind.inner_t] == 1 && braActiveHalf)
                   && (Info?.Bra.EnablePushUp == true);
        }

        private bool TopIsOnAndEnabled(bool topActiveHalf) {
            return ChaControl.IsClothesStateKind((int) ChaFileDefine.ClothesKind.top) &&
                   (ChaControl.fileStatus.clothesState[(int) ChaFileDefine.ClothesKind.top] == 0
                   ||ChaControl.fileStatus.clothesState[(int) ChaFileDefine.ClothesKind.top] == 1 && topActiveHalf)
                   && (Info?.Top.EnablePushUp == true);
        }

        private void CalculateCorset(Wearing wearing, PushUpInfo info) {
            var bra = info.Bra;
            var top = info.Top;
            if (wearing == Wearing.Bra) {
                CalculateCorsetFromClothes(bra);
                return;
            }

            if (wearing == Wearing.Top) {
                CalculateCorsetFromClothes(top);
                return;
            }

            var combo = new ClothData {
                Corset = Math.Max(bra.Corset, top.Corset),
            };

            CalculateCorsetFromClothes(combo);
        }

        private void CalculateCorsetFromClothes(ClothData cData) {
            
            float[] shapeValueBody = ChaControl.fileBody.shapeValueBody;
            if ((1f - cData.Corset) < shapeValueBody[16]) {
                pushUpBoneController.Corset.SetWidth(1-cData.Corset, shapeValueBody[16]);
            } else {
                pushUpBoneController.Corset.ResetWidth();
            }
            
            if ((1f - cData.Corset) < shapeValueBody[17]) {
                pushUpBoneController.Corset.SetThickness(1-cData.Corset, shapeValueBody[17]);
            } else {
                pushUpBoneController.Corset.ResetThickness();
            }
        }

        private void CalculatePush(Wearing wearing, PushUpInfo info) {

            var bra = info.Bra;
            var top = info.Top;
            if (wearing == Wearing.Bra) {
                CalculatePushFromClothes(bra);
                return;
            }

            if (wearing == Wearing.Top) {
                CalculatePushFromClothes(top);
                return;
            }

            var combo = new ClothData {
                Firmness = Math.Max(bra.Firmness, top.Firmness),
                Lift = Math.Max(bra.Lift, top.Lift),
                Squeeze = Math.Max(bra.Squeeze, top.Squeeze),
                PushTogether = Math.Max(bra.PushTogether, top.PushTogether),
                CenterNipples = Math.Max(bra.CenterNipples, top.CenterNipples),
                FlattenNipples = bra.FlattenNipples || top.FlattenNipples,
                HideNipples = bra.HideNipples || top.HideNipples,
                EnablePushUp = true
            };

            CalculatePushFromClothes(combo);
        }

        private void CalculatePushFromClothes(ClothData cData) {
            if (1f - cData.Firmness < ChaControl.fileBody.bustSoftness) {
                SetBreastSoftness(1 - cData.Firmness);
            } else {
                SetBreastSoftness(ChaControl.fileBody.bustSoftness);
            }

            float[] shapeValueBody = ChaControl.fileBody.shapeValueBody;
            if (cData.Lift > shapeValueBody[2]) {
                pushUpBoneController.Lift.SetValue(cData.Lift - shapeValueBody[2]);
            } else {
                pushUpBoneController.Lift.SetValue(0);
            }

            if (1f - cData.PushTogether < shapeValueBody[3]) {
                var zDeviation = Math.Abs(0.5f - shapeValueBody[3]);
                var trgDeviation = Math.Abs(0.5f - cData.PushTogether);
                pushUpBoneController.Direction.SetValue(cData.PushTogether - (1 - shapeValueBody[3]),
                    trgDeviation - zDeviation);
            } else {
                pushUpBoneController.Direction.SetValue(0f, 0f);
            }

            if (1f - cData.PushTogether < shapeValueBody[4]) {
                pushUpBoneController.Spacing.SetValue(cData.PushTogether - (1 - shapeValueBody[4]));
            } else {
                pushUpBoneController.Spacing.SetValue(0);
            }

            if (1f - cData.Squeeze < shapeValueBody[6]) {
                pushUpBoneController.Squeeze.SetValue(shapeValueBody[6], (1 - cData.Squeeze));
            } else {
                pushUpBoneController.Squeeze.SetValueZero();
            }

            if (cData.HideNipples || cData.FlattenNipples) {
                pushUpBoneController.Areola.SetValue(shapeValueBody[7]);
                pushUpBoneController.Nipple.SetValue(Mathf.Clamp(shapeValueBody[32], 0, 1));
                pushUpBoneController.HideNipple.SetActive(cData.HideNipples);
            } else {
                pushUpBoneController.Areola.Reset();
                pushUpBoneController.Nipple.Reset();
                pushUpBoneController.HideNipple.SetActive(false);
            }

            var nipDeviation = 0.5f - shapeValueBody[5];
            pushUpBoneController.Angle.SetValue(-nipDeviation * cData.CenterNipples);
        }

        private float softness;

        public void SetBreastSoftness(float soft) {
            softness = soft;
        }

        public void ApplyBreastSoftness() {
            var soft = softness;
            if (null == ChaControl)
                return;
            float rate = Mathf.Clamp((float) (soft * (double) ChaControl.fileBody.shapeValueBody[1] + 0.00999999977648258), 0.0f, 1f);
            float stiffness = TreeLerp(new[] {1f, 0.1f, 0.01f}, rate);
            float elasticity = TreeLerp(new[] {0.2f, 0.15f, 0.05f}, rate);
            float damping = TreeLerp(new[] {0.2f, 0.1f, 0.1f}, rate);
            DynamicBone_Ver02[] dynamicBoneVer02Array = {
                ChaControl.GetDynamicBoneBustAndHip(ChaControlDefine.DynamicBoneKind.BreastL),
                ChaControl.GetDynamicBoneBustAndHip(ChaControlDefine.DynamicBoneKind.BreastR)
            };
            foreach (DynamicBone_Ver02 dynamicBoneVer02 in dynamicBoneVer02Array) {
                if (dynamicBoneVer02 != null) {
                    dynamicBoneVer02.setSoftParams(0, -1, damping, elasticity, stiffness);
                }
            }
        }

        private float TreeLerp(float[] vals, float rate) {
            return (double) rate < 0.5 ? Mathf.Lerp(vals[0], vals[1], rate * 2f) : Mathf.Lerp(vals[1], vals[2], (float) ((rate - 0.5) * 2.0));
        }


        private static void Lg(string logEntry) {
            PushUpAiPlugin.Log.LogError(DateTime.Now + ": " + logEntry);
        }
    }

    public class PushUpInfo {
        private static string FIRMNESS = "FIRMNESS";
        private static string LIFT = "LIFT";
        private static string PUSH_TOGETHER = "PUSH_TOGETHER";
        private static string SQUEEZE = "SQUEEZE";
        private static string CENTER_NIPPLES = "CENTER_NIPPLES";
        private static string FLATTEN_NIPPLES = "FLATTEN_NIPPLES";
        private static string ENABLE_PUSHUP = "ENABLE_PUSHUP";
        private static string HIDE_ACCESSORIES = "HIDE_ACCESSORIES";
        private static string HIDE_NIPPLES = "HIDE_NIPPLES";
        private static string CORSET = "CORSET";
        private static string CORSET_HALF = "CORSET_HALF";
        private static String TOP_PREFIX = "TOP_";

        public ClothData Bra;
        public ClothData Top;

        public PushUpInfo(DefaultPushUp braDefault) {
            Init(braDefault);
        }

        private void Init(DefaultPushUp braDefault) {
            Bra = new ClothData(braDefault);
            Top = new ClothData(new DefaultPushUp());
        }

        public PluginData MapToSave() {
            var pluginData = new PluginData();
            MapToSave(pluginData, Bra, "");
            MapToSave(pluginData, Top, TOP_PREFIX);
            return pluginData;
        }

        private void MapToSave(PluginData pluginData, ClothData cData, string prefix) {
            MapToCoordinate(pluginData, cData, prefix);
        }

        public PluginData MapToCoordinate() {
            var pluginData = new PluginData();
            MapToCoordinate(pluginData, Bra, "");
            MapToCoordinate(pluginData, Top, TOP_PREFIX);
            return pluginData;
        }

        private void MapToCoordinate(PluginData pluginData, ClothData cData, string prefix) {
            pluginData.data.Add(prefix + FIRMNESS, cData.Firmness);
            pluginData.data.Add(prefix + LIFT, cData.Lift);
            pluginData.data.Add(prefix + PUSH_TOGETHER, cData.PushTogether);
            pluginData.data.Add(prefix + SQUEEZE, cData.Squeeze);
            pluginData.data.Add(prefix + CENTER_NIPPLES, cData.CenterNipples);
            pluginData.data.Add(prefix + FLATTEN_NIPPLES, cData.FlattenNipples);
            pluginData.data.Add(prefix + ENABLE_PUSHUP, cData.EnablePushUp);
            pluginData.data.Add(prefix + HIDE_ACCESSORIES, cData.HideAccessories);
            pluginData.data.Add(prefix + HIDE_NIPPLES, cData.HideNipples);
            pluginData.data.Add(prefix + CORSET, cData.Corset);
            pluginData.data.Add(prefix + CORSET_HALF, cData.CorsetHalf);


            pluginData.version = 3;
        }

        public void MapFromSave(PluginData pluginData) {
            MapFromSave(pluginData, Bra, "");
            MapFromSave(pluginData, Top, TOP_PREFIX);
        }

        private void MapFromSave(PluginData pluginData, ClothData cData, string prefix) {
            MapFromCoordinate(pluginData, cData, prefix);
        }

        public void MapFromCoordinate(PluginData pluginData) {
            Init(PushUpAiPlugin.BraDefault);
            MapFromCoordinate(pluginData, Bra, "");
            MapFromCoordinate(pluginData, Top, TOP_PREFIX);
        }


        private void MapFromCoordinate(PluginData pluginData, ClothData cData, string prefix) {
            cData.Firmness = getSaveFloat(pluginData, prefix + FIRMNESS, cData.Firmness);
            cData.Lift = getSaveFloat(pluginData, prefix + LIFT, cData.Lift);
            cData.PushTogether = getSaveFloat(pluginData, prefix + PUSH_TOGETHER, cData.PushTogether);
            cData.Squeeze = getSaveFloat(pluginData, prefix + SQUEEZE, cData.Squeeze);
            cData.FlattenNipples = getSaveBool(pluginData, prefix + FLATTEN_NIPPLES, cData.FlattenNipples);
            cData.CenterNipples = getSaveFloat(pluginData, prefix + CENTER_NIPPLES, cData.CenterNipples);
            cData.EnablePushUp = getSaveBool(pluginData, prefix + ENABLE_PUSHUP, cData.EnablePushUp);
            cData.HideAccessories = getSaveBool(pluginData, prefix + HIDE_ACCESSORIES, cData.HideAccessories);
            cData.HideNipples = getSaveBool(pluginData, prefix + HIDE_NIPPLES, cData.HideNipples);
            cData.Corset=getSaveFloat(pluginData, prefix + CORSET, cData.Corset);
            cData.CorsetHalf = getSaveBool(pluginData,prefix + CORSET_HALF, cData.CorsetHalf);
        }

        private float getSaveFloat(PluginData pluginData, string key, float defVal) {
            if (pluginData != null && pluginData.data.TryGetValue(key, out var val)) {
                return (float) val;
            }

            return defVal;
        }

        private bool getSaveBool(PluginData pluginData, string key, bool defVal) {
            if (pluginData != null && pluginData.data.TryGetValue(key, out var val)) {
                return (bool) val;
            }

            return defVal;
        }

        public void CopyOldInfo(PushUpInfo info) {
            if (info.Bra.EnablePushUp) {
                Bra = info.Bra;
            }

            if (info.Top.EnablePushUp) {
                Top = info.Top;
            }
        }
    }

    public class ClothData {
        public ClothData(DefaultPushUp defaultPushUp) {
            Firmness = defaultPushUp.Firmness;
            Lift = defaultPushUp.Lift;
            PushTogether = defaultPushUp.PushTogether;
            Squeeze = defaultPushUp.Squeeze;
            CenterNipples = defaultPushUp.CenterNipples;
            FlattenNipples = defaultPushUp.FlattenNipples;
            EnablePushUp = defaultPushUp.EnablePushUp;
            HideNipples = defaultPushUp.HideNipples;
            HideAccessories = false;
        }

        internal ClothData() {
        }

        public float Firmness { get; set; }
        public float Lift { get; set; }
        public float PushTogether { get; set; }
        public float Squeeze { get; set; }
        public bool FlattenNipples { get; set; }
        public bool HideNipples { get; set; }

        public bool EnablePushUp { get; set; }
        public float CenterNipples { get; set; }
        public bool HideAccessories { get; set; }

        public float Corset { get; set; }
        public bool CorsetHalf { get; set; }
    }

    public class DefaultPushUp {
        public float Firmness;
        public float Lift;
        public float PushTogether;
        public float Squeeze;
        public float CenterNipples;

        public bool FlattenNipples;
        public bool EnablePushUp;
        public bool HideNipples;
    }
}