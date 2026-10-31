using System;
using LastBullet.Player;
using UnityEngine;

namespace LastBullet.Enemy
{
    // 원거리형 적이 던지는 돌 (기획서 2.5). 쏜 방향으로 일정한 속도로 날아간다.
    // 플레이어에 닿으면 피해를 주고 사라진다 (회피 무적 중이면 피해 없이 사라진다).
    // 나무·바위·땅 같은 것에 닿으면 그냥 사라진다. 다른 적(Enemy 태그)은 그냥 통과한다.
    // 충돌 컴포넌트 없이 매 프레임 앞쪽을 SphereCast로 확인한다 (빠른 투사체가 얇은 것을 뚫고 지나가지 않게).
    // 붙일 곳: 투사체 프리팹의 맨 위 오브젝트 (모양은 자식 Model)
    public class EnemyProjectile : MonoBehaviour
    {
        [Tooltip("돌의 굵기 반지름 (m). 맞는 판정 크기")]
        [SerializeField] float radius = 0.25f;

        [Tooltip("이 시간(초) 안에 아무것에도 안 닿으면 사라진다")]
        [SerializeField] float lifetime = 5f;

        Vector3 direction;
        float speed;
        int damage;
        float endTime;

        // 던질 때 부른다
        public void Launch(Vector3 flightDirection, float flightSpeed, int hitDamage)
        {
            direction = flightDirection.normalized;
            speed = flightSpeed;
            damage = hitDamage;
            endTime = Time.time + lifetime;
            transform.rotation = Quaternion.LookRotation(direction);
        }

        void Update()
        {
            if (Time.time >= endTime)
            {
                Destroy(gameObject);
                return;
            }

            float step = speed * Time.deltaTime;
            var hits = Physics.SphereCastAll(transform.position, radius, direction, step, ~0, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                if (hit.collider.CompareTag("Enemy"))
                    continue; // 다른 적은 통과

                var player = hit.collider.GetComponentInParent<PlayerHealth>();
                if (player != null)
                    player.TakeDamage(damage);
                Destroy(gameObject);
                return;
            }
            transform.position += direction * step;
        }
    }
}
