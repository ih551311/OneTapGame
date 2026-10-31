using LastBullet.Drawing;
using LastBullet.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastBullet.Stage
{
    // 미술관 액자 9개를 다룬다 (기획서 2.9, 3.4).
    //  - 가장 가까운 액자 하나에만 "[E] 그림 걸기" 안내를 띄우고, E를 누르면 그 액자의 작품 목록을 연다.
    //  - 화면에 "서로 다른 작품 N / 9"를 보여 준다 (같은 그림은 한 번만 센다).
    // 붙일 곳: PictureFrames (액자들의 부모 오브젝트)
    public class GalleryManager : MonoBehaviour
    {
        [Tooltip("9 frames (in slot number order)")]
        [SerializeField] PictureFrame[] frames;

        [Tooltip("Can be opened with E within this distance (default)")]
        [SerializeField] float range = 3f;

        [Tooltip("Artwork list popup (pick a picture to hang on a frame)")]
        [SerializeField] GalleryPopup picker;

        [Tooltip("Prompt UI shown when close (Canvas/Prompt_Frame)")]
        [SerializeField] GameObject prompt;

        [Tooltip("Progress number text (\"3 / 9\")")]
        [SerializeField] TMP_Text progressCount;

        Transform player;

        void Start()
        {
            var playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
            prompt.SetActive(false);
            GalleryStore.Changed += RefreshProgress;
            RefreshProgress();
        }

        void OnDestroy()
        {
            GalleryStore.Changed -= RefreshProgress;
        }

        void Update()
        {
            if (player == null)
                return;

            PictureFrame nearest = null;
            float best = range;
            foreach (var frame in frames)
            {
                Vector3 offset = player.position - frame.transform.position;
                offset.y = 0f;
                float distance = offset.magnitude;
                if (distance <= best)
                {
                    best = distance;
                    nearest = frame;
                }
            }

            bool show = nearest != null && !picker.IsOpen;
            prompt.SetActive(show);

            // 일시정지 중(Time.timeScale 0)에는 열리지 않는다
            if (show && Time.timeScale > 0f && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                picker.OpenForFrame(nearest);
        }

        void RefreshProgress()
        {
            progressCount.text = $"{GalleryStore.DistinctCount()} / {GalleryStore.SlotCount}";
        }
    }
}
