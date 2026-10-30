using LastBullet.Stage;
using LastBullet.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.EditorTools
{
    // 스테이지 클리어를 없애고 자유 파밍으로 바꾼다 (2026-10-30 사용자 결정).
    //  1) 숲에서 StageManager(모두 처치하면 클리어)와 "클리어" 그림(Canvas/StageClear)을 지운다. 전투 가방은 RunBag 오브젝트로 남긴다.
    //  2) 숲의 ESC 창에 [미술관으로 귀환] 버튼을 더한다 (재료를 창고로 정산하고 돌아가는 방법)
    //  3) 클리어 그림 파일을 지운다
    // 적은 스포너가 죽은 뒤 다시 만든다 (EnemySpawner).
    // 메뉴: LastBullet > Phase 10 스테이지 클리어 없애고 자유 파밍으로
    public static class Phase10FreeFarmSetup
    {
        [MenuItem("LastBullet/Phase 10 스테이지 클리어 없애고 자유 파밍으로")]
        static void Switch()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/2_Forest.unity");

            // 1. StageManager 컴포넌트와 클리어 그림 지우기 (RunBag은 그대로 두고 오브젝트 이름만 알아보기 쉽게)
            var bagObject = Object.FindFirstObjectByType<RunBag>();
            var leftover = bagObject.gameObject.GetComponents<Component>();
            foreach (var component in leftover)
            {
                // 지워진 StageManager는 "Missing" 칸으로 남으니 함께 정리한다
                if (component == null)
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(bagObject.gameObject);
            }
            bagObject.gameObject.name = "RunBag";

            var clear = ArtSlots.FindCanvas().transform.Find("StageClear");
            if (clear != null)
                Object.DestroyImmediate(clear.gameObject);

            // 2. ESC 창: [계속하기] [미술관으로 귀환] [타이틀로 나가기] + 안내
            var pause = Object.FindFirstObjectByType<PauseMenu>();
            var panel = pause.transform.Find("Window/Panel");
            if (panel.Find("HomeButton") == null)
            {
                Phase4StageSelectSetup.SetRect(panel.gameObject, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800f, 700f));
                var resume = panel.Find("ResumeButton");
                var title = panel.Find("TitleButton");
                var warning = panel.Find("LossWarning");
                resume.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 60f);
                var home = Phase4StageSelectSetup.CreateStageButton("HomeButton", panel, "Assets/UI/ButtonLabel_Home.png", new Vector2(0f, -80f));
                title.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -220f);
                warning.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -310f);
                warning.GetComponent<RectTransform>().sizeDelta = new Vector2(760f, 70f);
                home.transform.SetSiblingIndex(title.GetSiblingIndex()); // 타이틀 버튼 앞에 둔다

                var so = new SerializedObject(pause);
                so.FindProperty("homeButton").objectReferenceValue = home;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // 3. 쓰지 않는 클리어 그림 파일
            AssetDatabase.DeleteAsset("Assets/UI/StageClear.png");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[LastBullet] 스테이지 클리어를 없애고 자유 파밍으로 바꿨어요. ESC 창에 [미술관으로 귀환]을 만들었어요.");
        }
    }
}
