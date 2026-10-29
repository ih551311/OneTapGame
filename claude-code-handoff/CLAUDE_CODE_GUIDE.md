# Claude Code 기본 사용법

## 세션 시작

- **프로젝트 폴더에서 시작하기**: 작업할 코드가 있는 폴더를 골라 세션을 시작한다. 폴더 없이 시작하면 임시 폴더가 만들어지고, 세션을 지우면 그 폴더도 함께 지워진다.
- **CLAUDE.md**: 프로젝트 폴더 맨 위에 두면 세션이 시작될 때 Claude가 자동으로 읽는다. 매번 반복해서 알려줘야 하는 지침을 여기에 적는다.
- `/init`: 프로젝트를 훑어보고 CLAUDE.md 초안을 만들어 준다.

## 모델 바꾸기

```
/model claude-opus-5-5
```

| 모델 | ID |
|---|---|
| Fable 5.1 | `claude-fable-5-1` |
| Opus 5.5 | `claude-opus-5-5` |
| Sonnet 5.5 | `claude-sonnet-5-5` |
| Haiku 5.5 | `claude-haiku-5-5` |

앱 화면 아래 모델 표시줄을 클릭해서 바꿀 수도 있다.

## 노력 수준 (effort)

- 단계: `low` → `medium` → `high` → `xhigh` → `max`
- 높을수록 더 깊게 생각하지만 시간과 사용량이 늘어난다.
- 앱의 모델 표시줄에서 `... effort` 부분을 클릭해 바꾼다.
- Claude는 자기 세션의 노력 수준을 스스로 바꿀 수 없다.

## 코드 리뷰

| 명령 | 하는 일 |
|---|---|
| `/code-review` | 현재 변경 사항을 리뷰 |
| `/code-review high` | 리뷰 강도 지정 (`low`, `medium`, `high`, `xhigh`, `max`) |
| `/code-review ultra` | 클라우드에서 여러 에이전트가 현재 브랜치를 깊게 리뷰 (요금 부과, 사용자가 직접 입력해야 함) |
| `/code-review ultra <PR번호>` | GitHub PR을 깊게 리뷰 |

- 코드 리뷰는 **Git 저장소**가 있어야 한다.
- `/ultrareview`는 `/code-review ultra`의 예전 이름이다.

## 데스크톱 앱에서 안 되는 명령

`/permissions`, `/config`, `/hooks`는 터미널 전용이라 데스크톱 앱의 Code 탭에서는 열리지 않는다. 필요하면 터미널에서 `claude`를 실행한 뒤 사용한다.

## 확인되지 않은 것

- **Ultracode**: 모델 표시줄에 보이는 항목. 무엇인지, 어떻게 켜는지 아직 확인하지 못했다. 자세한 내용은 `SESSION_HANDOFF.md` 참고.
