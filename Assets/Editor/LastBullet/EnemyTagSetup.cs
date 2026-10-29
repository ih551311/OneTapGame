using UnityEditor;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 모든 적에게 "Enemy" 태그를 붙인다 (사용자 요청, 2026-10-30).
    // 태그가 없으면 Tags and Layers 설정에 먼저 추가하고,
    // Assets/Prefabs의 적 프리팹(EnemyHealth가 있는 것)의 맨 위와 자식(몸, 머리)에 모두 붙인다.
    // 씬에 놓인 적은 프리팹을 따라 자동으로 바뀐다.
    // 메뉴: LastBullet > 적에 Enemy 태그 붙이기
    public static class EnemyTagSetup
    {
        public const string Tag = "Enemy";

        [MenuItem("LastBullet/적에 Enemy 태그 붙이기")]
        static void Run()
        {
            EnsureTag(Tag);

            int count = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(path);
                if (root.GetComponent<LastBullet.Enemy.EnemyHealth>() != null)
                {
                    foreach (var t in root.GetComponentsInChildren<Transform>(true))
                        t.gameObject.tag = Tag;
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    count++;
                }
                PrefabUtility.UnloadPrefabContents(root);
            }
            AssetDatabase.SaveAssets(); // 태그 목록(TagManager)도 파일에 저장
            Debug.Log($"[LastBullet] 적 프리팹 {count}개에 Enemy 태그를 붙였어요.");
        }

        // Tags and Layers 설정에 태그가 없으면 추가한다
        public static void EnsureTag(string tag)
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var tags = tagManager.FindProperty("tags");
            for (int i = 0; i < tags.arraySize; i++)
            {
                if (tags.GetArrayElementAtIndex(i).stringValue == tag)
                    return;
            }
            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
