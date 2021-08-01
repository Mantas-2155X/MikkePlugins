using System;
using System.Collections.Generic;
using System.Linq;
using Studio;
using UnityEngine;

namespace MoveController 
{
    public static class MoveObjectService 
    {
        private static readonly float BaseMoveSpeedFactor = 0.5f;
        private static readonly float BaseRotationSpeedFactor = 2f;
        private static readonly float BaseAnimationSpeedFactor = 0.05f;
        private static readonly float BaseSizeSpeedFactor = 0.03f;

        private static float moveSpeedFactor = BaseMoveSpeedFactor;
        private static float rotationSpeedFactor = BaseRotationSpeedFactor;
        private static float sizeSpeedFactor = BaseAnimationSpeedFactor;
        private static float animationSpeedFactor = BaseSizeSpeedFactor;
        
        public static bool IkSelected { get; set; }
        
        public static void updateSpeedFactors(float val) 
        {
            moveSpeedFactor = BaseMoveSpeedFactor * (calcMoveSpeed(val) / 5f);
            rotationSpeedFactor = BaseRotationSpeedFactor * (val / 5f);
            animationSpeedFactor = BaseAnimationSpeedFactor * (val / 5f);
            sizeSpeedFactor = BaseSizeSpeedFactor * (val / 5f); // (calcMoveSpeed(val) / 5f);
        }

        private static float calcMoveSpeed(float x) 
        {
            return (float) (Math.Pow(x / Math.Sqrt(10), 2) * 2);
        }

        public static void MoveObjectsToCamera(List<ObjectCtrlInfo> selectedObjs, bool relative) 
        {
            if (selectedObjs.Count < 1) 
            {
                return;
            }

            UndoRedoService.StoreOldPositions(selectedObjs);

            Vector3 primaryOffset = selectedObjs[0].guideObject.transformTarget.position;

            foreach (var obj in selectedObjs) 
            {
                if (relative) 
                {
                    obj.guideObject.transformTarget.position = MoveCtrlPlugin.cameraControl.targetPos +
                        obj.guideObject.transformTarget.position - primaryOffset;
                } 
                else 
                {
                    obj.guideObject.transformTarget.position = MoveCtrlPlugin.cameraControl.targetPos;
                }

                obj.guideObject.changeAmount.pos = obj.guideObject.transformTarget.localPosition;
            }

            UndoRedoService.CreateUndoForMove(selectedObjs);
        }
        
        public static void controlAnimation(List<ObjectCtrlInfo> selectedObjs, Vector2 input) 
        {
            try 
            {
                foreach (ObjectCtrlInfo ctrlInfo in selectedObjs) 
                {
                    Animator animator = null;
                    if (ctrlInfo is OCIChar ociChar) 
                    {
                        animator = ociChar.charAnimeCtrl.animator;
                    } 
                    else if (ctrlInfo is OCIItem ociItem && ociItem.isAnime) 
                    {
                        animator = ociItem.animator;
                    }

                    if (animator == null) 
                        continue;
                    
                    var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                    var clipInfo = animator.GetCurrentAnimatorClipInfo(0);

                    var normalizedTimeSkip = (input.x * animationSpeedFactor) / clipInfo[0].clip.length;
                    var normalizedTime = stateInfo.normalizedTime + normalizedTimeSkip;

                    if (stateInfo.loop == false) 
                    {
                        if (normalizedTime < 0) normalizedTime = 0;
                        if (normalizedTime > 1) normalizedTime = 1;
                    }

                    animator.Play(0, 0, normalizedTime);
                }
            } 
            catch (Exception e) 
            {
                Debug.Log(e.ToString());
            }
        }

        public static void resizeObj(List<ObjectCtrlInfo> selectedObjs, Vector3 sizeAmount) 
        {
            Vector3 sof = sizeAmount * sizeSpeedFactor;
            GuideCommand.EqualsInfo[] resizeCommands = new GuideCommand.EqualsInfo[selectedObjs.Count];
            int i = 0;
            foreach (var obj in selectedObjs) 
            {
                Vector3 currentSize = obj.objectInfo.changeAmount.scale;
                GuideCommand.EqualsInfo eqSize = new GuideCommand.EqualsInfo();
                eqSize.dicKey = obj.objectInfo.dicKey;
                eqSize.oldValue = currentSize;
                eqSize.newValue = currentSize + sof;
                resizeCommands[i++] = eqSize;
            }

            var sizeCom = new GuideCommand.ScaleEqualsCommand(resizeCommands);
            sizeCom.Do();

            //TODO: undo for resize
        }

