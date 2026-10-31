using LastBullet.Core;
using LastBullet.Drawing;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.UI
{
    // 타이틀 화면 양쪽의 액자 2개. 그림 보기 팝업에서 고른 작품을 보여 준다 (save.json의 titleFrames).
    // 고른 그림이 없거나 파일이 없으면 빈 액자로 둔다.
    // 붙일 곳: 0_Title의 Canvas/TitleFrames (자식 FrameLeft, FrameRight 각각 Picture(RawImage))
    public class TitleFrames : MonoBehaviour
    {
        [Tooltip("Order: left, right")]
        [SerializeField] RawImage[] pictures = new RawImage[SaveManager.TitleFrameCount];

        void Start()
        {
            Refresh();
        }

        public void Refresh()
        {
            for (int i = 0; i < pictures.Length; i++)
            {
                var texture = PaintingLibrary.LoadTexture(PaintingLibrary.Find(SaveManager.GetTitleFrame(i)));
                pictures[i].texture = texture;
                pictures[i].enabled = texture != null; // 그림이 없으면 액자 안쪽(빈 액자)이 보인다
                if (texture != null)
                    KeepAspect(pictures[i], texture);
            }
        }

        [Tooltip("Frame border thickness (the picture goes inside it)")]
        [SerializeField] float frameBorder = 30f;

        // 그림 비율을 지킨 채 액자 테두리 안쪽에 맞춰 가운데에 둔다 (정사각형 그림이 늘어나지 않게)
        void KeepAspect(RawImage picture, Texture texture)
        {
            var rect = picture.rectTransform;
            Vector2 area = ((RectTransform)rect.parent).rect.size - Vector2.one * (frameBorder * 2f);
            float aspect = (float)texture.width / texture.height;
            Vector2 size = area.x / area.y > aspect
                ? new Vector2(area.y * aspect, area.y)  // 액자가 더 넓으면 높이에 맞춘다
                : new Vector2(area.x, area.x / aspect); // 액자가 더 길쭉하면 너비에 맞춘다
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
        }
    }
}
