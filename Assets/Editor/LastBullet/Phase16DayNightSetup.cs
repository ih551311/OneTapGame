using LastBullet.Stage;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 숲·성 마을·사막 씬에 낮과 밤(DayNight)을 넣는다 (2026-10-31 사용자 요청: 능력 없이 겉모습만 무작위로).
    //  밤하늘: 숲 = Cartoon Base NightSky / 성 마을 = Night MoonBurst / 사막 = Cold Night (에셋 AllSky Free)
    // 낮 하늘은 Phase 15에서 정한 것을 그대로 쓴다. 이미 DayNight가 있으면 건너뛴다.
    // 메뉴: LastBullet > Phase 16 낮과 밤 넣기
    public static class Phase16DayNightSetup
    {
        const string Root = "Assets/AllSkyFree/";

        static readonly (string scene, string nightMaterial)[] Maps =
        {
            ("Assets/Scenes/2_Forest.unity", "Cartoon Base NightSky/Cartoon Base NightSky.mat"),
            ("Assets/Scenes/3_CastleVillage.unity", "Night MoonBurst/Night Moon Burst.mat"),
            ("Assets/Scenes/4_Desert.unity", "Cold Night/Cold Night.mat"),
        };

        [MenuItem("LastBullet/Phase 16 낮과 밤 넣기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            foreach (var (scenePath, materialPath) in Maps)
            {
                var night = AssetDatabase.LoadAssetAtPath<Material>(Root + materialPath);
                if (night == null)
                {
                    Debug.LogError($"[LastBullet] 밤하늘 머티리얼을 못 찾음: {materialPath}");
                    continue;
                }
                var scene = EditorSceneManager.OpenScene(scenePath);
                if (Object.FindFirstObjectByType<DayNight>() != null)
                    continue;

                var go = new GameObject("DayNight");
                var script = go.AddComponent<DayNight>();
                var so = new SerializedObject(script);
                so.FindProperty("nightSky").objectReferenceValue = night;
                so.FindProperty("sun").objectReferenceValue = FindSun();
                so.ApplyModifiedPropertiesWithoutUndo();

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[LastBullet] {scene.name}: DayNight를 넣었어요 (밤하늘 {night.name}).");
            }
            AssetDatabase.SaveAssets();
        }

        static Light FindSun()
        {
            if (RenderSettings.sun != null)
                return RenderSettings.sun;
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (light.type == LightType.Directional)
                    return light;
            }
            return null;
        }
    }
}
