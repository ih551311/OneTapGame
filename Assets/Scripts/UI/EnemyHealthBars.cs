using System.Collections.Generic;
using LastBullet.Enemy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.UI
{
    // 적 머리 위 체력바 (2026-11-04, 데스티니식): 총알을 맞힌 적의 머리 위에 이름과 체력바가 몇 초 동안 보인다.
    //   체력바는 판(EnemyBar_Back.png)과 채움(EnemyBar_Fill.png, Image.Type.Filled)이다. 이름은 TextMeshPro(글꼴 font1).
    //   마지막으로 맞은 뒤 showTime초가 지나면 사라지고, 적이 죽으면 바로 사라진다. 카메라 뒤에 있으면 숨긴다.
    // 붙일 곳: Canvas/EnemyHealthBars (전체 화면 크기의 빈 오브젝트) (메뉴 LastBullet > Phase 63 데스티니식 전투 화면 만들기)
    public class EnemyHealthBars : MonoBehaviour
    {
        [SerializeField] Sprite backSprite;
        [SerializeField] Sprite fillSprite;
        [SerializeField] TMP_FontAsset font;

        [Tooltip("맞은 뒤 체력바가 보이는 시간 (초)")]
        [SerializeField] float showTime = 3.5f;

        [Tooltip("머리 위로 띄우는 높이 (m)")]
        [SerializeField] float heightAboveHead = 0.55f;

        [SerializeField] Vector2 barSize = new Vector2(150f, 12f);

        class Bar
        {
            public RectTransform root;
            public Image fill;
            public TextMeshProUGUI name;
            public EnemyHealth enemy;
            public Transform anchor;
            public float lastHit;
            public float shownRatio;
        }

        readonly Dictionary<EnemyHealth, Bar> bars = new Dictionary<EnemyHealth, Bar>();
        readonly Stack<Bar> pool = new Stack<Bar>();
        readonly List<EnemyHealth> remove = new List<EnemyHealth>();
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
            if (info.enemy == null)
                return;
            if (!bars.TryGetValue(info.enemy, out var bar))
            {
                bar = pool.Count > 0 ? pool.Pop() : CreateBar();
                bar.root.gameObject.SetActive(true);
                bar.enemy = info.enemy;
                bar.anchor = FindHead(info.enemy);
                bar.name.text = info.enemy.DisplayName;
                bar.shownRatio = Mathf.Clamp01((info.enemy.Health + info.damage) / info.enemy.MaxHealth);
                bars[info.enemy] = bar;
            }
            bar.lastHit = Time.unscaledTime;
        }

        static Transform FindHead(EnemyHealth enemy)
        {
            foreach (var t in enemy.GetComponentsInChildren<Transform>())
                if (t.name == "Head" && t.GetComponent<Collider>() != null)
                    return t;
            return enemy.transform;
        }

        Bar CreateBar()
        {
            var root = new GameObject("EnemyBar", typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(transform, false);
            root.sizeDelta = barSize;

            var back = new GameObject("Back", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            back.transform.SetParent(root, false);
            Stretch(back.rectTransform);
            back.sprite = backSprite;
            back.raycastTarget = false;

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            fill.transform.SetParent(root, false);
            Stretch(fill.rectTransform);
            fill.sprite = fillSprite;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.raycastTarget = false;

            var text = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            text.transform.SetParent(root, false);
            var textRect = text.rectTransform;
            textRect.anchorMin = new Vector2(0f, 1f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.pivot = new Vector2(0.5f, 0f);
            textRect.anchoredPosition = new Vector2(0f, 2f);
            textRect.sizeDelta = new Vector2(0f, 28f);
            if (font != null)
                text.font = font;
            text.fontSize = 22f;
            text.alignment = TextAlignmentOptions.Bottom;
            text.color = new Color(1f, 1f, 1f, 0.95f);
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return new Bar { root = root, fill = fill, name = text };
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        void Update()
        {
            if (cam == null)
                cam = Camera.main;
            float scale = canvas != null ? canvas.scaleFactor : 1f;
            remove.Clear();
            foreach (var pair in bars)
            {
                var bar = pair.Value;
                bool gone = bar.enemy == null || bar.enemy.IsDead;
                if (gone || Time.unscaledTime - bar.lastHit > showTime || cam == null)
                {
                    remove.Add(pair.Key);
                    continue;
                }
                Vector3 world = bar.anchor.position + Vector3.up * heightAboveHead;
                Vector3 screen = cam.WorldToScreenPoint(world);
                bool visible = screen.z > 0.1f;
                bar.root.gameObject.SetActive(visible);
                if (!visible)
                    continue;
                bar.root.position = screen;
                bar.root.localScale = Vector3.one;
                // 줄어드는 모습이 보이게 천천히 따라간다
                bar.shownRatio = Mathf.MoveTowards(bar.shownRatio, bar.enemy.HealthRatio, Time.unscaledDeltaTime * 1.5f);
                bar.fill.fillAmount = bar.shownRatio;
                float fade = Time.unscaledTime - bar.lastHit > showTime - 0.5f ? Mathf.Clamp01((showTime - (Time.unscaledTime - bar.lastHit)) / 0.5f) : 1f;
                bar.fill.color = new Color(1f, 1f, 1f, fade);
                bar.name.alpha = fade * 0.95f;
            }
            foreach (var key in remove)
            {
                var bar = bars[key];
                bar.root.gameObject.SetActive(false);
                pool.Push(bar);
                bars.Remove(key);
            }
        }
    }
}
