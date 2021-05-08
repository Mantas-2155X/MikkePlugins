using System;
using System.Collections.Generic;
using System.Text;
using BepInEx;
using ExtensibleSaveFormat;
using Illusion.Extensions;
using Studio;
using UnityEngine;
using Vectrosity;
using Object = UnityEngine.Object;

namespace MapController {
    [BepInPlugin(GUID, "Map Controller plugin", VERSION)]
    [BepInProcess("StudioNEOV2")]
    public class MapControllerPlugin : BaseUnityPlugin {
        public const string GUID = "mikke.MapController";
        internal const string VERSION = "1.0";

        private readonly List<VectorLine> boundingLines = new List<VectorLine>();

        public void Start() {
            ExtendedSave.SceneBeingLoaded += ExtendedSaveOnSceneBeingLoaded;
            ExtendedSave.SceneBeingSaved += ExtendedSaveOnSceneBeingSaved;
        }

        private void InitBounds() {
            float size = 0.012f;
            if (boundingLines.Count == 0) {
                Vector3 topLeftForward = (Vector3.up + Vector3.left + Vector3.forward) * size,
                    topRightForward = (Vector3.up + Vector3.right + Vector3.forward) * size,
                    bottomLeftForward = ((Vector3.down + Vector3.left + Vector3.forward) * size),
                    bottomRightForward = ((Vector3.down + Vector3.right + Vector3.forward) * size),
                    topLeftBack = (Vector3.up + Vector3.left + Vector3.back) * size,
                    topRightBack = (Vector3.up + Vector3.right + Vector3.back) * size,
                    bottomLeftBack = (Vector3.down + Vector3.left + Vector3.back) * size,
                    bottomRightBack = (Vector3.down + Vector3.right + Vector3.back) * size;
                boundingLines.Add(VectorLine.SetLine(Color.green, topLeftForward, topRightForward));
                boundingLines.Add(VectorLine.SetLine(Color.green, topRightForward, bottomRightForward));
                boundingLines.Add(VectorLine.SetLine(Color.green, bottomRightForward, bottomLeftForward));
                boundingLines.Add(VectorLine.SetLine(Color.green, bottomLeftForward, topLeftForward));
                boundingLines.Add(VectorLine.SetLine(Color.green, topLeftBack, topRightBack));
                boundingLines.Add(VectorLine.SetLine(Color.green, topRightBack, bottomRightBack));
                boundingLines.Add(VectorLine.SetLine(Color.green, bottomRightBack, bottomLeftBack));
                boundingLines.Add(VectorLine.SetLine(Color.green, bottomLeftBack, topLeftBack));
                boundingLines.Add(VectorLine.SetLine(Color.green, topLeftBack, topLeftForward));
                boundingLines.Add(VectorLine.SetLine(Color.green, topRightBack, topRightForward));
                boundingLines.Add(VectorLine.SetLine(Color.green, bottomRightBack, bottomRightForward));
                boundingLines.Add(VectorLine.SetLine(Color.green, bottomLeftBack, bottomLeftForward));

                foreach (VectorLine line in boundingLines) {
                    line.lineWidth = 2f;
                    line.active = false;
                }
            }
        }

        private void ExtendedSaveOnSceneBeingLoaded(string path) {
            PluginData data = ExtendedSave.GetSceneExtendedDataById(GUID);
            if (data != null && data.data.TryGetValue("MAP", out var info) && info is string nodeInfo) {
                var infoStrings = ParseNodeInfo(nodeInfo);
                var enumerator = infoStrings.GetEnumerator();
                enumerator.MoveNext();
                enumerator.MoveNext();
                rootNode = new InfoNode(enumerator);
            }
        }

        private InfoNode rootNode;

        private void LateUpdate() {
            if (rootNode != null) {
                map = Singleton<Studio.Map>.Instance.MapRoot;
                if (map == null) {
                    return;
                }

                rootNode.TraverseOnLoad(map);
                rootNode = null;
            }
        }

        private IEnumerable<string> ParseNodeInfo(string data) {
            string[] str = data.Split('§');
            foreach (var s in str) {
                yield return s;
            }
        }

