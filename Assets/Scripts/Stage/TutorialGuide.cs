using System;
using LastBullet.Drawing;
using LastBullet.Inventory;
using LastBullet.UI;
using TMPro;
using UnityEngine;

namespace LastBullet.Stage
{
    // 미술관의 길 안내 (2026-11-11 사용자 요청: 튜토리얼 느낌으로 바닥에 표시, 2026-11-12: 편지를 우체통으로, 그 뒤 순서까지 늘림).
    // 단계를 차례로 안내한다. 각 단계: 바닥의 호박색 화살표가 플레이어 발밑에서 목적지 앞까지 흐르고, 목적지 앞 바닥에 고리가 숨 쉬듯 커졌다 작아지며,
    // 화면 위에 안내 글이 뜬다. 목적지의 창(popup)을 열면 그 단계가 끝나고 다음 단계로 간다.
    //   1 우체통(엄마의 편지) → 2 할아버지 일기장 → 3 지도 탁자(갈 곳 고르기) → 4 종이 합성대(종이조각 5개가 모이면)
    //   → 5 그림 작업대(종이가 생기면) → 6 액자(그림을 그렸으면)
    //  - 뒤 단계의 창을 먼저 열면 앞 단계는 끝난 것으로 친다. 이미 액자에 그림을 건 적이 있는 사람(익숙한 사람)에게는 안내하지 않는다.
    //  - 시작 조건(StartWhen)이 안 맞으면(예: 종이조각이 5개 미만) 안내를 쉬었다가 조건이 되면 다시 시작한다.
    //  - 다른 창이 열려 있는 동안에는 표시를 숨긴다. 진행은 PlayerPrefs에 기억하고, 다시하기를 하면 처음부터 다시 한다.
    //  - 화살표·고리 그림은 Assets/UI/Guide_Arrow.png, Guide_Ring.png (머티리얼 Assets/Materials/FloorGuide_Arrow.mat, FloorGuide_Ring.mat)라서 바꿔 끼울 수 있다.
    // 붙일 곳: 미술관 TutorialGuide 오브젝트 (메뉴 LastBullet > Phase 99 첫 입장 길 안내 만들기, 먼저 Phase 76·100)
    public class TutorialGuide : MonoBehaviour
    {
        public enum StartCondition { Always, HasScrapsForPaper, HasPaper, HasPainting }

        [Serializable]
        public class Step
        {
            [Tooltip("단계 이름 (기억하는 이름이라 바꾸면 처음부터 다시 안내한다)")]
            public string id;
            [Tooltip("이 창을 열면 단계가 끝난다")]
            public PopupBase popup;
            [Tooltip("서 있어야 할 자리 (월드 좌표, 바닥)")]
            public Vector3 stand;
            [Tooltip("화면 위 안내 글")]
            public string hint;
            [Tooltip("이 단계를 시작하는 조건")]
            public StartCondition startWhen;
        }

        const string KeyPrefix = "ReChroma.Guide.";
        const string LegacyLetterKey = "ReChroma.GuideLetterDone"; // 옛 방식(2단계 안내) 기억: 우체통 단계
        const string LegacyDoneKey = "ReChroma.GuideDone";         // 옛 방식: 일기장 단계

        [SerializeField] Step[] steps;

        [SerializeField] Material arrowMaterial;
        [SerializeField] Material ringMaterial;

        [Tooltip("화면 위 안내 판 (Canvas/GuideHint). 아래의 글자 칸을 단계마다 바꾼다")]
        [SerializeField] GameObject hint;

        [Header("화살표 (기본값)")]
        [SerializeField] float spacing = 1.7f;
        [SerializeField] float arrowSize = 1.1f;
        [Tooltip("화살표가 목적지 쪽으로 흐르는 속도 (m/초)")]
        [SerializeField] float flowSpeed = 1.6f;
        [SerializeField] Color color = new Color(0.95f, 0.69f, 0.2f, 1f);

