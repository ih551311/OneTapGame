using LastBullet.Stage;
using LastBullet.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 시작 이야기를 만든다 (2026-11-11 사용자 결정: 처음 입장에 한 번 자동으로 "부모님이 열쇠를 건네준 날" 이야기, 입구 안내판(개발 후기)은 자동으로 안 뜨게).
    //   Canvas/OpeningPopup: 입구 이야기 창(StoryPopup, 글 쪽 방식)을 복사. 제목 "처음 이야기", 글은 Assets/UI/0_OpeningPages.txt.
    //   Canvas/OpeningStory: 빈 오브젝트 + OpeningStory — 미술관에 처음 들어오면(다시하기 뒤에도) 1초 뒤 창을 연다.
    //   StorySign(입구 안내판)의 Open On First Visit은 끈다.
    // 먼저 Phase 74(이야기 창)가 있어야 한다. 다시 실행하면 새로 만든다 (글 파일은 그대로 둔다). 메뉴: LastBullet > Phase 98 시작 이야기 만들기
    public static class Phase98OpeningStorySetup
    {
        const string TextPath = "Assets/UI/0_OpeningPages.txt";

        [MenuItem("LastBullet/Phase 98 시작 이야기 만들기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/1_Museum.unity");
            var canvas = ArtSlots.FindCanvas();
            var storyPopup = canvas != null ? canvas.transform.Find("StoryPopup") : null;
            var text = AssetDatabase.LoadAssetAtPath<TextAsset>(TextPath);
            if (storyPopup == null || text == null)
            {
                Debug.LogWarning("[LastBullet] 1_Museum: 이야기 창(StoryPopup)이나 글 파일(0_OpeningPages.txt)이 없어 건너뜀. 먼저 Phase 74를 실행하세요.");
                return;
            }
            foreach (var name in new[] { "OpeningPopup", "OpeningStory" })
            {
                var old = canvas.transform.Find(name);
                if (old != null)
                    Object.DestroyImmediate(old.gameObject);
            }

            // 시작 이야기 창: 입구 이야기 창을 복사
            var popupGo = Object.Instantiate(storyPopup.gameObject, canvas.transform, false);
            popupGo.name = "OpeningPopup";
            popupGo.transform.SetSiblingIndex(storyPopup.GetSiblingIndex() + 1);
            var title = popupGo.transform.Find("Panel/Title");
            if (title != null)
                title.GetComponent<TMP_Text>().text = "부모님의 편지"; // 2026-11-11: 처음 이야기는 부모님이 쓴 편지 (튜토리얼: 일기장으로 안내)
            var popup = popupGo.GetComponent<StoryPopup>();
            var so = new SerializedObject(popup);
            so.FindProperty("textPages").objectReferenceValue = text;
            so.FindProperty("rememberSeen").boolValue = false; // 입구 안내판의 '봤다' 기억과 섞이지 않게
            so.ApplyModifiedPropertiesWithoutUndo();
            popupGo.SetActive(false);

            // 처음 입장에 여는 장치 (평소에도 켜져 있어야 Start가 돈다)
            var trigger = new GameObject("OpeningStory");
            trigger.transform.SetParent(canvas.transform, false);
            var opening = trigger.AddComponent<OpeningStory>();
            var oso = new SerializedObject(opening);
            oso.FindProperty("popup").objectReferenceValue = popup;
            oso.ApplyModifiedPropertiesWithoutUndo();

            // 입구 안내판(개발 후기)은 자동으로 안 열린다
            foreach (var sign in Object.FindObjectsByType<StorySign>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var sso = new SerializedObject(sign);
                sso.FindProperty("openOnFirstVisit").boolValue = false;
                sso.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[LastBullet] 1_Museum: 시작 이야기 창을 만들고, 입구 안내판의 자동 열림은 껐어요.");
        }
    }
}
