using LastBullet.Stage;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 포털 거울(StageMap/Portal/Mirror_Surface)에 내 모습이 비치게 한다 (2026-11-01 사용자 요청).
    //   - 거울 머티리얼 Assets/Materials/PortalMirror.mat의 셰이더를 LastBullet/PlanarMirror(Assets/Shaders)로 바꾼다.
    //   - StageMap에 PlanarMirror(반사 카메라 스크립트)를 붙이고 거울 면을 연결한다.
    // 먼저 Phase 53(포털)이 있어야 한다. 다시 실행해도 같은 결과다. 메뉴: LastBullet > Phase 55 포털 거울에 모습 비추기
    public static class Phase55PortalMirrorSetup
    {
        [MenuItem("LastBullet/Phase 55 포털 거울에 모습 비추기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/1_Museum.unity");
            var stage = GameObject.Find("StageMap");
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/PortalMirror.mat");
            var shader = Shader.Find("LastBullet/PlanarMirror");
            if (stage == null || material == null || shader == null)
            {
                Debug.LogError("[LastBullet] StageMap, PortalMirror.mat, PlanarMirror 셰이더 중 없는 것이 있어요 (먼저 Phase 53)");
                return;
            }

            material.shader = shader;
            material.SetColor("_Tint", new Color(0.86f, 0.92f, 1f, 1f));
            material.SetFloat("_Brightness", 1f);
            EditorUtility.SetDirty(material);

            Renderer mirror = null;
            foreach (var r in stage.GetComponentsInChildren<Renderer>(true))
                if (r.name.StartsWith("Mirror_Surface"))
                    mirror = r;
            if (mirror == null)
            {
                Debug.LogError("[LastBullet] Mirror_Surface를 찾지 못했어요");
                return;
            }

            var script = stage.GetComponent<PlanarMirror>();
            if (script == null)
                script = stage.AddComponent<PlanarMirror>();
            var so = new SerializedObject(script);
            so.FindProperty("mirror").objectReferenceValue = mirror;
            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[LastBullet] 포털 거울에 모습이 비치게 했어요 (PlanarMirror).");
        }
    }
}
