using LastBullet.Combat;
using UnityEngine;

namespace LastBullet.Player
{
    // 팝업이나 그림판이 열려 있는 동안 플레이어 조작(이동·카메라·사격)을 막고 마우스 커서를 보여 준다 (기획서 2.2).
    // 붙일 곳: Player
    public class PlayerControlLock : MonoBehaviour
    {
        public bool IsLocked { get; private set; }

        public void SetLocked(bool locked)
        {
            IsLocked = locked;
            SetEnabled<PlayerMovement>(!locked);
            SetEnabled<ThirdPersonCamera>(!locked);
            SetEnabled<OneBulletGun>(!locked);
            Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = locked;
        }

        void SetEnabled<T>(bool enabled) where T : Behaviour
        {
            var component = GetComponent<T>();
            if (component != null)
                component.enabled = enabled;
        }
    }
}
