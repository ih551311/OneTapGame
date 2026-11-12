using LastBullet.Drawing;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.EditorTools
{
    // 그림판 되돌리기 버튼을 만든다 (2026-11-12: 그림판에 되돌리기가 없다는 점검 결과). Ctrl+Z도 같은 일을 한다 (PaintBoardPopup.Undo).
    //   Canvas/PaintBoardPopup/Panel 아래 UndoButton: "내 그림" 버튼(GalleryButton)을 복사해서 만들고, 아래 버튼 줄을 네 개로 다시 늘어놓는다:
    //   [취소] [되돌리기] [내 그림] [완성] (각 165×90, 패널 폭 760 안에 들어간다).
    // 다시 실행하면 새로 만든다. 메뉴: LastBullet > Phase 102 그림판 되돌리기 버튼 만들기
    public static class Phase102PaintUndoSetup
    {
        [MenuItem("LastBullet/Phase 102 그림판 되돌리기 버튼 만들기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/1_Museum.unity");
            PaintBoardPopup board = null;
            foreach (var p in Resources.FindObjectsOfTypeAll<PaintBoardPopup>())
                if (p.gameObject.scene.IsValid())
                    board = p;
            if (board == null)
            {
                Debug.LogWarning("[LastBullet] 1_Museum: 그림판(PaintBoardPopup)을 찾지 못했어요.");
                return;
            }
            var panel = board.transform.Find("Panel");
            var gallery = panel.Find("GalleryButton");
            var oldUndo = panel.Find("UndoButton");
            if (oldUndo != null)
                Object.DestroyImmediate(oldUndo.gameObject);

            var undo = Object.Instantiate(gallery.gameObject, panel, false);
            undo.name = "UndoButton";
            var label = undo.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = "되돌리기";
            var button = undo.GetComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent(); // 복사된 연결을 비운다 (연결은 PaintBoardPopup이 한다)

            // 버튼 줄: 취소 · 되돌리기 · 내 그림 · 완성
            float y = ((RectTransform)gallery).anchoredPosition.y;
            Place((RectTransform)panel.Find("CancelButton"), 109.5f, y);
            Place((RectTransform)undo.transform, 289.5f, y);
            Place((RectTransform)gallery, 469.5f, y);
            Place((RectTransform)panel.Find("DoneButton"), 649.5f, y);

            var so = new SerializedObject(board);
            so.FindProperty("undoButton").objectReferenceValue = button;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[LastBullet] 1_Museum: 그림판 되돌리기 버튼을 만들었어요 (Ctrl+Z도 같음).");
        }

        static void Place(RectTransform rect, float x, float y)
        {
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(165f, 90f);
        }
    }
}
