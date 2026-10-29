using UnityEngine;

namespace LastBullet.Core
{
    // 옵션 설정 (소리 크기, 마우스 감도). 게임 진행(save.json)과 달리 이 컴퓨터의 설정이라 PlayerPrefs에 저장한다.
    // 소리 크기는 게임 전체 소리(AudioListener.volume)에 적용된다. 효과음·배경음은 Phase 11에서 넣는다.
    public static class GameSettings
    {
        const string VolumeKey = "MasterVolume";
        const string SensitivityKey = "MouseSensitivity";

        public const float MinSensitivity = 0.2f;
        public const float MaxSensitivity = 3f;

        // 0 ~ 1 (기본 1)
        public static float Volume
        {
            get => PlayerPrefs.GetFloat(VolumeKey, 1f);
            set
            {
                float v = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(VolumeKey, v);
                PlayerPrefs.Save();
                AudioListener.volume = v;
            }
        }

        // 기본 감도에 곱하는 배율, 0.2 ~ 3 (기본 1)
        public static float Sensitivity
        {
            get => PlayerPrefs.GetFloat(SensitivityKey, 1f);
            set
            {
                PlayerPrefs.SetFloat(SensitivityKey, Mathf.Clamp(value, MinSensitivity, MaxSensitivity));
                PlayerPrefs.Save();
            }
        }

        // 게임이 켜질 때 저장된 소리 크기를 적용한다
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void ApplyOnStart()
        {
            AudioListener.volume = Volume;
        }
    }
}
