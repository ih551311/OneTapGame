using LastBullet.Audio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 모든 씬에 클릭 소리(ClickSound)를 넣는다 (2026-10-31 사용자 요청, 에셋 Free UI Click Sound Pack의 나무 소리 3개).
    // 따뜻한 나무 클릭 소리가 오래된 미술관 분위기와 어울려서 골랐다. 이미 있으면 건너뛴다.
    // 메뉴: LastBullet > Phase 22 클릭 소리 넣기
    public static class Phase22ClickSoundSetup
    {
        const string Root = "Assets/Free UI Click Sound Effects Pack/AUDIO/Wooden/";
        const float Volume = 0.6f;

        static readonly string[] Scenes =
        {
            "Assets/Scenes/0_Title.unity",
            "Assets/Scenes/1_Museum.unity",
            "Assets/Scenes/2_Forest.unity",
            "Assets/Scenes/3_CastleVillage.unity",
            "Assets/Scenes/4_Desert.unity",
        };

        [MenuItem("LastBullet/Phase 22 클릭 소리 넣기")]
        static void Create()
        {
            var clips = new[]
            {
                AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "SFX_UI_Click_Organic_Wooden_Thin_1.wav"),
                AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "SFX_UI_Click_Organic_Wooden_Thin_2.wav"),
                AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "SFX_UI_Click_Organic_Wooden_Select_1.wav"),
            };
            if (clips[0] == null)
            {
                EditorUtility.DisplayDialog("LastBullet", "에셋 Free UI Click Sound Pack을 먼저 가져오세요.", "확인");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            foreach (var scenePath in Scenes)
            {
                var scene = EditorSceneManager.OpenScene(scenePath);
                if (Object.FindFirstObjectByType<ClickSound>() != null)
                    continue;

                var go = new GameObject("ClickSound");
                var source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f; // 2D: 화면 소리라 어디서든 똑같이 들린다
                var sfx = go.AddComponent<SfxPlayer>();
                var so = new SerializedObject(sfx);
                var array = so.FindProperty("clips");
                array.arraySize = clips.Length;
                for (int i = 0; i < clips.Length; i++)
                    array.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
                so.FindProperty("volume").floatValue = Volume;
                so.ApplyModifiedPropertiesWithoutUndo();
                go.AddComponent<ClickSound>();

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[LastBullet] {scene.name}: 클릭 소리(ClickSound)를 넣었어요.");
            }
            AssetDatabase.SaveAssets();
        }
    }
}
