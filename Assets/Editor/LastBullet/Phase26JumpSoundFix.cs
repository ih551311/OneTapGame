using LastBullet.Audio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 점프 소리를 "뽀잉"(음이 쭉 올라가는 소리)이 아닌 소리로 바꾼다 (2026-10-31 사용자 요청).
    // 팩의 Jump_6·7·22는 음이 165Hz에서 680Hz까지 계속 올라가서 "뽀잉"으로 들렸다.
    // 대신 음높이가 없는 "툭 + 슉" (땅을 박차는 둔한 소리와 옷자락·숨이 스치는 소리) 3개(Assets/Audio/Jump/jump_01~03)를 쓴다.
    // 직접 합성한 임시 소리다. 플레이어가 있는 모든 씬의 MovementSfx 점프 소리를 바꾼다.
    // 메뉴: LastBullet > Phase 26 점프 소리 바꾸기
    public static class Phase26JumpSoundFix
    {
        static readonly string[] Scenes =
        {
            "Assets/Scenes/1_Museum.unity",
            "Assets/Scenes/2_Forest.unity",
            "Assets/Scenes/3_CastleVillage.unity",
            "Assets/Scenes/4_Desert.unity",
        };

        [MenuItem("LastBullet/Phase 26 점프 소리 바꾸기")]
        static void Fix()
        {
            var clips = new AudioClip[3];
            for (int i = 0; i < clips.Length; i++)
                clips[i] = AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/Jump/jump_{i + 1:00}.wav");
            if (clips[0] == null)
            {
                EditorUtility.DisplayDialog("LastBullet", "Assets/Audio/Jump의 점프 소리 파일이 없어요.", "확인");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            foreach (var path in Scenes)
            {
                var scene = EditorSceneManager.OpenScene(path);
                var sounds = Object.FindFirstObjectByType<MovementSounds>();
                if (sounds == null)
                    continue;
                var sfx = (SfxPlayer)new SerializedObject(sounds).FindProperty("jump").objectReferenceValue;
                var so = new SerializedObject(sfx);
                var array = so.FindProperty("clips");
                array.arraySize = clips.Length;
                for (int i = 0; i < clips.Length; i++)
                    array.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
                so.FindProperty("volume").floatValue = 0.55f;
                so.FindProperty("pitchRange").vector2Value = new Vector2(0.96f, 1.06f);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[LastBullet] {scene.name}: 점프 소리를 바꿨어요.");
            }
            AssetDatabase.SaveAssets();
        }
    }
}
