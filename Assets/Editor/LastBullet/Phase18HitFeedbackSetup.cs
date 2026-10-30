using LastBullet.Player;
using LastBullet.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.EditorTools
{
    // 숲·성 마을·사막 씬에 피격 효과(HitFeedback)를 넣는다 (2026-10-31 사용자 요청).
    // Canvas 맨 뒤(다른 UI 아래)에 화면 전체 크기로 빨간 번쩍 이미지(Flash)와 저체력 테두리 이미지(LowHealth)를 만든다.
    // 그림은 Assets/UI/Vignette_Hit.png, Vignette_LowHealth.png (직접 그린 그림으로 바꿀 수 있다). 이미 있으면 건너뛴다.
    // 메뉴: LastBullet > Phase 18 피격 효과 넣기
    public static class Phase18HitFeedbackSetup
    {
        static readonly string[] Scenes =
        {
            "Assets/Scenes/2_Forest.unity",
            "Assets/Scenes/3_CastleVillage.unity",
            "Assets/Scenes/4_Desert.unity",
        };

        [MenuItem("LastBullet/Phase 18 피격 효과 넣기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            foreach (var scenePath in Scenes)
            {
                var scene = EditorSceneManager.OpenScene(scenePath);
                var canvas = ArtSlots.FindCanvas();
                var health = Object.FindFirstObjectByType<PlayerHealth>();
                var rig = Object.FindFirstObjectByType<ThirdPersonCamera>();
                if (canvas == null || health == null || rig == null || canvas.transform.Find("HitFeedback") != null)
                    continue;

                var root = new GameObject("HitFeedback", typeof(RectTransform));
                root.transform.SetParent(canvas.transform, false);
                root.transform.SetAsFirstSibling(); // 다른 UI 뒤에 그려지게 (글자·체력바가 가려지지 않게)
                Phase4StageSelectSetup.Stretch(root.GetComponent<RectTransform>());

                var lowHealth = CreateFullScreen("LowHealth", root.transform, "Assets/UI/Vignette_LowHealth.png");
                var flash = CreateFullScreen("Flash", root.transform, "Assets/UI/Vignette_Hit.png");

                var script = root.AddComponent<HitFeedback>();
                var so = new SerializedObject(script);
                so.FindProperty("playerHealth").objectReferenceValue = health;
                so.FindProperty("cameraRig").objectReferenceValue = rig;
                so.FindProperty("flash").objectReferenceValue = flash;
                so.FindProperty("lowHealth").objectReferenceValue = lowHealth;
                so.ApplyModifiedPropertiesWithoutUndo();

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[LastBullet] {scene.name}: 피격 효과(HitFeedback)를 만들었어요.");
            }
            AssetDatabase.SaveAssets();
        }

        static Image CreateFullScreen(string name, Transform parent, string spritePath)
        {
            var go = Phase4StageSelectSetup.CreateImage(name, parent, spritePath);
            Phase4StageSelectSetup.Stretch(go.GetComponent<RectTransform>());
            var image = go.GetComponent<Image>();
            image.preserveAspect = false; // 화면 전체로 늘린다
            image.raycastTarget = false;  // 마우스 클릭을 막지 않는다
            var color = image.color;
            color.a = 0f;
            image.color = color;
            return image;
        }
    }
}
