using LastBullet.Enemy;
using UnityEngine;

namespace LastBullet.Audio
{
    // 적을 맞혔을 때 나는 소리 (2026-11-04, 데스티니식 전투). 화면 속 힛마커와 같은 때에 난다.
    //  - 몸에 맞으면 짧은 "틱", 머리(크리티컬)에 맞으면 높은 "딩", 처치하면 낮은 "퍽".
    //  - 소리 파일은 Assets/Audio/Combat/Hit_Body.wav, Hit_Crit.wav, Hit_Kill.wav 이고, 같은 이름으로 덮어써서 바꿀 수 있다.
    //  - 2D 소리라서 거리와 상관없이 같은 크기로 들린다.
    // 붙일 곳: Player (메뉴 LastBullet > Phase 64 총구 불꽃·탄피·맞은 소리 만들기)
    public class HitSounds : MonoBehaviour
    {
        [SerializeField] AudioSource source;
        [SerializeField] AudioClip bodyClip;
        [SerializeField] AudioClip critClip;
        [SerializeField] AudioClip killClip;

        [Range(0f, 1f)]
        [SerializeField] float bodyVolume = 0.35f;
        [Range(0f, 1f)]
        [SerializeField] float critVolume = 0.6f;
        [Range(0f, 1f)]
        [SerializeField] float killVolume = 0.7f;

        void OnEnable()
        {
            EnemyHealth.DamageDealt += OnDamage;
        }

        void OnDisable()
        {
            EnemyHealth.DamageDealt -= OnDamage;
        }

        void OnDamage(EnemyHealth.DamageInfo info)
        {
            if (source == null || info.damage <= 0f)
                return;
            source.pitch = Random.Range(0.96f, 1.04f);
            if (info.killed)
            {
                if (killClip != null)
                    source.PlayOneShot(killClip, killVolume);
                if (info.critical && critClip != null)
                    source.PlayOneShot(critClip, critVolume * 0.7f);
            }
            else if (info.critical)
            {
                if (critClip != null)
                    source.PlayOneShot(critClip, critVolume);
            }
            else if (bodyClip != null)
                source.PlayOneShot(bodyClip, bodyVolume);
        }
    }
}
