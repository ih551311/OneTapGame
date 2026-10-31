using UnityEngine;

namespace LastBullet.Audio
{
    // 배경음악 (2026-10-31 사용자 요청). 씬이 시작되면 곡이 서서히 커지며 시작하고 끝없이 되풀이한다.
    // 소리 크기는 옵션의 소리 크기(AudioListener.volume)에 곱해져서 따라간다.
    // 일시정지(시간 0)에도 곡은 계속 나온다 (페이드만 시간 0의 영향을 안 받게 unscaled 시간을 쓴다).
    // 붙일 곳: 타이틀·미술관 씬의 BackgroundMusic 오브젝트 (AudioSource와 함께. 메뉴 LastBullet > Phase 27 배경음악 넣기)
    [RequireComponent(typeof(AudioSource))]
    public class MusicPlayer : MonoBehaviour
    {
        [Tooltip("Track to loop (LOOP version: the end joins the start smoothly)")]
        [SerializeField] AudioClip music;

        [Range(0f, 1f)]
        [SerializeField] float volume = 0.3f;

        [Tooltip("Fade-in time at the start (seconds)")]
        [SerializeField] float fadeInTime = 2f;

        AudioSource source;
        float elapsed;

        void Awake()
        {
            source = GetComponent<AudioSource>();
            source.clip = music;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f; // 2D: 어디서든 똑같이 들린다
            source.volume = 0f;
        }

        void Start()
        {
            if (music != null)
                source.Play();
        }

        void Update()
        {
            if (elapsed >= fadeInTime)
                return;
            elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.1f); // 씬을 불러온 직후 시간 튐 방지
            source.volume = volume * Mathf.Clamp01(elapsed / fadeInTime);
        }
    }
}
