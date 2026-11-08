using LastBullet.Combat;
using UnityEngine;

namespace LastBullet.Player
{
    // 3인칭에서 캐릭터가 총을 두 손으로 가슴 높이에 겨누고 있게 한다 (2026-11-04 사용자 요청: 들고 있는 모습이 어색하다).
    // 캐릭터 동작(대기·걷기·달리기)은 "큰 총을 가슴 앞에 안은" 자세라서, 새 소총(AK47)을 손에 그대로 붙이면 허리 높이에서 한 손으로 드는 모습이 된다.
    // 그래서 동작이 끝난 뒤(LateUpdate)에:
    //  1) 총을 몸 기준으로 가슴 앞 오른쪽에 놓는다 (총구는 몸이 보는 앞쪽, 재장전 때만 위로 들림)
    //  2) 오른팔은 총의 손잡이를, 왼팔은 총의 앞쪽 받침(총열 덮개)을 향해 두 팔 IK(어깨→팔꿈치→손)로 뻗는다. 손 모양(주먹)은 동작 그대로.
    // 총 모델이 달린 ThirdPersonGun 오브젝트의 앞(+Z) = 총구, 위(+Y) = 총 위쪽이다.
    // 이 스크립트는 총의 위치를 몸에서 정하므로, 총이 달린 부모(오른손 뼈)가 움직여도 매 프레임 총 자리를 다시 정한다.
    // 붙일 곳: R.Hand 아래의 ThirdPersonGun (메뉴 LastBullet > Phase 61 AK47로 바꾸기)
    public class ThirdPersonGunPose : MonoBehaviour
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

        [Header("총 자리 (가슴 뼈 기준, 몸이 보는 방향 기준 m)")]
        [Tooltip("손잡이를 쥐는 자리: x 오른쪽, y 위, z 앞")]
        [SerializeField] Vector3 gripFromChest = new Vector3(0.17f, -0.02f, 0.30f);

        [Tooltip("총 모델 안에서 손으로 쥘 점 (모델 크기 1 기준, 총구가 +Z)")]
        [SerializeField] Vector3 gripInGun = new Vector3(0f, -0.04f, -0.157f);

        [Tooltip("왼손이 받치는 자리 (총 모델 안, 모델 크기 1 기준). 앞쪽 총열 덮개 아래")]
        [SerializeField] Vector3 leftGripInGun = new Vector3(0f, -0.045f, 0.12f);

        [Tooltip("총구를 위아래로 돌리는 각도 (도, +는 위). 평소 겨눈 자세")]
        [SerializeField] float restPitch = 2f;

        [Header("손 모양")]
        [Tooltip("손 뼈 기준 주먹 가운데 (손가락이 말려 있는 곳)")]
        [SerializeField] Vector3 fistInHand = new Vector3(-0.02f, -0.055f, 0.10f);

        [Tooltip("왼손의 방향 (총 기준). 손바닥이 위로 총열 덮개를 받치게")]
        [SerializeField] Vector3 leftHandEuler = new Vector3(0f, 0f, -90f);

        [Tooltip("왼손이 총에서 얼마나 떨어진 자리에 오는지 (손 뼈 기준 m)")]
        [SerializeField] Vector3 leftFistInHand = new Vector3(0.02f, -0.055f, 0.10f);

        [Header("재장전")]
        [Tooltip("재장전 모션을 만들 총 (재장전 중에 총구가 위로 들렸다가 내려온다)")]
        [SerializeField] AutoGun gun;

        [Tooltip("재장전 때 총구가 위로 드는 각도 (도)")]
        [SerializeField] float reloadTilt = 12f;

        [Tooltip("탄창 자리 (총 모델 안 좌표, 왼손이 탄창을 잡으러 가는 곳)")]
        [SerializeField] Vector3 magazineInGun = new Vector3(0f, -0.08f, 0.0f);

        [Tooltip("장전 손잡이 자리 (총 모델 안 좌표)")]
        [SerializeField] Vector3 chargeInGun = new Vector3(0.03f, 0.05f, -0.02f);

        static readonly Quaternion RightHandInGun = Quaternion.LookRotation(Vector3.forward, Vector3.left); // 손의 앞(+Z) = 총구, 손의 -X = 총 위

