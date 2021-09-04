using System.Collections.Generic;
using System.Linq;
using Studio;

namespace MoveController 
{
    public static class FkManagerService 
    {
        private static int activeBoneIndex;
        private static int startBoneIndex = -1;
        private static int endBoneIndex = -1;

        private static List<OCIChar.BoneInfo> bones;

        public static OCIChar.BoneInfo ActiveBone { get; set; }

        private static void setBones(List<OCIChar.BoneInfo> _bones, int index) 
        {
            bones = _bones;
            activeBoneIndex = index;
        }

        private static void reset() 
        {
            startBoneIndex = -1;
            endBoneIndex = -1;
            if (bones != null) 
            {
                bones.ForEach(b => b.guideObject.isActive = false);
            }
        }

        private static void reset(OCIChar.BoneInfo activeBone) 
        {
            reset();
            if (!activeBone.guideObject.isActive) 
            {
                activeBone.guideObject.isActive = true;
            }
        }


        public static void up() 
        {
            if (activeBoneIndex < bones.Count - 1) 
            {
                if (startBoneIndex != -1 && startBoneIndex > activeBoneIndex) 
                {
                    activeBoneIndex = startBoneIndex;
                }

                GuideObjectManager.Instance.selectObject = bones[++activeBoneIndex].guideObject;
            }
        }

        public static void down() 
        {
            if (activeBoneIndex > 0) 
            {
                if (startBoneIndex != -1 && endBoneIndex < activeBoneIndex) 
                {
                    activeBoneIndex = endBoneIndex;
                }

                GuideObjectManager.Instance.selectObject = bones[--activeBoneIndex].guideObject;
            }
        }

        public static void multiUp() 
        {
            if (startBoneIndex == -1) 
            {
                startBoneIndex = activeBoneIndex;
                endBoneIndex = activeBoneIndex;
            }
            else if (endBoneIndex < activeBoneIndex) 
            {
                activeBoneIndex = endBoneIndex;
            }

            if (startBoneIndex < activeBoneIndex) //TODO: update activeboneindex?
            {
                bones[startBoneIndex++].guideObject.isActive = false;
            }
            else if (endBoneIndex < bones.Count - 1) 
            {
                bones[++endBoneIndex].guideObject.isActive = true;
            }
        }

        public static void multiDown() 
        {
            if (startBoneIndex == -1) 
            {
                startBoneIndex = activeBoneIndex;
                endBoneIndex = activeBoneIndex;
            }
            else if (startBoneIndex > activeBoneIndex) 
            {
                activeBoneIndex = startBoneIndex;
            }

            if (endBoneIndex > activeBoneIndex) 
            {
                bones[endBoneIndex--].guideObject.isActive = false;
            }
            else if (startBoneIndex > 0) 
            {
                bones[--startBoneIndex].guideObject.isActive = true;
            }
        }

        public static void slideUp() 
        {
            if (startBoneIndex == -1) 
            {
                up();
                return;
            }

            if (endBoneIndex < bones.Count - 1) 
            {
                bones[++endBoneIndex].guideObject.isActive = true;
                bones[startBoneIndex++].guideObject.isActive = false;
                activeBoneIndex = startBoneIndex;
            }
        }

        public static void slideDown() 
        {
            if (startBoneIndex == -1) 
            {
                down();
                return;
            }

            if (startBoneIndex > 0) 
            {
                bones[--startBoneIndex].guideObject.isActive = true;
                bones[endBoneIndex--].guideObject.isActive = false;
                activeBoneIndex = endBoneIndex;
            }
        }


        internal static List<OIBoneInfo> getActiveBones()
        {
            if (bones == null || bones.Count == 0)
                return new List<OIBoneInfo>();
            
            if (startBoneIndex == -1) 
            {
                return new List<OIBoneInfo> {bones[activeBoneIndex].boneInfo};
            }

            return bones.GetRange(startBoneIndex, endBoneIndex - startBoneIndex + 1).Select(b => b.boneInfo).ToList();
        }

        internal static void updateFkScale(float x) 
        {
            var localBones = bones;

            if (localBones == null && GuideObjectManager.Instance.selectObject != null) 
            {
                localBones = getBonesIfExist(GuideObjectManager.Instance.selectObject);
            }

            if (localBones != null) 
            {
                foreach (var bone in localBones) 
                {
                    bone.guideObject.scaleRate = x;
                }
            }
        }

        private static List<OCIChar.BoneInfo> getBonesIfExist(GuideObject guide) 
        {
            List<OCIChar.BoneInfo> bones = null;
            ObjectCtrlInfo tempSel;
            Studio.Studio.Instance.dicObjectCtrl.TryGetValue(guide.dicKey, out tempSel);
            
            if (tempSel != null) 
            {
                if (tempSel is OCIItem) 
                {
                    var selected = tempSel as OCIItem;
                    if (selected.isFK && selected.itemFKCtrl.enabled) 
                    {
                        bones = selected.listBones;
                    }
                } 
                else if (tempSel is OCIChar) 
                {
                    var selected = tempSel as OCIChar;
                    if (selected.fkCtrl.enabled) 
                    {
                        bones = selected.listBones;
                    }
                }
            }

            return bones;
        }

        internal static bool checkIfFkNodeSelected() 
        {
            var guide = GuideObjectManager.Instance.selectObject;
            if (ActiveBone != null && guide == ActiveBone.guideObject) 
            {
                return true;
            }

            if (guide != null) 
            {
                while (true) 
                {
                    bones = getBonesIfExist(guide);
                    if (bones != null) 
                    {
                        for (var i = 0; i < bones.Count; i++) 
                        {
                            var bone = bones[i];
                            if (bone.guideObject == GuideObjectManager.Instance.selectObject) 
                            {
                                setBones(bones, i);
                                if (ActiveBone != bone) 
                                {
                                    reset(bone);
                                }

                                ActiveBone = bone;
                                return true;
                            }
                        }
                    }

                    if (guide.parentGuide == null) 
                    {
                        reset();
                        ActiveBone = null;

                        return false;
                    }

                    guide = guide.parentGuide;
                }
            }

            return false;
        }
    }
}