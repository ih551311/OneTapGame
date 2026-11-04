using System.Collections;
using LastBullet.UI;
using UnityEngine;

namespace LastBullet.Stage
{
    // 미술관 입구 안내판 (2026-11-05 사용자 요청). 가까이 가서 E를 누르면 안내 팝업(GuidePopup)이 열린다 (InteractDevice와 같다).
    // 처음 미술관에 들어오면(안내를 한 번도 본 적이 없으면) 잠깐 뒤 안내 팝업이 저절로 한 번 열린다.
    // 붙일 곳: 미술관 GuideSign 오브젝트 (popup, prompt 연결. 메뉴 LastBullet > Phase 72 입구 안내판 만들기)
    public class GuideSign : InteractDevice
    {
        [Tooltip("처음 들어온 뒤 안내가 저절로 열리기까지 기다리는 시간 (초)")]
        [SerializeField] float firstVisitDelay = 1.2f;

        protected override void Start()
        {
            base.Start();
            if (!GuidePopup.Seen)
                StartCoroutine(OpenFirstVisit());
        }

        IEnumerator OpenFirstVisit()
        {
            yield return new WaitForSecondsRealtime(firstVisitDelay);
            if (Popup != null && !Popup.IsOpen)
                Popup.Open();
        }
    }
}
