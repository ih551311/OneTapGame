using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastBullet.EditorTools
{
    // 미술관 분위기: 천장 등에서 내려오는 빛 줄기와 떠다니는 먼지 (2026-11-05 사용자 요청).
    //   그림·머티리얼·모양 (이미 있으면 덮어쓰지 않는다 — 사용자가 바꾼 것을 지키려고):
    //     Assets/Effects/LightShaft.png   빛 줄기 그림 (위가 밝고 아래로 사라짐, 흰색)
    //     Assets/Effects/Dust.png         먼지 알갱이 그림 (부드러운 동그라미)
    //     Assets/Materials/LightShaft.mat, Dust.mat   전용 머티리얼 (밝게 겹쳐 보이는 방식)
    //     Assets/Models/LightShaftCone.asset          빛 줄기 원뿔 모양
    //   씬 1_Museum: Atmosphere/LightShafts 아래에 천장 등(LampLight_*)마다 원뿔 하나, Atmosphere/DustMotes 파티클 하나.
    // 이미 만들었으면 지우고 다시 만든다. 메뉴: LastBullet > Phase 69 미술관 빛 줄기와 먼지 만들기
    public static class Phase69MuseumAtmosphereSetup
    {
        const string ShaftPng = "Assets/Effects/LightShaft.png";
        const string DustPng = "Assets/Effects/Dust.png";
        const string ConePath = "Assets/Models/LightShaftCone.asset";

        [MenuItem("LastBullet/Phase 69 미술관 빛 줄기와 먼지 만들기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            Directory.CreateDirectory("Assets/Effects");
            Directory.CreateDirectory("Assets/Models");
            CreatePngs();
            AssetDatabase.Refresh();

            var shaftMaterial = GetMaterial("LightShaft", AssetDatabase.LoadAssetAtPath<Texture2D>(ShaftPng), new Color(1f, 0.82f, 0.55f, 0.075f), true);
            var dustMaterial = GetMaterial("Dust", AssetDatabase.LoadAssetAtPath<Texture2D>(DustPng), new Color(1f, 0.92f, 0.75f, 0.55f), false);
            var cone = GetCone();

            var scene = EditorSceneManager.OpenScene("Assets/Scenes/1_Museum.unity");
            var old = GameObject.Find("Atmosphere");
            if (old != null)
                Object.DestroyImmediate(old);
            RaiseLamps();
            var root = new GameObject("Atmosphere").transform;

            // 빛 줄기: 천장 등마다 아래로 퍼지는 원뿔
            var shafts = new GameObject("LightShafts").transform;
            shafts.SetParent(root, false);
            int count = 0;
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (!light.name.StartsWith("LampLight_"))
                    continue;
                var go = new GameObject("Shaft_" + light.name.Substring("LampLight_".Length), typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(shafts, false);
                go.transform.position = new Vector3(light.transform.position.x, 0f, light.transform.position.z); // 높이는 모양이 정한다
                go.GetComponent<MeshFilter>().sharedMesh = cone;
                var renderer = go.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = shaftMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                // 줄기는 전등 갓(불빛보다 0.3m 위)에서 시작해 바닥까지 내려온다
                go.transform.localScale = new Vector3(1f, Mathf.Max(1f, light.transform.position.y + LampShadeAboveLight) / 3f, 1f);
                count++;
            }

            // 먼지: 전시장 안을 천천히 떠다닌다
            var dustGo = new GameObject("DustMotes", typeof(ParticleSystem));
            dustGo.transform.SetParent(root, false);
            dustGo.transform.position = new Vector3(0f, 2.2f, 3.5f);
            var dust = dustGo.GetComponent<ParticleSystem>();
            dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = dust.main;
            main.loop = true;
            main.prewarm = true;
            main.duration = 10f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(10f, 18f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.06f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.92f, 0.75f, 0.25f), new Color(1f, 0.85f, 0.6f, 0.6f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 220;
            var emission = dust.emission;
            emission.rateOverTime = 14f;
            var shape = dust.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(34f, 3.6f, 21f);
            var noise = dust.noise;
            noise.enabled = true;
            noise.strength = 0.12f;
            noise.frequency = 0.15f;
            noise.scrollSpeed = 0.05f;
            var velocity = dust.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.03f, 0.03f);
            velocity.y = new ParticleSystem.MinMaxCurve(-0.02f, 0.05f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.03f, 0.03f);
            var fade = dust.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;
            var dustRenderer = dustGo.GetComponent<ParticleSystemRenderer>();
            dustRenderer.sharedMaterial = dustMaterial;
            dustRenderer.shadowCastingMode = ShadowCastingMode.Off;
            dustRenderer.receiveShadows = false;
            dust.Play();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[LastBullet] 1_Museum: 빛 줄기 {count}개와 먼지를 만들었어요.");
        }

        const float LampShadeAboveLight = 0.3f;
        const float LampRaise = 0.6f;

        // 천장 등을 LampRaise만큼 올린다 (2026-11-05 사용자 요청: 빛 줄기가 어색하니 전등을 조금 올려 달라).
        // 불빛(LampLight_*)과 갓(LampShade)은 그대로 올리고, 줄(LampCord)은 천장에 붙은 위쪽 끝은 두고 아래만 짧게 한다.
        // 이미 올렸으면(불빛 높이가 3.2m 이상) 다시 올리지 않는다.
        static void RaiseLamps()
        {
            var lights = GameObject.Find("Lights");
            if (lights == null)
                return;
            foreach (var light in lights.GetComponentsInChildren<Light>(true))
            {
                if (!light.name.StartsWith("LampLight_") || light.transform.position.y >= 3.2f)
                    continue;
                Vector3 at = light.transform.position;
                light.transform.position += Vector3.up * LampRaise;
                foreach (Transform sibling in light.transform.parent)
                {
                    if (Mathf.Abs(sibling.position.x - at.x) > 0.05f || Mathf.Abs(sibling.position.z - at.z) > 0.05f)
                        continue;
                    if (sibling.name == "LampShade")
                        sibling.position += Vector3.up * LampRaise;
                    else if (sibling.name == "LampCord")
                        ShortenCordBottom(sibling);
                }
            }
        }

        static void ShortenCordBottom(Transform cord)
        {
            var renderer = cord.GetComponent<Renderer>();
            if (renderer == null)
                return;
            Bounds before = renderer.bounds;
            float top = before.max.y;
            float newLength = Mathf.Max(0.1f, before.size.y - LampRaise);
            var scale = cord.localScale;
            scale.y *= newLength / before.size.y;
            cord.localScale = scale;
            cord.position += Vector3.up * (top - renderer.bounds.max.y); // 위쪽 끝(천장)은 그대로
        }

        // 위(좁음)에서 아래(넓음)로 퍼지는 속이 빈 원뿔. 세로 uv = 위 1 → 아래 0 (그림은 위가 밝다). 높이 3m, 바닥 반지름 1.5m.
        static Mesh GetCone()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(ConePath);
            if (existing != null)
                return existing;
            const int Segments = 24;
            const float Height = 3f, TopRadius = 0.22f, BottomRadius = 1.25f;
            var vertices = new Vector3[(Segments + 1) * 2];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[Segments * 6 * 2];
            for (int i = 0; i <= Segments; i++)
            {
                float a = i / (float)Segments * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                vertices[i] = dir * TopRadius + Vector3.up * Height;
                vertices[Segments + 1 + i] = dir * BottomRadius;
                uvs[i] = new Vector2(i / (float)Segments, 1f);
                uvs[Segments + 1 + i] = new Vector2(i / (float)Segments, 0f);
            }
            int t = 0;
            for (int i = 0; i < Segments; i++)
            {
                int a = i, b = i + 1, c = Segments + 1 + i, d = Segments + 2 + i;
                triangles[t++] = a; triangles[t++] = b; triangles[t++] = c;
                triangles[t++] = b; triangles[t++] = d; triangles[t++] = c;
                triangles[t++] = a; triangles[t++] = c; triangles[t++] = b;
                triangles[t++] = b; triangles[t++] = c; triangles[t++] = d;
            }
            var mesh = new Mesh { name = "LightShaftCone", vertices = vertices, uv = uvs, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, ConePath);
            return mesh;
        }

        static void CreatePngs()
        {
            if (!File.Exists(ShaftPng))
            {
                const int W = 32, H = 128;
                var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < W; x++)
                    {
                        float v = y / (float)(H - 1);                 // 아래 0 → 위 1
                        float fadeDown = Mathf.Pow(v, 1.4f);           // 아래로 갈수록 사라짐
                        float side = 1f - Mathf.Abs(x / (float)(W - 1) - 0.5f) * 0.0f; // 가로는 원뿔이 알아서 둥글다
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, fadeDown * side));
                    }
                tex.Apply();
                File.WriteAllBytes(ShaftPng, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(ShaftPng);
            }
            if (!File.Exists(DustPng))
            {
                const int N = 32;
                var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
                for (int y = 0; y < N; y++)
                    for (int x = 0; x < N; x++)
                    {
                        float dx = (x - (N - 1) / 2f) / (N / 2f), dy = (y - (N - 1) / 2f) / (N / 2f);
                        float r = Mathf.Sqrt(dx * dx + dy * dy);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(1f - Mathf.SmoothStep(0.2f, 1f, r))));
                    }
                tex.Apply();
                File.WriteAllBytes(DustPng, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(DustPng);
            }
        }

        static Material GetMaterial(string name, Texture2D texture, Color color, bool doubleSided)
        {
            string path = $"Assets/Materials/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
                return existing;
            var material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 2f); // 더하기: 밝게 겹쳐 보인다
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.One);
            material.SetFloat("_ZWrite", 0f);
            if (doubleSided)
                material.SetFloat("_Cull", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
