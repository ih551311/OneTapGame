using UnityEngine;

namespace LastBullet.Player
{
    // 3인칭에서 캐릭터가 든 총을 앞으로 겨누게 한다 (2026-10-31 사용자 요청: 총을 든 모습이 어색하다).
    // 캐릭터 동작(대기·걷기·달리기)은 "큰 총을 가슴 앞에 안은" 자세라서, 총을 손에 그대로 붙이면 총구가 몸 왼쪽 아래를 향해 몸에 가려진다.
    // 그래서 총을 주먹 가운데에 쥐게 두고(손잡이가 주먹을 뚫고 지나는 축 = 손의 X축은 그대로), 그 축을 중심으로만 돌려서
    // 총구가 캐릭터의 앞을 향하게 한다 (손목을 돌리는 것과 같다). 동작이 끝난 뒤(LateUpdate)에 맞춘다.
    // 겉모습(pistol1 모델)은 이 오브젝트 아래 메시다. 총을 바꿀 때는 gripInMesh(모델 안에서 손으로 쥘 점)를 새 모델에 맞춘다.
    // 붙일 곳: R.Hand 아래의 ThirdPersonPistol (메뉴 LastBullet > Phase 50 3인칭 총을 pistol1로 바꾸기)
    public class ThirdPersonGunPose : MonoBehaviour
    {
        [SerializeField] Transform hand;
        [SerializeField] Transform body;

        [Tooltip("손 뼈 기준 주먹 가운데 (손가락이 말려 있는 곳)")]
        [SerializeField] Vector3 fistInHand = new Vector3(-0.02f, -0.055f, 0.10f);

        [Tooltip("총 모델 안에서 손으로 쥘 점 (손잡이 가운데, 모델 크기 1 기준)")]
        [SerializeField] Vector3 gripInMesh = new Vector3(0f, -0.02f, -0.45f);

        [Tooltip("겨누는 정도 (0 = 동작 그대로, 1 = 완전히 앞)")]
        [Range(0f, 1f)]
        [SerializeField] float aimWeight = 1f;

        void LateUpdate()
        {
            if (hand == null || body == null)
                return;

            // 손잡이가 지나는 축 = 손의 X축, 총의 위쪽 = 손의 -X(엄지 쪽)
            Vector3 up = hand.TransformDirection(Vector3.left);
            Vector3 handForward = hand.forward;
            Vector3 aim = Vector3.ProjectOnPlane(body.forward, up);
            Vector3 forward = aim.sqrMagnitude > 0.0001f
                ? Vector3.Slerp(handForward, aim.normalized, aimWeight)
                : handForward;
            forward = Vector3.ProjectOnPlane(forward, up);
            if (forward.sqrMagnitude < 0.0001f)
                forward = handForward;

            transform.rotation = Quaternion.LookRotation(forward.normalized, up);
            // 쥘 점이 주먹 가운데에 오도록 위치를 맞춘다
            transform.position = hand.TransformPoint(fistInHand) - transform.rotation * (gripInMesh * transform.lossyScale.x);
        }
    }
}
