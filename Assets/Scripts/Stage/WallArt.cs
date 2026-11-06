using LastBullet.Drawing;
using UnityEngine;

namespace LastBullet.Stage
{
    // 미술관 벽을 채우는 장식 그림 액자 (2026-11-06 사용자 요청: "미술관처럼 꾸며").
    // 북쪽 벽의 진짜 액자(PictureFrame, 작품을 걸고 E로 여는 9칸)와 달리 장식이라 만질 수 없다.
    // 게임을 시작할 때마다 내가 그린 작품 중 하나를 무작위로 골라 그림 면(Picture)에 보여 준다. 작품이 없으면 이 액자는 빈 액자로 둔다.
    // 작품이 새로 생기거나 지워지면 다시 고른다.
    // 액자 구조: 이 오브젝트 / 자식 Frame(테두리 모양) / 자식 Picture(그림이 보이는 정사각형 면). 모양을 바꾸려면 Frame만 모델로 바꾼다.
    // 붙일 곳: MuseumHall/WallArt/Art_xx (메뉴 LastBullet > Phase 85 미술관 벽 그림 액자)
    public class WallArt : MonoBehaviour
    {
        [Tooltip("그림이 보이는 면. 이 액자 전용 머티리얼을 쓴다")]
        [SerializeField] Renderer pictureRenderer;

        void OnEnable()
        {
            PaintingLibrary.Changed += Refresh;
        }

        void OnDisable()
        {
            PaintingLibrary.Changed -= Refresh;
        }

        void Start()
        {
            Refresh();
        }

        // 내 작품 중 하나를 무작위로 보여 준다 (없으면 빈 액자 색)
        public void Refresh()
        {
            if (pictureRenderer == null)
                return;
            var list = PaintingLibrary.LoadList();
            Texture2D texture = null;
            for (int tries = 0; tries < 5 && texture == null && list.Count > 0; tries++)
                texture = PaintingLibrary.LoadTexture(list[Random.Range(0, list.Count)]);

            var material = pictureRenderer.material; // 이 액자 전용 복사본
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", texture != null ? Color.white : new Color32(0xD8, 0xD8, 0xD8, 0xFF));
        }
    }
}
