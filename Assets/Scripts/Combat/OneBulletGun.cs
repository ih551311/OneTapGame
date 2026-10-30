using LastBullet.Audio;
using LastBullet.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastBullet.Combat
{
    // 기획서 2.4의 탄환 상태
    public enum BulletState { Loaded, Fired, Dropped }

    // 한 발 탄환 총. 좌클릭으로 발사한다.
    // 1) 화면 가운데(카메라)에서 레이를 쏴서 조준점을 구하고
    // 2) 총구에서 그 조준점으로 다시 레이를 쏴서 실제로 맞은 곳을 정한다.
    //    (카메라는 어깨 뒤에 있어서, 총구와 조준점 사이에 벽이 있으면 벽에 맞아야 한다)
    // 붙일 곳: Player
    public class OneBulletGun : MonoBehaviour
    {
        [Tooltip("총알이 나가는 위치. Player 아래의 Muzzle")]
        [SerializeField] Transform muzzle;

        [Tooltip("발사 순간 보이는 탄도선")]
        [SerializeField] LineRenderer tracer;

        [Tooltip("맞은 자리에 잠깐 보이는 점. Prefabs/ImpactMarker")]
        [SerializeField] GameObject impactMarkerPrefab;

        [Tooltip("빗나갔을 때 바닥에 떨어지는 탄환. Prefabs/DroppedBullet")]
        [SerializeField] GameObject droppedBulletPrefab;

        [Tooltip("최대 사거리 (기본값)")]
        [SerializeField] float maxRange = 50f;

        [Tooltip("발사 소리 (핸드건). 없으면 소리 없음")]
        [SerializeField] SfxPlayer shotSound;

        [SerializeField] float tracerTime = 0.08f;

        [Tooltip("맞은 자리 노란 점이 남아 있는 시간 (초)")]
        [SerializeField] float impactMarkerTime = 0.4f;

        public BulletState State { get; private set; } = BulletState.Loaded;

        // 총을 쏜 순간 (캐릭터의 사격 동작 PlayerAnimator가 듣는다)
        public event System.Action Fired;

        // 지금 떨어져 있는 탄환. 회수(Phase 3-3)할 때 쓴다.
        public GameObject DroppedBullet { get; private set; }

        Camera cam;
        ThirdPersonCamera look;
        readonly RaycastHit[] hits = new RaycastHit[16];
        float tracerEndTime;

        void Awake()
        {
            cam = Camera.main;
            look = GetComponent<ThirdPersonCamera>();
            tracer.enabled = false;
        }

        void Update()
        {
            if (tracer.enabled && Time.time >= tracerEndTime)
                tracer.enabled = false;

            // 마우스 커서가 잠겨 있을 때만 쏜다. 커서가 풀린 상태의 클릭은 커서를 다시 잠그는 데 쓴다.
            var mouse = Mouse.current;
            if (mouse == null || Cursor.lockState != CursorLockMode.Locked)
                return;

            if (mouse.leftButton.wasPressedThisFrame && State == BulletState.Loaded)
                Fire();
        }

        void Fire()
        {
            State = BulletState.Fired; // 같은 순간에 두 번 쏘지 않게
            if (shotSound != null)
                shotSound.Play();
            Fired?.Invoke();

            // 몸을 카메라가 보는 방향으로 돌려서 총구가 조준 방향을 보게 한다.
            if (look != null)
                transform.rotation = Quaternion.Euler(0f, look.Yaw, 0f);

            // 1) 카메라 → 조준점
            Ray cameraRay = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Vector3 aimPoint = RaycastIgnoringSelf(cameraRay.origin, cameraRay.direction, maxRange, out var cameraHit)
                ? cameraHit.point
                : cameraRay.origin + cameraRay.direction * maxRange;

            // 2) 총구 → 조준점
            Vector3 origin = muzzle.position;
            Vector3 toAim = aimPoint - origin;
            bool hit = RaycastIgnoringSelf(origin, toAim.normalized, toAim.magnitude + 0.01f, out var muzzleHit);
            Vector3 impact = hit ? muzzleHit.point : aimPoint;

            ShowTracer(origin, impact);
            if (hit)
                SpawnImpactMarker(impact);

            // 헤드샷 원탭 (2026-10-30 변경): 머리에 맞아야만 처치되고 탄환을 바로 다시 쓸 수 있다.
            var hitbox = hit ? muzzleHit.collider.GetComponent<Hitbox>() : null;
            var result = hitbox != null ? hitbox.TakeHit(origin) : HitResult.Hurt; // 머리면 처치, 몸이면 깜빡임·멈칫만, 방패면 막힘

            if (hitbox != null && result == HitResult.Killed)
            {
                Debug.Log($"[OneBulletGun] 헤드샷: {hitbox.transform.parent.name}");
                State = BulletState.Loaded;
                return;
            }

            // 방어형의 방패에 막힌 탄환은 "명중으로 처리": 탄환이 떨어지지 않고 바로 다시 쓸 수 있다 (기획서 2.5)
            if (hitbox != null && result == HitResult.Blocked)
            {
                Debug.Log($"[OneBulletGun] 방패에 막힘: {hitbox.transform.parent.name} → 탄환 즉시 재사용");
                State = BulletState.Loaded;
                return;
            }

            // 몸샷이나 빗나가면 탄환이 떨어지고, 회수할 때까지 쏠 수 없다.
            // 벽·적의 몸에 맞으면 그 앞 바닥에, 아무것도 안 맞으면 최대 사거리 지점 아래 바닥에 떨어진다.
            Vector3 dropFrom = hit ? impact + muzzleHit.normal * 0.2f : impact;
            DropBullet(dropFrom);
            string what = hitbox != null ? "몸샷" : hit ? $"빗나감 ({muzzleHit.collider.name})" : "빗나감 (허공)";
            Debug.Log($"[OneBulletGun] {what} → 탄환이 떨어짐");
            State = BulletState.Dropped;
        }

        void DropBullet(Vector3 from)
        {
            // 아래로 레이를 쏴서 바닥을 찾는다. 바닥이 없으면(맵 밖) 그 자리에 둔다. 7초 뒤 자동 복귀(Phase 3-3).
            Vector3 position = RaycastIgnoringSelf(from + Vector3.up * 0.1f, Vector3.down, 200f, out var groundHit)
                ? groundHit.point + Vector3.up * 0.05f
                : from;
            DroppedBullet = Instantiate(droppedBulletPrefab, position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            DroppedBullet.name = "DroppedBullet";
            DroppedBullet.GetComponent<BulletPickup>().Init(this);
            DroppedBullet.GetComponent<BulletAutoReturn>().Init(this);
        }

        // 떨어진 탄환을 돌려받는다. E키 회수(BulletPickup)와 7초 자동 복귀(BulletAutoReturn)가 부른다.
        public void ReturnBullet(string reason)
        {
            if (State != BulletState.Dropped)
                return;
            if (DroppedBullet != null)
                Destroy(DroppedBullet);
            DroppedBullet = null;
            State = BulletState.Loaded;
            Debug.Log($"[OneBulletGun] 탄환 돌아옴: {reason}");
        }

        // 레이에 걸린 것 중 플레이어 자신을 빼고 가장 가까운 것을 찾는다.
        bool RaycastIgnoringSelf(Vector3 origin, Vector3 direction, float distance, out RaycastHit closest)
        {
            int count = Physics.RaycastNonAlloc(origin, direction, hits, distance, ~0, QueryTriggerInteraction.Ignore);
            closest = default;
            float best = float.MaxValue;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                if (hits[i].collider.transform.IsChildOf(transform))
                    continue;
                if (hits[i].distance < best)
                {
                    best = hits[i].distance;
                    closest = hits[i];
                    found = true;
                }
            }
            return found;
        }

        void ShowTracer(Vector3 from, Vector3 to)
        {
            tracer.SetPosition(0, from);
            tracer.SetPosition(1, to);
            tracer.enabled = true;
            tracerEndTime = Time.time + tracerTime;
        }

        void SpawnImpactMarker(Vector3 point)
        {
            if (impactMarkerPrefab == null)
                return;
            var marker = Instantiate(impactMarkerPrefab, point, Quaternion.identity);
            marker.name = "ImpactMarker";
            Destroy(marker, impactMarkerTime);
        }
    }
}
