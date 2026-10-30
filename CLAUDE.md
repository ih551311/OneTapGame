# OneTapGame - Claude 작업 지침

## 게임 기획 (가장 먼저 읽기)

- 이 프로젝트는 **LAST BULLET : 마지막 탄환**을 만든다. 폴더 이름만 OneTapGame이다.
- 기획서: [`docs/LAST_BULLET_Plan.md`](docs/LAST_BULLET_Plan.md). 기획은 끝났다. 다시 논의하지 않는다.
- 기획서 **0장 지침**을 그대로 따른다. 특히:
  - 4장 Phase 순서대로, 한 번에 작은 기능 하나만 진행하고 테스트 후 넘어간다.
  - 확정 사양은 바꾸지 않는다. **(기본값)**은 바꾸면 알린다.
  - 1.2 제외 항목은 다시 제안하지 않는다.
  - 실제 동작을 확인하기 전에는 완성이라고 하지 않는다.
  - **매 답변 끝에 현재 Phase와 다음 할 일을 한 줄로 알린다.**
- 진행 상황: [`docs/claude/SESSION_HANDOFF.md`](docs/claude/SESSION_HANDOFF.md)
- **claude.ai 프로젝트 "LAST BULLET"** (2026-10-30 사용자 요청): 사용자는 컴퓨터를 끈 상태에서도 휴대폰의 claude.ai 프로젝트에서 질문한다 ("어떤 리소스가 필요해?", "뭐가 부족해?" 등). 고치는 건 이 Code 세션에서 한다.
  - 프로젝트는 **GitHub 저장소 `ih551311/OneTapGame`의 `main`**과 연결되어 있다 (선택: `docs/`, `CLAUDE.md`, `Assets/Scripts/`, `Assets/Editor/`).
  - **기능이 완료되거나 결정이 바뀔 때마다**: [`docs/claude_project/PROGRESS_SUMMARY.md`](docs/claude_project/PROGRESS_SUMMARY.md)(진행 상황, 필요한 리소스/부족한 것)를 갱신 → 커밋 → 사용자 확인 후 `main`에 올리기(push) → 사용자에게 "claude.ai 프로젝트에서 GitHub 동기화를 눌러 주세요"라고 알린다.

## 역할 분담 (2026-10-29 사용자 결정)

- **당분간 Claude가 거의 다 한다.** 사용자는 유니티에서 확인과 테스트를 한다.
- 씬 오브젝트는 `.unity` 파일을 직접 고치지 않고, `Assets/Editor/`의 에디터 메뉴 스크립트(메뉴 `LastBullet`)로 만든다. 사용자는 메뉴를 한 번 누른다.
- 무엇을 만들었고 왜 그렇게 했는지는 짧게 설명한다. 사용자가 나중에 직접 할 수 있도록 배우는 것이 목표다.
- 사용자가 익숙해지면 직접 하는 비중을 늘린다. 사용자가 원하면 언제든 바꾼다.

## 그림 넣는 곳 (2026-10-30 사용자 결정, 앞으로 만드는 모든 UI·오브젝트에 적용)

사용자가 UI와 겉모습을 나중에 직접 그려서 넣는다. 그래서 새로 만드는 모든 그림 요소에 바꿔 끼울 자리를 만든다.
- **UI**: 그림마다 `Assets/UI/<이름>.png` 임시 그림을 만들고, `Image`가 그 스프라이트를 쓴다. Image 색은 흰색(그림 색 그대로), `preserveAspect` 켬. 색·테두리는 코드가 아니라 PNG 안에 그린다. 스프라이트 설정은 `ArtSlots.LoadSprite` 사용.
  - 체력바 같은 **게이지**: 배경 PNG + 채움 PNG 두 장, 채움은 `Image.Type.Filled`.
  - **버튼·패널·창**: 크기가 늘어나도 모서리가 안 깨지게 9-slice(스프라이트 border)로 설정하고 `Image.Type.Sliced`. 버튼 상태(보통/마우스 올림/누름)도 각각 PNG 칸을 만든다.
  - 글자는 TextMeshPro. 글꼴도 바꿀 수 있게 `Assets/UI/Fonts/`에 둔다.
  - **모든 UI는 씬의 `Canvas` 오브젝트 아래**에 uGUI 오브젝트로 만든다 (사용자가 Scene 창에서 위치를 옮길 수 있게). `OnGUI`, 코드로만 그리는 UI 금지. 에디터 스크립트는 `ArtSlots.FindCanvas()`로 찾는다.
