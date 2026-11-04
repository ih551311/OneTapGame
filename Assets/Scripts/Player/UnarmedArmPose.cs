using System.Collections.Generic;
using UnityEngine;

namespace LastBullet.Player
{
    // 총 없이 걷는 팔 (2026-11-04 사용자 요청: 미술관에서는 총을 안 든 모습으로).
    //  이 캐릭터의 동작 파일(대기·걷기·달리기)은 전부 총을 든 자세(주먹 쥔 손)라서, 동작이 끝난 뒤 매 프레임 양팔과 손을 덮어쓴다.
    //  - 팔: 어깨에서 몸 옆으로 늘어뜨리고, 걷거나 달리면 앞뒤로 흔든다 (달리면 팔꿈치가 굽고 더 크게 흔든다).
    //  - 손: 모델이 처음 만들어진 자세(손가락이 펴진 상태)로 되돌려 주먹을 푼다.
    //  - 가슴·다리 같은 몸 동작은 원래 동작 그대로다.
    // 붙일 곳: Player (메뉴 LastBullet > Phase 66 미술관 맨손 걷기)
    public class UnarmedArmPose : MonoBehaviour
    {
        [Header("뼈 (캐릭터 모델)")]
        [SerializeField] Transform body;
        [SerializeField] Transform rightUpper;
        [SerializeField] Transform rightMid;
        [SerializeField] Transform rightHand;
        [SerializeField] Transform leftUpper;
        [SerializeField] Transform leftMid;
        [SerializeField] Transform leftHand;
        [SerializeField] CharacterController controller;

        [Header("흔들기 (기본값)")]
        [Tooltip("걸을 때 팔이 앞뒤로 흔들리는 폭 (m)")]
        [SerializeField] float walkSwing = 0.16f;

        [Tooltip("달릴 때 팔이 앞뒤로 흔들리는 폭 (m)")]
        [SerializeField] float runSwing = 0.28f;

        [Tooltip("걷는 거리에 따른 흔들리는 빠르기 (클수록 빠르게 흔든다)")]
        [SerializeField] float stepRate = 1.3f;

        [Tooltip("팔이 몸 옆에서 바깥으로 벌어진 거리 (m)")]
        [SerializeField] float sideOffset = 0.07f;

        class Arm
        {
            public Transform upper, mid, hand;
            public Quaternion upperRel;     // 몸 기준 처음 방향
            public Vector3 dirRel;          // 몸 기준 어깨→팔꿈치 방향
            public Quaternion midLocal;
            public float a, b;
            public readonly List<Transform> handBones = new List<Transform>();
            public readonly List<Quaternion> handLocal = new List<Quaternion>();
        }

        Arm right, left;
        float phase;
        float smoothSpeed;

        void Awake()
        {
            right = Capture(rightUpper, rightMid, rightHand);
            left = Capture(leftUpper, leftMid, leftHand);
        }

        // 애니메이터가 움직이기 전, 모델이 만들어진 자세를 기억해 둔다
        Arm Capture(Transform upper, Transform mid, Transform hand)
        {
            if (body == null || upper == null || mid == null || hand == null)
                return null;
            var arm = new Arm { upper = upper, mid = mid, hand = hand };
            Quaternion inverseBody = Quaternion.Inverse(body.rotation);
            arm.upperRel = inverseBody * upper.rotation;
            arm.dirRel = inverseBody * (mid.position - upper.position);
            arm.midLocal = mid.localRotation;
            arm.a = Vector3.Distance(upper.position, mid.position);
            arm.b = Vector3.Distance(mid.position, hand.position);
            foreach (var t in hand.GetComponentsInChildren<Transform>(true))
            {
                arm.handBones.Add(t);
                arm.handLocal.Add(t.localRotation);
            }
            return arm;
        }

        void LateUpdate()
        {
            if (right == null || left == null)
                return;

            float speed = 0f;
            if (controller != null)
            {
                var velocity = controller.velocity;
                velocity.y = 0f;
                speed = velocity.magnitude;
            }
            smoothSpeed = Mathf.Lerp(smoothSpeed, speed, 1f - Mathf.Exp(-12f * Time.deltaTime));
            phase += smoothSpeed * stepRate * Time.deltaTime;
            float walk = Mathf.Clamp01(smoothSpeed / 5f);
            float run = Mathf.Clamp01((smoothSpeed - 5f) / 3f);
            float swing = Mathf.Lerp(walkSwing, runSwing, run) * walk;

            Vector3 forward = Vector3.ProjectOnPlane(body.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f)
                forward = body.forward;
            forward.Normalize();
            Vector3 sideRight = Vector3.Cross(Vector3.up, forward);

            Pose(right, 1f, Mathf.Sin(phase), swing, run, forward, sideRight);
            Pose(left, -1f, Mathf.Sin(phase + Mathf.PI), swing, run, forward, sideRight);
        }

        void Pose(Arm arm, float side, float wave, float swing, float run, Vector3 forward, Vector3 sideRight)
        {
            float length = arm.a + arm.b;
            Vector3 shoulder = arm.upper.position;
            // 손이 닿을 자리: 몸 옆으로 늘어뜨리고 앞뒤로 흔든다. 앞으로 갈 때는 손이 조금 올라가고, 달리면 팔꿈치가 굽어 손이 올라온다.
            Vector3 target = shoulder + Vector3.down * (length * 0.96f) + sideRight * (side * sideOffset) + forward * (wave * swing);
            target += Vector3.up * (Mathf.Max(0f, wave) * swing * 0.5f + run * 0.12f);
            Vector3 pole = -forward * 0.5f + sideRight * (side * 0.6f);

            Quaternion bodyRotation = body.rotation;
            // 처음 자세로 되돌리고 시작한다 (애니메이터의 총 든 자세를 지운다)
            arm.upper.rotation = bodyRotation * arm.upperRel;
            arm.mid.localRotation = arm.midLocal;
            for (int i = 0; i < arm.handBones.Count; i++)
                arm.handBones[i].localRotation = arm.handLocal[i];

            float distance = Mathf.Clamp(Vector3.Distance(target, shoulder), Mathf.Abs(arm.a - arm.b) + 0.001f, length - 0.001f);
            Vector3 direction = (target - shoulder).normalized;
            float along = (distance * distance + arm.a * arm.a - arm.b * arm.b) / (2f * distance);
            float height = Mathf.Sqrt(Mathf.Max(0f, arm.a * arm.a - along * along));
            Vector3 perpendicular = Vector3.ProjectOnPlane(pole, direction);
            if (perpendicular.sqrMagnitude < 0.0001f)
                perpendicular = Vector3.ProjectOnPlane(Vector3.down, direction);
            perpendicular.Normalize();
            Vector3 desiredMid = shoulder + direction * along + perpendicular * height;

            arm.upper.rotation = Quaternion.FromToRotation(bodyRotation * arm.dirRel, desiredMid - shoulder) * arm.upper.rotation;
            Vector3 reachable = shoulder + direction * distance;
            arm.mid.rotation = Quaternion.FromToRotation(arm.hand.position - arm.mid.position, reachable - arm.mid.position) * arm.mid.rotation;
        }
    }
}
