using System.Collections.Generic;
using System.Linq;
using LastBullet.Enemy;
using LastBullet.Inventory;
using LastBullet.Stage;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Random = System.Random;

namespace LastBullet.EditorTools
{
    // 성 마을 씬(3_CastleVillage)을 만든다 (2026-10-30 사용자 요청: 에셋 3D Cartoon Village로 성이 있는 마을 맵).
    //  - 2_Forest를 복사해서 시작한다 (플레이어·카메라·HUD·전투 가방·일시정지 창이 이미 들어 있다).
    //  - 에셋 조각은 원래 크기가 커서(집이 33m) 0.25배로 줄여 쓴다.
    //  - 지도 (남→북): 마을(시작) → 성 앞뜰 → 성문(Gats)이 있는 성벽 → 성 안(TownHall).
    //  - 물감: (2026-10-31 변경) 맵 전체가 초록·파랑·보라 (StageColors 표). 처음에는 지역(마을·성 앞뜰·성 안)마다 3색씩이었다.
    //  - 적은 지역마다 스포너로 놓는다 (마을 5 / 성 앞뜰 6 / 성 안 7).
    // 에셋 프리팹에는 충돌(Collider)이 없어서 벽·탑·집은 모양을 따라가는 MeshCollider, 나무·상자 같은 작은 것은 상자·캡슐 충돌을 붙인다.
    // 메뉴: LastBullet > Phase 11 성 마을 씬 만들기
    public static class Phase11CastleSetup
    {
        const string ForestPath = "Assets/Scenes/2_Forest.unity";
        const string CastlePath = "Assets/Scenes/3_CastleVillage.unity";
        const string Root = "Assets/3D Cartoon Village/Prefabs/";
        const string RpgRoot = "Assets/RPGPP_LT/Prefabs/";
        const float S = 0.25f; // 에셋 조각 크기 배율

        enum Collide { None, Box, Mesh, Capsule }

        static readonly Vector2 PlayerStart = new Vector2(0f, -48f);
        static readonly List<Vector3> Taken = new List<Vector3>(); // 큰 물건 자리 (x, z, 반지름)

        [MenuItem("LastBullet/Phase 11 성 마을 씬 만들기")]
        static void Create()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(CastlePath) != null)
            {
                EditorUtility.DisplayDialog("LastBullet", "이미 3_CastleVillage 씬이 있어요.", "확인");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            AssetDatabase.CopyAsset(ForestPath, CastlePath);
            var scene = EditorSceneManager.OpenScene(CastlePath);
            Lightmapping.lightingDataAsset = null; // 숲에서 구운 빛 데이터는 이 씬과 맞지 않는다
            // 구운 빛 데이터 없이도 그림자 쪽이 새까맣게 되지 않도록, 하늘·옆·땅 세 가지 주변광 색을 직접 정한다
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.74f, 0.92f);
            RenderSettings.ambientEquatorColor = new Color(0.60f, 0.60f, 0.56f);
            RenderSettings.ambientGroundColor = new Color(0.38f, 0.34f, 0.28f);
            RenderSettings.ambientIntensity = 1f;
            Taken.Clear();

            // 1. 숲의 환경·적 치우기
            Object.DestroyImmediate(GameObject.Find("Environment"));
            Object.DestroyImmediate(GameObject.Find("Spawners"));
            var enemiesRoot = GameObject.Find("Enemies").transform;
            foreach (var child in enemiesRoot.Cast<Transform>().ToArray())
                Object.DestroyImmediate(child.gameObject);

            var random = new Random(20261101);
            var environment = new GameObject("Environment").transform;
            var ground = Group("Ground", environment);
            var village = Group("Village", environment);   // 지역 1
            var field = Group("Field", environment);       // 지역 2
            var castle = Group("Castle", environment);     // 지역 3 + 성벽
            var trees = Group("Trees", environment);
            var small = Group("SmallDecor", environment);
            var backdrop = Group("Backdrop", environment);
            var walls = Group("InvisibleWalls", environment);

