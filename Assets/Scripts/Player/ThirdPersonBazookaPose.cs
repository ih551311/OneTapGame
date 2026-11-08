using LastBullet.Combat;
using UnityEngine;

namespace LastBullet.Player
{
    // 3인칭에서 캐릭터가 바주카를 어깨에 얹고 쏘는 자세 (2026-11-08 사용자 요청: 바주카 전용 모션).
    // 소총 자세(ThirdPersonGunPose)는 총을 가슴 앞에 안지만, 바주카는 이렇게 다르다:
    //  1) 발사관을 오른쪽 어깨 위에 얹는다 (뒤 끝이 어깨 뒤로 나오고 앞 끝이 앞으로 길게 나간다)
    //  2) 오른손은 뒤 손잡이, 왼손은 앞 손잡이를 쥔다 (두 팔 IK)
    //  3) 쏘면 발사관이 어깨에서 뒤로 밀렸다 돌아온다 (반동)
    //  4) 재장전 때는 발사관 앞이 위로 들리고, 왼손이 앞으로 뻗어 새 로켓을 잡아 끼운 뒤 돌아온다 (BazookaReloadTimeline)
    // 모델(RPG7)이 달린 ThirdPersonBazooka 오브젝트의 앞(+Z) = 총구, 위(+Y) = 위쪽이다. 팔 뼈 연결과 손 모양 값은 ThirdPersonGunPose와 같다.
    // 붙일 곳: R.Hand 아래의 ThirdPersonBazooka (메뉴 LastBullet > Phase 93 바주카포 만들기)
    public class ThirdPersonBazookaPose : MonoBehaviour
    {
        [Header("뼈 (캐릭터 모델)")]
        [SerializeField] Transform body;
        [SerializeField] Transform chest;
        [SerializeField] Transform rightUpper;
        [SerializeField] Transform rightMid;
        [SerializeField] Transform rightHand;
        [SerializeField] Transform leftUpper;
        [SerializeField] Transform leftMid;
        [SerializeField] Transform leftHand;

        [Header("자리 (가슴 뼈 기준, 몸이 보는 방향 기준 m)")]
        [Tooltip("발사관이 어깨에 닿는 점이 놓이는 자리: x 오른쪽, y 위, z 앞")]
        [SerializeField] Vector3 shoulderFromChest = new Vector3(0.2f, 0.17f, 0.02f);

        [Tooltip("발사관 모델 안에서 어깨에 닿는 점 (모델 크기 1 기준)")]
        [SerializeField] Vector3 shoulderInGun = new Vector3(0f, 0f, -0.4f);

        [Tooltip("오른손이 쥐는 뒤 손잡이 (모델 안)")]
        [SerializeField] Vector3 rearGripInGun = new Vector3(0f, -0.13f, -0.075f);

        [Tooltip("왼손이 쥐는 앞 손잡이 (모델 안)")]
        [SerializeField] Vector3 frontGripInGun = new Vector3(0f, -0.12f, 0.125f);

        [Tooltip("발사관 앞 끝 (모델 안 z). 재장전 때 왼손이 로켓을 받으러 가는 기준")]
        [SerializeField] float muzzleZ = 0.37f;

        [Tooltip("평소 총구를 위로 드는 각도 (도)")]
        [SerializeField] float restPitch = 3f;

        [Header("손 모양")]
        [SerializeField] Vector3 fistInHand = new Vector3(-0.02f, -0.055f, 0.10f);
        [SerializeField] Vector3 leftHandEuler = new Vector3(0f, 0f, -90f);
        [SerializeField] Vector3 leftFistInHand = new Vector3(0.02f, -0.055f, 0.10f);

        [Header("움직임")]
        [SerializeField] AutoGun gun;
        [Tooltip("재장전 때 총구가 위로 드는 각도 (도)")]
        [SerializeField] float reloadTilt = 14f;
        [Tooltip("쏠 때 뒤로 밀리는 거리 (m)")]
        [SerializeField] float recoilBack = 0.12f;
        [Tooltip("쏠 때 총구가 들리는 각도 (도)")]
        [SerializeField] float recoilPitch = 7f;
        [SerializeField] float recoilRecover = 7f;

        // 에디터 도구용: 0 이상이면 총 대신 이 값을 재장전 진행도로 쓴다 (1인칭 팔 모양을 뽑을 때, 메뉴 Phase 93)
        [System.NonSerialized] public float ForcedReloadProgress = -1f;

        static readonly Quaternion RightHandInGun = Quaternion.LookRotation(Vector3.forward, Vector3.left);
        float recoil;

        void OnEnable()
        {
            if (gun != null)
                gun.Fired += OnFired;
        }

        void OnDisable()
        {
            if (gun != null)
                gun.Fired -= OnFired;
        }

        void OnFired()
        {
            recoil = 1f;
        }

        void LateUpdate()
        {
            if (body == null || chest == null || rightUpper == null || rightHand == null)
                return;
            recoil = Mathf.MoveTowards(recoil, 0f, Time.deltaTime * recoilRecover);

            Vector3 forward = Vector3.ProjectOnPlane(body.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f)
                forward = body.forward;
            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward);

            float reloadProgress = gun != null && gun.IsReloading ? gun.ReloadProgress : 0f;
            if (ForcedReloadProgress >= 0f)
                reloadProgress = ForcedReloadProgress;
            float reload = reloadProgress > 0f ? Mathf.Sin(reloadProgress * Mathf.PI) : 0f;

            Quaternion rotation = Quaternion.LookRotation(forward, Vector3.up);
            float pitch = restPitch + recoil * recoilPitch + reload * reloadTilt;
            rotation = Quaternion.AngleAxis(-pitch, rotation * Vector3.right) * rotation;

            float scale = transform.lossyScale.x;
            Vector3 shoulderWorld = chest.position + right * shoulderFromChest.x + Vector3.up * shoulderFromChest.y + forward * (shoulderFromChest.z - recoil * recoilBack);
            Vector3 gunPosition = shoulderWorld - rotation * (shoulderInGun * scale);

            // 오른손: 뒤 손잡이
            Vector3 rearWorld = gunPosition + rotation * (rearGripInGun * scale);
            Quaternion rightRotation = rotation * Quaternion.Inverse(RightHandInGun);
            Vector3 rightTarget = rearWorld - rightRotation * fistInHand;
            Vector3 rightPole = right * 0.6f - Vector3.up * 0.8f - forward * 0.2f;
            ThirdPersonGunPose.SolveTwoBone(rightUpper, rightMid, rightHand, rightTarget, rightPole);
            rightHand.rotation = rightRotation;

            // 왼손: 앞 손잡이 (재장전 때는 시간표대로 로켓을 받으러 간다)
            if (leftUpper != null && leftMid != null && leftHand != null)
            {
                Vector3 leftInGun = BazookaReloadTimeline.LeftHand(reloadProgress, frontGripInGun, muzzleZ);
                Vector3 leftGripWorld = gunPosition + rotation * (leftInGun * scale);
                Quaternion leftRotation = rotation * Quaternion.Euler(leftHandEuler);
                Vector3 leftTarget = leftGripWorld - leftRotation * leftFistInHand;
                Vector3 leftPole = -right * 0.6f - Vector3.up * 0.8f - forward * 0.2f;
                ThirdPersonGunPose.SolveTwoBone(leftUpper, leftMid, leftHand, leftTarget, leftPole);
                leftHand.rotation = leftRotation;
            }

            transform.SetPositionAndRotation(gunPosition, rotation);
        }
    }
}
