using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MoveController 
{
    public static class ButtonActionManager 
    {
        public static Action<BaseEventData> ResetFk() 
        {
            return data =>
            {
                if (MoveCtrlPlugin.window.AllSelected.Count > 0)
                    MoveObjectService.resetFKRotation(FkManagerService.getActiveBones());
            };
        }

        public static Action<BaseEventData> Move2Camera() 
        {
            return data => 
            {
                MoveObjectService.MoveObjectsToCamera(MoveCtrlPlugin.window.AllSelected, ((PointerEventData) data).button == PointerEventData.InputButton.Right);
            };
        }

        public static DragButtonAction Animation() 
        {
            var dba = new DragButtonAction
            {
                StartDrag = data => { },
                Drag = data =>
                {
                    MoveObjectService.controlAnimation(MoveCtrlPlugin.window.AllSelected,
                        MoveCtrlWindow.getMouseInput());
                },
                EndDrag = data => { }
            };
            //TODO: undo?
            return dba;
        }

        public static DragButtonAction MoveXZ(Vector3 inputMask) 
        {
            var dba = new DragButtonAction
            {
                StartDrag = data =>
                {
                    if (AccessoryCtrlService.IsAccessoryControl())
                    {
                        AccessoryCtrlService.InitUndoMove();
                    }
                    else if (IsRightButton(data))
                    {
                        UndoRedoService.StoreOldSizes(MoveCtrlPlugin.window.AllSelected);
                    }
                    else if (MoveObjectService.CheckIfIkSelected())
                    {
                        UndoRedoService.StoreOldIkPosition();
                    }
                    else
                    {
                        UndoRedoService.StoreOldPositions(MoveCtrlPlugin.window.AllSelected);
                    }
                },
                Drag = data =>
                {
                    if (IsRightButton(data))
                    {
                        var input = MoveCtrlWindow.getMouseInput();
                        var input3d = new Vector3(input.x, input.x, input.x);
                        MoveObjectService.resizeObj(MoveCtrlPlugin.window.AllSelected, input3d);
                    }
                    else
                    {
                        var input = MoveCtrlWindow.getMouseInput();
                        var input3d = Vector3.Scale(new Vector3(input.x, input.y, input.y), inputMask);
                        var mappedInput = MoveCtrlWindow.getCameraQuaternion() * input3d;
                        if (AccessoryCtrlService.IsAccessoryControl()) 
                        { 
                            AccessoryCtrlService.MoveAccessory(mappedInput); 
                        }
                        else if (MoveObjectService.IkSelected)
                        {
                            MoveObjectService.MoveIk(mappedInput);
                        }
                        else
                        {
                            MoveObjectService.MoveObj(MoveCtrlPlugin.window.AllSelected, mappedInput);
                        }
                    }
                },
                EndDrag = data =>
                {
                    if (AccessoryCtrlService.IsAccessoryControl()) 
                    { 
                        AccessoryCtrlService.CreateUndoMove(); 
                    }
                    else if (IsRightButton(data))
                    {
                        UndoRedoService.CreateUndoForResize(MoveCtrlPlugin.window.AllSelected);
                    }
                    else if (MoveObjectService.IkSelected)
                    {
                        UndoRedoService.CreateUndoForIkMove();
                        MoveObjectService.IkSelected = false;
                    }
                    else
                    {
                        UndoRedoService.CreateUndoForMove(MoveCtrlPlugin.window.AllSelected);
                    }
                }
            };

            return dba;
        }

        public static DragButtonAction MoveY(Vector3 inputMask) 
        {
            var dba = new DragButtonAction
            {
                StartDrag = data =>
                {
                    if (AccessoryCtrlService.IsAccessoryControl()) 
                    { 
                        AccessoryCtrlService.InitUndoMove(); 
                    }
                    else if (MoveObjectService.CheckIfIkSelected())
                    {
                        UndoRedoService.StoreOldIkPosition();
                    }
                    else
                    {
                        UndoRedoService.StoreOldPositions(MoveCtrlPlugin.window.AllSelected);
                    }
                },
                Drag = data =>
                {
                    var input = MoveCtrlWindow.getMouseInput();
                    var input3d = Vector3.Scale(new Vector3(input.x, input.y, input.y), inputMask);
                    if (AccessoryCtrlService.IsAccessoryControl()) 
                    { 
                        AccessoryCtrlService.MoveAccessory(input3d); 
                    }
                    else if (MoveObjectService.IkSelected)
                    {
                        MoveObjectService.MoveIk(input3d);
                    }
                    else
                    {
                        MoveObjectService.MoveObj(MoveCtrlPlugin.window.AllSelected, input3d);
                    }
                },
                EndDrag = data =>
                {
                    if (AccessoryCtrlService.IsAccessoryControl()) 
                    { 
                        AccessoryCtrlService.CreateUndoMove(); 
                    }
                    else if (MoveObjectService.IkSelected)
                    {
                        UndoRedoService.CreateUndoForIkMove();
                        MoveObjectService.IkSelected = false;
                    }
                    else
                    {
                        UndoRedoService.CreateUndoForMove(MoveCtrlPlugin.window.AllSelected);
                    }
                }
            };
            
            return dba;
        }

        public static DragButtonAction RotateX() 
        {
            var dba = new DragButtonAction
            {
                StartDrag = data =>
                { 
                    if (AccessoryCtrlService.IsAccessoryControl()) 
                    { 
                        AccessoryCtrlService.InitUndoRotate();
                    }
                    else
                    {
                        UndoRedoService.StoreOldRotation(MoveCtrlPlugin.window.AllSelected);
                    }
                },
                Drag = data =>
                {
                    var tc = MoveCtrlPlugin.camera.transform;
                    var right = tc.right;
                    var input = MoveCtrlWindow.getMouseInput();
                    if (AccessoryCtrlService.IsAccessoryControl()) 
                    { 
                        AccessoryCtrlService.RotateAccessoryByCamera(right, -input.x, true);
                    }
                    else 
                    { 
                        MoveObjectService.RotateByCamera(MoveCtrlPlugin.window.AllSelected, right, -input, true);
                    }
                },
                EndDrag = data =>
                {
                    if (AccessoryCtrlService.IsAccessoryControl()) 
                    { 
                        AccessoryCtrlService.CreateUndoRotate();
                    }
                    else
                    {
                        UndoRedoService.CreateUndoForRotation(MoveCtrlPlugin.window.AllSelected);
                    }
                },
                Click = data =>
                {
                    var tc = MoveCtrlPlugin.camera.transform;
                    var right = tc.right;
                    var rightTurn = IsRightButton(data);
                    var input = new Vector3(rightTurn ? -90 : 90, 0, 0);
                    MoveObjectService.RotateByCamera(MoveCtrlPlugin.window.AllSelected, right, input,
                        false);
                    UndoRedoService.CreateUndoForRotation(MoveCtrlPlugin.window.AllSelected);
                }
            };

            return dba;
        }

        public static DragButtonAction RotateY() 
        {
            var dba = new DragButtonAction
            {
                StartDrag = data =>
                {
                    if (AccessoryCtrlService.IsAccessoryControl()) 
                    { 
                        AccessoryCtrlService.InitUndoRotate();
                    }
                    else
                    {
                        UndoRedoService.StoreOldRotation(MoveCtrlPlugin.window.AllSelected);
                    }
                },
                Drag = data =>
                {
                    var input = MoveCtrlWindow.getMouseInput();
                    var relativeRotation = IsRightButton(data);

                    var input3d = new Vector3(0, -input.x, 0);

                    if (AccessoryCtrlService.IsAccessoryControl()) 
                    { 
                        AccessoryCtrlService.RotateAccessoryInWorld(input3d, true);
                    }
                    else if (relativeRotation)
                    {
                        MoveObjectService.RotateRelative(MoveCtrlPlugin.window.AllSelected, input3d);
                    }
                    else
                    {
                        MoveObjectService.RotateObj(MoveCtrlPlugin.window.AllSelected, input3d, true);
                    }
                },
                EndDrag = data =>
                {
                    var relativeRotation = IsRightButton(data);
                    if (AccessoryCtrlService.IsAccessoryControl()) 
                    { 
                        AccessoryCtrlService.CreateUndoRotate();
                    }
                    else if (relativeRotation)
                    {
                        UndoRedoService.CreateUndoForRelativeRotation(MoveCtrlPlugin.window.AllSelected);
                    }
                    else
                    {
                        UndoRedoService.CreateUndoForRotation(MoveCtrlPlugin.window.AllSelected);
                    }
                },
                Click = data =>
                {
                    var rightTurn = IsRightButton(data);
                    var rot3d = new Vector3(0, rightTurn ? -90 : 90, 0);
                    MoveObjectService.RotateObj(MoveCtrlPlugin.window.AllSelected, rot3d, false);
                    UndoRedoService.CreateUndoForRotation(MoveCtrlPlugin.window.AllSelected);
                }
            };
            return dba;
        }

        public static DragButtonAction RotateZ() 
        {
            var dba = new DragButtonAction
            {
                StartDrag = data =>
                {
                    if (AccessoryCtrlService.IsAccessoryControl()) 
                    { 
                        AccessoryCtrlService.InitUndoRotate();
                    }
                    else
                    {
                        UndoRedoService.StoreOldRotation(MoveCtrlPlugin.window.AllSelected);
                    }
                },
                Drag = data =>
                {
                    var tc = MoveCtrlPlugin.camera.transform;
                    var forward = tc.forward;
                    var input = MoveCtrlWindow.getMouseInput();
                    if (AccessoryCtrlService.IsAccessoryControl()) 
                    { 
                        AccessoryCtrlService.RotateAccessoryByCamera(Vector3.forward, -input.x, true);
                    }
                    else
                    {
                        MoveObjectService.RotateByCamera(MoveCtrlPlugin.window.AllSelected, forward, -input,
                        true);
                    }
                },
                EndDrag = data =>
                {
                    if (AccessoryCtrlService.IsAccessoryControl()) 
                    { 
                        AccessoryCtrlService.CreateUndoRotate();
                    }
                    else
                    {
                        UndoRedoService.CreateUndoForRotation(MoveCtrlPlugin.window.AllSelected);
                    }
                },
                Click = data =>
                {
                    var tc = MoveCtrlPlugin.camera.transform;
                    var forward = tc.forward;
                    var rightTurn = IsRightButton(data);
                    var input = new Vector3(rightTurn ? -90 : 90, 0, 0);
                    MoveObjectService.RotateByCamera(MoveCtrlPlugin.window.AllSelected, forward, input,
                        false);
                    UndoRedoService.CreateUndoForRotation(MoveCtrlPlugin.window.AllSelected);
                }
            };
            return dba;
        }

        public static DragButtonAction RotateFk(Vector3 inputMask) 
        {
            var dba = new DragButtonAction
            {
                StartDrag = data =>
                {
                    if (AccessoryCtrlService.IsAccessoryControl()) 
                    { 
                        AccessoryCtrlService.InitUndoRotate();
                    }
                    else if (FkManagerService.ActiveBone != null)
                    {
                        UndoRedoService.StoreOldFkRotation(
                            FkManagerService.getActiveBones());
                    }
                    else if (MoveObjectService.CheckIfIkRotSelected())
                    {
                        UndoRedoService.StoreOldIkRotation();
                    }
                    else
                    {
                        UndoRedoService.StoreOldRotation(MoveCtrlPlugin.window.AllSelected);
                    }
                },
                Drag = data =>
                {
                    var input = MoveCtrlWindow.getMouseInput();
                    var input3d = Vector3.Scale(new Vector3(input.x, input.x, input.x), inputMask);
                    if (AccessoryCtrlService.IsAccessoryControl()) 
                    { 
                        AccessoryCtrlService.RotateAccessory(input3d, true);
                    }
                    else if (FkManagerService.ActiveBone != null)
                    {
                        MoveObjectService.rotateFk(FkManagerService.getActiveBones(),
                            input3d, true);
                    }
                    else if (MoveObjectService.CheckIfIkRotSelected())
                    {
                        MoveObjectService.RotateIk(input3d);
                    }
                    else
                    {
                        MoveObjectService.RotateObjAsGuided(MoveCtrlPlugin.window.AllSelected, input3d,
                            true);
                    }
                },
                EndDrag = data =>
                {
                    if (AccessoryCtrlService.IsAccessoryControl()) 
                    { 
                        AccessoryCtrlService.CreateUndoRotate();
                    }
                    else if (FkManagerService.ActiveBone != null)
                    {
                        UndoRedoService.CreateUndoForFk(FkManagerService
                            .getActiveBones());
                    }
                    else if (MoveObjectService.CheckIfIkRotSelected())
                    {
                        UndoRedoService.CreateUndoForIkRotation();
                    }
                    else
                    {
                        UndoRedoService.CreateUndoForRotation(MoveCtrlPlugin.window.AllSelected);
                    }
                },
                Click = data =>
                {
                    var rightTurn = IsRightButton(data);
                    var input = new Vector3(rightTurn ? 90 : -90, 0, 0);
                    var input3d = Vector3.Scale(new Vector3(input.x, input.x, input.x), inputMask);
                    
                    if (AccessoryCtrlService.IsAccessoryControl()) 
                    {
                        AccessoryCtrlService.RotateAccessory(input3d, false);
                        AccessoryCtrlService.CreateUndoRotate();
                    } 
                    else if (FkManagerService.ActiveBone != null)
                    {
                        MoveObjectService.rotateFk(FkManagerService.getActiveBones(),
                            input3d, false);
                        UndoRedoService.CreateUndoForFk(FkManagerService
                            .getActiveBones());
                    }
                    else
                    {
                        MoveObjectService.RotateObjAsGuided(MoveCtrlPlugin.window.AllSelected, input3d,
                            false);
                        UndoRedoService.CreateUndoForRotation(MoveCtrlPlugin.window.AllSelected);
                    }
                }
            };



            return dba;
        }

        public static Action<float> UpdateSpeedFactors() 
        {
            return MoveObjectService.updateSpeedFactors;
        }

        public static Action<float> UpdateFkScale() 
        {
            return FkManagerService.updateFkScale;
        }

        private static bool IsRightButton(BaseEventData data) 
        {
            return ((PointerEventData) data).button == PointerEventData.InputButton.Right;
        }
    }

    public class DragButtonAction 
    {
        public Action<BaseEventData> Click { get; set; }
        public Action<BaseEventData> StartDrag { get; set; }
        public Action<BaseEventData> Drag { get; set; }
        public Action<BaseEventData> EndDrag { get; set; }

        public DragButtonAction() 
        {
            StartDrag = a => { };
            EndDrag = a => { };
            Click = a => { };
        }
    }
}