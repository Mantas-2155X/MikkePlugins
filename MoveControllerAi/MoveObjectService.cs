using System;
using System.Collections.Generic;
using System.Linq;
using Studio;
using UnityEngine;

namespace MoveController {
    class MoveObjectService {
        private const float BaseMoveSpeedFactor = 0.5f;
        private const float BaseRotationSpeedFactor = 2f;
        private const float BaseAnimationSpeedFactor = 0.05f;
        private const float BaseSizeSpeedFactor = 0.03f;

        internal float moveSpeedFactor = 1f;
        internal float rotationSpeedFactor = 1f;
        internal float sizeSpeedFactor = 1f;
        internal float animationSpeedFactor = 1f;

        private Studio.CameraControl cameraControl;

        private UndoRedoService undoRedoService;

        //   public bool IkSelected { get; set; } = false;

        public MoveObjectService(Studio.CameraControl cameraControl, UndoRedoService undoRedoService) {
            this.cameraControl = cameraControl;
            this.undoRedoService = undoRedoService;

            moveSpeedFactor = BaseMoveSpeedFactor;
            rotationSpeedFactor = BaseRotationSpeedFactor;
            animationSpeedFactor = BaseAnimationSpeedFactor;
            sizeSpeedFactor = BaseSizeSpeedFactor;
        }

        public void updateSpeedFactors(float val) {
            moveSpeedFactor = BaseMoveSpeedFactor * (calcMoveSpeed(val) / 5f);
            rotationSpeedFactor = BaseRotationSpeedFactor * (val / 5f);
            animationSpeedFactor = BaseAnimationSpeedFactor * (val / 5f);
            sizeSpeedFactor = BaseSizeSpeedFactor * (val / 5f); // (calcMoveSpeed(val) / 5f);
        }

        private static float calcMoveSpeed(float x) {
            return (float) (Math.Pow(x / Math.Sqrt(10), 2) * 2);
        }

        public void MoveObjectsToCamera(List<ObjectCtrlInfo> selectedObjs, bool relative) {
            if (selectedObjs.Count < 1) {
                return;
            }

            GuideCommand.EqualsInfo[] moves = new GuideCommand.EqualsInfo[selectedObjs.Count];
            int primaryDicKey = selectedObjs.ElementAt(0).objectInfo.dicKey;
            ChangeAmount primaryChangeAmount = Studio.Studio.GetChangeAmount(primaryDicKey);
            Vector3 primaryOffset = cameraControl.targetPos - primaryChangeAmount.pos;
            for (int i = 0; i < selectedObjs.Count; i++) {
                GuideCommand.EqualsInfo eqMove = new GuideCommand.EqualsInfo();
                int dickEy = selectedObjs.ElementAt(i).objectInfo.dicKey; //lol
                eqMove.dicKey = dickEy;
                ChangeAmount changeAmount = Studio.Studio.GetChangeAmount(dickEy);
                eqMove.oldValue = changeAmount.pos;
                if (relative) {
                    eqMove.newValue = changeAmount.pos + primaryOffset;
                } else {
                    eqMove.newValue = cameraControl.targetPos;
                }

                moves[i] = eqMove;
            }

            var moveCom = new GuideCommand.MoveEqualsCommand(moves);
            moveCom.Do();
            UndoRedoManager.Instance.Push(moveCom);
        }


        public void controlAnimation(List<ObjectCtrlInfo> selectedObjs, Vector2 input) {
            try {
                foreach (ObjectCtrlInfo ctrlInfo in selectedObjs) {
                    Animator animator = null;
                    if (ctrlInfo is OCIChar ociChar) {
                        animator = ociChar.charAnimeCtrl.animator;
                    } else if (ctrlInfo is OCIItem ociItem && ociItem.isAnime) {
                        animator = ociItem.animator;
                    }

                    if (animator == null) continue;
                    var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                    var clipInfo = animator.GetCurrentAnimatorClipInfo(0);

                    var normalizedTimeSkip = (input.x * animationSpeedFactor) / clipInfo[0].clip.length;
                    var normalizedTime = stateInfo.normalizedTime + normalizedTimeSkip;

                    if (stateInfo.loop == false) {
                        if (normalizedTime < 0) normalizedTime = 0;
                        if (normalizedTime > 1) normalizedTime = 1;
                    }

                    animator.Play(0, 0, normalizedTime);
                }
            } catch (Exception e) {
                Debug.Log(e.ToString());
            }
        }

