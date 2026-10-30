using LastBullet.Drawing;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.EditorTools
{
    // Phase 7-2: 그림판 팝업에 "취소 확인창"을 만든다.
    // 확인창: 메시지 / [계속 그리기] [그만두기]. 평소에는 꺼져 있고, 취소 버튼·ESC로 뜬다.
    // 메뉴: LastBullet > Phase 7-2 그림판 취소 확인창 만들기
    public static class Phase7ConfirmSetup
    {
        [MenuItem("LastBullet/Phase 7-2 그림판 취소 확인창 만들기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/1_Museum.unity");
            var popup = Object.FindFirstObjectByType<PaintBoardPopup>(FindObjectsInactive.Include);
            if (popup == null)
            {
                EditorUtility.DisplayDialog("LastBullet", "Phase 6-1 메뉴를 먼저 실행하세요.", "확인");
                return;
            }
            if (popup.transform.Find("ConfirmDialog") != null)
            {
                EditorUtility.DisplayDialog("LastBullet", "이미 ConfirmDialog가 있어요.", "확인");
                return;
            }

            // 팝업 전체를 덮는 어두운 막 (뒤의 그림판을 못 누르게) + 가운데 창
            var root = new GameObject("ConfirmDialog", typeof(RectTransform));
            root.transform.SetParent(popup.transform, false);
            Phase4StageSelectSetup.Stretch(root.GetComponent<RectTransform>());

            var dim = new GameObject("Dim", typeof(RectTransform), typeof(Image));
            dim.transform.SetParent(root.transform, false);
            Phase4StageSelectSetup.Stretch(dim.GetComponent<RectTransform>());
            var dimImage = dim.GetComponent<Image>();
            dimImage.color = new Color(0f, 0f, 0f, 0.5f);
            dimImage.raycastTarget = true;

            var panel = Phase6PaintBoardSetup.Sliced("Panel", root.transform, "Assets/UI/Paint_Panel.png", 40f);
            Phase4StageSelectSetup.SetRect(panel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 360f));

            var message = Phase4StageSelectSetup.CreateImage("Message", panel.transform, "Assets/UI/Paint_Confirm_Message.png");
            Phase6PaintBoardSetup.Place(message, 360f, 120f, 640f, 160f);

            var keepDrawing = Phase6PaintBoardSetup.PaintButton("ContinueButton", panel.transform, "Cancel", "Continue", false, 190f, 280f);
            var discard = Phase6PaintBoardSetup.PaintButton("DiscardButton", panel.transform, "Done", "Discard", true, 530f, 280f);

            var so = new SerializedObject(popup);
            so.FindProperty("confirmDialog").objectReferenceValue = root;
            so.FindProperty("confirmContinueButton").objectReferenceValue = keepDrawing;
            so.FindProperty("confirmDiscardButton").objectReferenceValue = discard;
            so.ApplyModifiedPropertiesWithoutUndo();
            root.SetActive(false);

            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[LastBullet] Phase 7-2: 그림판에 취소 확인창(ConfirmDialog)을 만들고 1_Museum 씬을 저장했어요.");
        }
    }
}
