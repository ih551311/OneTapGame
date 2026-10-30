using LastBullet.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LastBullet.UI
{
    // 전투 장소 선택 팝업 (미술관의 지도 탁자에서 E로 연다).
    // 전투 장소는 숲과 성 마을 둘이고, 둘 다 처음부터 열려 있다 (스테이지 클리어·해금이 없다).
    //   숲: 지역 구분 없이 9색 중 랜덤 물감 / 성 마을: 마을·성 앞뜰·성 안 세 지역이 물감 3색씩 (PaintZone)
    // 붙일 곳: Canvas/StageSelectPopup (평소에는 꺼져 있다)
    public class StageSelectPopup : PopupBase
    {
        [SerializeField] Button forestButton;
        [SerializeField] Button castleButton;

        protected override void OnFirstOpen()
        {
            forestButton.onClick.AddListener(() => LoadStage(SceneNames.Forest));
            if (castleButton != null)
                castleButton.onClick.AddListener(() => LoadStage(SceneNames.CastleVillage));
        }

        void LoadStage(string sceneName)
        {
            Debug.Log($"[StageSelectPopup] {sceneName}(으)로 이동");
            SceneManager.LoadScene(sceneName);
        }
    }
}
