using Studio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MoveController
{

    class FkManagerService
    {
        private int activeBoneIndex = 0;
        int startBoneIndex = -1;
        int endBoneIndex = -1;  
                
        List<OCIChar.BoneInfo> bones = null;

        public OCIChar.BoneInfo ActiveBone { get; set; } = null;

        public void setBones(List<OCIChar.BoneInfo> bones, int index)
        {
            this.bones = bones;
            this.activeBoneIndex = index;   
        }

        internal void reset()
        {
            startBoneIndex = -1;
            endBoneIndex = -1;
            if (bones != null)
            {
                bones.ForEach(b => b.guideObject.isActive = false);

            }
        }

        public void reset(OCIChar.BoneInfo activeBone)
        {
            reset(); 
            if (!activeBone.guideObject.isActive)
            {
                activeBone.guideObject.isActive = true;
            }
        }


        public void up()
        {
            if(activeBoneIndex < bones.Count - 1)
            {
                if (startBoneIndex != -1 && startBoneIndex > activeBoneIndex)
                {
                    activeBoneIndex = startBoneIndex;
                }

                GuideObjectManager.Instance.selectObject = bones[++activeBoneIndex].guideObject;
            }
        }

        public void down()
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

        public void multiUp()
        {
            if(startBoneIndex == -1)
            {
                startBoneIndex = activeBoneIndex;
                endBoneIndex = activeBoneIndex;         
            }
            else if (endBoneIndex < activeBoneIndex)
            {
                activeBoneIndex = endBoneIndex;
            }

            if (startBoneIndex < activeBoneIndex)//TODO: update activeboneindex?
            {
                bones[startBoneIndex++].guideObject.isActive = false;
            }
            else if(endBoneIndex < bones.Count - 1)
            {            
                bones[++endBoneIndex].guideObject.isActive = true;
            }

        }

        public void multiDown()
        {
            if (startBoneIndex == -1)
            {
                startBoneIndex = activeBoneIndex;
                endBoneIndex = activeBoneIndex;         
            }
            else if(startBoneIndex > activeBoneIndex)
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

        public void slideUp()
        {
            if(startBoneIndex == -1)
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

        public void slideDown()
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
  

        internal List<OIBoneInfo> getActiveBones()
        {
            if(startBoneIndex == -1)
            {
                return new List<OIBoneInfo>() { bones[activeBoneIndex].boneInfo };
            }

            return bones.GetRange(startBoneIndex, endBoneIndex - startBoneIndex + 1).Select(b => b.boneInfo).ToList();

        }

        internal void updateFkScale(float x)
        {
            List<OCIChar.BoneInfo> localBones = bones;

            if (localBones == null && GuideObjectManager.Instance.selectObject !=null)
            {
                localBones = getBonesIfExist(GuideObjectManager.Instance.selectObject);
            }

            if (localBones != null)
            {
                foreach(var bone in localBones)
                {
                    bone.guideObject.scaleRate = x;                 
                }
            }
        }

        private static List<OCIChar.BoneInfo> getBonesIfExist( GuideObject guide)
        {
            List<OCIChar.BoneInfo> bones = null;
            ObjectCtrlInfo tempSel;
            Studio.Studio.Instance.dicObjectCtrl.TryGetValue(guide.dicKey, out tempSel);
            if (tempSel != null)
            {

                if (tempSel is OCIItem)
                {
                    OCIItem selected = tempSel as OCIItem;
                    if (selected.isFK && selected.itemFKCtrl.enabled == true)
                    {
                        bones = selected.listBones;
                    }
                }
                else if (tempSel is OCIChar)
                {
                    OCIChar selected = tempSel as OCIChar;
                    if (selected.fkCtrl.enabled == true)
                    {
                        bones = selected.listBones;
                    }
                }
            }
            return bones;
        }

        internal bool checkIfFkNodeSelected()
        {          
            GuideObject guide = GuideObjectManager.Instance.selectObject;
            if(ActiveBone != null && guide == ActiveBone.guideObject)
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
                        for (int i = 0; i < bones.Count; i++)
                        {
                            OCIChar.BoneInfo bone = bones[i];
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
