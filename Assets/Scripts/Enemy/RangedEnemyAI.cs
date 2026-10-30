using LastBullet.Player;
using UnityEngine;
using UnityEngine.AI;

namespace LastBullet.Enemy
{
    // 원거리형 적 AI (기획서 2.5): 거리를 유지하면서 돌을 던진다.
    // Idle(대기) → 플레이어 발견 → KeepDistance(거리 유지) :
    //   너무 멀면 다가오고, 너무 가까우면 물러나고, 알맞은 거리면 서서 쳐다본다.
    //   2.5초마다 Attack: 0.5초 예고(뒤로 젖히기) → 돌 던지기(12m/s) → 잠깐 쉬고 다시 KeepDistance.
    // 맞으면 Hit(잠깐 멈칫). 처치(Dead)는 EnemyHealth가 처리한다.
    // 돌은 던질 때 플레이어가 있던 곳으로 날아가서, 예고를 보고 옆으로 움직이면 피할 수 있다.
    // 붙일 곳: 원거리형 적 프리팹의 맨 위 오브젝트 (NavMeshAgent, EnemyHealth와 함께)
    [RequireComponent(typeof(NavMeshAgent), typeof(EnemyHealth))]
    public class RangedEnemyAI : MonoBehaviour
    {
        enum AIState { Idle, KeepDistance, Attack, Hit, Dead }

        [Header("감지 (기본값)")]
        [SerializeField] float detectRange = 22f;

        [Header("거리 유지 (기본값)")]
        [SerializeField] float moveSpeed = 3f;          // m/s
        [SerializeField] float keepMinDistance = 9f;    // 이보다 가까우면 물러난다
        [SerializeField] float keepMaxDistance = 14f;   // 이보다 멀면 다가온다
        [SerializeField] float retreatStep = 6f;        // 물러날 때 한 번에 정하는 거리

        [Header("공격 (기본값)")]
        [SerializeField] float attackInterval = 2.5f;   // 던지는 간격
        [SerializeField] float throwRange = 18f;        // 이 거리 안이면 던진다
        [SerializeField] float windupTime = 0.5f;       // 공격 예고 (기획서: 0.5초 전)
        [SerializeField] float leanAngle = 25f;         // 예고 동안 뒤로 젖히는 각도
        [SerializeField] float recoverTime = 0.4f;      // 던진 뒤 쉬는 시간
        [SerializeField] float projectileSpeed = 12f;   // m/s
        [SerializeField] int attackDamage = 12;         // 기획서 2.5: 원거리형 공격력 12

        [Header("피격 (기본값)")]
        [SerializeField] float hitStunTime = 0.25f;

        [Header("연결")]
        [Tooltip("던지는 돌 프리팹 (Prefabs/EnemyProjectile)")]
        [SerializeField] EnemyProjectile projectilePrefab;

        [Tooltip("돌이 나가는 자리 (가슴 앞)")]
        [SerializeField] Transform throwPoint;

        AIState state = AIState.Idle;
        NavMeshAgent agent;
        EnemyHealth health;
        Transform player;
        PlayerHealth playerHealth;
        float nextThrowTime;
        float phaseEndTime;
        bool thrown;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            health = GetComponent<EnemyHealth>();
            agent.speed = moveSpeed;
            agent.updateRotation = false; // 몸 방향은 이 스크립트가 정한다 (예고 동작 때문에)
            health.Damaged += OnDamaged;
            health.Died += _ => state = AIState.Dead;
        }

        void Start()
        {
            var playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
                playerHealth = playerObject.GetComponent<PlayerHealth>();
            }
        }

        void Update()
        {
            if (state == AIState.Dead || player == null)
                return;
            if (playerHealth != null && playerHealth.IsDead)
            {
                StopMoving(); // 플레이어가 쓰러지면 멈춘다
                return;
            }

            float distance = FlatDistance(player.position);
            switch (state)
            {
                case AIState.Idle:
                    if (distance <= detectRange)
                    {
                        state = AIState.KeepDistance;
                        nextThrowTime = Time.time + 1f; // 발견하고 1초 뒤 첫 공격
                    }
                    break;

                case AIState.KeepDistance:
                    UpdateKeepDistance(distance);
                    break;

                case AIState.Attack:
                    UpdateAttack();
                    break;

                case AIState.Hit:
                    if (Time.time >= phaseEndTime)
                        state = AIState.KeepDistance;
                    break;
            }
        }

        void UpdateKeepDistance(float distance)
        {
            FaceTowards(player.position, 0f);

            if (distance > keepMaxDistance)
            {
                agent.isStopped = false;
                agent.SetDestination(player.position);
            }
            else if (distance < keepMinDistance)
            {
                // 플레이어 반대쪽으로 물러난다. 갈 곳이 없으면(막다른 곳) 그냥 선다.
                Vector3 away = transform.position - player.position;
                away.y = 0f;
                Vector3 target = transform.position + away.normalized * retreatStep;
                if (NavMesh.SamplePosition(target, out var hit, 3f, NavMesh.AllAreas))
                {
                    agent.isStopped = false;
                    agent.SetDestination(hit.position);
                }
                else
                {
                    StopMoving();
                }
            }
            else
            {
                StopMoving();
            }

            if (Time.time >= nextThrowTime && distance <= throwRange)
                StartAttack();
        }

        void StartAttack()
        {
            state = AIState.Attack;
            thrown = false;
            phaseEndTime = Time.time + windupTime;
            StopMoving();
        }

        void UpdateAttack()
        {
            if (!thrown)
            {
                // 예고: 플레이어를 보면서 점점 뒤로 젖힌다
                float progress = 1f - (phaseEndTime - Time.time) / windupTime;
                FaceTowards(player.position, leanAngle * Mathf.Clamp01(progress));
                if (Time.time >= phaseEndTime)
                {
                    Throw();
                    thrown = true;
                    phaseEndTime = Time.time + recoverTime;
                    FaceTowards(player.position, 0f); // 던지면 자세를 바로 세운다
                }
            }
            else if (Time.time >= phaseEndTime)
            {
                nextThrowTime = Time.time + attackInterval;
                state = AIState.KeepDistance;
            }
        }

        // 던지는 순간 플레이어가 있던 곳(가슴 높이)으로 돌을 던진다
        void Throw()
        {
            if (projectilePrefab == null)
                return;
            Vector3 origin = throwPoint != null ? throwPoint.position : transform.position + Vector3.up * 1.4f;
            Vector3 target = player.position + Vector3.up * 1f;
            var projectile = Instantiate(projectilePrefab, origin, Quaternion.identity);
            projectile.Launch(target - origin, projectileSpeed, attackDamage);
        }

        void OnDamaged(EnemyHealth _)
        {
            state = AIState.Hit;
            phaseEndTime = Time.time + hitStunTime;
            StopMoving();
            if (player != null)
                FaceTowards(player.position, 0f); // 젖힌 자세를 바로 세운다
        }

        void StopMoving()
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        // 목표 쪽을 보게 하고, lean만큼 뒤로 젖힌다
        void FaceTowards(Vector3 target, float lean)
        {
            Vector3 direction = target - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
                return;
            transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(-lean, 0f, 0f);
        }

        float FlatDistance(Vector3 target)
        {
            Vector3 offset = target - transform.position;
            offset.y = 0f;
            return offset.magnitude;
        }
    }
}
