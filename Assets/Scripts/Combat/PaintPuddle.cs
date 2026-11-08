using System.Collections.Generic;
using LastBullet.Enemy;
using UnityEngine;

namespace LastBullet.Combat
{
    // 물감 폭탄이 터진 자리에 생기는 물감 구역 (2026-11-08 사용자 요청: 물감 폭탄 스킬).
    //  - 바닥에 번진 물감(투명 쿼드) 안에 들어간 적은 느려지고(ApplySlow) 일정 간격마다 지속 피해를 입는다.
    //  - 번지는 데 0.25초, 끝나기 1초 전부터 서서히 옅어지다 사라진다.
    //  - 보스는 느려짐이 약하다 (속도 70%까지만 느려진다).
    //  - 그림은 흰색 얼룩 Assets/UI/PaintBlob.png에 색을 입힌 머티리얼(Assets/Materials/FloorPaint_색.mat)이다.
    // 만드는 곳: PaintBombSkill (코드로 만든다 — 프리팹이 없다)
    public class PaintPuddle : MonoBehaviour
    {
        float radius, duration, tickInterval, tickDamage, slowFactor;
        Color color;
        Renderer visual;
        MaterialPropertyBlock block;
        float startTime, nextTick;

        public static PaintPuddle Spawn(Vector3 position, Vector3 normal, Color color, Material material, float radius, float duration, float tickInterval, float tickDamage, float slowFactor)
        {
            var go = new GameObject("PaintPuddle");
            go.transform.position = position;
            var puddle = go.AddComponent<PaintPuddle>();
            puddle.radius = radius;
            puddle.duration = duration;
            puddle.tickInterval = tickInterval;
            puddle.tickDamage = tickDamage;
            puddle.slowFactor = slowFactor;
            puddle.color = color;

            // 바닥에 눕힌 쿼드 (흰 얼룩의 반지름이 그림 가로의 약 0.33이라 지름을 키운다)
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Visual";
            Object.Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(go.transform, false);
            quad.transform.localPosition = normal * 0.04f;
            quad.transform.rotation = Quaternion.FromToRotation(Vector3.back, normal) * Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            quad.transform.localScale = Vector3.zero;
            var renderer = quad.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            puddle.visual = renderer;
            puddle.block = new MaterialPropertyBlock();
            puddle.startTime = Time.time;
            puddle.nextTick = Time.time + 0.15f; // 터지자마자 첫 피해
            return puddle;
        }

        void Update()
        {
            float age = Time.time - startTime;
            if (age >= duration)
            {
                Destroy(gameObject);
                return;
            }

            // 번지기 + 끝 무렵 옅어지기
            float spread = Mathf.Clamp01(age / 0.25f);
            float eased = 1f - (1f - spread) * (1f - spread);
            float size = radius * 2f / 0.66f * eased; // 얼룩 반지름 = 쿼드 가로의 약 0.33
            visual.transform.localScale = new Vector3(size, size, 1f);
            float fade = Mathf.Clamp01((duration - age) / 1f);
            var c = color;
            c.a = 0.78f * fade;
            block.SetColor("_BaseColor", c);
            visual.SetPropertyBlock(block);

            if (Time.time >= nextTick)
            {
                nextTick = Time.time + tickInterval;
                Tick();
            }
        }

        readonly HashSet<EnemyHealth> seen = new HashSet<EnemyHealth>();

        void Tick()
        {
            seen.Clear();
            foreach (var hit in Physics.OverlapSphere(transform.position, radius + 1f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (!hit.CompareTag("Enemy"))
                    continue;
                var health = hit.GetComponentInParent<EnemyHealth>();
                if (health == null || health.IsDead || !seen.Add(health))
                    continue;
                Vector3 flat = health.transform.position - transform.position;
                flat.y = 0f;
                if (flat.magnitude > radius)
                    continue; // 구역(바닥 원) 밖
                float slow = health.MaxHealth > 500f ? Mathf.Max(slowFactor, 0.7f) : slowFactor; // 보스는 덜 느려진다
                float hold = tickInterval * 1.6f;
                var melee = health.GetComponent<EnemyAI>();
                if (melee != null)
                    melee.ApplySlow(slow, hold);
                var ranged = health.GetComponent<RangedEnemyAI>();
                if (ranged != null)
                    ranged.ApplySlow(slow, hold);
                health.ApplyDamage(tickDamage, false, health.transform.position + Vector3.up * 1.2f);
            }
        }
    }
}