- **플레이어 모델**: 사용자가 **3ds Max**로 만든다 (FBX로 `Assets/Models/`). (2026-10-31부터 임시로 에셋 캐릭터 JellyFishGirl `Assets/Models/Character`를 쓴다. Player 아래 자식 `Model`이고 `PlayerAnimator`가 동작을 정한다.) 모델이 바뀔 때 FBX 가져오기 설정(크기, 축, 피벗)을 같이 맞춘다.
- **오브젝트**: 오브젝트 종류마다 전용 머티리얼 `Assets/Materials/<이름>.mat` (`ArtSlots.GetOrCreateMaterial`). 기본 머티리얼을 같이 쓰지 않는다. 기능 스크립트는 위쪽 오브젝트에, 겉모습은 자식 오브젝트에 둔다 (모델 교체 자리).
- 만들 때마다 [`docs/ART_SLOTS.md`](docs/ART_SLOTS.md) 표에 추가한다.
- **액자와 그림 종이는 정사각형 고정** (2026-10-30 사용자 결정): 그림판 캔버스 `PaintingLibrary.CanvasSize`(256×256), 타이틀 액자, 그림 보기 칸, 앞으로 만들 미술관 액자 9개 모두 정사각형. 정사각형이 아닌 그림은 작품으로 저장하지 않는다.

## 적 규칙 (2026-10-30 사용자 결정)

- **모든 적은 `Enemy` 태그**를 가진다: 프리팹의 맨 위와 자식(몸·머리 등) 전부. 새 적을 만들 때도 붙인다 (`EnemyTagSetup` 메뉴 또는 `EnemyTagSetup.Tag`).
- 적의 기능은 `EnemyHealth`(처치), `Hitbox`(머리/몸 콜라이더), `EnemyAI`, `EnemyDrop` 컴포넌트로 구성한다.
- 헤드샷 원탭 전용: 몸샷은 피해 없음, 탄환이 떨어진다.

## 대화

- 항상 **한국어**로 답한다.
- 사용자는 초보자다. 짧고 쉽게 설명하고, 전문 용어는 처음 쓸 때 풀어서 설명한다.
- 사용자가 직접 해야 하는 일은 최소화한다. 가능한 일은 Claude가 직접 한다.
- 사용자의 짧은 메시지(예: "반짝", "음")는 뜻이 모호하면 짐작으로 단정하지 말고 한 줄로 되묻는다.
- 확인하지 못한 내용은 사실처럼 말하지 않는다. 모르면 모른다고 말한다.
- 화면 상태에 관한 질문은 캡처를 요청해 직접 보고 판단한다.

## 프로젝트

- 유니티 버전: **6000.3.23f1** (Unity 6)
- 렌더 파이프라인: URP 17.3.0
- 입력: Input System 1.20.0
- 씬 (2026-10-30 사용자 요청으로 번호 + 영어 이름. 기획서 3.2의 이름과 다르다):
  - `Assets/Scenes/0_Title.unity` = 기획서 `MainMenu` (타이틀, 게임 시작 씬, 빌드 0번)
  - `Assets/Scenes/1_Museum.unity` = 기획서 `Hideout` (미술관)
  - `Assets/Scenes/2_Forest.unity` = 기획서 `Stage_Forest` (숲, 전투 스테이지)
  - `Assets/Scenes/3_CastleVillage.unity` = 성 마을 (전투 장소 2, 에셋 `Assets/3D Cartoon Village`)
  - `Assets/Scenes/4_Desert.unity` = 사막 (전투 장소 3, 에셋 `Assets/Tiny Teacup Studio`)
  - 물감 색은 맵마다 3색 (2026-10-31 사용자 결정): 숲 빨강·주황·노랑 / 성 마을 초록·파랑·남색 / 사막 보라·하양·검정. 표는 `Assets/Scripts/Stage/StageColors.cs`.
  - 훈련장(2_Training)은 2026-10-30 사용자 결정으로 없앴다 (훈련장 없이 바로 숲). 숲은 에셋 `Assets/RPGPP_LT`(RPG Poly Pack - Lite)로 꾸몄다 (출처는 `Assets/Credits.txt`).
  - 게임 코드는 씬 이름을 `LastBullet.Core.SceneNames`에서만 쓴다.
