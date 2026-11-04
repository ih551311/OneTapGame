using LastBullet.Audio;
using LastBullet.Combat;
using LastBullet.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 미술관에서는 총을 없앤다 (2026-11-04 사용자 요청: 미술관에서는 어차피 총을 안 쏘니 총 없이 그냥 걸어 다니게).
    //   1_Museum 씬에서 지우는 것:
    //     Player: AutoGun(연사 총), GunEffects(불꽃·탄피), HitSounds, 재장전·맞은 소리 칸, 3인칭 총 ThirdPersonGun(AK47과 양팔 자세)
    //     Main Camera: FirstPersonGun(1인칭 팔과 총)
    //     Canvas: AmmoDisplay(탄약), HitMarker, DamageNumbers, EnemyHealthBars, 조준점(Crosshair)
    //   전투 맵 3개(숲·성 마을·사막)는 그대로다. 총·화면 만들기 메뉴(Phase 59·61·62·63·64)는 미술관을 건너뛰도록 고쳤다.
    // 메뉴: LastBullet > Phase 65 미술관에서 총 없애기
    public static class Phase65MuseumNoGunSetup
    {
        const string MuseumPath = "Assets/Scenes/1_Museum.unity";

        [MenuItem("LastBullet/Phase 65 미술관에서 총 없애기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var scene = EditorSceneManager.OpenScene(MuseumPath);
            var player = GameObject.FindWithTag("Player");
            var canvas = ArtSlots.FindCanvas();
            if (player == null)
            {
                Debug.LogWarning("[LastBullet] 1_Museum: Player가 없어 건너뜀");
                return;
            }

            // Player
            RemoveComponent<AutoGun>(player);
            RemoveComponent<GunEffects>(player);
            RemoveComponent<HitSounds>(player);
            foreach (var name in new[] { "ReloadSound", "HitSound" })
            {
                var t = player.transform.Find(name);
                if (t != null)
                    Object.DestroyImmediate(t.gameObject);
            }
            foreach (var t in player.GetComponentsInChildren<Transform>(true))
                if (t != null && t.name == "ThirdPersonGun")
                {
                    Object.DestroyImmediate(t.gameObject);
                    break;
                }

            // 카메라의 1인칭 팔·총
            var camera = Camera.main;
            if (camera != null)
            {
                var fp = camera.transform.Find("FirstPersonGun");
                if (fp != null)
                    Object.DestroyImmediate(fp.gameObject);
            }

            // 화면
            if (canvas != null)
            {
                foreach (var name in new[] { "AmmoDisplay", "HitMarker", "DamageNumbers", "EnemyHealthBars", "Crosshair" })
                {
                    var t = canvas.transform.Find(name);
                    if (t != null)
                        Object.DestroyImmediate(t.gameObject);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[LastBullet] 1_Museum: 총과 총 화면을 없앴어요.");
        }

        static void RemoveComponent<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            if (c != null)
                Object.DestroyImmediate(c);
        }
    }
}
