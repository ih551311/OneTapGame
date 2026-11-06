using LastBullet.Stage;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 미술관 옆·남쪽 벽에 장식 그림 액자와 전용 조명을 단다 (2026-11-06 사용자 요청: "미술관처럼 꾸며").
    //   북쪽 벽의 진짜 액자(PictureFrame_01)의 Frame·Picture를 복사해서 만든다. 액자 모양·머티리얼을 바꾸면 같이 바뀌지 않으니 다시 이 메뉴를 누른다.
    //   MuseumHall/WallArt/Art_xx (WallArt: 게임 시작 때 내 작품 중 무작위 한 장) + 액자마다 위에서 비추는 스포트라이트(Light_Art_xx, 북쪽 액자 조명과 같은 값).
    //   위치는 아래 Spots 목록에서 고친다. 소파 위 액자는 작게(1.4배), 나머지는 1.8배 크기.
    // 다시 누르면 지우고 새로 만든다. 메뉴: LastBullet > Phase 85 미술관 벽 그림 액자
    public static class Phase85WallArtSetup
    {
        const float WestWall = -18.0f, EastWall = 18.0f, SouthWall = -8.0f;

        // (벽, 위치 좌표, 액자 크기, 가운데 높이)
        struct Spot
        {
            public char wall;   // W 서쪽, E 동쪽, S 남쪽
            public float along; // 서·동쪽 벽이면 z, 남쪽 벽이면 x
            public float size;  // 정사각형 한 변 (m)
            public float y;     // 그림 가운데 높이
            public Spot(char wall, float along, float size, float y) { this.wall = wall; this.along = along; this.size = size; this.y = y; }
        }

        static readonly Spot[] Spots =
        {
            // 서쪽 벽 (일기장 책상 z=0.5 옆, 옷장 z=11 옆은 비움)
            new Spot('W', -6.2f, 2.0f, 1.9f), new Spot('W', -2.9f, 2.0f, 1.9f),
            new Spot('W', 5.3f, 1.5f, 3.4f),   // 소파 위
            new Spot('W', 8.2f, 2.0f, 1.9f),
            // 동쪽 벽
            new Spot('E', -6.2f, 2.0f, 1.9f), new Spot('E', -2.9f, 2.0f, 1.9f),
            new Spot('E', 0.5f, 1.5f, 3.4f),   // 소파 위
            new Spot('E', 4.2f, 2.0f, 1.9f), new Spot('E', 7.4f, 2.0f, 1.9f),
            // 남쪽(입구) 벽
            new Spot('S', -14.5f, 2.0f, 1.9f), new Spot('S', -9.5f, 1.5f, 3.4f), new Spot('S', -4.6f, 2.0f, 1.9f),
            new Spot('S', 4.6f, 2.0f, 1.9f), new Spot('S', 9.5f, 1.5f, 3.4f), new Spot('S', 14.5f, 2.0f, 1.9f),
        };

        [MenuItem("LastBullet/Phase 85 미술관 벽 그림 액자")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/1_Museum.unity");
            var source = GameObject.Find("PictureFrames/PictureFrame_01");
            var hall = GameObject.Find("MuseumHall");
            var lightSource = GameObject.Find("MuseumHall/Lights/FrameLight_01");
            if (source == null || hall == null || lightSource == null)
            {
                Debug.LogWarning("[LastBullet] 1_Museum: PictureFrame_01 / MuseumHall / FrameLight_01 을 못 찾았어요.");
                return;
            }
            var sourceFrame = source.transform.Find("Frame");
            var sourcePicture = source.transform.Find("Picture");
            var sourceLight = lightSource.GetComponentInChildren<Light>();

            var old = hall.transform.Find("WallArt");
            if (old != null)
                Object.DestroyImmediate(old.gameObject);
            var container = new GameObject("WallArt").transform;
            container.SetParent(hall.transform, false);

            // 조명도 한 곳에 모은다 (Lights 아래 Light_Art_xx)
            var lights = hall.transform.Find("Lights");
            for (int i = lights.childCount - 1; i >= 0; i--)
                if (lights.GetChild(i).name.StartsWith("Light_Art_"))
                    Object.DestroyImmediate(lights.GetChild(i).gameObject);

            for (int i = 0; i < Spots.Length; i++)
            {
                var s = Spots[i];
                // 벽 쪽 좌표·앞(방 안쪽) 방향
                Vector3 position, front;
                switch (s.wall)
                {
                    case 'W': position = new Vector3(WestWall, s.y, s.along); front = Vector3.right; break;
                    case 'E': position = new Vector3(EastWall, s.y, s.along); front = Vector3.left; break;
                    default: position = new Vector3(s.along, s.y, SouthWall); front = Vector3.forward; break;
                }
                // 진짜 액자는 그림 면이 -Z를 보고(앞), 앞으로 -0.08 나와 있다. 우리도 앞(front)이 방 안쪽을 보게 돌린다.
                Quaternion rotation = Quaternion.LookRotation(-front, Vector3.up);

                var art = new GameObject($"Art_{i + 1:00}").transform;
                art.SetParent(container, false);
                art.SetPositionAndRotation(position + front * 0.1f, rotation);

                float scale = s.size / 2.0f; // 진짜 액자 그림 면이 2.0m
                var frame = Object.Instantiate(sourceFrame.gameObject, art, false);
                frame.name = "Frame";
                Object.DestroyImmediate(frame.GetComponent<Collider>()); // 부딪히지 않게 (벽에 붙은 장식)
                frame.transform.localPosition = Vector3.zero;
                frame.transform.localRotation = Quaternion.identity;
                frame.transform.localScale = sourceFrame.localScale * scale;

                var picture = Object.Instantiate(sourcePicture.gameObject, art, false);
                picture.name = "Picture";
                picture.transform.localPosition = new Vector3(0f, 0f, sourcePicture.localPosition.z * scale);
                picture.transform.localRotation = Quaternion.identity;
                picture.transform.localScale = sourcePicture.localScale * scale;

                var component = art.gameObject.AddComponent<WallArt>();
                var so = new SerializedObject(component);
                so.FindProperty("pictureRenderer").objectReferenceValue = picture.GetComponent<Renderer>();
                so.ApplyModifiedPropertiesWithoutUndo();

                // 위에서 비추는 조명 (북쪽 액자 조명과 같은 값)
                var lightGo = new GameObject($"Light_Art_{i + 1:00}");
                lightGo.transform.SetParent(lights, false);
                Vector3 lightPos = position + front * 2.5f;
                lightPos.y = 4.2f;
                lightGo.transform.position = lightPos;
                lightGo.transform.rotation = Quaternion.LookRotation((position - lightPos).normalized, Vector3.up);
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Spot;
                light.color = sourceLight.color;
                light.intensity = sourceLight.intensity;
                light.range = sourceLight.range;
                light.spotAngle = sourceLight.spotAngle;
                light.shadows = LightShadows.None;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[LastBullet] 1_Museum: 벽 장식 그림 액자 {Spots.Length}개와 조명을 만들었어요.");
        }
    }
}
