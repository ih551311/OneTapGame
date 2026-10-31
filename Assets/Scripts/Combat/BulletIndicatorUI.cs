using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.Combat
{
    // 탄환 위치 안내 UI. 탄환이 떨어져 있는 동안
    // 1) 크로스헤어 둘레에 자동 복귀까지 남은 시간을 원형 게이지로 보여 주고
    // 2) 탄환이 화면 밖에 있으면 화면 가장자리에 탄환 쪽을 가리키는 화살표를 보여 준다.
    // (탄환 위의 빛 기둥은 DroppedBullet 프리팹 안에 있다)
    // 붙일 곳: HUD
    public class BulletIndicatorUI : MonoBehaviour
    {
        [SerializeField] OneBulletGun gun;

        [Tooltip("Filled (Radial 360) image. Fills according to the remaining time.")]
        [SerializeField] Image returnGauge;

        [Tooltip("Arrow image that points up")]
        [SerializeField] Image arrow;

        [Tooltip("Margin between the arrow and the screen edge (px at 1920x1080)")]
        [SerializeField] float edgeMargin = 60f;

        Camera cam;
        RectTransform canvasRect;

        void Awake()
        {
            cam = Camera.main;
            canvasRect = (RectTransform)transform;
        }

        void Update()
        {
            var bullet = gun.State == BulletState.Dropped ? gun.DroppedBullet : null;

            var autoReturn = bullet != null ? bullet.GetComponent<BulletAutoReturn>() : null;
            returnGauge.enabled = autoReturn != null;
            if (autoReturn != null)
                returnGauge.fillAmount = autoReturn.RemainingRatio;

            UpdateArrow(bullet);
        }

        void UpdateArrow(GameObject bullet)
        {
            if (arrow == null)
                return;
            if (bullet == null)
            {
                arrow.enabled = false;
                return;
            }

            // 화면 위치: x, y는 0~1이면 화면 안, z가 음수면 카메라 뒤
            Vector3 viewport = cam.WorldToViewportPoint(bullet.transform.position);
            bool onScreen = viewport.z > 0f && viewport.x > 0f && viewport.x < 1f && viewport.y > 0f && viewport.y < 1f;
            arrow.enabled = !onScreen;
            if (onScreen)
                return;

            // 화면 가운데에서 탄환 쪽으로 향하는 방향 (UI 좌표)
            Vector2 size = canvasRect.rect.size;
            Vector2 direction = new Vector2((viewport.x - 0.5f) * size.x, (viewport.y - 0.5f) * size.y);
            if (viewport.z < 0f)
                direction = -direction; // 카메라 뒤에 있으면 방향이 뒤집혀 나오므로 되돌린다
            if (direction.sqrMagnitude < 0.0001f)
                direction = Vector2.down;

            // 그 방향으로 화면 가장자리(여백 안쪽)까지 늘린다
            Vector2 half = size / 2f - Vector2.one * edgeMargin;
            float scaleX = Mathf.Abs(direction.x) > 0.0001f ? half.x / Mathf.Abs(direction.x) : float.MaxValue;
            float scaleY = Mathf.Abs(direction.y) > 0.0001f ? half.y / Mathf.Abs(direction.y) : float.MaxValue;
            arrow.rectTransform.anchoredPosition = direction * Mathf.Min(scaleX, scaleY);

            // 화살표 그림은 위쪽을 가리키므로 90도를 빼서 돌린다
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            arrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
