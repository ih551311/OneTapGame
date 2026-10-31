using System.Collections.Generic;
using LastBullet.Core;
using LastBullet.Drawing;
using LastBullet.Stage;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.UI
{
    // 그림 보기 팝업 (타이틀의 "그림 보기" 버튼으로 연다).
    // 완성한 그림(작품)이 격자로 보이고, 많으면 위아래로 스크롤한다. 그림이 없으면 "아직 그림이 없어요".
    // 그림을 눌러 고른 뒤 "왼쪽에 걸기" / "오른쪽에 걸기"를 누르면 타이틀 화면 액자에 걸린다 (save.json에 저장).
    // 미술관에서는 보기 전용으로 쓴다 (그림판의 "내 그림" 버튼, viewOnly).
    // 붙일 곳: Canvas/GalleryPopup (평소에는 꺼져 있다)
    public class GalleryPopup : PopupBase
    {
        [Tooltip("Container for the picture cells (GridLayoutGroup)")]
        [SerializeField] RectTransform content;

        [Tooltip("Template for one picture cell (kept disabled). Button, child Picture (RawImage), Selected (selection mark)")]
        [SerializeField] GameObject itemTemplate;

        [Tooltip("Message shown when there are no pictures")]
        [SerializeField] GameObject emptyMessage;

        [SerializeField] Button hangLeftButton;
        [SerializeField] Button hangRightButton;

        [Tooltip("Title screen frames. They update as soon as a picture is hung.")]
        [SerializeField] TitleFrames titleFrames;

        [Tooltip("View-only mode (museum paint board \"My Pictures\"): hides the hang button and shows a picture enlarged when clicked")]
        [SerializeField] bool viewOnly;

        [Tooltip("Enlarged view window (view-only mode only. Disabled by default; clicking anywhere closes it)")]
        [SerializeField] GameObject largeView;

        [SerializeField] RawImage largePicture;

        [SerializeField] Button largeViewCloseArea;

        [Tooltip("Frame picker (E at a museum frame): clicking a picture hangs it on that frame and closes. Hides the hang button.")]
        [SerializeField] bool frameMode;

        [Header("Delete (user request 2026-10-31)")]
        [Tooltip("Button that deletes the selected picture. Not clickable without a selection. In view-only mode it is inside the enlarged view window. Unused in frame picker mode.")]
        [SerializeField] Button deleteButton;

        [Tooltip("\"이 그림을 지울까요?\" 확인창 (평소에는 꺼져 있다)")]
        [SerializeField] GameObject deleteConfirm;
        [SerializeField] Button deleteConfirmYesButton;
        [SerializeField] Button deleteConfirmNoButton;

        [Tooltip("Profile picture picker (title profile window): clicking a picture sets it as the profile picture and closes. Hides the hang and delete buttons.")]
        [SerializeField] bool profileMode;

        PictureFrame targetFrame;

        readonly List<GameObject> items = new List<GameObject>();
        string selectedId;

        // 액자 앞에서 부른다. 어느 액자에 걸지 기억해 두고 연다.
        public void OpenForFrame(PictureFrame frame)
        {
            targetFrame = frame;
            Open();
        }

        protected override void OnFirstOpen()
        {
            if (!frameMode && !profileMode && deleteButton != null)
            {
                deleteButton.onClick.AddListener(AskDelete);
                deleteConfirmYesButton.onClick.AddListener(ConfirmDelete);
                deleteConfirmNoButton.onClick.AddListener(() => deleteConfirm.SetActive(false));
            }
            if (frameMode || profileMode)
            {
                hangLeftButton.gameObject.SetActive(false);
                hangRightButton.gameObject.SetActive(false);
                return;
            }
            if (viewOnly)
            {
                hangLeftButton.gameObject.SetActive(false);
                hangRightButton.gameObject.SetActive(false);
                largeViewCloseArea.onClick.AddListener(() => largeView.SetActive(false));
                return;
            }
            hangLeftButton.onClick.AddListener(() => Hang(0));
            hangRightButton.onClick.AddListener(() => Hang(1));
        }

        protected override void OnOpened()
        {
            selectedId = frameMode && targetFrame != null ? targetFrame.CurrentId : null; // 지금 걸린 그림에 표시
            if (profileMode && ProfileStore.PictureId != ProfileStore.DrawnId)
                selectedId = string.IsNullOrEmpty(ProfileStore.PictureId) ? null : ProfileStore.PictureId; // 지금 프로필 사진에 표시
            if (largeView != null)
                largeView.SetActive(false);
            if (deleteConfirm != null)
                deleteConfirm.SetActive(false);
            Refresh();
        }

        // 확인창이 떠 있으면 ESC는 확인창만, 크게 보고 있으면 크게 보기만 닫는다
        protected override void RequestClose()
        {
            if (deleteConfirm != null && deleteConfirm.activeSelf)
                deleteConfirm.SetActive(false);
            else if (largeView != null && largeView.activeSelf)
                largeView.SetActive(false);
            else
                Close();
        }

        void Refresh()
        {
            itemTemplate.SetActive(false);
            foreach (var item in items)
                Destroy(item);
            items.Clear();

            var paintings = PaintingLibrary.LoadList();
            foreach (var info in paintings)
            {
                var item = Instantiate(itemTemplate, content);
                item.name = $"Item_{info.id}";
                item.transform.Find("Picture").GetComponent<RawImage>().texture = PaintingLibrary.LoadTexture(info);
                item.transform.Find("Selected").gameObject.SetActive(info.id == selectedId);
                string id = info.id;
                item.GetComponent<Button>().onClick.AddListener(() => Select(id));
                item.SetActive(true);
                items.Add(item);
            }
            emptyMessage.SetActive(paintings.Count == 0);
            hangLeftButton.interactable = hangRightButton.interactable = selectedId != null;
            if (deleteButton != null && !viewOnly)
                deleteButton.interactable = selectedId != null;
        }

        void AskDelete()
        {
            if (selectedId != null)
                deleteConfirm.SetActive(true);
        }

        // 확인창에서 [지우기]: 작품을 지우고 목록을 다시 그린다. 타이틀 액자도 같이 갱신한다.
        void ConfirmDelete()
        {
            deleteConfirm.SetActive(false);
            if (selectedId == null)
                return;
            PaintingLibrary.Delete(selectedId);
            selectedId = null;
            if (largeView != null)
                largeView.SetActive(false);
            Refresh();
            if (titleFrames != null)
                titleFrames.Refresh();
        }

        void Select(string id)
        {
            if (profileMode)
            {
                ProfileStore.SetPainting(id);
                Close();
                return;
            }
            if (frameMode)
            {
                if (targetFrame != null)
                    targetFrame.Hang(id);
                Close();
                return;
            }
            if (viewOnly)
            {
                selectedId = id; // 크게 보기 안의 [삭제]가 이 그림을 지운다
                largePicture.texture = PaintingLibrary.LoadTexture(PaintingLibrary.Find(id));
                largeView.SetActive(true);
                return;
            }
            selectedId = id;
            foreach (var item in items)
                item.transform.Find("Selected").gameObject.SetActive(item.name == $"Item_{id}");
            hangLeftButton.interactable = hangRightButton.interactable = true;
            if (deleteButton != null)
                deleteButton.interactable = true;
        }

        void Hang(int frameIndex)
        {
            if (selectedId == null)
                return;
            SaveManager.SetTitleFrame(frameIndex, selectedId);
            Debug.Log($"[GalleryPopup] {(frameIndex == 0 ? "왼쪽" : "오른쪽")} 액자에 {selectedId} 걸기");
            if (titleFrames != null)
                titleFrames.Refresh();
        }
    }
}
