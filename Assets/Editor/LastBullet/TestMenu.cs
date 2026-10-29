using System.IO;
using LastBullet.Core;
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

        [MenuItem("LastBullet/테스트/저장 폴더 열기")]
        static void OpenSaveFolder()
        {
            Directory.CreateDirectory(SaveManager.FolderPath);
            EditorUtility.RevealInFinder(File.Exists(SaveManager.SavePath) ? SaveManager.SavePath : SaveManager.FolderPath);
        }

        [MenuItem("LastBullet/테스트/저장 파일 지우기 (처음부터)")]
        static void DeleteSave()
        {
            if (Application.isPlaying)
            {
                EditorUtility.DisplayDialog("LastBullet", "재생을 멈춘 뒤에 눌러 주세요.", "확인");
                return;
            }
            if (!EditorUtility.DisplayDialog("LastBullet", "저장 파일(save.json)을 지울까요?\n창고 재료와 해금이 모두 처음으로 돌아가요.", "지우기", "취소"))
                return;
            SaveManager.DeleteSave();
            Debug.Log("[테스트] 저장 파일을 지웠어요.");
        }
    }
}
