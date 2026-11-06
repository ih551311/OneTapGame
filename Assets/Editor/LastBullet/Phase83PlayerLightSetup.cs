using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 어두운 미술관에서 플레이어가 잘 보이도록 아주 약한 조명을 붙인다 (2026-11-06 사용자 요청: "맵이 어두운 만큼 플레이어에 아주 아주 살짝의 조명").
    //   Player 아래 자식 PlayerLight (포인트 라이트, 그림자 없음)를 만든다. 머리 위쪽 약간 앞에 두어 얼굴과 몸이 은은하게 밝아진다.
    //   값은 아래 상수나 씬의 Player/PlayerLight 인스펙터에서 바꾼다. 미술관만 붙인다 (전투 맵은 밝아서 필요 없다).
    // 메뉴: LastBullet > Phase 83 플레이어 약한 조명 (다시 누르면 새로 만든다)
    public static class Phase83PlayerLightSetup
    {
        const float Intensity = 0.8f;   // 아주 약하게
        const float Range = 4.5f;       // m
        static readonly Color LightColor = new Color(1f, 0.9f, 0.75f); // 램프와 어울리는 따뜻한 흰색

        [MenuItem("LastBullet/Phase 83 플레이어 약한 조명")]
        static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/1_Museum.unity");
            var player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("[LastBullet] 1_Museum: Player를 못 찾았어요.");
                return;
            }
            var old = player.transform.Find("PlayerLight");
            if (old != null)
                Object.DestroyImmediate(old.gameObject);

            var go = new GameObject("PlayerLight");
            go.transform.SetParent(player.transform, false);
            go.transform.localPosition = new Vector3(0f, 2.2f, 0.6f); // 머리 위, 살짝 앞
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = LightColor;
            light.intensity = Intensity;
            light.range = Range;
            light.shadows = LightShadows.None; // 그림자 없이 가볍게

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[LastBullet] 1_Museum: 플레이어 약한 조명을 붙였어요 (세기 {Intensity}, 범위 {Range}m).");
        }
    }
}
