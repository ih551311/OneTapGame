using System.Linq;
using LastBullet.Enemy;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // Phase 10: 방어형·원거리형 적과 던지는 돌(투사체) 프리팹을 만들고, 숲의 적 7마리를 기획서 구성으로 바꾼다.
    //   숲 = 돌진형 3 + 원거리형 2 + 방어형 2 (기획서 2.6)
    // 두 적은 돌진형(Enemy_Charger) 프리팹을 복사해서 시작한다 (EnemyHealth·Hitbox·EnemyDrop·NavMeshAgent·Enemy 태그가 이미 들어 있다).
    // 모양은 임시(상자·공)이고, 적마다 전용 머티리얼을 쓴다 (나중에 3ds Max 모델로 교체).
    // 메뉴: LastBullet > Phase 10 방어형·원거리형 적 만들기
    public static class Phase10EnemiesSetup
    {
        const string ChargerPath = "Assets/Prefabs/Enemy_Charger.prefab";
        const string ShieldPath = "Assets/Prefabs/Enemy_Shield.prefab";
        const string ThrowerPath = "Assets/Prefabs/Enemy_Thrower.prefab";
        const string ProjectilePath = "Assets/Prefabs/EnemyProjectile.prefab";
        const string ForestPath = "Assets/Scenes/2_Forest.unity";

        [MenuItem("LastBullet/Phase 10 방어형·원거리형 적 만들기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var projectile = CreateProjectile();
            var shield = CreateShieldEnemy();
            var thrower = CreateThrowerEnemy(projectile);
            AssetDatabase.SaveAssets();

            PlaceInForest(shield, thrower);
            Debug.Log("[LastBullet] Phase 10: 방어형·원거리형 적과 투사체 프리팹을 만들고 숲의 적을 돌진형 3 + 원거리형 2 + 방어형 2로 바꿨어요.");
        }

        // ───────────── 투사체 ─────────────

        static EnemyProjectile CreateProjectile()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePath);
            if (existing != null)
                return existing.GetComponent<EnemyProjectile>();

            var root = new GameObject("EnemyProjectile");
            root.AddComponent<EnemyProjectile>();
            var model = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            model.name = "Model";
            Object.DestroyImmediate(model.GetComponent<Collider>()); // 충돌은 EnemyProjectile이 SphereCast로 본다
            model.transform.SetParent(root.transform, false);
            model.transform.localScale = Vector3.one * 0.5f;
            model.GetComponent<Renderer>().sharedMaterial = ArtSlots.GetOrCreateMaterial("EnemyProjectile", new Color32(0x5A, 0x55, 0x50, 0xFF));
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, ProjectilePath);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<EnemyProjectile>();
        }

        // ───────────── 방어형 ─────────────

        static GameObject CreateShieldEnemy()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ShieldPath) != null)
                return AssetDatabase.LoadAssetAtPath<GameObject>(ShieldPath);

            AssetDatabase.CopyAsset(ChargerPath, ShieldPath);
            var root = PrefabUtility.LoadPrefabContents(ShieldPath);
            root.name = "Enemy_Shield";

            // 전용 머티리얼: 푸르스름한 회색 돌
            var bodyMaterial = ArtSlots.GetOrCreateMaterial("Enemy_Shield", new Color32(0x6E, 0x78, 0x8C, 0xFF));
            foreach (var renderer in root.GetComponentsInChildren<Renderer>())
                renderer.sharedMaterial = bodyMaterial;

            // 기획서 2.5: 3m/s, 공격 20, 회전 느림. 공격은 방패 돌진이라 돌진형보다 느리고 묵직하다.
            var ai = new SerializedObject(root.GetComponent<EnemyAI>());
            ai.FindProperty("chaseSpeed").floatValue = 3f;
            ai.FindProperty("attackRange").floatValue = 3.5f;
            ai.FindProperty("windupTime").floatValue = 0.6f;
            ai.FindProperty("dashSpeed").floatValue = 7f;
            ai.FindProperty("dashTime").floatValue = 0.5f;
            ai.FindProperty("recoverTime").floatValue = 1.0f;
            ai.FindProperty("attackDamage").intValue = 20;
            ai.FindProperty("turnSpeed").floatValue = 55f; // 천천히 돌아서 옆·뒤를 노릴 틈이 생긴다
            ai.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, ShieldPath);
            PrefabUtility.UnloadPrefabContents(root);
            return prefab;
        }

        // ───────────── 원거리형 ─────────────

        static GameObject CreateThrowerEnemy(EnemyProjectile projectile)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ThrowerPath) != null)
                return AssetDatabase.LoadAssetAtPath<GameObject>(ThrowerPath);

            AssetDatabase.CopyAsset(ChargerPath, ThrowerPath);
            var root = PrefabUtility.LoadPrefabContents(ThrowerPath);
            root.name = "Enemy_Thrower";

            // 소형 조각: 돌진형보다 작고 가볍다. 전용 머티리얼: 따뜻한 갈색 돌
            var material = ArtSlots.GetOrCreateMaterial("Enemy_Thrower", new Color32(0x8A, 0x7A, 0x66, 0xFF));
            foreach (var renderer in root.GetComponentsInChildren<Renderer>())
                renderer.sharedMaterial = material;
            var body = root.transform.Find("Body");
            body.localPosition = new Vector3(0f, 0.65f, 0f);
            body.localScale = new Vector3(0.8f, 1.3f, 0.65f);
            var head = root.transform.Find("Head");
            head.localPosition = new Vector3(0f, 1.65f, 0f);
            head.localScale = Vector3.one * 0.8f;

            var agent = root.GetComponent<UnityEngine.AI.NavMeshAgent>();
            agent.radius = 0.5f;
            agent.height = 2.1f;

            // 돌이 나가는 자리 (가슴 앞)
            var throwPoint = new GameObject("ThrowPoint").transform;
            throwPoint.SetParent(root.transform, false);
            throwPoint.localPosition = new Vector3(0f, 1.3f, 0.5f);

            // 돌진형 AI 대신 원거리형 AI
            Object.DestroyImmediate(root.GetComponent<EnemyAI>());
            var ai = root.AddComponent<RangedEnemyAI>();
            var so = new SerializedObject(ai);
            so.FindProperty("projectilePrefab").objectReferenceValue = projectile;
            so.FindProperty("throwPoint").objectReferenceValue = throwPoint;
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, ThrowerPath);
            PrefabUtility.UnloadPrefabContents(root);
            return prefab;
        }

        // ───────────── 숲에 배치 ─────────────

        // 기존 7마리의 자리는 그대로 두고 종류만 바꾼다. 돌진형은 시작 쪽에 가까운 3마리, 나머지는 안쪽에 둔다.
        static void PlaceInForest(GameObject shield, GameObject thrower)
        {
            var scene = EditorSceneManager.OpenScene(ForestPath);
            var enemies = GameObject.Find("Enemies").transform;

            // (바꿀 적 번호, 새 프리팹, 새 이름)
            var replacements = new (int number, GameObject prefab, string name)[]
            {
                (4, shield, "Enemy_Shield_1"), (7, shield, "Enemy_Shield_2"),
                (5, thrower, "Enemy_Thrower_1"), (6, thrower, "Enemy_Thrower_2"),
            };
            foreach (var (number, prefab, name) in replacements)
            {
                var old = enemies.Find($"Enemy_Charger_{number}");
                if (old == null)
                    continue;
                var replacement = (GameObject)PrefabUtility.InstantiatePrefab(prefab, enemies);
                replacement.name = name;
                replacement.transform.SetPositionAndRotation(old.position, old.rotation);
                Object.DestroyImmediate(old.gameObject);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[LastBullet] 숲의 적: {string.Join(", ", enemies.Cast<Transform>().Select(t => t.name))}");
        }
    }
}
