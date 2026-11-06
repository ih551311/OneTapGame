using System.IO;
using LastBullet.Enemy;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastBullet.EditorTools
{
    // 보스를 만든다 (2026-11-06 사용자 결정): 돌진형 거대 버전 / 3맵(숲·성 마을·사막) 모두 / 맵 한가운데에 처음부터 / 잡으면 물감을 많이 준다.
    //   프리팹 Assets/Prefabs/Enemy_Boss.prefab: 돌진병(Enemy_Charger)을 복사해 2.5배로 키우고 체력 800, 돌진을 느리지만 세게(피해 30),
    //     BossSlam(가끔 땅을 내려쳐 충격파 — 점프로 피한다), BossReward(맵 물감 3색 × 150 + 종이 5)를 붙였다. 일반 적의 보상(EnemyDrop)은 0.
    //   그림 칸 (이미 있으면 덮어쓰지 않는다): Assets/Textures/BossSlamRing.png(경고 원), 머티리얼 Assets/Materials/BossSlamRing.mat(투명 Unlit).
    //   씬 3개: Spawners 아래 Spawner_Boss_01 (맵 한가운데, NavMesh 위로 맞춤). 보스가 죽으면 240초 뒤 플레이어가 40m 밖일 때 다시 나온다.
    // 이미 있으면 지우고 다시 만든다. 메뉴: LastBullet > Phase 86 보스 만들기
    public static class Phase86BossSetup
    {
        const string PrefabPath = "Assets/Prefabs/Enemy_Boss.prefab";
        const string RingPng = "Assets/Textures/BossSlamRing.png";
        const string RingMat = "Assets/Materials/BossSlamRing.mat";

        // 맵 한가운데 (NavMesh 가장 가까운 곳으로 맞춘다)
        static readonly (string path, Vector3 center)[] Maps =
        {
            ("Assets/Scenes/2_Forest.unity", new Vector3(0f, 0f, 0f)),
            ("Assets/Scenes/3_CastleVillage.unity", new Vector3(0f, 0f, 22f)),
            ("Assets/Scenes/4_Desert.unity", new Vector3(0f, 0f, 0f)),
        };

        [MenuItem("LastBullet/Phase 86 보스 만들기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            MakeRingPng();
            AssetDatabase.Refresh();
            var material = GetOrCreateRingMaterial();
            var prefab = BuildPrefab(material);

            foreach (var (path, center) in Maps)
            {
                var scene = EditorSceneManager.OpenScene(path);
                var spawners = GameObject.Find("Spawners");
                var enemies = GameObject.Find("Enemies");
                if (spawners == null)
                {
                    Debug.LogWarning($"[LastBullet] {scene.name}: Spawners가 없어 건너뜀");
                    continue;
                }
                var old = spawners.transform.Find("Spawner_Boss_01");
                if (old != null)
                    Object.DestroyImmediate(old.gameObject);

                var go = new GameObject("Spawner_Boss_01");
                go.transform.SetParent(spawners.transform, false);
                Vector3 position = center;
                if (UnityEngine.AI.NavMesh.SamplePosition(center, out var hit, 30f, UnityEngine.AI.NavMesh.AllAreas))
                    position = hit.position;
                else
                    Debug.LogWarning($"[LastBullet] {scene.name}: 한가운데 근처에 NavMesh가 없어요 — 보스 자리를 직접 옮겨 주세요.");
                go.transform.position = position;

                var spawner = go.AddComponent<EnemySpawner>();
                var so = new SerializedObject(spawner);
                so.FindProperty("enemyPrefab").objectReferenceValue = prefab;
                so.FindProperty("enemiesParent").objectReferenceValue = enemies != null ? enemies.transform : null;
                so.FindProperty("respawn").boolValue = true;
                so.FindProperty("respawnDelay").floatValue = 240f;
                so.FindProperty("respawnSafeDistance").floatValue = 40f;
                so.ApplyModifiedPropertiesWithoutUndo();

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[LastBullet] {scene.name}: 보스 스포너를 {position}에 만들었어요 (한가운데 {center}에서 {Vector3.Distance(center, position):0.#}m).");
            }
            AssetDatabase.SaveAssets();
        }

        static GameObject BuildPrefab(Material ringMaterial)
        {
            AssetDatabase.DeleteAsset(PrefabPath);
            AssetDatabase.CopyAsset("Assets/Prefabs/Enemy_Charger.prefab", PrefabPath);
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            root.name = "Enemy_Boss";
            root.transform.localScale = Vector3.one * 2.5f;

            Set(root.GetComponent<EnemyHealth>(), ("displayName", "거대 돌진병"), ("maxHealth", 800f));
            Set(root.GetComponent<EnemyAI>(),
                ("detectRange", 28f), ("chaseSpeed", 4.5f), ("attackRange", 9f), ("windupTime", 0.9f), ("leanAngle", 25f),
                ("dashSpeed", 13f), ("dashTime", 0.7f), ("recoverTime", 1.4f), ("attackDamage", 30), ("contactRange", 3.4f),
                ("turnSpeed", 120f), ("hitStunTime", 0f));
            Set(root.GetComponent<EnemyDrop>(), ("paperScraps", 0), ("paintAmount", 0));

            // 경고 원 (바닥에 눕히는 쿼드)
            var ring = GameObject.CreatePrimitive(PrimitiveType.Quad);
            ring.name = "SlamRing";
            Object.DestroyImmediate(ring.GetComponent<Collider>());
            ring.transform.SetParent(root.transform, false);
            ring.tag = "Enemy";
            var renderer = ring.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = ringMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            var slam = root.AddComponent<BossSlam>();
            Set(slam, ("ring", ring.transform));
            root.AddComponent<BossReward>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            return prefab;
        }

        // 컴포넌트의 (이름, 값) 칸을 한꺼번에 채운다
        static void Set(Component component, params (string name, object value)[] values)
        {
            var so = new SerializedObject(component);
            foreach (var (name, value) in values)
            {
                var p = so.FindProperty(name);
                if (p == null)
                {
                    Debug.LogWarning($"[LastBullet] {component.GetType().Name}에 {name} 칸이 없어요.");
                    continue;
                }
                switch (value)
                {
                    case int i: p.intValue = i; break;
                    case float f: p.floatValue = f; break;
                    case string s: p.stringValue = s; break;
                    case Object o: p.objectReferenceValue = o; break;
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static Material GetOrCreateRingMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(RingMat);
            if (existing != null)
                return existing;
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetFloat("_Surface", 1f); // 투명
            material.SetFloat("_Blend", 0f);   // 알파 섞기
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(RingPng));
            material.SetColor("_BaseColor", new Color(1f, 0.15f, 0.1f, 0.5f));
            AssetDatabase.CreateAsset(material, RingMat);
            return material;
        }

        // 붉은 경고 원: 안쪽은 옅게 채우고 가장자리는 진한 테두리
        static void MakeRingPng()
        {
            if (File.Exists(RingPng))
                return;
            const int N = 256;
            var texture = new Texture2D(N, N, TextureFormat.RGBA32, false);
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float dx = (x + 0.5f) / N * 2f - 1f, dy = (y + 0.5f) / N * 2f - 1f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float fill = r < 0.96f ? 0.35f : 0f;
                    float edge = Mathf.Clamp01(1f - Mathf.Abs(r - 0.93f) / 0.05f);
                    float a = Mathf.Max(fill, edge);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            texture.Apply();
            Directory.CreateDirectory("Assets/Textures");
            File.WriteAllBytes(RingPng, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(RingPng);
            var importer = (TextureImporter)AssetImporter.GetAtPath(RingPng);
            if (importer != null)
            {
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
        }
    }
}
