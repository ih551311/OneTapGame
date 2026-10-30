using UnityEngine;

namespace LastBullet.Enemy
{
    // 방어형 적의 방패 (기획서 2.5).
    // 정면(기본 ±60°)에서 날아온 탄환은 방패가 막는다. 머리에 맞아도 막힌다. 피해는 0이고,
    // 막힌 탄환은 "명중으로 처리"되어 플레이어가 바로 다시 쓸 수 있다.
    // 옆이나 뒤에서 쏘면 일반 적처럼 머리 한 방에 처치된다.
    // 방패 모양(Shield 자식)은 충돌 없이 보이기만 한다. 막는 건 각도 계산으로 정한다.
    // 붙일 곳: 방어형 적 프리팹의 맨 위 오브젝트 (EnemyHealth와 함께)
    public class EnemyShield : MonoBehaviour
    {
        [Tooltip("정면 기준 좌우 각도 (기본값). 이 안에서 온 탄환은 막힌다")]
        [SerializeField] float blockHalfAngle = 60f;

        [Tooltip("막았을 때 잠깐 하얗게 번쩍이는 방패 모양")]
        [SerializeField] Renderer plate;

        [SerializeField] float flashTime = 0.1f;

        MaterialPropertyBlock flashBlock;
        float flashEndTime;

        void Awake()
        {
            flashBlock = new MaterialPropertyBlock();
            flashBlock.SetColor("_BaseColor", Color.white);
        }

        // shooterPosition에서 쏜 탄환을 막는지
        public bool Blocks(Vector3 shooterPosition)
        {
            Vector3 toShooter = shooterPosition - transform.position;
            toShooter.y = 0f;
            Vector3 forward = transform.forward;
            forward.y = 0f;
            if (toShooter.sqrMagnitude < 0.0001f || forward.sqrMagnitude < 0.0001f)
                return true;
            return Vector3.Angle(forward, toShooter) <= blockHalfAngle;
        }

        // 막았을 때 방패가 번쩍인다
        public void OnBlocked()
        {
            if (plate == null)
                return;
            plate.SetPropertyBlock(flashBlock);
            flashEndTime = Time.time + flashTime;
        }

        void Update()
        {
            if (flashEndTime > 0f && Time.time >= flashEndTime)
            {
                plate.SetPropertyBlock(null);
                flashEndTime = 0f;
            }
        }
    }
}
