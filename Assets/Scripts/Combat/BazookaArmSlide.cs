using UnityEngine;

namespace LastBullet.Combat
{
    // 1인칭 바주카의 왼팔 재장전 움직임 (2026-11-08 만듦, 2026-11-09 사용자 지적으로 고침: "장전할 때 손이 이상하게 보여").
    // 1인칭 팔은 한 덩어리 메시(어깨 쪽에 굵은 통이 이어져 있다)라서 손을 앞으로 뻗게 옮기면 화면 밖에 있던 굵은 팔 끝이 화면에 들어와 막대·큰 덩어리로 보인다
    // (처음엔 팔 전체를 옮겼고, 다음엔 어깨 쪽 끝을 고정하고 돌려 봤지만 둘 다 어색했다).
    // 그래서 1인칭에서는 왼손이 앞 손잡이를 잡은 채로 있고, 재장전 때는 발사관 전체가 살짝 내려갔다 오는 것(FirstPersonGun)과 로켓이 앞에서 밀려 들어오는 것(BazookaWeapon)으로 보여 준다.
    // 손이 로켓을 잡으러 뻗는 움직임은 3인칭(ThirdPersonBazookaPose)에만 있다 (3인칭은 뼈를 구부리는 IK라서 자연스럽다).
    // 이 컴포넌트는 팔이 항상 제자리(앞 손잡이)에 있게 하고, 재장전 중에는 아주 살짝만 아래로 눌러 손이 로켓을 받치는 느낌만 준다.
    // 붙일 곳: BazookaHolder 아래의 LeftArm (메뉴 LastBullet > Phase 93 바주카포 만들기)
    public class BazookaArmSlide : MonoBehaviour
    {
        [SerializeField] AutoGun gun;

        [Tooltip("재장전 중 왼팔이 아래로 눌리는 최대 거리 (바주카 모델 단위)")]
        [SerializeField] float dip = 0.02f;

        Vector3 restPosition;

        void Awake()
        {
            restPosition = transform.localPosition;
        }

        void LateUpdate()
        {
            float p = gun != null && gun.IsReloading ? gun.ReloadProgress : 0f;
            transform.localPosition = restPosition + Vector3.down * (dip * Mathf.Sin(p * Mathf.PI));
        }
    }
}
