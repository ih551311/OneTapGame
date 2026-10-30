using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace LastBullet.EditorTools
{
    // Assets/UI/Fonts/ 안의 글꼴 파일(.ttf, .otf)마다 TextMeshPro 글꼴 에셋("<이름> SDF.asset")을 만든다 (2026-10-31 사용자 요청).
    //  - 글자를 쓸 때 필요한 글자만 그때그때 아틀라스에 그리는 "동적(Dynamic)" 방식이라 한글 전체를 미리 담지 않아도 된다.
    //  - 이미 만들어진 에셋은 건너뛴다. 글꼴 파일을 새로 넣었을 때 다시 실행하면 된다.
    // TextMeshPro 글자(TMP_Text)의 Font Asset 칸에 이 에셋을 끼우면 그 글꼴로 나온다.
    // 메뉴: LastBullet > 글꼴 에셋 만들기 (Assets/UI/Fonts)
    public static class FontAssetSetup
    {
        const string Folder = "Assets/UI/Fonts";

        [MenuItem("LastBullet/글꼴 에셋 만들기 (Assets/UI/Fonts)")]
        static void Create()
        {
            int made = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Font", new[] { Folder }))
            {
                string fontPath = AssetDatabase.GUIDToAssetPath(guid);
                string name = Path.GetFileNameWithoutExtension(fontPath);
                string assetPath = $"{Folder}/{name} SDF.asset";
                if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath) != null)
                    continue;

                var font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
                // 글자 크기 90, 가장자리 여백 9, 1024×1024 아틀라스 (아틀라스가 차면 새 아틀라스가 자동으로 늘어난다)
                var fontAsset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
                fontAsset.name = $"{name} SDF";
                AssetDatabase.CreateAsset(fontAsset, assetPath);
                fontAsset.atlasTexture.name = $"{name} Atlas";
                fontAsset.material.name = $"{name} Material";
                AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
                EditorUtility.SetDirty(fontAsset);
                made++;
                Debug.Log($"[LastBullet] 글꼴 에셋을 만들었어요: {assetPath}");
            }
            AssetDatabase.SaveAssets();
            if (made == 0)
                Debug.Log("[LastBullet] 새로 만들 글꼴이 없어요 (Assets/UI/Fonts에 .ttf/.otf를 넣으세요).");
        }
    }
}
