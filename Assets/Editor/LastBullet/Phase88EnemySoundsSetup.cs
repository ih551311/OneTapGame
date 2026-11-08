using System.IO;
using LastBullet.Enemy;
using UnityEditor;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 적 처치·보스 소리를 만든다 (2026-11-08 사용자가 고른 추천 작업: 적 처치 소리, 보스 소리).
    //   소리 파일 (이미 있으면 덮어쓰지 않는다 → 같은 이름으로 바꿔 끼울 수 있다): Assets/Audio/Combat/
    //     Enemy_Death.wav  몸이 터질 때: 물감이 "퐁" 터지고 철퍽 퍼지는 소리 (0.5초)
    //     Boss_Death.wav   보스가 터질 때: 더 낮고 길게 울리는 소리 (1.1초)
    //     Boss_SlamWarn.wav 내려치기 준비: 낮은 울림이 점점 커진다 (1.3초 = 경고 원이 커지는 시간)
    //     Boss_Slam.wav    충격파: 쿵 하는 깊은 울림 (0.9초)
    //   모두 이 메뉴가 코드로 합성한 임시 소리다. 프리팹 5개(적 4 + 보스)에 연결한다.
    //   (보스 프리팹은 Phase 86을 다시 실행하면 새로 만들어져 소리 연결이 지워지니, 그때는 이 메뉴를 다시 누른다.)
    // 메뉴: LastBullet > Phase 88 적·보스 소리 만들기
    public static class Phase88EnemySoundsSetup
    {
        const string Folder = "Assets/Audio/Combat/";
        const int SampleRate = 44100;

        static readonly string[] EnemyPrefabs =
        {
            "Assets/Prefabs/Enemy_Charger.prefab",
            "Assets/Prefabs/Enemy_Thrower.prefab",
            "Assets/Prefabs/Enemy_Splitter.prefab",
            "Assets/Prefabs/Enemy_SplitterMini.prefab",
        };

        [MenuItem("LastBullet/Phase 88 적·보스 소리 만들기")]
        static void Create()
        {
            var death = Ensure("Enemy_Death", MakeDeath);
            var bossDeath = Ensure("Boss_Death", MakeBossDeath);
            var warn = Ensure("Boss_SlamWarn", MakeWarn);
            var slam = Ensure("Boss_Slam", MakeSlam);

            foreach (var path in EnemyPrefabs)
                SetPrefab(path, deathSound: death, deathVolume: path.Contains("Mini") ? 0.4f : 0.55f);
            SetPrefab("Assets/Prefabs/Enemy_Boss.prefab", deathSound: bossDeath, deathVolume: 0.95f, warn: warn, slam: slam);
            AssetDatabase.SaveAssets();
            Debug.Log("[LastBullet] 적 처치 소리와 보스 소리를 만들고 프리팹에 연결했어요.");
        }

        static void SetPrefab(string path, AudioClip deathSound, float deathVolume, AudioClip warn = null, AudioClip slam = null)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            var effect = root.GetComponent<EnemyDeathEffect>();
            if (effect != null)
            {
                var so = new SerializedObject(effect);
                so.FindProperty("popSound").objectReferenceValue = deathSound;
                so.FindProperty("popVolume").floatValue = deathVolume;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            var bossSlam = root.GetComponent<BossSlam>();
            if (bossSlam != null)
            {
                var so = new SerializedObject(bossSlam);
                so.FindProperty("warnSound").objectReferenceValue = warn;
                so.FindProperty("slamSound").objectReferenceValue = slam;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        // ── 소리 만들기 ──
        static AudioClip Ensure(string name, System.Func<float[]> make)
        {
            string path = $"{Folder}{name}.wav";
            if (AssetDatabase.LoadAssetAtPath<AudioClip>(path) == null)
            {
                WriteWav(path, make());
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        // 물감 방울이 터지는 소리: 짧은 "퐁"(음이 내려가는 사인) + 철퍽(걸러진 잡음이 빠르게 줄어듦)
        static float[] MakeDeath()
        {
            var rnd = new System.Random(21);
            int n = (int)(SampleRate * 0.5f);
            var data = new float[n];
            float lp = 0f, phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float freq = 520f * Mathf.Exp(-t * 7f) + 120f;       // 높은 데서 낮은 데로 미끄러진다
                phase += freq / SampleRate;
                float pop = Mathf.Sin(6.283f * phase) * Mathf.Exp(-t * 12f);
                lp += (((float)rnd.NextDouble() * 2f - 1f) - lp) * 0.25f;
                float splat = lp * Mathf.Exp(-t * 14f) * (t > 0.015f ? 1f : t / 0.015f);
                data[i] = pop * 0.7f + splat * 0.9f;
            }
            return Normalize(data, 0.8f);
        }

        // 보스가 터지는 소리: 깊은 쿵 + 길게 이어지는 낮은 울림 + 철퍽
        static float[] MakeBossDeath()
        {
            var rnd = new System.Random(22);
            int n = (int)(SampleRate * 1.1f);
            var data = new float[n];
            float lp = 0f, phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float freq = 150f * Mathf.Exp(-t * 4f) + 45f;
                phase += freq / SampleRate;
                float boom = Mathf.Sin(6.283f * phase) * Mathf.Exp(-t * 4.2f);
                lp += (((float)rnd.NextDouble() * 2f - 1f) - lp) * 0.08f;
                float splat = lp * Mathf.Exp(-t * 6f);
                data[i] = boom * 0.85f + splat * 1.1f;
            }
            return Normalize(data, 0.9f);
        }

        // 내려치기 준비: 낮은 울림(55Hz 근처)이 1.3초 동안 점점 커지고 높아진다
        static float[] MakeWarn()
        {
            var rnd = new System.Random(23);
            int n = (int)(SampleRate * 1.3f);
            var data = new float[n];
            float lp = 0f, phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float k = t / 1.3f;
                float freq = 48f + 40f * k * k;
                phase += freq / SampleRate;
                lp += (((float)rnd.NextDouble() * 2f - 1f) - lp) * 0.04f;
                float envelope = k * k * (0.25f + 0.75f * k) * Mathf.Clamp01((1.3f - t) / 0.05f);
                data[i] = (Mathf.Sin(6.283f * phase) * 0.8f + Mathf.Sin(6.283f * phase * 2f) * 0.3f + lp * 0.5f) * envelope;
            }
            return Normalize(data, 0.7f);
        }

        // 충격파: 쿵(낮은 사인이 빠르게 내려감) + 땅이 갈라지는 잡음
        static float[] MakeSlam()
        {
            var rnd = new System.Random(24);
            int n = (int)(SampleRate * 0.9f);
            var data = new float[n];
            float lp = 0f, phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float freq = 110f * Mathf.Exp(-t * 6f) + 38f;
                phase += freq / SampleRate;
                float boom = Mathf.Sin(6.283f * phase) * Mathf.Exp(-t * 5f);
                lp += (((float)rnd.NextDouble() * 2f - 1f) - lp) * 0.18f;
                float crack = lp * Mathf.Exp(-t * 11f) * (t > 0.01f ? 1f : t / 0.01f);
                data[i] = boom * 0.95f + crack * 0.8f;
            }
            return Normalize(data, 0.9f);
        }

        static float[] Normalize(float[] data, float peak)
        {
            float max = 0.0001f;
            foreach (var v in data)
                max = Mathf.Max(max, Mathf.Abs(v));
            for (int i = 0; i < data.Length; i++)
                data[i] = data[i] / max * peak;
            return data;
        }

        // 16비트 모노 WAV로 저장
        static void WriteWav(string path, float[] data)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using (var stream = new FileStream(path, FileMode.Create))
            using (var w = new BinaryWriter(stream))
            {
                int bytes = data.Length * 2;
                w.Write(new[] { 'R', 'I', 'F', 'F' });
                w.Write(36 + bytes);
                w.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
                w.Write(16);
                w.Write((short)1);
                w.Write((short)1);
                w.Write(SampleRate);
                w.Write(SampleRate * 2);
                w.Write((short)2);
                w.Write((short)16);
                w.Write(new[] { 'd', 'a', 't', 'a' });
                w.Write(bytes);
                foreach (var v in data)
                    w.Write((short)(Mathf.Clamp(v, -1f, 1f) * 32767f));
            }
        }
    }
}
