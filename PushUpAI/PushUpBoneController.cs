using System;
using System.Collections.Generic;
using KKABMX.Core;
using KKAPI;
using KKAPI.Chara;
using UnityEngine;

namespace PushUpAI {
    public class PushUpBoneController : CharaCustomFunctionController {
        public LiftEffect Lift { get; private set; }
        public AngleEffect Angle { get; private set; }
        public DirectionEffect Direction { get; private set; }
        public SpacingEffect Spacing { get; private set; }
        public SqueezeEffect Squeeze { get; private set; }
        public AreolaEffect Areola { get; private set; }
        public NippleEffect Nipple { get; private set; }
        public HideNippleEffect HideNipple { get; private set; }
        public CorsetEffect Corset { get; private set; }
        public bool EnablePushUp { get; set; }
        public bool EnableCorset { get; set; }

        protected override void OnReload(GameMode currentGameMode) {
            if (Lift == null)
                Lift = new LiftEffect(this);
            if (Angle == null) {
                Angle = new AngleEffect(this);
            }

            if (Spacing == null) {
                Spacing = new SpacingEffect(this);
            }

            if (Direction == null) {
                Direction = new DirectionEffect(this);
            }

            if (Squeeze == null) {
                Squeeze = new SqueezeEffect(this);
            }

            if (Areola == null) {
                Areola = new AreolaEffect(this);
            }

            if (Nipple == null) {
                Nipple = new NippleEffect(this);
            }

            if (HideNipple == null) {
                HideNipple = new HideNippleEffect(this);
            }

            if (Corset == null) {
                Corset = new CorsetEffect(this);
            }

            BoneController boneController = GetComponent<BoneController>();


            boneController.AddBoneEffect(HideNipple);
            boneController.AddBoneEffect(Lift);
            boneController.AddBoneEffect(Angle);
            boneController.AddBoneEffect(Direction);
            boneController.AddBoneEffect(Spacing);
            boneController.AddBoneEffect(Squeeze);
            boneController.AddBoneEffect(Areola);
            boneController.AddBoneEffect(Nipple);
            boneController.AddBoneEffect(Corset);
        }

        protected override void OnCardBeingSaved(GameMode currentGameMode) {
        }
    }

    public abstract class PushUpEffect : BoneEffect {
        protected List<string> Bones;
        protected readonly BoneModifierData PushUpModifier = new BoneModifierData(Vector3.one, 1f, Vector3.zero, Vector3.zero);
        protected readonly PushUpBoneController PushUpBoneController;

        protected PushUpEffect(PushUpBoneController pushUpBoneController) {
            this.PushUpBoneController = pushUpBoneController;
        }

        public override IEnumerable<string> GetAffectedBones(BoneController origin) {
            return Bones;
        }

        public void Reset() {
            PushUpModifier.PositionModifier = Vector3.zero;
            PushUpModifier.RotationModifier = Vector3.zero;
            PushUpModifier.ScaleModifier = Vector3.one;
        }

        public override BoneModifierData GetEffect(string bone, BoneController origin, CoordinateType coordinate) {
            return (Bones.Contains(bone) && PushUpBoneController.EnablePushUp) ? PushUpModifier : null;
        }
    }

    public class CorsetEffect : PushUpEffect {
        internal CorsetEffect(PushUpBoneController pushUpBoneController) : base(pushUpBoneController) {
            Bones = new List<string> {
                "cf_J_Spine01_s"
            };
        }

        public void SetWidth(float trg, float val) {
            var x = PushUpModifier.ScaleModifier.x = (calcW(trg) * (1 / calcW(val)));
            PushUpModifier.ScaleModifier.x = x;
        }

        public void SetThickness(float trg, float val) {
            var vectorT = calcT(trg);
            var vectorV = calcT(val);
            PushUpModifier.ScaleModifier.z = (vectorT[2].z* (1 / vectorV[2].z));
            PushUpModifier.PositionModifier.z = (vectorT[0].z - vectorV[0].z);
        }

