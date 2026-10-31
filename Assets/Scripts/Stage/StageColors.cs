using System.Collections.Generic;
using LastBullet.Core;
using LastBullet.Inventory;

namespace LastBullet.Stage
{
    // 전투 장소(맵)마다 얻을 수 있는 물감 색 (2026-10-31 사용자 결정: 숲·마을·사막 순서로 9색을 3색씩 나눈다).
    //   숲 = 빨강·주황·노랑 / 성 마을 = 초록·파랑·보라 / 사막 = 핑크·하양·검정
    //   (2026-10-31 사용자 결정으로 남색을 없애고 핑크를 넣었다. 9색 순서: 빨강 주황 노랑 초록 파랑 보라 핑크 하양 검정)
    // 색을 바꾸고 싶으면 아래 표를 고친다. 적의 물감 색(PaintZone.PickColor)이 이 표를 쓴다.
    // 장소 선택 창의 안내 그림(Assets/UI/StageColors_*.png)은 그림이라 표와 따로 고쳐야 한다.
    public static class StageColors
    {
        static readonly Dictionary<string, PaintColor[]> table = new Dictionary<string, PaintColor[]>
        {
            [SceneNames.Forest] = new[] { PaintColor.Red, PaintColor.Orange, PaintColor.Yellow },
            [SceneNames.CastleVillage] = new[] { PaintColor.Green, PaintColor.Blue, PaintColor.Purple },
            [SceneNames.Desert] = new[] { PaintColor.Pink, PaintColor.White, PaintColor.Black },
        };

        // 그 씬에서 나오는 물감 색. 표에 없는 씬(미술관 등)은 null
        public static IReadOnlyList<PaintColor> For(string sceneName)
        {
            return table.TryGetValue(sceneName, out var colors) ? colors : null;
        }
    }
}
