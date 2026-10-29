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

        [Tooltip("맞은 자리에 잠깐 보이는 점의 머티리얼")]
        [SerializeField] Material impactMaterial;

        [Tooltip("최대 사거리 (기본값)")]
        [SerializeField] float maxRange = 50f;

        [SerializeField] float tracerTime = 0.08f;

        public BulletState State { get; private set; } = BulletState.Loaded;

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
            {
                SpawnImpactMarker(impact);
                Debug.Log($"[OneBulletGun] 명중: {muzzleHit.collider.name} ({Vector3.Distance(origin, impact):0.0}m)");
            }
            else
            {
                Debug.Log("[OneBulletGun] 아무것도 맞지 않음");
            }

            // Phase 3-1에서는 쏜 뒤 바로 다시 장전한다.
            // Phase 3-2에서 바꿀 부분: 적에게 맞으면 Loaded, 빗나가면 Dropped (탄환 오브젝트 생성).
            State = BulletState.Loaded;
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
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(marker.GetComponent<Collider>());
            marker.name = "ImpactMarker";
            marker.transform.position = point;
            marker.transform.localScale = Vector3.one * 0.15f;
            if (impactMaterial != null)
                marker.GetComponent<Renderer>().sharedMaterial = impactMaterial;
            Destroy(marker, 2f);
        }
    }
}
