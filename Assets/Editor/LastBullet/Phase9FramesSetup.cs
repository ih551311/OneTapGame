using LastBullet.Stage;
using LastBullet.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.EditorTools
{
    // Phase 9-1: 미술관 액자 9개를 한 줄로 세우고, 화면에 "서로 다른 작품 N / 9"를 만든다.
    // 액자 한 칸 구조 (기능은 위, 겉모습은 자식): PictureFrame_0N / Frame(테두리, 전용 머티리얼) / Picture(정사각형 그림 면, 전용 머티리얼)
    // 위치: 미술관 앞쪽 (X -12.8 ~ 12.8 간격 3.2, Z 12), 시작 위치 쪽(-Z)을 본다. 액자와 그림 면은 정사각형이다.
    // 기존 PictureFrame_01(Phase 8)이 있으면 그것을 첫 칸으로 쓴다.
    // 메뉴: LastBullet > Phase 9-1 액자 9개와 진행도 만들기
    public static class Phase9FramesSetup
    {
        const float FrameOuter = 2.4f;  // 테두리 포함 정사각형
        const float PictureSize = 2.0f; // 그림 면 정사각형
        const float Height = 1.7f;
        const float FirstX = -12.8f;
        const float Spacing = 3.2f;
        const float RowZ = 12f;

        [MenuItem("LastBullet/Phase 9-1 액자 9개와 진행도 만들기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/1_Museum.unity");
            var canvas = ArtSlots.FindCanvas();
            if (GameObject.Find("PictureFrames") != null)
            {
                EditorUtility.DisplayDialog("LastBullet", "이미 PictureFrames가 있어요.", "확인");
                return;
            }

            // 1. 액자 안내와 작품 목록 팝업 (Phase 8에서 만들었으면 그대로 쓴다)
            var prompt = canvas.transform.Find("Prompt_Frame")?.gameObject;
            if (prompt == null)
            {
                prompt = Phase4StageSelectSetup.CreateImage("Prompt_Frame", canvas.transform, "Assets/UI/Prompt_Frame.png");
                Phase4StageSelectSetup.SetRect(prompt, new Vector2(0.5f, 0f), new Vector2(0f, 180f), new Vector2(480f, 60f));
                prompt.SetActive(false);
            }
            var picker = canvas.transform.Find("FramePickerPopup")?.GetComponent<GalleryPopup>();
            if (picker == null)
            {
                // 타이틀의 그림 보기 팝업을 그대로 만들고 "액자 고르기"로 쓴다
                picker = TitleSetup.CreateGallery(canvas.transform, null);
                picker.name = "FramePickerPopup";
                picker.transform.Find("Panel/Title").GetComponent<Image>().sprite = ArtSlots.LoadSprite("Assets/UI/Frame_Title.png");
                var pickerSo = new SerializedObject(picker);
                pickerSo.FindProperty("frameMode").boolValue = true;
                pickerSo.ApplyModifiedPropertiesWithoutUndo();
            }

            // 2. 액자 9개
            var rootObject = new GameObject("PictureFrames");
            Undo.RegisterCreatedObjectUndo(rootObject, "Create PictureFrames");
            var first = GameObject.Find("PictureFrame_01") ?? BuildFrame();
            var frames = new PictureFrame[9];
            for (int i = 0; i < 9; i++)
            {
                var frame = i == 0 ? first : Object.Instantiate(first);
                frame.name = $"PictureFrame_{i + 1:00}";
                frame.transform.SetParent(rootObject.transform, false);
                frame.transform.position = new Vector3(FirstX + i * Spacing, 0f, RowZ);
                var script = frame.GetComponent<PictureFrame>();
                var frameSo = new SerializedObject(script);
                frameSo.FindProperty("slot").intValue = i;
                frameSo.ApplyModifiedPropertiesWithoutUndo();
                frames[i] = script;
            }

            // 3. 진행도 표시 "서로 다른 작품 N / 9" (화면 위 가운데)
            var progress = Phase6PaintBoardSetup.Sliced("FrameProgress", canvas.transform, "Assets/UI/BagPanel.png", 24f);
            Phase4StageSelectSetup.SetRect(progress, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(560f, 90f));
            var label = Phase4StageSelectSetup.CreateImage("Label", progress.transform, "Assets/UI/FrameProgress_Label.png");
            Phase4StageSelectSetup.SetRect(label, new Vector2(0.5f, 0.5f), new Vector2(-70f, 0f), new Vector2(360f, 64f));
            var countObject = new GameObject("Count", typeof(RectTransform), typeof(TextMeshProUGUI));
            countObject.transform.SetParent(progress.transform, false);
            Phase4StageSelectSetup.SetRect(countObject, new Vector2(0.5f, 0.5f), new Vector2(190f, 0f), new Vector2(150f, 64f));
            var count = countObject.GetComponent<TextMeshProUGUI>();
            count.font = TMP_Settings.defaultFontAsset;
            count.text = "0 / 9";
            count.fontSize = 44f;
            count.fontStyle = FontStyles.Bold;
            count.alignment = TextAlignmentOptions.Center;
            count.color = Color.white;
            count.raycastTarget = false;

            // 4. 관리자 연결
            var manager = rootObject.AddComponent<GalleryManager>();
            var so = new SerializedObject(manager);
            var array = so.FindProperty("frames");
            array.arraySize = 9;
            for (int i = 0; i < 9; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
            so.FindProperty("picker").objectReferenceValue = picker;
            so.FindProperty("prompt").objectReferenceValue = prompt;
            so.FindProperty("progressCount").objectReferenceValue = count;
            so.ApplyModifiedPropertiesWithoutUndo();

            Selection.activeGameObject = rootObject;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[LastBullet] Phase 9-1: 미술관에 액자 9개(PictureFrames)와 진행도 표시를 만들고 1_Museum 씬을 저장했어요.");
        }

        // 액자 한 칸을 새로 만든다 (기능 오브젝트 + 겉모습 자식 Frame·Picture)
        static GameObject BuildFrame()
        {
            var root = new GameObject("PictureFrame_01");
            Undo.RegisterCreatedObjectUndo(root, "Create PictureFrame");
            var script = root.AddComponent<PictureFrame>();

            var frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "Frame";
            frame.transform.SetParent(root.transform, false);
            frame.transform.localPosition = new Vector3(0f, Height, 0f);
            frame.transform.localScale = new Vector3(FrameOuter, FrameOuter, 0.15f);
            frame.GetComponent<Renderer>().sharedMaterial = ArtSlots.GetOrCreateMaterial("PictureFrame", new Color32(0xC9, 0xA2, 0x4B, 0xFF));

            var picture = GameObject.CreatePrimitive(PrimitiveType.Quad); // 앞면이 -Z쪽을 본다
            picture.name = "Picture";
            Object.DestroyImmediate(picture.GetComponent<Collider>());
            picture.transform.SetParent(root.transform, false);
            picture.transform.localPosition = new Vector3(0f, Height, -0.08f);
            picture.transform.localScale = new Vector3(PictureSize, PictureSize, 1f);
            picture.GetComponent<Renderer>().sharedMaterial = GetOrCreatePictureMaterial();

            var so = new SerializedObject(script);
            so.FindProperty("pictureRenderer").objectReferenceValue = picture.GetComponent<Renderer>();
            so.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        // 그림 면은 조명 영향 없이 그림 색 그대로 보이도록 Unlit 셰이더를 쓴다
        static Material GetOrCreatePictureMaterial()
        {
            const string path = "Assets/Materials/FramePicture.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
                return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetColor("_BaseColor", new Color32(0xD8, 0xD8, 0xD8, 0xFF)); // 빈 액자 색
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
