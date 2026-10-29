using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // Assets/UI의 그림 중 "Multiple" 방식으로 들어간 것을 "Single"로 바꾼다.
    // Multiple이면 9-slice 모서리 설정(spriteBorder)이 적용되지 않아 창·버튼이 늘어날 때 모서리가 찌그러진다.
    // 방식을 바꾸면 그림의 내부 번호가 바뀌므로, 씬과 프리팹에서 그 그림을 가리키던 연결도 새 번호로 고쳐 준다.
    // 메뉴: LastBullet > UI 그림 설정 고치기 (9-slice)
    public static class UiSpriteFix
    {
        const long SingleSpriteFileId = 21300000; // Single 방식 스프라이트의 고정 번호

        [MenuItem("LastBullet/UI 그림 설정 고치기 (9-slice)")]
        static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            // 고칠 씬이 열려 있지 않게 빈 씬을 잠깐 연다
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Multiple인 그림을 Single로 바꾸고, 옛 번호 → 새 번호 대응을 기록한다
            var replacements = new Dictionary<string, string>();
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/UI" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null || importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Multiple)
                    continue;

                var oldSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (oldSprite != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(oldSprite, out string _, out long oldId))
                    replacements[$"fileID: {oldId}, guid: {guid}"] = $"fileID: {SingleSpriteFileId}, guid: {guid}";

                importer.spriteImportMode = SpriteImportMode.Single; // spriteBorder는 그대로 유지된다
                importer.SaveAndReimport();
            }

            // 2. 씬과 프리팹 파일 안의 연결을 새 번호로 바꾼다
            int changedFiles = 0;
            var files = new List<string>();
            files.AddRange(Directory.GetFiles("Assets/Scenes", "*.unity", SearchOption.AllDirectories));
            files.AddRange(Directory.GetFiles("Assets/Prefabs", "*.prefab", SearchOption.AllDirectories));
            foreach (var file in files)
            {
                string text = File.ReadAllText(file);
                string original = text;
                foreach (var pair in replacements)
                    text = text.Replace(pair.Key, pair.Value);
                if (text != original)
                {
                    File.WriteAllText(file, text);
                    AssetDatabase.ImportAsset(file);
                    changedFiles++;
                }
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene("Assets/Scenes/1_Museum.unity");
            Debug.Log($"[LastBullet] UI 그림 {replacements.Count}개를 Single 방식으로 바꾸고, 씬·프리팹 {changedFiles}개의 연결을 고쳤어요.");
        }
    }
}
