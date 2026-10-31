using System.Collections.Generic;
using LastBullet.Enemy;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace LastBullet.EditorTools
{
    // 분열 적을 만든다 (2026-10-31 사용자 요청): 처치하면 작은 적 2마리로 나뉜다.
    //   Enemy_Splitter      돌진형을 복사해 더 크고(1.2배) 느리게(추적 3.5, 돌진 8). EnemySplitter가 처치 때 나뉘게 한다.
    //   Enemy_SplitterMini  돌진형을 복사해 작고(0.55배) 빠르게(추적 6.5, 돌진 11), 머리가 더 작다. 공격 8. 물감 15, 종이조각 0.
    //   두 적은 전용 머티리얼(Enemy_Splitter.mat, Enemy_SplitterMini.mat: 돌진형 것을 복사한 흰색 바탕)을 쓰고, 몸 색은 코드(EnemyDrop)가 칠한다.
    // 숲 2, 성 마을 3, 사막 3곳에 스포너(Spawner_Splitter_NN)를 놓는다 (돌진형 스포너 근처 걸을 수 있는 자리).
    // 다시 실행하면 프리팹 값을 새로 맞추고 스포너를 지우고 다시 놓는다. 메뉴: LastBullet > Phase 46 분열 적 만들기
    public static class Phase46SplitterSetup
    {
        const string ChargerPath = "Assets/Prefabs/Enemy_Charger.prefab";
        const string SplitterPath = "Assets/Prefabs/Enemy_Splitter.prefab";
        const string MiniPath = "Assets/Prefabs/Enemy_SplitterMini.prefab";

        static readonly (string scene, int count)[] Scenes =
        {
            ("Assets/Scenes/2_Forest.unity", 2),
            ("Assets/Scenes/3_CastleVillage.unity", 3),
            ("Assets/Scenes/4_Desert.unity", 3),
        };

        [MenuItem("LastBullet/Phase 46 분열 적 만들기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var mini = BuildEnemy(MiniPath, "Enemy_SplitterMini", 0.55f, 0.8f, 0.35f, 1.4f);
            var splitter = BuildEnemy(SplitterPath, "Enemy_Splitter", 1.2f, 1f, 0.7f, 3.0f);

            // 분열 적: 느리고 묵직하게, 나뉘는 기능 추가
            Edit(SplitterPath, root =>
            {
                SetAI(root, 3.5f, 8f, 4.5f, 0.5f, 0.5f, 0.8f, 15, 1.5f);
                var comp = root.GetComponent<EnemySplitter>() ?? root.AddComponent<EnemySplitter>();
                var so = new SerializedObject(comp);
                so.FindProperty("miniPrefab").objectReferenceValue = mini;
                so.ApplyModifiedPropertiesWithoutUndo();
            });

            // 작은 적: 빠르고 약하게, 물감은 조금(15), 종이조각 없음
            Edit(MiniPath, root =>
            {
                SetAI(root, 6.5f, 11f, 2.8f, 0.35f, 0.35f, 0.6f, 8, 0.9f);
                var drop = new SerializedObject(root.GetComponent<EnemyDrop>());
                drop.FindProperty("paperScraps").intValue = 0;
                drop.FindProperty("paintAmount").intValue = 15;
                drop.ApplyModifiedPropertiesWithoutUndo();
            });
            AssetDatabase.SaveAssets();

            foreach (var (path, count) in Scenes)
            {
                var scene = EditorSceneManager.OpenScene(path);
                int placed = PlaceSpawners(splitter, count);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[LastBullet] {scene.name}: 분열 적 스포너 {placed}개를 놓았어요.");
            }
        }

        // 돌진형 프리팹을 복사해서 크기·머리·NavMeshAgent를 맞춘다
        static GameObject BuildEnemy(string path, string name, float scale, float headScale, float radius, float height)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                AssetDatabase.CopyAsset(ChargerPath, path);

            // 전용 머티리얼: 돌진형의 것을 복사 (이미 있으면 사용자가 고친 것이므로 그대로 둔다)
            string materialPath = $"Assets/Materials/{name}.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(materialPath) == null)
                AssetDatabase.CopyAsset("Assets/Materials/Enemy_Charger.mat", materialPath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

            var root = PrefabUtility.LoadPrefabContents(path);
            root.name = name;
            // 크기: 맨 위 오브젝트는 1로 두고 모양(Model)과 충돌(Body·Head)만 키운다 (NavMeshAgent 크기는 따로)
            root.transform.Find("Model").localScale = Vector3.one * 1.25f * scale;
            var body = root.transform.Find("Body");
            body.localPosition = new Vector3(0f, 0.9f * scale, 0f);
            body.localScale = new Vector3(0.88f, 1.81f, 0.56f) * scale;
            var head = root.transform.Find("Head");
            head.localPosition = new Vector3(0f, 1.95f * scale, -0.03f * scale);
            head.localScale = Vector3.one * 0.75f * scale * headScale;
            foreach (var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>())
                renderer.sharedMaterial = material;

            var agent = root.GetComponent<NavMeshAgent>();
            agent.radius = radius;
            agent.height = height;

            // 맞는 머리 자리를 어긋나지 않게: 머리 충돌은 위에서 정한 위치(모델 머리 높이에 비례)
            var burst = new SerializedObject(root.GetComponent<EnemyDrop>());
            burst.FindProperty("burstPoint").objectReferenceValue = head;
            burst.ApplyModifiedPropertiesWithoutUndo();

            var saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
            return saved;
        }

        static void Edit(string path, System.Action<GameObject> change)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            change(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        static void SetAI(GameObject root, float chase, float dash, float attackRange, float windup, float dashTime, float recover, int damage, float contact)
        {
            var so = new SerializedObject(root.GetComponent<EnemyAI>());
            so.FindProperty("chaseSpeed").floatValue = chase;
            so.FindProperty("dashSpeed").floatValue = dash;
            so.FindProperty("attackRange").floatValue = attackRange;
            so.FindProperty("windupTime").floatValue = windup;
            so.FindProperty("dashTime").floatValue = dashTime;
            so.FindProperty("recoverTime").floatValue = recover;
            so.FindProperty("attackDamage").intValue = damage;
            so.FindProperty("contactRange").floatValue = contact;
            so.ApplyModifiedPropertiesWithoutUndo();
            root.GetComponent<NavMeshAgent>().speed = chase;
        }

        // 돌진형 스포너 하나를 복사해, 다른 스포너에서 6m 이상 떨어진 걸을 수 있는 자리에 놓는다
        static int PlaceSpawners(GameObject splitter, int count)
        {
            var all = new List<EnemySpawner>(Object.FindObjectsByType<EnemySpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            foreach (var old in all.FindAll(s => s.name.StartsWith("Spawner_Splitter_")))
                Object.DestroyImmediate(old.gameObject);
            all.RemoveAll(s => s == null);

            var template = all.Find(s => s.name.StartsWith("Spawner_Charger_"));
            if (template == null)
                return 0;
            var taken = all.ConvertAll(s => s.transform.position);

            Random.InitState(4646);
            int placed = 0;
            for (int attempt = 0; attempt < 400 && placed < count; attempt++)
            {
                var near = all[Random.Range(0, all.Count)].transform.position;
                var circle = Random.insideUnitCircle.normalized * Random.Range(8f, 16f);
                var candidate = near + new Vector3(circle.x, 0f, circle.y);
                if (!NavMesh.SamplePosition(candidate, out var hit, 4f, NavMesh.AllAreas))
                    continue;
                if (taken.Exists(p => (p - hit.position).sqrMagnitude < 36f))
                    continue;

                var copy = Object.Instantiate(template.gameObject, template.transform.parent);
                placed++;
                copy.name = $"Spawner_Splitter_{placed:00}";
                copy.transform.position = hit.position;
                var so = new SerializedObject(copy.GetComponent<EnemySpawner>());
                so.FindProperty("enemyPrefab").objectReferenceValue = splitter;
                so.ApplyModifiedPropertiesWithoutUndo();
                taken.Add(hit.position);
            }
            return placed;
        }
    }
}