        public void resizeObj(List<ObjectCtrlInfo> selectedObjs, Vector3 sizeAmount) {
            Vector3 sof = sizeAmount * sizeSpeedFactor;
            GuideCommand.EqualsInfo[] resizeCommands = new GuideCommand.EqualsInfo[selectedObjs.Count];
            int i = 0;
            foreach (var obj in selectedObjs) {
                Vector3 currentSize = obj.objectInfo.changeAmount.scale;
                GuideCommand.EqualsInfo eqSize = new GuideCommand.EqualsInfo();
                eqSize.dicKey = obj.objectInfo.dicKey;
                eqSize.oldValue = currentSize;
                eqSize.newValue = currentSize + sof;
                resizeCommands[i++] = eqSize;
            }

            var sizeCom = new GuideCommand.ScaleEqualsCommand(resizeCommands);
            sizeCom.Do();
        }

        public void MoveObj(List<ObjectCtrlInfo> selectedObjs, Vector3 moveAmount) {
            Vector3 mof = moveAmount * moveSpeedFactor;
            undoRedoService.MoveDelta += mof;

            GuideCommand.AddInfo[] moves = TransformAllSelected(selectedObjs, mof);
            var moveCom = new GuideCommand.MoveAddCommand(moves);
            moveCom.Do();
        }

        /*
        public void MoveIk(Vector3 moveAmount) {
          //  List<GuideObject> Objs = new List<GuideObject>(Singleton<GuideObjectManager>.Instance.selectObjects);
            Vector3 mof = moveAmount * moveSpeedFactor;
            undoRedoService.MoveDelta += mof;

            GuideCommand.AddInfo[] moves = TransformAllGuided(Singleton<GuideObjectManager>.Instance.selectObject, mof);
            var moveCom = new GuideCommand.MoveAddCommand(moves);
            moveCom.Do();
        }
        
        public void MoveIk2(Vector3 moveAmount) {
            var obj = Singleton<GuideObjectManager>.Instance.selectObject;
            var worldPosition = obj.transform.position;
            var newPos = worldPosition += moveAmount;

            var newLocPos = obj.transform.InverseTransformPoint(newPos);

            var localDiff = newLocPos - obj.transform.localPosition;
            
            obj.MoveLocal(localDiff);



        }

        // public GuideCommand.AddInfo[] TransformAllGuided(List<GuideObject> guidedObjs, Vector3 amount) {
        public GuideCommand.AddInfo[] TransformAllGuided(GuideObject guide, Vector3 amount) {
            GuideCommand.AddInfo[] moves = new GuideCommand.AddInfo[1];
            // for (int i = 0; i < guidedObjs.Count; i++) {
            // var guide = guidedObjs.ElementAt(i);

            var localAmount = amount;
            localAmount = guide.transform.InverseTransformVector(amount);
            guide.MoveWorld(amount);
            // var parent = guide.parentGuide;
            //  if (parent != null) {
            //     localAmount = Quaternion.Euler(parent.changeAmount.rot) * amount;
            //}

            GuideCommand.AddInfo addMove = new GuideCommand.AddInfo();
            addMove.dicKey = guide.dicKey;
            addMove.value = localAmount;
            moves[0] = addMove;
            // }

            return moves;
        }*/


        public GuideCommand.AddInfo[] TransformAllSelected(List<ObjectCtrlInfo> selectedObjs, Vector3 Amount) {
            GuideCommand.AddInfo[] moves = new GuideCommand.AddInfo[selectedObjs.Count];
            for (int i = 0; i < selectedObjs.Count; i++) {
                GuideCommand.AddInfo addMove = new GuideCommand.AddInfo();
                addMove.dicKey = selectedObjs.ElementAt(i).objectInfo.dicKey;
                addMove.value = Amount;
                moves[i] = addMove;
            }

            return moves;
        }

