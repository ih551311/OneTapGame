using System.Collections.Generic;
using System.IO;
using LastBullet.Inventory;
using LastBullet.Stage;
using LastBullet.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.EditorTools
{
    // 글자가 그림(PNG) 안에 들어 있던 UI를 모두 TextMeshPro 글자로 바꾼다 (2026-10-31 사용자 요청: 그림은 직접 그리고 글자는 유니티 글꼴로).
    // 5개 씬(타이틀·미술관·숲·성 마을·사막)에서 "글자 그림"을 쓰는 Image를 찾아 같은 오브젝트·같은 자리에 TMP 글자로 바꾼다.
    //  - 글꼴은 전부 Assets/UI/Fonts/font1 SDF 하나. 기존 숫자 글자도 같은 글꼴로 맞추고, 앞으로 새로 만드는 글자도 이 글꼴이 기본이 된다 (TMP Settings).
    //  - 글자 모양은 예전 그림과 같게: 흰 글씨 + 어두운 테두리(외곽선), 그림판 쪽은 어두운 남색 글씨, 도움말은 노란 제목 + 흰 글씨.
    //    외곽선은 글꼴 머티리얼 하나(Assets/UI/Fonts/font1 SDF Outline.mat)를 같이 쓴다. 그 머티리얼에서 테두리 색·굵기를 한 번에 바꾼다.
    //  - 글자 크기는 그림 속 글자 높이를 재서 맞춘다. 칸이 모자라면 자동으로 줄어든다.
    //  - "얻는 물감 ● 빨강 ● 주황 ● 노랑"은 맵의 물감 표(StageColors)에서 색 점까지 글자로 만든다.
    //  - 맵 입장 안내의 "사막 · 밤"은 MapIntroBanner가 맵 이름 + 낮/밤으로 만든다.
    //  - 도움말 내용은 현재 규칙(조준, 힐, 몸샷 처치 등)에 맞게 새로 썼다 (사용자가 Help_Content 칸의 글자를 고치면 된다).
    // 타이틀 로고(Title_Logo)와 배경 그림은 그림 그대로 둔다. 한 번 돌리면 바뀐 곳은 건너뛴다 (다시 실행해도 안전).
    // 메뉴: LastBullet > Phase 39 글자 그림을 TMP로 바꾸기
    public static class Phase39TextLabelSetup
    {
        const string FontPath = "Assets/UI/Fonts/font1 SDF.asset";
        const string OutlineMaterialPath = "Assets/UI/Fonts/font1 SDF Outline.mat";

        enum Style { Outline, Dark, Yellow }

        static readonly (string scene, string map)[] Scenes =
        {
            ("Assets/Scenes/0_Title.unity", null),
            ("Assets/Scenes/1_Museum.unity", null),
            ("Assets/Scenes/2_Forest.unity", "Forest"),
            ("Assets/Scenes/3_CastleVillage.unity", "Castle"),
            ("Assets/Scenes/4_Desert.unity", "Desert"),
        };

        static readonly Dictionary<string, (string text, Style style)> Table = new Dictionary<string, (string, Style)>
        {
            // 버튼 글자
            ["ButtonLabel_Craft"] = ("합성", Style.Outline),
            ["ButtonLabel_Delete"] = ("삭제", Style.Outline),
            ["ButtonLabel_DeleteNo"] = ("취소", Style.Outline),
            ["ButtonLabel_DeleteYes"] = ("지우기", Style.Outline),
            ["ButtonLabel_Gallery"] = ("그림 보기", Style.Outline),
            ["ButtonLabel_HangLeft"] = ("왼쪽에 걸기", Style.Outline),
            ["ButtonLabel_HangRight"] = ("오른쪽에 걸기", Style.Outline),
            ["ButtonLabel_Help"] = ("도움말", Style.Outline),
            ["ButtonLabel_Home"] = ("미술관으로 귀환", Style.Outline),
            ["ButtonLabel_Options"] = ("옵션", Style.Outline),
            ["ButtonLabel_ProfileDraw"] = ("직접 그리기", Style.Outline),
            ["ButtonLabel_ProfilePick"] = ("그림에서 고르기", Style.Outline),
            ["ButtonLabel_Quit"] = ("게임 나가기", Style.Outline),
            ["ButtonLabel_Resume"] = ("계속하기", Style.Outline),
            ["ButtonLabel_Start"] = ("시작", Style.Outline),
            ["ButtonLabel_ToMuseum"] = ("미술관으로", Style.Outline),
            ["ButtonLabel_ToTitle"] = ("타이틀로 나가기", Style.Outline),
            // 창 제목
            ["Frame_Title"] = ("액자에 걸 그림", Style.Outline),
            ["Gallery_Title"] = ("내 그림", Style.Outline),
            ["Help_Title"] = ("도움말", Style.Outline),
            ["Options_Title"] = ("옵션", Style.Outline),
            ["PaperCraft_Title"] = ("종이 합성", Style.Outline),
            ["Pause_Title"] = ("일시정지", Style.Outline),
            ["ProfileDraw_Title"] = ("프로필 그리기", Style.Outline),
            ["ProfilePick_Title"] = ("프로필 사진 고르기", Style.Outline),
            ["Profile_Title"] = ("프로필", Style.Outline),
            ["StageSelect_Title"] = ("전투 장소 선택", Style.Outline),
            ["Paint_Title"] = ("나의 그림판", Style.Dark),
            // 글자 칸
            ["Label_Nickname"] = ("닉네임", Style.Outline),
            ["Label_Sensitivity"] = ("마우스 감도", Style.Outline),
            ["Label_Volume"] = ("소리 크기", Style.Outline),
            ["PaperCraft_Owned"] = ("가진 것", Style.Outline),
            ["Gallery_Empty"] = ("아직 그림이 없어요", Style.Outline),
            ["Gallery_DeleteConfirm_Message"] = ("이 그림을 지울까요?\n지운 그림은 되살릴 수 없어요.", Style.Outline),
            ["Pause_LossWarning"] = ("타이틀로 나가면 이번 판 재료가 사라져요", Style.Outline),
            ["Profile_Hint"] = ("눌러서 프로필 바꾸기", Style.Outline),
            ["Toast_PaintFull"] = ("가득 참!", Style.Yellow),
            // 그림판 (밝은 판 위의 어두운 글씨)
            ["Paint_Label_Cancel"] = ("취소", Style.Dark),
            ["Paint_Label_Continue"] = ("계속 그리기", Style.Dark),
            ["Paint_Label_Discard"] = ("그만두기", Style.Dark),
            ["Paint_Label_Done"] = ("완성", Style.Dark),
            ["Paint_Label_Gallery"] = ("내 그림", Style.Dark),
            ["Paint_PaperLabel"] = ("종이", Style.Dark),
            ["Paint_PaperUnit"] = ("장", Style.Dark),
            ["Paint_Confirm_Message"] = ("그림을 버리고 나갈까요?\n쓴 물감은 돌아오지 않아요", Style.Dark),
            // 안내·결과
            ["Prompt_Frame"] = ("[E] 그림 걸기", Style.Outline),
            ["Prompt_Paint"] = ("[E] 그림 그리기", Style.Outline),
            ["Prompt_PaperCraft"] = ("[E] 종이 합성", Style.Outline),
            ["Prompt_StageSelect"] = ("[E] 전투 장소 선택", Style.Outline),
            ["ResultCaption_Gained"] = ("얻은 재료", Style.Outline),
            ["ResultCaption_Lost"] = ("잃은 재료", Style.Outline),
            ["ResultLabel_Kills"] = ("잡은 적", Style.Outline),
            ["ResultTitle_Clear"] = ("이번 판 결과", Style.Outline),
            ["ResultTitle_Dead"] = ("쓰러졌어요", Style.Outline),
            // 전투 장소
            ["StageLabel_Forest"] = ("숲", Style.Outline),
            ["StageLabel_Castle"] = ("성 마을", Style.Outline),
            ["StageLabel_Desert"] = ("사막", Style.Outline),
        };

        static readonly Dictionary<string, string> MapNames = new Dictionary<string, string>
        {
            ["Forest"] = "숲", ["Castle"] = "성 마을", ["Desert"] = "사막",
        };

        static readonly Dictionary<string, string> MapScenes = new Dictionary<string, string>
        {
            ["Forest"] = "2_Forest", ["Castle"] = "3_CastleVillage", ["Desert"] = "4_Desert",
        };

        [MenuItem("LastBullet/Phase 39 글자 그림을 TMP로 바꾸기")]
        static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font == null)
            {
                EditorUtility.DisplayDialog("LastBullet", "font1 SDF 글꼴 에셋이 없어요.", "확인");
                return;
            }
            var outline = GetOutlineMaterial(font);
            SetDefaultFont(font);

            int total = 0, fontsChanged = 0;
            foreach (var (path, _) in Scenes)
            {
                var scene = EditorSceneManager.OpenScene(path);
                int converted = 0;
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var image in root.GetComponentsInChildren<Image>(true))
                    {
                        if (image.sprite == null)
                            continue;
                        if (TryConvert(image, font, outline))
                            converted++;
                    }
                }

                // 남아 있는 다른 TMP 글자(숫자 등)도 같은 글꼴로
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
                    {
                        if (text.font != font)
                        {
                            Undo.RecordObject(text, "font1");
                            text.font = font;
                            fontsChanged++;
                        }
                    }
                }
                total += converted;
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[LastBullet] {scene.name}: 글자 그림 {converted}개를 TMP 글자로 바꿨어요.");
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[LastBullet] Phase 39: 글자 그림 {total}개를 TMP로 바꾸고, 다른 글자 {fontsChanged}개의 글꼴을 font1 SDF로 맞췄어요.");
        }

        static bool TryConvert(Image image, TMP_FontAsset font, Material outline)
        {
            string name = image.sprite.name;
            var go = image.gameObject;

            // 도움말 내용: 새로 쓴 두 칸
            if (name == "Help_Content")
            {
                BuildHelp(image, font);
                return true;
            }

            string text;
            Style style;
            var banner = go.GetComponentInParent<MapIntroBanner>(true);

            if (name.StartsWith("StageTitle_"))
            {
                // StageTitle_Forest_Day → 숲. "숲 · 낮/밤"은 MapIntroBanner가 만든다.
                string key = name.Split('_')[1];
                text = MapNames[key] + " · 낮";
                style = Style.Outline;
                if (banner != null)
                {
                    var so = new SerializedObject(banner);
                    so.FindProperty("mapName").stringValue = MapNames[key];
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            else if (name.StartsWith("StageColors_"))
            {
                text = PaintLine(name.Substring("StageColors_".Length));
                style = Style.Outline;
            }
            else if (Table.TryGetValue(name, out var entry))
            {
                text = entry.text;
                style = entry.style;
            }
            else
                return false; // 글자 그림이 아님 (아이콘, 판 등)

            var info = Measure(name, image);
            var rect = (RectTransform)go.transform;

            Object.DestroyImmediate(image);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.text = text;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.richText = true;
            tmp.alignment = info.left ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Center;
            if (info.left)
                tmp.margin = new Vector4(info.leftPadding, 0f, 0f, 0f);

            int lines = text.Split('\n').Length;
            // 글자 높이(그림 속 획이 차지한 높이)는 글꼴 크기보다 조금 작다: 한 줄이면 약 0.95배, 여러 줄이면 줄 사이 간격 포함
            float size = Mathf.Max(12f, info.textHeight / (lines == 1 ? 0.95f : lines * 1.15f));
            tmp.enableAutoSizing = true; // 칸이 좁으면 줄어든다
            tmp.fontSizeMax = size;
            tmp.fontSizeMin = Mathf.Max(8f, size * 0.5f);
            tmp.fontSize = size;

            switch (style)
            {
                case Style.Outline:
                    tmp.color = Color.white;
                    tmp.fontStyle = FontStyles.Bold; // 이서윤체는 얇아서, 예전 그림처럼 굵게
                    tmp.fontSharedMaterial = outline;
                    break;
                case Style.Yellow:
                    tmp.color = new Color32(0xFF, 0xC8, 0x20, 0xFF);
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.fontSharedMaterial = outline;
                    break;
                default:
                    tmp.color = new Color32(0x40, 0x40, 0x60, 0xFF);
                    break;
            }

            if (banner != null && name.StartsWith("StageTitle_"))
            {
                var so = new SerializedObject(banner);
                so.FindProperty("titleText").objectReferenceValue = tmp;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorUtility.SetDirty(go);
            return true;
        }

        // 맵의 물감 표(StageColors)로 "얻는 물감 ● 빨강 ● 주황 ● 노랑" (점마다 그 물감 색)
        static string PaintLine(string key)
        {
            var colors = StageColors.For(MapScenes[key]);
            string line = "얻는 물감";
            foreach (var color in colors)
            {
                string hex = ColorUtility.ToHtmlStringRGB(PaintColors.Colors[(int)color]);
                line += $"  <color=#{hex}>●</color> {PaintColors.KoreanNames[(int)color]}";
            }
            return line;
        }

        struct Metrics
        {
            public float textHeight; // 화면에서 글자 줄 전체가 차지하는 높이(px)
            public bool left;
            public float leftPadding;
        }

        // 그림 안 글자의 높이와 위치를 재서, 칸에 그려지던 크기로 환산한다
        static Metrics Measure(string name, Image image)
        {
            var result = new Metrics { textHeight = 40f };
            string path = AssetDatabase.GetAssetPath(image.sprite);
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return result;
            var texture = new Texture2D(2, 2);
            texture.LoadImage(File.ReadAllBytes(path));
            int w = texture.width, h = texture.height;
            var pixels = texture.GetPixels32();
            int minX = w, maxX = -1, minY = h, maxY = -1;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (pixels[y * w + x].a > 128)
                    {
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            }
            Object.DestroyImmediate(texture);
            if (maxX < 0)
                return result;

            // preserveAspect로 그림이 칸보다 작게 보일 때의 배율
            var rect = ((RectTransform)image.transform).rect;
            float scale = image.preserveAspect ? Mathf.Min(rect.width / w, rect.height / h) : rect.height / h;
            result.textHeight = (maxY - minY + 1) * scale;
            float center = (minX + maxX) * 0.5f;
            result.left = minX < w * 0.12f && center < w * 0.45f;
            result.leftPadding = minX * scale;
            return result;
        }

        static void BuildHelp(Image image, TMP_FontAsset font)
        {
            var go = image.gameObject;
            var rect = (RectTransform)go.transform;
            Object.DestroyImmediate(image);
            for (int i = rect.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(rect.GetChild(i).gameObject);

            const string yellow = "FFC020";
            string controls =
                $"<color=#{yellow}><size=130%>조작</size></color>\n" +
                "WASD  이동\n마우스  시점\n좌클릭  발사\n우클릭(누르고 있기)  조준\nSpace  점프\nShift  달리기\nQ  회피 (잠깐 무적)\nE  줍기 · 상호작용\n마우스 휠  1인칭 · 3인칭\nESC  메뉴";
            string rules =
                $"<color=#{yellow}><size=130%>규칙</size></color>\n" +
                "탄환은 한 발뿐이에요.\n빗나가면 탄환이 떨어져요. E키로 줍거나 기다리면 돌아와요.\n머리를 맞히면 처치하고 물감을 얻어요.\n몸을 맞혀도 처치되지만 물감은 없어요.\n적을 처치하면 체력이 조금 차요.\n얻은 재료는 미술관에 돌아가야 내 것이 돼요.\n쓰러지면 이번 판 재료를 잃어요.";

            MakeColumn(rect, "Controls", controls, font, 0f, 0.46f);
            MakeColumn(rect, "Rules", rules, font, 0.52f, 1f);
        }

        static void MakeColumn(RectTransform parent, string name, string content, TMP_FontAsset font, float xMin, float xMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(xMin, 0f);
            rect.anchorMax = new Vector2(xMax, 1f);
            rect.offsetMin = new Vector2(10f, 10f);
            rect.offsetMax = new Vector2(-10f, -10f);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = content;
            text.fontSize = 30f;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.lineSpacing = 12f;
            text.raycastTarget = false;
            text.richText = true;
        }

        // 흰 글씨 + 어두운 테두리용 글꼴 머티리얼 (font1 SDF 복사본). 이미 있으면 사용자가 고친 것이므로 그대로 둔다.
        static Material GetOutlineMaterial(TMP_FontAsset font)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(OutlineMaterialPath);
            if (existing != null)
                return existing;
            var material = new Material(font.material) { name = "font1 SDF Outline" };
            material.EnableKeyword("OUTLINE_ON");
            material.SetColor(ShaderUtilities.ID_OutlineColor, new Color32(0x20, 0x20, 0x20, 0xFF));
            material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.2f);
            material.SetFloat(ShaderUtilities.ID_OutlineSoftness, 0.05f);
            AssetDatabase.CreateAsset(material, OutlineMaterialPath);
            return material;
        }

        // 앞으로 새로 만드는 TMP 글자도 이 글꼴이 기본이 되게 한다
        static void SetDefaultFont(TMP_FontAsset font)
        {
            var settings = TMP_Settings.instance;
            if (settings == null)
                return;
            var so = new SerializedObject(settings);
            var property = so.FindProperty("m_defaultFontAsset");
            if (property != null && property.objectReferenceValue != font)
            {
                property.objectReferenceValue = font;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(settings);
            }
        }
    }
}