            BuildGround(ground);
            BuildCastle(castle, random);
            BuildVillage(village, random);
            BuildField(field, random);
            BuildTrees(trees, random);
            BuildSmallDecor(small, random);
            BuildBackdrop(backdrop, random);
            BuildInvisibleWalls(walls);
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
            if (!AssetDatabase.IsValidFolder("Assets/Scenes/3_CastleVillage"))
                AssetDatabase.CreateFolder("Assets/Scenes", "3_CastleVillage");
            AssetDatabase.CreateAsset(surface.navMeshData, "Assets/Scenes/3_CastleVillage/NavMesh-3_CastleVillage.asset");

            // 4. 물감 지역 3개 + 지역마다 적 스포너
            var spawnersRoot = new GameObject("Spawners").transform;
            var zones = new[]
            {
                new ZoneDef("Zone1_Village", "마을", new Vector2(0f, -20f), new Vector2(120f, 50f),
                    new[] { PaintColor.Red, PaintColor.Orange, PaintColor.Yellow },
                    new Rect(-44f, -20f, 88f, 24f), (3, 0, 2)),
                new ZoneDef("Zone2_Field", "성 앞뜰", new Vector2(0f, 22.5f), new Vector2(120f, 35f),
                    new[] { PaintColor.Green, PaintColor.Blue, PaintColor.Purple },
                    new Rect(-48f, 7f, 96f, 29f), (2, 2, 2)),
                new ZoneDef("Zone3_Castle", "성 안", new Vector2(0f, 67.5f), new Vector2(120f, 55f),
                    new[] { PaintColor.Pink, PaintColor.White, PaintColor.Black },
                    new Rect(-44f, 47f, 88f, 40f), (2, 3, 2)),
            };

            var enemyPrefabs = new Dictionary<string, GameObject>
            {
                ["Charger"] = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_Charger.prefab"),
                ["Shield"] = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_Shield.prefab"),
                ["Thrower"] = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_Thrower.prefab"),
            };
            var taken = new List<Vector2>();
            var counters = new Dictionary<string, int> { ["Charger"] = 0, ["Shield"] = 0, ["Thrower"] = 0 };
            foreach (var zone in zones)
            {
                // 2026-10-31: 맵마다 물감 3색(StageColors)이라 PaintZone은 만들지 않는다 (지역은 적 배치에만 쓴다)
                var zoneSpawners = Group(zone.Id, spawnersRoot);
                var (chargers, shields, throwers) = zone.Enemies;
                AddSpawners("Charger", chargers, zone, zoneSpawners, enemiesRoot, enemyPrefabs, taken, counters, random);
                AddSpawners("Shield", shields, zone, zoneSpawners, enemiesRoot, enemyPrefabs, taken, counters, random);
                AddSpawners("Thrower", throwers, zone, zoneSpawners, enemiesRoot, enemyPrefabs, taken, counters, random);
            }

