using UnityEditor;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 가져온 에셋 3D Cartoon Village(Assets/3D Cartoon Village)의 머티리얼을 URP용으로 바꾼다.
    // 에셋은 기본(Built-in) 파이프라인의 Standard 셰이더라서 그대로 두면 핑크색으로 보인다.
    // Standard → URP Lit: 색(_Color → _BaseColor)과 그림(_MainTex → _BaseMap)을 옮기고 반짝임을 0으로 둔다 (만화 같은 느낌).
    // 메뉴: LastBullet > 에셋 3D Cartoon Village URP로 바꾸기
    public static class CartoonVillageUrpSetup
    {
        [MenuItem("LastBullet/에셋 3D Cartoon Village URP로 바꾸기")]
        static void Convert()
        {
            int converted = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/3D Cartoon Village/Materials" }))
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (material == null || material.shader.name.StartsWith("Universal"))
                    continue;

                var texture = material.HasProperty("_MainTex") ? material.GetTexture("_MainTex") : null;
                var color = material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
                material.shader = Shader.Find("Universal Render Pipeline/Lit");
                material.SetTexture("_BaseMap", texture);
                material.SetColor("_BaseColor", color);
                material.SetFloat("_Smoothness", 0f);
                material.SetFloat("_Metallic", 0f);
                EditorUtility.SetDirty(material);
                converted++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[LastBullet] 3D Cartoon Village 머티리얼 {converted}개를 URP용으로 바꿨어요.");
        }
    }
}