        Transform player;
        Transform[] arrows;
        MaterialPropertyBlock[] blocks;
        Transform ring;
        MaterialPropertyBlock ringBlock;
        TMP_Text hintText;
        float baseY;
        float nextPaintingCheck;
        int paintingCount;
        const int MaxArrows = 16;

        static bool IsDone(string id)
        {
            try
            {
                if (PlayerPrefs.GetInt(KeyPrefix + id, 0) == 1)
                    return true;
                if (id == "mailbox")
                    return PlayerPrefs.GetInt(LegacyLetterKey, 0) == 1;
                if (id == "diary")
                    return PlayerPrefs.GetInt(LegacyDoneKey, 0) == 1;
                return false;
            }
            catch { return true; } // 저장소를 못 읽으면 안내가 계속 뜨지 않게 끝난 것으로 친다
        }

        static void SetDone(string id)
        {
            try
            {
                PlayerPrefs.SetInt(KeyPrefix + id, 1);
                PlayerPrefs.Save();
            }
            catch { }
        }

        // 안내가 모두 끝났는지 (설명용)
        public bool AllDone
        {
            get
            {
                if (steps == null)
                    return true;
                foreach (var step in steps)
                    if (!IsDone(step.id))
                        return false;
                return true;
            }
        }

        // 진행 기억을 지운다 (다시하기)
        public static void ForgetDone()
        {
            try
            {
                foreach (var id in new[] { "mailbox", "diary", "map", "craft", "paint", "frame" })
                    PlayerPrefs.DeleteKey(KeyPrefix + id);
                PlayerPrefs.DeleteKey(LegacyLetterKey);
                PlayerPrefs.DeleteKey(LegacyDoneKey);
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

            // 이미 액자에 그림을 걸어 본 사람은 게임을 아는 사람이라 안내하지 않는다
            if (steps != null && GalleryStore.DistinctCount() > 0)
                foreach (var step in steps)
                    SetDone(step.id);
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

        bool ConditionMet(StartCondition condition)
        {
            switch (condition)
            {
                case StartCondition.HasScrapsForPaper:
                    return InventoryManager.Instance.CanCraftPaper;
                case StartCondition.HasPaper:
                    return InventoryManager.Instance.DrawingPaper > 0;
                case StartCondition.HasPainting:
                    if (Time.unscaledTime >= nextPaintingCheck) // 파일을 읽으므로 1초에 한 번만
                    {
                        nextPaintingCheck = Time.unscaledTime + 1f;
                        paintingCount = PaintingLibrary.LoadList().Count;
                    }
                    return paintingCount > 0;
                default:
                    return true;
            }
        }

        void Update()
        {
            if (player == null || steps == null)
                return;

            // 지금 열린 창이 어느 단계의 창이면 그 단계(와 앞 단계들)를 끝낸다
            for (int i = steps.Length - 1; i >= 0; i--)
            {
                if (steps[i].popup != null && steps[i].popup.IsOpen)
                {
                    for (int j = 0; j <= i; j++)
                        SetDone(steps[j].id);
                    break;
                }
            }

            // 아직 안 끝난 첫 단계가 지금 안내할 단계
            Step current = null;
            foreach (var step in steps)
            {
                if (!IsDone(step.id))
                {
                    current = step;
                    break;
                }
            }
            if (current == null)
            {
                SetShown(false);
                enabled = false; // 모두 끝났다
                return;
            }
            // 창이 열려 있는 동안(편지 읽는 중 등)과 시작 조건이 안 맞을 때는 숨긴다
            if (PopupBase.AnyOpen || !ConditionMet(current.startWhen))
            {
                SetShown(false);
                return;
            }
            SetShown(true);
            if (hintText != null)
                hintText.text = current.hint;

            baseY = player.position.y; // 플레이어 발 높이
            Vector3 target = current.stand;
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
