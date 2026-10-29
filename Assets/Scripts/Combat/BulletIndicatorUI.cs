using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.Combat
{
    // 탄환 위치 안내 UI.
    // 지금(Phase 3-3): 탄환이 떨어져 있는 동안 크로스헤어 둘레에 자동 복귀까지 남은 시간을 원형 게이지로 보여 준다.
    // Phase 3-4에서 화면 밖 방향 화살표를 더한다.
    // 붙일 곳: HUD
    public class BulletIndicatorUI : MonoBehaviour
    {
        [SerializeField] OneBulletGun gun;

        [Tooltip("Filled(Radial 360) 이미지. 남은 시간만큼 채워진다")]
        [SerializeField] Image returnGauge;

        void Update()
        {
            var autoReturn = gun.State == BulletState.Dropped && gun.DroppedBullet != null
                ? gun.DroppedBullet.GetComponent<BulletAutoReturn>()
                : null;

            returnGauge.enabled = autoReturn != null;
            if (autoReturn != null)
                returnGauge.fillAmount = autoReturn.RemainingRatio;
        }
    }
}
