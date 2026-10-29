using LastBullet.Enemy;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace LastBullet.EditorTools
{
    // Phase 4-2: 적이 길을 찾아 다닐 수 있게 NavMesh(걸을 수 있는 바닥 지도)를 굽고,
    // 돌진형 적 프리팹에 NavMeshAgent와 EnemyAI를 붙인다.
    // 메뉴: LastBullet > Phase 4-2 적 AI 붙이기
    public static class Phase4AISetup
    {
        const string PrefabPath = "Assets/Prefabs/Enemy_Charger.prefab";

        [MenuItem("LastBullet/Phase 4-2 적 AI 붙이기")]
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

            var player = GameObject.Find("Player");
            if (player == null || AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            {
                EditorUtility.DisplayDialog("LastBullet", "Phase 4-1 메뉴를 먼저 실행하세요.", "확인");
                return;
            }
            if (GameObject.Find("NavMesh") != null)
            {
                EditorUtility.DisplayDialog("LastBullet", "이미 NavMesh가 있어요.", "확인");
                return;
            }

            // 1. 적 프리팹에 NavMeshAgent + EnemyAI
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (root.GetComponent<NavMeshAgent>() == null)
            {
                var agent = root.AddComponent<NavMeshAgent>();
                agent.radius = 0.6f;
                agent.height = 2.5f;
                agent.speed = 4f;
                agent.acceleration = 30f;      // 빨리 출발하고 빨리 멈추게
                agent.stoppingDistance = 0.5f;
            }
            if (root.GetComponent<EnemyAI>() == null)
                root.AddComponent<EnemyAI>();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            PrefabUtility.UnloadPrefabContents(root);

            // 2. 플레이어는 NavMesh를 구울 때 장애물로 치지 않는다 (시작 위치에 구멍이 나지 않게)
            if (player.GetComponent<NavMeshModifier>() == null)
            {
                var modifier = Undo.AddComponent<NavMeshModifier>(player);
                modifier.ignoreFromBuild = true;
            }

            // 3. NavMesh 굽기: 씬의 바닥과 과녁을 보고 걸을 수 있는 곳을 계산한다.
            //    NavMeshAgent가 붙은 적은 기본으로 빠진다. 바닥이나 장애물을 바꾸면 다시 구워야 한다.
            var navMeshObject = new GameObject("NavMesh");
            Undo.RegisterCreatedObjectUndo(navMeshObject, "Create NavMesh");
            var surface = navMeshObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.BuildNavMesh();

            // 구운 결과를 파일로 저장해야 씬을 다시 열어도 남는다
            if (!AssetDatabase.IsValidFolder("Assets/Scenes/1_Museum"))
                AssetDatabase.CreateFolder("Assets/Scenes", "1_Museum");
            AssetDatabase.CreateAsset(surface.navMeshData, "Assets/Scenes/1_Museum/NavMesh-1_Museum.asset");

            Selection.activeGameObject = navMeshObject;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[LastBullet] Phase 4-2: NavMesh를 굽고 Enemy_Charger에 AI를 붙이고 1_Museum 씬을 저장했어요.");
        }
    }
}
