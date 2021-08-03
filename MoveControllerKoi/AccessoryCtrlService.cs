using System;
using System.Collections.Generic;
using Studio;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace MoveController 
{
    public static class AccessoryCtrlService 
    {
        private static readonly Dictionary<string, Tuple<int, int>> parentNodeMap = new Dictionary<string, Tuple<int, int>>() 
        {
            {ChaAccessoryDefine.AccessoryParentKey.a_n_hair_pony.ToString(), new Tuple<int, int>(0, 0)},
            {ChaAccessoryDefine.AccessoryParentKey.a_n_hair_twin_L.ToString(), new Tuple<int, int>(0, 1)},
            {ChaAccessoryDefine.AccessoryParentKey.a_n_hair_twin_R.ToString(), new Tuple<int, int>(0, 2)},
            {ChaAccessoryDefine.AccessoryParentKey.a_n_hair_pin.ToString(), new Tuple<int, int>(0, 3)},
            {ChaAccessoryDefine.AccessoryParentKey.a_n_hair_pin_R.ToString(), new Tuple<int, int>(0, 4)},
            {ChaAccessoryDefine.AccessoryParentKey.a_n_headtop.ToString(), new Tuple<int, int>(1, 0)},
            {ChaAccessoryDefine.AccessoryParentKey.a_n_headflont.ToString(), new Tuple<int, int>(1, 1)},
            {ChaAccessoryDefine.AccessoryParentKey.a_n_head.ToString(), new Tuple<int, int>(1, 2)},
            {ChaAccessoryDefine.AccessoryParentKey.a_n_headside.ToString(), new Tuple<int, int>(1, 3)},
            {ChaAccessoryDefine.AccessoryParentKey.a_n_earrings_L.ToString(), new Tuple<int, int>(2, 0)},
            {ChaAccessoryDefine.AccessoryParentKey.a_n_earrings_R.ToString(), new Tuple<int, int>(2, 1)},
            {ChaAccessoryDefine.AccessoryParentKey.a_n_megane.ToString(), new Tuple<int, int>(2, 2)},
            {ChaAccessoryDefine.AccessoryParentKey.a_n_nose.ToString(), new Tuple<int, int>(2, 3)},
            {ChaAccessoryDefine.AccessoryParentKey.a_n_mouth.ToString(), new Tuple<int, int>(2, 4)},
            {ChaAccessoryDefine.AccessoryParentKey.a_n_neck.ToString(), new Tuple<int, int>(3, 0)},
            {ChaAccessoryDefine.AccessoryParentKey.a_n_bust_f.ToString(), new Tuple<int, int>(3, 1)},
            {ChaAccessoryDefine.AccessoryParentKey.a_n_bust.ToString(), new Tuple<int, int>(3, 2)},
            {ChaAccessoryDefine.AccessoryParentKey.a_n_nip_L.ToString(), new Tuple<int, int>(4, 0)},
            {ChaAccessoryDefine.AccessoryParentKey.a_n_nip_R.ToString(), new Tuple<int, int>(4, 1)},
            {ChaAccessoryDefine.AccessoryParentKey.a_n_back.ToString(), new Tuple<int, int>(4, 2)},
            {ChaAccessoryDefine.AccessoryParentKey.a_n_back_L.ToString(), new Tuple<int, int>(4, 3)},
            {ChaAccessoryDefine.AccessoryParentKey.a_n_back_R.ToString(), new Tuple<int, int>(4, 4)},
            {ChaAccessoryDefine.AccessoryParentKey.a_n_waist.ToString(), new Tuple<int, int>(5, 0)},
            {ChaAccessoryDefine.AccessoryParentKey.a_n_waist_f.ToString(), new Tuple<int, int>(5, 1)},
            {ChaAccessoryDefine.AccessoryParentKey.a_n_waist_b.ToString(), new Tuple<int, int>(5, 2)},
            {ChaAccessoryDefine.AccessoryParentKey.a_n_waist_L.ToString(), new Tuple<int, int>(5, 3)},
            {ChaAccessoryDefine.AccessoryParentKey.a_n_waist_R.ToString(), new Tuple<int, int>(5, 4)},
            {ChaAccessoryDefine.AccessoryParentKey.a_n_leg_L.ToString(), new Tuple<int, int>(6, 0)},
            {ChaAccessoryDefine.AccessoryParentKey.a_n_knee_L.ToString(), new Tuple<int, int>(6, 1)},
            {ChaAccessoryDefine.AccessoryParentKey.a_n_ankle_L.ToString(), new Tuple<int, int>(6, 2)},
            {ChaAccessoryDefine.AccessoryParentKey.a_n_heel_L.ToString(), new Tuple<int, int>(6, 3)},
            {ChaAccessoryDefine.AccessoryParentKey.a_n_leg_R.ToString(), new Tuple<int, int>(6, 4)},
            {ChaAccessoryDefine.AccessoryParentKey.a_n_knee_R.ToString(), new Tuple<int, int>(6, 5)},
            {ChaAccessoryDefine.AccessoryParentKey.a_n_ankle_R.ToString(), new Tuple<int, int>(6, 6)},
            {ChaAccessoryDefine.AccessoryParentKey.a_n_heel_R.ToString(), new Tuple<int, int>(6, 7)},
            {ChaAccessoryDefine.AccessoryParentKey.a_n_shoulder_L.ToString(), new Tuple<int, int>(7, 0)},
            {ChaAccessoryDefine.AccessoryParentKey.a_n_elbo_L.ToString(), new Tuple<int, int>(7, 1)},
            {ChaAccessoryDefine.AccessoryParentKey.a_n_arm_L.ToString(), new Tuple<int, int>(7, 2)},
            {ChaAccessoryDefine.AccessoryParentKey.a_n_wrist_L.ToString(), new Tuple<int, int>(7, 3)},
            {ChaAccessoryDefine.AccessoryParentKey.a_n_shoulder_R.ToString(), new Tuple<int, int>(7, 4)},
            {ChaAccessoryDefine.AccessoryParentKey.a_n_elbo_R.ToString(), new Tuple<int, int>(7, 5)},
            {ChaAccessoryDefine.AccessoryParentKey.a_n_arm_R.ToString(), new Tuple<int, int>(7, 6)},
            {ChaAccessoryDefine.AccessoryParentKey.a_n_wrist_R.ToString(), new Tuple<int, int>(7, 7)},
            {ChaAccessoryDefine.AccessoryParentKey.a_n_hand_L.ToString(), new Tuple<int, int>(8, 0)},
            {ChaAccessoryDefine.AccessoryParentKey.a_n_ind_L.ToString(), new Tuple<int, int>(8, 1)},
            {ChaAccessoryDefine.AccessoryParentKey.a_n_mid_L.ToString(), new Tuple<int, int>(8, 2)},
            {ChaAccessoryDefine.AccessoryParentKey.a_n_ring_L.ToString(), new Tuple<int, int>(8, 3)},
            {ChaAccessoryDefine.AccessoryParentKey.a_n_hand_R.ToString(), new Tuple<int, int>(8, 4)},
            {ChaAccessoryDefine.AccessoryParentKey.a_n_ind_R.ToString(), new Tuple<int, int>(8, 5)},
            {ChaAccessoryDefine.AccessoryParentKey.a_n_mid_R.ToString(), new Tuple<int, int>(8, 6)},
            {ChaAccessoryDefine.AccessoryParentKey.a_n_ring_R.ToString(), new Tuple<int, int>(8, 7)},
            {ChaAccessoryDefine.AccessoryParentKey.a_n_dan.ToString(), new Tuple<int, int>(9, 0)},
            {ChaAccessoryDefine.AccessoryParentKey.a_n_kokan.ToString(), new Tuple<int, int>(9, 1)},
            {ChaAccessoryDefine.AccessoryParentKey.a_n_ana.ToString(), new Tuple<int, int>(9, 2)}
        };

        private static readonly Color accColor = new Color(0f, 0.75f, 0.75f);

        public static readonly Dictionary<TreeNodeObject, AccMoveInfo> AccMoveInfos = new Dictionary<TreeNodeObject, AccMoveInfo>();
        public static AccMoveInfo Current = null;
        
        private static bool displayNodes;

        private static Quaternion initialRotation;
        private static Vector3 initialPosition = Vector3.zero;
        
        public static bool IsAccessoryControl() 
        {
            return Current != null && Current.Node!=null && Studio.Studio.Instance.treeNodeCtrl.selectNode == Current.Node;
        }

        public static void ToggleNodes(Button accCtrlButton) 
        {
            if (displayNodes == false) 
            {
                EnableAccNodes();
                accCtrlButton.image.color=Color.green;
                displayNodes = true;
            } 
            else 
            {
                DisableAccNodes();
                accCtrlButton.image.color=Color.white;
                displayNodes = false;
            }
        }

        public static void UpdateNodes() 
        {
            if (displayNodes) 
            {
                DisableAccNodes();
                EnableAccNodes();
            }
        }

        private static void DisableAccNodes() 
        {
            var treeNodeCtrl = Studio.Studio.Instance.treeNodeCtrl;
            var nodes = GameObject.Find("StudioScene").transform.Find("Canvas Object List/Image Bar").GetComponentsInChildren<TreeNodeObject>(true);

            foreach (var node in nodes) 
            {
                if (AccMoveInfos.ContainsKey(node)) 
                {
                    if (treeNodeCtrl.CheckSelect(node)) 
                    {
                        treeNodeCtrl.SelectSingle(node);
                    }

                    node.enableDelete = true;
                    node.enableChangeParent = true;
                    treeNodeCtrl.DeleteNode(node);
                }
            }
        }

        private static void EnableAccNodes() 
        {
            var nodes = GameObject.Find("StudioScene").transform.Find("Canvas Object List/Image Bar").GetComponentsInChildren<TreeNodeObject>(true);

            foreach (var node in nodes) 
            {
                if (Studio.Studio.Instance.dicInfo.TryGetValue(node, out var info)) 
                {
                    if (info is OCIChar chara) 
                    {
                        ProcessCharacterNode(chara, node);
                    }
                }
            }
        }

        private static void ProcessCharacterNode(OCIChar chara, TreeNodeObject charaNode) 
        {
            TreeNodeCtrl treeNodeCtrl = Studio.Studio.Instance.treeNodeCtrl;
            var charaInfo = chara.charInfo;
            var accessories = charaInfo.objAccessory;
            var listInfo = charaInfo.infoAccessory;

            for (int index = 0; index < accessories.Length; index++) 
            {
                if (accessories[index] == null) 
                {
                    continue;
                }

                var parentKey = charaInfo.nowCoordinate.accessory.parts[index].parentKey;

                if (!parentNodeMap.ContainsKey(parentKey)) 
                {
                    Debug.LogError("Could not find parentNode with key " + parentKey);
                    continue;
                }

                var item1 = parentNodeMap[parentKey].Item1;
                var item2 = parentNodeMap[parentKey].Item2;
                
                var parentNode = charaNode.child[item1].child[item2];

                var nodeName = listInfo[index].Name;

                var newNode = treeNodeCtrl.AddNode(nodeName);
                newNode.baseColor = accColor;
                newNode.colorSelect = accColor;

                SetParent(newNode, parentNode);

                newNode.enableChangeParent = false;
                newNode.enableCopy = false;
                newNode.enableDelete = false;
                newNode.enableAddChild = false;
                newNode.onDelete = () => AccMoveInfos.Remove(newNode);

                AccMoveInfos.Add(newNode, new AccMoveInfo(chara, index, newNode));
            }
        }

        private static GameObject GetAccessoryObject(GameObject slotObject) 
        {

            var tempObj = slotObject;
            //try {
                while (tempObj.transform.childCount > 0) 
                {
                    var nMove = tempObj.transform.Find("N_move");
                    if(nMove!=null) return nMove.gameObject;

                    tempObj = tempObj.transform.GetChild(0).gameObject;
                }

                throw new ArgumentException("Could not locate Accessory control object");
        }

        public static void MoveAccessory(Vector3 input) 
        {
            var accObject = GetAccessoryObject(Current.Chara.charInfo.objAccessory[Current.Index]);
            var amount = input * MoveObjectService.moveSpeedFactor;
            var charaInfo = Current.Chara.charInfo;

            MoveAcc(charaInfo, accObject, amount);
        }

        public static void RotateAccessoryInWorld(Vector3 rotAmount, bool useSpeedfactor) 
        {
            var slotObject = Current.Chara.charInfo.objAccessory[Current.Index];
            var accObject = GetAccessoryObject(slotObject);
            
            var rof = useSpeedfactor ? rotAmount * MoveObjectService.rotationSpeedFactor : rotAmount;

            accObject.transform.Rotate(rof,Space.World);
        }
        
        public static void RotateAccessoryByCamera(Vector3 angle, float rotAmount, bool useSpeedfactor) 
        {
            var slotObject = Current.Chara.charInfo.objAccessory[Current.Index];
            var accObject = GetAccessoryObject(slotObject);
            
            var rof = useSpeedfactor ? rotAmount * MoveObjectService.rotationSpeedFactor : rotAmount;

            accObject.transform.Rotate(angle,rof,Space.World);
        }

        private static void RotateAcc(ChaControl charaInfo, GameObject accObject, Quaternion newRotation) 
        {
            Quaternion startRotation = accObject.transform.localRotation;
            accObject.transform.localRotation = newRotation;

            var change = (newRotation.eulerAngles - startRotation.eulerAngles);
            charaInfo.nowCoordinate.accessory.parts[Current.Index].addMove[0, 1] += change;
        }

        public static void RotateAccessory(Vector3 rotAmount, bool useSpeedfactor) 
        {
            var slotObject = Current.Chara.charInfo.objAccessory[Current.Index];

            var accObject = GetAccessoryObject(slotObject);

            var charaInfo = Current.Chara.charInfo;
            var rof = useSpeedfactor ? rotAmount * MoveObjectService.rotationSpeedFactor : rotAmount;

            Quaternion startRotation = accObject.transform.localRotation;

            Quaternion newRotation = startRotation * Quaternion.Euler(rof);
            RotateAcc(charaInfo, accObject, newRotation);
        }

        public static void InitUndoRotate() 
        {
            var accObject = GetAccessoryObject(Current.Chara.charInfo.objAccessory[Current.Index]);
            initialRotation = accObject.transform.localRotation;
        }

        public static void CreateUndoRotate() 
        {
            var accObject = GetAccessoryObject(Current.Chara.charInfo.objAccessory[Current.Index]);
            var current = accObject.transform.localRotation;
            var start = initialRotation;

            var command = new AccessoryCommand(() => RotateAcc(Current.Chara.charInfo, accObject, start),
                () => RotateAcc(Current.Chara.charInfo, accObject, current));

            UndoRedoManager.Instance.Push(command);
        }

        public static void InitUndoMove() 
        {
            var accObject = GetAccessoryObject(Current.Chara.charInfo.objAccessory[Current.Index]);
            initialPosition = accObject.transform.position;
        }

        public static void CreateUndoMove() 
        {
            var accObject = GetAccessoryObject(Current.Chara.charInfo.objAccessory[Current.Index]);
            var current = accObject.transform.position;
            var change = current - initialPosition;

            var command = new AccessoryCommand(() => MoveAcc(Current.Chara.charInfo, accObject, -change),
                () => MoveAcc(Current.Chara.charInfo, accObject, change));

            UndoRedoManager.Instance.Push(command);
        }


        private static void MoveAcc(ChaControl charaInfo, GameObject accObject, Vector3 amount) 
        {
            var orgPos = accObject.transform.localPosition;
            accObject.transform.Translate(amount, Space.World);

            var newPos = accObject.transform.localPosition;
            var change = (newPos - orgPos) * 10f;

            charaInfo.nowCoordinate.accessory.parts[Current.Index].addMove[0, 0] += change;
        }

        private static void SetParent(TreeNodeObject node, TreeNodeObject parent) 
        {
            TreeNodeCtrl treeNodeCtrl = Studio.Studio.Instance.treeNodeCtrl;
            if (node == null) 
            {
                return;
            }

            if (!node.enableChangeParent) 
            {
                return;
            }

            if (treeNodeCtrl.CheckNode(node) && parent == null) 
            {
                return;
            }

            if (!node.SetParent(parent)) 
            {
                return;
            }

            treeNodeCtrl.RefreshHierachy();
        }
    }

    public class AccMoveInfo 
    {
        public OCIChar Chara { get; }
        public int Index { get; }

        public TreeNodeObject Node { get; }

        public AccMoveInfo(OCIChar chara, int index, TreeNodeObject node) 
        {
            Chara = chara;
            Index = index;
            Node = node;
        }
    }

    public class AccessoryCommand : ICommand 
    {

        private readonly Action undo;
        private readonly Action redo;
        
        public AccessoryCommand(Action undo, Action redo) 
        {
            this.undo = undo;
            this.redo = redo;
        }

        public void Do() 
        {
            Debug.LogError(GetType().Name + ".Do() should not be invoked");
        }

        public void Undo() 
        {
            undo.Invoke();
        }

        public void Redo() 
        {
            redo.Invoke();
        }
    }
}