using UnityEngine;

namespace LastBullet.Effects
{
    // 적을 처치하면 가둬 둔 색이 물감으로 터져 나온다 (기획서 2.11).
    // 머리 자리에서 물감 방울이 사방으로 튀고, 바닥에는 물감 자국이 번졌다가 천천히 줄어든다.
    // 색은 그 적이 떨어뜨린 물감 색이다 (EnemyDrop이 정한다).
    // 구성 (프리팹 Prefabs/PaintBurst): 이 오브젝트 / ParticleSystem(방울, 같은 오브젝트) / 자식 Puddle(바닥 자국)
    // 붙일 곳: PaintBurst 프리팹의 맨 위 오브젝트
    public class PaintBurstEffect : MonoBehaviour
    {
        [SerializeField] ParticleSystem droplets;

        [Tooltip("Paint stain spreading on the ground (flat disc)")]
        [SerializeField] Transform puddle;

        [Tooltip("Maximum stain diameter (m)")]
        [SerializeField] float puddleSize = 1.5f;

        [Tooltip("Time for the stain to spread (seconds)")]
        [SerializeField] float spreadTime = 0.2f;

        [Tooltip("Time at which the stain starts shrinking (seconds)")]
        [SerializeField] float shrinkStart = 3.5f;

        [Tooltip("Time at which the stain disappears and this object is destroyed (seconds)")]
        [SerializeField] float lifetime = 5.5f;

        float startTime;
        bool hasPuddle;

        // 처치된 자리에 효과를 만든다. headPosition: 방울이 터지는 곳, feetPosition: 바닥 자국을 찾을 기준
        public static void Spawn(PaintBurstEffect prefab, Vector3 headPosition, Vector3 feetPosition, Color color)
        {
            var effect = Instantiate(prefab, headPosition, Quaternion.identity);
            effect.Play(color, feetPosition);
        }

        void Play(Color color, Vector3 feetPosition)
        {
            startTime = Time.time;

            var main = droplets.main;
            main.startColor = color;
            droplets.Play();

            // 바닥 자국: 발밑에서 아래로 쏴서 땅을 찾는다. 땅이 없으면 자국은 만들지 않는다.
            if (Physics.Raycast(feetPosition + Vector3.up * 1f, Vector3.down, out var hit, 5f, ~0, QueryTriggerInteraction.Ignore))
            {
                puddle.SetParent(null, true); // 적이 사라져도, 효과 오브젝트가 움직여도 제자리에 있다
                puddle.position = hit.point + Vector3.up * 0.03f;
                puddle.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                puddle.localScale = Vector3.zero;

                var block = new MaterialPropertyBlock();
                block.SetColor("_BaseColor", color);
                puddle.GetComponent<Renderer>().SetPropertyBlock(block);
                hasPuddle = true;
            }
            else
            {
                puddle.gameObject.SetActive(false);
            }
        }

        void Update()
        {
            float age = Time.time - startTime;
            if (hasPuddle)
            {
                // 0.2초 동안 번지고, 한참 있다가 줄어든다
                float size = puddleSize * Mathf.Clamp01(age / spreadTime);
                if (age > shrinkStart)
                    size *= 1f - Mathf.Clamp01((age - shrinkStart) / (lifetime - shrinkStart));
                puddle.localScale = new Vector3(size, 0.02f, size);
            }
            if (age >= lifetime)
            {
                if (puddle != null)
                    Destroy(puddle.gameObject);
                Destroy(gameObject);
            }
        }
    }
}
