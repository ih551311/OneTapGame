using UnityEditor;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // "내 그림 넣는 곳" 공통 도구.
    // 규칙: UI 그림은 Assets/UI/*.png, 오브젝트 겉모습은 Assets/Materials/<이름>.mat (Base Map에 그림을 넣는다).
    // 사용자는 같은 이름의 PNG로 덮어쓰거나 머티리얼의 Base Map에 그림을 끌어 놓기만 하면 된다.
    // 목록: docs/ART_SLOTS.md
    public static class ArtSlots
    {
        // 모든 UI가 들어가는 캔버스. 예전 이름(HUD)도 찾는다.
        public static GameObject FindCanvas()
        {
            var canvas = GameObject.Find("Canvas");
            return canvas != null ? canvas : GameObject.Find("HUD");
        }

        // PNG를 UI용 스프라이트로 가져오도록 설정한다.
        // border > 0이면 9-slice: 네 모서리 border px은 늘리지 않고 가운데만 늘린다 (버튼, 창).
        public static Sprite LoadSprite(string path, float border = 0f)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var wantBorder = new Vector4(border, border, border, border);
            if (importer != null && (importer.textureType != TextureImporterType.Sprite
                                     || importer.spriteImportMode != SpriteImportMode.Single
                                     || importer.spriteBorder != wantBorder))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single; // Multiple이면 9-slice 모서리가 적용되지 않는다
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.spriteBorder = wantBorder;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // 오브젝트 하나에 쓰는 전용 머티리얼. 없으면 기본 색으로 만든다 (이미 있으면 사용자가 고친 것이므로 그대로 둔다).
        public static Material GetOrCreateMaterial(string name, Color color)
        {
            string path = $"Assets/Materials/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
                return material;

            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
