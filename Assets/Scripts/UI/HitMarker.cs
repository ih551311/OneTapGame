using LastBullet.Enemy;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.UI
{
    // 힛마커 (2026-11-04, 데스티니식): 총알이 적에 맞으면 조준점 주위에 작은 ×가 번쩍인다.
    //   일반 = 흰색, 크리티컬(머리) = 노랑, 처치 = 빨강으로 더 크게.
    //   그림은 Assets/UI/HitMarker.png (흰색 ×, 색은 코드가 입힌다) — 바꿔 끼울 수 있다.
    // 붙일 곳: Canvas/HitMarker (Image) (메뉴 LastBullet > Phase 63 데스티니식 전투 화면 만들기)
    public class HitMarker : MonoBehaviour
    {
        [SerializeField] Image image;

        [SerializeField] Color normalColor = new Color(1f, 1f, 1f, 0.95f);
        [SerializeField] Color criticalColor = new Color(1f, 0.85f, 0.25f, 1f);
        [SerializeField] Color killColor = new Color(1f, 0.25f, 0.2f, 1f);

        [Tooltip("번쩍이는 시간 (초)")]
        [SerializeField] float showTime = 0.18f;

        float endTime;
        float startScale = 1f;
        Color current;

        void OnEnable()
        {
            EnemyHealth.DamageDealt += OnDamage;
            if (image != null)
                image.enabled = false;
        }

        void OnDisable()
        {
            EnemyHealth.DamageDealt -= OnDamage;
        }

        void OnDamage(EnemyHealth.DamageInfo info)
        {
            if (image == null)
                return;
            current = info.killed ? killColor : info.critical ? criticalColor : normalColor;
            startScale = info.killed ? 1.6f : info.critical ? 1.3f : 1.1f;
            // 큰 피해(바주카 폭발 등 40 이상)는 마커도 더 크게 번쩍인다 (2026-11-11 게임 주스)
            startScale *= Mathf.Lerp(1f, 1.9f, Mathf.InverseLerp(30f, 90f, info.damage));
            endTime = Time.unscaledTime + showTime;
            image.enabled = true;
        }

        void Update()
        {
            if (image == null || !image.enabled)
                return;
            float remaining = endTime - Time.unscaledTime;
            if (remaining <= 0f)
            {
                image.enabled = false;
                return;
            }
            float t = remaining / showTime; // 1 → 0
            var color = current;
            color.a *= t;
            image.color = color;
            image.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, startScale, t);
        }
    }
}
