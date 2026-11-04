using LastBullet.Combat;
using UnityEngine;

namespace LastBullet.UI
{
    // 조준점 퍼짐 (2026-11-04, 데스티니식): 쏘면 조준점 원이 순간 커졌다가 돌아오고, 움직이면 조금 벌어진다.
    // 그림은 Assets/UI/Crosshair.png (작은 원 + 가운데 점)라서 바꿔 끼울 수 있다. 크기를 키울 뿐 그림을 늘리지는 않는다.
    // 붙일 곳: Canvas/Crosshair (메뉴 LastBullet > Phase 63 데스티니식 전투 화면 만들기)
    public class ReticleSpread : MonoBehaviour
    {
        [Tooltip("한 발 쏠 때 커지는 정도 (1 = 원래 크기의 2배)")]
        [SerializeField] float shotKick = 0.18f;

        [Tooltip("가장 커질 수 있는 정도")]
        [SerializeField] float maxSpread = 0.6f;

        [Tooltip("돌아오는 빠르기 (클수록 빨리)")]
        [SerializeField] float recover = 9f;

        [Tooltip("달릴 때 더 벌어지는 정도")]
        [SerializeField] float movingSpread = 0.12f;

        AutoGun gun;
        CharacterController controller;
        float spread;

        void Start()
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                gun = player.GetComponent<AutoGun>();
                controller = player.GetComponent<CharacterController>();
            }
            if (gun != null)
                gun.Fired += OnFired;
        }

        void OnDestroy()
        {
            if (gun != null)
                gun.Fired -= OnFired;
        }

        void OnFired()
        {
            spread = Mathf.Min(spread + shotKick, maxSpread);
        }

        void Update()
        {
            spread = Mathf.MoveTowards(spread, 0f, Time.unscaledDeltaTime * recover * Mathf.Max(0.2f, spread));
            float moving = 0f;
            if (controller != null)
            {
                var v = controller.velocity;
                v.y = 0f;
                moving = Mathf.Clamp01(v.magnitude / 7f) * movingSpread;
            }
            transform.localScale = Vector3.one * (1f + spread + moving);
        }
    }
}
