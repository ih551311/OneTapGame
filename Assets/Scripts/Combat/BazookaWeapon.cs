using System.Collections.Generic;
using LastBullet.Effects;
using LastBullet.Enemy;
using LastBullet.Inventory;
using LastBullet.Player;
using LastBullet.Stage;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace LastBullet.Combat
{
    // 바주카포 무기 (2026-11-08 사용자 요청: 에셋 Low Poly Weapons VOL.1의 RPG7을 바주카로 쓴다).
    //  - 1번 키 = 기본 소총, 2번 키 = 바주카, F키 = 둘 사이 바꾸기. (E는 물감 폭탄, Q는 구르기라서 쓰지 않는다)
    //  - 바주카: 한 발(탄창 1) 쏘면 직선으로 날아가는 로켓이 맞은 곳에서 터져 주변 적에게 범위 피해를 준다. 누를 때마다 한 발, 재장전은 로켓을 앞에서 다시 끼운다.
    //  - 터지는 곳에는 맵 물감 색의 방울과 번진 자국이 남는다 (물감 폭탄과 같은 색 돌림).
    //  - 쏘는 일(탄창·재장전·소리 시점)은 AutoGun이 하고, 이 컴포넌트는 "지금 어느 무기인가", 무기 모양 바꾸기, 로켓·폭발을 맡는다.
    //  - 겉모습은 1인칭 BazookaHolder(카메라 아래), 3인칭 ThirdPersonBazooka(오른손 아래)의 RPG7 모델이다 (모델 교체 자리).
    // 붙일 곳: 전투 맵의 Player (메뉴 LastBullet > Phase 93 바주카포 만들기). 미술관에는 없다.
    public class BazookaWeapon : MonoBehaviour
    {
        [Header("바주카 성능 (기본값)")]
        [Tooltip("탄창에 든 로켓 수")]
        [SerializeField] int magazine = 1;
        [Tooltip("재장전에 걸리는 시간 (초)")]
        [SerializeField] float reloadTime = 2.3f;
        [Tooltip("한 발 쏘고 다음 발까지 최소 시간 (초)")]
        [SerializeField] float fireInterval = 0.6f;
        [Tooltip("폭발 중심의 피해. 적 체력은 돌진형 60 · 원거리형 40 · 분열 적 140 · 작은 분열체 30 · 보스 800")]
        [SerializeField] float damage = 90f;
        [Tooltip("폭발 반지름 (m)")]
        [SerializeField] float blastRadius = 5f;
        [Tooltip("폭발 가장자리의 피해 비율 (0.3 = 30%)")]
        [Range(0f, 1f)]
        [SerializeField] float edgeDamage = 0.3f;
        [Tooltip("로켓 속도 (m/초)")]
        [SerializeField] float rocketSpeed = 55f;

        [Header("연결 - 무기 모양")]
        [Tooltip("1인칭 바주카 (Main Camera/FirstPersonGun/Model/BazookaHolder)")]
        [SerializeField] GameObject firstHolder;
        [Tooltip("3인칭 바주카 (R.Hand/ThirdPersonBazooka)")]
        [SerializeField] GameObject thirdHolder;
        [Tooltip("1인칭 소총 (Ak47Holder)")]
        [SerializeField] GameObject rifleFirst;
        [Tooltip("3인칭 소총 (ThirdPersonGun)")]
        [SerializeField] GameObject rifleThird;
        [Tooltip("1인칭 모델 안의 로켓 (쏘면 사라지고 재장전에서 다시 끼워진다)")]
        [SerializeField] Transform firstRocket;
        [Tooltip("3인칭 모델 안의 로켓")]
        [SerializeField] Transform thirdRocket;

        [Header("연결 - 로켓·폭발")]
        [Tooltip("날아가는 로켓 몸체 머티리얼 (Assets/Materials/Bazooka.mat)")]
        [SerializeField] Material rocketMaterial;
        [Tooltip("물감 방울·꼬리 머티리얼 (Assets/Materials/PaintBomb.mat)")]
        [SerializeField] Material paintMaterial;
        [Tooltip("물감 구역 바닥 그림 머티리얼 9개 (FloorPaint_*.mat, 빨강 주황 노랑 초록 파랑 보라 핑크 하양 검정 순서)")]
        [SerializeField] Material[] puddleMaterials;
        [Tooltip("터질 때 튀는 물감 방울 (Prefabs/PaintBurst)")]
        [SerializeField] PaintBurstEffect burstPrefab;

        [Header("소리")]
        [SerializeField] AudioClip fireSound;
        [SerializeField] AudioClip boomSound;
        [SerializeField] AudioClip[] reloadClips;
        [Tooltip("재장전 소리 시점 (재장전 진행도 0~1)")]
        [SerializeField] float[] reloadClipTimes = { 0.3f, 0.75f };

        public bool Active { get; private set; }
        public int Magazine => magazine;
        public float ReloadTime => reloadTime;
        public float FireInterval => fireInterval;
        public AudioClip[] ReloadClips => reloadClips;
        public float[] ReloadClipTimes => reloadClipTimes;

        // 로켓이 끼워진 자리에서 앞으로 떨어진 거리 (재장전 중. 0 = 끼워짐)
        public float RocketOffset { get; private set; }

        AutoGun gun;
        PlayerHealth health;
        int colorIndex;

        void Awake()
        {
            gun = GetComponent<AutoGun>();
            health = GetComponent<PlayerHealth>();
            Apply(false);
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || gun == null)
                return;
            if (Cursor.lockState != CursorLockMode.Locked || Time.timeScale <= 0f)
                return;
            if (health != null && health.IsDead)
                return;
            if (keyboard.digit1Key.wasPressedThisFrame)
                SetBazooka(false);
            else if (keyboard.digit2Key.wasPressedThisFrame)
                SetBazooka(true);
            else if (keyboard.fKey.wasPressedThisFrame)
                SetBazooka(!Active);
        }

        // 무기를 바꾼다 (이미 그 무기면 아무 일도 없다)
        public void SetBazooka(bool on)
        {
            if (on == Active || gun == null)
                return;
            gun.SwitchWeapon(on); // 남은 탄 저장·복원 (Active가 바뀌기 전에 현재 무기 탄을 저장한다)
            Apply(on);
        }

        void Apply(bool on)
        {
            Active = on;
            if (firstHolder != null) firstHolder.SetActive(on);
            if (thirdHolder != null) thirdHolder.SetActive(on);
            if (rifleFirst != null) rifleFirst.SetActive(!on);
            if (rifleThird != null) rifleThird.SetActive(!on);
        }

        void LateUpdate()
        {
            if (!Active || gun == null)
                return;
            // 로켓: 탄이 있으면 끼워져 있고, 쏘면 사라지고, 재장전 중에는 시간표(BazookaReloadTimeline)대로 앞에서 밀려 들어온다
            bool visible;
            float offset;
            if (!gun.IsReloading)
            {
                visible = gun.Ammo > 0;
                offset = 0f;
            }
            else
            {
                float p = gun.ReloadProgress;
                visible = BazookaReloadTimeline.RocketVisible(p);
                offset = BazookaReloadTimeline.RocketOffset(p);
            }
            RocketOffset = visible ? offset : 0f;
            ApplyRocket(firstRocket, visible, offset);
            ApplyRocket(thirdRocket, visible, offset);
        }

        void ApplyRocket(Transform rocket, bool visible, float offset)
        {
            if (rocket == null)
                return;
            // 보이고 숨기기는 크기 0으로 한다 (렌더러를 껐다 켜면 1인칭 총이 3인칭에서 숨는 동작(FirstPersonGun)과 부딪힌다)
            rocket.localScale = visible ? Vector3.one : Vector3.zero;
            // RPG7 모델 안에서 총구는 -Z 쪽이다 (BazookaHolder가 Y 180도 돌려서 바주카 앞이 +Z가 된다). 앞쪽 offset(m)만큼 밖에 있다.
            rocket.localPosition = new Vector3(0f, 0f, -offset);
        }

        public void PlayFireSound()
        {
            if (fireSound != null)
                AudioSource.PlayClipAtPoint(fireSound, Camera.main != null ? Camera.main.transform.position : transform.position, 1f);
        }

        // 로켓을 날린다 (AutoGun이 부른다)
        public void Launch(Vector3 origin, Vector3 direction, float range)
        {
            var colors = StageColors.For(SceneManager.GetActiveScene().name);
            PaintColor color = colors != null && colors.Count > 0 ? colors[colorIndex % colors.Count] : (PaintColor)(colorIndex % PaintColors.Count);
            colorIndex++;
            BazookaRocket.Spawn(this, origin, direction, rocketSpeed, range, PaintColors.Colors[(int)color], (int)color, rocketMaterial, paintMaterial);
        }

        // 로켓이 터진다 (BazookaRocket이 부른다)
        public void Explode(Vector3 point, Vector3 normal, Color paint, int colorId)
        {
            if (boomSound != null)
                AudioSource.PlayClipAtPoint(boomSound, point, 1f);
            if (burstPrefab != null)
                PaintBurstEffect.Spawn(burstPrefab, point + normal * 0.4f, point, paint);

            // 번진 물감 자국 (잠깐 보였다 사라진다)
            var material = puddleMaterials != null && colorId >= 0 && colorId < puddleMaterials.Length ? puddleMaterials[colorId] : null;
            if (material != null)
                ExplosionDecal.Spawn(point, normal, paint, material, blastRadius, 2.5f);
            ExplosionFlash.Spawn(point, paint, blastRadius, paintMaterial);

            // 범위 피해: 적의 몸에서 폭발 중심까지 가장 가까운 거리로 정한다 (1.5m 안은 전부, 가장자리는 edgeDamage 비율)
            var best = new Dictionary<EnemyHealth, float>();
            foreach (var hit in Physics.OverlapSphere(point, blastRadius, ~0, QueryTriggerInteraction.Ignore))
            {
                if (!hit.CompareTag("Enemy"))
                    continue;
                var enemy = hit.GetComponentInParent<EnemyHealth>();
                if (enemy == null || enemy.IsDead)
                    continue;
                float distance = Vector3.Distance(hit.ClosestPoint(point), point);
                if (!best.TryGetValue(enemy, out float old) || distance < old)
                    best[enemy] = distance;
            }
            foreach (var pair in best)
            {
                float t = Mathf.InverseLerp(1.5f, blastRadius, pair.Value);
                float scale = Mathf.Lerp(1f, edgeDamage, t);
                if (pair.Key.IsDead)
                    continue;
                pair.Key.ApplyDamage(damage * scale, false, pair.Key.transform.position + Vector3.up * 1.2f);
            }
        }
    }

    // 폭발 순간 번쩍이는 물감색 공 + 불빛 (0.25초)
    class ExplosionFlash : MonoBehaviour
    {
        Transform ball;
        Light flashLight;
        float radius, start;
        const float Life = 0.25f;

        public static void Spawn(Vector3 point, Color color, float radius, Material material)
        {
            var go = new GameObject("ExplosionFlash");
            go.transform.position = point;
            var flash = go.AddComponent<ExplosionFlash>();
            flash.radius = radius;
            flash.start = Time.time;

            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(sphere.GetComponent<Collider>());
            sphere.transform.SetParent(go.transform, false);
            sphere.transform.localScale = Vector3.zero;
            var renderer = sphere.GetComponent<MeshRenderer>();
            if (material != null)
                renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", Color.Lerp(color, Color.white, 0.45f));
            renderer.SetPropertyBlock(block);
            flash.ball = sphere.transform;

            var lightObject = new GameObject("Light");
            lightObject.transform.SetParent(go.transform, false);
            lightObject.transform.localPosition = Vector3.up * 0.8f;
            flash.flashLight = lightObject.AddComponent<Light>();
            flash.flashLight.type = LightType.Point;
            flash.flashLight.color = Color.Lerp(color, new Color(1f, 0.8f, 0.5f), 0.5f);
            flash.flashLight.range = radius * 2.2f;
            flash.flashLight.intensity = 14f;
            flash.flashLight.shadows = LightShadows.None;
        }

        void Update()
        {
            float t = (Time.time - start) / Life;
            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }
            float eased = 1f - (1f - t) * (1f - t);
            ball.localScale = Vector3.one * radius * 1.4f * eased * (1f - t * 0.5f);
            flashLight.intensity = 14f * (1f - t);
        }
    }

    // 바닥에 번진 물감 자국 (피해 없음, 천천히 사라진다)
    class ExplosionDecal : MonoBehaviour
    {
        Renderer visual;
        MaterialPropertyBlock block;
        Color color;
        float start, life, size;

        public static void Spawn(Vector3 point, Vector3 normal, Color color, Material material, float radius, float life)
        {
            var go = new GameObject("ExplosionDecal");
            go.transform.position = point;
            var decal = go.AddComponent<ExplosionDecal>();
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(go.transform, false);
            quad.transform.localPosition = normal * 0.04f;
            quad.transform.rotation = Quaternion.FromToRotation(Vector3.back, normal) * Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            quad.transform.localScale = Vector3.zero;
            var renderer = quad.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            decal.visual = renderer;
            decal.block = new MaterialPropertyBlock();
            decal.color = color;
            decal.start = Time.time;
            decal.life = life;
            decal.size = radius * 2f / 0.66f; // PaintPuddle과 같은 크기 계산 (얼룩 반지름 = 쿼드 가로의 약 0.33)
        }

        void Update()
        {
            float age = Time.time - start;
            if (age >= life)
            {
                Destroy(gameObject);
                return;
            }
            float spread = Mathf.Clamp01(age / 0.15f);
            float eased = 1f - (1f - spread) * (1f - spread);
            visual.transform.localScale = new Vector3(size * eased, size * eased, 1f);
            var c = color;
            c.a = 0.78f * Mathf.Clamp01((life - age) / 1f);
            block.SetColor("_BaseColor", c);
            visual.SetPropertyBlock(block);
        }
    }
}