        public static void MoveObj(List<ObjectCtrlInfo> selectedObjs, Vector3 moveAmount) 
        {
            var amount = moveAmount * moveSpeedFactor;

            foreach (var obj in selectedObjs) 
            {
                obj.guideObject.transformTarget.Translate(amount, Space.World);
                obj.guideObject.changeAmount.pos = obj.guideObject.transformTarget.localPosition;
            }
        }

        public static void MoveIk(Vector3 moveAmount) 
        {
            var ikGuide = Singleton<GuideObjectManager>.Instance.selectObject;
            if (ikGuide == null) return;

            var amount = moveAmount * moveSpeedFactor;

            ikGuide.transformTarget.Translate(amount, Space.World);
            ikGuide.changeAmount.pos = ikGuide.transformTarget.localPosition;
        }

        public static void RotateIk(Vector3 rotAmount) 
        {
            var ikGuide = Singleton<GuideObjectManager>.Instance.selectObject;
            if (ikGuide == null) return;

            var amount = rotAmount * rotationSpeedFactor;

            GuideCommand.AddInfo[] rotated = new GuideCommand.AddInfo[1];

            int dicKey = ikGuide.dicKey;

            rotated[0] = new GuideCommand.AddInfo() 
            {
                value = amount,
                dicKey = dicKey
            };

            var rotateCom = new GuideCommand.RotationAddCommand(rotated);
            rotateCom.Do();
        }

        public static GuideCommand.AddInfo[] TransformAllSelected(List<ObjectCtrlInfo> selectedObjs, Vector3 Amount) 
        {
            GuideCommand.AddInfo[] moves = new GuideCommand.AddInfo[selectedObjs.Count];
            for (int i = 0; i < selectedObjs.Count; i++) 
            {
                GuideCommand.AddInfo addMove = new GuideCommand.AddInfo();
                addMove.dicKey = selectedObjs.ElementAt(i).objectInfo.dicKey;
                addMove.value = Amount;
                moves[i] = addMove;
            }

            return moves;
        }

        public static void rotateFk(List<OIBoneInfo> bones, Vector3 rotAmount, bool useSpeedFactor) 
        {
            GuideCommand.EqualsInfo[] moves = new GuideCommand.EqualsInfo[bones.Count];
            int index = 0;
            foreach (var bone in bones) 
            {
                var rof = useSpeedFactor ? rotAmount * rotationSpeedFactor : rotAmount;
                Vector3 currentBoneRotation = bone.changeAmount.rot;

                Vector3 eulerAngles = (Quaternion.Euler(currentBoneRotation) * Quaternion.Euler(rof)).eulerAngles;
                eulerAngles.x %= 360f;
                eulerAngles.y %= 360f;
                eulerAngles.z %= 360f;


                GuideCommand.EqualsInfo addMove = new GuideCommand.EqualsInfo();
                addMove.dicKey = bone.dicKey; //
                addMove.newValue = eulerAngles;
                addMove.oldValue = currentBoneRotation;

                moves[index++] = addMove;
            }

            var rotateCom = new GuideCommand.RotationEqualsCommand(moves);
            rotateCom.Do();
        }


        public static void RotateObj(List<ObjectCtrlInfo> selectedObjs, Vector3 rotAmount, bool useSpeedFactor) 
        {
            var rof = useSpeedFactor ? rotAmount * rotationSpeedFactor : rotAmount;
            UndoRedoService.RotationDelta += rof;

            var rotateCom = new GuideCommand.RotationAddCommand(TransformAllSelected(selectedObjs, rof)); //TODO: modulo 360?
            rotateCom.Do();
        }

        public static void RotateObjAsGuided(List<ObjectCtrlInfo> selectedObjs, Vector3 rotAmount, bool useSpeedFactor) 
        {
            var rof = useSpeedFactor ? rotAmount * rotationSpeedFactor : rotAmount;
            UndoRedoService.RotationDelta += rof;
            GuideCommand.EqualsInfo[] moves = new GuideCommand.EqualsInfo[selectedObjs.Count];
            int index = 0;
            foreach (var obj in selectedObjs) 
            {
                Vector3 currentBoneRotation = obj.objectInfo.changeAmount.rot;

                Vector3 eulerAngles = (Quaternion.Euler(currentBoneRotation) * Quaternion.Euler(rof)).eulerAngles;
                eulerAngles.x %= 360f;
                eulerAngles.y %= 360f;
                eulerAngles.z %= 360f;


                GuideCommand.EqualsInfo addMove = new GuideCommand.EqualsInfo();
                addMove.dicKey = obj.objectInfo.dicKey;
                addMove.newValue = eulerAngles;
                addMove.oldValue = currentBoneRotation;

                moves[index++] = addMove;
            }

            var rotateCom = new GuideCommand.RotationEqualsCommand(moves);

            rotateCom.Do();
        }


