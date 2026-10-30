using LastBullet.Audio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 걷기·달리기 발소리를 풀밭 발소리로 바꾼다 (2026-10-31 사용자 요청: 걸을 때 너무 뽁뽁거림, 에셋 Footstep(Snow and Grass)로 교체).
    // 팩의 Steps_1·2는 거의 순수한 100Hz 저음이라 "뽁" 하고 들려서, 풀밭 발소리 8개(Assets/Audio/Footsteps/footstep_01~08, 원본을 모노·짧게·크게 다듬은 것)로 교체한다.
    // 플레이어가 있는 모든 씬의 MovementSfx에서 걷기·달리기 SfxPlayer의 소리 칸과 크기를 바꾼다.
    // 메뉴: LastBullet > Phase 25 발소리 바꾸기
    public static class Phase25FootstepFix
    {
        static readonly string[] Scenes =
        {
            "Assets/Scenes/1_Museum.unity",
            "Assets/Scenes/2_Forest.unity",
            "Assets/Scenes/3_CastleVillage.unity",
            "Assets/Scenes/4_Desert.unity",
        };

        [MenuItem("LastBullet/Phase 25 발소리 바꾸기")]
        static void Fix()
        {
            var clips = new AudioClip[8];
            for (int i = 0; i < clips.Length; i++)
                clips[i] = AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/Footsteps/footstep_{i + 1:00}.wav");
            if (clips[0] == null)
            {
                EditorUtility.DisplayDialog("LastBullet", "Assets/Audio/Footsteps의 발소리 파일이 없어요.", "확인");
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
                var so = new SerializedObject(sounds);
                Replace((SfxPlayer)so.FindProperty("walkStep").objectReferenceValue, clips, 0.5f, new Vector2(0.95f, 1.05f));
                Replace((SfxPlayer)so.FindProperty("runStep").objectReferenceValue, clips, 0.65f, new Vector2(1.05f, 1.15f));
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[LastBullet] {scene.name}: 발소리를 바꿨어요.");
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
