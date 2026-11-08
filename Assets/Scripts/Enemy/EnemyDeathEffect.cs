using System.Collections;
using LastBullet.Inventory;
using UnityEngine;
using UnityEngine.AI;

namespace LastBullet.Enemy
{
    // 적이 처치될 때의 연출 (2026-11-04 사용자 요청: 터지는 효과, 물감이 튀는 효과, 쓰러지는 동작. 예전에는 그냥 사라졌다).
    //  1) 처치 순간: 움직임·소리·충돌을 멈추고, 총 맞은 반대쪽으로 뒤로 넘어지기 시작한다 (물감이 머리에서 튀는 것은 EnemyDrop의 PaintBurst가 이미 한다).
    //  2) 넘어지는 동안(fallTime): 발을 축으로 점점 빨리 쓰러진다.
    //  3) 다 쓰러지면: 몸 한가운데에서 그 적의 물감 색 방울이 터지고(popPrefab), 몸이 순식간에 줄어들며 사라진다 (vanishTime).
    // 분열 적이 나누는 것·물감·체력 보상은 처치 순간에 바로 처리되므로 연출 때문에 늦어지지 않는다.
    // 붙일 곳: 적 프리팹의 맨 위 오브젝트 (메뉴 LastBullet > Phase 67 처치 연출 만들기)
    [RequireComponent(typeof(EnemyHealth))]
    public class EnemyDeathEffect : MonoBehaviour
    {
        [Tooltip("쓰러지는 데 걸리는 시간 (초)")]
        [SerializeField] float fallTime = 0.5f;

        [Tooltip("다 쓰러진 뒤 줄어들며 사라지는 시간 (초)")]
        [SerializeField] float vanishTime = 0.15f;

        [Tooltip("쓰러지며 뒤로 밀리는 거리 (m)")]
        [SerializeField] float pushBack = 0.7f;

        [Tooltip("몸이 터질 때의 물감 방울 연출 (Prefabs/DeathPop). 비우면 터지지 않고 그냥 사라진다")]
        [SerializeField] GameObject popPrefab;

        [Header("소리 (2026-11-08)")]
        [Tooltip("몸이 터질 때(다 쓰러졌을 때) 나는 소리 (Assets/Audio/Combat/Enemy_Death.wav). 비우면 소리 없음. 같은 이름으로 덮어쓰면 바뀐다")]
        [SerializeField] AudioClip popSound;

        [Range(0f, 1f)]
        [SerializeField] float popVolume = 0.55f;

        public float Duration => fallTime + vanishTime;

        void Awake()
        {
            GetComponent<EnemyHealth>().Died += OnDied;
        }

        void OnDied(EnemyHealth _)
        {
            // 움직임·소리·충돌을 멈춘다 (EnemyHealth·EnemyDrop·EnemySplitter·이 컴포넌트는 처치 처리를 위해 둔다)
            foreach (var behaviour in GetComponents<Behaviour>())
            {
                if (behaviour == this || behaviour is EnemyHealth || behaviour is EnemyDrop || behaviour is EnemySplitter)
                    continue;
                behaviour.enabled = false;
            }
            var agent = GetComponent<NavMeshAgent>();
            if (agent != null)
                agent.enabled = false;
            foreach (var collider in GetComponentsInChildren<Collider>())
                collider.enabled = false;
            foreach (var animator in GetComponentsInChildren<Animator>())
                animator.speed = 0f; // 마지막 자세 그대로 굳어서 쓰러진다
            foreach (var source in GetComponentsInChildren<AudioSource>())
                source.Stop();

            StartCoroutine(Fall());
        }

        IEnumerator Fall()
        {
            // 쓰러지는 방향 = 플레이어에게서 멀어지는 쪽 (총에 맞아 뒤로 넘어지는 느낌)
            Vector3 away = Vector3.zero;
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                away = transform.position - player.transform.position;
                away.y = 0f;
            }
            if (away.sqrMagnitude < 0.01f)
                away = Random.insideUnitSphere;
            away.y = 0f;
            away.Normalize();
            Vector3 axis = Vector3.Cross(Vector3.up, away);

            Quaternion startRotation = transform.rotation;
            Vector3 startPosition = transform.position;
            for (float t = 0f; t < fallTime; t += Time.deltaTime)
            {
                float k = Mathf.Clamp01(t / fallTime);
                float eased = k * k; // 처음엔 천천히, 갈수록 빨리 (중력)
                transform.rotation = Quaternion.AngleAxis(90f * eased, axis) * startRotation;
                transform.position = startPosition + away * (pushBack * k);
                yield return null;
            }
            transform.rotation = Quaternion.AngleAxis(90f, axis) * startRotation;
            transform.position = startPosition + away * pushBack;

            // 터지는 연출: 몸 한가운데에서 그 적의 물감 색 방울
            if (popPrefab != null)
            {
                Color color = Color.white;
                var drop = GetComponent<EnemyDrop>();
                if (drop != null)
                    color = PaintColors.Colors[(int)drop.DropColor];
                var pop = Instantiate(popPrefab, BodyCenter(), Quaternion.identity);
                foreach (var system in pop.GetComponentsInChildren<ParticleSystem>())
                {
                    var main = system.main;
                    main.startColor = color;
                }
                Destroy(pop, 2f);
            }
            // 터지는 소리: 몸이 있던 자리에서 나는 3D 소리 (멀면 작게 들린다)
            if (popSound != null)
                AudioSource.PlayClipAtPoint(popSound, BodyCenter(), popVolume);

            // 순식간에 줄어들며 사라진다
            Vector3 startScale = transform.localScale;
            for (float t = 0f; t < vanishTime; t += Time.deltaTime)
            {
                transform.localScale = startScale * (1f - Mathf.Clamp01(t / vanishTime));
                yield return null;
            }
            transform.localScale = Vector3.zero;
        }

        Vector3 BodyCenter()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return transform.position;
            var bounds = renderers[0].bounds;
            foreach (var r in renderers)
                bounds.Encapsulate(r.bounds);
            return bounds.center;
        }
    }
}
