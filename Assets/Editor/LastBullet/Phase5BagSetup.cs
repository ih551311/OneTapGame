using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.EditorTools
{
    // 전투 가방·창고 표시를 만들 때 쓰는 도우미 (아이콘 그림, 숫자 글자).
    // 예전에는 여기에 "Phase 5-1 재료 드롭과 전투 가방 만들기" 메뉴가 있었는데, 이미 씬에 만들어져 있고
    // 스테이지 클리어 시스템을 없애면서(2026-10-30) 쓸 일이 없어져 메뉴는 지웠다. 도우미는 Phase5InventorySetup이 쓴다.
    public static class Phase5BagSetup
    {
        // 패널 왼쪽 위 기준 위치(center)에 아이콘 그림
        internal static void CreateIcon(Transform parent, string name, string spritePath, Vector2 center, float size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.anchoredPosition = center;
            rect.sizeDelta = new Vector2(size, size);
            var image = go.GetComponent<Image>();
            image.sprite = ArtSlots.LoadSprite(spritePath);
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        internal static TMP_Text CreateCount(Transform parent, string name, Vector2 center, Vector2 size, float fontSize, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.anchoredPosition = center;
            rect.sizeDelta = size;
            var text = go.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.text = "0";
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }
    }
}
