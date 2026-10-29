using System.IO;
using LastBullet.Core;
using LastBullet.Drawing;
using LastBullet.Inventory;
using UnityEditor;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 테스트용 메뉴
    public static class TestMenu
    {
        // 그림판(Phase 6~7)이 생기기 전에 그림 보기·타이틀 액자를 시험하려고 작품을 만든다.
        // 256×256 흰 종이에 물감 9색으로 원과 선을 무작위로 그린다.
        [MenuItem("LastBullet/테스트/샘플 그림 3장 만들기")]
        static void CreateSamplePaintings()
        {
            for (int n = 0; n < 3; n++)
            {
                const int size = PaintingLibrary.CanvasSize;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var pixels = new Color32[size * size];
                for (int i = 0; i < pixels.Length; i++)
                    pixels[i] = new Color32(250, 248, 242, 255); // 종이색

                for (int shape = 0; shape < 6; shape++)
                {
                    var color = PaintColors.Colors[Random.Range(0, PaintColors.Count)];
                    int cx = Random.Range(30, size - 30), cy = Random.Range(30, size - 30), r = Random.Range(12, 50);
                    for (int y = cy - r; y <= cy + r; y++)
                        for (int x = cx - r; x <= cx + r; x++)
                            if (x >= 0 && y >= 0 && x < size && y < size && (x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r)
                                pixels[y * size + x] = color;
                }
                texture.SetPixels32(pixels);
                texture.Apply();
                PaintingLibrary.Add(texture);
                Object.DestroyImmediate(texture);
                System.Threading.Thread.Sleep(5); // ID(시간)가 겹치지 않게
            }
            Debug.Log($"[테스트] 샘플 그림 3장을 만들었어요: {PaintingLibrary.PaintingsFolder}");
        }

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
