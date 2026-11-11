using UnityEngine;

namespace LastBullet.Combat
{
    // 바주카 연출 도구 모음 (2026-11-11 사용자 요청: 게임 주스(타격감)가 밍밍하다). 먼지·불꽃 알갱이·파편·충격파 링·로켓 연기 꼬리를 코드로 만든다.
    //  - 프리팹 없이 파티클을 그때그때 만들어 쓰고 끝나면 지운다. 그림(머티리얼)은 BazookaWeapon이 연결해 준다:
    //    dust = AtmoSandPuff(먼지), glow = AtmoGlow(불꽃·빛 번짐, 더하기), ring = BossSlamRing(충격파 링의 바탕 머티리얼), debrisMat = PaintBurst(파편, 색은 코드가 입힘)
    //  - 머티리얼이 연결 안 되어 있으면 그 연출만 건너뛴다.
    public static class BlastFx
    {
        public static Material Dust, Glow, Ring, DebrisMat;

        static Texture2D ringTexture;
        static Material ringRuntime;
        static Mesh cubeMesh;

        // ── 파티클 한 덩어리를 만든다 (발사 전에는 멈춰 있고, 부르는 쪽이 Emit한다) ──
        static ParticleSystem Create(string name, Vector3 position, Quaternion rotation, Material material, float destroyAfter)
        {
            var go = new GameObject(name);
            go.transform.SetPositionAndRotation(position, rotation);
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = system.emission;
            emission.enabled = false;
            var shape = system.shape;
            shape.enabled = false;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            Object.Destroy(go, destroyAfter);
            return system;
        }

        static void Fade(ParticleSystem system, float peakAlpha, float peakAt)
        {
            var color = system.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(peakAlpha, peakAt), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;
        }

