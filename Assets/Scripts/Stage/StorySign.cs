using System.Collections;
using LastBullet.UI;
using UnityEngine;

namespace LastBullet.Stage
{
    // 미술관 입구의 이야기 안내판 (2026-11-05 사용자 요청: 스토리 보기용). 가까이 가서 E를 누르면 이야기 팝업(StoryPopup)이 열린다 (InteractDevice와 같다).
    // 처음 미술관에 들어오면(이야기를 한 번도 본 적이 없으면) 잠깐 뒤 이야기가 저절로 한 번 열린다. 원하지 않으면 Open On First Visit을 끈다.
    // 붙일 곳: 미술관 StorySign 오브젝트 (popup, prompt 연결. 메뉴 LastBullet > Phase 74 이야기 안내판 만들기)
    public class StorySign : InteractDevice
    {
        [Tooltip("처음 들어오면 이야기가 저절로 한 번 열리게 한다")]
        [SerializeField] bool openOnFirstVisit = true;

        [Tooltip("처음 들어온 뒤 이야기가 저절로 열리기까지 기다리는 시간 (초)")]
        [SerializeField] float firstVisitDelay = 1.2f;

        protected override void Start()
        {
            base.Start();
            if (openOnFirstVisit && !StoryPopup.Seen)
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
