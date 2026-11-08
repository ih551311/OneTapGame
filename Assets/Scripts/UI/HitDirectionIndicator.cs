using LastBullet.Player;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.UI
{
    // 맞은 방향 표시 (2026-11-08 사용자가 고른 추천 작업: 피격 연출). 맞으면 조준점 둘레에 붉은 호(弧)가 맞은 쪽에 나타났다가 사라진다.
    //  - 위쪽 = 카메라가 보는 쪽에서 맞음, 오른쪽 = 오른쪽에서, 아래쪽 = 등 뒤에서.
    //  - 근접 적은 때린 적의 위치, 원거리 돌은 던진 자리, 보스 내려치기는 보스 위치가 방향이 된다 (PlayerHealth.DamagedFrom).
    //  - 호 그림은 Assets/UI/HitDirection.png (위쪽을 향한 붉은 호, 정사각형 그림 한가운데가 조준점)라서 바꿔 끼울 수 있다.
    //  - 동시에 여러 방향에서 맞아도 보이게 호 이미지를 여러 개 돌려 쓴다.
    // 붙일 곳: 전투 맵 Canvas/HitDirection (자식 Arc_0 … 호 이미지, 메뉴 LastBullet > Phase 87 맞은 방향 표시 만들기)
    public class HitDirectionIndicator : MonoBehaviour
    {
        [SerializeField] Image[] arcs;

        [Tooltip("호가 사라지는 시간 (초)")]
        [SerializeField] float fadeTime = 1.1f;

        [Tooltip("호의 가장 진한 정도 (0~1)")]
        [Range(0f, 1f)]
        [SerializeField] float maxAlpha = 0.9f;

        PlayerHealth playerHealth;
        Transform player;
        float[] strength;
        float[] angle;

        void Start()
        {
            strength = new float[arcs != null ? arcs.Length : 0];
            angle = new float[strength.Length];
            var playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
                playerHealth = playerObject.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                    playerHealth.DamagedFrom += OnDamagedFrom;
            }
            Apply();
        }

        void OnDestroy()
        {
            if (playerHealth != null)
                playerHealth.DamagedFrom -= OnDamagedFrom;
        }

        // 카메라가 보는 방향 기준으로 어느 쪽에서 맞았는지 각도(도, 위 0 · 오른쪽 90)를 구해 빈(가장 약한) 호에 넣는다
        void OnDamagedFrom(Vector3 source)
        {
            if (player == null || strength.Length == 0)
                return;
            var cam = Camera.main;
            float lookYaw = cam != null ? cam.transform.eulerAngles.y : player.eulerAngles.y;
            Vector3 to = source - player.position;
            to.y = 0f;
            if (to.sqrMagnitude < 0.0001f)
                return;
            float worldYaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
            float relative = Mathf.DeltaAngle(lookYaw, worldYaw);

            // 같은 방향(30도 이내)의 호가 있으면 그것을 다시 진하게, 없으면 가장 약한 호를 쓴다
            int pick = -1;
            for (int i = 0; i < strength.Length; i++)
                if (strength[i] > 0f && Mathf.Abs(Mathf.DeltaAngle(angle[i], relative)) < 30f)
                    pick = i;
            if (pick < 0)
            {
                pick = 0;
                for (int i = 1; i < strength.Length; i++)
                    if (strength[i] < strength[pick])
                        pick = i;
            }
            angle[pick] = relative;
            strength[pick] = 1f;
            Apply();
        }

        void Update()
        {
            bool any = false;
            for (int i = 0; i < strength.Length; i++)
            {
                if (strength[i] <= 0f)
                    continue;
                strength[i] = Mathf.Max(0f, strength[i] - Time.unscaledDeltaTime / fadeTime);
                any = true;
            }
            if (any)
                Apply();
        }

        void Apply()
        {
            for (int i = 0; i < strength.Length; i++)
            {
                var image = arcs[i];
                if (image == null)
                    continue;
                var color = image.color;
                color.a = strength[i] * maxAlpha;
                image.color = color;
                image.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -angle[i]); // 화면의 오른쪽(시계 방향)이 +각도
            }
        }
    }
}
