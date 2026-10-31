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
    // 사막 씬(4_Desert)을 만든다 (2026-10-31 사용자 요청: 에셋 Low-Poly Desert Environment Pack으로 세 번째 맵).
    //  - 2_Forest를 복사해서 시작한다 (플레이어·카메라·HUD·전투 가방·일시정지 창이 이미 들어 있다).
    //  - 지도: 120m × 120m 모래 땅, 둘레는 절벽, 바위·선인장·고목이 흩어져 있다. 시작은 남쪽(0, -48).
    //  - 물감 색은 맵 전체가 핑크·하양·검정 (StageColors 표). 지역 구분(PaintZone)은 없다.
    //  - 적은 스포너 20개 (돌진 8 / 방패 6 / 원거리 6), 처치 60초 뒤 다시 나온다.
    // 에셋 프리팹에는 이미 충돌(Collider)이 들어 있다.
    // 메뉴: LastBullet > Phase 13 사막 씬 만들기
    public static class Phase13DesertSetup
    {
        const string ForestPath = "Assets/Scenes/2_Forest.unity";
        const string DesertPath = "Assets/Scenes/4_Desert.unity";
        const string Root = "Assets/Tiny Teacup Studio/Low Poly Desert Environment/";
        const string RpgRoot = "Assets/RPGPP_LT/Prefabs/";

        static readonly Vector2 PlayerStart = new Vector2(0f, -48f);
        static readonly List<Vector3> Taken = new List<Vector3>(); // 큰 물건 자리 (x, z, 반지름)

        [MenuItem("LastBullet/Phase 13 사막 씬 만들기")]
        static void Create()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(DesertPath) != null)
            {
                EditorUtility.DisplayDialog("LastBullet", "이미 4_Desert 씬이 있어요.", "확인");
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/Rock_01.prefab") == null)
            {
                EditorUtility.DisplayDialog("LastBullet", "에셋 Low-Poly Desert Environment Pack을 먼저 가져오세요.", "확인");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            ConvertMaterial();

            AssetDatabase.CopyAsset(ForestPath, DesertPath);
            var scene = EditorSceneManager.OpenScene(DesertPath);
            Lightmapping.lightingDataAsset = null; // 숲에서 구운 빛 데이터는 이 씬과 맞지 않는다
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.78f, 0.74f, 0.68f);
            RenderSettings.ambientEquatorColor = new Color(0.72f, 0.62f, 0.50f);
            RenderSettings.ambientGroundColor = new Color(0.46f, 0.37f, 0.28f);
            RenderSettings.ambientIntensity = 1f;
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (light.type == LightType.Directional)
                {
                    light.color = new Color(1f, 0.93f, 0.80f); // 뜨거운 낮 햇빛
                    light.intensity = 1.25f;
                }
            }
            Taken.Clear();

            // 1. 숲의 환경·적 치우기
            Object.DestroyImmediate(GameObject.Find("Environment"));
            Object.DestroyImmediate(GameObject.Find("Spawners"));
            var enemiesRoot = GameObject.Find("Enemies").transform;
            foreach (var child in enemiesRoot.Cast<Transform>().ToArray())
                Object.DestroyImmediate(child.gameObject);

            var random = new Random(20261031);
            var environment = new GameObject("Environment").transform;
            var ground = Group("Ground", environment);
            var cliffs = Group("Cliffs", environment);
            var rocks = Group("Rocks", environment);
            var plants = Group("Plants", environment);
            var small = Group("SmallDecor", environment);
            var backdrop = Group("Backdrop", environment);
            var walls = Group("InvisibleWalls", environment);

            BuildGround(ground);
            BuildCliffs(cliffs);
            BuildRocks(rocks, random);
            BuildPlants(plants, random);
            BuildSmallDecor(small, random);
            BuildBackdrop(backdrop, random);
            BuildInvisibleWalls(walls);
            Ignore(cliffs);
            Ignore(small);
            Ignore(backdrop);

            // 2. 플레이어 시작 위치
            var player = GameObject.Find("Player");
            var controller = player.GetComponent<CharacterController>();
            controller.enabled = false;
            player.transform.position = new Vector3(PlayerStart.x, player.transform.position.y, PlayerStart.y);
            player.transform.rotation = Quaternion.identity;
            controller.enabled = true;

            // 3. NavMesh: 이 씬 전용으로 새로 굽는다 (숲 것을 덮어쓰지 않게 먼저 비운다)
            var surface = GameObject.Find("NavMesh").GetComponent<NavMeshSurface>();
            surface.navMeshData = null;
            surface.BuildNavMesh();
            if (!AssetDatabase.IsValidFolder("Assets/Scenes/4_Desert"))
                AssetDatabase.CreateFolder("Assets/Scenes", "4_Desert");
            AssetDatabase.CreateAsset(surface.navMeshData, "Assets/Scenes/4_Desert/NavMesh-4_Desert.asset");

            // 4. 적 스포너 (물감 색은 StageColors 표가 정하므로 PaintZone은 없다)
            var spawnersRoot = new GameObject("Spawners").transform;
            var prefabs = new Dictionary<string, GameObject>
            {
                ["Charger"] = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_Charger.prefab"),
                ["Shield"] = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_Shield.prefab"),
                ["Thrower"] = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_Thrower.prefab"),
            };
            var area = new Rect(-50f, -14f, 100f, 66f);
            var taken = new List<Vector2>();
            var counters = new Dictionary<string, int> { ["Charger"] = 0, ["Shield"] = 0, ["Thrower"] = 0 };
            AddSpawners("Charger", 8, area, spawnersRoot, enemiesRoot, prefabs, taken, counters, random);
            AddSpawners("Shield", 6, area, spawnersRoot, enemiesRoot, prefabs, taken, counters, random);
            AddSpawners("Thrower", 6, area, spawnersRoot, enemiesRoot, prefabs, taken, counters, random);

            // 5. 빌드 목록
            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(s => s.path == DesertPath))
                scenes.Add(new EditorBuildSettingsScene(DesertPath, true));
            EditorBuildSettings.scenes = scenes.ToArray();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[LastBullet] Phase 13: 4_Desert 씬을 만들었어요 (스포너 {spawnersRoot.GetComponentsInChildren<EnemySpawner>().Length}개).");
        }

        // 에셋 머티리얼(Standard)을 URP Lit으로 바꾼다. 색 팔레트 그림(Palette.png)은 그대로 쓴다.
        static void ConvertMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/Mat_01.mat");
            if (material == null || material.shader.name.StartsWith("Universal"))
                return;
            var texture = material.GetTexture("_MainTex");
            var color = material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
            material.shader = Shader.Find("Universal Render Pipeline/Lit");
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0f);
            material.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(material);
        }

        // ───────────── 땅, 절벽 ─────────────

        static void BuildGround(Transform parent)
        {
            var sand = ArtSlots.GetOrCreateMaterial("DesertFloor", new Color32(0xE2, 0xB8, 0x72, 0xFF));
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.SetParent(parent, false);
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(13f, 1f, 13f); // 130m × 130m (충돌 있음)
            floor.GetComponent<Renderer>().sharedMaterial = sand;
        }

        // 지도 둘레의 절벽. 절벽의 앞면(깎인 면)이 안쪽을 보게 돌려 놓는다.
        static void BuildCliffs(Transform parent)
        {
            const float scale = 3f, edge = 66f, step = 15f;
            var cliff = Load("Cliff_01");
            for (int i = -4; i <= 4; i++)
            {
                float t = i * step;
                Spawn(cliff, parent, new Vector2(t, edge), 180f, scale, 0.1f);   // 북
                Spawn(cliff, parent, new Vector2(t, -edge), 0f, scale, 0.1f);    // 남
                Spawn(cliff, parent, new Vector2(edge, t), -90f, scale, 0.1f);   // 동 (앞면이 안쪽 -X를 보게)
                Spawn(cliff, parent, new Vector2(-edge, t), 90f, scale, 0.1f);   // 서 (앞면이 안쪽 +X를 보게)
            }
            // 모서리 전용 조각(CliffCorner)은 방향 맞추기가 어려워 쓰지 않는다. 네 변의 절벽이 모서리에서 서로 겹쳐 덮는다.
        }

        // ───────────── 바위, 식물, 작은 장식 ─────────────

        static void BuildRocks(Transform parent, Random random)
        {
            string[] kinds = { "Rock_01", "Rock_02", "Rock_03" };
            int placed = 0, tries = 0;
            while (placed < 26 && tries++ < 800)
            {
                var p = RandomPoint(random, 54f);
                if (!IsFree(p, 5f) || Vector2.Distance(p, PlayerStart) < 10f)
                    continue;
                float scale = 2f + (float)random.NextDouble() * 1.5f;
                Spawn(Load(kinds[random.Next(kinds.Length)]), parent, p, (float)random.NextDouble() * 360f, scale, 0.1f);
                Taken.Add(new Vector3(p.x, p.y, 2.2f * scale / 2f + 0.5f));
                placed++;
            }
        }

        static void BuildPlants(Transform parent, Random random)
        {
            string[] kinds = { "Cactus_01", "Cactus_02", "Cactus_03", "Tree_01" };
            int placed = 0, tries = 0;
            while (placed < 46 && tries++ < 1000)
            {
                var p = RandomPoint(random, 56f);
                if (!IsFree(p, 2f) || Vector2.Distance(p, PlayerStart) < 6f)
                    continue;
                string kind = kinds[random.Next(kinds.Length)];
                float scale = kind == "Tree_01" ? 2.2f : 2f + (float)random.NextDouble() * 1.2f;
                Spawn(Load(kind), parent, p, (float)random.NextDouble() * 360f, scale, 0.1f);
                Taken.Add(new Vector3(p.x, p.y, 1.8f));
                placed++;
            }
        }

        // 작은 돌멩이: 걷는 데 방해되지 않게 충돌을 없앤다 (길 찾기에도 안 쓴다)
        static void BuildSmallDecor(Transform parent, Random random)
        {
            string[] kinds = { "Rock_04", "Rock_05" };
            int placed = 0, tries = 0;
            while (placed < 70 && tries++ < 400)
            {
                var p = RandomPoint(random, 58f);
                if (!IsFree(p, 1f))
                    continue;
                var go = Spawn(Load(kinds[random.Next(kinds.Length)]), parent, p, (float)random.NextDouble() * 360f, 2.5f + (float)random.NextDouble() * 2f, 0.02f);
                foreach (var collider in go.GetComponentsInChildren<Collider>())
                    Object.DestroyImmediate(collider);
                placed++;
            }
        }

        static Vector2 RandomPoint(Random random, float half)
        {
            return new Vector2(Mathf.Lerp(-half, half, (float)random.NextDouble()), Mathf.Lerp(-half, half, (float)random.NextDouble()));
        }

        // ───────────── 배경, 벽 ─────────────

        // 바깥 풍경: 하늘·구름은 숲에서 쓴 RPG Poly Pack 것을 다시 쓴다
        static void BuildBackdrop(Transform parent, Random random)
        {
            var farFloor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            farFloor.name = "FloorFar";
            Object.DestroyImmediate(farFloor.GetComponent<Collider>());
            farFloor.transform.SetParent(parent, false);
            farFloor.transform.position = new Vector3(0f, -0.03f, 0f);
            farFloor.transform.localScale = new Vector3(80f, 1f, 80f);
            farFloor.GetComponent<Renderer>().sharedMaterial = ArtSlots.GetOrCreateMaterial("DesertFloor", new Color32(0xE2, 0xB8, 0x72, 0xFF));

            var sky = Spawn(LoadRpg("Nature/Sky/rpgpp_lt_sky_01"), parent, Vector2.zero, 0f, 10f, 0f);
            sky.name = "SkyDome";
            sky.transform.position = new Vector3(0f, -69f, 0f);
            var clouds = new[] { LoadRpg("Nature/Sky/rpgpp_lt_cloud_01"), LoadRpg("Nature/Sky/rpgpp_lt_cloud_02") };
            for (int i = 0; i < 10; i++)
            {
                float angle = i / 10f * 360f + (float)random.NextDouble() * 20f;
                var cloud = Spawn(clouds[i % 2], parent, Quaternion.Euler(0f, angle, 0f) * Vector3.forward * (270f + (float)random.NextDouble() * 60f), (float)random.NextDouble() * 360f, 1.5f, 0f);
                cloud.transform.position = new Vector3(cloud.transform.position.x, 80f + (float)random.NextDouble() * 90f, cloud.transform.position.z);
            }
        }

        static void BuildInvisibleWalls(Transform parent)
        {
            Wall(parent, "Wall_North", new Vector3(0f, 5f, 60f), new Vector3(130f, 10f, 2f));
            Wall(parent, "Wall_South", new Vector3(0f, 5f, -60f), new Vector3(130f, 10f, 2f));
            Wall(parent, "Wall_East", new Vector3(60f, 5f, 0f), new Vector3(2f, 10f, 130f));
            Wall(parent, "Wall_West", new Vector3(-60f, 5f, 0f), new Vector3(2f, 10f, 130f));
        }

        static void Wall(Transform parent, string name, Vector3 position, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.AddComponent<BoxCollider>().size = size;
        }

        // ───────────── 적 스포너 ─────────────

        static void AddSpawners(string kind, int amount, Rect area, Transform parent, Transform enemiesRoot,
            Dictionary<string, GameObject> prefabs, List<Vector2> taken, Dictionary<string, int> counters, Random random)
        {
            for (int i = 0; i < amount; i++)
            {
                if (!TryFindSpot(area, taken, random, out Vector3 position))
                {
                    Debug.LogWarning($"[LastBullet] 사막 {kind}: 알맞은 자리를 못 찾았어요");
                    continue;
                }
                taken.Add(new Vector2(position.x, position.z));

                var spawner = new GameObject($"Spawner_{kind}_{++counters[kind]:00}");
                spawner.transform.SetParent(parent, false);
                Vector3 toPlayer = new Vector3(PlayerStart.x - position.x, 0f, PlayerStart.y - position.z);
                spawner.transform.SetPositionAndRotation(position, Quaternion.LookRotation(toPlayer));
                var script = spawner.AddComponent<EnemySpawner>();
                var so = new SerializedObject(script);
                so.FindProperty("enemyPrefab").objectReferenceValue = prefabs[kind];
                so.FindProperty("enemiesParent").objectReferenceValue = enemiesRoot;
                so.FindProperty("faceTowardsPlayer").boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // 무작위로 골라, 걸어갈 수 있고(NavMesh), 시작에서 멀고, 다른 스포너와 떨어진 곳을 찾는다
        static bool TryFindSpot(Rect area, List<Vector2> taken, Random random, out Vector3 position)
        {
            for (int attempt = 0; attempt < 600; attempt++)
            {
                var candidate = new Vector2(
                    Mathf.Lerp(area.xMin, area.xMax, (float)random.NextDouble()),
                    Mathf.Lerp(area.yMin, area.yMax, (float)random.NextDouble()));
                if (Vector2.Distance(candidate, PlayerStart) < 28f)
                    continue;
                if (taken.Any(t => Vector2.Distance(candidate, t) < 9f))
                    continue;
                if (!NavMesh.SamplePosition(new Vector3(candidate.x, 0f, candidate.y), out var hit, 1f, NavMesh.AllAreas))
                    continue;
                position = hit.position;
                return true;
            }
            position = default;
            return false;
        }

        // ───────────── 도우미 ─────────────

        static Transform Group(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        static GameObject Load(string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}Prefabs/{name}.prefab");
            if (prefab == null)
                Debug.LogError($"[LastBullet] 사막 에셋 프리팹을 못 찾음: {name}");
            return prefab;
        }

        static GameObject LoadRpg(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{RpgRoot}{path}.prefab");
            if (prefab == null)
                Debug.LogError($"[LastBullet] RPG 프리팹을 못 찾음: {path}");
            return prefab;
        }

        // 모양(Renderer)의 가운데가 center(X,Z)에 오고, 맨 아래가 땅(0)보다 embed만큼 아래에 오게 놓는다.
        static GameObject Spawn(GameObject prefab, Transform parent, Vector2 center, float yaw, float scale, float embed)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one * scale;
            go.transform.position = Vector3.zero;
            var bounds = BoundsOf(go);
            go.transform.position = new Vector3(center.x - bounds.center.x, -embed - bounds.min.y, center.y - bounds.center.z);
            return go;
        }

        static GameObject Spawn(GameObject prefab, Transform parent, Vector3 position, float yaw, float scale, float embed)
        {
            return Spawn(prefab, parent, new Vector2(position.x, position.z), yaw, scale, embed);
        }

        static Bounds BoundsOf(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
                bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        static bool IsFree(Vector2 p, float radius)
        {
            foreach (var taken in Taken)
            {
                if (Vector2.Distance(p, new Vector2(taken.x, taken.y)) < taken.z + radius)
                    return false;
            }
            return true;
        }

        static void Ignore(Transform group)
        {
            var modifier = group.gameObject.AddComponent<NavMeshModifier>();
            modifier.ignoreFromBuild = true;
            modifier.applyToChildren = true;
        }
    }
}
