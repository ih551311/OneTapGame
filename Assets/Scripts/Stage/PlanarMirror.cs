using UnityEngine;

namespace LastBullet.Stage
{
    // 진짜 거울 (2026-11-01 사용자 요청: 포털 거울에 내 모습이 비치게).
    // 거울 면 건너편에 "반사 카메라"를 두고(주 카메라를 거울 면에 대칭으로 옮긴 위치) 장면을 그림(RenderTexture)으로 찍어,
    // 거울 면 머티리얼(셰이더 LastBullet/PlanarMirror)에 화면 좌표 그대로 입힌다. 그래서 캐릭터와 방이 거울 속에 보인다.
    //  - 거울 앞쪽에 있고, 거울이 화면 안에 있고, 멀지 않을 때만 그린다 (그 밖에는 멈춰서 성능을 아낀다).
    //  - 1인칭에서는 내 모습을 숨기므로(CameraZoom) 거울에도 내 모습이 안 비친다 (그림자만).
    //  - 반사 그림 크기는 화면의 절반 (resolution)이다. 더 선명하게 하려면 1에 가깝게 (대신 무거워진다).
    // 붙일 곳: StageMap (거울 면 Mirror_Surface가 자식에 있다. 메뉴 LastBullet > Phase 55 포털 거울에 모습 비추기)
    [DefaultExecutionOrder(200)]
    public class PlanarMirror : MonoBehaviour
    {
        [Tooltip("거울 면 (Portal 아래 Mirror_Surface). 이 오브젝트의 앞(+Z)쪽을 보는 면이어야 한다")]
        [SerializeField] Renderer mirror;

        [Tooltip("반사 그림 크기 (화면 대비, 0.5 = 절반)")]
        [Range(0.25f, 1f)]
        [SerializeField] float resolution = 0.5f;

        [Tooltip("이 거리(m) 안에서만 그린다")]
        [SerializeField] float maxDistance = 14f;

        [Tooltip("거울 면 바로 앞을 이만큼 띄워서 자른다 (거울 자신이 반사에 찍히지 않게)")]
        [SerializeField] float clipOffset = 0.03f;

        static readonly int ReflectionTex = Shader.PropertyToID("_ReflectionTex");

        Camera mainCamera;
        Camera reflectionCamera;
        RenderTexture target;
        MaterialPropertyBlock block;

        void OnDisable()
        {
            if (target != null)
            {
                target.Release();
                Destroy(target);
                target = null;
            }
            if (reflectionCamera != null)
                Destroy(reflectionCamera.gameObject);
        }

        void LateUpdate()
        {
            if (mirror == null)
                return;
            if (mainCamera == null)
                mainCamera = Camera.main;
            if (mainCamera == null)
                return;

            // 거울 면 위치와 앞쪽 방향: 이 오브젝트(StageMap)의 앞(+Z)이 거울이 보는 쪽이다 (포털 모델의 앞면을 이 방향으로 세웠다)
            Vector3 normal = transform.forward;
            Vector3 point = mirror.bounds.center;

            // 조건: 거울 앞쪽, 가깝고, 화면 안
            Vector3 cameraPosition = mainCamera.transform.position;
            float side = Vector3.Dot(cameraPosition - point, normal);
            if (side <= 0.05f || (cameraPosition - point).sqrMagnitude > maxDistance * maxDistance)
                return;
            if (!GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(mainCamera), mirror.bounds))
                return;

            EnsureResources();
            Vector4 plane = new Vector4(normal.x, normal.y, normal.z, -Vector3.Dot(normal, point));
            Matrix4x4 reflection = ReflectionMatrix(plane);

            // 반사 카메라: 주 카메라를 거울 면에 대칭으로
            reflectionCamera.CopyFrom(mainCamera);
            reflectionCamera.targetTexture = target;
            reflectionCamera.transform.position = reflection.MultiplyPoint(cameraPosition);
            reflectionCamera.transform.rotation = Quaternion.LookRotation(
                Vector3.Reflect(mainCamera.transform.forward, normal), Vector3.Reflect(mainCamera.transform.up, normal));
            reflectionCamera.worldToCameraMatrix = mainCamera.worldToCameraMatrix * reflection;

            // 거울 뒤쪽 물체는 자른다 (비스듬한 근평면)
            Vector3 clipPoint = point + normal * clipOffset;
            Vector3 clipPointCam = reflectionCamera.worldToCameraMatrix.MultiplyPoint(clipPoint);
            Vector3 clipNormalCam = reflectionCamera.worldToCameraMatrix.MultiplyVector(normal).normalized;
            var clipPlane = new Vector4(clipNormalCam.x, clipNormalCam.y, clipNormalCam.z, -Vector3.Dot(clipPointCam, clipNormalCam));
            reflectionCamera.projectionMatrix = mainCamera.CalculateObliqueMatrix(clipPlane);

            GL.invertCulling = true; // 거울에 비친 장면은 앞뒤가 뒤집혀 보인다 (끄면 바닥 같은 한 겹짜리 면이 사라진다)
            reflectionCamera.Render();

            if (block == null)
                block = new MaterialPropertyBlock();
            mirror.GetPropertyBlock(block);
            block.SetTexture(ReflectionTex, target);
            mirror.SetPropertyBlock(block);
        }

        void EnsureResources()
        {
            int width = Mathf.Max(64, Mathf.RoundToInt(Screen.width * resolution));
            int height = Mathf.Max(64, Mathf.RoundToInt(Screen.height * resolution));
            if (target == null || target.width != width || target.height != height)
            {
                if (target != null)
                {
                    target.Release();
                    Destroy(target);
                }
                target = new RenderTexture(width, height, 24, RenderTextureFormat.Default) { name = "PlanarMirror" };
            }
            if (reflectionCamera == null)
            {
                var go = new GameObject("MirrorReflectionCamera") { hideFlags = HideFlags.HideAndDontSave };
                reflectionCamera = go.AddComponent<Camera>();
                reflectionCamera.enabled = false; // 직접 Render()로만 그린다
            }
        }

        // 평면(n·x + d = 0)에 대한 대칭 행렬
        static Matrix4x4 ReflectionMatrix(Vector4 plane)
        {
            Matrix4x4 m = Matrix4x4.identity;
            m.m00 = 1f - 2f * plane.x * plane.x;
            m.m01 = -2f * plane.x * plane.y;
            m.m02 = -2f * plane.x * plane.z;
            m.m03 = -2f * plane.w * plane.x;
            m.m10 = -2f * plane.y * plane.x;
            m.m11 = 1f - 2f * plane.y * plane.y;
            m.m12 = -2f * plane.y * plane.z;
            m.m13 = -2f * plane.w * plane.y;
            m.m20 = -2f * plane.z * plane.x;
            m.m21 = -2f * plane.z * plane.y;
            m.m22 = 1f - 2f * plane.z * plane.z;
            m.m23 = -2f * plane.w * plane.z;
            return m;
        }
    }
}
