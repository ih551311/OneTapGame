namespace LastBullet.Core
{
    // 씬 이름을 한 곳에 모아 둔다. 씬 이름을 바꾸면 여기만 고치면 된다.
    // (기획서 3.2의 Hideout = 1_Museum, Stage_Forest = 2_Forest. 훈련장은 2026-10-30에 없앴다)
    public static class SceneNames
    {
        public const string Title = "0_Title";   // 타이틀 메인 메뉴. 게임 시작 씬
        public const string Museum = "1_Museum"; // 미술관 (아지트)
        public const string Forest = "2_Forest"; // 숲 (전투 스테이지)
    }
}
