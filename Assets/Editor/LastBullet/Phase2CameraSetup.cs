using LastBullet.Player;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.EditorTools
{
    // Phase 2-2: 3인칭 카메라(Cinemachine)와 화면 가운데 크로스헤어를 만든다.
    // 메뉴: LastBullet > Phase 2-2 3인칭 카메라와 크로스헤어 만들기
    public static class Phase2CameraSetup
    {
        [MenuItem("LastBullet/Phase 2-2 3인칭 카메라와 크로스헤어 만들기")]
        static void Create()
        {
            // Hideout 씬이 열려 있지 않으면 자동으로 연다.
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.name != "Hideout")
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    return;
                scene = EditorSceneManager.OpenScene("Assets/Scenes/Hideout.unity");
            }

            var player = GameObject.Find("Player");
            if (player == null)
            {
                EditorUtility.DisplayDialog("LastBullet", "Player가 없어요. Phase 2-1 메뉴를 먼저 실행하세요.", "확인");
                return;
            }
            if (GameObject.Find("PlayerFollowCamera") != null)
            {
                EditorUtility.DisplayDialog("LastBullet", "이미 PlayerFollowCamera가 있어요. 다시 만들려면 PlayerFollowCamera, HUD, Player/CameraTarget을 지운 뒤 실행하세요.", "확인");
                return;
            }

            // 1. 카메라가 따라갈 지점: 플레이어 머리 높이(1.6m)의 빈 오브젝트
            var cameraTarget = new GameObject("CameraTarget");
            Undo.RegisterCreatedObjectUndo(cameraTarget, "Create CameraTarget");
            cameraTarget.transform.SetParent(player.transform, false);
            cameraTarget.transform.localPosition = new Vector3(0f, 1.6f, 0f);

            // 2. 마우스로 CameraTarget을 돌리는 스크립트를 Player에 붙인다.
            var look = Undo.AddComponent<ThirdPersonCamera>(player);
            var so = new SerializedObject(look);
            so.FindProperty("cameraTarget").objectReferenceValue = cameraTarget.transform;
            so.ApplyModifiedPropertiesWithoutUndo();

            // 3. Main Camera에 CinemachineBrain을 붙인다. 실제 화면은 Main Camera가 찍고,
            //    Brain이 Cinemachine 카메라의 위치를 Main Camera에 옮겨 준다.
            var mainCamera = Camera.main;
            if (mainCamera != null && mainCamera.GetComponent<CinemachineBrain>() == null)
                Undo.AddComponent<CinemachineBrain>(mainCamera.gameObject);

            // 4. 플레이어 오른쪽 어깨 뒤에서 따라가는 Cinemachine 카메라
            var followCamera = new GameObject("PlayerFollowCamera");
            Undo.RegisterCreatedObjectUndo(followCamera, "Create PlayerFollowCamera");
            var vcam = followCamera.AddComponent<CinemachineCamera>();
            vcam.Follow = cameraTarget.transform;
            vcam.Lens.FieldOfView = 60f;

            var follow = followCamera.AddComponent<CinemachineThirdPersonFollow>();
            follow.ShoulderOffset = new Vector3(0.6f, 0.2f, 0f); // 오른쪽 0.6m, 위 0.2m
            follow.VerticalArmLength = 0f;
            follow.CameraSide = 1f;                              // 1 = 오른쪽 어깨
            follow.CameraDistance = 3.5f;                        // 뒤로 3.5m
            follow.Damping = new Vector3(0.1f, 0.2f, 0.2f);      // 살짝 늦게 따라와 부드럽게
            follow.AvoidObstacles.Enabled = true;                // 벽에 가리면 카메라가 앞으로 당겨진다
            follow.AvoidObstacles.CollisionFilter = 1;           // Default 레이어만 벽으로 본다
            follow.AvoidObstacles.IgnoreTag = "Player";

            // 5. 화면 가운데 크로스헤어
            var hud = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(hud, "Create HUD");
            hud.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = hud.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            BuildCrosshair(hud.transform);

            Selection.activeGameObject = followCamera;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[LastBullet] Phase 2-2: 3인칭 카메라와 크로스헤어를 만들고 Hideout 씬을 저장했어요.");
        }

        // 기존 크로스헤어를 지우고 십자 모양으로 다시 만든다.
        [MenuItem("LastBullet/크로스헤어 다시 만들기 (십자)")]
        static void RebuildCrosshair()
        {
            var scene = EditorSceneManager.GetActiveScene();
            var hud = GameObject.Find("HUD");
            if (scene.name != "Hideout" || hud == null)
            {
                EditorUtility.DisplayDialog("LastBullet", "Hideout 씬을 열고 Phase 2-2 메뉴를 먼저 실행하세요.", "확인");
                return;
            }

            var old = hud.transform.Find("Crosshair");
            if (old != null)
                Undo.DestroyObjectImmediate(old.gameObject);
            BuildCrosshair(hud.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[LastBullet] 크로스헤어를 십자 모양으로 다시 만들고 Hideout 씬을 저장했어요.");
        }

        // 십자 크로스헤어: 가운데를 비우고 위·아래·왼쪽·오른쪽에 짧은 선 4개 (1920x1080 기준 px)
        static void BuildCrosshair(Transform hud)
        {
            const float length = 12f;    // 선 길이
            const float thickness = 2f;  // 선 굵기
            const float gap = 5f;        // 가운데 빈 공간 (중심에서 선까지)

            var crosshair = new GameObject("Crosshair", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(crosshair, "Create Crosshair");
            crosshair.transform.SetParent(hud, false);
            var rect = crosshair.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;

            float offset = gap + length / 2f;
            CreateLine(crosshair.transform, "Top", new Vector2(0f, offset), new Vector2(thickness, length));
            CreateLine(crosshair.transform, "Bottom", new Vector2(0f, -offset), new Vector2(thickness, length));
            CreateLine(crosshair.transform, "Left", new Vector2(-offset, 0f), new Vector2(length, thickness));
            CreateLine(crosshair.transform, "Right", new Vector2(offset, 0f), new Vector2(length, thickness));
        }

        static void CreateLine(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var line = new GameObject(name, typeof(RectTransform), typeof(Image));
            line.transform.SetParent(parent, false);
            var rect = line.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = line.GetComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = false; // 마우스 클릭을 막지 않게
            // 밝은 바닥 위에서도 보이도록 검은 테두리
            var outline = line.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.6f);
            outline.effectDistance = new Vector2(1f, -1f);
        }
    }
}
