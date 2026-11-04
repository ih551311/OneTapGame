using System.Collections;
using LastBullet.Inventory;
using LastBullet.Stage;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LastBullet.UI
{
    // 거울 포털로 전투 장소에 들어갈 때의 색 번짐 전환 (2026-11-05 사용자 요청).
    //  - 가기 전(미술관): 화면 여기저기서 가려는 맵의 물감 3색 얼룩이 번져 나와 화면을 덮고, 다 덮이면 씬을 옮긴다.
    //  - 도착 후(전투 맵): 화면이 같은 색 얼룩으로 덮인 채 시작해서, 얼룩이 줄어들며 맵이 드러난다.
    //  - 얼룩 그림은 Assets/UI/PaintBlob.png (흰색 얼룩, 색은 코드가 입힌다)라서 바꿔 끼울 수 있다.
    //  - 시간이 멈춘 상태(Time.timeScale 0)에서도 돌아가도록 unscaled 시간을 쓴다.
    // 붙일 곳: Canvas/PaintTransition (화면 전체 크기, 자식 얼룩 Image 40개. 메뉴 LastBullet > Phase 70 색 번짐 전환 만들기)
    public class PaintTransition : MonoBehaviour
    {
        [SerializeField] Image[] blobs;

        [Tooltip("얼룩이 번지는 데 걸리는 시간 (초)")]
        [SerializeField] float spreadTime = 0.5f;

        [Tooltip("얼룩마다 시작이 늦어지는 최대 시간 (초). 클수록 하나씩 번지는 느낌")]
        [SerializeField] float stagger = 0.4f;

        // 다음 씬에서 화면이 얼룩으로 덮인 채 시작할지 (Load를 부르면 켜진다)
        static bool revealPending;
        static PaintTransition instance;

        CanvasGroup group;
        bool busy;

        void Awake()
        {
            instance = this;
            group = GetComponent<CanvasGroup>();
            if (group == null)
                group = gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            SetScale(0f); // 평소에는 얼룩이 없다 (오브젝트는 켜 둔다 — 꺼 두면 Awake가 돌지 않아 Load가 연출을 못 찾는다)
        }

        void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        void Start()
        {
            if (revealPending)
            {
                revealPending = false;
                Colorize(SceneManager.GetActiveScene().name);
                SetScale(1f);
                StartCoroutine(Reveal());
            }
        }

        // 씬을 옮기되 색 번짐 연출을 거친다. 이 씬에 연출 오브젝트가 없으면 그냥 옮긴다.
        public static void Load(string sceneName)
        {
            if (instance == null || instance.busy)
            {
                SceneManager.LoadScene(sceneName);
                return;
            }
            instance.StartCoroutine(instance.CoverThenLoad(sceneName));
        }

        IEnumerator CoverThenLoad(string sceneName)
        {
            busy = true;
            Colorize(sceneName);
            SetScale(0f);
            group.blocksRaycasts = true; // 번지는 동안 다른 버튼이 눌리지 않게
            yield return Animate(true);
            revealPending = true;
            SceneManager.LoadScene(sceneName);
        }

        IEnumerator Reveal()
        {
            group.blocksRaycasts = true;
            yield return Animate(false);
            group.blocksRaycasts = false;
            SetScale(0f);
        }

        // cover: 0 → 1로 번진다 / false: 1 → 0으로 줄어든다. 얼룩마다 시작이 조금씩 다르다.
        IEnumerator Animate(bool cover)
        {
            int count = blobs != null ? blobs.Length : 0;
            var delays = new float[count];
            for (int i = 0; i < count; i++)
                delays[i] = Random.value * stagger;
            float total = spreadTime + stagger;
            // 씬을 불러온 직후 프레임은 시간이 크게 튀므로 한 프레임에 0.05초까지만 센다 (연출이 순식간에 끝나지 않게)
            for (float t = 0f; t < total; t += Mathf.Min(Time.unscaledDeltaTime, 0.05f))
            {
                for (int i = 0; i < count; i++)
                {
                    float k = Mathf.Clamp01((t - delays[i]) / spreadTime);
                    float eased = 1f - (1f - k) * (1f - k); // 처음엔 빨리, 끝엔 천천히
                    blobs[i].rectTransform.localScale = Vector3.one * (cover ? eased : 1f - eased);
                }
                yield return null;
            }
            SetScale(cover ? 1f : 0f);
        }

        void SetScale(float scale)
        {
            if (blobs == null)
                return;
            foreach (var blob in blobs)
                if (blob != null)
                    blob.rectTransform.localScale = Vector3.one * scale;
        }

        // 그 맵의 물감 3색을 얼룩에 돌려가며 입힌다 (정해진 색이 없으면 9색)
        void Colorize(string sceneName)
        {
            if (blobs == null)
                return;
            var colors = StageColors.For(sceneName);
            for (int i = 0; i < blobs.Length; i++)
            {
                if (blobs[i] == null)
                    continue;
                PaintColor color = colors != null && colors.Count > 0 ? colors[i % colors.Count] : (PaintColor)(i % PaintColors.Count);
                blobs[i].color = PaintColors.Colors[(int)color];
            }
        }
    }
}
