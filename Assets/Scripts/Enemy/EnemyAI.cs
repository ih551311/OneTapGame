using LastBullet.Player;
using UnityEngine;
using UnityEngine.AI;

namespace LastBullet.Enemy
{
    // 돌진형 적 AI (기획서 2.5).
    // Idle(대기) → 플레이어 발견 → Chase(추적, 4m/s) → 가까워지면 Attack:
    //   0.5초 예고(뒤로 젖히기) → 돌진(9m/s) → 잠깐 숨 고르기 → 다시 추적.
    // 맞으면 Hit(잠깐 멈칫). 처치(Dead)는 EnemyHealth가 처리한다.
    // 돌진 중 플레이어에 닿으면 돌진 한 번에 한 번만 피해를 준다.
    // 붙일 곳: 적 프리팹의 맨 위 오브젝트 (NavMeshAgent, EnemyHealth와 함께)
    [RequireComponent(typeof(NavMeshAgent), typeof(EnemyHealth))]
    public class EnemyAI : MonoBehaviour
    {
        public enum AIState { Idle, Chase, Attack, Hit, Dead }
        enum AttackPhase { Windup, Dash, Recover }

        [Header("감지 (기본값)")]
        [SerializeField] float detectRange = 15f;      // 이 거리 안에 플레이어가 오면 쫓아온다

        [Header("추적 (기본값)")]
        [SerializeField] float chaseSpeed = 4f;        // m/s

        [Header("돌진 공격 (기본값)")]
        [SerializeField] float attackRange = 4f;       // 이 거리에서 돌진을 시작한다
        [SerializeField] float windupTime = 0.5f;      // 공격 예고 (기획서: 0.5초 전)
        [SerializeField] float leanAngle = 20f;        // 예고 동안 뒤로 젖히는 각도
        [SerializeField] float dashSpeed = 9f;         // m/s
        [SerializeField] float dashTime = 0.5f;        // 돌진 시간 (0.5초 × 9m/s = 4.5m)
        [SerializeField] float recoverTime = 0.8f;     // 돌진 뒤 숨 고르는 시간 (플레이어가 반격할 틈)
        [SerializeField] int attackDamage = 15;        // 기획서 2.5: 돌진형 공격력 15
        [SerializeField] float contactRange = 1.3f;    // 돌진 중 이 거리 안이면 맞은 것으로 본다

        [Header("피격 (기본값)")]
        [SerializeField] float hitStunTime = 0.25f;    // 맞으면 멈칫하는 시간

        public AIState State { get; private set; } = AIState.Idle;

        NavMeshAgent agent;
        EnemyHealth health;
        Transform player;
        PlayerHealth playerHealth;
        bool dashHit; // 이번 돌진에서 이미 피해를 줬는지
        AttackPhase attackPhase;
        float phaseEndTime;
        Vector3 dashDirection;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            health = GetComponent<EnemyHealth>();
            agent.speed = chaseSpeed;
            agent.updateRotation = false; // 몸 방향은 이 스크립트가 정한다 (예고 동작 때문에)
            health.Damaged += OnDamaged;
            health.Died += _ => State = AIState.Dead;
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
            if (State == AIState.Dead || player == null)
                return;
            if (playerHealth != null && playerHealth.IsDead)
            {
                StopMoving(); // 플레이어가 쓰러지면 멈춘다
                return;
            }

            float distance = FlatDistance(player.position);
            switch (State)
            {
                case AIState.Idle:
                    if (distance <= detectRange)
                        State = AIState.Chase;
                    break;

                case AIState.Chase:
                    agent.isStopped = false;
                    agent.SetDestination(player.position);
                    FaceTowards(player.position, 0f);
                    if (distance <= attackRange)
                        StartAttack();
                    break;

                case AIState.Attack:
                    UpdateAttack();
                    break;

                case AIState.Hit:
                    if (Time.time >= phaseEndTime)
                        State = AIState.Chase;
                    break;
            }
        }

        void StartAttack()
        {
            State = AIState.Attack;
            attackPhase = AttackPhase.Windup;
            phaseEndTime = Time.time + windupTime;
            StopMoving();
        }

        void UpdateAttack()
        {
            switch (attackPhase)
            {
                case AttackPhase.Windup:
                    // 예고: 플레이어를 보면서 점점 뒤로 젖힌다
                    float progress = 1f - (phaseEndTime - Time.time) / windupTime;
                    FaceTowards(player.position, leanAngle * Mathf.Clamp01(progress));
                    if (Time.time >= phaseEndTime)
                    {
                        // 돌진 방향은 이 순간 정해진다. 그래서 예고를 보고 옆으로 피하면 빗나간다.
                        dashDirection = player.position - transform.position;
                        dashDirection.y = 0f;
                        dashDirection.Normalize();
                        FaceTowards(transform.position + dashDirection, 0f);
                        attackPhase = AttackPhase.Dash;
                        phaseEndTime = Time.time + dashTime;
                        dashHit = false;
                    }
                    break;

                case AttackPhase.Dash:
                    agent.Move(dashDirection * dashSpeed * Time.deltaTime); // NavMesh 밖으로는 나가지 않는다
                    if (!dashHit && playerHealth != null && FlatDistance(player.position) <= contactRange)
                    {
                        dashHit = true; // 회피 무적으로 피했어도 이번 돌진은 끝
                        playerHealth.TakeDamage(attackDamage);
                    }
                    if (Time.time >= phaseEndTime)
                    {
                        attackPhase = AttackPhase.Recover;
                        phaseEndTime = Time.time + recoverTime;
                    }
                    break;

                case AttackPhase.Recover:
                    if (Time.time >= phaseEndTime)
                        State = AIState.Chase;
                    break;
            }
        }

        void OnDamaged(EnemyHealth _)
        {
            State = AIState.Hit;
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
