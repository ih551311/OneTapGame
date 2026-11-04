using System.IO;
using LastBullet.Audio;
using LastBullet.Stage;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastBullet.EditorTools
{
    // 미술관 벽시계와 작은 소리를 만든다 (2026-11-05 사용자 요청).
    //   벽시계 WallClock: 북쪽 벽 가운데 액자 위 (0, 3.75, 15.0). 둥근 시계(황동 테두리 + 상아색 판 + 12개 눈금 + 시·분·초침).
    //     전용 머티리얼 Assets/Materials/WallClock_Face·Rim·Hands.mat. 모양을 바꿀 때는 WallClock 아래 부품을 바꾼다 (모델 교체 자리).
    //   소리 (이미 있으면 덮어쓰지 않는다): Assets/Audio/Museum/Clock_Tick.wav, Clock_Tock.wav, Creak_01~03.wav — 코드로 만든 임시 소리.
    //   MuseumAmbience: 시계 똑딱(3D)과 가끔 나는 바닥 삐걱(3D) 연결.
    // 이미 만들었으면 지우고 다시 만든다. 메뉴: LastBullet > Phase 71 벽시계와 삐걱 소리 만들기
    public static class Phase71WallClockSetup
    {
        const string Folder = "Assets/Audio/Museum";

        [MenuItem("LastBullet/Phase 71 벽시계와 삐걱 소리 만들기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            Directory.CreateDirectory(Folder);
            CreateSounds();
            AssetDatabase.Refresh();
            var tick = AssetDatabase.LoadAssetAtPath<AudioClip>(Folder + "/Clock_Tick.wav");
            var tock = AssetDatabase.LoadAssetAtPath<AudioClip>(Folder + "/Clock_Tock.wav");
            var creaks = new[]
            {
                AssetDatabase.LoadAssetAtPath<AudioClip>(Folder + "/Creak_01.wav"),
                AssetDatabase.LoadAssetAtPath<AudioClip>(Folder + "/Creak_02.wav"),
                AssetDatabase.LoadAssetAtPath<AudioClip>(Folder + "/Creak_03.wav"),
            };

            var scene = EditorSceneManager.OpenScene("Assets/Scenes/1_Museum.unity");
            foreach (var name in new[] { "WallClock", "MuseumAmbience" })
            {
                var old = GameObject.Find(name);
                if (old != null)
                    Object.DestroyImmediate(old);
            }

            var face = ArtSlots.GetOrCreateMaterial("WallClock_Face", new Color(0.93f, 0.9f, 0.8f));
            var rim = ArtSlots.GetOrCreateMaterial("WallClock_Rim", new Color(0.72f, 0.54f, 0.2f));
            rim.SetFloat("_Metallic", 0.7f);
            rim.SetFloat("_Smoothness", 0.6f);
            var hands = ArtSlots.GetOrCreateMaterial("WallClock_Hands", new Color(0.08f, 0.07f, 0.06f));

            // 시계: 로컬 -Z쪽이 보이는 앞면. 북쪽 벽(안쪽 면 z=15.0)에 붙이고 남쪽(방 안)을 본다.
            var clockGo = new GameObject("WallClock");
            clockGo.transform.position = new Vector3(0f, 3.75f, 15.0f - 0.04f);
            Part("Rim", clockGo.transform, PrimitiveType.Cylinder, new Vector3(0f, 0f, -0.02f), new Vector3(0.68f, 0.03f, 0.68f), rim);
            Part("Face", clockGo.transform, PrimitiveType.Cylinder, new Vector3(0f, 0f, -0.045f), new Vector3(0.58f, 0.02f, 0.58f), face);
            for (int i = 0; i < 12; i++)
            {
                float angle = i * 30f * Mathf.Deg2Rad;
                var tickMark = Part("Mark_" + i, clockGo.transform, PrimitiveType.Cube,
                    new Vector3(Mathf.Sin(angle) * 0.24f, Mathf.Cos(angle) * 0.24f, -0.062f),
                    new Vector3(i % 3 == 0 ? 0.03f : 0.015f, i % 3 == 0 ? 0.07f : 0.04f, 0.01f), hands);
                tickMark.transform.localRotation = Quaternion.Euler(0f, 0f, -i * 30f);
            }
            var hour = Pivot("HourPivot", clockGo.transform, -0.07f, 0.14f, 0.03f, hands);
            var minute = Pivot("MinutePivot", clockGo.transform, -0.078f, 0.21f, 0.02f, hands);
            var second = Pivot("SecondPivot", clockGo.transform, -0.086f, 0.23f, 0.008f, hands);
            // 앞면이 -Z로 보이게 원기둥을 눕혔으므로 (Y축 원기둥 → Z축), 부품 방향을 맞춘다
            foreach (var part in new[] { clockGo.transform.Find("Rim"), clockGo.transform.Find("Face") })
                part.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var wallClock = clockGo.AddComponent<WallClock>();
            var clockSo = new SerializedObject(wallClock);
            clockSo.FindProperty("hourPivot").objectReferenceValue = hour;
            clockSo.FindProperty("minutePivot").objectReferenceValue = minute;
            clockSo.FindProperty("secondPivot").objectReferenceValue = second;
            clockSo.ApplyModifiedPropertiesWithoutUndo();

            // 소리
            var ambienceGo = new GameObject("MuseumAmbience");
            var clockSound = new GameObject("ClockSound", typeof(AudioSource));
            clockSound.transform.SetParent(clockGo.transform, false);
            Setup3D(clockSound.GetComponent<AudioSource>(), 1.5f, 16f);
            var creakSound = new GameObject("CreakSound", typeof(AudioSource));
            creakSound.transform.SetParent(ambienceGo.transform, false);
            Setup3D(creakSound.GetComponent<AudioSource>(), 2f, 18f);

            var ambience = ambienceGo.AddComponent<MuseumAmbience>();
            var so = new SerializedObject(ambience);
            so.FindProperty("clock").objectReferenceValue = wallClock;
            so.FindProperty("clockSource").objectReferenceValue = clockSound.GetComponent<AudioSource>();
            so.FindProperty("tick").objectReferenceValue = tick;
            so.FindProperty("tock").objectReferenceValue = tock;
            so.FindProperty("creakSource").objectReferenceValue = creakSound.GetComponent<AudioSource>();
            var array = so.FindProperty("creaks");
            array.arraySize = creaks.Length;
            for (int i = 0; i < creaks.Length; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = creaks[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[LastBullet] 1_Museum: 벽시계와 삐걱 소리를 만들었어요.");
        }

        static void Setup3D(AudioSource source, float minDistance, float maxDistance)
        {
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = minDistance;
            source.maxDistance = maxDistance;
        }

        static GameObject Part(string name, Transform parent, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        // 바늘: 중심(피벗)에서 위(+Y)로 length만큼 뻗는 얇은 막대
        static Transform Pivot(string name, Transform parent, float z, float length, float width, Material material)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = new Vector3(0f, 0f, z);
            var bar = Part("Hand", pivot, PrimitiveType.Cube, new Vector3(0f, length * 0.5f, 0f), new Vector3(width, length, 0.008f), material);
            return pivot;
        }

        // ---------- 소리 (이미 있으면 건너뜀) ----------

        static void CreateSounds()
        {
            const int Rate = 44100;
            var random = new System.Random(5);
            // 똑: 높은 나무 두드림, 딱: 조금 낮고 짧게
            WriteWav(Folder + "/Clock_Tick.wav", Rate, 0.09f, t =>
                (float)(System.Math.Sin(2 * System.Math.PI * 1800 * t) * System.Math.Exp(-t * 90) * 0.7 + Noise(random) * System.Math.Exp(-t * 400) * 0.5));
            WriteWav(Folder + "/Clock_Tock.wav", Rate, 0.1f, t =>
                (float)(System.Math.Sin(2 * System.Math.PI * 1250 * t) * System.Math.Exp(-t * 80) * 0.7 + Noise(random) * System.Math.Exp(-t * 350) * 0.45));
            // 삐걱: 마룻바닥이 붙었다 미끄러지는 소리 — 톱니파 펄스의 빠르기가 흔들리며 오르내린다
            for (int n = 1; n <= 3; n++)
            {
                float length = 0.55f + 0.2f * n;
                float baseFreq = 70f + 25f * n;
                float seed = n * 1.7f;
                double phase = 0;
                double low = 0;
                WriteWav($"{Folder}/Creak_0{n}.wav", Rate, length, t =>
                {
                    double freq = baseFreq + 40.0 * System.Math.Sin(2 * System.Math.PI * (1.6 + 0.4 * n) * t + seed) + 12.0 * Noise(random);
                    phase += freq / 44100.0;
                    double saw = (phase % 1.0) * 2.0 - 1.0;
                    double stutter = 0.6 + 0.4 * System.Math.Sin(2 * System.Math.PI * (7 + n) * t); // 붙었다 떨어지는 떨림
                    double raw = saw * stutter + Noise(random) * 0.08;
                    low += (raw - low) * 0.12; // 부드럽게 깎는다
                    double envelope = System.Math.Pow(System.Math.Sin(System.Math.PI * t / length), 0.7);
                    return (float)(low * envelope);
                });
            }
        }

        static double Noise(System.Random random)
        {
            return random.NextDouble() * 2.0 - 1.0;
        }

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
