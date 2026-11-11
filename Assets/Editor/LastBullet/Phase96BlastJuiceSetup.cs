using System.IO;
using LastBullet.Combat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 바주카 게임 주스 연결 (2026-11-11 사용자 요청: 게임 주스(타격감)가 밍밍하다).
    //   코드(BlastFx·HitStop·EnemyKnockback·BazookaWeapon·BazookaRocket)가 폭발 먼지·불꽃·파편·충격파 링, 히트스톱, 적 날아감, 로켓 연기 꼬리를 만든다.
    //   이 메뉴는 그 연출이 쓸 그림(머티리얼 4개)과 소리 2개를 숲·성 마을·사막의 Player(BazookaWeapon)에 연결한다:
    //     먼지 = AtmoSandPuff.mat, 불꽃·빛 = AtmoGlow.mat, 충격파 링 = BossSlamRing.mat, 파편 = PaintBurst.mat (모두 이미 있는 머티리얼)
    //     소리: Assets/Audio/Combat/Bazooka_Whoosh.wav (날아가는 쉬익, 반복), Bazooka_BoomBig.wav (겹친 폭발음 2초) — 코드로 합성한 임시 소리, 같은 이름으로 덮어쓰면 바뀐다
    // 먼저 Phase 93이 있어야 한다. 다시 실행해도 안전하다. 메뉴: LastBullet > Phase 96 바주카 게임 주스 연결
    public static class Phase96BlastJuiceSetup
    {
        const int SampleRate = 44100;

        static readonly string[] Scenes =
        {
            "Assets/Scenes/2_Forest.unity",
            "Assets/Scenes/3_CastleVillage.unity",
            "Assets/Scenes/4_Desert.unity",
        };

        [MenuItem("LastBullet/Phase 96 바주카 게임 주스 연결")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var dust = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/AtmoSandPuff.mat");
            var glow = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/AtmoGlow.mat");
            var ring = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BossSlamRing.mat");
            var debris = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/PaintBurst.mat");
            if (dust == null || glow == null || ring == null || debris == null)
            {
                Debug.LogWarning("[LastBullet] AtmoSandPuff·AtmoGlow·BossSlamRing·PaintBurst 머티리얼 중 없는 것이 있어요.");
                return;
            }
            var whoosh = EnsureSound("Bazooka_Whoosh", MakeWhoosh, true);
            var boom = EnsureSound("Bazooka_BoomBig", MakeBoomBig, false);

            foreach (var path in Scenes)
            {
                var scene = EditorSceneManager.OpenScene(path);
                var player = GameObject.FindWithTag("Player");
                var weapon = player != null ? player.GetComponent<BazookaWeapon>() : null;
                if (weapon == null)
                {
                    Debug.LogWarning($"[LastBullet] {scene.name}: BazookaWeapon이 없어 건너뜀 (먼저 Phase 93)");
                    continue;
                }
                var so = new SerializedObject(weapon);
                so.FindProperty("dustMaterial").objectReferenceValue = dust;
                so.FindProperty("glowMaterial").objectReferenceValue = glow;
                so.FindProperty("ringMaterial").objectReferenceValue = ring;
                so.FindProperty("debrisMaterial").objectReferenceValue = debris;
                so.FindProperty("whooshSound").objectReferenceValue = whoosh;
                so.FindProperty("boomSound").objectReferenceValue = boom;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[LastBullet] {scene.name}: 바주카 게임 주스(연출 그림·소리)를 연결했어요.");
            }
            AssetDatabase.SaveAssets();
        }

        // ── 소리 (합성) ──
        static AudioClip EnsureSound(string name, System.Func<float[]> make, bool loop)
        {
            string path = $"Assets/Audio/Combat/{name}.wav";
            if (AssetDatabase.LoadAssetAtPath<AudioClip>(path) == null)
            {
                WriteWav(path, make());
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        // 날아가는 쉬익 (0.6초, 이어 붙여도 끊기지 않게 양 끝 크기를 맞춘다): 거른 잡음이 일렁이며 낮은 쉭 소리
        static float[] MakeWhoosh()
        {
            var rnd = new System.Random(51);
            int n = (int)(SampleRate * 0.6f);
            var data = new float[n];
            float lp = 0f, lp2 = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float flutter = 0.75f + 0.25f * Mathf.Sin(6.2832f * t / 0.6f * 3f); // 0.6초에 정확히 3번 일렁여서 이어 붙여도 매끄럽다
                lp += (((float)rnd.NextDouble() * 2f - 1f) - lp) * 0.18f;
                lp2 += (lp - lp2) * 0.35f;
                data[i] = (lp * 0.5f + lp2 * 1.2f) * flutter;
            }
            // 양 끝을 서로 섞어서(크로스페이드) 반복할 때 딱 소리가 나지 않게
            int fade = (int)(SampleRate * 0.05f);
            for (int i = 0; i < fade; i++)
            {
                float k = i / (float)fade;
                data[i] = Mathf.Lerp(data[n - fade + i], data[i], k);
            }
            return Normalize(data, 0.6f);
        }

        // 겹친 폭발음 (2초): 맨 앞 "딱" 갈라짐 + 낮은 "쿵" + 길게 울리는 잡음 꼬리 + 뒤따르는 파편 소리 몇 개
        static float[] MakeBoomBig()
        {
            var rnd = new System.Random(52);
            int n = (int)(SampleRate * 2.0f);
            var data = new float[n];
            float lp = 0f, lp2 = 0f, lp3 = 0f, phase = 0f, phaseSub = 0f;
            float[] debris = { 0.28f, 0.42f, 0.55f, 0.71f, 0.9f };
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float raw = (float)rnd.NextDouble() * 2f - 1f;
                // 1) 갈라짐 (0.12초)
                float crack = raw * Mathf.Exp(-t * 40f);
                // 2) 낮은 쿵 (주파수가 내려간다)
                phase += (110f * Mathf.Exp(-t * 4f) + 32f) / SampleRate;
                float thump = Mathf.Sin(6.283f * phase) * Mathf.Exp(-t * 3.2f);
                // 3) 아주 낮은 울림
                phaseSub += 24f / SampleRate;
                float sub = Mathf.Sin(6.283f * phaseSub) * Mathf.Exp(-t * 1.8f) * 0.7f;
                // 4) 거른 잡음 꼬리 (시간이 갈수록 더 낮은 소리만 남는다)
                lp += (raw - lp) * Mathf.Lerp(0.55f, 0.04f, Mathf.Clamp01(t / 1.2f));
                lp2 += (lp - lp2) * 0.3f;
                lp3 += (lp2 - lp3) * 0.3f;
                float body = (lp * 0.6f + lp3 * 1.4f) * Mathf.Exp(-t * 1.9f);
                // 5) 떨어지는 파편 소리
                float rain = 0f;
                foreach (var d in debris)
                {
                    float c = t - d;
                    if (c > 0f)
                        rain += Mathf.Sin(6.283f * (900f + 1200f * d) * c) * Mathf.Exp(-c * 60f) * 0.18f * ((float)rnd.NextDouble() * 0.5f + 0.75f);
                }
                data[i] = crack * 0.9f + thump * 1.0f + sub + body + rain;
            }
            return Normalize(data, 0.97f);
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
