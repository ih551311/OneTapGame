using System.IO;
using LastBullet.Inventory;
using LastBullet.Stage;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 물감 9색을 바꾼다 (2026-10-31 사용자 결정): 남색을 없애고 핑크를 넣는다.
    //   새 순서: 빨강 주황 노랑 초록 파랑 보라 핑크 하양 검정 (6번 칸 = 보라, 7번 칸 = 핑크)
    //   맵마다 3색: 숲 빨강·주황·노랑 / 성 마을 초록·파랑·보라 / 사막 핑크·하양·검정 (코드: StageColors 표)
    // 코드(PaintColor 순서)는 이미 바뀌었다. 이 메뉴는 그림과 씬을 맞춘다:
    //   1) 그림 3종(Icon_Paint_, Paint_Color_, PaintDrop_): 옛 보라 그림을 핑크로 색만 바꿔 *_Pink.png로 옮기고,
    //      옛 남색 그림 자리(*_Indigo.png → *_Purple.png)에는 옛 보라 그림을 넣는다. (파일을 옮기는 것이라 씬의 그림 연결은 그대로 따라온다)
    //   2) 씬의 오브젝트 이름: *_Purple → *_Pink, *_Indigo → *_Purple (물감 아이콘, 숫자, 그림판 색 버튼 등)
    //   3) "얻는 물감 ● ● ●" 글을 StageColors 표대로 다시 쓴다
    // 한 번만 실행하는 메뉴다. 이미 핑크 그림(Icon_Paint_Pink.png)이 있으면 아무것도 하지 않는다.
    // 메뉴: LastBullet > Phase 47 물감 색 바꾸기 (남색→핑크)
    public static class Phase47PaintColorsSetup
    {
        static readonly string[] Prefixes = { "Icon_Paint", "Paint_Color", "PaintDrop" };

        static readonly string[] Scenes =
        {
            "Assets/Scenes/0_Title.unity",
            "Assets/Scenes/1_Museum.unity",
            "Assets/Scenes/2_Forest.unity",
            "Assets/Scenes/3_CastleVillage.unity",
            "Assets/Scenes/4_Desert.unity",
        };

        static readonly (string scene, string map)[] Maps =
        {
            ("Forest", SceneNamesForest), ("Castle", SceneNamesCastle), ("Desert", SceneNamesDesert),
        };
        const string SceneNamesForest = "2_Forest", SceneNamesCastle = "3_CastleVillage", SceneNamesDesert = "4_Desert";

        [MenuItem("LastBullet/Phase 47 물감 색 바꾸기 (남색→핑크)")]
        static void Run()
        {
            if (AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/UI/Icon_Paint_Pink.png") != null)
            {
                Debug.Log("[LastBullet] 핑크 그림이 이미 있어서 건너뜀 (이 메뉴는 한 번만 실행한다)");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            foreach (var prefix in Prefixes)
                SwapArt(prefix);
            AssetDatabase.Refresh();

            foreach (var path in Scenes)
            {
                var scene = EditorSceneManager.OpenScene(path);
                int renamed = 0;
                // 먼저 Purple → Pink, 그다음 Indigo → Purple (순서가 바뀌면 이름이 겹친다)
                foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
                {
                    if (t.gameObject.scene != scene)
                        continue;
                    if (t.name.EndsWith("_Purple")) { t.name = t.name.Substring(0, t.name.Length - 6) + "Pink"; renamed++; }
                }
                foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
                {
                    if (t.gameObject.scene != scene)
                        continue;
                    if (t.name.EndsWith("_Indigo")) { t.name = t.name.Substring(0, t.name.Length - 6) + "Purple"; renamed++; }
                }
                int texts = RewriteMapTexts(scene);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[LastBullet] {scene.name}: 오브젝트 이름 {renamed}개, 얻는 물감 글 {texts}개를 바꿨어요.");
            }
            AssetDatabase.SaveAssets();
        }

        // 옛 보라 그림 → 핑크로 색만 바꿔 *_Pink.png, 옛 남색 그림 자리에는 옛 보라 그림
        static void SwapArt(string prefix)
        {
            string purple = $"Assets/UI/{prefix}_Purple.png";
            string pink = $"Assets/UI/{prefix}_Pink.png";
            string indigo = $"Assets/UI/{prefix}_Indigo.png";
            byte[] purpleBytes = File.ReadAllBytes(purple);

            Debug.Log(AssetDatabase.MoveAsset(purple, pink).Length == 0 ? $"{purple} → {pink}" : $"이동 실패: {purple}");
            File.WriteAllBytes(pink, Recolor(purpleBytes));
            AssetDatabase.ImportAsset(pink);

            AssetDatabase.MoveAsset(indigo, purple);
            File.WriteAllBytes(purple, purpleBytes);
            AssetDatabase.ImportAsset(purple);
        }

        // 보라(색상 약 271°) 계열 픽셀을 핑크(330°)로. 흰 하이라이트·검은 테두리·투명도는 그대로
        static byte[] Recolor(byte[] png)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.LoadImage(png);
            var pixels = texture.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                Color.RGBToHSV(pixels[i], out float h, out float s, out float v);
                if (s < 0.12f)
                    continue; // 흰색·회색·검정 테두리
                float degrees = h * 360f;
                if (degrees < 230f || degrees > 310f)
                    continue; // 보라 계열이 아닌 부분 (예: 베이지 테두리)
                h = Mathf.Repeat((degrees + 59f) / 360f, 1f);
                var c = (Color32)Color.HSVToRGB(h, s, v);
                c.a = pixels[i].a;
                pixels[i] = c;
            }
            texture.SetPixels32(pixels);
            byte[] result = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);
            return result;
        }

        // "얻는 물감 ● 빨강 ● 주황 ● 노랑" 글을 StageColors 표대로 다시 쓴다
        static int RewriteMapTexts(UnityEngine.SceneManagement.Scene scene)
        {
            int count = 0;
            foreach (var text in Resources.FindObjectsOfTypeAll<TMP_Text>())
            {
                if (text.gameObject.scene != scene || text.text == null || !text.text.Contains("얻는 물감"))
                    continue;
                string map = null;
                foreach (var (key, sceneName) in Maps)
                    if (text.name.Contains(key))
                        map = sceneName;
                if (map == null && System.Array.Exists(Maps, m => m.map == scene.name))
                    map = scene.name; // 맵 안의 안내 글(예: MapIntro)은 그 씬의 맵이다
                if (map == null)
                {
                    Debug.LogWarning($"[LastBullet] {scene.name}/{text.name}: 어느 맵 글인지 몰라 건너뜀 ({text.text})");
                    continue;
                }
                string line = "얻는 물감";
                foreach (var color in StageColors.For(map))
                    line += $"  <color=#{ColorUtility.ToHtmlStringRGB(PaintColors.Colors[(int)color])}>●</color> {PaintColors.KoreanNames[(int)color]}";
                text.text = line;
                count++;
            }
            return count;
        }
    }
}
