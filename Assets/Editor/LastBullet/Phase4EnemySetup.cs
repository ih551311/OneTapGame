using LastBullet.Combat;
using LastBullet.Enemy;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // Phase 4-1: 돌진형 적 프리팹(임시 상자 + 큰 머리)을 만들고 1_Museum에 테스트용으로 3마리 놓는다.
    // 아직 움직이지 않는다 (AI는 Phase 4-2).
    // 메뉴: LastBullet > Phase 4-1 돌진형 적 만들기
    public static class Phase4EnemySetup
    {
        const string PrefabPath = "Assets/Prefabs/Enemy_Charger.prefab";

        [MenuItem("LastBullet/Phase 4-1 돌진형 적 만들기")]
        static void Create()
        {
            // 1_Museum 씬이 열려 있지 않으면 자동으로 연다.
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.name != "1_Museum")
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    return;
                scene = EditorSceneManager.OpenScene("Assets/Scenes/1_Museum.unity");
            }
            if (GameObject.Find("TestEnemies (temp)") != null)
            {
                EditorUtility.DisplayDialog("LastBullet", "이미 TestEnemies (temp)가 있어요.", "확인");
                return;
            }

            // 1. 돌진형 적 프리팹: 몸(상자)과 머리(공)에 Hitbox, 맨 위에 EnemyHealth (헤드샷 원탭)
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                var root = new GameObject("Enemy_Charger");
                root.AddComponent<EnemyHealth>();

                var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
                body.name = "Body";
                body.transform.SetParent(root.transform, false);
                body.transform.localPosition = new Vector3(0f, 0.8f, 0f);
                body.transform.localScale = new Vector3(1f, 1.6f, 0.8f);
                body.AddComponent<Hitbox>(); // 기본값 Body

                // 기획서 2.11: 머리를 몸보다 약간 크게 (헤드샷을 노리기 쉽게)
                var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                head.name = "Head";
                head.transform.SetParent(root.transform, false);
                head.transform.localPosition = new Vector3(0f, 2.05f, 0f);
                head.transform.localScale = Vector3.one * 0.9f;
                var headBox = new SerializedObject(head.AddComponent<Hitbox>());
                headBox.FindProperty("part").enumValueIndex = (int)HitPart.Head;
                headBox.ApplyModifiedPropertiesWithoutUndo();

                prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Object.DestroyImmediate(root);
            }

            // 2. 테스트용으로 3마리 배치 (임시, 훈련장 씬을 만들면 지운다)
            var group = new GameObject("TestEnemies (temp)");
            Undo.RegisterCreatedObjectUndo(group, "Create TestEnemies");
            Vector3[] positions = { new Vector3(-3f, 0f, 10f), new Vector3(0f, 0f, 14f), new Vector3(4f, 0f, 11f) };
            for (int i = 0; i < positions.Length; i++)
            {
                var enemy = (GameObject)PrefabUtility.InstantiatePrefab(prefab, group.transform);
                enemy.name = $"Enemy_Charger_{i + 1}";
                enemy.transform.position = positions[i];
                enemy.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // 플레이어 쪽을 본다
            }

            Selection.activeGameObject = group;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[LastBullet] Phase 4-1: Enemy_Charger 프리팹을 만들고 테스트용 3마리를 놓고 1_Museum 씬을 저장했어요.");
        }
    }
}
