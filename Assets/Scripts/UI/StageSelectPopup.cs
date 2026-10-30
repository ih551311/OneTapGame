using LastBullet.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LastBullet.UI
{
    // 전투 장소 선택 팝업 (미술관의 지도 탁자에서 E로 연다).
    // 전투 장소는 숲, 성 마을, 사막 셋이고 처음부터 모두 열려 있다 (스테이지 클리어·해금이 없다).
    // 맵마다 얻는 물감이 3색씩 정해져 있다(StageColors). 버튼 아래의 "얻는 물감 ● 빨강 ● 주황 ● 노랑" 글은
    // Assets/UI/StageColors_*.png 그림이다 (색을 바꾸면 그림도 같이 고친다).
    // 붙일 곳: Canvas/StageSelectPopup (평소에는 꺼져 있다)
    public class StageSelectPopup : PopupBase
    {
        [SerializeField] Button forestButton;
        [SerializeField] Button castleButton;
        [SerializeField] Button desertButton;

        protected override void OnFirstOpen()
        {
            Hook(forestButton, SceneNames.Forest);
            Hook(castleButton, SceneNames.CastleVillage);
            Hook(desertButton, SceneNames.Desert);
        }

        static void Hook(Button button, string sceneName)
        {
            if (button != null)
                button.onClick.AddListener(() => LoadStage(sceneName));
        }

        static void LoadStage(string sceneName)
        {
            Debug.Log($"[StageSelectPopup] {sceneName}(으)로 이동");
            SceneManager.LoadScene(sceneName);
        }
    }
}
