using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace UnityTrain.Editor
{
    public class PatrolRouteWindow : EditorWindow
    {
        PatrolController route;
        Vector2 scroll;
        [MenuItem("培训/巡检路线编辑器")]
        static void Open() { GetWindow<PatrolRouteWindow>("巡检路线"); }
        void OnEnable() { SceneView.duringSceneGui += DrawHandles; }
        void OnDisable() { SceneView.duringSceneGui -= DrawHandles; }
        void OnGUI()
        {
            EditorGUILayout.HelpBox("选择路线后按序配置巡检点。Scene 视图中的位置手柄可移动点位；Ctrl+Z 撤销。", MessageType.Info);
            route = (PatrolController)EditorGUILayout.ObjectField("巡检控制器", route, typeof(PatrolController), true);
            if (!route && GUILayout.Button("查找当前场景路线")) route = FindObjectOfType<PatrolController>();
            if (!route) return;
            scroll = EditorGUILayout.BeginScrollView(scroll);
            var so = new SerializedObject(route);
            so.Update(); EditorGUILayout.PropertyField(so.FindProperty("points"), true); so.ApplyModifiedProperties();
            EditorGUILayout.EndScrollView();
            if (GUILayout.Button("在当前视角添加巡检点"))
            {
                var go = new GameObject("InspectionPoint_" + ((route.points?.Length ?? 0) + 1));
                Undo.RegisterCreatedObjectUndo(go, "添加巡检点");
                go.transform.SetParent(route.transform);
                var view = SceneView.lastActiveSceneView;
                go.transform.position = view ? view.camera.transform.position : Vector3.up * 1.8f;
                var point = Undo.AddComponent<InspectionPoint>(go);
                Undo.RecordObject(route, "更新巡检点顺序");
                route.points = (route.points ?? new InspectionPoint[0]).Concat(new[] { point }).ToArray();
                EditorUtility.SetDirty(route); EditorSceneManager.MarkSceneDirty(route.gameObject.scene);
                Selection.activeGameObject = go;
            }
            if (GUILayout.Button("保存场景")) EditorSceneManager.SaveScene(route.gameObject.scene);
        }
        void DrawHandles(SceneView view)
        {
            if (!route || route.points == null) return;
            for (int i = 0; i < route.points.Length; i++)
            {
                var point = route.points[i]; if (!point) continue;
                Handles.Label(point.transform.position + Vector3.up * .3f, (i + 1) + " · " + (point.device ? point.device.displayName : "未绑定设备"));
                EditorGUI.BeginChangeCheck();
                var position = Handles.PositionHandle(point.transform.position, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(point.transform, "移动巡检点"); point.transform.position = position;
                    EditorSceneManager.MarkSceneDirty(point.gameObject.scene);
                }
            }
        }
    }
}
