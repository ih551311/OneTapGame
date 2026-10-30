using System.Collections.Generic;
using System.Linq;
using LastBullet.Stage;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Random = System.Random;

namespace LastBullet.EditorTools
{
    // 숲 스테이지 씬(2_Forest)을 만든다 (훈련장 없이 바로 숲, 2026-10-30 사용자 요청).
    //  - 2_Training을 복사해서 시작한다 (플레이어·카메라·HUD·전투 가방·일시정지 창이 이미 들어 있다).
    //  - 땅: 에셋(RPG Poly Pack - Lite)의 풀 타일 6×6 + 아래에 평평한 바닥 / 길 / 나무·바위·덤불·꽃 / 작은 마을 / 야영지
    //  - 가장자리: 나무 두 겹 + 보이지 않는 벽 → 플레이어가 밖으로 못 나간다. 바깥은 언덕·산·하늘·구름 배경.
    //  - 적: 돌진형 7마리를 NavMesh(적이 걷는 길) 위에 배치한다.
    // 에셋 프리팹에는 충돌(Collider)이 없어서, 나무·바위·건물에 충돌을 따로 붙인다 (총알이 맞고, 사람이 통과 못 하게).
    // 메뉴: LastBullet > Phase 10 숲 씬 만들기
    public static class Phase10ForestSetup
    {
        const string TrainingPath = "Assets/Scenes/2_Training.unity";
        const string ForestPath = "Assets/Scenes/2_Forest.unity";
        const string Root = "Assets/RPGPP_LT/Prefabs/";
        const int EnemyCount = 7;

        // 이미 놓은 큰 물건의 자리 (x, z, 반지름). 서로 겹치지 않게 한다.
        static readonly List<Vector3> Taken = new List<Vector3>();

        static readonly Vector3 PlayerStart = new Vector3(0f, 0f, -44f);

        static readonly Vector2[] EnemySpots =
        {
            new Vector2(-16f, -14f), new Vector2(15f, -10f), new Vector2(-24f, 8f), new Vector2(2f, 6f),
            new Vector2(22f, 10f), new Vector2(-10f, 30f), new Vector2(14f, 38f),
        };

