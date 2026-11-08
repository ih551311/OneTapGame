using UnityEngine;

namespace LastBullet.Combat
{
    // 바주카 로켓 (2026-11-08). 총구에서 조준 방향으로 직선으로 날아가다가, 무엇이든 처음 닿는 곳(적·벽·땅)에서 터진다.
    //  - 몸체는 RPG의 로켓처럼 올리브색 캡슐 + 물감색 머리, 꼬리는 물감색 줄기가 남는다.
    //  - 매 프레임 움직일 거리만큼 구 모양으로 쓸어 보며(SphereCast) 닿았는지 본다. 빠른 속도에도 벽을 뚫지 않는다.
    //  - 플레이어 자신과 트리거 콜라이더는 무시한다. 최대 사거리까지 아무것도 없으면 그 자리에서 터진다.
    // 만드는 곳: BazookaWeapon.Launch (코드로 만든다 — 프리팹이 없다)
    public class BazookaRocket : MonoBehaviour
    {
        BazookaWeapon owner;
        Transform ownerRoot;
        float speed, remaining;
        Color paint;
        int colorId;
        readonly RaycastHit[] hits = new RaycastHit[8];

        public static BazookaRocket Spawn(BazookaWeapon owner, Vector3 origin, Vector3 direction, float speed, float range,
            Color paint, int colorId, Material bodyMaterial, Material paintMaterial)
        {
            var go = new GameObject("BazookaRocket");
            go.transform.position = origin;
            go.transform.rotation = Quaternion.LookRotation(direction);
            var rocket = go.AddComponent<BazookaRocket>();
            rocket.owner = owner;
            rocket.ownerRoot = owner.transform;
            rocket.speed = speed;
            rocket.remaining = range;
            rocket.paint = paint;
            rocket.colorId = colorId;

            // 몸체 (캡슐을 앞(+Z)으로 눕힌다)
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Object.Destroy(body.GetComponent<Collider>());
            body.name = "Body";
            body.transform.SetParent(go.transform, false);
            body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            body.transform.localScale = new Vector3(0.12f, 0.28f, 0.12f);
            Style(body, bodyMaterial, null);

            // 물감색 머리
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(head.GetComponent<Collider>());
            head.name = "Head";
            head.transform.SetParent(go.transform, false);
            head.transform.localPosition = new Vector3(0f, 0f, 0.3f);
            head.transform.localScale = Vector3.one * 0.17f;
            Style(head, paintMaterial, paint);

            // 꼬리
            var trailObject = new GameObject("Trail");
            trailObject.transform.SetParent(go.transform, false);
            var trail = trailObject.AddComponent<TrailRenderer>();
            trail.time = 0.4f;
            trail.startWidth = 0.22f;
            trail.endWidth = 0f;
            trail.minVertexDistance = 0.1f;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (paintMaterial != null)
                trail.sharedMaterial = paintMaterial;
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", paint);
            trail.SetPropertyBlock(block);
            return rocket;
        }

        static void Style(GameObject part, Material material, Color? color)
        {
            var renderer = part.GetComponent<MeshRenderer>();
            if (material != null)
                renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (color.HasValue)
            {
                var block = new MaterialPropertyBlock();
                block.SetColor("_BaseColor", color.Value);
                renderer.SetPropertyBlock(block);
            }
        }

        void Update()
        {
            float step = Mathf.Min(speed * Time.deltaTime, remaining);
            Vector3 direction = transform.forward;
            int count = Physics.SphereCastNonAlloc(transform.position, 0.12f, direction, hits, step, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            int bestIndex = -1;
            for (int i = 0; i < count; i++)
            {
                if (hits[i].collider.transform.IsChildOf(ownerRoot))
                    continue;
                if (hits[i].distance < best)
                {
                    best = hits[i].distance;
                    bestIndex = i;
                }
            }
            if (bestIndex >= 0)
            {
                Vector3 point = transform.position + direction * best;
                Vector3 normal = hits[bestIndex].normal.sqrMagnitude > 0.01f ? hits[bestIndex].normal : -direction;
                Detonate(point, normal);
                return;
            }
            transform.position += direction * step;
            remaining -= step;
            if (remaining <= 0.01f)
                Detonate(transform.position, Vector3.up);
        }

        void Detonate(Vector3 point, Vector3 normal)
        {
            owner.Explode(point, normal, paint, colorId);
            // 꼬리가 바로 사라지지 않게 잠깐 남긴다
            var trailObject = transform.Find("Trail");
            if (trailObject != null)
            {
                trailObject.SetParent(null);
                Destroy(trailObject.gameObject, 0.5f);
            }
            Destroy(gameObject);
        }
    }
}
