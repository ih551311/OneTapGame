using UnityEngine;
using UnityEngine.InputSystem;

namespace LastBullet.Combat
{
    // 떨어진 탄환 근처에서 E키를 누르면 회수한다.
    // 붙일 곳: Prefabs/DroppedBullet (총이 탄환을 떨어뜨릴 때 Init으로 총을 알려 준다)
    public class BulletPickup : MonoBehaviour
    {
        [Tooltip("이 거리 안에서 E키로 회수할 수 있다 (기본값)")]
        [SerializeField] float pickupRange = 2f;

        OneBulletGun gun;

        public void Init(OneBulletGun owner) => gun = owner;

        // 플레이어가 회수할 수 있는 거리에 있는지
        public bool InRange => gun != null && Vector3.Distance(gun.transform.position, transform.position) <= pickupRange;

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.eKey.wasPressedThisFrame && InRange)
                gun.ReturnBullet("E키 회수");
        }
    }
}