        private void ExtendedSaveOnSceneBeingSaved(string path) {
            if (map == null) return;
            PluginData data = new PluginData();
            data.data.Add("VERSION", VERSION);
            data.data.Add("MAP", SaveChanges());
            ExtendedSave.SetSceneExtendedDataById(GUID, data);
        }

        private Rect mapControllerWindowRect = Rect.zero;

        void OnGUI() {
            if (MapWindowIsInactive()) return;

            map = Singleton<Studio.Map>.Instance.MapRoot;
            if (map == null) {
                return;
            }

            if (mapControllerWindowRect == Rect.zero) {
                mapControllerWindowRect = new Rect(Screen.width - 400, Screen.height < 1440 ? 20 : Screen.height / 2, 375, 715);
            }

            mapControllerWindowRect = GUILayout.Window(GetHashCode(), mapControllerWindowRect, MakeWin, "Map Controller Plugin");
            if (!mapControllerWindowRect.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y)))
                return;
            Input.ResetInputAxes();
        }

        private static bool MapWindowIsInactive() {
            return Singleton<MapCtrl>.Instance == null || !Singleton<MapCtrl>.Instance.gameObject.activeSelf;
        }

        void MakeWin(int num) {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            GUILayout.BeginHorizontal();
            string search = Search;
            GUILayout.Label("Search", GUILayout.ExpandWidth(false));
            Search = GUILayout.TextField(Search);
            if (GUILayout.Button("X", GUILayout.ExpandWidth(false)))
                Search = "";
            if (search.Length != 0 && selected != null && (Search.Length == 0 || Search.Length < search.Length && search.StartsWith(Search))) {
                string objName = selected.name;

                if (selected.name.IndexOf(search, StringComparison.OrdinalIgnoreCase) != -1 || objName.IndexOf(search, StringComparison.OrdinalIgnoreCase) != -1)
                    OpenParents(selected.gameObject);
            }


            Color color = GUI.color;
            if (ShowModified) GUI.color = Color.magenta;
            if (GUILayout.Button("Modified only", GUILayout.Width(100))) {
                ShowModified = !ShowModified;
            }

            GUI.color = color;

            GUILayout.EndHorizontal();
            scrollVector = GUILayout.BeginScrollView(scrollVector, GUI.skin.box, GUILayout.Width(375), GUILayout.Height(470));

            DisplayObjectTree(map, 0);
            GUILayout.EndScrollView();

            if (selected != null) {
                if (GUILayout.Button("Set " + (selected.activeSelf ? "inactive" : "active"))) {
                    ToggleActive();
                }

                GUILayout.BeginHorizontal("box");
                MakeBox("Move", SetTranslate, ResetTranslate);
                MakeBox("Rotate", SetRotate, ResetRotate);
                MakeBox("Scale", SetScale, ResetScale);
                GUILayout.EndHorizontal();


                GUILayout.BeginHorizontal("box");
                GUILayout.Label("Step: " + step, GUILayout.MinWidth(200), GUILayout.MaxWidth(200));

                if (GUILayout.Button("-")) slideStep--;
                if (GUILayout.Button("+")) slideStep++;

                step = (float) Math.Pow(10, slideStep);

                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUI.DragWindow();
        }

        private void SetScale(float x, float y, float z) {
            var node = CheckDirtyNode();
            var scl = new Vector3(x / 10, y / 10, z / 10);
            node.NewScl += scl;

            selected.transform.localScale += scl;
        }

        private void ResetScale() {
            if (dirtyNodes.TryGetValue(selected, out var node)) {
                selected.transform.localScale -= node.NewScl;
                node.NewScl = Vector3.zero;
                UpdateDirtyNode(node);
            }
        }

        private void SetRotate(float x, float y, float z) {
            var node = CheckDirtyNode();
            selected.transform.Rotate(x, y, z, Space.Self);
            node.NewRot = selected.transform.eulerAngles;
        }

        private void ResetRotate() {
            if (dirtyNodes.TryGetValue(selected, out var node)) {
                selected.transform.rotation = Quaternion.Euler(node.OrgRot);
                node.NewRot = Vector3.zero;
                UpdateDirtyNode(node);
            }
        }

        private void SetTranslate(float x, float y, float z) {
            var node = CheckDirtyNode();
            node.NewPos += new Vector3(x, y, z);

            selected.transform.Translate(x, y, z, Space.World);
        }

        private void ResetTranslate() {
            if (dirtyNodes.TryGetValue(selected, out var node)) {
                selected.transform.Translate(-node.NewPos, Space.World);
                node.NewPos = Vector3.zero;
                UpdateDirtyNode(node);
            }
        }

        private float slideStep;

        private float step = 1f;

        private void MakeBox(string label, Action<float, float, float> transformAction, Action resetAction) {
            GUILayout.BeginVertical();
            GUILayout.Label(label);

            GUILayout.BeginHorizontal();
            GUILayout.Label("X");
            if (GUILayout.Button("-1")) transformAction(-step, 0f, 0f);
            if (GUILayout.Button("+1")) transformAction(step, 0f, 0f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Y");
            if (GUILayout.Button("-1")) transformAction(0f, -step, 0f);
            if (GUILayout.Button("+1")) transformAction(0f, step, 0f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Z");
            if (GUILayout.Button("-1")) transformAction(0f, 0f, -step);
            if (GUILayout.Button("+1")) transformAction(0f, 0f, step);
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Reset")) resetAction();
            GUILayout.EndVertical();
        }

        private void ToggleActive() {
            var infoNode = CheckDirtyNode();
            selected.SetActive(!selected.activeSelf);
            UpdateDirtyNode(infoNode);
        }

        private void UpdateDirtyNode(InfoNode infoNode) {
            if (infoNode.DefaultActive == selected.activeSelf
                && infoNode.NewPos == Vector3.zero
                && infoNode.NewRot == Vector3.zero
                && infoNode.NewScl == Vector3.zero) {
                dirtyNodes.Remove(selected);
            }
        }

        private InfoNode CheckDirtyNode() {
            InfoNode infoNode;
            if (dirtyNodes.TryGetValue(selected, out infoNode)) {
                return infoNode;
            }

            infoNode = new InfoNode();
            infoNode.Dirty = true;
            infoNode.DefaultActive = selected.activeSelf;
            infoNode.OrgRot = selected.transform.rotation.eulerAngles;
            dirtyNodes.Add(selected, infoNode);

            return infoNode;
        }

        private string SaveChanges() {
            InfoNode rootNode = MapInfoNode(map);

            StringBuilder stringBuilder = new StringBuilder();
            rootNode.WriteToString(stringBuilder);
            var series = stringBuilder.ToString();

            return series;
        }

        private InfoNode MapInfoNode(GameObject o) {
            InfoNode node = new InfoNode();
            if (dirtyNodes.ContainsKey(o)) {
                var dirtyNode = dirtyNodes[o];
                node.Dirty = true;
                node.NewPos = dirtyNode.NewPos;
                node.NewRot = dirtyNode.NewRot;
                node.NewScl = dirtyNode.NewScl;
            }

            node.Name = o.name;
            node.Inactive = !o.activeSelf;
            node.Children = new List<InfoNode>();

            foreach (var child in o.Children()) {
                node.Children.Add(MapInfoNode(child));
            }

            return node;
        }

        private void OpenParents(GameObject child) {
            for (child = child.transform.parent.gameObject; (Object) child.transform != (Object) map.transform; child = child.transform.parent.gameObject)
                openedObjects.Add(child);
            openedObjects.Add(child);
        }

        //placeholder variables
        private readonly HashSet<GameObject> openedObjects = new HashSet<GameObject>();
        private static string Search = "";
        private static bool ShowModified;
        public static readonly Dictionary<GameObject, InfoNode> dirtyNodes = new Dictionary<GameObject, InfoNode>();
        private readonly HashSet<GameObject> childObjects = new HashSet<GameObject>();
        private Vector2 scrollVector;

        public static GameObject map;
        private GameObject selected;

        private void DisplayObjectTree(GameObject go, int indent) {
            if (go == null)
                return;
            string objName = go.name;
            if (!ShowModified && (Search.Length == 0 || go.name.IndexOf(Search, StringComparison.OrdinalIgnoreCase) != -1 || objName.IndexOf(Search, StringComparison.OrdinalIgnoreCase) != -1)
                || ShowModified && dirtyNodes.ContainsKey(go)) {
                Color color = GUI.color;
                if (!go.activeSelf)
                    GUI.color = Color.red;
                if (dirtyNodes.ContainsKey(go))
                    GUI.color = Color.magenta;
                if (selected == go)
                    GUI.color = Color.cyan;
                GUILayout.BeginHorizontal();
                if (Search.Length == 0 && !ShowModified) {
                    GUILayout.Space(indent * 20f);
                    int num = 0;
                    for (int index = 0; index < go.transform.childCount; ++index) {
                        if (!childObjects.Contains(go.transform.GetChild(index).gameObject))
                            ++num;
                    }

                    if (num != 0) {
                        if (GUILayout.Toggle((openedObjects.Contains(go) ? 1 : 0) != 0, "", GUILayout.ExpandWidth(false))) {
                            if (!openedObjects.Contains(go))
                                openedObjects.Add(go);
                        } else if (openedObjects.Contains(go))
                            openedObjects.Remove(go);
                    } else
                        GUILayout.Space(20f);
                }

                if (GUILayout.Button(objName + (dirtyNodes.ContainsKey(go) ? "*" : ""), GUILayout.ExpandWidth(false))) {
                    var e = Event.current;

                    if ((e.type == EventType.MouseUp || EventType.Used == e.type) && e.button == 1) {
                        StartFlash(go);
                    } else {
                        selected = go;
                    }
                }

                GUI.color = color;
                GUILayout.EndHorizontal();
            }

            if (Search.Length == 0 && !openedObjects.Contains(go) && !ShowModified)
                return;
            for (int index = 0; index < go.transform.childCount; ++index)
                DisplayObjectTree(go.transform.GetChild(index).gameObject, indent + 1);
        }

        private GameObject flashing;
        private int flashCount;
        private float lastflash;

        private void StartFlash(GameObject go) {
            if (!go.activeSelf)
                return;
            if (flashing != null) {
                flashing.SetActive(true);
            }

            flashing = go;
            flashCount = 6;
            lastflash = Time.time;
        }

        private void EndFlash() {
            flashing.SetActive(true);
            flashing = null;
        }

        private void Flash() {
            flashing.SetActive(!flashing.activeSelf);
            lastflash = Time.time;
        }

        private void Update() {
            if (flashCount > 0 && Time.time - lastflash > 0.1f) {
                if (--flashCount <= 0) {
                    EndFlash();
                } else {
                    Flash();
                }
            }

            DrawBounds();
        }

        private bool LinesActive = false;

        private void DrawBounds() {
            if (MapWindowIsInactive() || selected == null) {
                if (LinesActive) {
                    LinesActive = false;
                    foreach (VectorLine line in boundingLines)
                        line.active = false;
                }

                return;
            }

            LinesActive = true;
            InitBounds();
            Bounds? boundsOpt = UpdateSelectedBounds();
            if (boundsOpt == null) return;

            Bounds bounds = boundsOpt.Value;
            Vector3 topLeftForward = new Vector3(bounds.min.x, bounds.max.y, bounds.max.z),
                topRightForward = bounds.max,
                bottomLeftForward = new Vector3(bounds.min.x, bounds.min.y, bounds.max.z),
                bottomRightForward = new Vector3(bounds.max.x, bounds.min.y, bounds.max.z),
                topLeftBack = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z),
                topRightBack = new Vector3(bounds.max.x, bounds.max.y, bounds.min.z),
                bottomLeftBack = bounds.min,
                bottomRightBack = new Vector3(bounds.max.x, bounds.min.y, bounds.min.z);
            int i = 0;
            SetPoints(boundingLines[i++], topLeftForward, topRightForward);
            SetPoints(boundingLines[i++], topRightForward, bottomRightForward);
            SetPoints(boundingLines[i++], bottomRightForward, bottomLeftForward);
            SetPoints(boundingLines[i++], bottomLeftForward, topLeftForward);
            SetPoints(boundingLines[i++], topLeftBack, topRightBack);
            SetPoints(boundingLines[i++], topRightBack, bottomRightBack);
            SetPoints(boundingLines[i++], bottomRightBack, bottomLeftBack);
            SetPoints(boundingLines[i++], bottomLeftBack, topLeftBack);
            SetPoints(boundingLines[i++], topLeftBack, topLeftForward);
            SetPoints(boundingLines[i++], topRightBack, topRightForward);
            SetPoints(boundingLines[i++], bottomRightBack, bottomRightForward);
            SetPoints(boundingLines[i++], bottomLeftBack, bottomLeftForward);

            foreach (VectorLine line in boundingLines) {
                line.active = true;
                line.Draw();
            }
        }

        public static void SetPoints(VectorLine vl, params Vector3[] points) {
            for (int index = 0; index < vl.points3.Count; ++index)
                vl.points3[index] = points[index];
        }

        private Bounds? UpdateSelectedBounds() {
            var renderer = selected.GetComponent<Renderer>();
            if (renderer != null) return renderer.bounds;

            var childRendrz = selected.GetComponentsInChildren<Renderer>();
            if (childRendrz.IsNullOrEmpty()) return null;

            Bounds baz = childRendrz[0].bounds;
            for (int index = 1; index < childRendrz.Length; index++) {
                baz.Encapsulate(childRendrz[index].bounds);
            }

            return baz;
        }
    }


    public class InfoNode {
        public string Name;
        public bool Inactive;
        public List<InfoNode> Children;
        public bool Dirty;

        public Vector3 NewPos, NewRot, OrgRot, NewScl;
        public bool DefaultActive = true;

        public InfoNode(IEnumerator<string> enumerator) {
            var nodeString = enumerator.Current;
            enumerator.MoveNext();
            var nodeData = nodeString.Split('%');

            int childCount = Convert.ToInt32(nodeData[0]);
            Name = nodeData[1];

            Dirty = nodeData[2].Equals("1");
            Inactive = nodeData[3].Equals("1");

            if (nodeData.Length > 3) {
                NewPos = ReadVector(nodeData[4]);
                NewRot = ReadVector(nodeData[5]);
                NewScl = ReadVector(nodeData[6]);
            }

            Children = new List<InfoNode>();

            for (int index = 0; index < childCount; index++) {
                Children.Add(new InfoNode(enumerator));
            }
        }

        public InfoNode() {
        }


        public void WriteToString(StringBuilder stringBuilder) {
            stringBuilder.Append("§").Append(Children.Count).Append("%")
                .Append(Name.Replace("§", "").Replace("%", ""))
                .Append("%").Append(Dirty ? "1" : "0")
                .Append("%").Append(Inactive ? "1" : "0")
                .Append("%").Append(WriteVector(NewPos))
                .Append("%").Append(WriteVector(NewRot))
                .Append("%").Append(WriteVector(NewScl));
            foreach (var child in Children) {
                child.WriteToString(stringBuilder);
            }
        }

        private static Vector3 ReadVector(String s) {
            var vec = s.Split(',');
            return new Vector3(float.Parse(vec[0]), float.Parse(vec[1]), float.Parse(vec[2]));
        }

        private static string WriteVector(Vector3 v) {
            return v.x + "," + v.y + "," + v.z;
        }

        public void TraverseOnLoad(GameObject mapElement) {
            if (Dirty) {
                mapElement.SetActive(!Inactive);
                MapControllerPlugin.dirtyNodes[mapElement] = this;
                mapElement.transform.Translate(NewPos, Space.World);
                OrgRot = mapElement.transform.rotation.eulerAngles;
                mapElement.transform.rotation = Quaternion.Euler(NewRot);
                mapElement.transform.localScale += NewScl;
            }

            if (mapElement.name != Name) {
                UnityEngine.Debug.LogError("name mismatch: " + mapElement.name + "|" + Name);
            }

            var mapkids = mapElement.Children();
            if (mapkids.Count != Children.Count) {
                UnityEngine.Debug.LogError("child count mismatch in " + Name + " : " + mapkids.Count + "|" + Children.Count);
            } else {
                for (int index = 0; index < Children.Count; index++) {
                    Children[index].TraverseOnLoad(mapkids[index]);
                }
            }
        }
    }
}