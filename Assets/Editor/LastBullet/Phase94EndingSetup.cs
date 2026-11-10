using LastBullet.Stage;
using LastBullet.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 결말 보기를 만든다 (2026-11-11 사용자 결정: 그림 9개를 채우면 할아버지 일기장 쪽에 "결말 보기"가 따로 생기고, F로 연다. 이야기는 직접 넘기는 형식, 글은 나중에 쓴다).
    //   Canvas/EndingPopup: 입구 이야기 창(StoryPopup, 글 쪽 방식)을 복사. 제목 "결말", 글은 Assets/UI/0_EndingPages.txt (줄에 --- 만 쓰면 쪽 나뉨).
    //   Canvas/Prompt_Ending: 일기장 안내("[E] 일기장 읽기") 바로 아래에 뜨는 "[F] 결말 보기".
    //   GrandpaDesk에 EndingDevice를 붙인다 (액자 9칸이 서로 다른 그림으로 다 차야 안내가 뜬다).
    // 먼저 Phase 74(이야기 창)·76(일기장 책상)이 있어야 한다. 다시 실행하면 새로 만든다 (글 파일은 이미 있으면 그대로 둔다).
    // 메뉴: LastBullet > Phase 94 결말 보기 만들기
    public static class Phase94EndingSetup
    {
        const string TextPath = "Assets/UI/0_EndingPages.txt";

        [MenuItem("LastBullet/Phase 94 결말 보기 만들기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/1_Museum.unity");
            var canvas = ArtSlots.FindCanvas();
            var storyPopup = canvas != null ? canvas.transform.Find("StoryPopup") : null;
            var diaryPopup = canvas != null ? canvas.transform.Find("DiaryPopup") : null;
            var diaryPrompt = canvas != null ? canvas.transform.Find("Prompt_Diary") : null;
            var desk = GameObject.Find("GrandpaDesk");
            if (storyPopup == null || diaryPrompt == null || desk == null)
            {
                Debug.LogWarning("[LastBullet] 1_Museum: 이야기 창·일기장 안내·책상 중 없는 것이 있어 건너뜀. 먼저 Phase 74·76 메뉴를 실행하세요.");
                return;
            }
            var text = AssetDatabase.LoadAssetAtPath<TextAsset>(TextPath);
            if (text == null)
            {
                Debug.LogWarning($"[LastBullet] 결말 글 파일이 없어요: {TextPath}");
                return;
            }

            // 지난번 것 지우기
            foreach (var name in new[] { "EndingPopup", "Prompt_Ending" })
            {
                var old = canvas.transform.Find(name);
                if (old != null)
                    Object.DestroyImmediate(old.gameObject);
            }
            var oldDevice = desk.GetComponent<EndingDevice>();
            if (oldDevice != null)
                Object.DestroyImmediate(oldDevice);

            // ── 결말 창: 입구 이야기 창(글 쪽)을 복사 ──
            var popupGo = Object.Instantiate(storyPopup.gameObject, canvas.transform, false);
            popupGo.name = "EndingPopup";
            popupGo.transform.SetSiblingIndex((diaryPopup != null ? diaryPopup : storyPopup).GetSiblingIndex() + 1);
            var title = popupGo.transform.Find("Panel/Title");
            if (title != null)
                title.GetComponent<TMP_Text>().text = "결말";
            var popup = popupGo.GetComponent<StoryPopup>();
            var so = new SerializedObject(popup);
            so.FindProperty("textPages").objectReferenceValue = text;
            so.FindProperty("rememberSeen").boolValue = false; // 결말을 봐도 '입구 이야기를 봤다'로 치지 않는다
            so.ApplyModifiedPropertiesWithoutUndo();
            popupGo.SetActive(false);

            // ── 안내 문구: 일기장 안내 바로 아래 ──
            var prompt = Object.Instantiate(diaryPrompt.gameObject, canvas.transform, false);
            prompt.name = "Prompt_Ending";
            prompt.transform.SetSiblingIndex(diaryPrompt.GetSiblingIndex() + 1);
            prompt.GetComponent<TMP_Text>().text = "[F] 결말 보기";
            var rect = (RectTransform)prompt.transform;
            float height = Mathf.Max(40f, rect.rect.height);
            rect.anchoredPosition += new Vector2(0f, -(height + 6f));
            prompt.SetActive(false);

            // ── 책상에 결말 장치 ──
            var device = desk.AddComponent<EndingDevice>();
            var deviceSo = new SerializedObject(device);
            deviceSo.FindProperty("popup").objectReferenceValue = popup;
            deviceSo.FindProperty("prompt").objectReferenceValue = prompt;
            deviceSo.FindProperty("otherPopup").objectReferenceValue = diaryPopup != null ? diaryPopup.GetComponent<PopupBase>() : null;
            deviceSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[LastBullet] 1_Museum: 결말 보기를 만들었어요 (액자 9칸을 채우면 일기장 책상에서 F).");
        }
    }
}
