using System.Collections.Generic;
using System.Linq;
using LastBullet.Enemy;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Random = System.Random;

namespace LastBullet.EditorTools
{
    // 숲의 적을 늘린다 (2026-10-30 사용자 요청: 적이 너무 적다). 스포너를 더 만든다.
    //   기존 7마리(돌진형 3 + 방어형 2 + 원거리형 2) + 추가 7마리(돌진형 3 + 방어형 2 + 원거리형 2) = 14마리 (같은 비율)
    // 새 스포너 자리 규칙: 걸어갈 수 있는 곳(NavMesh) 위, 플레이어 시작에서 30m 이상, 다른 스포너에서 9m 이상, 숲 안쪽.
    // 이미 있는 스포너는 건드리지 않는다. 더 늘리고 싶으면 아래 숫자를 바꾸고 메뉴를 다시 누르면 된다 (이름 번호가 이어진다).
    // 메뉴: LastBullet > Phase 10 적 스포너 늘리기
    public static class Phase10MoreSpawnersSetup
    {
        const string ForestPath = "Assets/Scenes/2_Forest.unity";
        const int AddCharger = 3, AddShield = 2, AddThrower = 2;
        static readonly Vector2 PlayerStart = new Vector2(0f, -44f);

        [MenuItem("LastBullet/Phase 10 적 스포너 늘리기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var scene = EditorSceneManager.OpenScene(ForestPath);
            var spawnersRoot = GameObject.Find("Spawners")?.transform;
            var enemies = GameObject.Find("Enemies")?.transform;
            if (spawnersRoot == null || enemies == null)
            {
                EditorUtility.DisplayDialog("LastBullet", "Phase 10 적 스포너 만들기를 먼저 실행하세요.", "확인");
                return;
            }

            var taken = spawnersRoot.Cast<Transform>().Select(t => new Vector2(t.position.x, t.position.z)).ToList();
            var random = new Random(20261031 + taken.Count);
            int count = 0;
            count += Add("Charger", AddCharger, spawnersRoot, enemies, taken, random);
            count += Add("Shield", AddShield, spawnersRoot, enemies, taken, random);
            count += Add("Thrower", AddThrower, spawnersRoot, enemies, taken, random);

            Selection.activeGameObject = spawnersRoot.gameObject;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[LastBullet] 스포너 {count}개를 더 만들었어요. 지금 숲의 스포너: {spawnersRoot.childCount}개.");
        }

        static int Add(string kind, int amount, Transform spawnersRoot, Transform enemies, List<Vector2> taken, Random random)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Enemy_{kind}.prefab");
            int number = spawnersRoot.Cast<Transform>().Count(t => t.name.StartsWith($"Spawner_{kind}_"));
            int added = 0;

            for (int i = 0; i < amount; i++)
            {
                if (!TryFindSpot(taken, random, out Vector3 position))
                {
                    Debug.LogWarning($"[LastBullet] {kind}: 알맞은 자리를 못 찾았어요 ({added}/{amount}개만 만듦)");
                    break;
                }
                taken.Add(new Vector2(position.x, position.z));

                var spawner = new GameObject($"Spawner_{kind}_{++number:00}");
                spawner.transform.SetParent(spawnersRoot, false);
                Vector3 toPlayer = new Vector3(PlayerStart.x - position.x, 0f, PlayerStart.y - position.z);
                spawner.transform.SetPositionAndRotation(position, Quaternion.LookRotation(toPlayer));

                var script = spawner.AddComponent<EnemySpawner>();
                var so = new SerializedObject(script);
                so.FindProperty("enemyPrefab").objectReferenceValue = prefab;
                so.FindProperty("enemiesParent").objectReferenceValue = enemies;
                so.FindProperty("faceTowardsPlayer").boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
                added++;
            }
            return added;
        }

        // 숲 안쪽에서 무작위로 고른 자리 중, 규칙에 맞는 곳을 찾는다
        static bool TryFindSpot(List<Vector2> taken, Random random, out Vector3 position)
        {
            for (int attempt = 0; attempt < 400; attempt++)
            {
                var candidate = new Vector2(
                    Mathf.Lerp(-44f, 44f, (float)random.NextDouble()),
                    Mathf.Lerp(-30f, 50f, (float)random.NextDouble()));

                if (Vector2.Distance(candidate, PlayerStart) < 30f)
                    continue;
                if (taken.Any(t => Vector2.Distance(candidate, t) < 9f))
                    continue;
                // 걸어갈 수 있는 곳인지: 후보 바로 옆(1m 안)에 NavMesh가 있어야 한다 (나무·바위·건물 안이면 멀어진다)
                if (!NavMesh.SamplePosition(new Vector3(candidate.x, 0f, candidate.y), out var hit, 1f, NavMesh.AllAreas))
                    continue;

                position = hit.position;
                return true;
            }
            position = default;
            return false;
        }
    }
}
