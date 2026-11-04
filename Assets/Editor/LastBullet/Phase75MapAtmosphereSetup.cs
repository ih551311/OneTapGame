using System.Collections.Generic;
using System.IO;
using LastBullet.Stage;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastBullet.EditorTools
{
    // 전투 맵 분위기 입자를 만든다 (2026-11-05 사용자 요청: 미술관처럼 맵에 맞게 먼지 같은 요소).
    //   숲:      낮 꽃가루 + 낙엽 / 밤 반딧불이
    //   성 마을: 낮 먼지 + 꽃잎 / 밤 불씨 + 약한 먼지
    //   사막:    낮 모래바람 + 먼지 안개 / 밤 차가운 모래바람 + 옅은 안개
    //   씬마다 Atmosphere(MapAtmosphere)가 플레이어를 따라다니며, 자식 입자들 중 낮용·밤용 한쪽만 켠다.
    //   그림 (이미 있으면 덮어쓰지 않는다): Assets/Effects/Leaf.png(잎), Glow.png(빛 알갱이), Streak.png(모래바람 줄), Dust.png(미술관 먼지 알갱이와 같이 씀)
    //   머티리얼: Assets/Materials/AtmoMote.mat(보통 섞기 알갱이), AtmoGlow.mat(밝게 겹침), AtmoLeaf.mat(잎·꽃잎), AtmoSandStreak.mat, AtmoSandPuff.mat
    // 메뉴: LastBullet > Phase 75 전투 맵 분위기 입자 만들기 (다시 실행하면 지우고 다시 만든다)
    public static class Phase75MapAtmosphereSetup
    {
        static readonly (string path, string kind)[] Scenes =
        {
            ("Assets/Scenes/2_Forest.unity", "forest"),
            ("Assets/Scenes/3_CastleVillage.unity", "castle"),
            ("Assets/Scenes/4_Desert.unity", "desert"),
        };

        // 입자 한 종류의 설정
        class Spec
        {
            public string name;
            public Material material;
            public float count;            // 동시에 떠 있는 개수(대략)
            public Vector2 lifetime = new Vector2(10f, 16f);
            public Vector2 size = new Vector2(0.04f, 0.08f);
            public Color colorA = Color.white, colorB = Color.white;
            public Vector3 area = new Vector3(44f, 10f, 44f); // 플레이어 둘레에서 입자가 나는 상자 크기
            public float centerY = 4f;                         // 상자 가운데 높이 (플레이어 기준)
            public Vector3 drift;                              // 평균 바람 (m/s)
            public Vector3 driftRandom = new Vector3(0.05f, 0.03f, 0.05f);
            public float noiseStrength = 0.3f, noiseFrequency = 0.12f;
            public float spin;                                 // 빙글 도는 정도 (라디안/초). 0이면 안 돈다
            public bool pulse;                                 // 반짝이며 켜졌다 꺼졌다 (반딧불이)
            public bool stretch;                               // 바람 줄처럼 늘이기
        }

        [MenuItem("LastBullet/Phase 75 전투 맵 분위기 입자 만들기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            Directory.CreateDirectory("Assets/Effects");
            CreatePngs();
            AssetDatabase.Refresh();

            var mote = GetMaterial("AtmoMote", "Assets/Effects/Dust.png", false, false);
            var glow = GetMaterial("AtmoGlow", "Assets/Effects/Glow.png", true, false);
            var leaf = GetMaterial("AtmoLeaf", "Assets/Effects/Leaf.png", false, true);
            var streak = GetMaterial("AtmoSandStreak", "Assets/Effects/Streak.png", false, true);
            var puff = GetMaterial("AtmoSandPuff", "Assets/Effects/Dust.png", false, false);

            foreach (var (path, kind) in Scenes)
            {
                var scene = EditorSceneManager.OpenScene(path);
                var old = GameObject.Find("Atmosphere");
                if (old != null)
                    Object.DestroyImmediate(old);
                var root = new GameObject("Atmosphere");
                var day = new List<GameObject>();
                var night = new List<GameObject>();

                switch (kind)
                {
                    case "forest":
                        day.Add(Make(root.transform, new Spec { name = "Pollen", material = mote, count = 200, size = new Vector2(0.12f, 0.24f),
                            colorA = new Color(1f, 0.93f, 0.45f, 0.6f), colorB = new Color(1f, 0.85f, 0.3f, 0.9f), drift = new Vector3(0.25f, 0.05f, 0.1f), noiseStrength = 0.35f }));
                        day.Add(Make(root.transform, new Spec { name = "Leaves", material = leaf, count = 45, lifetime = new Vector2(9f, 14f), size = new Vector2(0.28f, 0.45f),
                            colorA = new Color(0.45f, 0.65f, 0.2f, 1f), colorB = new Color(0.9f, 0.7f, 0.2f, 1f), area = new Vector3(44f, 4f, 44f), centerY = 9f,
                            drift = new Vector3(0.8f, -0.6f, 0.3f), driftRandom = new Vector3(0.4f, 0.2f, 0.3f), noiseStrength = 0.7f, spin = 2f }));
                        night.Add(Make(root.transform, new Spec { name = "Fireflies", material = glow, count = 90, lifetime = new Vector2(7f, 12f), size = new Vector2(0.35f, 0.6f),
                            colorA = new Color(0.75f, 1f, 0.25f, 1f), colorB = new Color(1f, 0.95f, 0.35f, 1f), area = new Vector3(36f, 4f, 36f), centerY = 2.2f,
                            drift = Vector3.zero, driftRandom = new Vector3(0.15f, 0.1f, 0.15f), noiseStrength = 0.9f, noiseFrequency = 0.25f, pulse = true }));
                        break;
                    case "castle":
                        day.Add(Make(root.transform, new Spec { name = "Dust", material = mote, count = 170, size = new Vector2(0.13f, 0.24f),
                            colorA = new Color(0.8f, 0.68f, 0.45f, 0.55f), colorB = new Color(0.9f, 0.78f, 0.55f, 0.9f), drift = new Vector3(0.2f, 0.02f, 0.1f) }));
                        day.Add(Make(root.transform, new Spec { name = "Petals", material = leaf, count = 32, lifetime = new Vector2(10f, 16f), size = new Vector2(0.2f, 0.33f),
                            colorA = new Color(1f, 0.85f, 0.9f, 1f), colorB = new Color(1f, 1f, 1f, 1f), area = new Vector3(44f, 4f, 44f), centerY = 8f,
                            drift = new Vector3(0.7f, -0.45f, 0.3f), driftRandom = new Vector3(0.3f, 0.15f, 0.3f), noiseStrength = 0.6f, spin = 2.5f }));
                        night.Add(Make(root.transform, new Spec { name = "Embers", material = glow, count = 55, lifetime = new Vector2(6f, 10f), size = new Vector2(0.18f, 0.32f),
                            colorA = new Color(1f, 0.45f, 0.1f, 1f), colorB = new Color(1f, 0.75f, 0.25f, 1f), area = new Vector3(40f, 3f, 40f), centerY = 1.5f,
                            drift = new Vector3(0.1f, 0.5f, 0.05f), driftRandom = new Vector3(0.2f, 0.25f, 0.2f), noiseStrength = 0.5f, pulse = true }));
                        night.Add(Make(root.transform, new Spec { name = "NightDust", material = mote, count = 80, size = new Vector2(0.08f, 0.15f),
                            colorA = new Color(0.7f, 0.8f, 1f, 0.25f), colorB = new Color(0.8f, 0.85f, 1f, 0.4f), drift = new Vector3(0.15f, 0.02f, 0.08f) }));
                        break;
                    case "desert":
                        day.Add(Make(root.transform, new Spec { name = "SandWind", material = streak, count = 180, lifetime = new Vector2(2.5f, 4f), size = new Vector2(0.05f, 0.085f),
                            colorA = new Color(0.95f, 0.8f, 0.5f, 0.3f), colorB = new Color(1f, 0.88f, 0.6f, 0.5f), area = new Vector3(50f, 5f, 50f), centerY = 2.5f,
                            drift = new Vector3(8f, -0.1f, 1.2f), driftRandom = new Vector3(2f, 0.2f, 0.8f), noiseStrength = 0.2f, stretch = true }));
                        day.Add(Make(root.transform, new Spec { name = "DustHaze", material = puff, count = 22, lifetime = new Vector2(9f, 14f), size = new Vector2(3.5f, 6f),
                            colorA = new Color(0.9f, 0.75f, 0.5f, 0.07f), colorB = new Color(0.95f, 0.82f, 0.58f, 0.12f), area = new Vector3(50f, 1.5f, 50f), centerY = 1f,
                            drift = new Vector3(1.6f, 0f, 0.3f), driftRandom = new Vector3(0.5f, 0.05f, 0.3f), noiseStrength = 0.3f }));
                        night.Add(Make(root.transform, new Spec { name = "NightSandWind", material = streak, count = 110, lifetime = new Vector2(2.5f, 4f), size = new Vector2(0.035f, 0.06f),
                            colorA = new Color(0.55f, 0.65f, 0.95f, 0.18f), colorB = new Color(0.7f, 0.78f, 1f, 0.3f), area = new Vector3(50f, 5f, 50f), centerY = 2.5f,
                            drift = new Vector3(6f, -0.1f, 1f), driftRandom = new Vector3(1.5f, 0.2f, 0.6f), noiseStrength = 0.2f, stretch = true }));
                        night.Add(Make(root.transform, new Spec { name = "NightHaze", material = puff, count = 16, lifetime = new Vector2(9f, 14f), size = new Vector2(3.5f, 6f),
                            colorA = new Color(0.45f, 0.55f, 0.85f, 0.05f), colorB = new Color(0.55f, 0.65f, 0.9f, 0.08f), area = new Vector3(50f, 1.5f, 50f), centerY = 1f,
                            drift = new Vector3(1.2f, 0f, 0.2f), driftRandom = new Vector3(0.4f, 0.05f, 0.3f), noiseStrength = 0.3f }));
                        break;
                }

                var atmosphere = root.AddComponent<MapAtmosphere>();
                var so = new SerializedObject(atmosphere);
                SetArray(so.FindProperty("dayEffects"), day);
                SetArray(so.FindProperty("nightEffects"), night);
                so.ApplyModifiedPropertiesWithoutUndo();

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[LastBullet] {scene.name}: 분위기 입자를 만들었어요 (낮 {day.Count}종, 밤 {night.Count}종).");
            }
            AssetDatabase.SaveAssets();
        }

        static void SetArray(SerializedProperty array, List<GameObject> list)
        {
            array.arraySize = list.Count;
            for (int i = 0; i < list.Count; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
        }

        static GameObject Make(Transform parent, Spec spec)
        {
            var go = new GameObject(spec.name, typeof(ParticleSystem));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, spec.centerY, 0f);
            var system = go.GetComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = system.main;
            main.loop = true;
            main.prewarm = true;
            main.duration = 10f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(spec.lifetime.x, spec.lifetime.y);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(spec.size.x, spec.size.y);
            main.startColor = new ParticleSystem.MinMaxGradient(spec.colorA, spec.colorB);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = Mathf.CeilToInt(spec.count * 1.4f);
            if (spec.spin > 0f)
                main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

            float averageLife = (spec.lifetime.x + spec.lifetime.y) * 0.5f;
            var emission = system.emission;
            emission.rateOverTime = spec.count / averageLife;

            var shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = spec.area;

            var velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(spec.drift.x - spec.driftRandom.x, spec.drift.x + spec.driftRandom.x);
            velocity.y = new ParticleSystem.MinMaxCurve(spec.drift.y - spec.driftRandom.y, spec.drift.y + spec.driftRandom.y);
            velocity.z = new ParticleSystem.MinMaxCurve(spec.drift.z - spec.driftRandom.z, spec.drift.z + spec.driftRandom.z);

            var noise = system.noise;
            noise.enabled = spec.noiseStrength > 0f;
            noise.strength = spec.noiseStrength;
            noise.frequency = spec.noiseFrequency;
            noise.scrollSpeed = 0.05f;

            if (spec.spin > 0f)
            {
                var rotation = system.rotationOverLifetime;
                rotation.enabled = true;
                rotation.z = new ParticleSystem.MinMaxCurve(-spec.spin, spec.spin);
            }

            // 생길 때 부드럽게 나타나고 사라질 때 부드럽게 사라진다 (반짝이는 입자는 중간에 한 번 더 어두워진다)
            var fade = system.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            var alphaKeys = spec.pulse
                ? new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0.25f, 0.4f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0.3f, 0.8f), new GradientAlphaKey(0f, 1f) }
                : new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(1f, 0.85f), new GradientAlphaKey(0f, 1f) };
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, alphaKeys);
            fade.color = gradient;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = spec.material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            if (spec.stretch)
            {
                renderer.renderMode = ParticleSystemRenderMode.Stretch;
                renderer.velocityScale = 0.12f;
                renderer.lengthScale = 5f;
                renderer.cameraVelocityScale = 0f;
            }

            system.Play();
            return go;
        }

        // ---------- 그림·머티리얼 (이미 있으면 건너뜀) ----------

        static void CreatePngs()
        {
            if (!File.Exists("Assets/Effects/Dust.png"))
                WritePng("Assets/Effects/Dust.png", 32, 32, (u, v) => Mathf.Clamp01(1f - Smooth(0.2f, 1f, Mathf.Sqrt(u * u + v * v))));
            // 빛 알갱이: 환한 가운데 + 부드러운 번짐
            WritePng("Assets/Effects/Glow.png", 64, 64, (u, v) =>
            {
                float r = Mathf.Sqrt(u * u + v * v);
                return Mathf.Clamp01(Mathf.Exp(-r * r * 18f) + 0.35f * Mathf.Exp(-r * r * 3.5f));
            });
            // 잎: 양 끝이 뾰족한 타원 + 가운데 잎맥(살짝 어둡게)
            WritePng("Assets/Effects/Leaf.png", 64, 64, (u, v) =>
            {
                float w = Mathf.Pow(Mathf.Max(0f, 1f - v * v), 0.8f) * 0.5f; // v: 세로(-1~1), 가로 폭
                float a = 1f - Smooth(w - 0.05f, w + 0.02f, Mathf.Abs(u));
                return a;
            });
            // 모래바람 줄: 가로로 길고 양 끝이 사라지는 얇은 줄
            WritePng("Assets/Effects/Streak.png", 64, 16, (u, v) =>
            {
                float along = 1f - Mathf.Abs(u);                 // 가운데가 진하다
                float across = 1f - Smooth(0.2f, 1f, Mathf.Abs(v));
                return Mathf.Clamp01(along * 1.2f) * across;
            });
        }

        // 0 이하는 0, 1 이상은 1, 사이는 부드럽게 (Mathf.SmoothStep과 인자 뜻이 다르다)
        static float Smooth(float e0, float e1, float x)
        {
            float t = Mathf.Clamp01((x - e0) / (e1 - e0));
            return t * t * (3f - 2f * t);
        }

        // alpha(u, v): u·v는 -1~1. 색은 흰색, 알파만 정한다 (색은 입자가 입힌다)
        static void WritePng(string path, int width, int height, System.Func<float, float, float> alpha)
        {
            if (File.Exists(path))
                return;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    float u = (x - (width - 1) / 2f) / (width / 2f), v = (y - (height - 1) / 2f) / (height / 2f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(alpha(u, v))));
                }
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
        }

        static Material GetMaterial(string name, string texturePath, bool additive, bool doubleSided)
        {
            string path = $"Assets/Materials/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
                return existing;
            var material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            material.SetFloat("_Surface", 1f); // 투명
            material.SetFloat("_Blend", additive ? 2f : 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            if (doubleSided)
                material.SetFloat("_Cull", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
            material.SetColor("_BaseColor", Color.white);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