        private float calcW(float val) {
            var animkey = PushUpAiPlugin.getAnimKeyInfo();

            Vector3[] vector3Array = new Vector3[3];
            for (int index2 = 0; index2 < 3; ++index2)
                vector3Array[index2] = Vector3.zero;

            animkey.GetInfo("cf_s_Spine01_s", val, ref vector3Array, new[] {false, false, true});
            return vector3Array[2].x;
        }

        private Vector3[] calcT(float val) {
            var animkey = PushUpAiPlugin.getAnimKeyInfo();

            Vector3[] vector3Array = new Vector3[3];
            for (int index2 = 0; index2 < 3; ++index2)
                vector3Array[index2] = Vector3.zero;

            animkey.GetInfo("cf_s_Spine01_s_sz", val, ref vector3Array, new[] {true, false, true});
            return vector3Array;
        }

        public void ResetWidth() {
            PushUpModifier.ScaleModifier.x = 1;
        }

        public void ResetThickness() {
            PushUpModifier.ScaleModifier.z = 1;
            PushUpModifier.PositionModifier.z = 0;
        }
        public override BoneModifierData GetEffect(string bone, BoneController origin, CoordinateType coordinate) {
            return (Bones.Contains(bone) && PushUpBoneController.EnableCorset) ? PushUpModifier : null;
        }
    }

    public class HideNippleEffect : PushUpEffect {
        public HideNippleEffect(PushUpBoneController pushUpBoneController) : base(pushUpBoneController) {
            Bones = new List<string> {
                "cf_j_Mune04_s_L",
                "cf_j_Mune04_s_R"
            };
            PushUpModifier.ScaleModifier.x = 0.9f;
            PushUpModifier.ScaleModifier.y = 0.9f;
            PushUpModifier.ScaleModifier.z = 0.7f;
        }

        private bool isActive;

        public void SetActive(bool active) {
            isActive = active;
        }

        private readonly List<string> nippleBones = new List<string> {
            "cf_J_Mune_Nip02_s_L",
            "cf_J_Mune_Nip02_s_R"
        };

        private readonly BoneModifierData hideNippleMod = new BoneModifierData(new Vector3(0.7f, 0.7f, 0.7f), 1f, new Vector3(0, 0, -0.03f), Vector3.zero);

        public override IEnumerable<string> GetAffectedBones(BoneController origin) {
            return nippleBones;
        }

        private bool initialize = true;

        public override BoneModifierData GetEffect(string bone, BoneController origin, CoordinateType coordinate) {
            if (initialize) {
                //Workaround to ensure that breast softness is set correctly on loading a scene in studioNEO.
                initialize = false;
                return new BoneModifierData(Vector3.one, 1f, new Vector3(0, 0, 0.00001f), Vector3.zero);
            }

            if (!PushUpBoneController.EnablePushUp || !isActive) {
                return null;
            }

            if (nippleBones.Contains(bone)) {
                return hideNippleMod;
            }

            return Bones.Contains(bone) ? PushUpModifier : null;
        }
    }

    public class AreolaEffect : PushUpEffect {
        public AreolaEffect(PushUpBoneController pushUpBoneController) : base(pushUpBoneController) {
            Bones = new List<string> {
                "cf_J_Mune04_s_L",
                "cf_J_Mune04_s_R"
            };
        }

        public void SetValue(float val) {
            PushUpModifier.PositionModifier.z = (-1 - val) * 0.025f;
            float v2 = 1 + (val / 10f);
            float v3 = (1 / v2) * 0.9f;

            float scale = v3;

            PushUpModifier.ScaleModifier.x = scale;
            PushUpModifier.ScaleModifier.y = scale;
            PushUpModifier.ScaleModifier.z = scale;
        }
    }

