using LastBullet.UI;
using TMPro;
using UnityEngine;

namespace LastBullet.Stage
{
    // 처음 들어왔을 때의 길 안내 (2026-11-11 사용자 요청, 2026-11-12 사용자 요청으로 2단계로 고침: 시작 편지를 우체통으로 옮김).
    //  - 1단계: 시작하자마자 바닥의 호박색 화살표가 플레이어 발밑에서 우체통(엄마의 편지) 앞까지 이어지고, 우체통 앞 바닥에서는 고리가 숨 쉬듯 커졌다 작아진다.
    //    화면 위에는 "바닥의 화살표를 따라 우체통으로 가 보세요". 편지 창(엄마의 편지)을 열면 1단계가 끝난다.
    //  - 2단계: 편지를 닫으면 화살표가 일기장 책상 쪽으로 바뀐다. "이번에는 할아버지의 일기장으로 가 보세요". 일기장 창을 열면 안내가 모두 끝난다.
    //  - 일기장을 먼저 열면 바로 끝난다. 다른 창이 열려 있는 동안에는 표시를 숨긴다. 진행은 PlayerPrefs에 기억하고, 다시하기를 하면 처음부터 다시 한다.
    //  - 화살표·고리 그림은 Assets/UI/Guide_Arrow.png, Guide_Ring.png (머티리얼 Assets/Materials/FloorGuide_Arrow.mat, FloorGuide_Ring.mat)라서 바꿔 끼울 수 있다.
    // 붙일 곳: 미술관 TutorialGuide 오브젝트 (메뉴 LastBullet > Phase 99 첫 입장 길 안내 만들기, 먼저 Phase 76·100)
    public class TutorialGuide : MonoBehaviour
    {
        const string LetterKey = "ReChroma.GuideLetterDone";
        const string DoneKey = "ReChroma.GuideDone";

        [Tooltip("1단계 목적지: 우체통 (Mailbox)")]
        [SerializeField] Transform mailbox;

        [Tooltip("엄마의 편지 창 (열면 1단계가 끝난다)")]
        [SerializeField] PopupBase letterPopup;

        [Tooltip("2단계 목적지: 일기장 책상 (GrandpaDesk)")]
        [SerializeField] Transform desk;

        [Tooltip("일기장 창 (열면 안내가 모두 끝난다)")]
        [SerializeField] PopupBase diaryPopup;

        [Tooltip("책상 앞에서 서야 할 자리 (월드 좌표 기준으로 책상 위치에서 더하는 m, 책상 앞면이 월드 +X를 본다)")]
        [SerializeField] Vector3 standOffset = new Vector3(2.0f, 0f, 0f);

        [Tooltip("우체통 앞에서 서야 할 자리 (우체통 앞쪽으로 m)")]
        [SerializeField] float mailboxStand = 1.3f;

        [SerializeField] Material arrowMaterial;
        [SerializeField] Material ringMaterial;

        [Tooltip("화면 위 안내 판 (Canvas/GuideHint). 아래의 글자 칸을 단계마다 바꾼다")]
        [SerializeField] GameObject hint;

        [Header("화살표 (기본값)")]
        [SerializeField] float spacing = 1.7f;
        [SerializeField] float arrowSize = 1.1f;
        [Tooltip("화살표가 목적지 쪽으로 흘러가는 속도 (m/초)")]
        [SerializeField] float flowSpeed = 1.6f;
        [SerializeField] Color color = new Color(0.95f, 0.69f, 0.2f, 1f);

        Transform player;
        Transform[] arrows;
        MaterialPropertyBlock[] blocks;
        Transform ring;
        MaterialPropertyBlock ringBlock;
        TMP_Text hintText;
        float baseY;
        const int MaxArrows = 16;

        public static bool Done => GetFlag(DoneKey);
        public static bool LetterRead => GetFlag(LetterKey);

        static bool GetFlag(string key)
        {
            try { return PlayerPrefs.GetInt(key, 0) == 1; }
            catch { return true; } // 저장소를 못 읽으면 안내가 계속 뜨지 않게 끝난 것으로 친다
        }

        static void SetFlag(string key)
        {
            try
            {
                PlayerPrefs.SetInt(key, 1);
                PlayerPrefs.Save();
            }
            catch { }
        }

        // 진행 기억을 지운다 (다시하기)
        public static void ForgetDone()
        {
            try
            {
                PlayerPrefs.DeleteKey(DoneKey);
                PlayerPrefs.DeleteKey(LetterKey);
                PlayerPrefs.Save();
            }
            catch { }
        }

        void Start()
        {
            var playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
            if (hint != null)
                hintText = hint.GetComponentInChildren<TMP_Text>(true);
            arrows = new Transform[MaxArrows];
            blocks = new MaterialPropertyBlock[MaxArrows];
            for (int i = 0; i < MaxArrows; i++)
            {
                arrows[i] = MakeQuad("Arrow_" + i, arrowMaterial);
                blocks[i] = new MaterialPropertyBlock();
            }
            ring = MakeQuad("Ring", ringMaterial);
            ringBlock = new MaterialPropertyBlock();
            SetShown(false);
        }