        [MenuItem("LastBullet/Phase 10 숲 씬 만들기")]
        static void Create()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ForestPath) != null)
            {
                EditorUtility.DisplayDialog("LastBullet", "이미 2_Forest 씬이 있어요.", "확인");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            AssetDatabase.CopyAsset(TrainingPath, ForestPath);
            var scene = EditorSceneManager.OpenScene(ForestPath);
            Taken.Clear();

            // 1. 훈련장의 땅과 적 치우기
            Object.DestroyImmediate(GameObject.Find("Ground"));
            var enemiesRoot = GameObject.Find("Enemies");
            foreach (var child in enemiesRoot.transform.Cast<Transform>().ToArray())
                Object.DestroyImmediate(child.gameObject);

            var random = new Random(20261030);

            // 2. 예약: 플레이어 시작과 적 자리에는 나무·바위를 놓지 않는다
            Taken.Add(new Vector3(PlayerStart.x, PlayerStart.z, 9f));
            foreach (var spot in EnemySpots)
                Taken.Add(new Vector3(spot.x, spot.y, 5f));

            var environment = new GameObject("Environment").transform;
            var ground = Group("Ground", environment);
            var path = Group("Path", environment);
            var trees = Group("Trees", environment);
            var rocks = Group("Rocks", environment);
            var village = Group("Village", environment);
            var camp = Group("Camp", environment);
            var small = Group("SmallDecor", environment); // 꽃·풀·작은 돌: 충돌 없음, NavMesh에서 뺀다
            var backdrop = Group("Backdrop", environment); // 언덕·산·하늘·구름: 멀리 있는 배경, NavMesh에서 뺀다
            var walls = Group("InvisibleWalls", environment);

            BuildGround(ground);
            BuildPath(path);
            BuildVillage(village, random);
            BuildCamp(camp, random);
            BuildTrees(trees, random);
            BuildRocks(rocks, random);
            BuildSmallDecor(small, random);
            BuildBackdrop(backdrop, random);
            BuildWalls(walls);
            IgnoreInNavMesh(small);
            IgnoreInNavMesh(backdrop);

            SetupLightAndCamera();
            SnapToGround(); // 물건이 땅 위에 뜨거나 파묻히지 않게 땅 높이에 맞춘다

            // 3. 플레이어 시작 위치
            var player = GameObject.Find("Player");
            var controller = player.GetComponent<CharacterController>();
            controller.enabled = false;
            player.transform.position = new Vector3(PlayerStart.x, player.transform.position.y, PlayerStart.z);
            player.transform.rotation = Quaternion.identity; // +Z(숲 안쪽)를 본다
            controller.enabled = true;
            // 팝업·일시정지 창이 열렸을 때 이동·카메라·사격을 멈추고 마우스를 보이게 하는 것 (훈련장 복사본에는 빠져 있었다)
            if (player.GetComponent<LastBullet.Player.PlayerControlLock>() == null)
                player.AddComponent<LastBullet.Player.PlayerControlLock>();

            // 4. NavMesh: 이 씬 전용 데이터를 새로 굽는다 (훈련장 것을 덮어쓰지 않게 먼저 비운다)
            var surface = GameObject.Find("NavMesh").GetComponent<NavMeshSurface>();
            surface.navMeshData = null;
            surface.BuildNavMesh();
            if (!AssetDatabase.IsValidFolder("Assets/Scenes/2_Forest"))
                AssetDatabase.CreateFolder("Assets/Scenes", "2_Forest");
            AssetDatabase.CreateAsset(surface.navMeshData, "Assets/Scenes/2_Forest/NavMesh-2_Forest.asset");

            // 5. 적 7마리 (NavMesh 위로 자리를 맞춘다)
            var enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_Charger.prefab");
            for (int i = 0; i < EnemyCount; i++)
            {
                var enemy = (GameObject)PrefabUtility.InstantiatePrefab(enemyPrefab, enemiesRoot.transform);
                enemy.name = $"Enemy_Charger_{i + 1}";
                Vector3 position = new Vector3(EnemySpots[i].x, 0f, EnemySpots[i].y);
                if (NavMesh.SamplePosition(position, out var hit, 8f, NavMesh.AllAreas))
                    position = hit.position;
                enemy.transform.position = position;
                enemy.transform.rotation = Quaternion.LookRotation(PlayerStart - position, Vector3.up);
            }

            // 6. 빌드 목록
            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(s => s.path == ForestPath))
                scenes.Add(new EditorBuildSettingsScene(ForestPath, true));
            EditorBuildSettings.scenes = scenes.ToArray();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[LastBullet] Phase 10: 2_Forest 씬을 만들었어요 (적 {EnemyCount}마리, 큰 물건 {Taken.Count}개 자리).");
        }

        // ───────────── 땅 ─────────────

        static void BuildGround(Transform parent)
        {
            // 평평한 바닥(풀색)이 아래에 있어서 타일 사이가 벌어져도 빠지지 않는다
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.SetParent(parent, false);
            floor.transform.position = new Vector3(0f, -0.3f, 0f);
            floor.transform.localScale = new Vector3(14f, 1f, 14f);
            floor.GetComponent<Renderer>().sharedMaterial = ArtSlots.GetOrCreateMaterial("ForestFloor", new Color32(0x6F, 0x8F, 0x3F, 0xFF));

            var tile1 = Load("Nature/Terrain/rpgpp_lt_terrain_grass_01");
            var tile2 = Load("Nature/Terrain/rpgpp_lt_terrain_grass_02");
            float[] xs = { -47.5f, -28.5f, -9.5f, 9.5f, 28.5f, 47.5f };
            float[] zs = { -51f, -30.6f, -10.2f, 10.2f, 30.6f, 51f };
            int index = 0;
            foreach (float x in xs)
            {
                foreach (float z in zs)
                {
                    var tile = Spawn(index++ % 3 == 0 ? tile2 : tile1, parent, new Vector3(x, -0.28f, z), 0f);
                    tile.name = $"GrassTile_{index:00}";
                    tile.AddComponent<MeshCollider>().sharedMesh = tile.GetComponentInChildren<MeshFilter>().sharedMesh;
                }
            }
        }

        // 시작에서 마을까지 이어지는 길 (눈에 보이는 장식. 걷는 데 영향 없음)
        static void BuildPath(Transform parent)
        {
            var a = Load("Nature/Terrain/rpgpp_lt_terrain_path_01a");
            var b = Load("Nature/Terrain/rpgpp_lt_terrain_path_01b");
            Vector2[] points =
            {
                new Vector2(0f, -40f), new Vector2(-4f, -30f), new Vector2(-4f, -19f), new Vector2(1f, -9f),
                new Vector2(8f, -1f), new Vector2(15f, 9f), new Vector2(20f, 20f), new Vector2(25f, 29f),
            };
            for (int i = 0; i < points.Length - 1; i++)
            {
                Vector2 from = points[i], to = points[i + 1];
                Vector2 mid = (from + to) * 0.5f;
                float yaw = Mathf.Atan2(to.x - from.x, to.y - from.y) * Mathf.Rad2Deg - 90f; // 길쭉한 쪽(X)을 진행 방향으로
                var piece = Spawn(i % 2 == 0 ? a : b, parent, new Vector3(mid.x, 0.02f, mid.y), yaw);
                piece.name = $"Path_{i + 1:00}";
                // 길 위에는 큰 물건을 놓지 않는다
                Taken.Add(new Vector3(mid.x, mid.y, 4f));
            }
        }

        // ───────────── 마을, 야영지 ─────────────

        static void BuildVillage(Transform parent, Random random)
        {
            // 건물은 바닥이 땅속(-1)까지 내려가 있어서 1만큼 올린다
            Building(parent, "Buildings/Bld_closed/rpgpp_lt_building_02", new Vector3(36f, 1f, 36f), 200f, 6f);
            Building(parent, "Buildings/Bld_closed/rpgpp_lt_building_04", new Vector3(23f, 1f, 41f), 160f, 6.5f);
            Building(parent, "Exterior/Wood_path/rpgpp_lt_shed_wood_01", new Vector3(40f, 0f, 24f), 250f, 3f);
            Building(parent, "Exterior/Wood_path/rpgpp_lt_shed_wood_02", new Vector3(13f, 0f, 44f), 190f, 2.5f);
            Building(parent, "Props/Misc/rpgpp_lt_well_01", new Vector3(28f, 0f, 31f), 0f, 2f);
            Building(parent, "Props/Misc/rpgpp_lt_wagon_01", new Vector3(17f, 0f, 27f), 35f, 2.5f);

            // 울타리 한 줄 (엄폐물)
            for (int i = 0; i < 5; i++)
                Building(parent, "Exterior/Wood_path/rpgpp_lt_fence_wood_01a", new Vector3(30f + i * 3f, 0f, 20f), 0f, 0f, takes: false);

            // 마을 소품: 상자·통·바구니·자루
            string[] props =
            {
                "Props/Containers/rpgpp_lt_crate_01", "Props/Containers/rpgpp_lt_crate_02", "Props/Containers/rpgpp_lt_barrel_01",
                "Props/Containers/rpgpp_lt_barrel_02", "Props/Containers/rpgpp_lt_crate_03", "Props/Containers/rpgpp_lt_basket_01",
            };
            Vector2[] spots =
            {
                new Vector2(32f, 29f), new Vector2(33.5f, 29.5f), new Vector2(27f, 38f), new Vector2(28.5f, 39f),
                new Vector2(19f, 34f), new Vector2(40f, 32f),
            };
            for (int i = 0; i < props.Length; i++)
                Building(parent, props[i], new Vector3(spots[i].x, 0f, spots[i].y), (float)random.NextDouble() * 360f, 0f, takes: false);
        }

        static void BuildCamp(Transform parent, Random random)
        {
            Building(parent, "Props/Tables/rpgpp_lt_table_01", new Vector3(-27f, 0f, 3f), 80f, 2f);
            Building(parent, "Props/Benches/rpgpp_lt_bench_wood_01", new Vector3(-24f, 0f, 0f), 10f, 0f, takes: false);
            Building(parent, "Props/Benches/rpgpp_lt_bench_wood_02", new Vector3(-30f, 0f, 6f), 160f, 0f, takes: false);
            Building(parent, "Props/Wood/rpgpp_lt_log_wood_01", new Vector3(-21f, 0f, 6f), 40f, 1.5f);
            Building(parent, "Props/Wood/rpgpp_lt_log_wood_02a", new Vector3(-33f, 0f, -1f), 100f, 1.5f);
            Building(parent, "Props/Containers/rpgpp_lt_barrel_01", new Vector3(-28.5f, 0f, 6.5f), 0f, 0f, takes: false);
            Building(parent, "Props/Containers/rpgpp_lt_crate_01", new Vector3(-25.5f, 0f, 7f), 25f, 0f, takes: false);
        }

        // ───────────── 나무, 바위, 작은 장식 ─────────────

        static void BuildTrees(Transform parent, Random random)
        {
            string[] kinds = { "Nature/Vegetation/Trees/rpgpp_lt_tree_01", "Nature/Vegetation/Trees/rpgpp_lt_tree_02", "Nature/Vegetation/Trees/rpgpp_lt_tree_pine_01" };

            // 가장자리: 둘레를 따라 나무 두 겹
            for (int layer = 0; layer < 2; layer++)
            {
                float halfX = 50f + layer * 4f, halfZ = 56f + layer * 4f;
                int count = layer == 0 ? 44 : 36;
                for (int i = 0; i < count; i++)
                {
                    float t = (i + (float)random.NextDouble() * 0.6f) / count * 4f; // 0..4 = 네 변
                    Vector2 p;
                    if (t < 1f) p = new Vector2(Mathf.Lerp(-halfX, halfX, t), -halfZ);
                    else if (t < 2f) p = new Vector2(halfX, Mathf.Lerp(-halfZ, halfZ, t - 1f));
                    else if (t < 3f) p = new Vector2(Mathf.Lerp(halfX, -halfX, t - 2f), halfZ);
                    else p = new Vector2(-halfX, Mathf.Lerp(halfZ, -halfZ, t - 3f));
                    p += new Vector2((float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f) * 3f;
                    PlaceTree(parent, kinds[random.Next(kinds.Length)], p, random, 1.1f + (float)random.NextDouble() * 0.5f, reserve: false);
                }
            }

            // 안쪽: 흩어진 나무
            int placed = 0, tries = 0;
            while (placed < 55 && tries++ < 600)
            {
                var p = new Vector2(Mathf.Lerp(-46f, 46f, (float)random.NextDouble()), Mathf.Lerp(-40f, 52f, (float)random.NextDouble()));
                if (PlaceTree(parent, kinds[random.Next(kinds.Length)], p, random, 0.9f + (float)random.NextDouble() * 0.5f, reserve: true))
                    placed++;
            }
        }

        static bool PlaceTree(Transform parent, string kind, Vector2 p, Random random, float scale, bool reserve)
        {
            if (reserve && !IsFree(p, 2.2f))
                return false;
            var tree = Spawn(Load(kind), parent, new Vector3(p.x, 0f, p.y), (float)random.NextDouble() * 360f);
            tree.transform.localScale = Vector3.one * scale;
            var capsule = tree.AddComponent<CapsuleCollider>(); // 줄기만 막는다
            capsule.center = new Vector3(0f, 1.8f, 0f);
            capsule.radius = 0.45f;
            capsule.height = 3.6f;
            if (reserve)
                Taken.Add(new Vector3(p.x, p.y, 2.2f * scale));
            return true;
        }

        static void BuildRocks(Transform parent, Random random)
        {
            string[] kinds =
            {
                "Nature/Rocks/rpgpp_lt_rock_01", "Nature/Rocks/rpgpp_lt_rock_02", "Nature/Rocks/rpgpp_lt_rock_03",
            };
            int placed = 0, tries = 0;
            while (placed < 18 && tries++ < 600)
            {
                var p = new Vector2(Mathf.Lerp(-46f, 46f, (float)random.NextDouble()), Mathf.Lerp(-38f, 50f, (float)random.NextDouble()));
                if (!IsFree(p, 3f))
                    continue;
                var rock = Spawn(Load(kinds[random.Next(kinds.Length)]), parent, new Vector3(p.x, 0.15f, p.y), (float)random.NextDouble() * 360f);
                rock.transform.localScale = Vector3.one * (1.0f + (float)random.NextDouble() * 0.9f);
                AddBoxCollider(rock, 0.85f);
                Taken.Add(new Vector3(p.x, p.y, 3f));
                placed++;
            }
        }

        static void BuildSmallDecor(Transform parent, Random random)
        {
            string[] bushes = { "Nature/Vegetation/Bushes/rpgpp_lt_bush_01", "Nature/Vegetation/Bushes/rpgpp_lt_bush_02" };
            string[] flowers =
            {
                "Nature/Vegetation/Flowers/rpgpp_lt_flower_01", "Nature/Vegetation/Flowers/rpgpp_lt_flower_02", "Nature/Vegetation/Flowers/rpgpp_lt_flower_03",
            };
            string[] grass = { "Nature/Vegetation/Grass/rpgpp_lt_grass_small_01a", "Nature/Vegetation/Grass/rpgpp_lt_grass_small_01b", "Nature/Vegetation/Plants/rpgpp_lt_plant_01", "Nature/Vegetation/Plants/rpgpp_lt_plant_02" };
            string[] stones = { "Nature/Rocks/rpgpp_lt_rock_small_01", "Nature/Rocks/rpgpp_lt_rock_small_02" };

            Scatter(parent, bushes, 26, 1.8f, random, 0.8f, 1.3f, 0.2f);
            Scatter(parent, flowers, 55, 0f, random, 0.9f, 1.4f, 0.3f);
            Scatter(parent, grass, 80, 0f, random, 0.9f, 1.5f, 0.3f);
            Scatter(parent, stones, 30, 0f, random, 0.8f, 1.6f, 0.2f);
        }

        static void Scatter(Transform parent, string[] kinds, int count, float clearRadius, Random random, float minScale, float maxScale, float y)
        {
            int placed = 0, tries = 0;
            while (placed < count && tries++ < count * 20)
            {
                var p = new Vector2(Mathf.Lerp(-50f, 50f, (float)random.NextDouble()), Mathf.Lerp(-52f, 54f, (float)random.NextDouble()));
                if (Vector2.Distance(p, new Vector2(PlayerStart.x, PlayerStart.z)) < 4f)
                    continue; // 시작 자리 바로 옆은 비워 둔다
                if (clearRadius > 0f && !IsFree(p, clearRadius))
                    continue;
                var item = Spawn(Load(kinds[random.Next(kinds.Length)]), parent, new Vector3(p.x, y, p.y), (float)random.NextDouble() * 360f);
                item.transform.localScale = Vector3.one * Mathf.Lerp(minScale, maxScale, (float)random.NextDouble());
                placed++;
            }
        }

        // ───────────── 배경, 벽, 빛 ─────────────

        static void BuildBackdrop(Transform parent, Random random)
        {
            // 아주 넓은 풀밭: 언덕·산 아래가 비어 보이지 않게 한다 (충돌 없음, 걷는 곳은 위의 Floor와 타일)
            var farFloor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            farFloor.name = "FloorFar";
            Object.DestroyImmediate(farFloor.GetComponent<Collider>());
            farFloor.transform.SetParent(parent, false);
            farFloor.transform.position = new Vector3(0f, -0.32f, 0f);
            farFloor.transform.localScale = new Vector3(80f, 1f, 80f);
            farFloor.GetComponent<Renderer>().sharedMaterial = ArtSlots.GetOrCreateMaterial("ForestFloor", new Color32(0x6F, 0x8F, 0x3F, 0xFF));

            // 언덕·산은 가장자리 바깥에 둘러 세운다 (바닥을 땅에 붙이려고 y는 0)
            var hills = new[] { Load("Nature/Terrain/rpgpp_lt_hill_small_01"), Load("Nature/Terrain/rpgpp_lt_hill_small_02") };
            for (int i = 0; i < 10; i++)
            {
                float angle = i / 10f * 360f + 12f;
                float radius = 125f + (i % 3) * 18f;
                var hill = Spawn(hills[i % 2], parent, Quaternion.Euler(0f, angle, 0f) * Vector3.forward * radius, (float)random.NextDouble() * 360f);
                hill.transform.localScale = Vector3.one * (0.9f + (float)random.NextDouble() * 0.35f);
            }
            var mountain = Load("Nature/Terrain/rpgpp_lt_mountain_01");
            for (int i = 0; i < 6; i++)
            {
                float angle = i / 6f * 360f + 40f;
                var m = Spawn(mountain, parent, Quaternion.Euler(0f, angle, 0f) * Vector3.forward * (235f + (i % 2) * 40f), (float)random.NextDouble() * 360f);
                m.transform.localScale = Vector3.one * (1f + (float)random.NextDouble() * 0.3f);
            }

            // 하늘 돔과 구름
            var sky = Spawn(Load("Nature/Sky/rpgpp_lt_sky_01"), parent, new Vector3(0f, -69f, 0f), 0f);
            sky.name = "SkyDome";
            sky.transform.localScale = Vector3.one * 10f;
            var clouds = new[] { Load("Nature/Sky/rpgpp_lt_cloud_01"), Load("Nature/Sky/rpgpp_lt_cloud_02") };
            for (int i = 0; i < 12; i++)
            {
                float angle = i / 12f * 360f + (float)random.NextDouble() * 20f;
                var cloud = Spawn(clouds[i % 2], parent, Quaternion.Euler(0f, angle, 0f) * Vector3.forward * (250f + (float)random.NextDouble() * 60f) + Vector3.up * (80f + (float)random.NextDouble() * 90f), (float)random.NextDouble() * 360f);
                cloud.transform.localScale = Vector3.one * 1.5f;
            }
        }

        // 보이지 않는 벽: 가장자리 나무 바깥으로 못 나가게 한다
        static void BuildWalls(Transform parent)
        {
            Wall(parent, "Wall_North", new Vector3(0f, 5f, 59f), new Vector3(130f, 10f, 2f));
            Wall(parent, "Wall_South", new Vector3(0f, 5f, -59f), new Vector3(130f, 10f, 2f));
            Wall(parent, "Wall_East", new Vector3(53f, 5f, 0f), new Vector3(2f, 10f, 130f));
            Wall(parent, "Wall_West", new Vector3(-53f, 5f, 0f), new Vector3(2f, 10f, 130f));
        }

        static void Wall(Transform parent, string name, Vector3 position, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.AddComponent<BoxCollider>().size = size;
        }

        static void SetupLightAndCamera()
        {
            var light = GameObject.Find("Directional Light");
            if (light != null)
            {
                light.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
                var component = light.GetComponent<Light>();
                component.color = new Color(1f, 0.96f, 0.86f);
                component.intensity = 1.15f;
            }

            // 멀리 있는 산과 하늘 돔이 잘리지 않게 카메라가 보는 거리를 늘린다
            var follow = GameObject.Find("PlayerFollowCamera");
            if (follow != null)
            {
                var so = new SerializedObject(follow.GetComponent("CinemachineCamera"));
                var far = so.FindProperty("Lens.FarClipPlane");
                if (far != null)
                {
                    far.floatValue = 1500f;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            var main = Camera.main;
            if (main != null)
                main.farClipPlane = 1500f;
        }

        // ───────────── 땅에 붙이기 ─────────────

        // 풀 타일은 울퉁불퉁(높이 -0.2 ~ +0.2)해서, 물건을 같은 높이에 놓으면 어떤 곳은 뜨고 어떤 곳은 파묻힌다.
        // 물건마다 그 자리의 땅 높이를 재서, 물건의 맨 아래가 땅에 살짝 묻히도록 높이를 맞춘다.
        [MenuItem("LastBullet/숲 장식 땅에 붙이기 (다시 맞추기)")]
        static void SnapMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var scene = EditorSceneManager.OpenScene(ForestPath);
            SnapToGround();

            // 높이가 바뀌었으니 NavMesh도 다시 굽는다 (이 씬의 데이터 파일을 그대로 갱신)
            var surface = GameObject.Find("NavMesh").GetComponent<NavMeshSurface>();
            surface.BuildNavMesh();
            EditorUtility.SetDirty(surface.navMeshData);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[LastBullet] 숲 장식을 땅 높이에 맞추고 NavMesh를 다시 구웠어요.");
        }

        // 묶음 이름 → 땅에 묻는 깊이(m). 클수록 더 깊이 묻힌다.
        static readonly (string group, float embed)[] SnapGroups =
        {
            ("Trees", 0.12f), ("Rocks", 0.2f), ("SmallDecor", 0.06f), ("Village", 0.25f), ("Camp", 0.08f),
        };

        static void SnapToGround()
        {
            var environment = GameObject.Find("Environment");
            if (environment == null)
                return;
            Physics.SyncTransforms(); // 방금 놓은 충돌이 바로 잡히게 한다

            foreach (var (groupName, embed) in SnapGroups)
            {
                var group = environment.transform.Find(groupName);
                if (group == null)
                    continue;
                foreach (Transform item in group)
                {
                    if (!TryGroundHeight(item.position, out float groundY) || !TryBounds(item, out var bounds))
                        continue;
                    item.position += Vector3.up * ((groundY - embed) - bounds.min.y);
                }
            }

            // 길은 땅 위에 살짝 보이게: 길 조각 아래 땅의 가장 높은 곳보다 조금 위에 맨 위를 맞춘다 (울퉁불퉁한 땅에 가려져 끊기지 않게)
            var path = environment.transform.Find("Path");
            if (path != null)
            {
                foreach (Transform piece in path)
                {
                    if (!TryBounds(piece, out var bounds))
                        continue;
                    float highest = float.MinValue;
                    for (int x = -2; x <= 2; x++)
                    {
                        for (int z = -1; z <= 1; z++)
                        {
                            // 길 조각의 길쭉한 쪽이 로컬 X축이다 (길이 약 4.5m, 폭 약 2.9m)
                            if (TryGroundHeight(piece.TransformPoint(new Vector3(x, 0f, z)), out float sample))
                                highest = Mathf.Max(highest, sample);
                        }
                    }
                    if (highest > float.MinValue)
                        piece.position += Vector3.up * ((highest + 0.05f) - bounds.max.y);
                }
            }
        }

        // 그 자리의 땅 높이. 위에서 아래로 쏴서 풀 타일이나 바닥에 처음 닿는 곳 (나무·건물 충돌은 무시한다)
        static bool TryGroundHeight(Vector3 position, out float height)
        {
            var hits = Physics.RaycastAll(new Vector3(position.x, 30f, position.z), Vector3.down, 60f, ~0, QueryTriggerInteraction.Ignore)
                .OrderBy(h => h.distance);
            foreach (var hit in hits)
            {
                string name = hit.collider.gameObject.name;
                if (name.StartsWith("GrassTile") || name == "Floor")
                {
                    height = hit.point.y;
                    return true;
                }
            }
            height = 0f;
            return false;
        }

        static bool TryBounds(Transform item, out Bounds bounds)
        {
            var renderers = item.GetComponentsInChildren<Renderer>();
            bounds = default;
            if (renderers.Length == 0)
                return false;
            bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
                bounds.Encapsulate(renderer.bounds);
            return true;
        }

        // ───────────── 도우미 ─────────────

        static Transform Group(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        static GameObject Load(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}{path}.prefab");
            if (prefab == null)
                Debug.LogError($"[LastBullet] 프리팹을 못 찾음: {path}");
            return prefab;
        }

        static GameObject Spawn(GameObject prefab, Transform parent, Vector3 position, float yaw)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.transform.position = position;
            instance.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            return instance;
        }

        // 건물·소품 하나를 놓고 충돌을 붙인다. takes: 큰 물건 자리로 기록할지 (반지름 radius)
        static void Building(Transform parent, string path, Vector3 position, float yaw, float radius, bool takes = true)
        {
            var go = Spawn(Load(path), parent, position, yaw);
            AddBoxCollider(go, 0.95f);
            if (takes && radius > 0f)
                Taken.Add(new Vector3(position.x, position.z, radius));
        }

        // 프리팹의 모양(Renderer)을 감싸는 상자 충돌을 붙인다 (회전·크기 변화는 오브젝트 따라간다)
        static void AddBoxCollider(GameObject go, float shrink)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return;
            // 월드 → 로컬: 회전한 오브젝트라도 상자가 모양을 따라가도록 로컬 축 기준으로 다시 잰다
            var local = new Bounds(go.transform.InverseTransformPoint(renderers[0].bounds.center), Vector3.zero);
            foreach (var renderer in renderers)
            {
                var b = renderer.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var corner = new Vector3(
                        (i & 1) == 0 ? b.min.x : b.max.x,
                        (i & 2) == 0 ? b.min.y : b.max.y,
                        (i & 4) == 0 ? b.min.z : b.max.z);
                    local.Encapsulate(go.transform.InverseTransformPoint(corner));
                }
            }
            var box = go.AddComponent<BoxCollider>();
            box.center = local.center;
            box.size = local.size * shrink;
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

        // 이 묶음(과 자식)은 적이 걷는 길(NavMesh)을 만들 때 빼 둔다
        static void IgnoreInNavMesh(Transform group)
        {
            var modifier = group.gameObject.AddComponent<NavMeshModifier>();
            modifier.ignoreFromBuild = true;
            modifier.applyToChildren = true;
        }
    }
}
