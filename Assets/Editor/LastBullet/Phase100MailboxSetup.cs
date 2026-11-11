using LastBullet.Stage;
using LastBullet.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastBullet.EditorTools
{
    // 입구 "아무말이나 쓰는 곳" 안내판을 엄마의 편지함으로 바꾼다 (2026-11-12 사용자 요청: 개발자 글 대신 엄마의 편지, 오브젝트도 게시판 말고 편지함).
    //   오브젝트: 기둥 위에 얹힌 빨간 우체통(둥근 뚜껑, 앞 덮개, 편지 투입구, 옆 깃발, 삐죽 나온 편지). 안내판과 같은 자리·방향. 겉모습은 자식 Model 아래라서 통째로 바꿔 끼울 수 있다.
    //   머티리얼: Assets/Materials/Mailbox_Body·Wood·Flag·Letter·Slot.mat (오브젝트 종류 전용, 그림·색을 바꿔 쓴다).
    //   이야기 창(Canvas/StoryPopup): 제목 "엄마의 편지", 글은 Assets/UI/0_MomLetterPages.txt (줄에 --- 만 쓰면 쪽 나뉨). 안내 글(Canvas/Prompt_Story)은 "[E] 편지 읽기".
    //   기능은 예전 그대로 StorySign 컴포넌트(E로 StoryPopup 열기, 자동 열림 없음)를 쓴다. 사용자가 쓴 개발자 글 원본은 docs/claude/dev_message_backup.txt에 보관.
    // 먼저 Phase 74(이야기 창)가 있어야 한다. 다시 실행하면 새로 만든다. (Phase 74를 다시 누르면 안내판·옛 글로 돌아가니 그 뒤에 이 메뉴를 다시 누른다.)
    // 메뉴: LastBullet > Phase 100 편지함 만들기
    public static class Phase100MailboxSetup
    {
        const string TextPath = "Assets/UI/0_MomLetterPages.txt";

        [MenuItem("LastBullet/Phase 100 편지함 만들기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/1_Museum.unity");
            var canvas = ArtSlots.FindCanvas();
            var popupTransform = canvas != null ? canvas.transform.Find("StoryPopup") : null;
            var promptTransform = canvas != null ? canvas.transform.Find("Prompt_Story") : null;
            var text = AssetDatabase.LoadAssetAtPath<TextAsset>(TextPath);
            if (popupTransform == null || promptTransform == null || text == null)
            {
                Debug.LogWarning("[LastBullet] 1_Museum: 이야기 창·안내 글·편지 글 파일 중 없는 것이 있어 건너뜀 (먼저 Phase 74).");
                return;
            }

            // 예전 오브젝트(안내판 또는 지난번 편지함)의 자리를 이어받는다
            var old = GameObject.Find("StorySign");
            if (old == null)
                old = GameObject.Find("Mailbox");
            Vector3 position = old != null ? old.transform.position : new Vector3(-2.6f, 0f, 0.9f);
            Quaternion rotation = old != null ? old.transform.rotation : Quaternion.LookRotation(new Vector3(0.8f, 0f, -0.45f));
            if (old != null)
                Object.DestroyImmediate(old);
            var stale = GameObject.Find("Mailbox");
            if (stale != null)
                Object.DestroyImmediate(stale);

            // ── 창과 안내 글 ──
            var popup = popupTransform.GetComponent<StoryPopup>();
            var so = new SerializedObject(popup);
            so.FindProperty("textPages").objectReferenceValue = text;
            so.ApplyModifiedPropertiesWithoutUndo();
            var title = popupTransform.Find("Panel/Title");
            if (title != null)
                title.GetComponent<TMP_Text>().text = "엄마의 편지";
            promptTransform.GetComponent<TMP_Text>().text = "[E] 편지 읽기";

            // ── 편지함 (3D) ──
            var wood = ArtSlots.GetOrCreateMaterial("Mailbox_Wood", new Color(0.36f, 0.24f, 0.13f));
            var body = ArtSlots.GetOrCreateMaterial("Mailbox_Body", new Color(0.72f, 0.16f, 0.13f));
            body.SetFloat("_Smoothness", 0.35f);
            var flag = ArtSlots.GetOrCreateMaterial("Mailbox_Flag", new Color(0.96f, 0.78f, 0.18f));
            var letter = ArtSlots.GetOrCreateMaterial("Mailbox_Letter", new Color(0.97f, 0.94f, 0.84f));
            var slot = ArtSlots.GetOrCreateMaterial("Mailbox_Slot", new Color(0.10f, 0.06f, 0.05f));
            EditorUtility.SetDirty(body);

            var root = new GameObject("Mailbox");
            root.transform.SetPositionAndRotation(position, rotation);
            var model = new GameObject("Model").transform;
            model.SetParent(root.transform, false);

            Part(model, "Post", PrimitiveType.Cube, new Vector3(0f, 0.55f, 0f), Vector3.zero, new Vector3(0.12f, 1.1f, 0.12f), wood);
            Part(model, "Base", PrimitiveType.Cube, new Vector3(0f, 1.2f, 0f), Vector3.zero, new Vector3(0.62f, 0.14f, 0.46f), wood);
            Part(model, "Box", PrimitiveType.Cube, new Vector3(0f, 1.38f, 0f), Vector3.zero, new Vector3(0.5f, 0.3f, 0.4f), body);
            // 둥근 뚜껑: 지름 0.5, 길이 0.4인 원기둥을 눕힌다 (축이 앞뒤)
            Part(model, "Lid", PrimitiveType.Cylinder, new Vector3(0f, 1.53f, 0f), new Vector3(90f, 0f, 0f), new Vector3(0.5f, 0.2f, 0.5f), body);
            Part(model, "Door", PrimitiveType.Cube, new Vector3(0f, 1.42f, 0.205f), Vector3.zero, new Vector3(0.44f, 0.34f, 0.02f), body);
            Part(model, "Slot", PrimitiveType.Cube, new Vector3(0f, 1.47f, 0.217f), Vector3.zero, new Vector3(0.3f, 0.035f, 0.01f), slot);
            Part(model, "Handle", PrimitiveType.Cube, new Vector3(0f, 1.34f, 0.222f), Vector3.zero, new Vector3(0.1f, 0.025f, 0.015f), flag);
            Part(model, "Letter", PrimitiveType.Cube, new Vector3(0.04f, 1.5f, 0.225f), new Vector3(0f, 0f, -9f), new Vector3(0.2f, 0.13f, 0.008f), letter);
            Part(model, "FlagPole", PrimitiveType.Cube, new Vector3(0.28f, 1.46f, 0f), Vector3.zero, new Vector3(0.025f, 0.3f, 0.025f), wood);
            Part(model, "Flag", PrimitiveType.Cube, new Vector3(0.35f, 1.56f, 0f), Vector3.zero, new Vector3(0.14f, 0.09f, 0.02f), flag);

            var blocker = root.AddComponent<BoxCollider>(); // 지나가다 통과하지 않게
            blocker.center = new Vector3(0f, 0.85f, 0f);
            blocker.size = new Vector3(0.7f, 1.7f, 0.5f);

            var device = root.AddComponent<StorySign>();
            var dso = new SerializedObject(device);
            dso.FindProperty("popup").objectReferenceValue = popup;
            dso.FindProperty("prompt").objectReferenceValue = promptTransform.gameObject;
            dso.FindProperty("openOnFirstVisit").boolValue = false;
            dso.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[LastBullet] 1_Museum: 입구 안내판을 편지함으로 바꿨어요 ({position}).");
        }

        static void Part(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 euler, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localEulerAngles = euler;
            go.transform.localScale = scale;
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }
    }
}
