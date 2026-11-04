using UnityEngine;

namespace LastBullet.Combat
{
    // 1인칭 왼팔을 재장전 시간표(RifleReloadTimeline)대로 미끄러뜨린다 (2026-11-04).
    // 1인칭 팔은 한 덩어리 그림(메시)이라 관절을 구부릴 수는 없어서, 팔 전체를 왼손이 움직이는 만큼 옮긴다
    // (팔은 화면 아래로 빠져 있어서 어깨 쪽이 늘어나는 모습은 보이지 않는다).
    // 이 오브젝트의 좌표는 총 모델 단위다 (총 쥐는 점을 기준으로 맞춘 Ak47Holder의 자식).
    // 붙일 곳: Ak47Holder 아래의 LeftArm (메뉴 LastBullet > Phase 62 1인칭 양손 만들기)
    public class ArmReloadSlide : MonoBehaviour
    {
        [SerializeField] AutoGun gun;

        [Tooltip("평소 왼손 자리 (총열 덮개 아래, 총 모델 안 좌표)")]
        [SerializeField] Vector3 rest = new Vector3(0f, -0.045f, 0.12f);

        [Tooltip("탄창 자리 (총 모델 안 좌표)")]
        [SerializeField] Vector3 magazine = new Vector3(0f, -0.08f, 0.0f);

        [Tooltip("장전 손잡이 자리 (총 모델 안 좌표)")]
        [SerializeField] Vector3 charge = new Vector3(0.03f, 0.05f, -0.02f);

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
            Vector3 target = RifleReloadTimeline.LeftHand(p, rest, magazine, charge);
            transform.localPosition = restPosition + (target - rest);
        }
    }
}
