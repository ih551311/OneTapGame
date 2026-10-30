using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 미술관(1_Museum)을 "오래된 작은 미술관"으로 꾸민다 (2026-10-30 사용자 요청: 옛날 에셋 Old LowPoly premium Furniture).
    // 예전 현대식 침실 에셋으로 꾸민 MuseumHall을 지우고 다시 만든다.
    //  - 건물: 어두운 판벽·나무 바닥·어두운 천장 (가로 36m × 세로 23m × 높이 4.5m, 막힌 전시장)
    //  - 조명: 천장에 매달린 등 8개(따뜻한 불빛) + 액자마다 위에서 비추는 따뜻한 스포트라이트, 어둡고 따뜻한 주변광
    //  - 가구(에셋): 옷장 4개(진열장 대신), 유리 탁자 2개와 의자, 액자 앞 의자 줄, 붉은 카펫
    // 에셋의 8K 텍스처 폴더(천장 조명·벽 조명·업데이트 의자)는 GitHub 한도 때문에 쓰지 않고, 천장 등은 기본 도형으로 만든다.
    // 바닥·벽 그림은 임시(Assets/Textures/Museum)라서 나중에 직접 그린 것으로 바꿔 끼울 수 있다.
    // 메뉴: LastBullet > Phase 12 오래된 미술관 만들기
    public static class Phase12OldMuseumSetup
    {
        const string Furniture = "Assets/Creation_Forge Projects/Old Furniture/";
        const float XMin = -18f, XMax = 18f, ZMin = -8f, ZMax = 15f;
        const float Height = 4.5f;
        const float WallThickness = 0.4f;

        [MenuItem("LastBullet/Phase 12 오래된 미술관 만들기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/1_Museum.unity");
            Object.DestroyImmediate(GameObject.Find("MuseumHall"));

            ConvertStandardMaterials();

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
            Debug.Log("[LastBullet] Phase 12: 미술관을 오래된 미술관으로 다시 꾸미고 1_Museum 씬을 저장했어요.");
        }

        // ───────────── 건물 ─────────────

        static void BuildShell(Transform parent)
        {
            // 바닥: 어두운 나무 판 (큰 평면 하나, 위의 그림은 2m마다 반복)
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.SetParent(parent, false);
            floor.transform.position = new Vector3(0f, 0.01f, (ZMin + ZMax) / 2f);
            floor.transform.localScale = new Vector3(4.2f, 1f, 2.8f); // 42m × 28m (벽 아래까지 덮는다)
            floor.GetComponent<Renderer>().sharedMaterial = TexturedMaterial("MuseumFloor_Old", "Assets/Textures/Museum/Museum_Floor_Planks.png", new Vector2(21f, 14f), 0.3f);

            // 벽: 어두운 판벽
            var wallLong = TexturedMaterial("MuseumWall_Old_Long", "Assets/Textures/Museum/Museum_Wall_Panels.png", new Vector2(9.2f, 1.1f), 0.15f);
            var wallShort = TexturedMaterial("MuseumWall_Old_Short", "Assets/Textures/Museum/Museum_Wall_Panels.png", new Vector2(6f, 1.1f), 0.15f);
            float width = XMax - XMin, depth = ZMax - ZMin;
            Cube("Wall_North", parent, new Vector3(0f, Height / 2f, ZMax + WallThickness / 2f), new Vector3(width + WallThickness * 2f, Height, WallThickness), wallLong);
            Cube("Wall_South", parent, new Vector3(0f, Height / 2f, ZMin - WallThickness / 2f), new Vector3(width + WallThickness * 2f, Height, WallThickness), wallLong);
            Cube("Wall_West", parent, new Vector3(XMin - WallThickness / 2f, Height / 2f, (ZMin + ZMax) / 2f), new Vector3(WallThickness, Height, depth), wallShort);
            Cube("Wall_East", parent, new Vector3(XMax + WallThickness / 2f, Height / 2f, (ZMin + ZMax) / 2f), new Vector3(WallThickness, Height, depth), wallShort);

            // 천장: 어두운 나무색 (그림자는 만들지 않는다: 천장이 해를 막아 실내가 새까매지지 않게)
            var ceiling = Cube("Ceiling", parent, new Vector3(0f, Height + 0.1f, (ZMin + ZMax) / 2f), new Vector3(width + WallThickness * 2f, 0.2f, depth + WallThickness * 2f),
                SolidMaterial("MuseumCeiling", new Color32(0x3A, 0x2A, 0x1E, 0xFF)));
            ceiling.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // ───────────── 가구와 장식 ─────────────

        static void BuildDecor(Transform parent)
        {
            // 붉은 카펫: 입구에서 액자 쪽으로 가운데를 가로지른다 (눈에 보이는 장식, 충돌 없음)
            var carpet = Cube("Carpet", parent, new Vector3(0f, 0.025f, 3f), new Vector3(3.6f, 0.03f, 20f), SolidMaterial("MuseumCarpet", new Color32(0x5C, 0x1F, 0x22, 0xFF)));
            Object.DestroyImmediate(carpet.GetComponent<Collider>());

            // 옷장: 양옆 벽을 따라 진열장처럼 (크기 2.2 × 3.2 × 1.4m)
            var wardrobe = Furniture + "old rustic wardrobe/old rustic wardrobe 1.prefab";
            Vector2[] west = { new Vector2(-17.2f, -3f), new Vector2(-17.2f, 4f), new Vector2(-17.2f, 11f) };
            Vector2[] east = { new Vector2(17.2f, -3f), new Vector2(17.2f, 4f), new Vector2(17.2f, 11f) };
            foreach (var p in west) AddBox(Place(wardrobe, parent, p, 90f, 1f), 0.95f).name = "Wardrobe_West";
            foreach (var p in east) AddBox(Place(wardrobe, parent, p, -90f, 1f), 0.95f).name = "Wardrobe_East";

            // 유리 탁자와 의자: 액자 앞 양쪽에 앉아서 쉬는 자리
            var table = Furniture + "old rustic glass table/Table Updates/Updated Table 1.prefab";
            var chair = Furniture + "Old Seat/oldChair 1.prefab";
            var straw = Furniture + "Old Staw Chair/Old Rustic Straw chair.prefab";
            foreach (float side in new[] { -1f, 1f })
            {
                var center = new Vector2(side * 9f, 7.5f);
                AddBox(Place(table, parent, center, 0f, 1f), 0.95f).name = "GlassTable";
                Place(chair, parent, center + new Vector2(-0.8f, 1.5f), 180f, 1f).name = "OldChair";
                Place(chair, parent, center + new Vector2(0.8f, 1.5f), 180f, 1f).name = "OldChair";
                Place(straw, parent, center + new Vector2(-0.8f, -1.5f), 0f, 1f).name = "StrawChair";
                Place(straw, parent, center + new Vector2(0.8f, -1.5f), 0f, 1f).name = "StrawChair";
            }
            // (2026-10-31 사용자 요청: 액자 앞 의자 줄은 액자를 가려서 없앴다)
        }

        // 액자 9개를 북쪽 벽에 붙인다 (두께 0.15의 뒷면이 벽에 닿게)
        static void HangFrames()
        {
            var frames = GameObject.Find("PictureFrames");
            if (frames == null)
                return;
            foreach (Transform frame in frames.transform)
                frame.position = new Vector3(frame.position.x, frame.position.y, ZMax - 0.09f);
        }

        // ───────────── 조명 ─────────────

        static void BuildLights(Transform parent)
        {
            var shade = EmissiveMaterial("MuseumLampShade", new Color(1f, 0.72f, 0.38f), 2.4f);
            var cord = SolidMaterial("MuseumLampCord", new Color32(0x1E, 0x16, 0x10, 0xFF));

            // 천장에 매달린 등 8개: 줄 + 갓(납작한 공) + 따뜻한 점광원
            for (int ix = 0; ix < 4; ix++)
            {
                for (int iz = 0; iz < 2; iz++)
                {
                    var origin = new Vector3(-13.5f + 9f * ix, 0f, 1f + 8f * iz);
                    float cordLength = Height - 3.3f;
                    var cordObject = Cylinder("LampCord", parent, new Vector3(origin.x, Height - cordLength / 2f, origin.z), new Vector3(0.04f, cordLength / 2f, 0.04f), cord);
                    Object.DestroyImmediate(cordObject.GetComponent<Collider>());
                    var lamp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    lamp.name = "LampShade";
                    Object.DestroyImmediate(lamp.GetComponent<Collider>());
                    lamp.transform.SetParent(parent, false);
                    lamp.transform.position = new Vector3(origin.x, 3.3f, origin.z);
                    lamp.transform.localScale = new Vector3(0.8f, 0.4f, 0.8f);
                    lamp.GetComponent<Renderer>().sharedMaterial = shade;
                    lamp.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    AddLight($"LampLight_{ix}{iz}", parent, new Vector3(origin.x, 3.0f, origin.z), LightType.Point, 1.5f, 10f, Quaternion.identity, new Color(1f, 0.76f, 0.46f));
                }
            }

            // 액자마다 위에서 비추는 따뜻한 스포트라이트
            var frames = GameObject.Find("PictureFrames");
            if (frames == null)
                return;
            int n = 0;
            foreach (Transform frame in frames.transform)
            {
                var position = new Vector3(frame.position.x, Height - 0.3f, ZMax - 2.4f);
                var target = new Vector3(frame.position.x, 1.7f, ZMax - 0.1f);
                var spot = AddLight($"FrameLight_{++n:00}", parent, position, LightType.Spot, 5f, 7f, Quaternion.LookRotation(target - position), new Color(1f, 0.86f, 0.62f));
                spot.spotAngle = 65f;
            }
        }

        static Light AddLight(string name, Transform parent, Vector3 position, LightType type, float intensity, float range, Quaternion rotation, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, rotation);
            var light = go.AddComponent<Light>();
            light.type = type;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
            return light;
        }

        // 어둡고 따뜻한 실내 분위기: 주변광을 낮춘 갈색 톤, 해는 거의 끈다
        static void SetupAmbient()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.40f, 0.33f, 0.27f);
            RenderSettings.ambientEquatorColor = new Color(0.34f, 0.27f, 0.21f);
            RenderSettings.ambientGroundColor = new Color(0.22f, 0.17f, 0.12f);
            RenderSettings.ambientIntensity = 1f;
            var sun = GameObject.Find("Directional Light");
            if (sun != null)
                sun.GetComponent<Light>().intensity = 0.15f;
        }

        // ───────────── 머티리얼 ─────────────

        // 에셋 폴더에서 아직 Standard(기본 파이프라인) 셰이더인 머티리얼(유리 탁자)을 URP로 바꾼다
        static void ConvertStandardMaterials()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { Furniture + "old rustic glass table", Furniture + "old rustic wardrobe", Furniture + "Old Seat", Furniture + "Old Staw Chair" }))
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (material == null || material.shader.name.StartsWith("Universal"))
                    continue;
                var texture = material.HasProperty("_MainTex") ? material.GetTexture("_MainTex") : null;
                var color = material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
                var normal = material.HasProperty("_BumpMap") ? material.GetTexture("_BumpMap") : null;
                material.shader = Shader.Find("Universal Render Pipeline/Lit");
                material.SetTexture("_BaseMap", texture);
                material.SetColor("_BaseColor", color);
                if (normal != null)
                {
                    material.SetTexture("_BumpMap", normal);
                    material.EnableKeyword("_NORMALMAP");
                }
                material.SetFloat("_Smoothness", 0.3f);
                EditorUtility.SetDirty(material);
            }
        }

        static Material TexturedMaterial(string name, string texturePath, Vector2 tiling, float smoothness)
        {
            string path = $"Assets/Materials/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            material.SetTextureScale("_BaseMap", tiling);
            material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material SolidMaterial(string name, Color color)
        {
            string path = $"Assets/Materials/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetTexture("_BaseMap", null);
            material.SetTextureScale("_BaseMap", Vector2.one);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.2f);
            material.DisableKeyword("_EMISSION");
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material EmissiveMaterial(string name, Color color, float strength)
        {
            var material = SolidMaterial(name, color);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * strength);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(material);
            return material;
        }

        // ───────────── 도우미 ─────────────

        static Transform Group(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
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

        static GameObject Cylinder(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        // 프리팹을 놓는다: 모양의 가운데(X,Z)를 center에, 맨 아래를 바닥(0.01)에 맞춘다
        static GameObject Place(string prefabPath, Transform parent, Vector2 center, float yaw, float scale)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[LastBullet] 프리팹을 못 찾음: {prefabPath}");
                return new GameObject("MISSING");
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one * scale;
            go.transform.position = Vector3.zero;
            var bounds = BoundsOf(go);
            go.transform.position = new Vector3(center.x - bounds.center.x, 0.01f - bounds.min.y, center.y - bounds.center.z);
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

        // 모양을 감싸는 상자 충돌 (회전해도 로컬 축 기준). 같은 오브젝트를 돌려준다.
        static GameObject AddBox(GameObject go, float shrink)
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
            return go;
        }
    }
}