    public class NippleEffect : PushUpEffect {
        public NippleEffect(PushUpBoneController pushUpBoneController) : base(pushUpBoneController) {
            Bones = new List<string> {
                "cf_J_Mune_Nip02_s_L",
                "cf_J_Mune_Nipacs01_L",
                "cf_J_Mune_Nip02_s_R",
                "cf_J_Mune_Nipacs01_R"
            };
        }

        private readonly BoneModifierData modifier2 = new BoneModifierData(Vector3.one, 1f, Vector3.zero, Vector3.zero);

        private readonly List<string> bones2 = new List<string> {
            "cf_J_Mune_Nipacs01_L",
            "cf_J_Mune_Nipacs01_R"
        };

        public void SetValue(float val) {
            PushUpModifier.PositionModifier.z = -(val * 0.08f);
            float scale = 1 / (1 + (val * 0.6f));
            PushUpModifier.ScaleModifier.x = scale;
            PushUpModifier.ScaleModifier.y = scale;
            PushUpModifier.ScaleModifier.z = scale;
            modifier2.PositionModifier.z = -(val * 0.06f);
        }

        public override BoneModifierData GetEffect(string bone, BoneController origin, CoordinateType coordinate) {
            if (!PushUpBoneController.EnablePushUp) {
                return null;
            }

            if (bones2.Contains(bone)) {
                return modifier2;
            }

            return Bones.Contains(bone) ? PushUpModifier : null;
        }
    }

    public class SqueezeEffect : PushUpEffect {
        public SqueezeEffect(PushUpBoneController pushUpBoneController) : base(pushUpBoneController) {
            Bones = new List<string> {
                "cf_J_Mune02_t_L",
                "cf_J_Mune01_t_L",
                "cf_J_Mune02_t_R",
                "cf_J_Mune01_t_R",
                "cf_J_Mune01_s_L",
                "cf_J_Mune01_s_R"
            };
        }

        private readonly BoneModifierData modifier2 = new BoneModifierData(Vector3.one, 1f, Vector3.zero, Vector3.zero);

        private readonly List<string> bones2 = new List<string> {
            "cf_J_Mune02_t_L",
            "cf_J_Mune02_t_R",
        };

        private readonly List<string> bonesSize = new List<string> {
            "cf_J_Mune01_s_L",
            "cf_J_Mune01_s_R"
        };

        private readonly BoneModifierData modifierSize = new BoneModifierData(Vector3.one, 1f, Vector3.zero, Vector3.zero);

        public void SetValue(float val, float valPush) {
            var len1 = calcLength(val, 0.3f) - calcLength(valPush, 0.3f);
            PushUpModifier.PositionModifier.z = -len1;
            var len2 = calcLength(val, 0.36f) - calcLength(valPush, 0.36f);
            modifier2.PositionModifier.z = -len2;

            float scaleMod = 1 + (val - valPush) * 0.2f;
            modifierSize.ScaleModifier.x = scaleMod;
            modifierSize.ScaleModifier.y = scaleMod;
        }

        private float calcLength(float val, float lMod) {
            float mod = val - 0.5f;
            if (mod < 0) {
                return mod * 0.1f;
            }

            return mod * lMod;
        }

        public void SetValueZero() {
            PushUpModifier.PositionModifier = Vector3.zero;
            modifier2.PositionModifier = Vector3.zero;
            modifierSize.ScaleModifier = Vector3.one;
        }

        public override BoneModifierData GetEffect(string bone, BoneController origin, CoordinateType coordinate) {
            if (!PushUpBoneController.EnablePushUp) {
                return null;
            }

            if (bones2.Contains(bone)) {
                return modifier2;
            }

            if (bonesSize.Contains(bone)) {
                return modifierSize;
            }

            return Bones.Contains(bone) ? PushUpModifier : null;
        }
    }

    public class DirectionEffect : PushUpEffect {
        public DirectionEffect(PushUpBoneController pushUpBoneController) : base(pushUpBoneController) {
            Bones = new List<string> {
                "cf_J_Mune00_d_L",
                "cf_J_Mune00_d_R"
            };
        }

