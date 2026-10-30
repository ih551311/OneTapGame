using LastBullet.Audio;
using LastBullet.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 플레이어 움직임 소리를 넣는다 (2026-10-31 사용자 요청: 걸을 때, 달릴 때, 점프할 때 전부. 에셋 HALFTONE SFX Pack LITE의 Movement).
    // Player 아래에 MovementSfx 오브젝트(AudioSource 하나 + SfxPlayer 5개 + MovementSounds)를 만든다. 플레이어가 있는 모든 씬.
    //   걷기·달리기 발소리 풀밭 발소리 footstep_01~08(달리기는 더 크고 높게) / 점프 Jump_6·7·22(음이 올라가는 소리) /
    //   착지 Bounce_2·3(낮은 쿵) / 회피 Jump_14·15(스치는 소리)
    // 이미 있으면 건너뛴다. 메뉴: LastBullet > Phase 23 움직임 소리 넣기
    public static class Phase23MovementSoundSetup
    {
        const string Root = "Assets/VOiD1 Gaming - HALFTONE SFX PACK LITE/HALFTONE SFX Pack LITE/Gameplay/3. Movement/";

        // 걷기·달리기 발소리: 풀밭 발소리 8개 (에셋 Footstep(Snow and Grass)를 다듬은 것. 팩의 Steps는 순수한 저음이라 "뽁뽁"거려서 쓰지 않는다)
        static readonly string[] Footsteps = { "Assets/Audio/Footsteps/footstep_01.wav", "Assets/Audio/Footsteps/footstep_02.wav", "Assets/Audio/Footsteps/footstep_03.wav", "Assets/Audio/Footsteps/footstep_04.wav", "Assets/Audio/Footsteps/footstep_05.wav", "Assets/Audio/Footsteps/footstep_06.wav", "Assets/Audio/Footsteps/footstep_07.wav", "Assets/Audio/Footsteps/footstep_08.wav" };

        static readonly string[] Scenes =
        {
            "Assets/Scenes/1_Museum.unity",
            "Assets/Scenes/2_Forest.unity",
            "Assets/Scenes/3_CastleVillage.unity",
            "Assets/Scenes/4_Desert.unity",
        };

        [MenuItem("LastBullet/Phase 23 움직임 소리 넣기")]
        static void Create()
        {
            if (AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "Steps_1.wav") == null)
            {
                EditorUtility.DisplayDialog("LastBullet", "에셋 HALFTONE SFX Pack LITE(Movement)를 먼저 가져오세요.", "확인");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            foreach (var scenePath in Scenes)
            {
                var scene = EditorSceneManager.OpenScene(scenePath);
                var player = Object.FindFirstObjectByType<PlayerMovement>();
                if (player == null || player.GetComponentInChildren<MovementSounds>(true) != null)
                    continue;

                var go = new GameObject("MovementSfx");
                go.transform.SetParent(player.transform, false);
                var source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f; // 2D: 내 발소리는 항상 똑같이 들린다

                var walk = Sfx(go, Footsteps, 0.4f, new Vector2(0.92f, 1.04f));
                var run = Sfx(go, Footsteps, 0.5f, new Vector2(1.04f, 1.16f));
                var jump = Sfx(go, new[] { "Jump_6", "Jump_7", "Jump_22" }, 0.45f, new Vector2(0.96f, 1.04f));
                var land = Sfx(go, new[] { "Bounce_2", "Bounce_3" }, 0.5f, new Vector2(0.92f, 1.04f));
                var dodge = Sfx(go, new[] { "Jump_14", "Jump_15" }, 0.45f, new Vector2(0.96f, 1.06f));

                var script = go.AddComponent<MovementSounds>();
                var so = new SerializedObject(script);
                so.FindProperty("movement").objectReferenceValue = player;
                so.FindProperty("controller").objectReferenceValue = player.GetComponent<CharacterController>();
                so.FindProperty("walkStep").objectReferenceValue = walk;
                so.FindProperty("runStep").objectReferenceValue = run;
                so.FindProperty("jump").objectReferenceValue = jump;
                so.FindProperty("land").objectReferenceValue = land;
                so.FindProperty("dodge").objectReferenceValue = dodge;
                so.ApplyModifiedPropertiesWithoutUndo();

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[LastBullet] {scene.name}: 움직임 소리(MovementSfx)를 넣었어요.");
            }
            AssetDatabase.SaveAssets();
        }

        static SfxPlayer Sfx(GameObject go, string[] names, float volume, Vector2 pitch)
        {
            var sfx = go.AddComponent<SfxPlayer>();
            var so = new SerializedObject(sfx);
            var clips = so.FindProperty("clips");
            clips.arraySize = names.Length;
            for (int i = 0; i < names.Length; i++)
                clips.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(names[i].StartsWith("Assets/") ? names[i] : $"{Root}{names[i]}.wav");
            so.FindProperty("volume").floatValue = volume;
            so.FindProperty("pitchRange").vector2Value = pitch;
            so.ApplyModifiedPropertiesWithoutUndo();
            return sfx;
        }
    }
}
