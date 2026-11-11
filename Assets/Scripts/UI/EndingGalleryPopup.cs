using System.Collections.Generic;
using LastBullet.Core;
using LastBullet.Drawing;
using LastBullet.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LastBullet.UI
{
    // 결말 뒤에 나오는 "내가 그린 그림들" 창 (2026-11-11 사용자 요청).
    //  - 결말 이야기(EndingPopup)의 마지막 쪽에서 [다음]을 누르면 열린다. 지금까지 그린 그림을 전부 한 화면에 보여 준다 (개수에 맞춰 칸 크기를 줄인다).
    //  - [다시하기]: 진행 상황(종이·물감·해금)과 그림(작품 파일·액자)을 모두 지우고 타이틀로 돌아간다. 되돌릴 수 없다.
    //    실수로 누르지 않게 두 번 눌러야 한다: 처음 누르면 버튼 글자가 "정말 지울까요?"로 바뀌고 4초 안에 한 번 더 누르면 지운다.
    //    버튼 아래에 "누르면 돌아갈 수 없습니다" 주의 글이 있다.
    //  - 닉네임과 타이틀에서 직접 그린 프로필 그림, 옵션 설정은 남는다.
    //  - 닫기(X)·ESC로 그냥 닫으면 아무것도 지워지지 않는다.
    // 붙일 곳: Canvas/EndingGalleryPopup (평소에는 꺼져 있다. 메뉴 LastBullet > Phase 95 결말 뒤 그림 모음 만들기)
    public class EndingGalleryPopup : PopupBase
    {
        [Tooltip("그림들이 놓이는 칸 (이 크기 안에 전부 들어가게 줄여 놓는다)")]
        [SerializeField] RectTransform grid;

        [Tooltip("그림이 한 장도 없을 때 보이는 글")]
        [SerializeField] TMP_Text emptyText;

        [SerializeField] Button restartButton;
        [SerializeField] TMP_Text restartLabel;

        [Tooltip("그림 사이 간격 (px)")]
        [SerializeField] float spacing = 14f;

        [Tooltip("그림 한 칸의 최대 크기 (px). 그림이 적어도 이보다 커지지 않는다")]
        [SerializeField] float maxCell = 260f;

        [Tooltip("처음 누른 뒤 한 번 더 누를 수 있는 시간 (초)")]
        [SerializeField] float confirmTime = 4f;

        const string RestartText = "다시하기";
        const string ConfirmText = "정말 지울까요?";

        readonly List<GameObject> cells = new List<GameObject>();
        float confirmUntil = -1f;

        protected override void OnFirstOpen()
        {
            if (restartButton != null)
                restartButton.onClick.AddListener(OnRestartClicked);
        }

        protected override void OnOpened()
        {
            confirmUntil = -1f;
            if (restartLabel != null)
                restartLabel.text = RestartText;
            Rebuild();
        }

        protected override void Update()
        {
            base.Update(); // ESC로 닫기
            if (confirmUntil > 0f && Time.unscaledTime > confirmUntil)
            {
                confirmUntil = -1f;
                if (restartLabel != null)
                    restartLabel.text = RestartText;
            }
        }

        // 그린 그림 전부를 칸에 맞춰 늘어놓는다
        void Rebuild()
        {
            foreach (var cell in cells)
                if (cell != null)
                    Destroy(cell);
            cells.Clear();
            if (grid == null)
                return;

            var paintings = PaintingLibrary.LoadList();
            if (emptyText != null)
                emptyText.gameObject.SetActive(paintings.Count == 0);
            if (paintings.Count == 0)
                return;

            // 전부 들어가는 가장 큰 정사각형 칸 크기와 줄 수를 찾는다
            float width = grid.rect.width, height = grid.rect.height;
            int n = paintings.Count, bestColumns = 1;
            float bestCell = 0f;
            for (int columns = 1; columns <= n; columns++)
            {
                int rows = Mathf.CeilToInt(n / (float)columns);
                float cell = Mathf.Min((width - spacing * (columns - 1)) / columns, (height - spacing * (rows - 1)) / rows);
                if (cell > bestCell)
                {
                    bestCell = cell;
                    bestColumns = columns;
                }
            }
            bestCell = Mathf.Min(bestCell, maxCell);
            int rowCount = Mathf.CeilToInt(n / (float)bestColumns);
            float totalHeight = rowCount * bestCell + (rowCount - 1) * spacing;

            for (int i = 0; i < n; i++)
            {
                int row = i / bestColumns, column = i % bestColumns;
                int inRow = Mathf.Min(bestColumns, n - row * bestColumns); // 마지막 줄은 가운데로 모은다
                float rowWidth = inRow * bestCell + (inRow - 1) * spacing;

                var go = new GameObject("Painting_" + i, typeof(RectTransform), typeof(RawImage));
                go.transform.SetParent(grid, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(bestCell, bestCell);
                rect.anchoredPosition = new Vector2(
                    -rowWidth * 0.5f + bestCell * 0.5f + column * (bestCell + spacing),
                    totalHeight * 0.5f - bestCell * 0.5f - row * (bestCell + spacing));
                var image = go.GetComponent<RawImage>();
                image.texture = PaintingLibrary.LoadTexture(paintings[i]);
                image.color = image.texture != null ? Color.white : new Color(1f, 1f, 1f, 0.15f);
                image.raycastTarget = false;
                cells.Add(go);
            }
        }

        void OnRestartClicked()
        {
            if (confirmUntil < 0f)
            {
                // 첫 번째 누름: 한 번 더 누르게 한다
                confirmUntil = Time.unscaledTime + confirmTime;
                if (restartLabel != null)
                    restartLabel.text = ConfirmText;
                return;
            }
            RestartGame();
        }

        // 진행 상황과 그림을 모두 지우고 타이틀로 돌아간다 (되돌릴 수 없다)
        public static void RestartGame()
        {
            PaintingLibrary.DeleteAll();
            GalleryStore.ClearAll();
            SaveManager.ResetProgress();
            if (InventoryManager.Exists)
                InventoryManager.Instance.ReloadFromSave();
            StoryPopup.ForgetSeen();
            LastBullet.Stage.TutorialGuide.ForgetDone(); // 우체통 → 일기장 길 안내를 다시 처음부터
            Debug.Log("[다시하기] 진행 상황과 그림을 처음으로 되돌렸어요. 타이틀로 돌아가요.");
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneManager.LoadScene(SceneNames.Title);
        }
    }
}
