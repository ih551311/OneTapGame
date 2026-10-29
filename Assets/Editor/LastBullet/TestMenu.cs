using LastBullet.Inventory;
using UnityEditor;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 테스트용 메뉴. 게임을 재생 중일 때만 쓴다.
    public static class TestMenu
    {
        [MenuItem("LastBullet/테스트/창고 물감 모두 490으로 (재생 중)")]
        static void FillPaints()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("LastBullet", "게임을 재생한 뒤에 눌러 주세요.", "확인");
                return;
            }
            InventoryManager.Instance.DebugSetAllPaints(490);
            Debug.Log("[테스트] 창고 물감을 모두 490으로 만들었어요. 훈련장에서 적을 잡고 클리어하면 '가득 참'이 떠야 해요.");
        }

        [MenuItem("LastBullet/테스트/창고 종이조각 +10 (재생 중)")]
        static void AddScraps()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("LastBullet", "게임을 재생한 뒤에 눌러 주세요.", "확인");
                return;
            }
            InventoryManager.Instance.DebugAddPaperScraps(10);
            Debug.Log("[테스트] 창고 종이조각 +10. 미술관 종이 합성대에서 합성해 보세요.");
        }
    }
}
