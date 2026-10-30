using LastBullet.Drawing;
using LastBullet.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.EditorTools
{
    // 미술관 그림판에서 내가 완성한 그림을 볼 수 있게 한다.
    //  1) 타이틀의 그림 보기 팝업(GalleryPopup)을 미술관에 만든다 (보기 전용: 걸기 버튼 없음, 그림을 누르면 크게 보임)
    //  2) 그림판 아래 줄을 [취소] [내 그림] [완성] 세 버튼으로 바꾼다
    // 메뉴: LastBullet > 그림판 내 그림 보기 만들기
    public static class Phase7GalleryViewSetup
    {
        [MenuItem("LastBullet/그림판 내 그림 보기 만들기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/1_Museum.unity");
            var canvas = ArtSlots.FindCanvas();
            var board = Object.FindFirstObjectByType<PaintBoardPopup>(FindObjectsInactive.Include);
            if (board == null)
            {
                EditorUtility.DisplayDialog("LastBullet", "Phase 6-1 메뉴를 먼저 실행하세요.", "확인");
                return;
            }
            if (canvas.transform.Find("GalleryPopup") != null)
            {
                EditorUtility.DisplayDialog("LastBullet", "이미 GalleryPopup이 있어요.", "확인");
                return;
            }

            // 1. 보기 전용 작품 팝업
            var gallery = TitleSetup.CreateGallery(canvas.transform, null);
            gallery.transform.SetAsLastSibling(); // 그림판 위에 그려지게
            var panel = gallery.transform.Find("Panel");

            var largeView = new GameObject("LargeView", typeof(RectTransform));
            largeView.transform.SetParent(gallery.transform, false);
            Phase4StageSelectSetup.Stretch(largeView.GetComponent<RectTransform>());

            var dim = new GameObject("Dim", typeof(RectTransform), typeof(Image), typeof(Button));
            dim.transform.SetParent(largeView.transform, false);
            Phase4StageSelectSetup.Stretch(dim.GetComponent<RectTransform>());
            var dimImage = dim.GetComponent<Image>();
            dimImage.color = new Color(0f, 0f, 0f, 0.75f);
            dimImage.raycastTarget = true;
            var closeArea = dim.GetComponent<Button>();
            closeArea.transition = Selectable.Transition.None;

            var frame = Phase4StageSelectSetup.CreateImage("Frame", largeView.transform, "Assets/UI/Gallery_Frame.png", 24f);
            frame.GetComponent<Image>().type = Image.Type.Sliced;
            frame.GetComponent<Image>().preserveAspect = false;
            Phase4StageSelectSetup.SetRect(frame, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * 760f); // 정사각형
            var picture = new GameObject("Picture", typeof(RectTransform), typeof(RawImage));
            picture.transform.SetParent(frame.transform, false);
            var pictureRect = picture.GetComponent<RectTransform>();
            Phase4StageSelectSetup.Stretch(pictureRect);
            pictureRect.offsetMin = new Vector2(30f, 30f);
            pictureRect.offsetMax = new Vector2(-30f, -30f);
            var pictureImage = picture.GetComponent<RawImage>();
            pictureImage.raycastTarget = false;
            largeView.SetActive(false);

            var gallerySo = new SerializedObject(gallery);
            gallerySo.FindProperty("viewOnly").boolValue = true;
            gallerySo.FindProperty("largeView").objectReferenceValue = largeView;
            gallerySo.FindProperty("largePicture").objectReferenceValue = pictureImage;
            gallerySo.FindProperty("largeViewCloseArea").objectReferenceValue = closeArea;
            gallerySo.ApplyModifiedPropertiesWithoutUndo();

            // 2. 그림판 아래 줄: [취소] [내 그림] [완성]
            var paintPanel = board.transform.Find("Panel");
            var cancel = paintPanel.Find("CancelButton").gameObject;
            var done = paintPanel.Find("DoneButton").gameObject;
            ResizeBottomButton(cancel, 154f);
            ResizeBottomButton(done, 606f);
            var galleryButton = Phase6PaintBoardSetup.PaintButton("GalleryButton", paintPanel, "Cancel", "Gallery", false, 380f, 820f);
            ResizeBottomButton(galleryButton.gameObject, 380f);

            var boardSo = new SerializedObject(board);
            boardSo.FindProperty("galleryButton").objectReferenceValue = galleryButton;
            boardSo.FindProperty("galleryPopup").objectReferenceValue = gallery;
            boardSo.ApplyModifiedPropertiesWithoutUndo();

            Selection.activeGameObject = gallery.gameObject;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[LastBullet] 미술관에 보기 전용 GalleryPopup을 만들고 그림판에 [내 그림] 버튼을 연결한 뒤 1_Museum 씬을 저장했어요.");
        }

        static void ResizeBottomButton(GameObject button, float x)
        {
            Phase6PaintBoardSetup.Place(button, x, 820f, 200f, 90f);
            var label = button.transform.Find("Label");
            if (label != null)
                label.GetComponent<RectTransform>().sizeDelta = new Vector2(180f, 90f);
        }
    }
}
