using LastBullet.Core;
using LastBullet.Drawing;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.UI
{
    // 옵션 팝업: 닉네임, 소리 크기, 마우스 감도, 게임 나가기.
    // 값은 바꾸는 즉시 저장된다 (소리·감도는 GameSettings/PlayerPrefs, 닉네임은 ProfileStore/save.json).
    // 닉네임은 입력을 마치면(엔터·다른 곳 클릭) 적용되고, 창을 닫을 때도 적용된다. 최대 10글자, 비우면 기본 이름.
    // 붙일 곳: Canvas/OptionsPopup (평소에는 꺼져 있다)
    public class OptionsPopup : PopupBase
    {
        [Tooltip("Nickname input field (only in the title options. Without it the nickname feature is unused.)")]
        [SerializeField] TMP_InputField nicknameInput;

        [Tooltip("Volume 0 to 1")]
        [SerializeField] Slider volumeSlider;

        [Tooltip("Mouse sensitivity multiplier 0.2 to 3")]
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

            if (nicknameInput != null)
            {
                nicknameInput.characterLimit = ProfileStore.MaxNicknameLength;
                nicknameInput.onEndEdit.AddListener(ApplyNickname);
            }
        }

        protected override void OnOpened()
        {
            // 저장된 값으로 맞춘다 (값을 바꾼 알림은 보내지 않는다)
            volumeSlider.SetValueWithoutNotify(GameSettings.Volume);
            sensitivitySlider.SetValueWithoutNotify(GameSettings.Sensitivity);
            if (nicknameInput != null)
                nicknameInput.SetTextWithoutNotify(ProfileStore.Nickname);
        }

        // 입력 중에 창이 닫혀도 지금 쓴 이름을 적용한다
        void OnDisable()
        {
            if (nicknameInput != null && IsInitialized)
                ApplyNickname(nicknameInput.text);
        }

        void ApplyNickname(string text)
        {
            ProfileStore.Nickname = text;
            nicknameInput.SetTextWithoutNotify(ProfileStore.Nickname); // 다듬어진(공백 제거·기본 이름) 모양으로 보여 준다
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
