using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 타이틀의 도움말 등 팝업이 제목 글자 아래에 가려지고 버튼이 안 눌리던 것을 고친다 (2026-11-05 사용자 요청).
    //   원인: 제목 글자(Text (TMP))가 Canvas의 맨 아래 자식(= 맨 위에 그려짐)이라 팝업을 덮고, 클릭도 받아 가렸다.
    //   고침: 제목 글자를 팝업들보다 앞(아래에 그려짐)으로 옮기고, 클릭을 받지 않게 한다 (Raycast Target 끔).
    // 메뉴: LastBullet > Phase 73 타이틀 팝업 순서 바로잡기
    public static class Phase73TitlePopupOrderFix
    {
        [MenuItem("LastBullet/Phase 73 타이틀 팝업 순서 바로잡기")]
        static void Fix()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/0_Title.unity");
            var canvas = ArtSlots.FindCanvas();
            if (canvas == null)
            {
                Debug.LogWarning("[LastBullet] 0_Title: Canvas가 없어 건너뜀");
                return;
            }

            // 가장 앞에 있는 팝업(…Popup) 자리를 찾는다. 제목 글자는 그 바로 앞으로 옮겨 모든 팝업 아래에 그려지게 한다.
            int firstPopup = int.MaxValue;
            for (int i = 0; i < canvas.transform.childCount; i++)
                if (canvas.transform.GetChild(i).name.EndsWith("Popup"))
                {
                    firstPopup = i;
                    break;
                }

            int moved = 0;
            foreach (var text in canvas.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.transform.parent != canvas.transform)
                    continue; // 캔버스 바로 아래 글자(제목)만
                text.raycastTarget = false;
                if (firstPopup != int.MaxValue && text.transform.GetSiblingIndex() > firstPopup)
                {
                    text.transform.SetSiblingIndex(firstPopup);
                    firstPopup++; // 방금 끼워 넣어 팝업이 한 칸 밀렸다
                    moved++;
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[LastBullet] 0_Title: 제목 글자 {moved}개를 팝업 아래로 옮기고 클릭이 통과하게 했어요.");
        }
    }
}
