using LastBullet.Stage;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 미술관(1_Museum)을 진짜 미술관처럼 꾸민다 (2026-10-30 사용자 요청, 에셋 Minimalist ArchViz Bedroom 사용).
    //  - 건물: 흰 벽 + 나무 바닥 + 천장 (가로 36m × 세로 23m × 높이 4.5m, 완전히 막힌 전시장)
    //  - 북쪽 벽에 액자 9개를 걸고, 액자마다 위에서 비추는 조명
    //  - 장식: 카펫, 서 있는 조명, 화분, 받침대 위 조각, 벤치, 벽에 건 그림·거울, 안내 화면
    //  - 실내 조명: 천장 불빛 판 + 점광원, 밝은 주변광
    // 에셋은 침실 용품이라 크기가 작은 것(조각 0.1~0.2m)은 키워서 쓴다. 기존 작업대·지도 탁자·액자는 위치를 그대로 둔다 (액자만 벽에 붙인다).
    // 메뉴: LastBullet > Phase 12 미술관 꾸미기
    public static class Phase12MuseumDecorSetup
    {
        const string Root = "Assets/MinimalistBedroom/Prefabs/";
        const float XMin = -18f, XMax = 18f, ZMin = -8f, ZMax = 15f;
        const float Height = 4.5f;
        const float WallThickness = 0.4f;

        [MenuItem("LastBullet/Phase 12 미술관 꾸미기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/1_Museum.unity");
            if (GameObject.Find("MuseumHall") != null)
            {
                EditorUtility.DisplayDialog("LastBullet", "이미 MuseumHall이 있어요.", "확인");
                return;
            }

            var hall = new GameObject("MuseumHall").transform;
            var shell = Group("Shell", hall);
            var decor = Group("Decor", hall);
            var lights = Group("Lights", hall);

            BuildShell(shell);
            BuildDecor(decor);
            HangFrames();
            BuildLights(lights);
            SetupAmbient();

            Selection.activeGameObject = hall.gameObject;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[LastBullet] Phase 12: 미술관을 전시장으로 꾸미고 1_Museum 씬을 저장했어요.");
        }

        // ───────────── 건물 (바닥, 벽, 천장) ─────────────

        static void BuildShell(Transform parent)
        {
            // 바닥: 에셋의 나무 바닥 조각(8.11 × 5.08m)을 5 × 5로 깐다
            var floorPrefab = Load("Exterior/Floor_01");
            for (int ix = 0; ix < 5; ix++)
            {
                for (int iz = 0; iz < 5; iz++)
                {
                    var tile = Place(floorPrefab, parent, new Vector2(-16.2f + 8.1f * ix, -7.6f + 5.07f * iz), 0f, 1f, 0.01f);
                    tile.name = $"FloorTile_{ix}{iz}";
                    AddBox(tile, 1f);
                }
            }

            // 벽: 흰 회반죽 (에셋의 회반죽 그림을 복사해 전용 머티리얼로 만든다. 길이에 맞춰 타일 수를 다르게)
            var wallLong = WallMaterial("MuseumWall_Long", new Vector2(12f, 2f));
            var wallShort = WallMaterial("MuseumWall_Short", new Vector2(8f, 2f));
            float width = XMax - XMin, depth = ZMax - ZMin;
            Cube("Wall_North", parent, new Vector3(0f, Height / 2f, ZMax + WallThickness / 2f), new Vector3(width + WallThickness * 2f, Height, WallThickness), wallLong);
            Cube("Wall_South", parent, new Vector3(0f, Height / 2f, ZMin - WallThickness / 2f), new Vector3(width + WallThickness * 2f, Height, WallThickness), wallLong);
            Cube("Wall_West", parent, new Vector3(XMin - WallThickness / 2f, Height / 2f, (ZMin + ZMax) / 2f), new Vector3(WallThickness, Height, depth), wallShort);
            Cube("Wall_East", parent, new Vector3(XMax + WallThickness / 2f, Height / 2f, (ZMin + ZMax) / 2f), new Vector3(WallThickness, Height, depth), wallShort);

            // 천장 (그림자는 만들지 않는다: 해(Directional Light)가 천장에 막혀 실내가 까맣게 되지 않도록)
            var ceiling = Cube("Ceiling", parent, new Vector3(0f, Height + 0.1f, (ZMin + ZMax) / 2f), new Vector3(width + WallThickness * 2f, 0.2f, depth + WallThickness * 2f),
                ArtSlots.GetOrCreateMaterial("MuseumCeiling", new Color32(0xF2, 0xF0, 0xEC, 0xFF)));
            ceiling.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // 천장의 불빛 판 (켜진 것처럼 보이는 긴 판)
            var panel = EmissiveMaterial("MuseumLightPanel");
            for (int ix = 0; ix < 3; ix++)
            {
                for (int iz = 0; iz < 2; iz++)
                {
                    var p = Cube($"LightPanel_{ix}{iz}", parent, new Vector3(-10f + 10f * ix, Height - 0.02f, 1f + 8f * iz), new Vector3(6f, 0.05f, 0.5f), panel);
                    Object.DestroyImmediate(p.GetComponent<Collider>());
                    p.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }
        }

        // ───────────── 장식 ─────────────

        static void BuildDecor(Transform parent)
        {
            var plinthMaterial = ArtSlots.GetOrCreateMaterial("MuseumPlinth", new Color32(0xE8, 0xE4, 0xDC, 0xFF));

            // 카펫: 가운데 장치(지도 탁자·작업대) 아래
            var rug = Place(Load("Interior/Rug_01"), parent, new Vector2(0f, 4f), 0f, 1.6f, 0f);
            rug.name = "Rug_Center";

            // 서 있는 조명 (1.7m)
            foreach (var p in new[] { new Vector2(-16.6f, -6.5f), new Vector2(16.6f, -6.5f), new Vector2(-16.6f, 13.6f), new Vector2(16.6f, 13.6f), new Vector2(-9f, 2f), new Vector2(9f, 2f) })
                AddBox(Place(Load("Interior/StandingLamp_01"), parent, p, 0f, 1.15f, 0f), 0.8f);

            // 화분: 모서리와 액자 양옆
            foreach (var p in new[] { new Vector2(-17f, -7f), new Vector2(17f, -7f), new Vector2(-17f, 14f), new Vector2(17f, 14f), new Vector2(-15.5f, 5f), new Vector2(15.5f, 5f) })
            {
                var pot = Place(Load("Interior/Pot_a_01"), parent, p, 0f, 1.5f, 0f);
                AddBox(pot, 1f);
                var plant = Place(Load("Interior/Plant_a_01"), parent, p, 40f * (p.x + p.y), 1.5f, 0f);
                plant.transform.position += Vector3.up * BoundsOf(pot).size.y * 0.85f; // 화분 위에 얹는다
            }

            // 받침대 위 조각 (조각은 침실 장식이라 작아서 6배로 키운다)
            string[] sculptures = { "Interior/Sculpture_a_01", "Interior/Sculpture_b_01", "Interior/Sculpture_c_01", "Interior/Sculpture_e_01" };
            Vector2[] spots = { new Vector2(-12f, -3.5f), new Vector2(12f, -3.5f), new Vector2(-12f, 9f), new Vector2(12f, 9f) };
            for (int i = 0; i < spots.Length; i++)
            {
                var plinth = Cube($"Plinth_{i + 1}", parent, new Vector3(spots[i].x, 0.5f, spots[i].y), new Vector3(0.9f, 1f, 0.9f), plinthMaterial);
                var sculpture = Place(Load(sculptures[i]), parent, spots[i], 30f * i, 6f, 0f);
                sculpture.transform.position += Vector3.up * 1f;
                sculpture.name = $"Sculpture_{i + 1}";
            }

            // 벤치: 액자 앞에서 감상하는 자리 (낮은 나무 선반 TvStand를 벤치로 쓴다)
            var bench1 = Place(Load("Interior/TvStand_01"), parent, new Vector2(-6.4f, 10f), 90f, 1f, 0f);
            var bench2 = Place(Load("Interior/TvStand_01"), parent, new Vector2(6.4f, 10f), 90f, 1f, 0f);
            bench1.name = "Bench_1"; bench2.name = "Bench_2";
            AddBox(bench1, 1f); AddBox(bench2, 1f);

            // 벽에 건 그림 (옆벽 3개씩)과 거울 (남쪽 벽)
            for (int i = 0; i < 3; i++)
            {
                float z = -3f + 7f * i;
                var west = Place(Load("Interior/Painting_01"), parent, new Vector2(XMin + 0.05f, z), 0f, 1.5f, 0f);
                west.transform.position = new Vector3(west.transform.position.x, 1.9f, west.transform.position.z);
                west.transform.position += Vector3.up * 0f;
                var east = Place(Load("Interior/Painting_01"), parent, new Vector2(XMax - 0.05f, z), 180f, 1.5f, 0f);
                east.transform.position = new Vector3(east.transform.position.x, 1.9f, east.transform.position.z);
                west.name = $"Painting_West_{i + 1}"; east.name = $"Painting_East_{i + 1}";
            }
            foreach (float x in new[] { -10f, 10f })
            {
                var mirror = Place(Load("Interior/WallMirror_01"), parent, new Vector2(x, ZMin + 0.05f), 90f, 1.3f, 0f);
                mirror.name = "WallMirror";
            }

            // 안내 화면: 입구(남쪽 벽) 가운데 낮은 선반 위에 TV
            var stand = Place(Load("Interior/TvStand_01"), parent, new Vector2(0f, ZMin + 0.4f), 90f, 1.3f, 0f);
            stand.name = "InfoStand";
            AddBox(stand, 1f);
            var tv = Place(Load("Interior/Tv_01"), parent, new Vector2(0f, ZMin + 0.4f), 90f, 1.3f, 0f);
            tv.transform.position += Vector3.up * BoundsOf(stand).size.y;
            tv.name = "InfoScreen";

            // 한쪽 벽의 진열장 (옷장을 진열장으로 쓴다)
            var cabinet = Place(Load("Interior/Cupboard_a_01"), parent, new Vector2(XMax - 1.1f, 11f), 180f, 1f, 0f);
            cabinet.name = "DisplayCabinet";
            AddBox(cabinet, 0.95f);
        }

        // 액자 9개를 북쪽 벽에 붙이고, 액자마다 위에서 비추는 조명을 단다
        static void HangFrames()
        {
            var frames = GameObject.Find("PictureFrames");
            if (frames == null)
                return;
            foreach (Transform frame in frames.transform)
                frame.position = new Vector3(frame.position.x, frame.position.y, ZMax - 0.09f); // 두께 0.15의 뒷면이 벽에 닿게
        }

        // ───────────── 조명 ─────────────

        static void BuildLights(Transform parent)
        {
            // 실내 전체: 천장 아래 점광원 8개 (그림자 없음)
            for (int ix = 0; ix < 4; ix++)
            {
                for (int iz = 0; iz < 2; iz++)
                    AddLight($"RoomLight_{ix}{iz}", parent, new Vector3(-13.5f + 9f * ix, Height - 0.6f, 1f + 8f * iz), LightType.Point, 1.6f, 13f, Quaternion.identity);
            }

            // 액자마다 위에서 비추는 스포트라이트
            var frames = GameObject.Find("PictureFrames");
            if (frames == null)
                return;
            int i = 0;
            foreach (Transform frame in frames.transform)
            {
                var position = new Vector3(frame.position.x, Height - 0.3f, ZMax - 2.4f);
                var target = new Vector3(frame.position.x, 1.7f, ZMax - 0.1f);
                var spot = AddLight($"FrameLight_{++i:00}", parent, position, LightType.Spot, 4f, 7f, Quaternion.LookRotation(target - position));
                spot.spotAngle = 75f;
            }
        }

        static Light AddLight(string name, Transform parent, Vector3 position, LightType type, float intensity, float range, Quaternion rotation)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, rotation);
            var light = go.AddComponent<Light>();
            light.type = type;
            light.color = new Color(1f, 0.96f, 0.9f);
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
            return light;
        }

        // 실내가 어둡지 않게 주변광을 밝게, 해는 약하게 (천장 때문에 거의 들어오지 않는다)
        static void SetupAmbient()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.85f, 0.86f, 0.9f);
            RenderSettings.ambientEquatorColor = new Color(0.8f, 0.79f, 0.76f);
            RenderSettings.ambientGroundColor = new Color(0.62f, 0.58f, 0.52f);
            RenderSettings.ambientIntensity = 1f;
            var sun = GameObject.Find("Directional Light");
            if (sun != null)
                sun.GetComponent<Light>().intensity = 0.4f;
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
                Debug.LogError($"[LastBullet] 에셋 프리팹을 못 찾음: {path}");
            return prefab;
        }

        static GameObject Cube(string name, Transform parent, Vector3 position, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        // 에셋 회반죽 머티리얼을 복사해 우리 전용 머티리얼로 만든다 (이미 있으면 그대로 쓴다)
        static Material WallMaterial(string name, Vector2 tiling)
        {
            string path = $"Assets/Materials/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
                return existing;
            var source = AssetDatabase.LoadAssetAtPath<Material>("Assets/MinimalistBedroom/Materials/Misc/Plaster_1k/Plaster_1k_01.mat");
            var material = new Material(source);
            material.SetTextureScale("_BaseMap", tiling);
            material.SetTextureScale("_BumpMap", tiling);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static Material EmissiveMaterial(string name)
        {
            string path = $"Assets/Materials/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
                return existing;
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", Color.white);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(1f, 0.97f, 0.88f) * 2.2f);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        // 프리팹 조각을 놓는다: 모양의 가운데(X,Z)를 center에, 맨 아래를 floorY에 맞춘다
        static GameObject Place(GameObject prefab, Transform parent, Vector2 center, float yaw, float scale, float floorY)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one * scale;
            go.transform.position = Vector3.zero;
            var bounds = BoundsOf(go);
            go.transform.position = new Vector3(center.x - bounds.center.x, floorY - bounds.min.y, center.y - bounds.center.z);
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

        // 모양을 감싸는 상자 충돌 (회전해도 로컬 축 기준)
        static void AddBox(GameObject go, float shrink)
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
    }
}