            // 5. 빌드 목록
            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(s => s.path == CastlePath))
                scenes.Add(new EditorBuildSettingsScene(CastlePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[LastBullet] Phase 11: 3_CastleVillage 씬을 만들었어요 (스포너 {spawnersRoot.GetComponentsInChildren<EnemySpawner>().Length}개).");
        }

        // ───────────── 땅 ─────────────

        static void BuildGround(Transform parent)
        {
            var floor = Plane("Floor", parent, new Vector3(0f, 0f, 22f), new Vector3(14f, 1f, 17f),
                ArtSlots.GetOrCreateMaterial("CastleFloor", new Color32(0x86, 0xA8, 0x4B, 0xFF)), keepCollider: true);
            // 마을에서 성문까지 이어지는 흙길, 성 안의 돌바닥 (눈에 보이는 장식, 충돌 없음)
            var road = ArtSlots.GetOrCreateMaterial("CastleRoad", new Color32(0xC9, 0xB3, 0x8A, 0xFF));
            Plane("Road", parent, new Vector3(0f, 0.015f, -4f), new Vector3(0.7f, 1f, 9.4f), road);        // x ±3.5, z -51..43
            Plane("Plaza", parent, new Vector3(0f, 0.016f, -18f), new Vector3(1.8f, 1f, 1.8f), road);      // 마을 광장
            var stone = ArtSlots.GetOrCreateMaterial("CastleCourtyard", new Color32(0x9A, 0x96, 0x90, 0xFF));
            Plane("Courtyard", parent, new Vector3(0f, 0.02f, 68f), new Vector3(9.6f, 1f, 4.4f), stone);   // 성 안 바닥 x ±48, z 46..90
        }

        static GameObject Plane(string name, Transform parent, Vector3 position, Vector3 scale, Material material, bool keepCollider = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
            go.name = name;
            if (!keepCollider)
                Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        // ───────────── 성 ─────────────

        static void BuildCastle(Transform parent, Random random)
        {
            const float frontZ = 40f, backZ = 92f, sideX = 52f;

            // 성문: 앞 성벽 가운데 (원래 길쭉한 쪽이 Z라 90도 돌려 X 방향으로 놓는다)
            var gate = Piece("Gats", parent, new Vector2(0f, frontZ), 90f, Collide.Mesh, reserve: 11f);
            // 문짝(Mesh2, Mesh7, Mesh12)은 빼서 성문을 열어 둔다 (플레이어가 지나가야 한다). 쇠창살(내려오는 철문)은 남는다.
            foreach (var part in gate.GetComponentsInChildren<Transform>(true))
            {
                if (part.name == "Mesh2" || part.name == "Mesh7" || part.name == "Mesh12")
                    Object.DestroyImmediate(part.gameObject);
            }

            // 앞 성벽: 성문 양옆 (벽 한 조각의 길이 7.4m)
            for (int k = 0; k < 6; k++)
            {
                float x = 13.1f + 7.4f * k;
                Piece("Wall", parent, new Vector2(x, frontZ), 90f, Collide.Mesh);
                Piece("Wall", parent, new Vector2(-x, frontZ), 90f, Collide.Mesh);
            }
            // 옆 성벽 (Z 방향)
            for (int k = 0; k < 6; k++)
            {
                float z = 47.2f + 7.4f * k;
                Piece("Wall", parent, new Vector2(sideX, z), 0f, Collide.Mesh);
                Piece("Wall", parent, new Vector2(-sideX, z), 0f, Collide.Mesh);
            }
            // 뒤 성벽 (X 방향)
            for (int k = 0; k < 14; k++)
                Piece("Wall", parent, new Vector2(-48.1f + 7.4f * k, backZ), 90f, Collide.Mesh);

            // 모서리 탑 4개 + 가운데 탑
            foreach (float x in new[] { -sideX - 2f, sideX + 2f })
            {
                Piece("Tower10", parent, new Vector2(x, frontZ), 0f, Collide.Mesh, reserve: 5f);
                Piece("Tower12", parent, new Vector2(x, backZ), 0f, Collide.Mesh, reserve: 5f);
                Piece("Tower30", parent, new Vector2(x, 66f), 0f, Collide.Mesh, reserve: 5f);
            }
            foreach (float x in new[] { -28f, 28f })
            {
                Piece("Tower30", parent, new Vector2(x, frontZ), 0f, Collide.Mesh, reserve: 5f);
                Piece("Tower12", parent, new Vector2(x, backZ), 0f, Collide.Mesh, reserve: 5f);
            }
            Piece("Tower3", parent, new Vector2(-12f, frontZ + 3f), 0f, Collide.Mesh, reserve: 3f); // 성문 옆 가는 탑
            Piece("Tower4", parent, new Vector2(12f, frontZ + 3f), 0f, Collide.Mesh, reserve: 3f);

            // 성 안: 가운데 시청(성의 본관)과 집, 우물, 상자
            Piece("TownHall", parent, new Vector2(0f, 72f), 0f, Collide.Mesh, reserve: 6f);
            Piece("House3", parent, new Vector2(-38f, 60f), 90f, Collide.Mesh, reserve: 7f);
            Piece("House", parent, new Vector2(38f, 60f), -90f, Collide.Mesh, reserve: 7f);
            RoofedHouse("House2", parent, new Vector2(-38f, 80f), 90f);
            RoofedHouse("House4", parent, new Vector2(38f, 80f), -90f);
            Piece("Well", parent, new Vector2(0f, 54f), 0f, Collide.Box, reserve: 3f);
            Piece("Lamp", parent, new Vector2(-6f, 50f), 0f, Collide.Capsule);
            Piece("Lamp", parent, new Vector2(6f, 50f), 180f, Collide.Capsule);
            Piece("Lamp", parent, new Vector2(-6f, 62f), 0f, Collide.Capsule);
            Piece("Lamp", parent, new Vector2(6f, 62f), 180f, Collide.Capsule);
            Piece("Cart", parent, new Vector2(-22f, 58f), 20f, Collide.Box, reserve: 4f);
            Piece("Barrel", parent, new Vector2(-18f, 84f), 0f, Collide.Box);
            Piece("Barrel2", parent, new Vector2(-16.5f, 85f), 40f, Collide.Box);
            Piece("Box", parent, new Vector2(18f, 84f), 10f, Collide.Box);
            Piece("Box2", parent, new Vector2(19.5f, 85f), 55f, Collide.Box);
            Piece("Table", parent, new Vector2(24f, 56f), 0f, Collide.Box, reserve: 3f);
            Piece("Bench", parent, new Vector2(24f, 59f), 0f, Collide.Box);
        }

        // ───────────── 마을 (남쪽, 시작 쪽) ─────────────

        static void BuildVillage(Transform parent, Random random)
        {
            Piece("House3", parent, new Vector2(-20f, -30f), 90f, Collide.Mesh, reserve: 7f);
            Piece("House", parent, new Vector2(20f, -34f), -90f, Collide.Mesh, reserve: 7f);
            RoofedHouse("House2", parent, new Vector2(-22f, -8f), 90f);
            RoofedHouse("House4", parent, new Vector2(22f, -6f), -90f);
            RoofedHouse("House2", parent, new Vector2(-32f, -20f), 0f);
            RoofedHouse("House4", parent, new Vector2(34f, -20f), 180f);

            Piece("Well", parent, new Vector2(0f, -18f), 0f, Collide.Box, reserve: 3f);
            Piece("Cart", parent, new Vector2(-10f, -12f), 25f, Collide.Box, reserve: 4f);
            Piece("Camp", parent, new Vector2(-12f, -42f), 30f, Collide.Box, reserve: 5f);
            Piece("Camp2", parent, new Vector2(14f, -44f), -20f, Collide.Box, reserve: 5f);
            Piece("Table", parent, new Vector2(12f, -16f), 90f, Collide.Box, reserve: 3f);
            Piece("Bench", parent, new Vector2(12f, -12.5f), 90f, Collide.Box);
            Piece("Chair", parent, new Vector2(9f, -16f), 0f, Collide.Box);
            Piece("Chair2", parent, new Vector2(15f, -16.5f), 160f, Collide.Box);
            Piece("Barrel", parent, new Vector2(-16f, -25f), 0f, Collide.Box);
            Piece("Barrel2", parent, new Vector2(-15f, -26.5f), 30f, Collide.Box);
            Piece("Box", parent, new Vector2(15f, -29f), 15f, Collide.Box);
            Piece("Box2", parent, new Vector2(16.5f, -30f), 70f, Collide.Box);

            // 길 양옆 가로등
            for (int i = 0; i < 6; i++)
            {
                float z = -42f + 13f * i;
                Piece("Lamp", parent, new Vector2(-5.5f, z), 0f, Collide.Capsule);
                Piece("Lamp", parent, new Vector2(5.5f, z + 6f), 180f, Collide.Capsule);
            }

            // 마을 가장자리 울타리
            for (int i = 0; i < 4; i++)
            {
                Piece("Fence", parent, new Vector2(-8f + 4f * i, -52f), 0f, Collide.Box, reserve: 0f);
                Piece("Fence", parent, new Vector2(2f + 4f * i, -52f), 0f, Collide.Box, reserve: 0f);
            }
        }

        // ───────────── 성 앞뜰 (가운데) ─────────────

        static void BuildField(Transform parent, Random random)
        {
            Piece("Camp", parent, new Vector2(-24f, 18f), -60f, Collide.Box, reserve: 5f);
            Piece("Camp2", parent, new Vector2(28f, 14f), 40f, Collide.Box, reserve: 5f);
            Piece("Cart", parent, new Vector2(10f, 26f), -15f, Collide.Box, reserve: 4f);
            Piece("Tower20", parent, new Vector2(-40f, 28f), 90f, Collide.Mesh, reserve: 7f);  // 무너진 감시탑
            Piece("Tower21", parent, new Vector2(40f, 24f), -90f, Collide.Mesh, reserve: 7f);
            Piece("Barrel", parent, new Vector2(-22f, 14f), 0f, Collide.Box);
            Piece("Barrel2", parent, new Vector2(-21f, 15.5f), 50f, Collide.Box);
            Piece("Box", parent, new Vector2(24f, 18f), 20f, Collide.Box);
            for (int i = 0; i < 5; i++)
            {
                Piece("Fence", parent, new Vector2(-34f + 4f * i, 10f), 0f, Collide.Box, reserve: 0f);
                Piece("Fence2", parent, new Vector2(18f + 3f * i, 32f), 0f, Collide.Box, reserve: 0f);
            }

            // 흩어진 큰 돌 (엄폐물)
            string[] rocks = { "Rock3", "Rock6", "Rock4", "Rock5" };
            int placed = 0, tries = 0;
            while (placed < 10 && tries++ < 400)
            {
                var p = new Vector2(Mathf.Lerp(-46f, 46f, (float)random.NextDouble()), Mathf.Lerp(8f, 36f, (float)random.NextDouble()));
                if (Mathf.Abs(p.x) < 9f || !IsFree(p, 5f))
                    continue; // 성문으로 가는 길(가운데)을 막지 않는다
                var rock = Piece(rocks[random.Next(rocks.Length)], parent, p, (float)random.NextDouble() * 360f, Collide.Mesh, reserve: 4.5f, scale: S * 0.6f);
                if (rock != null)
                    placed++;
            }
        }

        // ───────────── 나무와 작은 장식 ─────────────

        static void BuildTrees(Transform parent, Random random)
        {
            string[] kinds = { "Tree2", "Tree3", "Tree" };

            // 가장자리: 지도 둘레에 나무
            var min = new Vector2(-58f, -55f);
            var max = new Vector2(58f, 99f);
            float perimeter = (max.x - min.x) * 2f + (max.y - min.y) * 2f;
            int count = 100;
            for (int i = 0; i < count; i++)
            {
                float t = (i + (float)random.NextDouble() * 0.6f) / count * perimeter;
                Vector2 p;
                float w = max.x - min.x, h = max.y - min.y;
                if (t < w) p = new Vector2(min.x + t, min.y);
                else if (t < w + h) p = new Vector2(max.x, min.y + (t - w));
                else if (t < 2f * w + h) p = new Vector2(max.x - (t - w - h), max.y);
                else p = new Vector2(min.x, max.y - (t - 2f * w - h));
                p += new Vector2((float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f) * 3f;
                PlaceTree(parent, kinds, p, random, reserve: false);
            }

            // 안쪽: 마을과 앞뜰에 흩어진 나무 (길·건물 자리는 피한다)
            int placed = 0, tries = 0;
            while (placed < 45 && tries++ < 800)
            {
                var p = new Vector2(Mathf.Lerp(-50f, 50f, (float)random.NextDouble()), Mathf.Lerp(-50f, 36f, (float)random.NextDouble()));
                if (Mathf.Abs(p.x) < 6f)
                    continue; // 길 위
                if (PlaceTree(parent, kinds, p, random, reserve: true))
                    placed++;
            }
        }

        static bool PlaceTree(Transform parent, string[] kinds, Vector2 p, Random random, bool reserve)
        {
            if (reserve && !IsFree(p, 2.5f))
                return false;
            string kind = kinds[random.Next(kinds.Length)];
            float scale = kind == "Tree" ? S * 0.55f : S * (1.1f + (float)random.NextDouble() * 0.5f);
            var tree = Spawn(kind, parent, p, (float)random.NextDouble() * 360f, scale, 0.1f);
            if (tree == null)
                return false;
            var capsule = tree.AddComponent<CapsuleCollider>(); // 줄기만 막는다
            capsule.center = new Vector3(0f, 6f, 0f);
            capsule.radius = 1.6f;
            capsule.height = 12f;
            if (reserve)
                Taken.Add(new Vector3(p.x, p.y, 2.5f));
            return true;
        }

        static void BuildSmallDecor(Transform parent, Random random)
        {
            string[] tufts = { "Grass", "Grass2", "Flower" };
            int placed = 0, tries = 0;
            while (placed < 150 && tries++ < 600)
            {
                var p = new Vector2(Mathf.Lerp(-56f, 56f, (float)random.NextDouble()), Mathf.Lerp(-52f, 96f, (float)random.NextDouble()));
                if (Mathf.Abs(p.x) < 4f || Vector2.Distance(p, PlayerStart) < 4f)
                    continue;
                if (p.y > 44f && Mathf.Abs(p.x) < 50f && p.y < 90f)
                    continue; // 성 안은 돌바닥
                if (!IsFree(p, 1.5f))
                    continue;
                var tuft = Spawn(tufts[random.Next(tufts.Length)], parent, p, (float)random.NextDouble() * 360f, S * (0.9f + (float)random.NextDouble() * 0.7f), 0.05f);
                if (tuft != null)
                    placed++;
            }
            string[] stones = { "Stone", "Stone2", "Stone3", "Stone4", "Stone5" };
            placed = 0; tries = 0;
            while (placed < 25 && tries++ < 400)
            {
                var p = new Vector2(Mathf.Lerp(-54f, 54f, (float)random.NextDouble()), Mathf.Lerp(-50f, 38f, (float)random.NextDouble()));
                if (Mathf.Abs(p.x) < 4f || !IsFree(p, 2f))
                    continue;
                var stone = Spawn(stones[random.Next(stones.Length)], parent, p, (float)random.NextDouble() * 360f, S * 0.7f, 0.1f);
                if (stone != null)
                    placed++;
            }
        }

        // ───────────── 배경, 벽 ─────────────

        // 바깥 풍경: 언덕·산·하늘·구름은 숲에서 쓴 RPG Poly Pack 것을 다시 쓴다 (같은 하늘, 같은 분위기)
        static void BuildBackdrop(Transform parent, Random random)
        {
            Vector3 center = new Vector3(0f, 0f, 22f);

            var farFloor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            farFloor.name = "FloorFar";
            Object.DestroyImmediate(farFloor.GetComponent<Collider>());
            farFloor.transform.SetParent(parent, false);
            farFloor.transform.position = new Vector3(0f, -0.03f, 22f);
            farFloor.transform.localScale = new Vector3(80f, 1f, 80f);
            farFloor.GetComponent<Renderer>().sharedMaterial = ArtSlots.GetOrCreateMaterial("CastleFloor", new Color32(0x86, 0xA8, 0x4B, 0xFF));

            var hills = new[] { LoadRpg("Nature/Terrain/rpgpp_lt_hill_small_01"), LoadRpg("Nature/Terrain/rpgpp_lt_hill_small_02") };
            for (int i = 0; i < 10; i++)
            {
                float angle = i / 10f * 360f + 12f;
                float radius = 150f + (i % 3) * 18f;
                var hill = Spawn(hills[i % 2], parent, center + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * radius, (float)random.NextDouble() * 360f, 0.9f + (float)random.NextDouble() * 0.35f, 0f);
                hill.transform.position = new Vector3(hill.transform.position.x, 0f, hill.transform.position.z);
            }
            var mountain = LoadRpg("Nature/Terrain/rpgpp_lt_mountain_01");
            for (int i = 0; i < 6; i++)
            {
                float angle = i / 6f * 360f + 40f;
                var m = Spawn(mountain, parent, center + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * (255f + (i % 2) * 40f), (float)random.NextDouble() * 360f, 1f + (float)random.NextDouble() * 0.3f, 0f);
                m.transform.position = new Vector3(m.transform.position.x, 0f, m.transform.position.z);
            }

            var sky = Spawn(LoadRpg("Nature/Sky/rpgpp_lt_sky_01"), parent, new Vector2(center.x, center.z), 0f, 10f, 0f);
            sky.name = "SkyDome";
            sky.transform.position = new Vector3(center.x, -69f, center.z);
            var clouds = new[] { LoadRpg("Nature/Sky/rpgpp_lt_cloud_01"), LoadRpg("Nature/Sky/rpgpp_lt_cloud_02") };
            for (int i = 0; i < 12; i++)
            {
                float angle = i / 12f * 360f + (float)random.NextDouble() * 20f;
                var cloud = Spawn(clouds[i % 2], parent, center + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * (270f + (float)random.NextDouble() * 60f), (float)random.NextDouble() * 360f, 1.5f, 0f);
                cloud.transform.position = new Vector3(cloud.transform.position.x, 80f + (float)random.NextDouble() * 90f, cloud.transform.position.z);
            }
        }

        static void BuildInvisibleWalls(Transform parent)
        {
            Wall(parent, "Wall_North", new Vector3(0f, 5f, 99f), new Vector3(130f, 10f, 2f));
            Wall(parent, "Wall_South", new Vector3(0f, 5f, -57f), new Vector3(130f, 10f, 2f));
            Wall(parent, "Wall_East", new Vector3(60f, 5f, 21f), new Vector3(2f, 10f, 170f));
            Wall(parent, "Wall_West", new Vector3(-60f, 5f, 21f), new Vector3(2f, 10f, 170f));
        }

        static void Wall(Transform parent, string name, Vector3 position, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.AddComponent<BoxCollider>().size = size;
        }

        // ───────────── 물감 지역, 적 스포너 ─────────────

        sealed class ZoneDef
        {
            public readonly string Id, Name;
            public readonly Vector2 Center, Size;
            public readonly PaintColor[] Colors;
            public readonly Rect SpawnArea;
            public readonly (int charger, int shield, int thrower) Enemies;

            public ZoneDef(string id, string name, Vector2 center, Vector2 size, PaintColor[] colors, Rect spawnArea, (int, int, int) enemies)
            {
                Id = id; Name = name; Center = center; Size = size; Colors = colors; SpawnArea = spawnArea; Enemies = enemies;
            }
        }

        static void MakeZone(Transform parent, ZoneDef def)
        {
            var go = new GameObject($"Zone_{def.Id.Substring(def.Id.IndexOf('_') + 1)}");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(def.Center.x, 0f, def.Center.y);
            var zone = go.AddComponent<PaintZone>();
            var so = new SerializedObject(zone);
            so.FindProperty("zoneName").stringValue = def.Name;
            so.FindProperty("size").vector2Value = def.Size;
            var colors = so.FindProperty("colors");
            colors.arraySize = def.Colors.Length;
            for (int i = 0; i < def.Colors.Length; i++)
                colors.GetArrayElementAtIndex(i).enumValueIndex = (int)def.Colors[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AddSpawners(string kind, int amount, ZoneDef zone, Transform parent, Transform enemiesRoot,
            Dictionary<string, GameObject> prefabs, List<Vector2> taken, Dictionary<string, int> counters, Random random)
        {
            for (int i = 0; i < amount; i++)
            {
                if (!TryFindSpot(zone.SpawnArea, taken, random, out Vector3 position))
                {
                    Debug.LogWarning($"[LastBullet] {zone.Name} {kind}: 알맞은 자리를 못 찾았어요");
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

        // 지역 안에서 무작위로 골라, 걸어갈 수 있고(NavMesh), 시작에서 멀고, 다른 스포너와 떨어진 곳을 찾는다
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

        static GameObject LoadRpg(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{RpgRoot}{path}.prefab");
            if (prefab == null)
                Debug.LogError($"[LastBullet] RPG 프리팹을 못 찾음: {path}");
            return prefab;
        }

        static GameObject Load(string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}{name}.prefab");
            if (prefab == null)
                Debug.LogError($"[LastBullet] 에셋 프리팹을 못 찾음: {name}");
            return prefab;
        }

        // 에셋 조각 하나를 놓는다: 모양(Renderer)의 가운데가 center(X,Z)에 오고, 맨 아래가 땅에 닿게 한다.
        static GameObject Piece(string name, Transform parent, Vector2 center, float yaw, Collide collide, float reserve = -1f, float scale = S)
        {
            var prefab = Load(name);
            if (prefab == null)
                return null;
            var go = Spawn(prefab, parent, center, yaw, scale, 0.05f);
            switch (collide)
            {
                case Collide.Mesh: AddMeshColliders(go); break;
                case Collide.Box: AddBoxCollider(go, 0.9f); break;
                case Collide.Capsule: AddThinCapsule(go); break;
            }
            if (reserve < 0f)
                reserve = collide == Collide.None ? 0f : Mathf.Max(1.5f, BoundsOf(go).extents.magnitude * 0.6f);
            if (reserve > 0f)
                Taken.Add(new Vector3(center.x, center.y, reserve));
            return go;
        }

        // 지붕이 없는 집(House2, House4)에 지붕(Roof)을 얹는다. 지붕의 가운데를 집 가운데에 맞추고, 지붕 맨 아래를 집 맨 위에 닿게 한다.
        static void RoofedHouse(string houseName, Transform parent, Vector2 center, float yaw)
        {
            var house = Piece(houseName, parent, center, yaw, Collide.Mesh, 6f);
            var roofPrefab = Load("Roof");
            if (house == null || roofPrefab == null)
                return;
            var houseBounds = BoundsOf(house);
            var roof = (GameObject)PrefabUtility.InstantiatePrefab(roofPrefab, parent);
            roof.name = $"Roof_{houseName}";
            roof.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            roof.transform.localScale = Vector3.one * S * 0.88f; // 집(약 29)보다 지붕(약 35)이 커서 조금 줄인다
            roof.transform.position = Vector3.zero;
            var roofBounds = BoundsOf(roof);
            roof.transform.position = new Vector3(
                houseBounds.center.x - roofBounds.center.x,
                houseBounds.max.y - 0.05f - roofBounds.min.y,
                houseBounds.center.z - roofBounds.center.z);
            AddMeshColliders(roof);
        }

        static GameObject Spawn(string name, Transform parent, Vector2 center, float yaw, float scale, float embed)
        {
            var prefab = Load(name);
            return prefab == null ? null : Spawn(prefab, parent, center, yaw, scale, embed);
        }

        static GameObject Spawn(GameObject prefab, Transform parent, Vector2 center, float yaw, float scale, float embed)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one * scale;
            go.transform.position = Vector3.zero;
            var bounds = BoundsOf(go);
            // 모양의 가운데(X,Z)를 center로, 맨 아래(Y)를 땅(0)보다 embed만큼 아래로
            go.transform.position = new Vector3(center.x - bounds.center.x, -embed - bounds.min.y, center.y - bounds.center.z);
            return go;
        }

        static GameObject Spawn(GameObject prefab, Transform parent, Vector3 position, float yaw, float scale, float embed)
        {
            var go = Spawn(prefab, parent, new Vector2(position.x, position.z), yaw, scale, embed);
            return go;
        }

        static Bounds BoundsOf(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
                bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        // 모양(MeshFilter)이 있는 모든 자식에 모양을 따라가는 충돌을 붙인다 (성문 구멍으로 지나갈 수 있다)
        static void AddMeshColliders(GameObject go)
        {
            foreach (var filter in go.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh != null && filter.GetComponent<Collider>() == null)
                    filter.gameObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
            }
        }

        // 프리팹 모양을 감싸는 상자 충돌 (회전해도 로컬 축 기준으로 모양을 따라간다)
        static void AddBoxCollider(GameObject go, float shrink)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
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

        // 가로등처럼 가늘고 긴 것: 아래쪽 기둥만 막는다
        static void AddThinCapsule(GameObject go)
        {
            var bounds = BoundsOf(go);
            var capsule = go.AddComponent<CapsuleCollider>();
            capsule.center = go.transform.InverseTransformPoint(new Vector3(bounds.center.x, bounds.min.y + bounds.size.y * 0.5f, bounds.center.z));
            capsule.radius = 0.35f / go.transform.lossyScale.x;
            capsule.height = bounds.size.y / go.transform.lossyScale.y;
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
