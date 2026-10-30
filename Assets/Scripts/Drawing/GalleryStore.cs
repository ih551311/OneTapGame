using System;
using System.Collections.Generic;
using System.IO;
using LastBullet.Core;
using UnityEngine;

namespace LastBullet.Drawing
{
    [Serializable]
    class GalleryData
    {
        public List<string> frames = new List<string>(); // 액자 칸마다 걸린 작품 ID. 빈 문자열이면 빈 액자
    }

    // 미술관 액자에 걸린 그림 (gallery.json, 기획서 3.5).
    //   SavedData/gallery.json   액자 9칸의 그림 ID
    // 액자는 9칸으로 정해져 있다 (Phase 9). 지금(Phase 8)은 0번 칸 하나만 쓴다.
    public static class GalleryStore
    {
        public const int SlotCount = 9;

        static GalleryData data;

        static string FilePath => Path.Combine(SaveManager.FolderPath, "gallery.json");

        // 칸에 걸린 작품 ID. 비어 있거나 범위 밖이면 빈 문자열.
        public static string GetFrame(int slot)
        {
            if (slot < 0 || slot >= SlotCount)
                return "";
            return Data.frames[slot] ?? "";
        }

        // 칸에 그림을 건다 (id가 빈 문자열이면 비운다). 바로 저장한다.
        public static void SetFrame(int slot, string id)
        {
            if (slot < 0 || slot >= SlotCount)
                return;
            Data.frames[slot] = id ?? "";
            Save();
        }

        static GalleryData Data
        {
            get
            {
                if (data == null)
                    data = Load();
                return data;
            }
        }

        static GalleryData Load()
        {
            GalleryData loaded = null;
            try
            {
                if (File.Exists(FilePath))
                    loaded = JsonUtility.FromJson<GalleryData>(File.ReadAllText(FilePath));
            }
            catch (Exception e)
            {
                Debug.LogError($"[GalleryStore] gallery.json 읽기 실패: {e.Message}");
            }
            loaded ??= new GalleryData();
            while (loaded.frames.Count < SlotCount)
                loaded.frames.Add("");
            return loaded;
        }

        // 임시 파일에 먼저 쓰고 바꿔 넣어서, 쓰다가 꺼져도 예전 파일이 남게 한다 (SaveManager와 같은 방식)
        static void Save()
        {
            try
            {
                Directory.CreateDirectory(SaveManager.FolderPath);
                string temp = FilePath + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(Data, true));
                if (File.Exists(FilePath))
                    File.Replace(temp, FilePath, null);
                else
                    File.Move(temp, FilePath);
            }
            catch (Exception e)
            {
                Debug.LogError($"[GalleryStore] gallery.json 저장 실패: {e.Message}");
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay()
        {
            data = null;
        }
    }
}
