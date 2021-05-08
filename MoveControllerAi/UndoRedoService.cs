using System.Collections.Generic;
using Studio;
using UnityEngine;

namespace MoveController {
    class UndoRedoService {
        public Vector3 MoveDelta = Vector3.zero;
        public Vector3 RotationDelta = Vector3.zero;
        public Dictionary<int, Vector3> OldFkRotations = new Dictionary<int, Vector3>();

        public Dictionary<int, Vector3> OldRotations = new Dictionary<int, Vector3>();

        MoveObjectService moveObjectService;

        public GuideCommand.EqualsInfo[] CreateUndoRotateForAllSelected(List<ObjectCtrlInfo> selectedObjs, bool isResize) {
            GuideCommand.EqualsInfo[] rotations = new GuideCommand.EqualsInfo[selectedObjs.Count];
            int i = 0;

            foreach (ObjectCtrlInfo selected in selectedObjs) {
                int dicKey = selected.objectInfo.dicKey;
                ChangeAmount changeAmount = Studio.Studio.GetChangeAmount(dicKey);
                if (changeAmount != null && OldRotations.TryGetValue(dicKey, out Vector3 oldValue)) {
                    GuideCommand.EqualsInfo eqRot = new GuideCommand.EqualsInfo();
                    eqRot.dicKey = dicKey;
                    if (isResize) {
                        eqRot.newValue = changeAmount.scale;
                    } else {
                        eqRot.newValue = changeAmount.rot;
                    }

                    eqRot.oldValue = oldValue;
                    rotations[i++] = eqRot;
                }
            }

            return rotations;
        }

        public void createUndoForFK(List<OIBoneInfo> bones) {
            GuideCommand.EqualsInfo[] undoRotation = TransformUndoForFK(bones, MoveDelta);
            var rotateCom = new GuideCommand.RotationEqualsCommand(undoRotation);
            UndoRedoManager.Instance.Push(rotateCom);
            resetDelta();
        }

        public void createUndoForMove(List<ObjectCtrlInfo> selectedObjs) {
            GuideCommand.AddInfo[] moves = moveObjectService.TransformAllSelected(selectedObjs, MoveDelta);
            var moveCom = new GuideCommand.MoveAddCommand(moves);
            UndoRedoManager.Instance.Push(moveCom);
            resetDelta();
        }
        
        /*public void createUndoForIK() {
            GuideCommand.AddInfo[] moves = moveObjectService.TransformAllGuided(new List<GuideObject>(Singleton<GuideObjectManager>.Instance.selectObjects), MoveDelta);
            var moveCom = new GuideCommand.MoveAddCommand(moves);
            UndoRedoManager.Instance.Push(moveCom);
            resetDelta();
        }*/

        public void createUndoForRelativeRotation(List<ObjectCtrlInfo> selectedObjs) {
            MoveAndRotateAddCommand moveAddCom = moveObjectService.moveAndRotateAllSelected(selectedObjs, -RotationDelta, true);
            GuideCommand.EqualsInfo[] undoRotation = CreateUndoRotateForAllSelected(selectedObjs, false);
            var rotateCom = new GuideCommand.RotationEqualsCommand(undoRotation);
            UndoRedoManager.Instance.Push(new MoveAndRotateEqualsCommand(rotateCom, moveAddCom.moveCom));
            resetDelta();
        }

        public void createUndoForRotation(List<ObjectCtrlInfo> selectedObjs) {
            GuideCommand.EqualsInfo[] undoRotation = CreateUndoRotateForAllSelected(selectedObjs, false);
            var rotateCom = new GuideCommand.RotationEqualsCommand(undoRotation);
            UndoRedoManager.Instance.Push(rotateCom);
            resetDelta();
        }

        public void createUndoForResize(List<ObjectCtrlInfo> selectedObjs) {
            GuideCommand.EqualsInfo[] undoResize = CreateUndoRotateForAllSelected(selectedObjs, true);
            var rotateCom = new GuideCommand.ScaleEqualsCommand(undoResize);
            UndoRedoManager.Instance.Push(rotateCom);
            resetDelta();
        }

        private void resetDelta() {
            MoveDelta = Vector3.zero;
            RotationDelta = Vector3.zero;
        }


        internal void setMoveObjectService(MoveObjectService moveObjectService) {
            this.moveObjectService = moveObjectService;
        }

        public GuideCommand.EqualsInfo[] TransformUndoForFK(List<OIBoneInfo> bones, Vector3 moveDelta) {
            GuideCommand.EqualsInfo[] rotations = new GuideCommand.EqualsInfo[bones.Count];
            int index = 0;
            foreach (var bone in bones) {
                GuideCommand.EqualsInfo eqRot = new GuideCommand.EqualsInfo();
                eqRot.dicKey = bone.dicKey;
                eqRot.newValue = bone.changeAmount.rot;
                if (OldFkRotations.TryGetValue(bone.dicKey, out Vector3 oldValue)) {
                    eqRot.oldValue = oldValue;
                } else {
                    Debug.Log("MoveController: missing FK undo information");
                    eqRot.oldValue = Vector3.zero;
                }

                rotations[index++] = eqRot;
            }

            return rotations;
        }

        public void StoreOldFkRotation(List<OIBoneInfo> bones) {
            OldFkRotations.Clear();
            foreach (OIBoneInfo bone in bones) {
                int dicKey = bone.dicKey;
                ChangeAmount changeAmount = Studio.Studio.GetChangeAmount(dicKey);
                if (changeAmount != null) {
                    OldFkRotations.Add(dicKey, changeAmount.rot);
                }
            }
        }


        public void StoreOldRotationOrSize(List<ObjectCtrlInfo> selectedObjs, bool isResize) {
            OldRotations.Clear();
            foreach (ObjectCtrlInfo selected in selectedObjs) {
                int dicKey = selected.objectInfo.dicKey;
                ChangeAmount changeAmount = Studio.Studio.GetChangeAmount(dicKey);
                if (changeAmount != null) {
                    if (isResize) {
                        OldRotations.Add(dicKey, changeAmount.scale);
                    } else {
                        OldRotations.Add(dicKey, changeAmount.rot);
                    }
                }
            }
        }
    }
}