        public void rotateFk(List<OIBoneInfo> bones, Vector3 rotAmount, bool useSpeedFactor) {
            GuideCommand.EqualsInfo[] moves = new GuideCommand.EqualsInfo[bones.Count];
            int index = 0;
            foreach (var bone in bones) {
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


        public void RotateObj(List<ObjectCtrlInfo> selectedObjs, Vector3 rotAmount, bool useSpeedFactor) {
            var rof = useSpeedFactor ? rotAmount * rotationSpeedFactor : rotAmount;
            undoRedoService.RotationDelta += rof;

            var rotateCom = new GuideCommand.RotationAddCommand(TransformAllSelected(selectedObjs, rof)); //TODO: modulo 360?
            rotateCom.Do();
        }

        public void RotateObjAsGuided(List<ObjectCtrlInfo> selectedObjs, Vector3 rotAmount, bool useSpeedFactor) {
            var rof = useSpeedFactor ? rotAmount * rotationSpeedFactor : rotAmount;
            undoRedoService.RotationDelta += rof;
            GuideCommand.EqualsInfo[] moves = new GuideCommand.EqualsInfo[selectedObjs.Count];
            int index = 0;
            foreach (var obj in selectedObjs) {
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


        public void RotateRelative(List<ObjectCtrlInfo> selectedObjs, Vector3 rotAmount) {
            Vector3 rof = rotAmount * rotationSpeedFactor;
            undoRedoService.RotationDelta += rof;

            MoveAndRotateAddCommand moveAddCom = moveAndRotateAllSelected(selectedObjs, rof, false);
            if (moveAddCom != null) {
                moveAddCom.Do();
            }
        }

        public void RotateByCamera(List<ObjectCtrlInfo> windowAllSelected, Vector3 angle, Vector2 rotAmount, bool useSpeedFactor) {
            var amount = useSpeedFactor ? rotAmount * rotationSpeedFactor : rotAmount;
            // foreach (GuideObject guideObject in Singleton<GuideObjectManager>.Instance.selectObjects.Where(v => v.enableRot))
            foreach (GuideObject guideObject in windowAllSelected.ConvertAll(oc => oc.guideObject))
                guideObject.Rotation(angle, amount.x);
        }

        public MoveAndRotateAddCommand moveAndRotateAllSelected(List<ObjectCtrlInfo> selectedObjs, Vector3 rof, bool isUndo) {
            if (selectedObjs.Count < 1) {
                return null;
            }

            GuideCommand.AddInfo[] moves = new GuideCommand.AddInfo[selectedObjs.Count];
            int primaryDicKey = selectedObjs.ElementAt(0).objectInfo.dicKey;
            ChangeAmount primaryChangeAmount = Studio.Studio.GetChangeAmount(primaryDicKey);
            Vector3 primaryPos = primaryChangeAmount.pos;
            for (int i = 0; i < selectedObjs.Count; i++) {
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
            for (int i = 0; i < selectedObjs.Count; i++) {
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

        internal void resetFKRotation(List<OIBoneInfo> bones) {
            // undoRedoService.createUndoForFK(bones);

            GuideCommand.EqualsInfo[] moves = new GuideCommand.EqualsInfo[bones.Count];
            int index = 0;
            foreach (var bone in bones) {
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

        /*public bool CheckIfIkSelected() {
            var guided = Singleton<GuideObjectManager>.Instance.selectObject;
            if (guided == null) {
                //TODO: figure out how to block IK control when guideobjects are hidden.
                return false;
            }

            var selectedObj = Studio.Studio.GetSelectObjectCtrl()[0];
            if (selectedObj is OCIChar selected && selected.ikCtrl.enabled) {
                if (selected.listIKTarget.Exists(ik => ik.guideObject == guided)) {
                    IkSelected = true;
                    return true;
                }
            }

            return false;
        }*/
    }
}