using LastBullet.Core;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.UI
{
    // 옵션 팝업: 소리 크기, 마우스 감도, 게임 나가기.
    // 값은 바꾸는 즉시 저장된다 (GameSettings, PlayerPrefs).
    // 붙일 곳: Canvas/OptionsPopup (평소에는 꺼져 있다)
    public class OptionsPopup : PopupBase
    {
        [Tooltip("소리 크기 0 ~ 1")]
        [SerializeField] Slider volumeSlider;

        [Tooltip("마우스 감도 배율 0.2 ~ 3")]
        [SerializeField] Slider sensitivitySlider;

        [SerializeField] Button quitButton;

        protected override void OnFirstOpen()
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            sensitivitySlider.minValue = GameSettings.MinSensitivity;
            sensitivitySlider.maxValue = GameSettings.MaxSensitivity;

            volumeSlider.onValueChanged.AddListener(v => GameSettings.Volume = v);
            sensitivitySlider.onValueChanged.AddListener(v => GameSettings.Sensitivity = v);
            quitButton.onClick.AddListener(Quit);
        }

        protected override void OnOpened()
        {
            // 저장된 값으로 슬라이더를 맞춘다 (값을 바꾼 알림은 보내지 않는다)
            volumeSlider.SetValueWithoutNotify(GameSettings.Volume);
            sensitivitySlider.SetValueWithoutNotify(GameSettings.Sensitivity);
        }

        static void Quit()
        {
            Debug.Log("[OptionsPopup] 게임 나가기");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false; // 에디터에서는 재생을 멈춘다
#else
            Application.Quit();
#endif
        }
    }
}
