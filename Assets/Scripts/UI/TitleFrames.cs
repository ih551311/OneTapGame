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
        [Tooltip("왼쪽, 오른쪽 순서")]
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
            }
        }
    }
}
