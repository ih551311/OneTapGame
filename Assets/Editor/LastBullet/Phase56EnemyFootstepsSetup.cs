using LastBullet.Enemy;
using UnityEditor;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 적 프리팹에 발소리를 붙인다 (2026-11-02 사용자 요청). EnemyFootsteps + AudioSource(3D, 직선 감쇠 3~18m).
    //   풀 소리 = Assets/Audio/Footsteps/footstep_01~08, 모래 소리 = sand_01~06 (플레이어 발소리와 같은 소리, 더 작게)
    //   크기·음높이: 돌진형 0.30·0.85(묵직) / 분열 적 0.34·0.78(더 크고 묵직) / 작은 분열체 0.20·1.25(가볍고 높게) / 원거리형 0.22·1.05
    // 다시 실행하면 값을 새로 맞춘다. 메뉴: LastBullet > Phase 56 적 발소리 넣기
    public static class Phase56EnemyFootstepsSetup
    {
        static readonly (string prefab, float volume, float pitch)[] Enemies =
        {
            ("Assets/Prefabs/Enemy_Charger.prefab", 0.30f, 0.85f),
            ("Assets/Prefabs/Enemy_Splitter.prefab", 0.34f, 0.78f),
            ("Assets/Prefabs/Enemy_SplitterMini.prefab", 0.20f, 1.25f),
            ("Assets/Prefabs/Enemy_Thrower.prefab", 0.22f, 1.05f),
        };

        [MenuItem("LastBullet/Phase 56 적 발소리 넣기")]
        static void Create()
        {
            var grass = Load("footstep_", 1, 8);
            var sand = Load("sand_", 1, 6);
            foreach (var (path, volume, pitch) in Enemies)
            {
                var root = PrefabUtility.LoadPrefabContents(path);

                // 원거리형은 이미 던지는 소리용 AudioSource가 있다. 발소리는 따로 자식 오브젝트에 둔다 (소리 크기·거리가 서로 안 섞이게).
                var holder = root.transform.Find("Footsteps");
                if (holder != null)
                    Object.DestroyImmediate(holder.gameObject);
                holder = new GameObject("Footsteps", typeof(AudioSource)).transform;
                holder.SetParent(root.transform, false);
                holder.gameObject.tag = EnemyTagSetup.Tag;

                var audio = holder.GetComponent<AudioSource>();
                audio.playOnAwake = false;
                audio.spatialBlend = 1f;
                audio.rolloffMode = AudioRolloffMode.Linear;
                audio.minDistance = 3f;
                audio.maxDistance = 18f;
                audio.dopplerLevel = 0f;

                var script = holder.gameObject.AddComponent<EnemyFootsteps>();
                var so = new SerializedObject(script);
                Fill(so.FindProperty("grassClips"), grass);
                Fill(so.FindProperty("sandClips"), sand);
                so.FindProperty("volume").floatValue = volume;
                so.FindProperty("pitch").floatValue = pitch;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
                Debug.Log($"[LastBullet] {path}: 발소리를 붙였어요 (크기 {volume}, 음높이 {pitch}).");
            }
            AssetDatabase.SaveAssets();
        }

        static AudioClip[] Load(string prefix, int from, int to)
        {
            var list = new System.Collections.Generic.List<AudioClip>();
            for (int i = from; i <= to; i++)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/Footsteps/{prefix}{i:00}.wav");
                if (clip != null)
                    list.Add(clip);
            }
            return list.ToArray();
        }

        static void Fill(SerializedProperty property, AudioClip[] clips)
        {
            property.arraySize = clips.Length;
            for (int i = 0; i < clips.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
        }
    }
}
