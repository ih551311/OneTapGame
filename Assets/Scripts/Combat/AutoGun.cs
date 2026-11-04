using System;
using LastBullet.Audio;
using LastBullet.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastBullet.Combat
{
    // 연사 총 (2026-11-04 사용자 결정: 전투를 데스티니식으로 바꾼다 — 한 발 탄환(OneBulletGun)을 없애고 이 총으로 교체).
    //  - 탄은 무한이고, 탄창(magazineSize발)을 다 쏘면 재장전한다. 좌클릭을 누르고 있으면 연사, R키로 미리 재장전, 탄창이 비면 저절로 재장전한다.
    //  - 쏘는 방식은 예전과 같다: 화면 가운데(카메라)에서 레이를 쏴 조준점을 구하고, 총구에서 그 점으로 다시 쏴서 실제로 맞은 곳을 정한다.
    //  - 맞은 적의 처치는 지금은 Hitbox·EnemyHealth가 정한다 (적 HP·크리티컬은 다음 단계에서 넣는다).
    //  - 재장전 소리: 탄창 빼기 → 탄창 넣기 → 장전 (Handgun/Reload 소리, 시점은 reloadClipTimes).
    // 화면의 탄약 표시(AmmoDisplay), 1인칭 총(FirstPersonGun), 캐릭터 동작(PlayerAnimator), 카메라 반동(ThirdPersonCamera)이 이 총의 이벤트를 듣는다.
    // 붙일 곳: Player (메뉴 LastBullet > Phase 59 연사 총으로 바꾸기)
    public class AutoGun : MonoBehaviour
    {
        [Header("연결")]
        [Tooltip("총알이 나가는 위치. Player 아래의 Muzzle")]
        [SerializeField] Transform muzzle;

        [Tooltip("발사 순간 보이는 탄도선")]
        [SerializeField] LineRenderer tracer;

        [Tooltip("맞은 자리에 잠깐 보이는 점. Prefabs/ImpactMarker")]
        [SerializeField] GameObject impactMarkerPrefab;

        [Tooltip("발사 소리. 없으면 소리 없음")]
        [SerializeField] SfxPlayer shotSound;

        [Header("총 성능 (기본값)")]
        [Tooltip("탄창에 든 탄 수")]
        [SerializeField] int magazineSize = 30;

        [Tooltip("한 발 쏘고 다음 발까지 걸리는 시간 (초). 0.11이면 초당 약 9발")]
        [SerializeField] float fireInterval = 0.11f;

        [Tooltip("한 발의 피해 (몸에 맞았을 때, 기본값). 적 체력은 돌진형 60 · 원거리형 40 · 분열 적 140 · 작은 분열체 30")]
        [SerializeField] float damage = 10f;

        [Tooltip("머리에 맞으면(크리티컬) 피해가 몇 배가 되는지 (기본값)")]
        [SerializeField] float headshotMultiplier = 2f;

        [Tooltip("재장전에 걸리는 시간 (초)")]
        [SerializeField] float reloadTime = 1.6f;

        [Tooltip("최대 사거리 (m)")]
        [SerializeField] float maxRange = 80f;

        [Tooltip("탄도선이 보이는 시간 (초). 연사라서 짧게")]
        [SerializeField] float tracerTime = 0.04f;

        [Tooltip("맞은 자리 점이 남아 있는 시간 (초)")]
        [SerializeField] float impactMarkerTime = 0.25f;

        [Header("재장전 소리")]
        [Tooltip("재장전 소리를 낼 AudioSource (2D)")]
        [SerializeField] AudioSource reloadAudio;

        [Tooltip("재장전 소리들 (탄창 빼기, 탄창 넣기, 장전 순서)")]
        [SerializeField] AudioClip[] reloadClips;

        [Tooltip("각 소리가 나는 때 (재장전 진행도 0~1)")]
        [SerializeField] float[] reloadClipTimes = { 0.05f, 0.55f, 0.85f };

        [Range(0f, 1f)]
        [SerializeField] float reloadVolume = 0.5f;

        public int Ammo { get; private set; }
        public int MagazineSize => magazineSize;
        public bool IsReloading { get; private set; }

        // 재장전 진행도 0~1 (재장전 중이 아니면 0)
        public float ReloadProgress => IsReloading ? Mathf.Clamp01((Time.time - reloadStartTime) / reloadTime) : 0f;

        // 총을 쏜 순간 (캐릭터 사격 동작, 카메라 반동, 1인칭 총 반동이 듣는다)
        public event Action Fired;
        public event Action ReloadStarted;
        public event Action ReloadFinished;

        // 탄 수가 바뀔 때 (화면 표시가 듣는다)
        public event Action AmmoChanged;

        Camera cam;
        ThirdPersonCamera look;
        readonly RaycastHit[] hits = new RaycastHit[16];
        float tracerEndTime;
        float nextFireTime;
        float reloadStartTime;
        int nextReloadClip;

        void Awake()
        {
            cam = Camera.main;
            look = GetComponent<ThirdPersonCamera>();
            Ammo = magazineSize;
            if (tracer != null)
                tracer.enabled = false;
        }

        void Update()
        {
            if (tracer != null && tracer.enabled && Time.time >= tracerEndTime)
                tracer.enabled = false;

            if (IsReloading)
                UpdateReload();

            // 마우스 커서가 잠겨 있을 때만 쏜다. 커서가 풀린 상태의 클릭은 커서를 다시 잠그는 데 쓴다.
            var mouse = Mouse.current;
            var keyboard = Keyboard.current;
            if (mouse == null || Cursor.lockState != CursorLockMode.Locked || Time.timeScale <= 0f)
                return;

            if (keyboard != null && keyboard.rKey.wasPressedThisFrame && !IsReloading && Ammo < magazineSize)
                StartReload();

            if (IsReloading)
                return;
            if (mouse.leftButton.isPressed && Time.time >= nextFireTime)
            {
                if (Ammo <= 0)
                    StartReload(); // 빈 탄창으로 쏘려 하면 재장전
                else
                    Fire();
            }
        }

        void Fire()
        {
            nextFireTime = Time.time + fireInterval;
            Ammo--;
            AmmoChanged?.Invoke();
            if (shotSound != null)
                shotSound.Play();
            Fired?.Invoke();

            // 몸을 카메라가 보는 방향으로 돌려서 총구가 조준 방향을 보게 한다.
            if (look != null)
                transform.rotation = Quaternion.Euler(0f, look.Yaw, 0f);

            // 1) 카메라 → 조준점
            if (cam == null)
                cam = Camera.main;
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
                var hitbox = muzzleHit.collider.GetComponent<Hitbox>();
                if (hitbox != null)
                {
                    hitbox.TakeHit(damage, headshotMultiplier, impact);
                }
            }

            if (Ammo <= 0)
                StartReload(); // 마지막 탄을 쏘면 바로 재장전
        }

        // 재장전을 시작한다 (이미 재장전 중이거나 탄창이 가득이면 아무 일도 없다)
        public void StartReload()
        {
            if (IsReloading || Ammo >= magazineSize)
                return;
            IsReloading = true;
            reloadStartTime = Time.time;
            nextReloadClip = 0;
            ReloadStarted?.Invoke();
        }

        void UpdateReload()
        {
            float progress = ReloadProgress;
            while (reloadClips != null && nextReloadClip < reloadClips.Length && nextReloadClip < reloadClipTimes.Length
                   && progress >= reloadClipTimes[nextReloadClip])
            {
                if (reloadAudio != null && reloadClips[nextReloadClip] != null)
                    reloadAudio.PlayOneShot(reloadClips[nextReloadClip], reloadVolume);
                nextReloadClip++;
            }
            if (progress < 1f)
                return;
            IsReloading = false;
            Ammo = magazineSize;
            AmmoChanged?.Invoke();
            ReloadFinished?.Invoke();
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
            if (tracer == null)
                return;
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
