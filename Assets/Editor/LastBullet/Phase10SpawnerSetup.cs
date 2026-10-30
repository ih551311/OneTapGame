using System.Linq;
using LastBullet.Enemy;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 숲의 적 7마리를 "적마다 스포너" 방식으로 바꾼다 (2026-10-30 사용자 요청).
    //   Enemies 아래에 직접 놓여 있던 적을 지우고, 같은 자리·같은 종류의 스포너(EnemySpawner)를 Spawners 아래에 만든다.
    //   스테이지가 시작되면 스포너가 자기 적을 만들어 Enemies 아래에 둔다.
    // 또 원거리형 적 프리팹의 탄속을 16으로 바꾼다 (이전 12).
    // 메뉴: LastBullet > Phase 10 적 스포너 만들기
    public static class Phase10SpawnerSetup
    {
        const string ForestPath = "Assets/Scenes/2_Forest.unity";
        const string ThrowerPath = "Assets/Prefabs/Enemy_Thrower.prefab";

        [MenuItem("LastBullet/Phase 10 적 스포너 만들기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            SetProjectileSpeed(16f);

            var scene = EditorSceneManager.OpenScene(ForestPath);
            if (GameObject.Find("Spawners") != null)
            {
                EditorUtility.DisplayDialog("LastBullet", "이미 Spawners가 있어요.", "확인");
                return;
            }
            var enemies = GameObject.Find("Enemies").transform;
            var spawners = new GameObject("Spawners").transform;

            // 종류별 번호를 매기며, 지금 놓여 있는 적 자리마다 스포너를 만든다
            int charger = 0, shield = 0, thrower = 0;
            foreach (var enemy in enemies.Cast<Transform>().ToArray())
            {
                string kind = enemy.name.Split('_')[1]; // Enemy_Charger_1 → Charger
                string prefabPath = $"Assets/Prefabs/Enemy_{kind}.prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                int number = kind == "Charger" ? ++charger : kind == "Shield" ? ++shield : ++thrower;

                var spawner = new GameObject($"Spawner_{kind}_{number:00}");
                spawner.transform.SetParent(spawners, false);
                spawner.transform.SetPositionAndRotation(enemy.position, enemy.rotation);
                var script = spawner.AddComponent<EnemySpawner>();
                var so = new SerializedObject(script);
                so.FindProperty("enemyPrefab").objectReferenceValue = prefab;
                so.FindProperty("enemiesParent").objectReferenceValue = enemies;
                so.FindProperty("faceTowardsPlayer").boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();

                Object.DestroyImmediate(enemy.gameObject);
            }

            Selection.activeGameObject = spawners.gameObject;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[LastBullet] 숲의 적을 스포너로 바꿨어요: 돌진형 {charger}, 방어형 {shield}, 원거리형 {thrower}. 원거리형 탄속은 16이에요.");
        }

        static void SetProjectileSpeed(float speed)
        {
            var root = PrefabUtility.LoadPrefabContents(ThrowerPath);
            var so = new SerializedObject(root.GetComponent<RangedEnemyAI>());
            so.FindProperty("projectileSpeed").floatValue = speed;
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, ThrowerPath);
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
