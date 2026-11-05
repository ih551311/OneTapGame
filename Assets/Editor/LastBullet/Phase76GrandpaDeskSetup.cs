using System.Collections.Generic;
using LastBullet.Stage;
using LastBullet.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 미술관에 할아버지의 일기장 책상을 놓는다 (2026-11-05 사용자 요청).
    //   모델: Assets/Models/GrandpaDesk/GrandpaDesk.fbx (3ds Max 스크립트 Tools/3dsMax/GrandpaDesk.ms가 만든다)
    //   머티리얼: FBX 안의 색 12개를 전용 머티리얼 Assets/Materials/GrandpaDesk_*.mat 로 바꿔 끼운다 (이미 있으면 사용자가 고친 것이라 그대로 둔다).
    //   자리: 서쪽 벽, 옷장 두 개 사이 (z 0.5), 앞면이 전시장 안쪽(+X)을 본다. 벽과 바닥 높이는 재서 맞춘다.
    //   상호작용: 가까이 가면 "[E] 일기장 읽기" → Canvas/DiaryPopup (이야기 창 StoryPopup을 복사, 쪽 그림 Assets/UI/Diary_01.png … 순서대로).
    // 메뉴: LastBullet > Phase 76 할아버지 일기장 책상 놓기 (다시 실행하면 책상·창을 지우고 다시 만든다)
    public static class Phase76GrandpaDeskSetup
    {
        const string ModelPath = "Assets/Models/GrandpaDesk/GrandpaDesk.fbx";
        const float DeskDepth = 0.7f;
        static readonly Vector3 DefaultPosition = new Vector3(-17.25f, 0f, 0.5f);

        [MenuItem("LastBullet/Phase 76 할아버지 일기장 책상 놓기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/1_Museum.unity");
            var canvas = ArtSlots.FindCanvas();
            var storyPopup = canvas != null ? canvas.transform.Find("StoryPopup") : null;
            var storyPrompt = canvas != null ? canvas.transform.Find("Prompt_Story") : null;
            if (storyPopup == null || storyPrompt == null)
            {
                Debug.LogWarning("[LastBullet] 1_Museum: 이야기 창(StoryPopup)이 없어 건너뜀. 먼저 Phase 74 메뉴를 실행하세요.");
                return;
            }

            RemapMaterials();
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null)
            {
                Debug.LogWarning($"[LastBullet] 모델이 없어요: {ModelPath} (3ds Max에서 Tools/3dsMax/GrandpaDesk.ms를 실행하세요)");
                return;
            }

            // 지난번 것 지우기
            var oldDesk = GameObject.Find("GrandpaDesk");
            if (oldDesk != null)
                Object.DestroyImmediate(oldDesk);
            foreach (var name in new[] { "DiaryPopup", "Prompt_Diary" })
            {
                var old = canvas.transform.Find(name);
                if (old != null)
                    Object.DestroyImmediate(old.gameObject);
            }

            // ── 일기장 창: 이야기 창을 복사 ──
            var popupGo = Object.Instantiate(storyPopup.gameObject, canvas.transform, false);
            popupGo.name = "DiaryPopup";
            popupGo.transform.SetSiblingIndex(storyPopup.GetSiblingIndex() + 1);
            var title = popupGo.transform.Find("Panel/Title");
            if (title != null)
                title.GetComponent<TMP_Text>().text = "할아버지의 일기장";

            var pages = new List<Sprite>();
            for (int n = 1; n < 100; n++)
            {
                string path = $"Assets/UI/Diary_{n:00}.png";
                if (AssetDatabase.LoadAssetAtPath<Object>(path) == null)
                    break;
                pages.Add(ArtSlots.LoadSprite(path));
            }
            var popup = popupGo.GetComponent<StoryPopup>();
            var so = new SerializedObject(popup);
            var array = so.FindProperty("pages");
            array.arraySize = pages.Count;
            for (int i = 0; i < pages.Count; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = pages[i];
            so.FindProperty("rememberSeen").boolValue = false; // 일기장을 읽어도 '입구 이야기를 봤다'로 치지 않는다
            so.ApplyModifiedPropertiesWithoutUndo();
            var pageImage = popupGo.transform.Find("Panel/PageImage");
            if (pageImage != null && pages.Count > 0)
                pageImage.GetComponent<UnityEngine.UI.Image>().sprite = pages[0];
            var number = popupGo.transform.Find("Panel/PageNumber");
            if (number != null)
                number.GetComponent<TMP_Text>().text = $"1 / {Mathf.Max(1, pages.Count)}";
            popupGo.SetActive(false);

            // ── 안내 문구 ──
            var prompt = Object.Instantiate(storyPrompt.gameObject, canvas.transform, false);
            prompt.name = "Prompt_Diary";
            prompt.transform.SetSiblingIndex(storyPrompt.GetSiblingIndex() + 1);
            prompt.GetComponent<TMP_Text>().text = "[E] 일기장 읽기";
            prompt.SetActive(false);

            // ── 책상 놓기 ──
            Vector3 position = FindSpot();
            var desk = (GameObject)PrefabUtility.InstantiatePrefab(model, scene);
            desk.name = "GrandpaDesk";
            // 모델 앞면(+Z)이 +X(전시장 안쪽)를 보게. 3ds Max FBX는 맨 위에 축 변환 회전(-90°)이 있어서 그 위에 더한다
            desk.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 90f, 0f) * model.transform.rotation);
            // 지나가다 통과하지 않게 (맨 위 물체는 축 변환으로 기울어 있어서, 똑바로 선 자식에 붙인다)
            var blockerGo = new GameObject("Blocker");
            blockerGo.transform.SetParent(desk.transform, false);
            blockerGo.transform.SetPositionAndRotation(position + Vector3.up * 0.53f, Quaternion.Euler(0f, 90f, 0f));
            blockerGo.transform.localScale = Vector3.one;
            var blocker = blockerGo.AddComponent<BoxCollider>();
            blocker.size = new Vector3(1.4f, 1.06f, DeskDepth);

            var device = desk.AddComponent<InteractDevice>();
            var deviceSo = new SerializedObject(device);
            deviceSo.FindProperty("popup").objectReferenceValue = popup;
            deviceSo.FindProperty("prompt").objectReferenceValue = prompt;
            deviceSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[LastBullet] 1_Museum: 할아버지 일기장 책상을 {position}에 놓고 일기장 창({pages.Count}쪽)을 만들었어요.");
        }

        // 서쪽 벽 안쪽 면과 바닥 높이를 재서 책상 뒤가 벽에 붙게 한다
        static Vector3 FindSpot()
        {
            var p = DefaultPosition;
            float wallX = float.NegativeInfinity;
            var shell = GameObject.Find("MuseumHall/Shell");
            if (shell != null)
            {
                foreach (var r in shell.GetComponentsInChildren<Renderer>())
                {
                    var b = r.bounds;
                    // 서쪽의 얇고 긴 벽 (z 0.5를 지나는 것)
                    if (b.center.x < -15f && b.size.x < 1.5f && b.size.z > 3f && b.min.z <= p.z && b.max.z >= p.z && b.size.y > 1f)
                        wallX = Mathf.Max(wallX, b.max.x);
                }
            }
            if (!float.IsNegativeInfinity(wallX))
                p.x = wallX + DeskDepth * 0.5f + 0.02f;

            if (Physics.Raycast(new Vector3(p.x, 3f, p.z), Vector3.down, out var hit, 5f, ~0, QueryTriggerInteraction.Ignore))
                p.y = hit.point.y;
            return p;
        }

        // FBX 안의 머티리얼을 전용 머티리얼(Assets/Materials/GrandpaDesk_*.mat)로 바꿔 끼운다
        static void RemapMaterials()
        {
            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (importer == null)
                return;
            bool changed = false;
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(ModelPath))
            {
                if (asset is not Material source)
                    continue;
                Color color = source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor") : source.color;
                color.a = 1f;
                bool existed = AssetDatabase.LoadAssetAtPath<Material>($"Assets/Materials/{source.name}.mat") != null;
                var material = ArtSlots.GetOrCreateMaterial(source.name, color);
                if (!existed)
                {
                    material.SetFloat("_Smoothness", 0.15f);
                    if (source.name.EndsWith("Flame"))
                    {
                        material.EnableKeyword("_EMISSION");
                        material.SetColor("_EmissionColor", new Color(1f, 0.7f, 0.3f) * 1.5f);
                        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                    }
                    EditorUtility.SetDirty(material);
                }
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), source.name), material);
                changed = true;
            }
            if (changed)
            {
                AssetDatabase.SaveAssets();
                importer.SaveAndReimport();
            }
        }
    }
}
