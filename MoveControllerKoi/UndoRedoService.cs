using System.Collections.Generic;
using Studio;
using UnityEngine;

namespace MoveController 
{
    public static class UndoRedoService 
    {
        private static Vector3 MoveDelta = Vector3.zero;
        public static Vector3 RotationDelta = Vector3.zero;
        private static readonly Dictionary<int, Vector3> OldFkRotations = new Dictionary<int, Vector3>();

        private static readonly Dictionary<int, Vector3> OldRotations = new Dictionary<int, Vector3>();
        private static readonly Dictionary<int, Vector3> OldPositions = new Dictionary<int, Vector3>();
        private static readonly Dictionary<int, Vector3> OldSizes = new Dictionary<int, Vector3>();
        
        private static GuideCommand.EqualsInfo[] CreateUndoRotateForAllSelected(List<ObjectCtrlInfo> selectedObjs, bool isResize) 
        {
            GuideCommand.EqualsInfo[] rotations = new GuideCommand.EqualsInfo[selectedObjs.Count];
            int i = 0;

            foreach (ObjectCtrlInfo selected in selectedObjs) 
            {
                int dicKey = selected.objectInfo.dicKey;
                ChangeAmount changeAmount = Studio.Studio.GetChangeAmount(dicKey);
                if (changeAmount != null && OldRotations.TryGetValue(dicKey, out Vector3 oldValue)) 
                {
                    GuideCommand.EqualsInfo eqRot = new GuideCommand.EqualsInfo();
                    eqRot.dicKey = dicKey;
                    if (isResize) 
                    {
                        eqRot.newValue = changeAmount.scale;
                    }
                    else 
                    {
                        eqRot.newValue = changeAmount.rot;
                    }

                    eqRot.oldValue = oldValue;
                    rotations[i++] = eqRot;
                }
            }

            return rotations;
        }

        public static void CreateUndoForFk(List<OIBoneInfo> bones) 
        {
            GuideCommand.EqualsInfo[] undoRotation = TransformUndoForFk(bones, MoveDelta);
            var rotateCom = new GuideCommand.RotationEqualsCommand(undoRotation);
            UndoRedoManager.Instance.Push(rotateCom);
            ResetDelta();
        }

        public static void CreateUndoForMove(List<ObjectCtrlInfo> selectedObjs) 
        {
            GuideCommand.EqualsInfo[] moved = new GuideCommand.EqualsInfo[selectedObjs.Count];
            for (int i = 0; i < selectedObjs.Count; i++) 
            {
                var selected = selectedObjs[i];
                int dicKey = selected.objectInfo.dicKey;

                moved[i] = new GuideCommand.EqualsInfo() 
                {
                    dicKey = dicKey,
                    newValue = selected.guideObject.transformTarget.localPosition,
                    oldValue = OldPositions[dicKey]
                };

                var moveCom = new GuideCommand.MoveEqualsCommand(moved);
                UndoRedoManager.Instance.Push(moveCom);
            }

            OldPositions.Clear();
        }

        public static void CreateUndoForIkMove() 
        {
            var ikGuide = Singleton<GuideObjectManager>.Instance.selectObject;
            if (ikGuide == null) return;

            GuideCommand.EqualsInfo[] moved = new GuideCommand.EqualsInfo[1];

            int dicKey = ikGuide.dicKey;

            moved[0] = new GuideCommand.EqualsInfo() 
            {
                dicKey = ikGuide.dicKey,
                newValue = ikGuide.transformTarget.localPosition,
                oldValue = OldPositions[dicKey]
            };

            var moveCom = new GuideCommand.MoveEqualsCommand(moved);
            UndoRedoManager.Instance.Push(moveCom);
        }
        
        public static void CreateUndoForIkRotation() 
        {
            var ikGuide = Singleton<GuideObjectManager>.Instance.selectObject;
            if (ikGuide == null) return;

            GuideCommand.EqualsInfo[] rotated = new GuideCommand.EqualsInfo[1];

            int dicKey = ikGuide.dicKey;

            ChangeAmount changeAmount = Studio.Studio.GetChangeAmount(dicKey);

            rotated[0] = new GuideCommand.EqualsInfo() 
            {
                dicKey = ikGuide.dicKey,
                newValue = changeAmount.rot,
                oldValue = OldRotations[dicKey]
            };

            var rotCom = new GuideCommand.RotationEqualsCommand(rotated);
            UndoRedoManager.Instance.Push(rotCom);
        }

        public static void CreateUndoForRelativeRotation(List<ObjectCtrlInfo> selectedObjs) 
        {
            MoveAndRotateAddCommand moveAddCom = MoveObjectService.moveAndRotateAllSelected(selectedObjs, -RotationDelta, true);
            GuideCommand.EqualsInfo[] undoRotation = CreateUndoRotateForAllSelected(selectedObjs, false);
            var rotateCom = new GuideCommand.RotationEqualsCommand(undoRotation);
            UndoRedoManager.Instance.Push(new MoveAndRotateEqualsCommand(rotateCom, moveAddCom.moveCom));
            ResetDelta();
        }

