using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MoveController {
    public class ButtonActionManager {
        private readonly MoveObjectService moveObjectService;
        private readonly FkManagerService fkManagerService;
        private readonly MoveCtrlWindow _window;
        private UndoRedoService undoRedoService;

        internal ButtonActionManager(MoveObjectService moveObjectService, FkManagerService fkManagerService, MoveCtrlWindow window, UndoRedoService undoRedoService) {
            this.moveObjectService = moveObjectService;
            this.fkManagerService = fkManagerService;
            _window = window;
            this.undoRedoService = undoRedoService;
        }

        public Action<BaseEventData> ResetFk() {
            return data => { moveObjectService.resetFKRotation(fkManagerService.getActiveBones()); };
        }

        public Action<BaseEventData> Move2Camera() {
            return data => { moveObjectService.MoveObjectsToCamera(_window.AllSelected, ((PointerEventData) data).button == PointerEventData.InputButton.Right); };
        }

        public DragButtonAction Animation() {
            var dba = new DragButtonAction();
            dba.StartDrag = data => { };
            dba.Drag = data => { moveObjectService.controlAnimation(_window.AllSelected, _window.getMouseInput()); };
            dba.EndDrag = data => { };
            //TODO: undo?
            return dba;
        }

        public DragButtonAction MoveXZ(Vector3 inputMask) {
            var dba = new DragButtonAction();

            dba.StartDrag = data => {
                //if (isRightButton(data)) {
                    undoRedoService.StoreOldRotationOrSize(_window.AllSelected, true);
                /*} else {
                    moveObjectService.CheckIfIkSelected();
                }*/
            };

            dba.Drag = data => {
                if (isRightButton(data)) {
                    var input = _window.getMouseInput();
                    var input3d = new Vector3(input.x, input.x, input.x);
                    moveObjectService.resizeObj(_window.AllSelected, input3d);
                } else {
                    var input = _window.getMouseInput();
                    var input3d = Vector3.Scale(new Vector3(input.x, input.y, input.y), inputMask);
                    var mappedInput = _window.getCameraQuaternion() * input3d;
                    /*if (moveObjectService.IkSelected) {

                        var parent = _window.AllSelected[0].objectInfo;
                        var localAmount = mappedInput;
                        if (parent != null) {
                            localAmount = Quaternion.Euler(parent.changeAmount.rot) * mappedInput;
                           //localAmount = Studio.GuideObjectManager.Instance.
                        }
                        
                        moveObjectService.MoveIk2(mappedInput);
                    } else {*/
                        moveObjectService.MoveObj(_window.AllSelected, mappedInput);
                   // }
                }
            };
            dba.EndDrag = data => {
                /*if (isRightButton(data)) {
                    undoRedoService.createUndoForResize(_window.AllSelected);
                } else if (moveObjectService.IkSelected) {
                    undoRedoService.createUndoForIK();
                    moveObjectService.IkSelected = false;
                } else {*/
                    undoRedoService.createUndoForMove(_window.AllSelected);
              //  }
            };

            return dba;
        }

        public DragButtonAction MoveY(Vector3 inputMask) {
            var dba = new DragButtonAction();

      //      dba.StartDrag = data => { moveObjectService.CheckIfIkSelected(); };

            dba.Drag = data => {
                var input = _window.getMouseInput();
                var input3d = Vector3.Scale(new Vector3(input.x, input.y, input.y), inputMask);
                /*if (moveObjectService.IkSelected) {
                    moveObjectService.MoveIk(input3d);
                } else {*/
                    moveObjectService.MoveObj(_window.AllSelected, input3d);
             //  }
            };
            dba.EndDrag = data => {
                /*if (moveObjectService.IkSelected) {
                    undoRedoService.createUndoForIK();
                    moveObjectService.IkSelected = false;
                } else {*/
                    undoRedoService.createUndoForMove(_window.AllSelected);
             //   }
            };

            return dba;
        }

        public DragButtonAction RotateX() {
            var dba = new DragButtonAction();
            dba.StartDrag = data => { undoRedoService.StoreOldRotationOrSize(_window.AllSelected, false); };
            dba.Drag = data => {
                Transform tc = Camera.main.transform;
                var right = tc.right;
                var input = _window.getMouseInput();
                moveObjectService.RotateByCamera(_window.AllSelected, right, -input, true);
            };
            dba.EndDrag = data => { undoRedoService.createUndoForRotation(_window.AllSelected); };

            dba.Click = data => {
                Transform tc = Camera.main.transform;
                var right = tc.right;
                var rightTurn = isRightButton(data);
                var input = new Vector3(rightTurn ? -90 : 90, 0, 0);
                moveObjectService.RotateByCamera(_window.AllSelected, right, input, false);
                undoRedoService.createUndoForRotation(_window.AllSelected);
            };
            return dba;
        }

        public DragButtonAction RotateY() {
            var dba = new DragButtonAction();
            dba.StartDrag = data => { undoRedoService.StoreOldRotationOrSize(_window.AllSelected, false); };
            dba.Drag = data => {
                var input = _window.getMouseInput();
                var relativeRotation = isRightButton(data);

                var input3d = new Vector3(0, -input.x, 0);

                if (relativeRotation) {
                    moveObjectService.RotateRelative(_window.AllSelected, input3d);
                } else {
                    moveObjectService.RotateObj(_window.AllSelected, input3d, true);
                }
            };
            dba.EndDrag = data => {
                var relativeRotation = isRightButton(data);
                if (relativeRotation) {
                    undoRedoService.createUndoForRelativeRotation(_window.AllSelected);
                } else {
                    undoRedoService.createUndoForRotation(_window.AllSelected);
                }
            };
            dba.Click = data => {
                var rightTurn = isRightButton(data);
                var rot3d = new Vector3(0, rightTurn ? -90 : 90, 0);
                moveObjectService.RotateObj(_window.AllSelected, rot3d, false);
                undoRedoService.createUndoForRotation(_window.AllSelected);
            };
            return dba;
        }

        public DragButtonAction RotateZ() {
            var dba = new DragButtonAction();
            dba.StartDrag = data => { undoRedoService.StoreOldRotationOrSize(_window.AllSelected, false); };
            dba.Drag = data => {
                Transform tc = Camera.main.transform;
                var forward = tc.forward;
                var input = _window.getMouseInput();
                moveObjectService.RotateByCamera(_window.AllSelected, forward, -input, true);
            };
            dba.EndDrag = data => { undoRedoService.createUndoForRotation(_window.AllSelected); };
            dba.Click = data => {
                Transform tc = Camera.main.transform;
                var forward = tc.forward;
                var rightTurn = isRightButton(data);
                var input = new Vector3(rightTurn ? -90 : 90, 0, 0);
                moveObjectService.RotateByCamera(_window.AllSelected, forward, input, false);
                undoRedoService.createUndoForRotation(_window.AllSelected);
            };
            return dba;
        }

        public DragButtonAction RotateFk(Vector3 inputMask) {
            var dba = new DragButtonAction();

            dba.StartDrag = data => {
                if (fkManagerService.ActiveBone != null) {
                    undoRedoService.StoreOldFkRotation(fkManagerService.getActiveBones());
                } else {
                    undoRedoService.StoreOldRotationOrSize(_window.AllSelected, false);
                }
            };
            dba.Drag = data => {
                var input = _window.getMouseInput();
                var input3d = Vector3.Scale(new Vector3(input.x, input.x, input.x), inputMask);
                if (fkManagerService.ActiveBone != null) {
                    moveObjectService.rotateFk(fkManagerService.getActiveBones(), input3d, true);
                } else {
                    moveObjectService.RotateObjAsGuided(_window.AllSelected, input3d, true);
                }
            };
            dba.EndDrag = data => {
                if (fkManagerService.ActiveBone != null) {
                    undoRedoService.createUndoForFK(fkManagerService.getActiveBones());
                } else {
                    undoRedoService.createUndoForRotation(_window.AllSelected);
                }
            };

            dba.Click = data => {
                var rightTurn = isRightButton(data);
                var input = new Vector3(rightTurn ? 90 : -90, 0, 0);
                var input3d = Vector3.Scale(new Vector3(input.x, input.x, input.x), inputMask);
                if (fkManagerService.ActiveBone != null) {
                    moveObjectService.rotateFk(fkManagerService.getActiveBones(), input3d, false);
                    undoRedoService.createUndoForFK(fkManagerService.getActiveBones());
                } else {
                    moveObjectService.RotateObjAsGuided(_window.AllSelected, input3d, false);
                    undoRedoService.createUndoForRotation(_window.AllSelected);
                }
            };

            return dba;
        }

        public Action<float> updateSpeedFactors() {
            return x => { moveObjectService.updateSpeedFactors(x); };
        }

        public Action<float> updateFkScale() {
            return x => { fkManagerService.updateFkScale(x); };
        }

        private static bool isRightButton(BaseEventData data) {
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