        Transform MakeQuad(string name, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            go.SetActive(false);
            return go.transform;
        }

        void Update()
        {
            if (player == null || desk == null || mailbox == null)
                return;
            if (Done)
            {
                SetShown(false);
                enabled = false;
                return;
            }
            // 일기장을 열면 (어느 단계든) 모두 끝
            if (diaryPopup != null && diaryPopup.IsOpen)
            {
                SetFlag(LetterKey);
                SetFlag(DoneKey);
                SetShown(false);
                enabled = false;
                return;
            }
            // 편지 창을 열면 1단계 끝
            bool letterRead = LetterRead;
            if (!letterRead && letterPopup != null && letterPopup.IsOpen)
            {
                SetFlag(LetterKey);
                letterRead = true;
            }
            // 창이 열려 있는 동안(편지 읽는 중 등)에는 숨긴다
            if (PopupBase.AnyOpen)
            {
                SetShown(false);
                return;
            }
            SetShown(true);
            if (hintText != null)
                hintText.text = letterRead ? "이번에는 할아버지의 일기장으로 가 보세요" : "바닥의 화살표를 따라 우체통으로 가 보세요";

            baseY = player.position.y; // 플레이어 발 높이
            // 우체통 앞은 우체통이 보는 쪽, 책상 앞은 월드 +X (책상 오브젝트는 3ds Max 축 변환으로 기울어 있어 TransformPoint를 쓰면 방향이 틀어진다)
            Vector3 target = letterRead ? desk.position + standOffset : mailbox.position + mailbox.forward * mailboxStand;
            target.y = FloorY(target);
            Vector3 start = player.position;
            start.y = baseY;
            Vector3 flat = target - start;
            flat.y = 0f;
            float length = flat.magnitude;
            Vector3 dir = length > 0.01f ? flat / length : Vector3.right;

            // 화살표: 발밑 앞에서 목표 앞까지 목표 쪽으로 흐른다 (가까우면 짧게)
            float from = Mathf.Min(1.5f, length * 0.25f), to = Mathf.Max(from, length - 1.0f);
            float span = to - from;
            float phase = (Time.time * flowSpeed) % spacing;
            int count = span > 0.2f ? Mathf.Min(MaxArrows, Mathf.FloorToInt(span / spacing) + 1) : 0;
            for (int i = 0; i < MaxArrows; i++)
            {
                if (i >= count)
                {
                    arrows[i].gameObject.SetActive(false);
                    continue;
                }
                float d = from + i * spacing + phase;
                if (d > to)
                {
                    arrows[i].gameObject.SetActive(false);
                    continue;
                }
                Vector3 p = start + dir * d;
                p.y = FloorY(p) + 0.04f;
                arrows[i].gameObject.SetActive(true);
                arrows[i].SetPositionAndRotation(p, Quaternion.LookRotation(Vector3.down, dir));
                arrows[i].localScale = new Vector3(arrowSize, arrowSize, 1f);
                float fade = Mathf.Clamp01(Mathf.Min(d - from, to - d) / 1.0f + 0.15f); // 양 끝에서 서서히 나타나고 사라진다
                var c = color;
                c.a = 0.9f * fade;
                blocks[i].SetColor("_BaseColor", c);
                arrows[i].GetComponent<Renderer>().SetPropertyBlock(blocks[i]);
            }

            // 목적지 앞 고리: 숨 쉬듯 커졌다 작아진다
            ring.gameObject.SetActive(true);
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 3f);
            float ringSize = Mathf.Lerp(1.9f, 2.5f, pulse);
            ring.SetPositionAndRotation(target + Vector3.up * 0.05f, Quaternion.LookRotation(Vector3.down, Vector3.forward));
            ring.localScale = new Vector3(ringSize, ringSize, 1f);
            var rc = color;
            rc.a = Mathf.Lerp(0.55f, 0.95f, pulse);
            ringBlock.SetColor("_BaseColor", rc);
            ring.GetComponent<Renderer>().SetPropertyBlock(ringBlock);
        }

        void SetShown(bool shown)
        {
            if (hint != null && hint.activeSelf != shown)
                hint.SetActive(shown);
            if (!shown && arrows != null)
            {
                foreach (var a in arrows)
                    if (a != null && a.gameObject.activeSelf)
                        a.gameObject.SetActive(false);
                if (ring != null && ring.gameObject.activeSelf)
                    ring.gameObject.SetActive(false);
            }
        }

        // 바닥 높이: 플레이어 발 높이보다 0.4m 위에서 아래로 레이를 쏜다. 카펫과 나무 바닥의 높이가 달라서이고,
        // 너무 높이에서 쏘면 탁자·의자 위에 화살표가 놓이므로 낮게 쏜다.
        float FloorY(Vector3 position)
        {
            if (Physics.Raycast(new Vector3(position.x, baseY + 0.4f, position.z), Vector3.down, out var hit, 1.2f, ~0, QueryTriggerInteraction.Ignore)
                && !hit.collider.CompareTag("Player"))
                return hit.point.y;
            return baseY;
        }
    }
}
