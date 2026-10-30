using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LastBullet.Core
{
    // save.json의 내용 (기획서 3.5)
    [Serializable]
    public class SaveData
    {
        public int paperScraps;
        public int drawingPaper;
        public PaintAmounts paints = new PaintAmounts();
        public List<string> unlockedStages = new List<string> { "Forest" };
        public bool endingSeen;

        // 타이틀 화면 액자 2개(왼쪽, 오른쪽)에 건 그림의 작품 ID. 빈 문자열이면 빈 액자. (사용자 요청, 2026-10-30)
        public List<string> titleFrames = new List<string> { "", "" };
    }

    // 물감 9색 양. JSON에서 "red": 350 처럼 보이도록 색마다 칸을 둔다.
    [Serializable]
    public class PaintAmounts
    {
        public int red, orange, yellow, green, blue, indigo, purple, white, black;

        // PaintColor 순서(빨강~검정)대로 읽고 쓴다
        public int this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return red; case 1: return orange; case 2: return yellow;
                    case 3: return green; case 4: return blue; case 5: return indigo;
                    case 6: return purple; case 7: return white; default: return black;
                }
            }
            set
            {
                switch (index)
                {
                    case 0: red = value; break; case 1: orange = value; break; case 2: yellow = value; break;
                    case 3: green = value; break; case 4: blue = value; break; case 5: indigo = value; break;
                    case 6: purple = value; break; case 7: white = value; break; default: black = value; break;
                }
            }
        }
    }

    // 저장·불러오기 (기획서 3.5). 위치: Application.persistentDataPath/SavedData/save.json
    // 처음 Data를 읽을 때 파일에서 불러오고, 없거나 깨졌으면 새 데이터로 시작한다.
    // 주요 상태가 바뀔 때 Save()를 부른다 (마우스 이벤트마다 부르지 않는다).
    public static class SaveManager
    {
        static SaveData data;

        public static string FolderPath => Path.Combine(Application.persistentDataPath, "SavedData");
        public static string SavePath => Path.Combine(FolderPath, "save.json");

        public static SaveData Data
        {
            get
            {
                if (data == null)
                    data = Load();
                return data;
            }
        }

        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(FolderPath);
                // 임시 파일에 먼저 쓰고 바꿔치기: 쓰는 도중에 꺼져도 기존 저장이 깨지지 않는다
                string temp = SavePath + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(Data, true));
                if (File.Exists(SavePath))
                    File.Replace(temp, SavePath, null);
                else
                    File.Move(temp, SavePath);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] 저장 실패: {e.Message}");
            }
        }

        static SaveData Load()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    var loaded = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
                    if (loaded != null)
                    {
                        Debug.Log($"[SaveManager] 불러옴: {SavePath}");
                        return loaded;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] 불러오기 실패, 새로 시작: {e.Message}");
            }
            return new SaveData();
        }

        public static bool IsStageUnlocked(string stage) => Data.unlockedStages.Contains(stage);

        public static void UnlockStage(string stage)
        {
            if (IsStageUnlocked(stage))
                return;
            Data.unlockedStages.Add(stage);
            Debug.Log($"[SaveManager] 스테이지 해금: {stage}");
            Save();
        }

        public const int TitleFrameCount = 2;

        public static string GetTitleFrame(int index)
        {
            var frames = Data.titleFrames;
            return frames != null && index < frames.Count ? frames[index] : "";
        }

        // 지워진 작품이 타이틀 액자에 걸려 있었다면 비운다 (PaintingLibrary.Delete가 부른다)
        public static void ClearTitleFramesWith(string paintingId)
        {
            if (Data.titleFrames == null)
                return;
            bool changed = false;
            for (int i = 0; i < Data.titleFrames.Count; i++)
            {
                if (Data.titleFrames[i] == paintingId)
                {
                    Data.titleFrames[i] = "";
                    changed = true;
                }
            }
            if (changed)
                Save();
        }

        // 타이틀 액자에 그림을 건다 (index 0 = 왼쪽, 1 = 오른쪽)
        public static void SetTitleFrame(int index, string paintingId)
        {
            if (Data.titleFrames == null)
                Data.titleFrames = new List<string>();
            while (Data.titleFrames.Count < TitleFrameCount)
                Data.titleFrames.Add("");
            Data.titleFrames[index] = paintingId ?? "";
            Save();
        }

        // 저장 파일을 지운다 (테스트 메뉴, 나중에 '새 게임'에서 쓴다)
        public static void DeleteSave()
        {
            if (File.Exists(SavePath))
                File.Delete(SavePath);
            data = null;
        }

        // 재생을 시작할 때 이전 재생의 값이 남지 않게 한다
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay()
        {
            data = null;
        }
    }
}
