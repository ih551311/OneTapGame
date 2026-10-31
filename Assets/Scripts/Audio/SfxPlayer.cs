using UnityEngine;

namespace LastBullet.Audio
{
    // 효과음 하나를 맡는 작은 재생기 (2026-10-31). 같은 소리의 변형 여러 개 중 하나를 무작위로 골라 재생한다.
    // 같은 소리를 계속 틀어도 덜 질리도록 음높이도 살짝 흔든다. 여운(tails)이 있으면 같이 깔아 준다.
    // 2D(플레이어 총소리)인지 3D(적 총소리: 멀수록 작아짐)인지는 같은 오브젝트의 AudioSource 설정(Spatial Blend)이 정한다.
    // 붙일 곳: 소리가 나는 오브젝트 (AudioSource와 함께). 플레이어의 사격, 원거리 적의 공격
    [RequireComponent(typeof(AudioSource))]
    public class SfxPlayer : MonoBehaviour
    {
        [Tooltip("변형들. 재생할 때마다 이 중 하나를 무작위로 고른다")]
        [SerializeField] AudioClip[] clips;

        [Tooltip("소리 뒤에 깔리는 여운 (없어도 된다). 이 중 하나를 같이 재생한다")]
        [SerializeField] AudioClip[] tails;

        [Range(0f, 1f)]
        [SerializeField] float volume = 1f;

        [Tooltip("여운의 크기 (volume에 곱해진다)")]
        [Range(0f, 1f)]
        [SerializeField] float tailVolume = 0.45f;

        [Tooltip("음높이를 이 범위에서 무작위로 흔든다 (1 = 원래 높이)")]
        [SerializeField] Vector2 pitchRange = new Vector2(0.96f, 1.04f);

        AudioSource source;

        void Awake()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
        }

        public void Play()
        {
            Play(1f, 1f);
        }

        // 크기·음높이를 이번 한 번만 바꿔서 낸다 (예: 점프는 같은 발소리를 더 크고 낮게)
        public void Play(float volumeMultiplier, float pitchMultiplier)
        {
            if (clips == null || clips.Length == 0)
                return;
            source.pitch = Random.Range(pitchRange.x, pitchRange.y) * pitchMultiplier;
            source.PlayOneShot(clips[Random.Range(0, clips.Length)], volume * volumeMultiplier);
            if (tails != null && tails.Length > 0)
                source.PlayOneShot(tails[Random.Range(0, tails.Length)], volume * volumeMultiplier * tailVolume);
        }
    }
}
