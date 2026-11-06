using System.Collections;
using LastBullet.Player;
using UnityEngine;

namespace LastBullet.Enemy
{
    // 보스의 내려치기 (2026-11-06 사용자 요청: 보스가 나오게). 돌진형 거대 버전 보스가 가끔 땅을 내려쳐서 주변에 충격을 준다.
    //  - 플레이어를 쫓는 중 일정 거리 안에 오면, 보스가 멈추고 바닥에 붉은 원(경고)이 점점 커진다 (windupTime 동안).
    //  - 원이 다 차면 충격파: 원 안에 있고 땅에 발이 닿은 플레이어가 피해를 입는다. **점프해서 피할 수 있다.** 회피(Q)의 무적으로도 피한다.
    //  - 내려친 뒤 잠깐 숨을 고르고(recoverTime) 다시 쫓는다. 다음 내려치기는 interval초 뒤.
    //  - 경고 원 그림은 Assets/Textures/BossSlamRing.png, 머티리얼은 Assets/Materials/BossSlamRing.mat 라서 바꿔 끼울 수 있다.
    // 붙일 곳: 보스 프리팹(Enemy_Boss)의 맨 위 오브젝트 (EnemyAI와 함께, 자식 SlamRing 연결)
    [RequireComponent(typeof(EnemyAI), typeof(EnemyHealth))]
    public class BossSlam : MonoBehaviour
    {
        [Tooltip("경고 원 (바닥에 눕힌 쿼드). 평소에는 꺼져 있다")]
        [SerializeField] Transform ring;

        [Header("내려치기 (기본값)")]
        [Tooltip("다음 내려치기까지 걸리는 시간 (초)")]
        [SerializeField] float interval = 8f;

        [Tooltip("플레이어가 이 거리 안에 있을 때만 시작한다 (m)")]
        [SerializeField] float triggerRange = 16f;

        [Tooltip("경고 원이 다 커지기까지의 시간 (초). 이 안에 점프할 준비를 한다")]
        [SerializeField] float windupTime = 1.3f;

        [Tooltip("충격파 반지름 (m)")]
        [SerializeField] float radius = 9f;

        [SerializeField] int damage = 25;

        [Tooltip("내려친 뒤 숨 고르는 시간 (초)")]
        [SerializeField] float recoverTime = 0.8f;

        EnemyAI ai;
        EnemyHealth health;
        Transform player;
        PlayerHealth playerHealth;
        CharacterController playerController;
        float nextTime;
        bool slamming;
        Renderer ringRenderer;
        MaterialPropertyBlock block;

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        void Awake()
        {
            ai = GetComponent<EnemyAI>();
            health = GetComponent<EnemyHealth>();
            block = new MaterialPropertyBlock();
            if (ring != null)
            {
                ringRenderer = ring.GetComponent<Renderer>();
                ring.gameObject.SetActive(false);
            }
        }

        void Start()
        {
            var playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
                playerHealth = playerObject.GetComponent<PlayerHealth>();
                playerController = playerObject.GetComponent<CharacterController>();
            }
            nextTime = Time.time + interval * 0.5f; // 처음에는 조금 일찍
        }

        void Update()
        {
            if (slamming || health.IsDead || player == null)
                return;
            if (playerHealth != null && playerHealth.IsDead)
                return;
            if (ai.State != EnemyAI.AIState.Chase || Time.time < nextTime)
                return;
            Vector3 offset = player.position - transform.position;
            offset.y = 0f;
            if (offset.magnitude <= triggerRange)
                StartCoroutine(Slam());
        }

        void OnDisable()
        {
            if (ring != null)
                ring.gameObject.SetActive(false);
            if (ai != null)
                ai.Paused = false;
        }

        IEnumerator Slam()
        {
            slamming = true;
            ai.Paused = true;

            // 경고 원: 보스 발밑에서 점점 커진다 (보스 크기와 상관없이 월드 반지름 radius까지)
            if (ring != null)
            {
                ring.gameObject.SetActive(true);
                ring.position = new Vector3(transform.position.x, transform.position.y + 0.06f, transform.position.z);
                ring.rotation = Quaternion.Euler(90f, 0f, 0f);
            }
            for (float t = 0f; t < windupTime; t += Time.deltaTime)
            {
                if (health.IsDead)
                    break;
                float k = Mathf.Clamp01(t / windupTime);
                SetRing(radius * 2f * Mathf.Lerp(0.15f, 1f, k), Mathf.Lerp(0.25f, 0.7f, k));
                yield return null;
            }

            if (!health.IsDead)
            {
                // 충격파: 원 안에서 땅에 서 있는 플레이어만 맞는다 (점프로 피하기)
                if (player != null && playerHealth != null && !playerHealth.IsDead)
                {
                    Vector3 offset = player.position - transform.position;
                    offset.y = 0f;
                    bool grounded = playerController == null || playerController.isGrounded;
                    if (offset.magnitude <= radius && grounded)
                        playerHealth.TakeDamage(damage);
                }
                SetRing(radius * 2f, 1f); // 번쩍
                yield return new WaitForSeconds(0.15f);
            }

            if (ring != null)
                ring.gameObject.SetActive(false);
            yield return new WaitForSeconds(recoverTime);

            ai.Paused = false;
            slamming = false;
            nextTime = Time.time + interval;
        }

        // 경고 원 크기(월드 지름 m)와 진하기
        void SetRing(float diameter, float alpha)
        {
            if (ring == null)
                return;
            float parentScale = ring.parent != null ? ring.parent.lossyScale.x : 1f;
            ring.localScale = new Vector3(diameter / parentScale, diameter / parentScale, 1f);
            if (ringRenderer != null)
            {
                ringRenderer.GetPropertyBlock(block);
                block.SetColor(BaseColor, new Color(1f, 0.15f, 0.1f, alpha));
                ringRenderer.SetPropertyBlock(block);
            }
        }
    }
}
