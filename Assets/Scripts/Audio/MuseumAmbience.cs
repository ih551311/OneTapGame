using LastBullet.Stage;
using UnityEngine;

namespace LastBullet.Audio
{
    // 미술관의 작은 소리들 (2026-11-05 사용자 요청: 벽시계 소리, 바닥 삐걱 소리).
    //  - 벽시계: 1초마다 "똑" · "딱"을 번갈아 낸다 (벽시계 위치에서 나는 3D 소리라 멀면 작게 들린다).
    //  - 삐걱: 가끔(기본 18~45초마다) 플레이어 근처 어딘가에서 오래된 마룻바닥이 삐걱거린다 (3D 소리, 방향이 느껴진다).
    //  - 소리 파일은 Assets/Audio/Museum/ 의 Clock_Tick·Clock_Tock·Creak_01~03.wav 이고, 같은 이름으로 덮어써서 바꿀 수 있다.
    // 붙일 곳: 미술관 MuseumAmbience 오브젝트 (메뉴 LastBullet > Phase 71 벽시계와 삐걱 소리 만들기)
    public class MuseumAmbience : MonoBehaviour
    {
        [SerializeField] WallClock clock;
        [SerializeField] AudioSource clockSource;
        [SerializeField] AudioClip tick;
        [SerializeField] AudioClip tock;

        [SerializeField] AudioSource creakSource;
        [SerializeField] AudioClip[] creaks;

        [Range(0f, 1f)]
        [SerializeField] float clockVolume = 0.35f;

        [Range(0f, 1f)]
        [SerializeField] float creakVolume = 0.4f;

        [Tooltip("삐걱 소리 사이의 시간 (초, 최소~최대)")]
        [SerializeField] Vector2 creakInterval = new Vector2(18f, 45f);

        [Tooltip("삐걱 소리가 나는 곳이 플레이어에서 떨어진 거리 (m, 최소~최대)")]
        [SerializeField] Vector2 creakDistance = new Vector2(4f, 9f);

        int lastSecond = -1;
        float nextCreakTime;
        Transform player;

        void Start()
        {
            var playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
            nextCreakTime = Time.time + Random.Range(creakInterval.x, creakInterval.y);
        }

        void Update()
        {
            if (Time.timeScale <= 0f)
                return; // 일시정지 중에는 조용히
            UpdateClock();
            UpdateCreak();
        }

        void UpdateClock()
        {
            if (clock == null || clockSource == null)
                return;
            int second = clock.Second;
            if (second < 0 || second == lastSecond)
                return;
            lastSecond = second;
            var clip = second % 2 == 0 ? tick : tock;
            if (clip != null)
                clockSource.PlayOneShot(clip, clockVolume);
        }

        void UpdateCreak()
        {
            if (creakSource == null || creaks == null || creaks.Length == 0 || player == null || Time.time < nextCreakTime)
                return;
            nextCreakTime = Time.time + Random.Range(creakInterval.x, creakInterval.y);
            // 플레이어 둘레의 아무 방향, 바닥 높이에서
            Vector2 around = Random.insideUnitCircle.normalized * Random.Range(creakDistance.x, creakDistance.y);
            creakSource.transform.position = player.position + new Vector3(around.x, 0f, around.y);
            creakSource.pitch = Random.Range(0.9f, 1.1f);
            creakSource.PlayOneShot(creaks[Random.Range(0, creaks.Length)], creakVolume);
        }
    }
}