        // ── 먼지 구름: dir 방향 원뿔(angle도)로 뭉게뭉게 퍼진다 ──
        public static void Puff(Vector3 position, Vector3 direction, float angle, int count, float speedMin, float speedMax,
            float sizeMin, float sizeMax, float lifeMin, float lifeMax, Color colorA, Color colorB, float peakAlpha = 0.7f, float rise = 0.06f)
        {
            if (Dust == null)
                return;
            var system = Create("BlastPuff", position, Quaternion.LookRotation(direction), Dust, lifeMax + 0.5f);
            var main = system.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            main.startColor = new ParticleSystem.MinMaxGradient(colorA, colorB);
            main.gravityModifier = -rise; // 위로 살짝 떠오른다
            main.maxParticles = Mathf.Max(count, 16);
            var shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = angle;
            shape.radius = Mathf.Max(0.05f, sizeMin * 0.3f);
            var limit = system.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = 0.6f;
            limit.dampen = 0.12f; // 처음엔 확 퍼지고 금방 느려진다
            var size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.45f), new Keyframe(1f, 1.7f)));
            var spin = system.rotationOverLifetime;
            spin.enabled = true;
            spin.z = new ParticleSystem.MinMaxCurve(-0.8f, 0.8f);
            Fade(system, peakAlpha, 0.12f);
            system.Emit(count);
        }

        // ── 불꽃 알갱이: 길쭉하게 번쩍이며 튀고 중력으로 떨어진다 ──
        public static void Sparks(Vector3 position, Vector3 direction, float angle, int count, float speedMin, float speedMax)
        {
            if (Glow == null)
                return;
            var system = Create("BlastSparks", position, Quaternion.LookRotation(direction), Glow, 1.5f);
            var renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 2.2f;
            renderer.velocityScale = 0.05f;
            var main = system.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.95f, 0.6f, 1f), new Color(1f, 0.5f, 0.15f, 1f));
            main.gravityModifier = 1.3f;
            main.maxParticles = Mathf.Max(count, 16);
            var shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = angle;
            shape.radius = 0.15f;
            Fade(system, 1f, 0.05f);
            system.Emit(count);
        }

        // ── 파편: 작은 상자 조각이 튀어 올라 바닥에 튕기고 줄어들어 사라진다 ──
        public static void Debris(Vector3 position, Vector3 normal, int count, Color paint)
        {
            if (DebrisMat == null)
                return;
            if (cubeMesh == null)
                cubeMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            var system = Create("BlastDebris", position, Quaternion.LookRotation(normal), DebrisMat, 3f);
            var renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = cubeMesh;
            var main = system.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(6f, 14f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
            main.startRotation3D = true;
            main.startRotationX = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            main.startRotationY = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            // 땅 색(흙)과 물감 색을 섞은 조각들
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.45f, 0.36f, 0.26f, 1f), Color.Lerp(paint, Color.white, 0.15f));
            main.gravityModifier = 2.6f;
            main.maxParticles = Mathf.Max(count, 16);
            var shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 55f;
            shape.radius = 0.3f;
            var spin = system.rotationOverLifetime;
            spin.enabled = true;
            spin.separateAxes = true;
            spin.x = new ParticleSystem.MinMaxCurve(-9f, 9f);
            spin.y = new ParticleSystem.MinMaxCurve(-9f, 9f);
            spin.z = new ParticleSystem.MinMaxCurve(-9f, 9f);
            var size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.7f, 1f), new Keyframe(1f, 0f)));
            var collision = system.collision;
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.World;
            collision.mode = ParticleSystemCollisionMode.Collision3D;
            collision.bounce = 0.35f;
            collision.dampen = 0.3f;
            collision.lifetimeLoss = 0.15f;
            collision.radiusScale = 0.6f;
            collision.quality = ParticleSystemCollisionQuality.Low;
            system.Emit(count);
        }

        // ── 충격파 링: 바닥(또는 벽)에 눕혀서 빠르게 퍼지며 사라진다 ──
        public static void ShockRing(Vector3 position, Vector3 normal, float radius, Color color)
        {
            if (Ring == null)
                return;
            if (ringTexture == null)
                ringTexture = MakeRingTexture();
            if (ringRuntime == null)
            {
                ringRuntime = new Material(Ring);
                ringRuntime.mainTexture = ringTexture;
                if (ringRuntime.HasProperty("_BaseMap"))
                    ringRuntime.SetTexture("_BaseMap", ringTexture);
            }
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(go.GetComponent<Collider>());
            go.name = "ShockRing";
            go.transform.position = position + normal * 0.08f;
            go.transform.rotation = Quaternion.FromToRotation(Vector3.back, normal);
            go.transform.localScale = Vector3.zero;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = ringRuntime;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            go.AddComponent<ShockRingAnimation>().Begin(renderer, radius * 2.1f, Color.Lerp(color, Color.white, 0.3f));
        }

        // 가운데가 비고 가장자리가 부드러운 흰 고리 (알파만 쓴다)
        static Texture2D MakeRingTexture()
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float ring = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.62f, 0.86f, r)) * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.88f, 0.99f, r)));
                    float haze = Mathf.Clamp01(1f - r) * 0.12f; // 안쪽은 아주 옅게
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(ring + haze)));
                }
            texture.Apply();
            return texture;
        }

        // ── 로켓 꼬리: 연기 줄기 + 꽁무니 불꽃 + 은은한 주황 불빛 ──
        public static void AttachRocketTrail(Transform rocket, Color paint)
        {
            if (Dust != null)
            {
                var smoke = NewLoop("Smoke", rocket, new Vector3(0f, 0f, -0.3f), Dust);
                var main = smoke.main;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.2f);
                main.startSpeed = 0.15f;
                main.startSize = new ParticleSystem.MinMaxCurve(0.28f, 0.5f);
                main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.9f, 0.9f, 0.88f, 0.6f), Color.Lerp(new Color(0.65f, 0.65f, 0.65f, 0.6f), paint, 0.25f));
                main.gravityModifier = -0.05f;
                main.maxParticles = 160;
                var emission = smoke.emission;
                emission.enabled = true;
                emission.rateOverDistance = 22f;
                var size = smoke.sizeOverLifetime;
                size.enabled = true;
                size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(1f, 2.6f)));
                Fade(smoke, 0.65f, 0.1f);
                smoke.Play();
            }
            if (Glow != null)
            {
                var flame = NewLoop("Flame", rocket, new Vector3(0f, 0f, -0.34f), Glow);
                var main = flame.main;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 0.22f);
                main.startSpeed = 0.2f;
                main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.6f);
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.45f, 1f), new Color(1f, 0.45f, 0.1f, 1f));
                main.maxParticles = 60;
                var emission = flame.emission;
                emission.enabled = true;
                emission.rateOverTime = 70f;
                var size = flame.sizeOverLifetime;
                size.enabled = true;
                size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.1f)));
                Fade(flame, 1f, 0.05f);
                flame.Play();
            }
            var lightObject = new GameObject("Glow");
            lightObject.transform.SetParent(rocket, false);
            lightObject.transform.localPosition = new Vector3(0f, 0f, -0.4f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.6f, 0.25f);
            light.range = 7f;
            light.intensity = 3.5f;
            light.shadows = LightShadows.None;
        }

        static ParticleSystem NewLoop(string name, Transform parent, Vector3 localPosition, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.playOnAwake = false;
            main.loop = true;
            main.duration = 5f;
            main.simulationSpace = ParticleSystemSimulationSpace.World; // 로켓이 지나간 자리에 남는다
            var emission = system.emission;
            emission.enabled = false;
            var shape = system.shape;
            shape.enabled = false;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return system;
        }
    }

    // 충격파 링 애니메이션: 0.38초 동안 커지며(처음 빠르게) 옅어진다
    class ShockRingAnimation : MonoBehaviour
    {
        Renderer ring;
        MaterialPropertyBlock block;
        float size, start;
        Color color;
        const float Life = 0.38f;

        public void Begin(Renderer renderer, float finalSize, Color tint)
        {
            ring = renderer;
            size = finalSize;
            color = tint;
            start = Time.time;
            block = new MaterialPropertyBlock();
        }

        void Update()
        {
            float t = (Time.time - start) / Life;
            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }
            float eased = 1f - (1f - t) * (1f - t) * (1f - t);
            transform.localScale = new Vector3(size * eased, size * eased, 1f);
            var c = color;
            c.a = 0.6f * (1f - t) * Mathf.Clamp01(t * 12f);
            block.SetColor("_BaseColor", c);
            ring.SetPropertyBlock(block);
        }
    }

    // 맞는 순간 잠깐 멈칫하는 연출 (히트스톱)과 여러 마리를 한꺼번에 잡았을 때의 짧은 슬로모션.
    // Time.timeScale을 잠깐 낮췄다가 실제 시간(unscaled)으로 되돌린다. 일시정지(timeScale 0) 중이거나 이미 느려진 중이면 아무것도 하지 않는다.
    public class HitStop : MonoBehaviour
    {
        static HitStop instance;
        float endTime;
        float scale;
        bool active;

        public static void Do(float duration, float timeScale)
        {
            if (!Mathf.Approximately(Time.timeScale, 1f))
                return;
            if (instance == null)
                instance = new GameObject("HitStop").AddComponent<HitStop>();
            instance.endTime = Time.unscaledTime + duration;
            instance.scale = timeScale;
            instance.active = true;
            Time.timeScale = timeScale;
        }

        void Update()
        {
            if (active && Time.unscaledTime >= endTime)
                Restore();
        }

        void Restore()
        {
            active = false;
            if (Mathf.Approximately(Time.timeScale, scale))
                Time.timeScale = 1f; // 그 사이 일시정지(0)가 되었다면 건드리지 않는다
        }

        void OnDestroy()
        {
            if (active)
                Restore();
            if (instance == this)
                instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay()
        {
            instance = null;
        }
    }
}
