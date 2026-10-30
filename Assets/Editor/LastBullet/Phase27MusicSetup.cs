using LastBullet.Audio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 배경음악을 넣는다 (2026-10-31 사용자 요청, 에셋 FREE Adventure RPG Fantasy Game Music Pack / Cheryl Mcc Music).
    //   타이틀 = Happy Clouds (밝고 경쾌한 첫인상) / 미술관 = Harping On (하프, 차분한 갤러리)
    //   전투 맵(숲·성 마을·사막)은 음악을 넣지 않는다 — Claude가 판단: 한 발 탄환 게임은 적의 접근·돌 던지는 소리·탄환 떨어지는 소리를 들어야 하고,
    //   자유 파밍으로 오래 머물러서 음악이 반복되면 질리고 효과음(총소리·발소리)이 묻힌다. 필요하면 나중에 전투 맵에 낮은 소리로 넣을 수 있다.
    // 에셋은 100MB 가까이 되고 쓰는 곡은 둘(LOOP 버전)이라, 안 쓰는 곡은 프로젝트에 그대로 두되 GitHub에는 올라가지 않게 .gitignore에 넣었다.
    // 곡은 메모리를 아끼도록 스트리밍·Vorbis 압축으로 가져온다. 이미 있으면 건너뛴다.
    // 메뉴: LastBullet > Phase 27 배경음악 넣기
    public static class Phase27MusicSetup
    {
        const string Root = "Assets/FREE Adventure RPG Fantasy Game Music Pack/Core Gameplay/";

        static readonly (string scene, string file, float volume)[] Tracks =
        {
            ("Assets/Scenes/0_Title.unity", "Happy Clouds LOOP BPM110.wav", 0.35f),
            ("Assets/Scenes/1_Museum.unity", "Harping On LOOP BPM98.wav", 0.3f),
        };

        [MenuItem("LastBullet/Phase 27 배경음악 넣기")]
        static void Create()
        {
            foreach (var (_, file, _) in Tracks)
            {
                if (AssetDatabase.LoadAssetAtPath<AudioClip>(Root + file) == null)
                {
                    EditorUtility.DisplayDialog("LastBullet", $"에셋 음악 팩의 {file}을 먼저 가져오세요.", "확인");
                    return;
                }
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            // 1. 가져오기 설정: 스트리밍(메모리 절약) + Vorbis 압축
            foreach (var (_, file, _) in Tracks)
            {
                var importer = (AudioImporter)AssetImporter.GetAtPath(Root + file);
                var settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.Streaming;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.6f;
                importer.defaultSampleSettings = settings;
                importer.loadInBackground = true;
                importer.SaveAndReimport();
            }

            // 2. 씬마다 BackgroundMusic
            foreach (var (scenePath, file, volume) in Tracks)
            {
                var scene = EditorSceneManager.OpenScene(scenePath);
                if (Object.FindFirstObjectByType<MusicPlayer>() != null)
                    continue;
                var go = new GameObject("BackgroundMusic");
                go.AddComponent<AudioSource>().playOnAwake = false;
                var player = go.AddComponent<MusicPlayer>();
                var so = new SerializedObject(player);
                so.FindProperty("music").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + file);
                so.FindProperty("volume").floatValue = volume;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[LastBullet] {scene.name}: 배경음악 {file}을 넣었어요.");
            }
            AssetDatabase.SaveAssets();
        }
    }
}
