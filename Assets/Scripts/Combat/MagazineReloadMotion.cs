using UnityEngine;

namespace LastBullet.Combat
{
    // 총의 재장전 모션 (2026-11-04 사용자 요청: AK47 장전 모션). 총 모델의 탄창(Ak47_Magazine) 부품을 움직인다.
    //   재장전 진행도(AutoGun.ReloadProgress, 0~1)에 맞춰:
    //     0.05~0.25  탄창이 아래로 빠진다           (소리: 탄창 빼기 0.05)
    //     0.25~0.45  탄창이 사라진다 (옛 탄창을 버리고 새 탄창을 꺼내는 동안)
    //     0.45~0.70  새 탄창이 아래에서 올라와 끼워진다 (소리: 탄창 넣기 0.55)
    //     0.70~1.00  제자리 (소리: 장전 0.85)
    //   재장전이 아닐 때는 탄창이 원래 자리에 있다.
    // 3인칭 총과 1인칭 총에 모두 붙인다 (같은 총 모델이라 같은 스크립트). 총 전체의 기울임은 ThirdPersonGunPose·FirstPersonGun이 한다.
    // 붙일 곳: 총 모델(Ak47)의 맨 위 오브젝트 (메뉴 LastBullet > Phase 61 AK47로 바꾸기)
    public class MagazineReloadMotion : MonoBehaviour
    {
        [SerializeField] AutoGun gun;

        [Tooltip("움직일 탄창 부품 (Ak47_Magazine)")]
        [SerializeField] Transform magazine;

        [Tooltip("탄창이 빠지는 방향 (탄창 부모 기준. 총 모델의 아래쪽)")]
        [SerializeField] Vector3 slideDirection = Vector3.down;

        [Tooltip("탄창이 빠지는 거리 (총 모델 단위 m)")]
        [SerializeField] float travel = 0.22f;

        Vector3 restPosition;
        Renderer[] renderers;

        void Awake()
        {
            if (magazine == null)
                return;
            restPosition = magazine.localPosition;
            renderers = magazine.GetComponentsInChildren<Renderer>(true);
        }

        void LateUpdate()
        {
            if (gun == null || magazine == null)
                return;

            float p = gun.IsReloading ? gun.ReloadProgress : 0f;
            float offset = 0f;
            bool visible = true;
            if (p >= 0.05f && p < 0.25f)
                offset = Ease((p - 0.05f) / 0.20f);
            else if (p >= 0.25f && p < 0.45f)
            {
                offset = 1f;
                visible = false;
            }
            else if (p >= 0.45f && p < 0.70f)
                offset = 1.3f * (1f - Ease((p - 0.45f) / 0.25f));

            magazine.localPosition = restPosition + slideDirection.normalized * (travel * offset);
            foreach (var r in renderers)
                if (r != null && r.enabled != visible)
                    r.enabled = visible;
        }

        static float Ease(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }
}
