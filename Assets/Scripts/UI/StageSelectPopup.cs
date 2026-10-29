using LastBullet.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LastBullet.UI
{
    // 전투 장소 선택 팝업 (미술관의 지도 탁자에서 E로 연다).
    // 숲은 훈련장을 클리어하면 열린다 (기획서 2.6). 해금 저장은 Phase 5-4, 숲 씬은 Phase 10이라 지금은 잠겨 있다.
    // 붙일 곳: Canvas/StageSelectPopup (평소에는 꺼져 있다)
    public class StageSelectPopup : PopupBase
    {
        [SerializeField] Button trainingButton;
        [SerializeField] Button forestButton;

        protected override void OnFirstOpen()
        {
            trainingButton.onClick.AddListener(() => LoadStage(SceneNames.Training));
            forestButton.interactable = false; // 숲: 아직 잠김
        }

        void LoadStage(string sceneName)
        {
            Debug.Log($"[StageSelectPopup] {sceneName}(으)로 이동");
            SceneManager.LoadScene(sceneName);
        }
    }
}
