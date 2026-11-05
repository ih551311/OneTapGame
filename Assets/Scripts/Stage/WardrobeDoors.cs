using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastBullet.Stage
{
    // 미술관 벽 장롱: E를 누르면 두 문이 열리고 닫힌다. 열리면 안으로 들어갈 수 있다 (2026-11-06 사용자 요청).
    //  - 문이 닫혀 있는 동안은 앞을 막는 충돌 상자(frontBlock)가 켜져 있고, 열리면 꺼져서 안으로 걸어 들어갈 수 있다.
    //  - 문은 힌지(경첩) 오브젝트를 돌려서 연다. 모델의 문 구조: 장롱 아래 Left/Right(힌지) → Left Door/Right Door.
    //  - 안내 글자(prompt)는 장롱 여러 개가 같이 쓴다. 가장 가까운 한 곳만 글자를 켠다.
    // 붙일 곳: 미술관 장롱 오브젝트 (메뉴 LastBullet > Phase 77 장롱 문 열기)
    public class WardrobeDoors : MonoBehaviour
    {
        [Tooltip("왼쪽 문의 힌지 (열 때 +각도로 돈다)")]
        [SerializeField] Transform leftHinge;

        [Tooltip("오른쪽 문의 힌지 (열 때 -각도로 돈다)")]
        [SerializeField] Transform rightHinge;

        [Tooltip("문이 열리는 각도 (기본값 100도)")]
        [SerializeField] float openAngle = 100f;

        [Tooltip("문이 도는 속도 (초당 도, 기본값)")]
        [SerializeField] float turnSpeed = 200f;

        [Tooltip("장롱 앞에서 E를 누를 수 있는 곳 (장롱 문 앞 한가운데)")]
        [SerializeField] Transform interactPoint;

        [Tooltip("이 거리 안에서 E로 열고 닫는다 (기본값)")]
        [SerializeField] float range = 2.2f;

        [Tooltip("문이 닫혀 있을 때만 켜지는 앞 충돌 상자")]
        [SerializeField] Collider frontBlock;

        [Tooltip("안내 글자 (Canvas/Prompt_Wardrobe, 장롱끼리 같이 쓴다)")]
        [SerializeField] TMP_Text prompt;

        // 지금 안내 글자를 켜 둔 장롱 (여러 장롱이 서로 끄고 켜지 않게)
        static WardrobeDoors promptOwner;

        Transform player;
        bool open;
        float current; // 0(닫힘) ~ 1(열림)

        void Start()
        {
            var playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
            if (prompt != null && promptOwner == null)
                prompt.gameObject.SetActive(false);
            Apply();
        }

        void OnDisable()
        {
            if (promptOwner == this)
            {
                promptOwner = null;
                if (prompt != null)
                    prompt.gameObject.SetActive(false);
            }
        }

        void Update()
        {
            float target = open ? 1f : 0f;
            if (!Mathf.Approximately(current, target))
            {
                current = Mathf.MoveTowards(current, target, turnSpeed / Mathf.Max(1f, openAngle) * Time.deltaTime);
                Apply();
            }

            if (player == null || interactPoint == null)
                return;

            Vector3 offset = player.position - interactPoint.position;
            offset.y = 0f;
            bool near = offset.magnitude <= range;

            if (near)
            {
                promptOwner = this;
                if (prompt != null)
                {
                    prompt.gameObject.SetActive(true);
                    prompt.text = open ? "[E] 장롱 닫기" : "[E] 장롱 열기";
                }
                // 일시정지 중(Time.timeScale 0)에는 열리지 않게 한다
                if (Time.timeScale > 0f && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                {
                    // 문 앞(문턱)에 서 있으면 닫지 않는다 (닫히는 충돌 상자에 끼지 않게)
                    if (open && InDoorway())
                        return;
                    open = !open;
                }
            }
            else if (promptOwner == this)
            {
                promptOwner = null;
                if (prompt != null)
                    prompt.gameObject.SetActive(false);
            }
        }

        // 플레이어가 문 앞 충돌 상자 자리(문턱)에 겹쳐 있는지
        bool InDoorway()
        {
            if (frontBlock == null || player == null)
                return false;
            // 꺼진 충돌 상자는 bounds가 0이라, 상자 오브젝트 기준 좌표로 잰다 (앞뒤 0.6m 안 = 플레이어 반지름 0.5 + 상자 반두께, 좌우는 상자 폭 안).
            // 장롱 맨 안쪽(뒷판 쪽)에 서 있는 건 괜찮다: 그 상태로 닫으면 안에 갇히지만 안에서 E로 다시 열 수 있다.
            Vector3 local = frontBlock.transform.InverseTransformPoint(player.position);
            var box = frontBlock as BoxCollider;
            float halfWidth = box != null ? box.size.x * 0.5f : 1f;
            return Mathf.Abs(local.z) < 0.6f && Mathf.Abs(local.x) < halfWidth + 0.3f;
        }

        // 문 각도와 앞 충돌 상자를 현재 상태에 맞춘다
        void Apply()
        {
            // 부드럽게 (시작과 끝이 느리게)
            float eased = current * current * (3f - 2f * current);
            if (leftHinge != null)
                leftHinge.localRotation = Quaternion.Euler(0f, openAngle * eased, 0f);
            if (rightHinge != null)
                rightHinge.localRotation = Quaternion.Euler(0f, -openAngle * eased, 0f);
            // 문이 조금이라도 열리기 시작하면 앞을 터 준다 (닫힌 문을 통과해서 들어가지는 않는다)
            if (frontBlock != null)
                frontBlock.enabled = current < 0.35f;
        }
    }
}