- 패키지: Cinemachine 3.1.7. TextMeshPro는 Unity 6에서 uGUI(`com.unity.ugui`)에 포함되어 있다.
- GitHub: https://github.com/ih551311/OneTapGame (기본 브랜치 `main`)

## 환경

- OS: Windows 11 Home, 셸: PowerShell 5.1
  - `&&`, `||` 사용 불가 → `A; if ($?) { B }` 형태로 쓴다.
- **`git` 명령이 PATH에 없다.** 깃 GUI로 Fork를 쓰고 있으므로 Fork에 들어 있는 git을 쓴다.

```powershell
& "C:\Users\ih551\AppData\Local\Fork\gitInstance\2.50.1\bin\git.exe" status
```

## 유니티 에디터 조작 (2026-10-30 연결)

- **Unity MCP**(`unity-editor-mcp`, 프로젝트 루트 `.mcp.json`)와 **Unity CLI**(`C:\Users\ih551\AppData\Local\Unity\bin\unity.exe`), 프로젝트 패키지 `com.unity.pipeline`으로 열린 에디터를 직접 조작한다. 화면 클릭(computer-use)으로 메뉴를 누르지 않는다.
- 먼저 `unity status --format json`으로 에디터 연결(`state: ready`)을 확인한다. 에디터가 꺼져 있으면 `unity open C:\Users\ih551\OneTapGame`.
- 메뉴 실행: MCP 도구 `invoke_menu` 또는 `unity command invoke_menu --path "LastBullet/..."`.
- 컴파일: `recompile` → `recompile_status` (에디터 창이 뒤에 있어도 된다). 씬 화면 확인: `scene_view`. 코드 실행: `eval`.
- 연결이 안 되면 컴파일 오류로 Safe Mode일 수 있다: `unity pipeline list`로 확인 후 오류를 고친다.
- MCP 도구는 세션을 새로 시작해야 보인다. 그 전에는 위 CLI 명령으로 같은 일을 한다.

## 유니티 작업 규칙

- `Library/`, `Temp/`, `Logs/`, `UserSettings/`는 유니티가 자동으로 만드는 폴더다. 직접 고치거나 커밋하지 않는다.
- `Assets/` 안에 파일을 만들면 유니티가 `.meta` 파일을 만든다. `.meta` 파일도 함께 커밋한다.
- 문서(`.md`)는 `Assets/` 밖(프로젝트 루트나 `docs/`)에 둔다.
- 스크립트는 `Assets/Scripts/` 아래에 둔다.

## 작업 방식

- 파일을 고치기 전에 먼저 읽고, 무엇을 바꿀지 설명한다.
- 되돌리기 어려운 작업(삭제, 덮어쓰기, 푸시, 외부 전송)은 실행 전에 사용자에게 확인한다.
- 작업 결과는 그대로 보고한다. 실패하면 실패했다고 말한다.
- **커밋 메시지에 `Co-Authored-By: Claude ...` 줄을 넣지 않는다** (2026-10-30 사용자 요청: GitHub·Fork에 "ih551311 및 claude 작성함"으로 뜨지 않게). PR 설명에도 Claude Code 표시를 넣지 않는다.
- 참고 문서: `docs/claude/`


