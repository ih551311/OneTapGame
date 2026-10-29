using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 씬 정리: 알아보기 쉬운 이름(번호 + 영어)으로 바꾸고, 쓰지 않는 SampleScene을 지운다.
    //   Hideout → 1_Museum (미술관), Stage_Training → 2_Training (훈련장)
    // 유니티 안에서 이름을 바꿔야 씬 안의 연결(GUID)이 유지된다.
    // 메뉴: LastBullet > 씬 정리 (이름 바꾸기, SampleScene 지우기)
    public static class SceneCleanup
    {
        [MenuItem("LastBullet/씬 정리 (이름 바꾸기, SampleScene 지우기)")]
        static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            // 이름을 바꿀 씬이 열려 있지 않게 빈 씬을 잠깐 연다
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Rename("Assets/Scenes/Hideout.unity", "1_Museum");
            Rename("Assets/Scenes/Hideout/NavMesh-Hideout.asset", "NavMesh-1_Museum");
            Rename("Assets/Scenes/Hideout", "1_Museum");
            Rename("Assets/Scenes/Stage_Training.unity", "2_Training");
            Rename("Assets/Scenes/Stage_Training/NavMesh-Stage_Training.asset", "NavMesh-2_Training");
            Rename("Assets/Scenes/Stage_Training", "2_Training");

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/SampleScene.unity") != null)
                AssetDatabase.DeleteAsset("Assets/Scenes/SampleScene.unity");

            // 빌드 목록: 첫 번째 씬이 게임 시작 씬 (타이틀이 있으면 타이틀, 없으면 미술관)
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>();
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/0_Title.unity") != null)
                scenes.Add(new EditorBuildSettingsScene("Assets/Scenes/0_Title.unity", true));
            scenes.Add(new EditorBuildSettingsScene("Assets/Scenes/1_Museum.unity", true));
            scenes.Add(new EditorBuildSettingsScene("Assets/Scenes/2_Training.unity", true));
            EditorBuildSettings.scenes = scenes.ToArray();

            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene("Assets/Scenes/1_Museum.unity");
            Debug.Log("[LastBullet] 씬 정리: 1_Museum(미술관), 2_Training(훈련장). SampleScene 삭제. 빌드 목록 정리.");
        }

        static void Rename(string path, string newName)
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) == null && !AssetDatabase.IsValidFolder(path))
                return; // 이미 바뀌었거나 없음
            string error = AssetDatabase.RenameAsset(path, newName);
            if (!string.IsNullOrEmpty(error))
                Debug.LogError($"[LastBullet] 이름 바꾸기 실패: {path} → {newName}: {error}");
        }
    }
}