        public static void RotateRelative(List<ObjectCtrlInfo> selectedObjs, Vector3 rotAmount) 
        {
            Vector3 rof = rotAmount * rotationSpeedFactor;
            UndoRedoService.RotationDelta += rof;

            MoveAndRotateAddCommand moveAddCom = moveAndRotateAllSelected(selectedObjs, rof, false);
            if (moveAddCom != null) 
            {
                moveAddCom.Do();
            }
        }

        public static void RotateByCamera(List<ObjectCtrlInfo> windowAllSelected, Vector3 angle, Vector2 rotAmount, bool useSpeedFactor) 
        {
            var amount = useSpeedFactor ? rotAmount * rotationSpeedFactor : rotAmount;
            // foreach (GuideObject guideObject in Singleton<GuideObjectManager>.Instance.selectObjects.Where(v => v.enableRot))
            foreach (GuideObject guideObject in windowAllSelected.ConvertAll(oc => oc.guideObject))
                guideObject.Rotation(angle, amount.x);
        }

        public static MoveAndRotateAddCommand moveAndRotateAllSelected(List<ObjectCtrlInfo> selectedObjs, Vector3 rof, bool isUndo) 
        {
            if (selectedObjs.Count < 1) 
            {
                return null;
            }

            GuideCommand.AddInfo[] moves = new GuideCommand.AddInfo[selectedObjs.Count];
            int primaryDicKey = selectedObjs.ElementAt(0).objectInfo.dicKey;
            ChangeAmount primaryChangeAmount = Studio.Studio.GetChangeAmount(primaryDicKey);
            Vector3 primaryPos = primaryChangeAmount.pos;
            for (int i = 0; i < selectedObjs.Count; i++) 
            {
                GuideCommand.AddInfo eqMove = new GuideCommand.AddInfo();
                int dickEy = selectedObjs.ElementAt(i).objectInfo.dicKey; //lol
                eqMove.dicKey = dickEy;
                ChangeAmount changeAmount = Studio.Studio.GetChangeAmount(dickEy);
                Vector3 diff = changeAmount.pos - primaryPos;
                Vector3 newDiff = Quaternion.Euler(rof) * diff;

                eqMove.value = newDiff - diff;
                moves[i] = eqMove;
            }


            GuideCommand.AddInfo[] rots = new GuideCommand.AddInfo[selectedObjs.Count];
            for (int i = 0; i < selectedObjs.Count; i++) 
            {
                GuideCommand.AddInfo addRot = new GuideCommand.AddInfo();
                addRot.dicKey = selectedObjs.ElementAt(i).objectInfo.dicKey;
                addRot.value = rof;
                rots[i] = addRot;
            }

            var moveCom = new GuideCommand.MoveAddCommand(moves);
            var rotateCom = new GuideCommand.RotationAddCommand(rots);

            var moveAddCom = new MoveAndRotateAddCommand(rotateCom, moveCom);
            return moveAddCom;
        }

        internal static void resetFKRotation(List<OIBoneInfo> bones) 
        {
            // undoRedoService.createUndoForFK(bones);

            GuideCommand.EqualsInfo[] moves = new GuideCommand.EqualsInfo[bones.Count];
            int index = 0;
            foreach (var bone in bones) 
            {
                Vector3 currentBoneRotation = bone.changeAmount.rot;

                GuideCommand.EqualsInfo addMove = new GuideCommand.EqualsInfo();
                addMove.dicKey = bone.dicKey; //
                addMove.newValue = Vector3.zero;
                addMove.oldValue = currentBoneRotation;

                moves[index++] = addMove;
            }

            var rotateCom = new GuideCommand.RotationEqualsCommand(moves);

            rotateCom.Do();
            UndoRedoManager.Instance.Push(rotateCom);
        }

        public static bool CheckIfIkSelected() 
        {
            var guided = Singleton<GuideObjectManager>.Instance.selectObject;
            if (guided == null) {
                //TODO: figure out how to block IK control when guideobjects are hidden?
                return false;
            }
            
            if (guided.guideSelect.isActiveAndEnabled) 
            {
                return false;
            }

            var selectedObj = Studio.Studio.GetSelectObjectCtrl()[0];
            if (selectedObj is OCIChar selected && selected.ikCtrl.enabled) 
            {
                if (selected.listIKTarget.Exists(ik => ik.guideObject == guided)) 
                {
                    IkSelected = true;
                    return true;
                }
            }

            return false;
        }

        public static bool CheckIfIkRotSelected() 
        {
            if (CheckIfIkSelected() == false) 
            {
                return false;
            }

            var guided = Singleton<GuideObjectManager>.Instance.selectObject;
            if (!guided.enableRot) 
            {
                IkSelected = false;
                return false;
            }

            return true;
        }
    }
}