using System.Collections;
using LastBullet.Drawing;
using LastBullet.Inventory;
using LastBullet.Stage;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LastBullet.UI
{
    // 전투 장소로 갈 때 뜨는 로딩 화면 (2026-11-06 사용자 요청: 물감 번짐 대신, 어떤 맵인지·나오는 물감 색을 작게 쓰고,
    // 내가 그린 그림 중 하나를 무작위로 보여 주고, 아재개그를 넣는다).
    //  - 맵 이름, 얻는 물감 3색(그림 아이콘 + 이름), 내 그림 한 장(작품이 없으면 빈 그림), 아재개그(질문 → 잠시 뒤 답), 불러오는 막대.
    //  - 로딩이 순식간에 끝나도 글을 읽을 수 있게 최소 시간(minTime)은 보여 준다. 씬은 비동기로 미리 불러 두었다가 시간이 되면 넘어간다.
    //  - 아재개그는 인스펙터의 Jokes 목록에서 고친다. 한 줄에 "질문|답" 형식.
    //  - 그림은 Assets/UI/Loading_*.png 라서 바꿔 끼울 수 있다.
    //  - 시간이 멈춘 상태(Time.timeScale 0)에서도 돌아가도록 unscaled 시간을 쓴다.
    // 붙일 곳: Canvas/LoadingScreen (평소에는 안 보인다. 메뉴 LastBullet > Phase 80 로딩 화면 만들기)
    public class LoadingScreen : MonoBehaviour
    {
        [Header("화면 요소")]
        [SerializeField] GameObject paintingFrame;
        [SerializeField] Image paintingImage;
        [SerializeField] Sprite emptyPainting;
        [SerializeField] TMP_Text mapName;
        [SerializeField] Image[] colorIcons;
        [SerializeField] TMP_Text[] colorNames;
        [SerializeField] TMP_Text jokeQuestion;
        [SerializeField] TMP_Text jokeAnswer;
        [SerializeField] Image barFill;
        [SerializeField] TMP_Text loadingText;

        [Tooltip("물감 9색 아이콘 (빨강 주황 노랑 초록 파랑 보라 핑크 하양 검정 순서)")]
        [SerializeField] Sprite[] paintIcons;

        // 2026-11-12 사용자 요청: 아재개그 대신 게임 팁과 할아버지의 메모를 번갈아 보여 준다. 한 줄에 "제목|글" (제목은 윗줄, 글은 아랫줄에 바로 나온다).
        // (칸 이름이 jokes인 것은 씬에 저장된 연결을 지키려고 그대로 둔 것이다.)
        [Header("로딩 문구 (한 줄에 제목|글)")]
        [SerializeField] string[] jokes =
        {
            "게임 팁|머리를 맞히면 피해 2배, 물감도 1.5배예요.",
            "게임 팁|Q로 구르는 동안은 잠깐 공격을 맞지 않아요.",
            "게임 팁|E 물감 폭탄의 물감 구역 안에서는 적이 느려지고 계속 피해를 입어요.",
            "게임 팁|바주카는 폭발 범위가 넓어요. 적이 모였을 때 쏘세요.",
            "게임 팁|죽으면 모은 재료를 잃어요. ESC에서 귀환하면 정산돼요.",
            "게임 팁|종이조각 5개로 그림 종이 1장을 만들 수 있어요.",
            "게임 팁|적을 잡으면 그 적 몸 색의 물감이 나와요.",
            "게임 팁|1, 2 키로 소총과 바주카를 바꿀 수 있어요. F로도 바꿔요.",
            "게임 팁|그림판에서 Ctrl+Z로 되돌리면 쓴 물감도 돌아와요.",
            "게임 팁|맵 한가운데 보스의 내려치기는 점프나 Q로 피할 수 있어요.",
            "할아버지의 메모|아침마다 액자의 먼지를 턴다. 아홉 개를 다 털면 한 시간이 간다.",
            "할아버지의 메모|비 오는 날엔 할머니가 오신다. 늘 아홉 번째 액자 앞에 서 계신다.",
            "할아버지의 메모|색은 처음 짜 놓았을 때가 제일 곱다.",
            "할아버지의 메모|창고 안쪽에 큰 거울이 있다. 낮에는 그냥 거울이다.",
        };

        [Header("시간 (기본값)")]
        [Tooltip("로딩 화면을 최소한 이만큼은 보여 준다 (초). 2026-11-12 사용자 요청으로 3.5 → 1.5")]
        [SerializeField] float minTime = 1.5f;

        [Tooltip("(안 씀) 예전 아재개그의 답이 나오던 때")]
        [SerializeField] float answerDelay = 0f;

        [Tooltip("화면이 서서히 나타나는 시간 (초)")]
        [SerializeField] float fadeTime = 0.2f;

        static LoadingScreen instance;

        CanvasGroup group;
        bool busy;
        Sprite shownSprite;

        void Awake()
        {
            instance = this;
            group = GetComponent<CanvasGroup>();
            if (group == null)
                group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false; // 평소에는 뒤의 버튼이 눌려야 한다 (오브젝트는 켜 둔다 — 꺼 두면 Awake가 돌지 않아 Load가 못 찾는다)
            group.interactable = false;
        }

        void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        // 씬을 옮기되 로딩 화면을 거친다. 이 씬에 로딩 화면이 없으면 예전 색 번짐 전환(없으면 바로 이동)으로 옮긴다.
        public static void Load(string sceneName)
        {
            if (instance == null)
            {
                PaintTransition.Load(sceneName);
                return;
            }
            if (instance.busy)
                return;
            instance.StartCoroutine(instance.Run(sceneName));
        }

        IEnumerator Run(string sceneName)
        {
            busy = true;
            Prepare(sceneName);
            group.blocksRaycasts = true;
            group.interactable = true;

            for (float t = 0f; t < fadeTime; t += Mathf.Min(Time.unscaledDeltaTime, 0.05f))
            {
                group.alpha = Mathf.Clamp01(t / fadeTime);
                yield return null;
            }
            group.alpha = 1f;

            // 화면이 다 나타난 뒤에 불러오기를 시작한다 (불러오는 동안 화면이 멈춰 보이지 않게 미리 비동기로)
            var operation = SceneManager.LoadSceneAsync(sceneName);
            operation.allowSceneActivation = false;

            float elapsed = 0f;
            float shown = 0f;
            bool answered = false;
            while (true)
            {
                elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                if (!answered && elapsed >= answerDelay)
                {
                    answered = true;
                    if (jokeAnswer != null)
                        jokeAnswer.gameObject.SetActive(true);
                }

                // 막대: 실제 불러온 정도와 지나간 시간 중 느린 쪽을 따라간다 (끝까지 차야 넘어간다)
                float loaded = Mathf.Clamp01(operation.progress / 0.9f);
                float timed = Mathf.Clamp01(elapsed / minTime);
                float target = Mathf.Min(loaded, timed);
                shown = Mathf.MoveTowards(shown, target, Time.unscaledDeltaTime * 1.5f);
                if (barFill != null)
                    barFill.fillAmount = shown;
                if (loadingText != null)
                    loadingText.text = shown >= 1f ? "준비 끝!" : "불러오는 중...";

                if (shown >= 1f && loaded >= 1f)
                    break;
                yield return null;
            }

            yield return new WaitForSecondsRealtime(0.05f);
            operation.allowSceneActivation = true;
        }

        // 맵 이름, 물감 3색, 내 그림, 아재개그를 채운다
        void Prepare(string sceneName)
        {
            if (mapName != null)
                mapName.text = StageNames.For(sceneName);

            var colors = StageColors.For(sceneName);
            for (int i = 0; colorIcons != null && i < colorIcons.Length; i++)
            {
                bool has = colors != null && i < colors.Count;
                colorIcons[i].gameObject.SetActive(has);
                if (colorNames != null && i < colorNames.Length)
                    colorNames[i].gameObject.SetActive(has);
                if (!has)
                    continue;
                int index = (int)colors[i];
                if (paintIcons != null && index < paintIcons.Length)
                    colorIcons[i].sprite = paintIcons[index];
                if (colorNames != null && i < colorNames.Length)
                    colorNames[i].text = PaintColors.KoreanNames[index];
            }

            ShowRandomPainting();

            if (jokes != null && jokes.Length > 0)
            {
                string[] parts = jokes[Random.Range(0, jokes.Length)].Split('|');
                if (jokeQuestion != null)
                    jokeQuestion.text = parts[0].Trim();                       // 제목 (게임 팁 / 할아버지의 메모)
                if (jokeAnswer != null)
                {
                    jokeAnswer.text = parts.Length > 1 ? parts[1].Trim() : ""; // 글은 바로 보인다
                    jokeAnswer.gameObject.SetActive(true);
                }
            }

            if (barFill != null)
                barFill.fillAmount = 0f;
            if (loadingText != null)
                loadingText.text = "불러오는 중...";
        }

        // 내가 그린 작품 중 하나를 무작위로 보여 준다 (없으면 빈 그림)
        void ShowRandomPainting()
        {
            if (paintingImage == null)
                return;
            var list = PaintingLibrary.LoadList();
            Texture2D texture = null;
            for (int tries = 0; tries < 5 && texture == null && list.Count > 0; tries++)
                texture = PaintingLibrary.LoadTexture(list[Random.Range(0, list.Count)]);

            if (shownSprite != null)
            {
                Destroy(shownSprite);
                shownSprite = null;
            }
            if (texture != null)
            {
                shownSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                paintingImage.sprite = shownSprite;
            }
            else
            {
                paintingImage.sprite = emptyPainting;
            }
            paintingImage.enabled = paintingImage.sprite != null;
        }
    }
}
