using System;
using System.Collections;
using LastBullet.Combat;
using LastBullet.Effects;
using LastBullet.Enemy;
using LastBullet.Inventory;
using LastBullet.Stage;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace LastBullet.Player
{
    // E키 스킬: 물감 폭탄 (2026-11-08 사용자 결정).
    //  - 조준점이 가리키는 곳으로 물감 덩어리를 포물선으로 던져 터뜨린다 (최대 45m).
    //  - 터진 자리에 반지름 5m 물감 구역이 6초 동안 남는다 (PaintPuddle): 안에 있는 적은 속도가 45%로 느려지고 0.5초마다 4 피해를 입는다. 터지는 순간 근처 적에게 12 피해.
    //  - 색은 그 맵의 물감 3색을 돌아가며 쓴다 (숲 빨강·주황·노랑 …). 터지는 모습은 적 처치 때의 물감 방울(PaintBurst)을 쓴다.
    //  - 쿨다운 15초. 메뉴·팝업·일시정지·사망 중에는 못 쓴다 (마우스가 풀려 있으면 안 됨). 미술관에서는 붙이지 않는다.
    //  - 쿨다운 표시는 SkillCooldownUI가 이 컴포넌트를 읽어 보여 준다.
    // 붙일 곳: 전투 맵의 Player (메뉴 LastBullet > Phase 92 물감 폭탄 스킬 만들기)
    public class PaintBombSkill : MonoBehaviour
    {
        [Header("스킬 값 (기본값)")]
        [Tooltip("쿨다운 (초)")]
        [SerializeField] float cooldown = 15f;
        [Tooltip("던질 수 있는 최대 거리 (m)")]
        [SerializeField] float maxRange = 45f;
        [Tooltip("물감 구역 반지름 (m)")]
        [SerializeField] float radius = 5f;
        [Tooltip("물감 구역이 남는 시간 (초)")]
        [SerializeField] float duration = 6f;
        [Tooltip("구역 안 적에게 피해를 주는 간격 (초)")]
        [SerializeField] float tickInterval = 0.5f;
        [Tooltip("한 번에 주는 피해")]
        [SerializeField] float tickDamage = 4f;
        [Tooltip("구역 안 적의 속도 배율 (0.45 = 45%)")]
        [Range(0.1f, 1f)]
        [SerializeField] float slowFactor = 0.45f;
        [Tooltip("터지는 순간 근처(2.5m) 적에게 주는 피해")]
        [SerializeField] float impactDamage = 12f;

        [Header("연결")]
        [Tooltip("물감 구역 바닥 그림 머티리얼 9개 (빨강 주황 노랑 초록 파랑 보라 핑크 하양 검정 순서, Assets/Materials/FloorPaint_*.mat)")]
        [SerializeField] Material[] puddleMaterials;
        [Tooltip("날아가는 물감 덩어리 머티리얼 (Assets/Materials/PaintBomb.mat)")]
        [SerializeField] Material bombMaterial;
        [Tooltip("터질 때 튀는 물감 방울 (Prefabs/PaintBurst)")]
        [SerializeField] PaintBurstEffect burstPrefab;
        [SerializeField] AudioClip throwSound;
        [SerializeField] AudioClip splatSound;

        public float Cooldown => cooldown;
        public float Remaining => Mathf.Max(0f, readyTime - Time.time);
        public bool Ready => Remaining <= 0f;
        public event Action Used;

        PlayerHealth health;
        float readyTime;
        int colorIndex;

        void Awake()
        {
            health = GetComponent<PlayerHealth>();
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.eKey.wasPressedThisFrame)
                return;
            if (!Ready || Cursor.lockState != CursorLockMode.Locked || Time.timeScale <= 0f)
                return;
            if (health != null && health.IsDead)
                return;
            Throw();
        }

        void Throw()
        {
            var cam = Camera.main;
            if (cam == null)
                return;
            readyTime = Time.time + cooldown;
            Used?.Invoke();

            // 던질 곳: 조준점 레이가 처음 닿는 곳 (플레이어 자신은 건너뜀). 아무것도 없으면 35m 앞 땅.
            var ray = new Ray(cam.transform.position, cam.transform.forward);
            Vector3 target;
            var hits = Physics.RaycastAll(ray, maxRange, ~0, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            bool found = false;
            target = ray.origin + ray.direction * Mathf.Min(35f, maxRange);
            foreach (var hit in hits)
            {
                if (hit.collider.transform.IsChildOf(transform))
                    continue;
                target = hit.point;
                found = true;
                break;
            }
            // 땅 위로 맞춘다 (벽·적 몸에 맞았어도 바닥 구역이 되게)
            Vector3 groundNormal = Vector3.up;
            if (Physics.Raycast(target + Vector3.up * (found ? 0.6f : 30f), Vector3.down, out var ground, found ? 8f : 80f, ~0, QueryTriggerInteraction.Ignore))
            {
                target = ground.point;
                groundNormal = ground.normal.y > 0.5f ? ground.normal : Vector3.up;
            }

            // 이번에 쓸 물감 색 (맵의 색을 돌아가며)
            var colors = StageColors.For(SceneManager.GetActiveScene().name);
            PaintColor color = colors != null && colors.Count > 0 ? colors[colorIndex % colors.Count] : (PaintColor)(colorIndex % PaintColors.Count);
            colorIndex++;

            StartCoroutine(Fly(cam.transform, target, groundNormal, color));
        }

        IEnumerator Fly(Transform cam, Vector3 target, Vector3 normal, PaintColor color)
        {
            Color paint = PaintColors.Colors[(int)color];
            Vector3 start = cam.position + cam.forward * 0.7f + cam.right * 0.25f - cam.up * 0.2f;
            Vector3 gravity = new Vector3(0f, -24f, 0f);
            float distance = Vector3.Distance(start, target);
            float time = Mathf.Clamp(distance / 26f, 0.4f, 1.1f);
            Vector3 velocity = (target - start - 0.5f * gravity * time * time) / time;

            // 날아가는 물감 덩어리 (작은 공 + 그 색)
            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "PaintBomb";
            Destroy(ball.GetComponent<Collider>());
            ball.transform.localScale = Vector3.one * 0.38f;
            var renderer = ball.GetComponent<MeshRenderer>();
            if (bombMaterial != null)
                renderer.sharedMaterial = bombMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", paint);
            renderer.SetPropertyBlock(block);
            if (throwSound != null)
                AudioSource.PlayClipAtPoint(throwSound, start, 0.7f);

            for (float t = 0f; t < time; t += Time.deltaTime)
            {
                ball.transform.position = start + velocity * t + 0.5f * gravity * t * t;
                ball.transform.localScale = Vector3.one * 0.38f * (1f + 0.15f * Mathf.Sin(t * 30f)); // 출렁이는 물감 덩어리
                yield return null;
            }
            Destroy(ball);
            Burst(target, normal, color, paint);
        }

        void Burst(Vector3 position, Vector3 normal, PaintColor color, Color paint)
        {
            if (burstPrefab != null)
                PaintBurstEffect.Spawn(burstPrefab, position + Vector3.up * 0.4f, position, paint);
            if (splatSound != null)
                AudioSource.PlayClipAtPoint(splatSound, position, 0.9f);

            var material = puddleMaterials != null && (int)color < puddleMaterials.Length ? puddleMaterials[(int)color] : null;
            if (material != null)
                PaintPuddle.Spawn(position, normal, paint, material, radius, duration, tickInterval, tickDamage, slowFactor);

            // 터지는 순간 근처 적에게 피해
            var seen = new System.Collections.Generic.HashSet<EnemyHealth>();
            foreach (var hit in Physics.OverlapSphere(position, 2.5f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (!hit.CompareTag("Enemy"))
                    continue;
                var enemy = hit.GetComponentInParent<EnemyHealth>();
                if (enemy != null && !enemy.IsDead && seen.Add(enemy))
                    enemy.ApplyDamage(impactDamage, false, enemy.transform.position + Vector3.up * 1.2f);
            }
        }
    }
}
