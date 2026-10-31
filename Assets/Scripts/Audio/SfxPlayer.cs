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
        [Tooltip("Variations. One of these is picked at random each time it plays.")]
        [SerializeField] AudioClip[] clips;

        [Tooltip("Tail sounds layered behind the main sound (optional). One of these plays together with it.")]
        [SerializeField] AudioClip[] tails;

        [Range(0f, 1f)]
        [SerializeField] float volume = 1f;

        [Tooltip("Tail loudness (multiplied by volume)")]
        [Range(0f, 1f)]
        [SerializeField] float tailVolume = 0.45f;

        [Tooltip("Pitch is randomized within this range (1 = original pitch)")]
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
