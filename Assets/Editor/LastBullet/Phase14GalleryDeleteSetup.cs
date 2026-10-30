using LastBullet.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.EditorTools
{
    // 그림 보기 팝업(GalleryPopup)에 [삭제] 버튼과 "지울까요?" 확인창을 더한다 (2026-10-31 사용자 요청: 그린 그림을 하나씩 골라 지우기).
    //  - 타이틀(0_Title)의 그림 보기: 그림을 고르면 [삭제]가 눌린다. [왼쪽에 걸기] [오른쪽에 걸기] [삭제] 한 줄.
    //  - 미술관(1_Museum) 그림판의 [내 그림]: 그림을 눌러 크게 본 창 아래에 [삭제].
    //  - 액자 고르기(FramePickerPopup)에는 넣지 않는다 (액자에 걸 그림을 고르는 곳).
    // 그림은 모두 Assets/UI의 PNG다 (ButtonLabel_Delete*, Gallery_DeleteConfirm_Message).
    // 메뉴: LastBullet > Phase 14 그림 삭제 버튼 넣기
    public static class Phase14GalleryDeleteSetup
    {
        [MenuItem("LastBullet/Phase 14 그림 삭제 버튼 넣기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            int added = 0;
            added += AddToScene("Assets/Scenes/0_Title.unity");
            added += AddToScene("Assets/Scenes/1_Museum.unity");
            AssetDatabase.SaveAssets();
            Debug.Log($"[LastBullet] Phase 14: 그림 보기 팝업 {added}개에 삭제 버튼과 확인창을 넣었어요.");
        }

        static int AddToScene(string path)
        {
            var scene = EditorSceneManager.OpenScene(path);
            int added = 0;
            foreach (var popup in Object.FindObjectsByType<GalleryPopup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var so = new SerializedObject(popup);
                if (so.FindProperty("frameMode").boolValue || so.FindProperty("deleteButton").objectReferenceValue != null)
                    continue; // 액자 고르기, 또는 이미 넣었다

                bool viewOnly = so.FindProperty("viewOnly").boolValue;
                Button delete;
                if (viewOnly)
                {
                    var largeView = popup.transform.Find("LargeView");
                    delete = CreateButton("DeleteButton", largeView, "Assets/UI/ButtonLabel_Delete.png", new Vector2(0f, -460f), new Vector2(300f, 100f));
                }
                else
                {
                    var panel = popup.transform.Find("Panel");
                    // 한 줄에 세 버튼: [왼쪽에 걸기] [오른쪽에 걸기] [삭제]
                    panel.Find("HangLeftButton").GetComponent<RectTransform>().anchoredPosition = new Vector2(-400f, -360f);
                    panel.Find("HangRightButton").GetComponent<RectTransform>().anchoredPosition = new Vector2(30f, -360f);
                    delete = CreateButton("DeleteButton", panel, "Assets/UI/ButtonLabel_Delete.png", new Vector2(490f, -360f), new Vector2(280f, 100f));
                }

                // 확인창: 팝업 전체를 덮는 어두운 막 + 가운데 창 (맨 위에 그려진다)
                var root = new GameObject("DeleteConfirm", typeof(RectTransform));
                root.transform.SetParent(popup.transform, false);
                Phase4StageSelectSetup.Stretch(root.GetComponent<RectTransform>());
                var dim = new GameObject("Dim", typeof(RectTransform), typeof(Image));
                dim.transform.SetParent(root.transform, false);
                Phase4StageSelectSetup.Stretch(dim.GetComponent<RectTransform>());
                var dimImage = dim.GetComponent<Image>();
                dimImage.color = new Color(0f, 0f, 0f, 0.6f);
                dimImage.raycastTarget = true;

                var box = Phase4StageSelectSetup.CreateImage("Panel", root.transform, "Assets/UI/Popup_Panel.png", Phase4StageSelectSetup.PanelBorder);
                box.GetComponent<Image>().type = Image.Type.Sliced;
                box.GetComponent<Image>().preserveAspect = false;
                Phase4StageSelectSetup.SetRect(box, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860f, 440f));
                var message = Phase4StageSelectSetup.CreateImage("Message", box.transform, "Assets/UI/Gallery_DeleteConfirm_Message.png");
                Phase4StageSelectSetup.SetRect(message, new Vector2(0.5f, 0.5f), new Vector2(0f, 70f), new Vector2(720f, 200f));
                var yes = CreateButton("YesButton", box.transform, "Assets/UI/ButtonLabel_DeleteYes.png", new Vector2(-200f, -120f), new Vector2(320f, 100f));
                var no = CreateButton("NoButton", box.transform, "Assets/UI/ButtonLabel_DeleteNo.png", new Vector2(200f, -120f), new Vector2(320f, 100f));
                root.SetActive(false);

                so.FindProperty("deleteButton").objectReferenceValue = delete;
                so.FindProperty("deleteConfirm").objectReferenceValue = root;
                so.FindProperty("deleteConfirmYesButton").objectReferenceValue = yes;
                so.FindProperty("deleteConfirmNoButton").objectReferenceValue = no;
                so.ApplyModifiedPropertiesWithoutUndo();
                added++;
            }
            if (added > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            return added;
        }

        static Button CreateButton(string name, Transform parent, string labelPath, Vector2 position, Vector2 size)
        {
            var button = Phase4StageSelectSetup.CreateStageButton(name, parent, labelPath, position);
            button.GetComponent<RectTransform>().sizeDelta = size;
            button.transform.Find("Label").GetComponent<RectTransform>().sizeDelta = new Vector2(size.x - 40f, size.y - 20f);
            return button;
        }
    }
}
