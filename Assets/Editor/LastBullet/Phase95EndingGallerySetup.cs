using LastBullet.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 결말 뒤 "내가 그린 그림들" 창을 만든다 (2026-11-11 사용자 요청: 결말 보기가 끝나면 내가 그린 그림을 다 보여 주고,
    //   [다시하기] 버튼으로 진행도와 그림을 초기화. 아래에 "누르면 돌아갈 수 없습니다" 주의 표시).
    //   Canvas/EndingGalleryPopup: 결말 창(EndingPopup)을 복사해 고친다 — 제목 "내가 그린 그림들", 그림 칸(Grid), [다시하기] 버튼, 붉은 주의 글.
    //   결말 창의 마지막 쪽 [다음]이 이 창을 열게 연결한다 (StoryPopup.nextPopup).
    // 먼저 Phase 94가 있어야 한다. 다시 실행하면 새로 만든다. 메뉴: LastBullet > Phase 95 결말 뒤 그림 모음 만들기
    public static class Phase95EndingGallerySetup
    {
        [MenuItem("LastBullet/Phase 95 결말 뒤 그림 모음 만들기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/1_Museum.unity");
            var canvas = ArtSlots.FindCanvas();
            var ending = canvas != null ? canvas.transform.Find("EndingPopup") : null;
            if (ending == null)
            {
                Debug.LogWarning("[LastBullet] 1_Museum: 결말 창(EndingPopup)이 없어 건너뜀. 먼저 Phase 94 메뉴를 실행하세요.");
                return;
            }
            var old = canvas.transform.Find("EndingGalleryPopup");
            if (old != null)
                Object.DestroyImmediate(old.gameObject);

            // 결말 창을 복사해서 고친다 (어두운 배경 Dim + 창 Panel + 닫기 버튼은 그대로)
            var popupGo = Object.Instantiate(ending.gameObject, canvas.transform, false);
            popupGo.name = "EndingGalleryPopup";
            popupGo.transform.SetSiblingIndex(ending.GetSiblingIndex() + 1);
            var story = popupGo.GetComponent<StoryPopup>();
            var closeButton = new SerializedObject(story).FindProperty("closeButton").objectReferenceValue;
            Object.DestroyImmediate(story);

            var panel = popupGo.transform.Find("Panel");
            panel.Find("Title").GetComponent<TMP_Text>().text = "내가 그린 그림들";
            foreach (var name in new[] { "PageImage", "PageNumber", "PrevButton" })
            {
                var t = panel.Find(name);
                if (t != null)
                    Object.DestroyImmediate(t.gameObject);
            }

            // 그림 칸 (창 1500×900 안, 제목 아래 ~ 버튼 위)
            var gridGo = new GameObject("Grid", typeof(RectTransform));
            gridGo.transform.SetParent(panel, false);
            var grid = (RectTransform)gridGo.transform;
            grid.anchorMin = grid.anchorMax = grid.pivot = new Vector2(0.5f, 0.5f);
            grid.anchoredPosition = new Vector2(0f, 55f);
            grid.sizeDelta = new Vector2(1320f, 520f);

            // [다시하기] 버튼: [다음] 버튼을 가운데 아래로 옮겨 쓴다
            var restart = panel.Find("NextButton");
            restart.name = "RestartButton";
            var restartRect = (RectTransform)restart;
            restartRect.anchoredPosition = new Vector2(0f, -305f);
            var restartLabel = restart.Find("Label").GetComponent<TMP_Text>();
            restartLabel.text = "다시하기";

            // 글 쪽 글자 칸을 주의 글로 쓴다 (버튼 아래, 붉은색)
            TMP_Text warning = null, empty = null;
            foreach (var text in panel.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.transform.parent != panel || text.name == "Title")
                    continue;
                warning = text; // 글 쪽 글자 칸 (Phase 75에서 만든 PageText)
            }
            if (warning == null)
            {
                Debug.LogWarning("[LastBullet] 결말 창에 글 칸이 없어 주의 글을 못 만들었어요.");
                return;
            }
            // 그림이 없을 때 글 (주의 글을 복사해 가운데에 둔다)
            empty = Object.Instantiate(warning.gameObject, panel, false).GetComponent<TMP_Text>();
            empty.name = "EmptyText";
            empty.text = "아직 그린 그림이 없어요";
            var emptyRect = empty.rectTransform;
            emptyRect.anchorMin = emptyRect.anchorMax = emptyRect.pivot = new Vector2(0.5f, 0.5f);
            emptyRect.anchoredPosition = new Vector2(0f, 55f);
            emptyRect.sizeDelta = new Vector2(1000f, 100f);
            empty.alignment = TextAlignmentOptions.Center;
            empty.gameObject.SetActive(false);

            warning.name = "Warning";
            warning.gameObject.SetActive(true);
            warning.text = "주의: 누르면 돌아갈 수 없습니다 (진행 상황과 그림이 모두 지워져요)";
            warning.fontSize = 30f;
            warning.enableAutoSizing = false;
            warning.color = new Color(0.78f, 0.16f, 0.12f, 1f);
            warning.alignment = TextAlignmentOptions.Center;
            var warningRect = warning.rectTransform;
            warningRect.anchorMin = warningRect.anchorMax = warningRect.pivot = new Vector2(0.5f, 0.5f);
            warningRect.anchoredPosition = new Vector2(0f, -385f);
            warningRect.sizeDelta = new Vector2(1300f, 50f);

            var popup = popupGo.AddComponent<EndingGalleryPopup>();
            var so = new SerializedObject(popup);
            so.FindProperty("closeButton").objectReferenceValue = closeButton;
            so.FindProperty("grid").objectReferenceValue = grid;
            so.FindProperty("emptyText").objectReferenceValue = empty;
            so.FindProperty("restartButton").objectReferenceValue = restart.GetComponent<UnityEngine.UI.Button>();
            so.FindProperty("restartLabel").objectReferenceValue = restartLabel;
            so.ApplyModifiedPropertiesWithoutUndo();
            popupGo.SetActive(false);

            // 결말 창의 마지막 쪽 [다음] → 이 창
            var endingSo = new SerializedObject(ending.GetComponent<StoryPopup>());
            endingSo.FindProperty("nextPopup").objectReferenceValue = popup;
            endingSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[LastBullet] 1_Museum: 결말 뒤 그림 모음 창(다시하기)을 만들었어요.");
        }
    }
}
