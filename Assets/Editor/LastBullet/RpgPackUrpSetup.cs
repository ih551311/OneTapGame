using UnityEditor;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 가져온 에셋 RPG Poly Pack - Lite(Assets/RPGPP_LT)의 머티리얼 3개를 URP용으로 바꾼다.
    // 에셋은 기본(Built-in) 렌더 파이프라인용이라 그대로 두면 핑크색으로 보인다.
    //   rpgpp_lt_mat_a   (Standard)       → URP Lit    (색 팔레트 그림 rpgpp_lt_tex_a를 그대로 쓴다)
    //   rpgpp_lt_sky_a   (Unlit/Texture)  → URP Unlit  (하늘 그림)
    //   rpgpp_lt_cloud_a (Standard, 흰색) → URP Unlit  (구름)
    // 메뉴: LastBullet > 에셋 RPG Poly Pack URP로 바꾸기
    public static class RpgPackUrpSetup
    {
        const string Folder = "Assets/RPGPP_LT/Materials/";

        [MenuItem("LastBullet/에셋 RPG Poly Pack URP로 바꾸기")]
        static void Convert()
        {
            ToLit("rpgpp_lt_mat_a");
            ToUnlit("rpgpp_lt_sky_a");
            ToUnlit("rpgpp_lt_cloud_a");
            AssetDatabase.SaveAssets();
            Debug.Log("[LastBullet] RPG Poly Pack 머티리얼 3개를 URP용으로 바꿨어요.");
        }

        static void ToLit(string name)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>($"{Folder}{name}.mat");
            if (material == null || material.shader.name.StartsWith("Universal"))
                return;
            var texture = material.GetTexture("_MainTex");
            var color = material.GetColor("_Color");
            material.shader = Shader.Find("Universal Render Pipeline/Lit");
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0f); // 로우폴리 느낌: 반짝임 없음
            material.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(material);
        }

        static void ToUnlit(string name)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>($"{Folder}{name}.mat");
            if (material == null || material.shader.name.StartsWith("Universal"))
                return;
            var texture = material.GetTexture("_MainTex");
            var color = material.GetColor("_Color");
            material.shader = Shader.Find("Universal Render Pipeline/Unlit");
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(material);
        }
    }
}
