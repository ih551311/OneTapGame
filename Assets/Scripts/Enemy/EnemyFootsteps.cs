using LastBullet.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastBullet.Enemy
{
    // 적 발소리 (2026-11-02 사용자 요청). 플레이어 발소리(MovementSounds)처럼 "움직인 거리"로 낸다:
    // 걸으면 한 걸음마다, 멈추면 바로 멈추고, 돌진·달리기는 걸음이 더 빠르고 크다. 이동 방식(NavMeshAgent, 돌진)과 상관없이 실제 위치 변화로 잰다.
    //  - 3D 소리(직선 감쇠): 가까울수록 크고 방향이 들린다. 플레이어에게서 hearRange보다 멀면 아예 내지 않아 많은 적이 한꺼번에 시끄럽지 않다.
    //  - 바닥 소리는 맵을 따른다: 사막은 모래, 그 밖(숲·성 마을)은 풀. 미술관에는 적이 없다.
    //  - 크기·음높이는 프리팹마다 다르다 (큰 돌진형은 낮고 묵직하게, 작은 분열체는 높고 가볍게).
    // 붙일 곳: 적 프리팹의 맨 위 오브젝트 (AudioSource와 함께. 메뉴 LastBullet > Phase 56 적 발소리 넣기)
    [RequireComponent(typeof(AudioSource))]
    public class EnemyFootsteps : MonoBehaviour
    {
        [Tooltip("풀 발소리 (숲·성 마을)")]
        [SerializeField] AudioClip[] grassClips;

        [Tooltip("모래 발소리 (사막)")]
        [SerializeField] AudioClip[] sandClips;

        [Tooltip("한 걸음 거리 (m). 걸을 때")]
        [SerializeField] float walkStride = 2.2f;

        [Tooltip("한 걸음 거리 (m). 달리거나 돌진할 때 (이 속도 이상)")]
        [SerializeField] float runStride = 3.0f;

        [Tooltip("이 속도(m/s) 이상이면 달리는 걸음")]
        [SerializeField] float runSpeed = 5f;

        [Range(0f, 1f)]
        [Tooltip("발소리 크기 (기본값)")]
        [SerializeField] float volume = 0.28f;

        [Tooltip("음높이 배율 (1보다 작으면 낮고 묵직하다)")]
        [SerializeField] float pitch = 1f;

        [Tooltip("플레이어가 이 거리(m)보다 멀면 소리를 내지 않는다")]
        [SerializeField] float hearRange = 20f;

        AudioSource source;
        Transform player;
        AudioClip[] clips;
        Vector3 lastPosition;
        float distance;

        void Awake()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
        }

        void Start()
        {
            lastPosition = transform.position;
            var playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
            clips = SceneManager.GetActiveScene().name == SceneNames.Desert ? sandClips : grassClips;
        }

        void Update()
        {
            if (Time.deltaTime <= 0f)
                return;
            Vector3 delta = transform.position - lastPosition;
            delta.y = 0f;
            lastPosition = transform.position;

            float step = delta.magnitude;
            if (step <= 0.0001f)
                return; // 서 있으면 걸음 거리를 쌓지 않는다
            float speed = step / Time.deltaTime;
            distance += step;

            bool running = speed >= runSpeed;
            if (distance < (running ? runStride : walkStride))
                return;
            distance = 0f;

            if (clips == null || clips.Length == 0)
                return;
            if (player != null && (player.position - transform.position).sqrMagnitude > hearRange * hearRange)
                return; // 멀리 있는 적은 소리를 내지 않는다
            source.pitch = Random.Range(0.94f, 1.06f) * pitch * (running ? 1.08f : 1f);
            source.PlayOneShot(clips[Random.Range(0, clips.Length)], running ? volume * 1.3f : volume);
        }
    }
}