        void LateUpdate()
        {
            if (body == null || chest == null || rightUpper == null || rightHand == null)
                return;

            Vector3 forward = Vector3.ProjectOnPlane(body.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f)
                forward = body.forward;
            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward);

            Quaternion rotation = Quaternion.LookRotation(forward, Vector3.up);
            rotation = Quaternion.AngleAxis(-restPitch, rotation * Vector3.right) * rotation;

            // 총 자리 (손잡이가 놓이는 월드 위치)
            Vector3 gripWorld = chest.position + right * gripFromChest.x + Vector3.up * gripFromChest.y + forward * gripFromChest.z;

            // 재장전: 손잡이를 중심으로 총구가 위로 들렸다가 내려온다
            float reload = gun != null && gun.IsReloading ? Mathf.Sin(gun.ReloadProgress * Mathf.PI) : 0f;
            if (reload > 0f)
                rotation = Quaternion.AngleAxis(-reloadTilt * reload, rotation * Vector3.right) * rotation;

            float scale = transform.lossyScale.x;
            Vector3 gunPosition = gripWorld - rotation * (gripInGun * scale);

            // 오른손: 손잡이를 쥔다 (손 뼈 = 쥘 점 - 손 방향 × 주먹 위치)
            Quaternion rightRotation = rotation * Quaternion.Inverse(RightHandInGun);
            Vector3 rightTarget = gripWorld - rightRotation * fistInHand;
            Vector3 rightPole = right * 0.6f - Vector3.up * 0.8f - forward * 0.2f;
            SolveTwoBone(rightUpper, rightMid, rightHand, rightTarget, rightPole);
            rightHand.rotation = rightRotation;

            // 왼손: 총 앞쪽 받침을 받친다
            if (leftUpper != null && leftMid != null && leftHand != null)
            {
                float reloadProgress = gun != null && gun.IsReloading ? gun.ReloadProgress : 0f;
                Vector3 leftInGun = RifleReloadTimeline.LeftHand(reloadProgress, leftGripInGun, magazineInGun, chargeInGun);
                Vector3 leftGripWorld = gunPosition + rotation * (leftInGun * scale);
                Quaternion leftRotation = rotation * Quaternion.Euler(leftHandEuler);
                Vector3 leftTarget = leftGripWorld - leftRotation * leftFistInHand;
                Vector3 leftPole = -right * 0.6f - Vector3.up * 0.8f - forward * 0.2f;
                SolveTwoBone(leftUpper, leftMid, leftHand, leftTarget, leftPole);
                leftHand.rotation = leftRotation;
            }

            // 총은 오른손 뼈의 자식이라 손이 움직이면 같이 움직인다. 마지막에 총 자리를 다시 정한다.
            transform.SetPositionAndRotation(gunPosition, rotation);
        }

        // 두 뼈 IK: 어깨(upper)에서 팔꿈치(mid)를 거쳐 손(end)이 목표에 닿게 한다. 팔꿈치는 poleDirection 쪽으로 굽는다.
        internal static void SolveTwoBone(Transform upper, Transform mid, Transform end, Vector3 target, Vector3 poleDirection)
        {
            float a = Vector3.Distance(upper.position, mid.position);
            float b = Vector3.Distance(mid.position, end.position);
            Vector3 toTarget = target - upper.position;
            float distance = Mathf.Clamp(toTarget.magnitude, Mathf.Abs(a - b) + 0.001f, a + b - 0.001f);
            if (toTarget.sqrMagnitude < 0.000001f)
                return;
            Vector3 direction = toTarget.normalized;

            // 팔꿈치 위치: 코사인 법칙
            float along = (distance * distance + a * a - b * b) / (2f * distance);
            float height = Mathf.Sqrt(Mathf.Max(0f, a * a - along * along));
            Vector3 perpendicular = Vector3.ProjectOnPlane(poleDirection, direction);
            if (perpendicular.sqrMagnitude < 0.0001f)
                perpendicular = Vector3.ProjectOnPlane(Vector3.down, direction);
            perpendicular.Normalize();
            Vector3 desiredMid = upper.position + direction * along + perpendicular * height;

            upper.rotation = Quaternion.FromToRotation(mid.position - upper.position, desiredMid - upper.position) * upper.rotation;
            Vector3 reachable = upper.position + direction * distance;
            mid.rotation = Quaternion.FromToRotation(end.position - mid.position, reachable - mid.position) * mid.rotation;
        }
    }
}
