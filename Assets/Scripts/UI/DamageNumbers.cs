using System.Collections.Generic;
using LastBullet.Enemy;
using TMPro;
using UnityEngine;

namespace LastBullet.UI
{
    // 데미지 숫자 (2026-11-04, 데스티니식): 적을 맞히면 맞은 자리에서 숫자가 떠올랐다 사라진다.
    //   일반 = 흰색 작게, 크리티컬(머리) = 노랑 크게. 같은 자리에 겹치지 않게 조금씩 흩어진다.
    //   글자는 TextMeshPro(글꼴 font1)이고 Canvas 아래에 만들어진다. 한 번 만든 글자는 재활용한다.
    // 붙일 곳: Canvas/DamageNumbers (전체 화면 크기의 빈 오브젝트) (메뉴 LastBullet > Phase 63 데스티니식 전투 화면 만들기)
    public class DamageNumbers : MonoBehaviour
    {
        [SerializeField] TMP_FontAsset font;
        [SerializeField] Color normalColor = new Color(1f, 1f, 1f, 1f);
        [SerializeField] Color criticalColor = new Color(1f, 0.85f, 0.25f, 1f);
        [SerializeField] float normalSize = 30f;
        [SerializeField] float criticalSize = 44f;

        [Tooltip("글자가 보이는 시간 (초)")]
        [SerializeField] float lifetime = 0.75f;

        [Tooltip("떠오르는 높이 (화면 px, 1920×1080 기준)")]
        [SerializeField] float riseHeight = 70f;

        class Entry
        {
            public TextMeshProUGUI text;
            public Vector3 world;
            public Vector2 offset;
            public float start;
            public bool critical;
            public Color color;
        }

        readonly List<Entry> active = new List<Entry>();
        readonly Stack<TextMeshProUGUI> pool = new Stack<TextMeshProUGUI>();
        Camera cam;
        Canvas canvas;

        void Awake()
        {
            canvas = GetComponentInParent<Canvas>();
        }

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
            if (info.damage <= 0f)
                return;
            TextMeshProUGUI text = pool.Count > 0 ? pool.Pop() : CreateText();
            text.gameObject.SetActive(true);
            text.text = Mathf.RoundToInt(info.damage).ToString();
            // 피해가 클수록 숫자가 커지고(바주카 폭발 80~90이면 약 2배) 주황빛이 돈다 (2026-11-11 게임 주스)
            float big = Mathf.InverseLerp(25f, 90f, info.damage);
            text.fontSize = (info.critical ? criticalSize : normalSize) * Mathf.Lerp(1f, 2.1f, big);
            text.color = info.critical ? criticalColor : Color.Lerp(normalColor, new Color(1f, 0.62f, 0.2f, 1f), big);
            active.Add(new Entry
            {
                text = text,
                world = info.point,
                offset = new Vector2(Random.Range(-28f, 28f), Random.Range(0f, 24f)),
                start = Time.unscaledTime,
                critical = info.critical,
                color = text.color,
            });
        }

        TextMeshProUGUI CreateText()
        {
            var go = new GameObject("Number", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(transform, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            if (font != null)
                text.font = font;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.fontStyle = FontStyles.Bold;
            var rect = text.rectTransform;
            rect.sizeDelta = new Vector2(140f, 60f);
            return text;
        }

        void Update()
        {
            if (cam == null)
                cam = Camera.main;
            float scale = canvas != null ? canvas.scaleFactor : 1f;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var e = active[i];
                float t = (Time.unscaledTime - e.start) / lifetime;
                if (t >= 1f || cam == null)
                {
                    e.text.gameObject.SetActive(false);
                    pool.Push(e.text);
                    active.RemoveAt(i);
                    continue;
                }
                Vector3 screen = cam.WorldToScreenPoint(e.world);
                if (screen.z <= 0f)
                {
                    e.text.enabled = false;
                    continue;
                }
                e.text.enabled = true;
                float ease = 1f - (1f - t) * (1f - t);
                Vector2 position = (Vector2)screen + (e.offset + Vector2.up * (riseHeight * ease)) * scale;
                e.text.rectTransform.position = position;
                var color = e.color;
                color.a = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
                e.text.color = color;
                float pop = t < 0.12f ? Mathf.Lerp(1.5f, 1f, t / 0.12f) : 1f;
                e.text.rectTransform.localScale = Vector3.one * pop;
            }
        }
    }
}
