using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastBullet.EditorTools
{
    // 미술관 안을 더 어둡게 (2026-11-06 사용자 요청: "미술관 안은 어두워야 하는데 유니티의 무언가 빛 때문에 밝게 보인다는 피드백").
    //   원인: 램프(스포트라이트)가 아닌 "바탕 빛"이 미술관 전체를 고르게 밝히고 있었다.
    //     1) 환경광(Ambient, 3단계 Trilight): 하늘 (0.40, 0.33, 0.27) · 수평 (0.34, 0.27, 0.21) · 바닥 (0.22, 0.17, 0.12) — 꽤 밝은 갈색이라 그림자 자리도 환하다.
    //     2) 태양 빛(Directional Light, 세기 0.15): 실내인데 비스듬히 비추는 해가 있다.
    //     3) 반사(Default-Skybox 반사 세기 1): 바닥·가구에 하늘색이 비친다.
    //   고침: 환경광을 AmbientScale배로 줄이고, 태양 빛은 SunIntensity로, 반사는 ReflectionIntensity로 낮춘다. 램프 빛은 그대로라서 램프 아래만 밝게 남는다.
    //   값은 아래 상수를 고치고 메뉴를 다시 누르면 된다 (Window > Rendering > Lighting 창에서 직접 고쳐도 된다).
    // 메뉴: LastBullet > Phase 82 미술관 바탕 빛 줄이기
    public static class Phase82MuseumLightingSetup
    {
        const float AmbientScale = 0.45f;       // 환경광을 원래 값의 몇 배로 (원래 = 1)
        const float SunIntensity = 0f;          // 태양 빛 세기 (원래 0.15)
        const float ReflectionIntensity = 0.3f; // 반사 세기 (원래 1)

        // 처음 값 (되돌릴 때 참고)
        static readonly Color OriginalSky = new Color(0.40f, 0.33f, 0.27f);
        static readonly Color OriginalEquator = new Color(0.34f, 0.27f, 0.21f);
        static readonly Color OriginalGround = new Color(0.22f, 0.17f, 0.12f);

        [MenuItem("LastBullet/Phase 82 미술관 바탕 빛 줄이기")]
        static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/1_Museum.unity");

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = OriginalSky * AmbientScale;
            RenderSettings.ambientEquatorColor = OriginalEquator * AmbientScale;
            RenderSettings.ambientGroundColor = OriginalGround * AmbientScale;
            RenderSettings.reflectionIntensity = ReflectionIntensity;

            var sun = RenderSettings.sun;
            if (sun != null)
                sun.intensity = SunIntensity;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[LastBullet] 1_Museum: 바탕 빛을 줄였어요 (환경광 x{AmbientScale}, 태양 {SunIntensity}, 반사 {ReflectionIntensity}).");
        }
    }
}
