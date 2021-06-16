using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MoveController {
    public class ButtonActionManager {
        private readonly MoveObjectService moveObjectService;
        private readonly FkManagerService fkManagerService;
        private readonly MoveCtrlWindow window;
        private readonly UndoRedoService undoRedoService;

        internal ButtonActionManager(MoveObjectService moveObjectService, FkManagerService fkManagerService, MoveCtrlWindow window,
            UndoRedoService undoRedoService) {
            this.moveObjectService = moveObjectService;
            this.fkManagerService = fkManagerService;
            this.window = window;
            this.undoRedoService = undoRedoService;
        }

        public Action<BaseEventData> ResetFk() {
            return data => { moveObjectService.resetFKRotation(fkManagerService.getActiveBones()); };
        }

        public Action<BaseEventData> Move2Camera() {
            return data => {
                moveObjectService.MoveObjectsToCamera(window.AllSelected, ((PointerEventData) data).button == PointerEventData.InputButton.Right);
            };
        }

        public DragButtonAction Animation() {
            var dba = new DragButtonAction();
            dba.StartDrag = data => { };
            dba.Drag = data => { moveObjectService.controlAnimation(window.AllSelected, window.getMouseInput()); };
            dba.EndDrag = data => { };
            //TODO: undo?
            return dba;
        }

        public DragButtonAction MoveXZ(Vector3 inputMask) {
            var dba = new DragButtonAction();

            dba.StartDrag = data => {
                if (IsRightButton(data)) {
                    undoRedoService.StoreOldSizes(window.AllSelected);
                } else if (moveObjectService.CheckIfIkSelected()) {
                    undoRedoService.StoreOldIkPosition();
                } else {
                    undoRedoService.StoreOldPositions(window.AllSelected);
                }
            };

            dba.Drag = data => {
                if (IsRightButton(data)) {
                    var input = window.getMouseInput();
                    var input3d = new Vector3(input.x, input.x, input.x);
                    moveObjectService.resizeObj(window.AllSelected, input3d);
                } else {
                    var input = window.getMouseInput();
                    var input3d = Vector3.Scale(new Vector3(input.x, input.y, input.y), inputMask);
                    var mappedInput = window.getCameraQuaternion() * input3d;
                    if (moveObjectService.IkSelected) {
                        moveObjectService.MoveIk(mappedInput);
                    } else {
                        moveObjectService.MoveObj(window.AllSelected, mappedInput);
                    }
                }
            };
            dba.EndDrag = data => {
                if (IsRightButton(data)) {
                    undoRedoService.CreateUndoForResize(window.AllSelected);
                } else if (moveObjectService.IkSelected) {
                    undoRedoService.CreateUndoForIkMove();
                    moveObjectService.IkSelected = false;
                } else {
                    undoRedoService.CreateUndoForMove(window.AllSelected);
                }
            };

            return dba;
        }

        public DragButtonAction MoveY(Vector3 inputMask) {
            var dba = new DragButtonAction();

            dba.StartDrag = data => {
                if (moveObjectService.CheckIfIkSelected()) {
                    undoRedoService.StoreOldIkPosition();
                } else {
                    undoRedoService.StoreOldPositions(window.AllSelected);
                }
            };

            dba.Drag = data => {
                var input = window.getMouseInput();
                var input3d = Vector3.Scale(new Vector3(input.x, input.y, input.y), inputMask);
                if (moveObjectService.IkSelected) {
                    moveObjectService.MoveIk(input3d);
                } else {
                    moveObjectService.MoveObj(window.AllSelected, input3d);
                }
            };
            dba.EndDrag = data => {
                if (moveObjectService.IkSelected) {
                    undoRedoService.CreateUndoForIkMove();
                    moveObjectService.IkSelected = false;
                } else {
                    undoRedoService.CreateUndoForMove(window.AllSelected);
                }
            };

            return dba;
        }

        public DragButtonAction RotateX() {
            var dba = new DragButtonAction();
            dba.StartDrag = data => { undoRedoService.StoreOldRotation(window.AllSelected); };
            dba.Drag = data => {
                Transform tc = Camera.main.transform;
                var right = tc.right;
                var input = window.getMouseInput();
                moveObjectService.RotateByCamera(window.AllSelected, right, -input, true);
            };
            dba.EndDrag = data => { undoRedoService.CreateUndoForRotation(window.AllSelected); };

            dba.Click = data => {
                Transform tc = Camera.main.transform;
                var right = tc.right;
                var rightTurn = IsRightButton(data);
                var input = new Vector3(rightTurn ? -90 : 90, 0, 0);
                moveObjectService.RotateByCamera(window.AllSelected, right, input, false);
                undoRedoService.CreateUndoForRotation(window.AllSelected);
            };
            return dba;
        }

        public DragButtonAction RotateY() {
            var dba = new DragButtonAction();
            dba.StartDrag = data => { undoRedoService.StoreOldRotation(window.AllSelected); };
            dba.Drag = data => {
                var input = window.getMouseInput();
                var relativeRotation = IsRightButton(data);

                var input3d = new Vector3(0, -input.x, 0);

                if (relativeRotation) {
                    moveObjectService.RotateRelative(window.AllSelected, input3d);
                } else {
                    moveObjectService.RotateObj(window.AllSelected, input3d, true);
                }
            };
            dba.EndDrag = data => {
                var relativeRotation = IsRightButton(data);
                if (relativeRotation) {
                    undoRedoService.CreateUndoForRelativeRotation(window.AllSelected);
                } else {
                    undoRedoService.CreateUndoForRotation(window.AllSelected);
                }
            };
            dba.Click = data => {
                var rightTurn = IsRightButton(data);
                var rot3d = new Vector3(0, rightTurn ? -90 : 90, 0);
                moveObjectService.RotateObj(window.AllSelected, rot3d, false);
                undoRedoService.CreateUndoForRotation(window.AllSelected);
            };
            return dba;
        }

        public DragButtonAction RotateZ() {
            var dba = new DragButtonAction();
            dba.StartDrag = data => { undoRedoService.StoreOldRotation(window.AllSelected); };
            dba.Drag = data => {
                Transform tc = Camera.main.transform;
                var forward = tc.forward;
                var input = window.getMouseInput();
                moveObjectService.RotateByCamera(window.AllSelected, forward, -input, true);
            };
            dba.EndDrag = data => { undoRedoService.CreateUndoForRotation(window.AllSelected); };
            dba.Click = data => {
                Transform tc = Camera.main.transform;
                var forward = tc.forward;
                var rightTurn = IsRightButton(data);
                var input = new Vector3(rightTurn ? -90 : 90, 0, 0);
                moveObjectService.RotateByCamera(window.AllSelected, forward, input, false);
                undoRedoService.CreateUndoForRotation(window.AllSelected);
            };
            return dba;
        }

        public DragButtonAction RotateFk(Vector3 inputMask) {
            var dba = new DragButtonAction();

            dba.StartDrag = data => {
                if (fkManagerService.ActiveBone != null) {
                    undoRedoService.StoreOldFkRotation(fkManagerService.getActiveBones());
                } else if (moveObjectService.CheckIfIkRotSelected()) {
                    undoRedoService.StoreOldIkRotation();
                } else {
                    undoRedoService.StoreOldRotation(window.AllSelected);
                }
            };
            dba.Drag = data => {
                var input = window.getMouseInput();
                var input3d = Vector3.Scale(new Vector3(input.x, input.x, input.x), inputMask);
                if (fkManagerService.ActiveBone != null) {
                    moveObjectService.rotateFk(fkManagerService.getActiveBones(), input3d, true);
                } else if(moveObjectService.CheckIfIkRotSelected()) {
                    moveObjectService.RotateIk(input3d);
                } else {
                    moveObjectService.RotateObjAsGuided(window.AllSelected, input3d, true);
                }
            };
            dba.EndDrag = data => {
                if (fkManagerService.ActiveBone != null) {
                    undoRedoService.CreateUndoForFk(fkManagerService.getActiveBones());
                } else if (moveObjectService.CheckIfIkRotSelected()) {
                    undoRedoService.CreateUndoForIkRotation();
                }else {
                    undoRedoService.CreateUndoForRotation(window.AllSelected);
                }
            };

            dba.Click = data => {
                var rightTurn = IsRightButton(data);
                var input = new Vector3(rightTurn ? 90 : -90, 0, 0);
                var input3d = Vector3.Scale(new Vector3(input.x, input.x, input.x), inputMask);
                if (fkManagerService.ActiveBone != null) {
                    moveObjectService.rotateFk(fkManagerService.getActiveBones(), input3d, false);
                    undoRedoService.CreateUndoForFk(fkManagerService.getActiveBones());
                } else {
                    moveObjectService.RotateObjAsGuided(window.AllSelected, input3d, false);
                    undoRedoService.CreateUndoForRotation(window.AllSelected);
                }
            };

            return dba;
        }

        public Action<float> UpdateSpeedFactors() {
            return x => { moveObjectService.updateSpeedFactors(x); };
        }

        public Action<float> UpdateFkScale() {
            return x => { fkManagerService.updateFkScale(x); };
        }

        private static bool IsRightButton(BaseEventData data) {
            return ((PointerEventData) data).button == PointerEventData.InputButton.Right;
        }
    }

    public class DragButtonAction {
        public Action<BaseEventData> Click { get; set; }
        public Action<BaseEventData> StartDrag { get; set; }
        public Action<BaseEventData> Drag { get; set; }
        public Action<BaseEventData> EndDrag { get; set; }

        public DragButtonAction() {
            StartDrag = a => { };
            EndDrag = a => { };
            Click = a => { };
        }
    }
}