using UnityEngine;

namespace LastBullet.Combat
{
    // 1인칭 바주카의 왼팔을 재장전 시간표(BazookaReloadTimeline)대로 미끄러뜨린다 (2026-11-08).
    // 소총의 ArmReloadSlide와 같은 방식이다: 1인칭 팔은 한 덩어리 메시라서 관절은 못 구부리고, 왼손이 움직이는 만큼 팔 전체를 옮긴다.
    // 이 오브젝트의 좌표는 바주카 모델 단위다 (BazookaHolder의 자식 LeftArm).
    // 붙일 곳: BazookaHolder 아래의 LeftArm (메뉴 LastBullet > Phase 93 바주카포 만들기)
    public class BazookaArmSlide : MonoBehaviour
    {
        [SerializeField] AutoGun gun;

        [Tooltip("평소 왼손 자리 (앞 손잡이, 바주카 모델 안 좌표)")]
        [SerializeField] Vector3 rest = new Vector3(0f, -0.12f, 0.125f);

        [Tooltip("발사관 앞 끝 (모델 안 z)")]
        [SerializeField] float muzzleZ = 0.37f;

        Vector3 restPosition;

        void Awake()
        {
            restPosition = transform.localPosition;
        }

        void LateUpdate()
        {
            if (gun == null)
                return;
            float p = gun.IsReloading ? gun.ReloadProgress : 0f;
            Vector3 target = BazookaReloadTimeline.LeftHand(p, rest, muzzleZ);
            transform.localPosition = restPosition + (target - rest);
        }
    }
}
