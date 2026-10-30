using LastBullet.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.EditorTools
{
    // 그림 보기 창(GalleryPopup)에서 두 번째 줄 아래와 고른 그림의 노란 테두리 아래쪽이 잘리던 것을 고친다 (2026-10-31 사용자 요청).
    // 원인: 그림 칸 두 줄이 차지하는 높이(630)보다 스크롤 창 높이(600)가 작았다.
    // 고침: 스크롤 창 높이 620, 칸 위아래 여백 10 (두 줄 = 610이 딱 맞게 들어간다. 세 줄 이상이면 스크롤).
    // 타이틀(0_Title)의 그림 보기, 미술관(1_Museum)의 내 그림과 액자 고르기 모두 같은 틀이라 모두 고친다.
    // 메뉴: LastBullet > Phase 24 그림 보기 칸 잘림 고치기
    public static class Phase24GalleryLayoutFix
    {
        [MenuItem("LastBullet/Phase 24 그림 보기 칸 잘림 고치기")]
        static void Fix()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            foreach (var path in new[] { "Assets/Scenes/0_Title.unity", "Assets/Scenes/1_Museum.unity" })
            {
                var scene = EditorSceneManager.OpenScene(path);
                int fixedCount = 0;
                foreach (var popup in Object.FindObjectsByType<GalleryPopup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    var scroll = popup.transform.Find("Panel/Scroll");
                    if (scroll == null)
                        continue;
                    var rect = (RectTransform)scroll;
                    rect.anchoredPosition = new Vector2(0f, 10f);
                    rect.sizeDelta = new Vector2(1300f, 620f);
                    var grid = scroll.GetComponentInChildren<GridLayoutGroup>(true);
                    if (grid != null)
                        grid.padding = new RectOffset(grid.padding.left, grid.padding.right, 10, 10);
                    fixedCount++;
                }
                if (fixedCount > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
                Debug.Log($"[LastBullet] {scene.name}: 그림 보기 창 {fixedCount}개를 고쳤어요.");
            }
            AssetDatabase.SaveAssets();
        }
    }
}
