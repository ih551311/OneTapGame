using LastBullet.Combat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 맞은 자리에 잠깐 보이는 점을 코드로 만들지 않고 프리팹으로 바꾼다.
    // 프리팹: Assets/Prefabs/ImpactMarker.prefab (Model 자식에 전용 머티리얼 Materials/ImpactMarker.mat)
    // 메뉴: LastBullet > 맞은 자리 점을 프리팹으로 바꾸기
    public static class ImpactMarkerSetup
    {
        const string PrefabPath = "Assets/Prefabs/ImpactMarker.prefab";
        static readonly string[] Scenes = { "Assets/Scenes/1_Museum.unity", "Assets/Scenes/2_Forest.unity" };

        [MenuItem("LastBullet/맞은 자리 점을 프리팹으로 바꾸기")]
        static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                var material = ArtSlots.GetOrCreateMaterial("ImpactMarker", new Color(0.98f, 0.8f, 0.08f));
                var root = new GameObject("ImpactMarker");
                var model = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                model.name = "Model";
                Object.DestroyImmediate(model.GetComponent<Collider>());
                model.transform.SetParent(root.transform, false);
                model.transform.localScale = Vector3.one * 0.15f;
                model.GetComponent<Renderer>().sharedMaterial = material;
                prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Object.DestroyImmediate(root);
            }

            foreach (var scenePath in Scenes)
            {
                var scene = EditorSceneManager.OpenScene(scenePath);
                var gun = Object.FindFirstObjectByType<OneBulletGun>();
                if (gun == null)
                {
                    Debug.LogWarning($"[LastBullet] {scene.name}: OneBulletGun이 없어서 건너뜀");
                    continue;
                }
                var so = new SerializedObject(gun);
                so.FindProperty("impactMarkerPrefab").objectReferenceValue = prefab;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[LastBullet] {scene.name}: 총에 ImpactMarker 프리팹을 연결했어요.");
            }
            AssetDatabase.SaveAssets();
        }
    }
}
