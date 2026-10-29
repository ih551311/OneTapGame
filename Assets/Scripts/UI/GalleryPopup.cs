using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.UI
{
    // 그린 그림 보기 팝업 (틀만 먼저 만들어 둔 것).
    // 그림이 격자로 보이고, 많으면 위아래로 스크롤한다. 그림이 없으면 "아직 그림이 없어요"를 보여 준다.
    // 실제 그림 목록은 그림 저장(Phase 7)이 생긴 뒤 SetPaintings로 넘겨준다.
    // 붙일 곳: Canvas/GalleryPopup (평소에는 꺼져 있다)
    public class GalleryPopup : PopupBase
    {
        [Tooltip("그림 칸들이 들어가는 곳 (GridLayoutGroup)")]
        [SerializeField] RectTransform content;

        [Tooltip("그림 한 칸의 틀 (꺼 둔다). 자식 Picture에 RawImage")]
        [SerializeField] GameObject itemTemplate;

        [Tooltip("그림이 없을 때 보이는 안내")]
        [SerializeField] GameObject emptyMessage;

        readonly List<GameObject> items = new List<GameObject>();
        IReadOnlyList<Texture2D> paintings = new List<Texture2D>();

        // 보여 줄 그림 목록을 정한다 (Phase 7 이후 그림 목록에서 부른다)
        public void SetPaintings(IReadOnlyList<Texture2D> newPaintings)
        {
            paintings = newPaintings ?? new List<Texture2D>();
            if (IsOpen)
                Refresh();
        }

        protected override void OnOpened()
        {
            Refresh();
        }

        void Refresh()
        {
            itemTemplate.SetActive(false);
            foreach (var item in items)
                Destroy(item);
            items.Clear();

            foreach (var texture in paintings)
            {
                var item = Instantiate(itemTemplate, content);
                item.transform.Find("Picture").GetComponent<RawImage>().texture = texture;
                item.SetActive(true);
                items.Add(item);
            }
            emptyMessage.SetActive(paintings.Count == 0);
        }
    }
}
