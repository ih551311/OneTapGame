using LastBullet.Drawing;
using LastBullet.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastBullet.Stage
{
    // 미술관 액자 (기획서 2.9). 가까이 가면 안내("[E] ...")가 뜨고, E를 누르면 작품 목록이 열린다.
    // 목록에서 그림을 누르면 이 액자에 걸리고(gallery.json에 저장), 걸린 그림이 액자 속 그림 면(Picture)에 보인다.
    // 액자 구조: 이 오브젝트(기능) / 자식 Frame(액자 테두리 모양) / 자식 Picture(그림이 보이는 정사각형 면).
    // 모양을 바꾸려면 Frame을 모델로 바꾸고 Picture 면의 위치·크기만 맞추면 된다.
    // 붙일 곳: PictureFrame_01 ... (slot은 0부터)
    public class PictureFrame : MonoBehaviour
    {
        [Tooltip("액자 칸 번호 (0부터, 최대 8). 걸린 그림은 gallery.json의 이 칸에 저장된다")]
        [SerializeField] int slot;

        [Tooltip("이 거리 안에서 E로 열 수 있다 (기본값)")]
        [SerializeField] float range = 3f;

        [Tooltip("그림이 보이는 면. 이 액자 전용 머티리얼을 쓴다")]
        [SerializeField] Renderer pictureRenderer;

        [Tooltip("작품 목록 팝업 (액자에 걸 그림 고르기)")]
        [SerializeField] GalleryPopup picker;

        [Tooltip("가까이 가면 켜지는 안내 UI (Canvas/Prompt_...)")]
        [SerializeField] GameObject prompt;

        Transform player;

        public int Slot => slot;

        // 지금 걸린 작품 ID (비어 있으면 빈 액자)
        public string CurrentId => GalleryStore.GetFrame(slot);

        void Start()
        {
            var playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
            prompt.SetActive(false);
            Refresh();
        }

        void Update()
        {
            if (player == null)
                return;

            Vector3 offset = player.position - transform.position;
            offset.y = 0f;
            bool near = offset.magnitude <= range && !picker.IsOpen;
            prompt.SetActive(near);

            // 일시정지 중(Time.timeScale 0)에는 열리지 않는다
            if (near && Time.timeScale > 0f && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                picker.OpenForFrame(this);
        }

        // 고른 그림을 이 액자에 건다
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
