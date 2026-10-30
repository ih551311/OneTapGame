using LastBullet.Drawing;
using UnityEngine;

namespace LastBullet.Stage
{
    // 미술관 액자 한 칸 (기획서 2.9). 걸린 그림을 액자 속 그림 면(Picture)에 보여 준다.
    // E로 여는 것과 안내는 GalleryManager가 맡는다 (가장 가까운 액자 하나만 안내하려고).
    // 액자 구조: 이 오브젝트(기능) / 자식 Frame(액자 테두리 모양) / 자식 Picture(그림이 보이는 정사각형 면).
    // 모양을 바꾸려면 Frame을 모델로 바꾸고 Picture 면의 위치·크기만 맞추면 된다.
    // 붙일 곳: PictureFrame_01 ... PictureFrame_09 (slot은 0부터)
    public class PictureFrame : MonoBehaviour
    {
        [Tooltip("액자 칸 번호 (0부터, 최대 8). 걸린 그림은 gallery.json의 이 칸에 저장된다")]
        [SerializeField] int slot;

        [Tooltip("그림이 보이는 면. 이 액자 전용 머티리얼을 쓴다")]
        [SerializeField] Renderer pictureRenderer;

        public int Slot => slot;

        // 지금 걸린 작품 ID (비어 있으면 빈 액자)
        public string CurrentId => GalleryStore.GetFrame(slot);

        void Start()
        {
            Refresh();
        }

        // 고른 그림을 이 액자에 건다 (같은 그림을 여러 액자에 걸 수 있다)
        public void Hang(string paintingId)
        {
            GalleryStore.SetFrame(slot, paintingId);
            Debug.Log($"[PictureFrame] {slot}번 액자에 {paintingId} 걸기");
            Refresh();
        }

        // 걸린 그림을 액자 면에 보여 준다. 없거나 파일이 사라졌으면 빈 액자 색으로 둔다.
        public void Refresh()
        {
            var texture = PaintingLibrary.LoadTexture(PaintingLibrary.Find(CurrentId));
            var material = pictureRenderer.material; // 이 액자 전용 복사본
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", texture != null ? Color.white : new Color32(0xD8, 0xD8, 0xD8, 0xFF));
        }
    }
}
