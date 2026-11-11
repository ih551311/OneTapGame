using LastBullet.UI;
using UnityEngine;

namespace LastBullet.Stage
{
    // 처음 들어왔을 때의 길 안내 (2026-11-11 사용자 요청: 튜토리얼 느낌으로 부모님 편지를 보고 → 할아버지 일기장 쪽으로 가라고 바닥에 표시).
    //  - 부모님의 편지(OpeningPopup)를 다 읽고 닫으면 시작한다: 플레이어 발밑에서 일기장 책상 앞까지 바닥에 호박색 화살표가 줄지어 책상 쪽으로 흘러가고,
    //    책상 앞 바닥에서는 둥근 고리가 숨 쉬듯 커졌다 작아진다. 화면 위에는 "바닥의 화살표를 따라가요" 안내 글이 뜬다.
    //  - 일기장 창(DiaryPopup)을 열면 "끝"으로 기억하고(PlayerPrefs) 모든 표시가 사라진다. 다시하기를 하면 다시 처음부터.
    //  - 다른 창이 열려 있는 동안(편지 읽는 중 등)에는 표시를 숨긴다.
    //  - 화살표·고리 그림은 Assets/UI/Guide_Arrow.png, Guide_Ring.png (머티리얼 Assets/Materials/FloorGuide_Arrow.mat, FloorGuide_Ring.mat)라서 바꿔 끼울 수 있다.
    // 붙일 곳: 미술관 TutorialGuide 오브젝트 (메뉴 LastBullet > Phase 99 첫 입장 길 안내 만들기)
    public class TutorialGuide : MonoBehaviour
    {
        const string DoneKey = "ReChroma.GuideDone";

        [Tooltip("가야 할 곳: 일기장 책상 (GrandpaDesk)")]
        [SerializeField] Transform desk;

        [Tooltip("책상 앞에서 서야 할 자리 (월드 좌표 기준으로 책상 위치에서 더하는 m, 책상 앞면이 월드 +X를 본다)")]
        [SerializeField] Vector3 standOffset = new Vector3(2.0f, 0f, 0f);

        [SerializeField] Material arrowMaterial;
        [SerializeField] Material ringMaterial;

        [Tooltip("일기장 창 (열면 안내가 끝난다)")]
        [SerializeField] PopupBase diaryPopup;

        [Tooltip("화면 위 안내 글 (Canvas/GuideHint)")]
        [SerializeField] GameObject hint;

        [Header("화살표 (기본값)")]
        [SerializeField] float spacing = 1.7f;
        [SerializeField] float arrowSize = 1.1f;
        [Tooltip("화살표가 책상 쪽으로 흘러가는 속도 (m/초)")]
        [SerializeField] float flowSpeed = 1.6f;
        [SerializeField] Color color = new Color(0.95f, 0.69f, 0.2f, 1f);

        Transform player;
        Transform[] arrows;
        MaterialPropertyBlock[] blocks;
        Transform ring;
        MaterialPropertyBlock ringBlock;
        const int MaxArrows = 16;

        public static bool Done
        {
            get
            {
                try { return PlayerPrefs.GetInt(DoneKey, 0) == 1; }
                catch { return true; } // 저장소를 못 읽으면 안내가 계속 뜨지 않게 끝난 것으로 친다
            }
        }

        // '끝' 기억을 지운다 (다시하기)
        public static void ForgetDone()
        {
            try
            {
                PlayerPrefs.DeleteKey(DoneKey);
                PlayerPrefs.Save();
            }
            catch { }
        }

        void Start()
        {
            var playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
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
            if (player == null || desk == null)
                return;
            if (Done)
            {
                SetShown(false);
                enabled = false;
                return;
            }
            // 일기장을 열면 끝
            if (diaryPopup != null && diaryPopup.IsOpen)
            {
                try
                {
                    PlayerPrefs.SetInt(DoneKey, 1);
                    PlayerPrefs.Save();
                }
                catch { }
                SetShown(false);
                enabled = false;
                return;
            }
            // 편지를 아직 안 읽었거나(곧 열린다), 다른 창이 열려 있는 동안에는 숨긴다
            if (!OpeningStory.Seen || PopupBase.AnyOpen)
            {
                SetShown(false);
                return;
            }
            SetShown(true);

            baseY = player.position.y; // 플레이어 발 높이
            // 책상 앞(전시장 쪽 = 월드 +X)에 선다. (책상 오브젝트는 3ds Max 축 변환으로 기울어 있어서 TransformPoint를 쓰면 방향이 틀어진다)
            Vector3 target = desk.position + standOffset;
            target.y = FloorY(target);
            Vector3 start = player.position;
            start.y = baseY;
            Vector3 flat = target - start;
            flat.y = 0f;
            float length = flat.magnitude;
            Vector3 dir = length > 0.01f ? flat / length : Vector3.right;

            // 화살표: 발밑 1.5m 앞에서 목표 1.2m 앞까지, 책상 쪽으로 흐른다
            float from = 1.5f, to = Mathf.Max(from, length - 1.2f);
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
                // 양 끝에서 서서히 나타나고 사라진다
                float fade = Mathf.Clamp01(Mathf.Min(d - from, to - d) / 1.0f + 0.15f);
                var c = color;
                c.a = 0.9f * fade;
                blocks[i].SetColor("_BaseColor", c);
                arrows[i].GetComponent<Renderer>().SetPropertyBlock(blocks[i]);
            }

            // 책상 앞 고리: 숨 쉬듯 커졌다 작아진다
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

        // 바닥 높이: 기준 높이(플레이어 발밑)보다 0.4m 위에서 아래로 레이를 쏜다. 카펫과 나무 바닥의 높이가 달라서이고,
        // 너무 높이에서 쏘면 탁자·의자 위에 화살표가 놓이므로 낮게 쏜다.
        float baseY;
        float FloorY(Vector3 position)
        {
            if (Physics.Raycast(new Vector3(position.x, baseY + 0.4f, position.z), Vector3.down, out var hit, 1.2f, ~0, QueryTriggerInteraction.Ignore)
                && !hit.collider.CompareTag("Player"))
                return hit.point.y;
            return baseY;
        }
    }
}
