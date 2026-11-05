using LastBullet.Stage;
using LastBullet.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 미술관 벽 장롱 6개의 문이 E로 열리고 닫히게 하고, 열리면 안으로 들어갈 수 있게 한다 (2026-11-06 사용자 요청).
    //   - 장롱 모델(에셋 Old Furniture)은 안이 비어 있고 문이 힌지 오브젝트(Left/Right)에 붙어 있다.
    //   - 통짜 충돌 상자(BoxCollider)를 지우고, 뒷판·양옆판·윗판 충돌 상자와 "문이 닫혔을 때만 켜지는 앞 상자"를 만든다.
    //   - 안내 글자 Canvas/Prompt_Wardrobe 는 안내판 글자(Prompt_Story)를 복사해서 만든다.
    // 메뉴: LastBullet > Phase 77 장롱 문 열기 (다시 실행하면 충돌 상자를 새로 만든다)
    public static class Phase77WardrobeDoorsSetup
    {
        const float Wall = 0.1f;   // 장롱 옆판·뒷판 두께로 잡는 충돌 상자 두께 (m)

        [MenuItem("LastBullet/Phase 77 장롱 문 열기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/1_Museum.unity");
            var decor = GameObject.Find("MuseumHall/Decor");
            var canvas = ArtSlots.FindCanvas();
            var storyPrompt = canvas != null ? canvas.transform.Find("Prompt_Story") : null;
            if (decor == null || storyPrompt == null)
            {
                Debug.LogWarning("[LastBullet] 1_Museum: MuseumHall/Decor 또는 Canvas/Prompt_Story 가 없어 건너뜀.");
                return;
            }

            // 안내 글자
            var old = canvas.transform.Find("Prompt_Wardrobe");
            if (old != null)
                Object.DestroyImmediate(old.gameObject);
            var promptGo = Object.Instantiate(storyPrompt.gameObject, canvas.transform, false);
            promptGo.name = "Prompt_Wardrobe";
            promptGo.transform.SetSiblingIndex(storyPrompt.GetSiblingIndex() + 1);
            var promptText = promptGo.GetComponent<TMP_Text>();
            promptText.text = "[E] 장롱 열기";
            promptGo.SetActive(false);

            int count = 0;
            foreach (Transform child in decor.transform)
            {
                if (!child.name.StartsWith("Wardrobe_"))
                    continue;
                if (SetUp(child, promptText))
                    count++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[LastBullet] 1_Museum: 장롱 {count}개에 문 열기와 안으로 들어가는 충돌 상자를 만들었어요.");
        }

        static bool SetUp(Transform wardrobe, TMP_Text prompt)
        {
            var left = wardrobe.Find("Left");
            var right = wardrobe.Find("Right");
            var body = wardrobe.Find("Wardrobe body");
            if (left == null || right == null || body == null)
            {
                Debug.LogWarning($"[LastBullet] {wardrobe.name}: Left/Right/Wardrobe body 가 없어 건너뜀.");
                return false;
            }

            // 지난번 것 지우기
            var root = wardrobe.GetComponent<WardrobeDoors>();
            if (root != null)
                Object.DestroyImmediate(root);
            foreach (var name in new[] { "Colliders", "FrontBlock", "InteractPoint" })
            {
                var t = wardrobe.Find(name);
                if (t != null)
                    Object.DestroyImmediate(t.gameObject);
            }
            foreach (var box in wardrobe.GetComponents<BoxCollider>())
                Object.DestroyImmediate(box);
            left.localRotation = Quaternion.identity;
            right.localRotation = Quaternion.identity;

            // 장롱 몸통 크기를 장롱 기준 좌표로 잰다 (본체 위치는 그대로, 앞면은 로컬 +Z)
            var renderer = body.GetComponent<Renderer>();
            var b = renderer.bounds;
            var local = new Bounds(wardrobe.InverseTransformPoint(b.center), Vector3.zero);
            for (int i = 0; i < 8; i++)
            {
                var corner = new Vector3(
                    (i & 1) == 0 ? b.min.x : b.max.x,
                    (i & 2) == 0 ? b.min.y : b.max.y,
                    (i & 4) == 0 ? b.min.z : b.max.z);
                local.Encapsulate(wardrobe.InverseTransformPoint(corner));
            }
            Vector3 min = local.min, max = local.max, size = local.size, center = local.center;

            var colliders = new GameObject("Colliders").transform;
            colliders.SetParent(wardrobe, false);
            AddBox(colliders, "Back", new Vector3(center.x, center.y, min.z + Wall * 0.5f), new Vector3(size.x, size.y, Wall));
            AddBox(colliders, "SideLeft", new Vector3(min.x + Wall * 0.5f, center.y, center.z), new Vector3(Wall, size.y, size.z));
            AddBox(colliders, "SideRight", new Vector3(max.x - Wall * 0.5f, center.y, center.z), new Vector3(Wall, size.y, size.z));
            AddBox(colliders, "Top", new Vector3(center.x, max.y - Wall * 0.5f, center.z), new Vector3(size.x, Wall, size.z));

            // 문이 닫혀 있을 때만 켜지는 앞 상자 (문 앞면 바로 안쪽)
            var front = AddBox(wardrobe, "FrontBlock", new Vector3(center.x, center.y, max.z - 0.05f), new Vector3(size.x, size.y, 0.1f));

            // E를 누를 수 있는 자리 (문 앞 0.5m, 허리 높이)
            var point = new GameObject("InteractPoint").transform;
            point.SetParent(wardrobe, false);
            point.localPosition = new Vector3(center.x, 1f, max.z + 0.5f);

            var doors = wardrobe.gameObject.AddComponent<WardrobeDoors>();
            var so = new SerializedObject(doors);
            so.FindProperty("leftHinge").objectReferenceValue = left;
            so.FindProperty("rightHinge").objectReferenceValue = right;
            so.FindProperty("interactPoint").objectReferenceValue = point;
            so.FindProperty("frontBlock").objectReferenceValue = front;
            so.FindProperty("prompt").objectReferenceValue = prompt;
            so.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        static BoxCollider AddBox(Transform parent, string name, Vector3 localCenter, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localCenter;
            var box = go.AddComponent<BoxCollider>();
            box.size = size;
            return box;
        }
    }
}
