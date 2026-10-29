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
- 씬: `Assets/Scenes/Hideout.unity` (미술관), `SampleScene.unity` (템플릿 기본 씬)
- 패키지: Cinemachine 3.1.7. TextMeshPro는 Unity 6에서 uGUI(`com.unity.ugui`)에 포함되어 있다.
- GitHub: https://github.com/ih551311/OneTapGame (기본 브랜치 `main`)

## 환경

- OS: Windows 11 Home, 셸: PowerShell 5.1
  - `&&`, `||` 사용 불가 → `A; if ($?) { B }` 형태로 쓴다.
- **`git` 명령이 PATH에 없다.** 깃 GUI로 Fork를 쓰고 있으므로 Fork에 들어 있는 git을 쓴다.

```powershell
& "C:\Users\ih551\AppData\Local\Fork\gitInstance\2.50.1\bin\git.exe" status
```

## 유니티 작업 규칙

- `Library/`, `Temp/`, `Logs/`, `UserSettings/`는 유니티가 자동으로 만드는 폴더다. 직접 고치거나 커밋하지 않는다.
- `Assets/` 안에 파일을 만들면 유니티가 `.meta` 파일을 만든다. `.meta` 파일도 함께 커밋한다.
- 문서(`.md`)는 `Assets/` 밖(프로젝트 루트나 `docs/`)에 둔다.
- 스크립트는 `Assets/Scripts/` 아래에 둔다.

## 작업 방식

- 파일을 고치기 전에 먼저 읽고, 무엇을 바꿀지 설명한다.
- 되돌리기 어려운 작업(삭제, 덮어쓰기, 푸시, 외부 전송)은 실행 전에 사용자에게 확인한다.
- 작업 결과는 그대로 보고한다. 실패하면 실패했다고 말한다.
- 참고 문서: `docs/claude/`
