using LastBullet.Audio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 전투 맵(숲·성 마을·사막)에 배경음악을 넣는다 (2026-11-02 사용자 요청, 에셋 Sci-Fi Music Pack 1 / pdkmusic).
    //   곡은 Claude가 골랐다 (직접 들어 보지는 못해서 곡 이름과 소리 분석 — 길이, 평균 크기, 리듬 밀도 — 으로 판단):
    //     숲 = Buying crap_2 (50초, 조용하고 리듬이 듬성듬성해서 오래 들어도 덜 질림)
    //     성 마을 = Illusion (21초, 이름처럼 몽환적, 중간 정도의 움직임)
    //     사막 = Zero8 _2 (33초, 조금 긴장감 있는 중간 크기)
    //   소리는 작게(0.2) 깐다: 이 게임은 적의 발소리·돌 던지는 소리·탄환 떨어지는 소리를 들어야 해서 음악이 묻히지 않게 한다.
    //   LOOP 버전(끝과 처음이 자연스럽게 이어진다)만 쓴다. 곡은 스트리밍·Vorbis 압축으로 가져온다 (메모리·용량 절약).
    // 이미 있으면 곡과 소리 크기를 새로 맞춘다. 메뉴: LastBullet > Phase 57 전투 맵 배경음악 넣기
    public static class Phase57BattleMusicSetup
    {
        const string Root = "Assets/Sci-Fi Music Pack 1/Loops/";

        static readonly (string scene, string file, float volume)[] Tracks =
        {
            ("Assets/Scenes/2_Forest.unity", "SMP1_LOOP_Buying crap_2.wav", 0.2f),
            ("Assets/Scenes/3_CastleVillage.unity", "SMP1_LOOP_Illusion.wav", 0.2f),
            ("Assets/Scenes/4_Desert.unity", "SMP1_LOOP_Zero8 _2.wav", 0.2f),
        };

        [MenuItem("LastBullet/Phase 57 전투 맵 배경음악 넣기")]
        static void Create()
        {
            foreach (var (_, file, _) in Tracks)
            {
                if (AssetDatabase.LoadAssetAtPath<AudioClip>(Root + file) == null)
                {
                    EditorUtility.DisplayDialog("LastBullet", $"Sci-Fi Music Pack 1의 {file}을 먼저 가져오세요.", "확인");
                    return;
                }
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

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

            foreach (var (scenePath, file, volume) in Tracks)
            {
                var scene = EditorSceneManager.OpenScene(scenePath);
                var player = Object.FindFirstObjectByType<MusicPlayer>();
                if (player == null)
                {
                    var go = new GameObject("BackgroundMusic");
                    go.AddComponent<AudioSource>().playOnAwake = false;
                    player = go.AddComponent<MusicPlayer>();
                }
                var so = new SerializedObject(player);
                so.FindProperty("music").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + file);
                so.FindProperty("volume").floatValue = volume;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[LastBullet] {scene.name}: 배경음악 {file}을 넣었어요 (소리 {volume}).");
            }
            AssetDatabase.SaveAssets();
        }
    }
}
