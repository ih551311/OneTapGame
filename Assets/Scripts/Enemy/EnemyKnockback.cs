using UnityEngine;
using UnityEngine.AI;

namespace LastBullet.Enemy
{
    // 폭발에 날아가는 적 (2026-11-11 사용자 요청: 게임 주스). 바주카 폭발이 터진 곳에서 멀어지는 쪽으로 밀려나며 살짝 뜬다.
    //  - 살아 있는 적: 이 컴포넌트가 잠깐(0.55초) NavMeshAgent를 직접 밀고(agent.Move, 갈 수 있는 길 밖으로는 안 나간다), 몸을 포물선으로 띄운다(agent.baseOffset).
    //    날아가는 동안 AI가 걷지 못하게 거의 멈춘다 (ApplySlow). 보스는 거의 안 밀린다.
    //  - 이미 처치된 적: EnemyDeathEffect가 쓰러지는 동안 같이 날아가게 SetLaunch를 부른다 (더 멀리, 더 많이 뒤집힌다).
    // 붙일 곳: 코드가 적에게 붙인다 (BazookaWeapon.Explode → EnemyKnockback.Apply)
    public class EnemyKnockback : MonoBehaviour
    {
        public const float Duration = 0.55f;
        const float Decay = 3f; // 처음엔 빠르게 밀리고 점점 느려진다

        NavMeshAgent agent;
        Vector3 displacement;
        float hop, baseOffset0, start, lastFraction, currentHop;

        // direction = 밀리는 방향(수평), distance = 밀리는 거리 (m), hop = 뜨는 높이 (m)
        public static void Apply(EnemyHealth enemy, Vector3 direction, float distance, float hop)
        {
            if (enemy == null)
                return;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
                direction = Random.insideUnitSphere;
            direction.y = 0f;
            direction.Normalize();

            if (enemy.MaxHealth > 500f)
            {
                distance *= 0.12f; // 보스는 거의 안 밀린다
                hop *= 0.1f;
            }
            distance = ClampByWalls(enemy.transform, direction, distance);

            if (enemy.IsDead)
            {
                var death = enemy.GetComponent<EnemyDeathEffect>();
                if (death != null)
                    death.SetLaunch(direction * distance, hop);
                return;
            }

            var agent = enemy.GetComponent<NavMeshAgent>();
            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
                return;
            var knockback = enemy.GetComponent<EnemyKnockback>();
            if (knockback == null)
                knockback = enemy.gameObject.AddComponent<EnemyKnockback>();
            knockback.Begin(agent, direction * distance, hop);

            // 날아가는 동안 걸어서 맞서지 못하게
            var melee = enemy.GetComponent<EnemyAI>();
            if (melee != null)
                melee.ApplySlow(0.05f, Duration);
            var ranged = enemy.GetComponent<RangedEnemyAI>();
            if (ranged != null)
                ranged.ApplySlow(0.05f, Duration);
        }

        // 가슴 높이에서 밀리는 방향으로 벽이 있으면 그 앞까지만 간다
        static float ClampByWalls(Transform body, Vector3 direction, float distance)
        {
            if (distance <= 0.01f)
                return 0f;
            var hits = Physics.RaycastAll(body.position + Vector3.up * 1f, direction, distance + 0.5f, ~0, QueryTriggerInteraction.Ignore);
            float nearest = float.MaxValue;
            foreach (var hit in hits)
            {
                if (hit.collider.transform.IsChildOf(body))
                    continue;
                if (hit.collider.CompareTag("Player") || hit.collider.CompareTag("Enemy"))
                    continue;
                nearest = Mathf.Min(nearest, hit.distance);
            }
            return nearest == float.MaxValue ? distance : Mathf.Clamp(nearest - 0.5f, 0f, distance);
        }

        void Begin(NavMeshAgent targetAgent, Vector3 totalDisplacement, float height)
        {
            if (agent != null)
                agent.baseOffset = baseOffset0; // 날아가는 중에 또 맞으면 처음부터 다시 (뜬 높이는 원래대로)
            agent = targetAgent;
            displacement = totalDisplacement;
            hop = height;
            baseOffset0 = agent.baseOffset;
            start = Time.time;
            lastFraction = 0f;
            enabled = true;
        }

        void Update()
        {
            if (agent == null || !agent.enabled)
            {
                enabled = false;
                return;
            }
            float t = Time.time - start;
            float k = Mathf.Clamp01(t / Duration);
            float fraction = (1f - Mathf.Exp(-Decay * t)) / (1f - Mathf.Exp(-Decay * Duration));
            fraction = Mathf.Clamp01(fraction);
            if (agent.isOnNavMesh)
                agent.Move(displacement * (fraction - lastFraction));
            lastFraction = fraction;
            currentHop = hop * Mathf.Sin(Mathf.PI * k);
            agent.baseOffset = baseOffset0 + currentHop;
            if (k >= 1f)
            {
                agent.baseOffset = baseOffset0;
                currentHop = 0f;
                Destroy(this);
            }
        }

        void OnDisable()
        {
            // 날아가는 도중 처치되어 꺼지면 뜬 높이를 원래대로 (쓰러지는 연출이 이어받는다)
            if (agent != null)
            {
                if (!agent.enabled && currentHop > 0f)
                    transform.position -= Vector3.up * currentHop;
                agent.baseOffset = baseOffset0;
            }
            currentHop = 0f;
        }
    }
}
