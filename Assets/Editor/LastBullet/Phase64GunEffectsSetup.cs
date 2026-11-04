using System.IO;
using LastBullet.Audio;
using LastBullet.Combat;
using LastBullet.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastBullet.EditorTools
{
    // 총구 불꽃·탄피와 맞은 소리를 만든다 (2026-11-04 사용자 요청, 데스티니식 전투).
    //   그림·소리 칸 (이미 있으면 덮어쓰지 않는다 — 사용자가 바꾼 것을 지키려고):
    //     Assets/Effects/MuzzleFlash.png      총구 불꽃 그림 (별 모양, 흰색)
    //     Assets/Materials/MuzzleFlash.mat     불꽃 전용 머티리얼 (밝게 겹쳐 보이는 방식)
    //     Assets/Materials/ShellCasing.mat     탄피 전용 머티리얼 (황동색)
    //     Assets/Audio/Combat/Hit_Body.wav, Hit_Crit.wav, Hit_Kill.wav   맞은 소리 (코드로 만든 임시 소리)
    //   씬 (4개): 3인칭 총(R.Hand/ThirdPersonGun)과 1인칭 총(Main Camera/FirstPersonGun/Model/Ak47Holder) 아래에
    //             MuzzleFlash, ShellCasing, MuzzleLight를 AK47 총구·탄피 배출구 자리에 만든다.
    //             Player에 GunEffects(불꽃·탄피)와 HitSounds(맞은 소리) + 2D 소리 칸 HitSound를 붙인다.
    // Phase 62(1인칭 양손)를 다시 실행하면 1인칭 총이 새로 만들어지므로 이 메뉴도 다시 실행한다. 메뉴: LastBullet > Phase 64 총구 불꽃·탄피·맞은 소리 만들기
    public static class Phase64GunEffectsSetup
    {
        const string FlashPng = "Assets/Effects/MuzzleFlash.png";

        static readonly string[] Scenes =
        {
            "Assets/Scenes/1_Museum.unity",
            "Assets/Scenes/2_Forest.unity",
            "Assets/Scenes/3_CastleVillage.unity",
            "Assets/Scenes/4_Desert.unity",
        };

        [MenuItem("LastBullet/Phase 64 총구 불꽃·탄피·맞은 소리 만들기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            Directory.CreateDirectory("Assets/Effects");
            Directory.CreateDirectory("Assets/Audio/Combat");
            CreateFlashPng();
            CreateSounds();
            AssetDatabase.Refresh();

            var flashTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(FlashPng);
            var flashMaterial = GetFlashMaterial(flashTexture);
            var casingMaterial = GetCasingMaterial();
            var body = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Combat/Hit_Body.wav");
            var crit = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Combat/Hit_Crit.wav");
            var kill = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Combat/Hit_Kill.wav");

            foreach (var path in Scenes)
            {
                var scene = EditorSceneManager.OpenScene(path);
                var player = GameObject.FindWithTag("Player");
                var camera = Camera.main;
                var gun = player != null ? player.GetComponent<AutoGun>() : null;
                var zoom = player != null ? player.GetComponent<CameraZoom>() : null;
                Transform thirdGun = null;
                if (player != null)
                    foreach (var t in player.GetComponentsInChildren<Transform>(true))
                        if (t.name == "ThirdPersonGun")
                            thirdGun = t;
                var firstHolder = camera != null ? camera.transform.Find("FirstPersonGun/Model/Ak47Holder") : null;
                if (gun == null || zoom == null || thirdGun == null || firstHolder == null)
                {
                    Debug.LogWarning($"[LastBullet] {scene.name}: AutoGun·CameraZoom·3인칭 총·1인칭 총 중 없는 것이 있어 건너뜀 (먼저 Phase 61·62)");
                    continue;
                }

                var effects = player.GetComponent<GunEffects>();
                if (effects == null)
                    effects = player.AddComponent<GunEffects>();
                var so = new SerializedObject(effects);
                so.FindProperty("gun").objectReferenceValue = gun;
                so.FindProperty("zoom").objectReferenceValue = zoom;
                BuildSet(so.FindProperty("third"), thirdGun, flashMaterial, casingMaterial, 1f);
                BuildSet(so.FindProperty("first"), firstHolder, flashMaterial, casingMaterial, 0.6f);
                so.ApplyModifiedPropertiesWithoutUndo();

                // 맞은 소리
                var sound = player.transform.Find("HitSound");
                if (sound == null)
                {
                    var go = new GameObject("HitSound", typeof(AudioSource));
                    go.transform.SetParent(player.transform, false);
                    sound = go.transform;
                }
                var audio = sound.GetComponent<AudioSource>();
                audio.playOnAwake = false;
                audio.spatialBlend = 0f;
                var hits = player.GetComponent<HitSounds>();
                if (hits == null)
                    hits = player.AddComponent<HitSounds>();
                var hitSo = new SerializedObject(hits);
                hitSo.FindProperty("source").objectReferenceValue = audio;
                hitSo.FindProperty("bodyClip").objectReferenceValue = body;
                hitSo.FindProperty("critClip").objectReferenceValue = crit;
                hitSo.FindProperty("killClip").objectReferenceValue = kill;
                hitSo.ApplyModifiedPropertiesWithoutUndo();

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[LastBullet] {scene.name}: 총구 불꽃·탄피·맞은 소리를 만들었어요.");
            }
            AssetDatabase.SaveAssets();
        }

        // gunFrame = 총 모델 단위 좌표(+Z 총구, +Y 위, +X 오른쪽)의 부모. 그 아래 Ak47의 모양에서 총구·탄피 배출구 자리를 잰다.
        static void BuildSet(SerializedProperty set, Transform gunFrame, Material flashMaterial, Material casingMaterial, float flashScale)
        {
            foreach (var name in new[] { "MuzzleFlash", "ShellCasing", "MuzzleLight" })
            {
                var old = gunFrame.Find(name);
                if (old != null)
                    Object.DestroyImmediate(old.gameObject);
            }
            Measure(gunFrame, out Vector3 tip, out Vector3 port);

            // 불꽃
            var flashGo = new GameObject("MuzzleFlash", typeof(ParticleSystem));
            flashGo.transform.SetParent(gunFrame, false);
            flashGo.transform.localPosition = tip + Vector3.forward * 0.02f;
            var flash = flashGo.GetComponent<ParticleSystem>();
            flash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = flash.main;
            main.loop = true;
            main.playOnAwake = true;
            main.duration = 1f;
            main.startLifetime = 0.05f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.22f * flashScale, 0.34f * flashScale);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new Color(1f, 0.85f, 0.55f, 1f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 20;
            var emission = flash.emission;
            emission.rateOverTime = 0f;
            var shape = flash.shape;
            shape.enabled = false;
            var fade = flash.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.6f, 0.2f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;
            var flashRenderer = flashGo.GetComponent<ParticleSystemRenderer>();
            flashRenderer.sharedMaterial = flashMaterial;
            flashRenderer.shadowCastingMode = ShadowCastingMode.Off;
            flashRenderer.receiveShadows = false;
            flash.Play();

            // 탄피
            var casingGo = new GameObject("ShellCasing", typeof(ParticleSystem));
            casingGo.transform.SetParent(gunFrame, false);
            casingGo.transform.localPosition = port;
            casingGo.transform.localRotation = Quaternion.LookRotation(new Vector3(1f, 0.7f, -0.25f));
            var casing = casingGo.GetComponent<ParticleSystem>();
            casing.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var cMain = casing.main;
            cMain.loop = true;
            cMain.playOnAwake = true;
            cMain.duration = 1f;
            cMain.startLifetime = 0.8f;
            cMain.startSpeed = new ParticleSystem.MinMaxCurve(1.6f, 2.6f);
            cMain.startSize3D = true;
            cMain.startSizeX = 0.014f;
            cMain.startSizeY = 0.014f;
            cMain.startSizeZ = 0.04f;
            cMain.startRotation3D = true;
            cMain.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            cMain.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            cMain.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            cMain.gravityModifier = 1.4f;
            cMain.simulationSpace = ParticleSystemSimulationSpace.World;
            cMain.scalingMode = ParticleSystemScalingMode.Local;
            cMain.maxParticles = 40;
            var cEmission = casing.emission;
            cEmission.rateOverTime = 0f;
            var cShape = casing.shape;
            cShape.enabled = true;
            cShape.shapeType = ParticleSystemShapeType.Cone;
            cShape.angle = 12f;
            cShape.radius = 0.002f;
            var spin = casing.rotationOverLifetime;
            spin.enabled = true;
            spin.separateAxes = true;
            spin.x = new ParticleSystem.MinMaxCurve(-12f, 12f);
            spin.y = new ParticleSystem.MinMaxCurve(-12f, 12f);
            spin.z = new ParticleSystem.MinMaxCurve(-12f, 12f);
            var casingRenderer = casingGo.GetComponent<ParticleSystemRenderer>();
            casingRenderer.renderMode = ParticleSystemRenderMode.Mesh;
            casingRenderer.mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            casingRenderer.sharedMaterial = casingMaterial;
            casingRenderer.shadowCastingMode = ShadowCastingMode.Off;
            casingRenderer.receiveShadows = false;
            casing.Play();

            // 불빛
            var lightGo = new GameObject("MuzzleLight", typeof(Light));
            lightGo.transform.SetParent(gunFrame, false);
            lightGo.transform.localPosition = tip + Vector3.forward * 0.12f;
            var light = lightGo.GetComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.7f, 0.3f);
            light.intensity = 3f;
            light.range = 5f;
            light.shadows = LightShadows.None;
            light.enabled = false;

            set.FindPropertyRelative("flash").objectReferenceValue = flash;
            set.FindPropertyRelative("casing").objectReferenceValue = casing;
            set.FindPropertyRelative("light").objectReferenceValue = light;
        }

        // 총구 끝 가운데(tip)와 탄피 배출구(port)를 gunFrame 좌표로 잰다
        static void Measure(Transform gunFrame, out Vector3 tip, out Vector3 port)
        {
            var ak = gunFrame.Find("Ak47");
            tip = new Vector3(0f, 0.02f, 0.45f);
            port = new Vector3(0.03f, 0.03f, 0.05f);
            if (ak == null)
                return;
            Matrix4x4 toFrame = gunFrame.worldToLocalMatrix;
            bool any = false;
            Vector3 min = Vector3.positiveInfinity, max = Vector3.negativeInfinity;
            foreach (var filter in ak.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null)
                    continue;
                var m = toFrame * filter.transform.localToWorldMatrix;
                foreach (var v in filter.sharedMesh.vertices)
                {
                    var p = m.MultiplyPoint3x4(v);
                    min = Vector3.Min(min, p);
                    max = Vector3.Max(max, p);
                    any = true;
                }
            }
            if (!any)
                return;
            // 총구 끝에 가까운 점들의 가운데
            Vector3 sum = Vector3.zero;
            int count = 0;
            foreach (var filter in ak.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null)
                    continue;
                var m = toFrame * filter.transform.localToWorldMatrix;
                foreach (var v in filter.sharedMesh.vertices)
                {
                    var p = m.MultiplyPoint3x4(v);
                    if (p.z > max.z - 0.012f)
                    {
                        sum += p;
                        count++;
                    }
                }
            }
            if (count > 0)
            {
                tip = sum / count;
                tip.x = 0f;
            }
            float length = max.z - min.z;
            port = new Vector3(max.x * 0.8f, tip.y + 0.02f, min.z + length * 0.5f);
            Debug.Log($"[LastBullet] 총 크기 {min} ~ {max}, 총구 {tip}, 탄피 배출구 {port}");
        }

        // ---------- 그림·머티리얼·소리 (이미 있으면 건너뜀) ----------

        static void CreateFlashPng()
        {
            if (File.Exists(FlashPng))
                return;
            const int N = 128;
            var texture = new Texture2D(N, N, TextureFormat.RGBA32, false);
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float dx = (x - (N - 1) / 2f) / (N / 2f), dy = (y - (N - 1) / 2f) / (N / 2f);
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float core = Mathf.Exp(-r * r * 14f);
                    float spikeH = Mathf.Exp(-dy * dy * 220f) * Mathf.Clamp01(1.1f - Mathf.Abs(dx));
                    float spikeV = Mathf.Exp(-dx * dx * 220f) * Mathf.Clamp01(1.1f - Mathf.Abs(dy));
                    float diag = 0.45f * (Mathf.Exp(-Mathf.Pow(dx - dy, 2f) * 160f) + Mathf.Exp(-Mathf.Pow(dx + dy, 2f) * 160f)) * Mathf.Clamp01(0.8f - r);
                    float a = Mathf.Clamp01(core + 0.8f * (spikeH + spikeV) + diag);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            texture.Apply();
            File.WriteAllBytes(FlashPng, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(FlashPng);
        }

        static Material GetFlashMaterial(Texture2D texture)
        {
            const string path = "Assets/Materials/MuzzleFlash.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
                return existing;
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            var material = new Material(shader);
            material.SetFloat("_Surface", 1f); // 투명
            material.SetFloat("_Blend", 2f);   // 더하기: 밝게 겹쳐 보인다
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.One);
            material.SetFloat("_ZWrite", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static Material GetCasingMaterial()
        {
            const string path = "Assets/Materials/ShellCasing.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
                return existing;
            var shader = Shader.Find("Universal Render Pipeline/Particles/Lit");
            var material = new Material(shader != null ? shader : Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", new Color(0.85f, 0.65f, 0.2f));
            material.SetFloat("_Metallic", 0.8f);
            material.SetFloat("_Smoothness", 0.6f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static void CreateSounds()
        {
            const int Rate = 44100;
            var random = new System.Random(7);
            // 몸에 맞음: 짧은 "틱" (잡음 + 700Hz)
            WriteWav("Assets/Audio/Combat/Hit_Body.wav", Rate, 0.07f, t =>
                (float)(Noise(random) * System.Math.Exp(-t * 70) * 0.5 + System.Math.Sin(2 * System.Math.PI * 700 * t) * System.Math.Exp(-t * 55) * 0.6));
            // 머리(크리티컬): 높은 "딩" (1900Hz + 2850Hz 종소리)
            WriteWav("Assets/Audio/Combat/Hit_Crit.wav", Rate, 0.25f, t =>
                (float)(System.Math.Sin(2 * System.Math.PI * 1900 * t) * System.Math.Exp(-t * 16) * 0.6
                      + System.Math.Sin(2 * System.Math.PI * 2850 * t) * System.Math.Exp(-t * 22) * 0.35
                      + Noise(random) * System.Math.Exp(-t * 120) * 0.4));
            // 처치: 낮은 "퍽" (내려가는 160Hz + 짧은 잡음 + 1200Hz 꼬리)
            WriteWav("Assets/Audio/Combat/Hit_Kill.wav", Rate, 0.32f, t =>
                (float)(System.Math.Sin(2 * System.Math.PI * (180 - System.Math.Min(t, 0.2) * 280) * t) * System.Math.Exp(-t * 12) * 0.8
                      + Noise(random) * System.Math.Exp(-t * 45) * 0.45
                      + System.Math.Sin(2 * System.Math.PI * 1200 * t) * System.Math.Exp(-t * 30) * 0.25));
        }

        static double Noise(System.Random random)
        {
            return random.NextDouble() * 2.0 - 1.0;
        }

        // 16비트 모노 WAV. 이미 있으면 건너뛴다 (사용자가 바꾼 소리를 지키려고).
        static void WriteWav(string path, int rate, float seconds, System.Func<double, float> sample)
        {
            if (File.Exists(path))
                return;
            int count = (int)(rate * seconds);
            var samples = new float[count];
            float peak = 0.0001f;
            for (int i = 0; i < count; i++)
            {
                samples[i] = sample(i / (double)rate);
                peak = Mathf.Max(peak, Mathf.Abs(samples[i]));
            }
            using (var stream = new FileStream(path, FileMode.Create))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + count * 2);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)1);
                writer.Write(rate);
                writer.Write(rate * 2);
                writer.Write((short)2);
                writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                writer.Write(count * 2);
                for (int i = 0; i < count; i++)
                    writer.Write((short)(Mathf.Clamp(samples[i] / peak * 0.9f, -1f, 1f) * short.MaxValue));
            }
        }
    }
}
