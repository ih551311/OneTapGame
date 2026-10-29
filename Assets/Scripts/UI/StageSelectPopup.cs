using LastBullet.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LastBullet.UI
{
    // 전투 장소 선택 팝업 (미술관의 지도 탁자에서 E로 연다).
    // 숲은 훈련장을 클리어하면 해금된다 (기획서 2.6, save.json에 저장). 숲 씬은 Phase 10에서 만들기 때문에
    // 해금되어도 지금은 버튼을 잠가 둔다 (해금 여부는 로그로 확인).
    // 붙일 곳: Canvas/StageSelectPopup (평소에는 꺼져 있다)
    public class StageSelectPopup : PopupBase
    {
        [SerializeField] Button trainingButton;
        [SerializeField] Button forestButton;

        protected override void OnFirstOpen()
        {
            trainingButton.onClick.AddListener(() => LoadStage(SceneNames.Training));
            forestButton.interactable = false; // 숲 씬이 생기면(Phase 10) SaveManager.IsStageUnlocked("Forest")로 바꾼다
            Debug.Log($"[StageSelectPopup] 숲 해금 여부: {SaveManager.IsStageUnlocked("Forest")}");
        }

        void LoadStage(string sceneName)
        {
            Debug.Log($"[StageSelectPopup] {sceneName}(으)로 이동");
            SceneManager.LoadScene(sceneName);
        }
    }
}