        private readonly BoneModifierData inverseModifier = new BoneModifierData(Vector3.one, 1f, Vector3.zero, Vector3.zero);

        private const float PosEffect = 0.1f;
        private const float PosEffectZ = 0.1f;
        private const float RotEffect = 30f;

        public void SetValue(float val, float posZ) {
            PushUpModifier.PositionModifier.x = val * PosEffect;
            PushUpModifier.PositionModifier.z = posZ * PosEffectZ;
            PushUpModifier.RotationModifier.y = val * RotEffect;
            inverseModifier.PositionModifier.x = -(val * PosEffect);
            inverseModifier.PositionModifier.z = posZ * PosEffectZ;
            inverseModifier.RotationModifier.y = -(val * RotEffect);
        }

        public override BoneModifierData GetEffect(string bone, BoneController origin, CoordinateType coordinate) {
            if (!PushUpBoneController.EnablePushUp) {
                return null;
            }

            if (bone.Equals("cf_J_Mune00_d_R")) {
                return inverseModifier;
            }

            return Bones.Contains(bone) ? PushUpModifier : null;
        }
    }

    public class SpacingEffect : PushUpEffect {
        public SpacingEffect(PushUpBoneController pushUpBoneController) : base(pushUpBoneController) {
            Bones = new List<string> {
                "cf_J_Mune00_d_L",
                "cf_J_Mune00_d_R"
            };
        }

        private readonly BoneModifierData inverseModifier = new BoneModifierData(Vector3.one, 1f, Vector3.zero, Vector3.zero);

        private const float PosEffect = 0.4f;

        public void SetValue(float val) {
            PushUpModifier.PositionModifier.x = val * PosEffect;
            inverseModifier.PositionModifier.x = -(val * PosEffect);
        }

        public override BoneModifierData GetEffect(string bone, BoneController origin, CoordinateType coordinate) {
            if (!PushUpBoneController.EnablePushUp) {
                return null;
            }

            if (bone.Equals("cf_J_Mune00_d_R")) {
                return inverseModifier;
            }

            return Bones.Contains(bone) ? PushUpModifier : null;
        }
    }

    public class AngleEffect : PushUpEffect {
        public AngleEffect(PushUpBoneController pushUpBoneController) : base(pushUpBoneController) {
            Bones = new List<string> {
                "cf_J_Mune00_d_L",
                "cf_J_Mune02_t_L",
                "cf_J_Mune01_t_L",
                "cf_J_Mune00_d_R",
                "cf_J_Mune02_t_R",
                "cf_J_Mune01_t_R"
            };
        }

        private readonly List<string> rootBones = new List<string> {
            "cf_J_Mune00_d_L",
            "cf_J_Mune00_d_R"
        };

        private readonly BoneModifierData rootModifier = new BoneModifierData(Vector3.one, 1f, Vector3.zero, Vector3.zero);

        private const float Effect = 30f;

        public void SetValue(float val) {
            PushUpModifier.RotationModifier.x = val * Effect;
            rootModifier.RotationModifier.x = val * 10;
            rootModifier.PositionModifier.z = Math.Abs(val) * -0.03f;
        }

        public override BoneModifierData GetEffect(string bone, BoneController origin, CoordinateType coordinate) {
            if (!PushUpBoneController.EnablePushUp) {
                return null;
            }

            if (rootBones.Contains(bone)) {
                return rootModifier;
            }

            return Bones.Contains(bone) ? PushUpModifier : null;
        }
    }

    public class LiftEffect : PushUpEffect {
        internal LiftEffect(PushUpBoneController pushUpBoneController) : base(pushUpBoneController) {
            Bones = new List<string> {
                "cf_J_Mune00_t_L",
                "cf_J_Mune00_t_R"
            };
        }

        private const float Effect = 0.3f;

        public void SetValue(float val) {
            PushUpModifier.PositionModifier.y = val * Effect;
        }
    }
}