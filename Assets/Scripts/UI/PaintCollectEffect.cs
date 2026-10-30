using System.Collections.Generic;
using LastBullet.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.UI
{
    // 물감 획득 연출 (2026-10-31 사용자 요청): 적을 잡으면 그 자리에서 물감 방울이 튀어 올랐다가
    // 화면 오른쪽 위 전투 가방의 그 색 아이콘으로 날아 들어가고, 도착하면 아이콘이 통통 튄다.
    // 피격 효과(HitFeedback)와 짝: 맞는 느낌과 얻는 느낌.
    // 그림: Assets/UI/PaintDrop_<색>.png (9장, 직접 그린 그림으로 바꿀 수 있다)
    // 붙일 곳: 전투 맵 Canvas/PaintCollect (자식 Drop은 방울 틀, 평소에는 꺼져 있다)
    public class PaintCollectEffect : MonoBehaviour
    {
        public static PaintCollectEffect Current { get; private set; }

        [Tooltip("방울 한 개의 틀 (꺼 둔다). Image 하나")]
        [SerializeField] Image dropTemplate;

        [Tooltip("물감 9색 순서(빨강, 주황, 노랑, 초록, 파랑, 남색, 보라, 하양, 검정)의 방울 그림")]
        [SerializeField] Sprite[] dropSprites = new Sprite[PaintColors.Count];

        [Tooltip("같은 순서의 도착 지점: 전투 가방 패널의 물감 아이콘")]
        [SerializeField] RectTransform[] targets = new RectTransform[PaintColors.Count];

        [Header("연출")]
        [SerializeField] int dropsPerKill = 5;
        [SerializeField] float dropSize = 40f;
        [Tooltip("처치 자리에서 튀어 오르며 흩어지는 시간 (처치 이펙트가 보이도록)")]
        [SerializeField] float burstTime = 0.25f;
        [SerializeField] float flyTime = 0.6f;
        [Tooltip("도착한 아이콘이 통통 튀는 시간과 커지는 정도")]
        [SerializeField] float punchTime = 0.25f;
        [SerializeField] float punchScale = 0.4f;

        class Drop
        {
            public RectTransform rect;
            public int color;
            public Vector2 start, burst, control, end;
            public float delay, age;
            public float fly;
        }

        readonly List<Drop> drops = new List<Drop>();
        readonly float[] punchLeft = new float[PaintColors.Count];
        RectTransform root;

        void Awake()
        {
            root = (RectTransform)transform;
            dropTemplate.gameObject.SetActive(false);
        }

        void OnEnable()
        {
            Current = this;
        }

        void OnDisable()
        {
            if (Current == this)
                Current = null;
        }

        // 월드 위치(적의 머리 등)에서 가방 아이콘으로 방울 여러 개를 날린다. 연출이 없는 씬에서는 아무 일도 없다.
        public static void Play(PaintColor color, Vector3 worldPosition)
        {
            if (Current != null)
                Current.Spawn((int)color, worldPosition);
        }

        void Spawn(int color, Vector3 worldPosition)
        {
            var camera = Camera.main;
            if (camera == null || targets[color] == null)
                return;

            // 화면 좌표 → 이 캔버스 안의 좌표 (오버레이 캔버스). 카메라 뒤쪽이면 화면 가운데 근처에서 시작한다.
            Vector3 screen = camera.WorldToScreenPoint(worldPosition);
            if (screen.z < 0f)
                screen = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out Vector2 start);
            Vector2 end = root.InverseTransformPoint(targets[color].position);

            for (int i = 0; i < dropsPerKill; i++)
            {
                var image = Instantiate(dropTemplate, root);
                image.sprite = dropSprites[color];
                image.rectTransform.sizeDelta = Vector2.one * dropSize;
                image.gameObject.SetActive(true);

                // 처치 자리에서 위로 솟으며 퍼지는 자리, 그리고 가방으로 가는 곡선의 중간점(위쪽으로 휜다)
                Vector2 burst = start + new Vector2(Random.Range(-70f, 70f), Random.Range(20f, 90f));
                Vector2 control = (burst + end) * 0.5f + new Vector2(Random.Range(-120f, 120f), Random.Range(120f, 240f));
                drops.Add(new Drop
                {
                    rect = image.rectTransform,
                    color = color,
                    start = start,
                    burst = burst,
                    control = control,
                    end = end,
                    delay = i * 0.04f,
                    fly = flyTime * Random.Range(0.85f, 1.15f),
                });
                image.rectTransform.anchoredPosition = start;
                image.rectTransform.localScale = Vector3.zero;
            }
        }

        void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f); // 맵 불러온 직후 시간 튐 방지 (MapIntroBanner와 같다)

            for (int i = drops.Count - 1; i >= 0; i--)
            {
                var drop = drops[i];
                drop.age += dt;
                float t = drop.age - drop.delay;
                if (t < 0f)
                    continue;

                if (t < burstTime)
                {
                    // 튀어 오른다: 처치 자리 → 흩어진 자리 (빠르게 시작해 천천히)
                    float k = Mathf.Sqrt(t / burstTime);
                    drop.rect.anchoredPosition = Vector2.Lerp(drop.start, drop.burst, k);
                    drop.rect.localScale = Vector3.one * Mathf.Lerp(0.3f, 1f, k);
                }
                else if (t < burstTime + drop.fly)
                {
                    // 가방으로 날아간다: 서서히 빨라지는 곡선 (2차 베지어)
                    float k = (t - burstTime) / drop.fly;
                    float e = k * k;
                    Vector2 a = Vector2.Lerp(drop.burst, drop.control, e);
                    Vector2 b = Vector2.Lerp(drop.control, drop.end, e);
                    drop.rect.anchoredPosition = Vector2.Lerp(a, b, e);
                    drop.rect.localScale = Vector3.one * Mathf.Lerp(1f, 0.55f, e);
                }
                else
                {
                    punchLeft[drop.color] = punchTime; // 도착: 아이콘이 통통 튄다
                    Destroy(drop.rect.gameObject);
                    drops.RemoveAt(i);
                }
            }

            for (int c = 0; c < punchLeft.Length; c++)
            {
                if (targets[c] == null)
                    continue;
                if (punchLeft[c] > 0f)
                {
                    punchLeft[c] = Mathf.Max(0f, punchLeft[c] - dt);
                    float k = 1f - punchLeft[c] / punchTime;
                    targets[c].localScale = Vector3.one * (1f + punchScale * Mathf.Sin(k * Mathf.PI));
                }
                else if (targets[c].localScale != Vector3.one)
                {
                    targets[c].localScale = Vector3.one;
                }
            }
        }
    }
}
