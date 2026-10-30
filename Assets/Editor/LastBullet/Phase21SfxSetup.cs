using System.Collections.Generic;
using LastBullet.Audio;
using LastBullet.Combat;
using LastBullet.Enemy;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 총소리를 연결한다 (2026-10-31 사용자 요청, 에셋 Free Weapon Sound Effects의 핸드건).
    //  - 플레이어: 사격할 때 핸드건 소리 (2D, 크게) — 플레이어(OneBulletGun)가 있는 모든 씬
    //  - 원거리 적: 공격(던지기)할 때 같은 핸드건 소리를 작게 (3D: 멀수록 더 작게, 기본 40% 크기)
    // 소리는 변형 3개 중 무작위 + 여운 2개 중 하나를 깐다 (플레이어만. 적은 여운 없이 가볍게).
    // 메뉴: LastBullet > Phase 21 총소리 넣기
    public static class Phase21SfxSetup
    {
        const string Root = "Assets/FreeWeaponSFX_v1.1/Handgun/Gunshots/";
        const float PlayerVolume = 0.8f;
        const float EnemyVolume = 0.3f; // 플레이어 총소리의 약 40%

        static readonly string[] Scenes =
        {
            "Assets/Scenes/1_Museum.unity",
            "Assets/Scenes/2_Forest.unity",
            "Assets/Scenes/3_CastleVillage.unity",
            "Assets/Scenes/4_Desert.unity",
        };

        [MenuItem("LastBullet/Phase 21 총소리 넣기")]
        static void Create()
        {
            var shots = Load("sfx_wpn_hgn_gunshot_", 3);
            var tails = Load("sfx_wpn_hgn_tail_", 2);
            if (shots.Length == 0)
            {
                EditorUtility.DisplayDialog("LastBullet", "에셋 Free Weapon Sound Effects(핸드건)를 먼저 가져오세요.", "확인");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            // 1. 원거리 적 프리팹 (먼저: 씬에 열린 것이 없을 때 안전하다)
            var path = "Assets/Prefabs/Enemy_Thrower.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            var enemyAi = root.GetComponent<RangedEnemyAI>();
            if (enemyAi != null && root.GetComponent<SfxPlayer>() == null)
            {
                var source = GetOrAdd<AudioSource>(root);
                Configure(source, spatialBlend: 1f, minDistance: 6f, maxDistance: 45f);
                var sfx = root.AddComponent<SfxPlayer>();
                SetSfx(sfx, shots, new AudioClip[0], EnemyVolume);
                var so = new SerializedObject(enemyAi);
                so.FindProperty("attackSound").objectReferenceValue = sfx;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log("[LastBullet] Enemy_Thrower에 공격 소리를 넣었어요.");
            }
            PrefabUtility.UnloadPrefabContents(root);

            // 2. 플레이어가 있는 씬마다 사격 소리
            foreach (var scenePath in Scenes)
            {
                var scene = EditorSceneManager.OpenScene(scenePath);
                var gun = Object.FindFirstObjectByType<OneBulletGun>();
                if (gun == null || gun.GetComponent<SfxPlayer>() != null)
                    continue;

                var source = GetOrAdd<AudioSource>(gun.gameObject);
                Configure(source, spatialBlend: 0f, minDistance: 1f, maxDistance: 50f); // 2D: 어디서든 똑같이 들린다
                var sfx = gun.gameObject.AddComponent<SfxPlayer>();
                SetSfx(sfx, shots, tails, PlayerVolume);
                var so = new SerializedObject(gun);
                so.FindProperty("shotSound").objectReferenceValue = sfx;
                so.ApplyModifiedPropertiesWithoutUndo();

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[LastBullet] {scene.name}: 플레이어 사격 소리를 넣었어요.");
            }
            AssetDatabase.SaveAssets();
        }

        // 유니티 오브젝트에는 ?? 를 쓸 수 없다 (없는 컴포넌트가 null이 아닌 것처럼 취급된다)
        static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();
            return component != null ? component : go.AddComponent<T>();
        }

        static void Configure(AudioSource source, float spatialBlend, float minDistance, float maxDistance)
        {
            source.playOnAwake = false;
            source.spatialBlend = spatialBlend;
            source.minDistance = minDistance;
            source.maxDistance = maxDistance;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.dopplerLevel = 0f; // 움직이는 적에서 음높이가 흔들리지 않게
        }

        static void SetSfx(SfxPlayer sfx, AudioClip[] clips, AudioClip[] tails, float volume)
        {
            var so = new SerializedObject(sfx);
            Fill(so.FindProperty("clips"), clips);
            Fill(so.FindProperty("tails"), tails);
            so.FindProperty("volume").floatValue = volume;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Fill(SerializedProperty array, AudioClip[] clips)
        {
            array.arraySize = clips.Length;
            for (int i = 0; i < clips.Length; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
        }

        static AudioClip[] Load(string prefix, int count)
        {
            var list = new List<AudioClip>();
            for (int i = 1; i <= count; i++)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{Root}{prefix}{i:00}.wav");
                if (clip != null)
                    list.Add(clip);
            }
            return list.ToArray();
        }
    }
}
