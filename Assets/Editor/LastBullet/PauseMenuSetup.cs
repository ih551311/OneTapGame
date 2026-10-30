using LastBullet.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace LastBullet.EditorTools
{
    // ESC 일시정지 창을 미술관과 훈련장 씬에 만든다 (기획서 Phase 9의 일시정지 메뉴를 앞당김).
    // 창: 제목 / (훈련장만) 재료 손실 안내 / [계속하기] / [타이틀로 나가기]
    // 메뉴: LastBullet > ESC 일시정지 창 만들기
    public static class PauseMenuSetup
    {
        [MenuItem("LastBullet/ESC 일시정지 창 만들기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            Build("Assets/Scenes/1_Museum.unity", losesRunBag: false);
            Build("Assets/Scenes/2_Forest.unity", losesRunBag: true);
            AssetDatabase.SaveAssets();
        }

        static void Build(string scenePath, bool losesRunBag)
        {
            var scene = EditorSceneManager.OpenScene(scenePath);
            var canvas = ArtSlots.FindCanvas();
            if (GameObject.Find("PauseMenu") != null)
            {
                Debug.Log($"[LastBullet] {scene.name}: 이미 PauseMenu가 있어서 건너뜀");
                return;
            }
            // 훈련장에는 팝업이 없어서 EventSystem이 없을 수 있다. 버튼을 누르려면 필요하다.
            if (Object.FindAnyObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var root = new GameObject("PauseMenu", typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);
            var script = root.AddComponent<PauseMenu>();

            var window = new GameObject("Window", typeof(RectTransform));
            window.transform.SetParent(root.transform, false);
            Phase4StageSelectSetup.Stretch(window.GetComponent<RectTransform>());

            var dim = new GameObject("Dim", typeof(RectTransform), typeof(Image));
            dim.transform.SetParent(window.transform, false);
            Phase4StageSelectSetup.Stretch(dim.GetComponent<RectTransform>());
            var dimImage = dim.GetComponent<Image>();
            dimImage.color = new Color(0f, 0f, 0f, 0.6f);
            dimImage.raycastTarget = true;

            var panel = Phase4StageSelectSetup.CreateImage("Panel", window.transform, "Assets/UI/Popup_Panel.png", Phase4StageSelectSetup.PanelBorder);
            panel.GetComponent<Image>().type = Image.Type.Sliced;
            panel.GetComponent<Image>().preserveAspect = false;
            Phase4StageSelectSetup.SetRect(panel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800f, 560f));

            var title = Phase4StageSelectSetup.CreateImage("Title", panel.transform, "Assets/UI/Pause_Title.png");
            Phase4StageSelectSetup.SetRect(title, new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(600f, 100f));

            var warning = Phase4StageSelectSetup.CreateImage("LossWarning", panel.transform, "Assets/UI/Pause_LossWarning.png");
            Phase4StageSelectSetup.SetRect(warning, new Vector2(0.5f, 0.5f), new Vector2(0f, 90f), new Vector2(700f, 80f));

            var resume = Phase4StageSelectSetup.CreateStageButton("ResumeButton", panel.transform, "Assets/UI/ButtonLabel_Resume.png", new Vector2(0f, -40f));
            var toTitle = Phase4StageSelectSetup.CreateStageButton("TitleButton", panel.transform, "Assets/UI/ButtonLabel_ToTitle.png", new Vector2(0f, -180f));

            var so = new SerializedObject(script);
            so.FindProperty("window").objectReferenceValue = window;
            so.FindProperty("resumeButton").objectReferenceValue = resume;
            so.FindProperty("titleButton").objectReferenceValue = toTitle;
            so.FindProperty("losesRunBag").boolValue = losesRunBag;
            so.FindProperty("lossWarning").objectReferenceValue = warning;
            so.ApplyModifiedPropertiesWithoutUndo();
            window.SetActive(false);

            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[LastBullet] {scene.name}: ESC 일시정지 창(PauseMenu)을 만들고 씬을 저장했어요.");
        }
    }
}
