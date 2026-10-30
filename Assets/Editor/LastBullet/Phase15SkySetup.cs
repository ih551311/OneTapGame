using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 맵마다 어울리는 하늘로 바꾼다 (2026-10-31 사용자 요청, 에셋 AllSky Free - 10 Sky / Skybox Set, 제작자 rpgwhitelock).
    //  숲 = Cartoon Base BlueSky (맑은 낮, 카툰풍) / 성 마을 = Epic_BlueSunset (푸른 노을, 성에 어울리는 웅장한 하늘) / 사막 = Epic_GloriousPink (달아오른 분홍 노을)
    // 하늘은 장면 설정(Lighting의 Skybox Material)이라 Scene 창의 Window > Rendering > Lighting에서 머티리얼만 바꾸면 하늘이 바뀐다.
    // 예전에 쓰던 하늘 돔과 구름(RPG Poly Pack)은 지운다. 맵을 다시 만들면 예전 하늘이 돌아오므로 이 메뉴를 다시 실행한다.
    // 메뉴: LastBullet > Phase 15 맵마다 하늘 바꾸기
    public static class Phase15SkySetup
    {
        const string Root = "Assets/AllSkyFree/";

        static readonly (string scene, string material)[] Skies =
        {
            ("Assets/Scenes/2_Forest.unity", "Cartoon Base BlueSky/Day_BlueSky_Nothing.mat"),
            ("Assets/Scenes/3_CastleVillage.unity", "Epic_BlueSunset/Epic_BlueSunset.mat"),
            ("Assets/Scenes/4_Desert.unity", "Epic_GloriousPink/Epic_GloriousPink.mat"),
        };

        [MenuItem("LastBullet/Phase 15 맵마다 하늘 바꾸기")]
        static void Apply()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            foreach (var (scenePath, materialPath) in Skies)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(Root + materialPath);
                if (material == null)
                {
                    Debug.LogError($"[LastBullet] 하늘 머티리얼을 못 찾음: {materialPath}");
                    continue;
                }
                var scene = EditorSceneManager.OpenScene(scenePath);
                RenderSettings.skybox = material;

                // 예전 하늘 돔·구름 지우기 (Environment/Backdrop 아래)
                int removed = 0;
                foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (t == null)
                        continue;
                    if (t.name == "SkyDome" || t.name.StartsWith("rpgpp_lt_cloud"))
                    {
                        Object.DestroyImmediate(t.gameObject);
                        removed++;
                    }
                }

                var camera = Camera.main;
                if (camera != null)
                    camera.clearFlags = CameraClearFlags.Skybox;
                DynamicGI.UpdateEnvironment();

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[LastBullet] {scene.name}: 하늘을 {material.name}(으)로 바꿨어요 (예전 하늘·구름 {removed}개 지움).");
            }
            AssetDatabase.SaveAssets();
        }
    }
}