        public static void CreateUndoForRotation(List<ObjectCtrlInfo> selectedObjs) 
        {
            GuideCommand.EqualsInfo[] undoRotation = CreateUndoRotateForAllSelected(selectedObjs, false);
            var rotateCom = new GuideCommand.RotationEqualsCommand(undoRotation);
            UndoRedoManager.Instance.Push(rotateCom);
            ResetDelta();
        }

        public static void CreateUndoForResize(List<ObjectCtrlInfo> selectedObjs) 
        {
            GuideCommand.EqualsInfo[] moved = new GuideCommand.EqualsInfo[selectedObjs.Count];
            for (int i = 0; i < selectedObjs.Count; i++) 
            {
                var selected = selectedObjs[i];
                int dicKey = selected.objectInfo.dicKey;

                moved[i] = new GuideCommand.EqualsInfo() 
                {
                    dicKey = dicKey,
                    newValue = selected.objectInfo.changeAmount.scale,
                    oldValue = OldSizes[dicKey]
                };

                var sizeCom = new GuideCommand.ScaleEqualsCommand(moved);
                UndoRedoManager.Instance.Push(sizeCom);
            }

            OldSizes.Clear();
        }

        private static void ResetDelta() 
        {
            MoveDelta = Vector3.zero;
            RotationDelta = Vector3.zero;
        }


        private static GuideCommand.EqualsInfo[] TransformUndoForFk(List<OIBoneInfo> bones, Vector3 moveDelta) 
        {
            GuideCommand.EqualsInfo[] rotations = new GuideCommand.EqualsInfo[bones.Count];
            int index = 0;
            foreach (var bone in bones) 
            {
                GuideCommand.EqualsInfo eqRot = new GuideCommand.EqualsInfo();
                eqRot.dicKey = bone.dicKey;
                eqRot.newValue = bone.changeAmount.rot;
                if (OldFkRotations.TryGetValue(bone.dicKey, out Vector3 oldValue)) 
                {
                    eqRot.oldValue = oldValue;
                } 
                else 
                {
                    Debug.Log("MoveController: missing FK undo information");
                    eqRot.oldValue = Vector3.zero;
                }

                rotations[index++] = eqRot;
            }

            return rotations;
        }

        public static void StoreOldFkRotation(List<OIBoneInfo> bones) 
        {
            OldFkRotations.Clear();
            foreach (OIBoneInfo bone in bones) 
            {
                int dicKey = bone.dicKey;
                ChangeAmount changeAmount = Studio.Studio.GetChangeAmount(dicKey);
                if (changeAmount != null) 
                {
                    OldFkRotations.Add(dicKey, changeAmount.rot);
                }
            }
        }


        public static void StoreOldRotation(List<ObjectCtrlInfo> selectedObjs) 
        {
            OldRotations.Clear();
            foreach (ObjectCtrlInfo selected in selectedObjs) 
            {
                int dicKey = selected.objectInfo.dicKey;
                ChangeAmount changeAmount = Studio.Studio.GetChangeAmount(dicKey);
                if (changeAmount != null) 
                {
                    OldRotations.Add(dicKey, changeAmount.rot);
                }
            }
        }

        public static void StoreOldSizes(List<ObjectCtrlInfo> selectedObjs) 
        {
            OldRotations.Clear();
            foreach (ObjectCtrlInfo selected in selectedObjs) 
            {
                int dicKey = selected.objectInfo.dicKey;
                ChangeAmount changeAmount = Studio.Studio.GetChangeAmount(dicKey);
                if (changeAmount != null) 
                {
                    OldSizes.Add(dicKey, changeAmount.scale);
                }
            }
        }

        public static void StoreOldPositions(List<ObjectCtrlInfo> selectedObjs) 
        {
            OldPositions.Clear();
            foreach (ObjectCtrlInfo selected in selectedObjs) 
            {
                int dicKey = selected.objectInfo.dicKey;
                ChangeAmount changeAmount = Studio.Studio.GetChangeAmount(dicKey);
                if (changeAmount != null) 
                {
                    OldPositions.Add(dicKey, changeAmount.pos);
                }
            }
        }

        public static void StoreOldIkPosition() 
        {
            OldPositions.Clear();
            var ikGuide = Singleton<GuideObjectManager>.Instance.selectObject;
            if (ikGuide == null) return;

            int dicKey = ikGuide.dicKey;
            ChangeAmount changeAmount = Studio.Studio.GetChangeAmount(dicKey);
            if (changeAmount != null) 
            {
                OldPositions.Add(dicKey, changeAmount.pos);
            }
        }
        
        public static void StoreOldIkRotation() 
        {
            OldRotations.Clear();
            var ikGuide = Singleton<GuideObjectManager>.Instance.selectObject;
            if (ikGuide == null) return;

            int dicKey = ikGuide.dicKey;
            ChangeAmount changeAmount = Studio.Studio.GetChangeAmount(dicKey);
            if (changeAmount != null) 
            {
                OldRotations.Add(dicKey, changeAmount.rot);
            }
        }
    }
}