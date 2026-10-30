using LastBullet.Audio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 맵마다 바닥에 맞는 발소리로 바꾼다 (2026-10-31 사용자 요청: 걸을 때 "뽁뽁"거림 → 새 에셋 → 사막·미술관은 바닥을 다르게).
    //   숲·성 마을 = 풀밭 (에셋 Footstep(Snow and Grass)를 모노·짧게·크게 다듬은 footstep_01~08)
    //   사막 = 모래 "사박" (sand_01~06) / 미술관 = 나무 바닥 "또각" (wood_01,02,03,05,06)
    // 모래·나무 소리는 쓸 수 있는 에셋이 없어서 코드로 만든 임시 소리다 (Assets/Audio/Footsteps). 나중에 진짜 소리로 바꾸면 된다.
    // 팩의 Steps_1·2는 거의 순수한 100Hz 저음이라 "뽁"하고 들려서 쓰지 않는다.
    // 플레이어가 있는 모든 씬의 MovementSfx에서 걷기·달리기 SfxPlayer의 소리 칸과 크기를 바꾼다.
    // 메뉴: LastBullet > Phase 25 발소리 바꾸기
    public static class Phase25FootstepFix
    {
        const string Dir = "Assets/Audio/Footsteps/";

        struct Floor
        {
            public string scene;
            public string[] files;
            public float walkVolume, runVolume;
        }

        static readonly Floor[] Floors =
        {
            new Floor { scene = "Assets/Scenes/1_Museum.unity", files = new[] { "wood_01", "wood_02", "wood_03", "wood_05", "wood_06" }, walkVolume = 0.5f, runVolume = 0.6f },
            new Floor { scene = "Assets/Scenes/2_Forest.unity", files = Range("footstep_", 8), walkVolume = 0.5f, runVolume = 0.65f },
            new Floor { scene = "Assets/Scenes/3_CastleVillage.unity", files = Range("footstep_", 8), walkVolume = 0.5f, runVolume = 0.65f },
            new Floor { scene = "Assets/Scenes/4_Desert.unity", files = Range("sand_", 6), walkVolume = 0.55f, runVolume = 0.7f },
        };

        static string[] Range(string prefix, int count)
        {
            var names = new string[count];
            for (int i = 0; i < count; i++)
                names[i] = $"{prefix}{i + 1:00}";
            return names;
        }

        [MenuItem("LastBullet/Phase 25 발소리 바꾸기")]
        static void Fix()
        {
            if (AssetDatabase.LoadAssetAtPath<AudioClip>(Dir + "footstep_01.wav") == null)
            {
                EditorUtility.DisplayDialog("LastBullet", "Assets/Audio/Footsteps의 발소리 파일이 없어요.", "확인");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            foreach (var floor in Floors)
            {
                var clips = new AudioClip[floor.files.Length];
                for (int i = 0; i < clips.Length; i++)
                    clips[i] = AssetDatabase.LoadAssetAtPath<AudioClip>($"{Dir}{floor.files[i]}.wav");

                var scene = EditorSceneManager.OpenScene(floor.scene);
                var sounds = Object.FindFirstObjectByType<MovementSounds>();
                if (sounds == null)
                    continue;
                var so = new SerializedObject(sounds);
                Replace((SfxPlayer)so.FindProperty("walkStep").objectReferenceValue, clips, floor.walkVolume, new Vector2(0.95f, 1.05f));
                Replace((SfxPlayer)so.FindProperty("runStep").objectReferenceValue, clips, floor.runVolume, new Vector2(1.05f, 1.15f));
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[LastBullet] {scene.name}: 발소리 {clips.Length}개로 바꿨어요 ({floor.files[0]} …).");
            }
            AssetDatabase.SaveAssets();
        }

        static void Replace(SfxPlayer sfx, AudioClip[] clips, float volume, Vector2 pitch)
        {
            var so = new SerializedObject(sfx);
            var array = so.FindProperty("clips");
            array.arraySize = clips.Length;
            for (int i = 0; i < clips.Length; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
            so.FindProperty("volume").floatValue = volume;
            so.FindProperty("pitchRange").vector2Value = pitch;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
