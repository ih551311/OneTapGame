using System.Collections.Generic;
using LastBullet.Core;
using LastBullet.Drawing;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.UI
{
    // 그림 보기 팝업 (타이틀의 "그림 보기" 버튼으로 연다).
    // 완성한 그림(작품)이 격자로 보이고, 많으면 위아래로 스크롤한다. 그림이 없으면 "아직 그림이 없어요".
    // 그림을 눌러 고른 뒤 "왼쪽에 걸기" / "오른쪽에 걸기"를 누르면 타이틀 화면 액자에 걸린다 (save.json에 저장).
    // 붙일 곳: Canvas/GalleryPopup (평소에는 꺼져 있다)
    public class GalleryPopup : PopupBase
    {
        [Tooltip("그림 칸들이 들어가는 곳 (GridLayoutGroup)")]
        [SerializeField] RectTransform content;

        [Tooltip("그림 한 칸의 틀 (꺼 둔다). Button, 자식 Picture(RawImage), Selected(고른 표시)")]
        [SerializeField] GameObject itemTemplate;

        [Tooltip("그림이 없을 때 보이는 안내")]
        [SerializeField] GameObject emptyMessage;

        [SerializeField] Button hangLeftButton;
        [SerializeField] Button hangRightButton;

        [Tooltip("타이틀 화면 액자. 그림을 걸면 바로 바뀐다")]
        [SerializeField] TitleFrames titleFrames;

        readonly List<GameObject> items = new List<GameObject>();
        string selectedId;

        protected override void OnFirstOpen()
        {
            hangLeftButton.onClick.AddListener(() => Hang(0));
            hangRightButton.onClick.AddListener(() => Hang(1));
        }

        protected override void OnOpened()
        {
            selectedId = null;
            Refresh();
        }

        void Refresh()
        {
            itemTemplate.SetActive(false);
            foreach (var item in items)
                Destroy(item);
            items.Clear();

            var paintings = PaintingLibrary.LoadList();
            foreach (var info in paintings)
            {
                var item = Instantiate(itemTemplate, content);
                item.name = $"Item_{info.id}";
                item.transform.Find("Picture").GetComponent<RawImage>().texture = PaintingLibrary.LoadTexture(info);
                item.transform.Find("Selected").gameObject.SetActive(info.id == selectedId);
                string id = info.id;
                item.GetComponent<Button>().onClick.AddListener(() => Select(id));
                item.SetActive(true);
                items.Add(item);
            }
            emptyMessage.SetActive(paintings.Count == 0);
            hangLeftButton.interactable = hangRightButton.interactable = selectedId != null;
        }

        void Select(string id)
        {
            selectedId = id;
            foreach (var item in items)
                item.transform.Find("Selected").gameObject.SetActive(item.name == $"Item_{id}");
            hangLeftButton.interactable = hangRightButton.interactable = true;
        }

        void Hang(int frameIndex)
        {
            if (selectedId == null)
                return;
            SaveManager.SetTitleFrame(frameIndex, selectedId);
            Debug.Log($"[GalleryPopup] {(frameIndex == 0 ? "왼쪽" : "오른쪽")} 액자에 {selectedId} 걸기");
            if (titleFrames != null)
                titleFrames.Refresh();
        }
    }
}
