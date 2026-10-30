using System.Linq;
using LastBullet.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.EditorTools
{
    // 전투 장소를 훈련장에서 숲으로 바꾼다 (2026-10-30 사용자 요청: 훈련장 없이 바로 숲).
    //  1) 미술관 장소 선택 팝업: 훈련장 버튼을 지우고 숲 버튼을 가운데로 (처음부터 열려 있다)
    //  2) 훈련장 씬(2_Training)과 그 NavMesh 데이터, 훈련장 글자 그림을 지운다 (되돌리려면 GitHub 기록에서 복구)
    //  3) 빌드 목록을 [0_Title, 1_Museum, 2_Forest]로 정리한다
    // 2_Forest를 먼저 만들어 두어야 한다 (Phase 10 숲 씬 만들기).
    // 메뉴: LastBullet > Phase 10 훈련장을 숲으로 바꾸기
    public static class Phase10StageSwitchSetup
    {
        [MenuItem("LastBullet/Phase 10 훈련장을 숲으로 바꾸기")]
        static void Switch()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/2_Forest.unity") == null)
            {
                EditorUtility.DisplayDialog("LastBullet", "Phase 10 숲 씬 만들기를 먼저 실행하세요.", "확인");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            // 1. 미술관 장소 선택 팝업
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/1_Museum.unity");
            var popup = Object.FindFirstObjectByType<StageSelectPopup>(FindObjectsInactive.Include);
            var panel = popup.transform.Find("Panel");
            var training = panel.Find("TrainingButton");
            if (training != null)
                Object.DestroyImmediate(training.gameObject);
            var forest = panel.Find("ForestButton");
            forest.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -30f);
            forest.GetComponent<Button>().interactable = true;
            forest.Find("Label").GetComponent<Image>().sprite = ArtSlots.LoadSprite("Assets/UI/StageLabel_Forest.png");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            // 2. 빌드 목록을 먼저 정리하고, 훈련장 파일을 지운다
            EditorBuildSettings.scenes = EditorBuildSettings.scenes
                .Where(s => !s.path.Contains("2_Training"))
                .ToArray();
            AssetDatabase.DeleteAsset("Assets/Scenes/2_Training.unity");
            AssetDatabase.DeleteAsset("Assets/Scenes/2_Training");
            AssetDatabase.DeleteAsset("Assets/UI/StageLabel_Training.png");
            AssetDatabase.SaveAssets();
            Debug.Log("[LastBullet] 장소 선택을 숲 하나로 바꾸고 훈련장 씬을 지웠어요. 빌드 목록: " + string.Join(", ", EditorBuildSettings.scenes.Select(s => System.IO.Path.GetFileNameWithoutExtension(s.path))));
        }
    }
}
