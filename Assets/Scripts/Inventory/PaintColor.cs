using UnityEngine;

namespace LastBullet.Inventory
{
    // 물감 9색 (기획서 2.7)
    public enum PaintColor { Red, Orange, Yellow, Green, Blue, Indigo, Purple, White, Black }

    public static class PaintColors
    {
        public const int Count = 9;

        public static readonly string[] KoreanNames = { "빨강", "주황", "노랑", "초록", "파랑", "남색", "보라", "하양", "검정" };

        // 그림판에서 칠할 색 (기본값). UI 아이콘 색은 Assets/UI/Icon_Paint_*.png 그림에 들어 있다.
        public static readonly Color32[] Colors =
        {
            new Color32(0xEF, 0x44, 0x44, 0xFF), new Color32(0xF9, 0x73, 0x16, 0xFF), new Color32(0xFA, 0xCC, 0x15, 0xFF),
            new Color32(0x22, 0xC5, 0x5E, 0xFF), new Color32(0x3B, 0x82, 0xF6, 0xFF), new Color32(0x37, 0x30, 0xA3, 0xFF),
            new Color32(0xA8, 0x55, 0xF7, 0xFF), new Color32(0xFF, 0xFF, 0xFF, 0xFF), new Color32(0x17, 0x17, 0x17, 0xFF),
        };

        public static PaintColor Random() => (PaintColor)UnityEngine.Random.Range(0, Count); // 9색 균등 확률
    }
}
