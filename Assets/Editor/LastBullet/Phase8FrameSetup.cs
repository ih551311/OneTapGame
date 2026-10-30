using LastBullet.Stage;
using LastBullet.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.EditorTools
{
    // Phase 8: 미술관에 액자 1개를 세우고, E로 여는 "액자에 걸 그림 고르기" 팝업을 만든다.
    // 액자 구조 (기능은 위, 겉모습은 자식): PictureFrame_01 / Frame(테두리, 전용 머티리얼) / Picture(정사각형 그림 면, 전용 머티리얼)
    // 위치: 미술관 앞쪽 가운데 (0, 0, 12), 시작 위치 쪽(-Z)을 본다. 액자는 정사각형이다.
    // 메뉴: LastBullet > Phase 8 미술관 액자 만들기
    public static class Phase8FrameSetup
    {
        const float FrameOuter = 2.4f; // 테두리 포함 정사각형
        const float PictureSize = 2.0f; // 그림 면 정사각형
        const float Height = 1.7f;

        [MenuItem("LastBullet/Phase 8 미술관 액자 만들기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/1_Museum.unity");
            var canvas = ArtSlots.FindCanvas();
            if (GameObject.Find("PictureFrame_01") != null)
            {
                EditorUtility.DisplayDialog("LastBullet", "이미 PictureFrame_01이 있어요.", "확인");
                return;
            }

            // 1. 액자: 기능 오브젝트 + 겉모습 자식 2개
            var root = new GameObject("PictureFrame_01");
            Undo.RegisterCreatedObjectUndo(root, "Create PictureFrame_01");
            root.transform.position = new Vector3(0f, 0f, 12f);
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

            // 2. 가까이 가면 뜨는 안내
            var prompt = Phase4StageSelectSetup.CreateImage("Prompt_Frame", canvas.transform, "Assets/UI/Prompt_Frame.png");
            Phase4StageSelectSetup.SetRect(prompt, new Vector2(0.5f, 0f), new Vector2(0f, 180f), new Vector2(480f, 60f));
            prompt.SetActive(false);

            // 3. 액자에 걸 그림 고르기 팝업: 타이틀의 그림 보기 팝업을 그대로 만들고 "액자 고르기"로 쓴다
            var picker = TitleSetup.CreateGallery(canvas.transform, null);
            picker.name = "FramePickerPopup";
            var title = picker.transform.Find("Panel/Title").GetComponent<Image>();
            title.sprite = ArtSlots.LoadSprite("Assets/UI/Frame_Title.png");
            var pickerSo = new SerializedObject(picker);
            pickerSo.FindProperty("frameMode").boolValue = true;
            pickerSo.ApplyModifiedPropertiesWithoutUndo();

            // 4. 연결
            var so = new SerializedObject(script);
            so.FindProperty("slot").intValue = 0;
            so.FindProperty("pictureRenderer").objectReferenceValue = picture.GetComponent<Renderer>();
            so.FindProperty("picker").objectReferenceValue = picker;
            so.FindProperty("prompt").objectReferenceValue = prompt;
            so.ApplyModifiedPropertiesWithoutUndo();

            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[LastBullet] Phase 8: 미술관에 액자(PictureFrame_01)와 액자 고르기 팝업을 만들고 1_Museum 씬을 저장했어요.");
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
