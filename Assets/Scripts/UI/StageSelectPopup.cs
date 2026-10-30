using LastBullet.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LastBullet.UI
{
    // 전투 장소 선택 팝업 (미술관의 지도 탁자에서 E로 연다).
    // 지금 전투 장소는 숲 하나다 (2026-10-30 사용자 결정: 훈련장 없이 바로 숲). 숲은 처음부터 열려 있다.
    // 붙일 곳: Canvas/StageSelectPopup (평소에는 꺼져 있다)
    public class StageSelectPopup : PopupBase
    {
        [SerializeField] Button forestButton;

        protected override void OnFirstOpen()
        {
            forestButton.onClick.AddListener(() => LoadStage(SceneNames.Forest));
        }

        void LoadStage(string sceneName)
        {
            Debug.Log($"[StageSelectPopup] {sceneName}(으)로 이동");
            SceneManager.LoadScene(sceneName);
        }
    }
}
