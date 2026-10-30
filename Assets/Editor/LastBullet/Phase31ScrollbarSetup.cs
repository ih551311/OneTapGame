using LastBullet.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.EditorTools
{
    // 그림 보기 창(GalleryPopup)의 그림 목록에 스크롤바를 단다 (2026-10-31 사용자 요청: 그림이 많을 때 내릴 수 있게).
    // 타이틀의 "그림 보기", 미술관의 "내 그림", 액자의 "액자에 걸 그림" 모두 같은 틀(Panel/Scroll)이라 모두 단다.
    // 스크롤바는 목록 오른쪽 끝에 세로로 놓이고, 그림이 한 화면에 다 보이면 저절로 숨는다(눈에 안 보이고 목록 폭도 그대로).
    // 마우스 휠로도 내려가는 건 그대로다. 그림은 Assets/UI/Scrollbar_Track.png, Scrollbar_Handle.png (9-slice, 직접 그린 그림으로 바꿀 수 있다).
    // 이미 있으면 건너뛴다. 메뉴: LastBullet > Phase 31 그림 목록 스크롤바 넣기
    public static class Phase31ScrollbarSetup
    {
        const float Border = 12f;

        [MenuItem("LastBullet/Phase 31 그림 목록 스크롤바 넣기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            foreach (var path in new[] { "Assets/Scenes/0_Title.unity", "Assets/Scenes/1_Museum.unity" })
            {
                var scene = EditorSceneManager.OpenScene(path);
                int added = 0;
                foreach (var popup in Object.FindObjectsByType<GalleryPopup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    var scroll = popup.transform.Find("Panel/Scroll");
                    if (scroll == null || scroll.Find("Scrollbar") != null)
                        continue;
                    AddScrollbar(scroll);
                    added++;
                }
                if (added > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
                Debug.Log($"[LastBullet] {scene.name}: 그림 목록 {added}개에 스크롤바를 달았어요.");
            }
            AssetDatabase.SaveAssets();
        }

        internal static void AddScrollbar(Transform scroll)
        {
            var go = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            go.transform.SetParent(scroll, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.sizeDelta = new Vector2(24f, -8f); // 폭 24, 위아래로 4씩 여백
            rect.anchoredPosition = Vector2.zero;
            var track = go.GetComponent<Image>();
            track.sprite = ArtSlots.LoadSprite("Assets/UI/Scrollbar_Track.png", Border);
            track.type = Image.Type.Sliced;
            track.color = Color.white;

            var slidingArea = new GameObject("Sliding Area", typeof(RectTransform));
            slidingArea.transform.SetParent(go.transform, false);
            var areaRect = (RectTransform)slidingArea.transform;
            areaRect.anchorMin = Vector2.zero;
            areaRect.anchorMax = Vector2.one;
            areaRect.offsetMin = new Vector2(3f, 3f);
            areaRect.offsetMax = new Vector2(-3f, -3f);

            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(slidingArea.transform, false);
            var handleRect = (RectTransform)handle.transform;
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = handleRect.offsetMax = Vector2.zero;
            var handleImage = handle.GetComponent<Image>();
            handleImage.sprite = ArtSlots.LoadSprite("Assets/UI/Scrollbar_Handle.png", Border);
            handleImage.type = Image.Type.Sliced;
            handleImage.color = Color.white;

            var bar = go.GetComponent<Scrollbar>();
            bar.direction = Scrollbar.Direction.BottomToTop;
            bar.handleRect = handleRect;
            bar.targetGraphic = handleImage;

            var scrollRect = scroll.GetComponent<ScrollRect>();
            scrollRect.verticalScrollbar = bar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scrollRect.verticalScrollbarSpacing = 6f;
            scrollRect.scrollSensitivity = 30f; // 휠 한 칸에 더 많이 움직이게
        }
    }
}
