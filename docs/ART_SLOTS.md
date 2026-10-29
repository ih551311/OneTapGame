# 내 그림 넣는 곳 (아트 슬롯)

게임에 나오는 그림과 겉모습은 모두 아래 파일 중 하나로 정해진다.
지금 들어 있는 것은 **임시 그림**이다. 직접 그린 것으로 바꾸면 게임에 바로 반영된다.

## UI 그림 바꾸기 (PNG)

1. 그림을 PNG로 저장한다. 배경은 **투명**으로 둔다.
2. `Assets/UI/` 폴더에 **같은 파일 이름으로 덮어쓴다.**
3. 유니티 창을 클릭하면 자동으로 바뀐다.

- 그림 색이 그대로 나온다. 유니티에서 색을 덧칠하지 않는다.
- 크기는 아래 "권장 크기"보다 크게 그려도 된다. 비율은 유지된다.

| 파일 | 어디에 나오나 | 권장 크기 | 비고 |
|---|---|---|---|
| `Assets/UI/Crosshair.png` | 화면 가운데 조준점 | 64×64 | 가운데가 정확히 조준 위치 |
| `Assets/UI/Ring.png` | 탄환 자동 복귀 게이지 (조준점 둘레) | 128×128 | 남은 시간만큼 시계 방향으로 사라진다. 고리 모양 권장 |
| `Assets/UI/Arrow.png` | 화면 밖 탄환 방향 화살표 | 64×64 | **위쪽을 가리키게** 그린다. 게임이 알아서 돌린다 |
| `Assets/UI/HealthBar_Back.png` | 체력바 배경 (왼쪽 아래) | 256×32 | 화면에서는 400×40으로 보인다 |
| `Assets/UI/HealthBar_Fill.png` | 체력바 채움 | 256×32 | 체력만큼 왼쪽부터 채워진다. 배경 테두리 안쪽에 맞게 그린다 |
| `Assets/UI/StageClear.png` | 스테이지 클리어 문구 (화면 가운데) | 1024×256 | 화면에서는 800×200 안에 비율 유지 |
| `Assets/UI/Prompt_StageSelect.png` | 지도 탁자 근처 안내 "[E] 전투 장소 선택" (화면 아래) | 768×96 | |
| `Assets/UI/Popup_Panel.png` | 팝업 창 배경 | 128×128 | **9-slice**, 모서리 32px. 창 크기 900×560으로 늘어난다 |
| `Assets/UI/StageSelect_Title.png` | 팝업 제목 "전투 장소 선택" | 768×128 | |
| `Assets/UI/Button_Normal.png` | 버튼 (보통) | 96×96 | **9-slice**, 모서리 24px. 모든 버튼이 같이 쓴다 |
| `Assets/UI/Button_Highlighted.png` | 버튼 (마우스 올림) | 96×96 | 9-slice 24px |
| `Assets/UI/Button_Pressed.png` | 버튼 (누름) | 96×96 | 9-slice 24px |
| `Assets/UI/Button_Disabled.png` | 버튼 (잠김) | 96×96 | 9-slice 24px |
| `Assets/UI/Button_Close.png` | 팝업 닫기 (X) | 64×64 | |
| `Assets/UI/StageLabel_Training.png` | 버튼 글자 "훈련장" | 512×128 | |
| `Assets/UI/StageLabel_Forest.png` | 버튼 글자 "숲 (잠김)" | 512×128 | |
| `Assets/UI/BagPanel.png` | 전투 가방 표시 배경 (전투 중 오른쪽 위) | 128×128 | **9-slice**, 모서리 24px |
| `Assets/UI/Icon_PaperScrap.png` | 종이조각 아이콘 | 64×64 | |
| `Assets/UI/Icon_Paper.png` | 그림용 종이 아이콘 (미술관 창고 표시) | 64×64 | |
| `Assets/UI/Toast_PaintFull.png` | "가득 참!" 알림 글자 (물감 500 초과 시) | 256×64 | 알림 한 줄의 배경은 BagPanel.png |
| `Assets/UI/Icon_Paint_Red.png` 외 8개 | 물감 아이콘 (Red, Orange, Yellow, Green, Blue, Indigo, Purple, White, Black) | 64×64 | 색마다 한 장. 인벤토리·그림판에서도 같이 쓴다 |

숫자 글자는 TextMeshPro 기본 글꼴(LiberationSans)이다. 글꼴을 바꾸려면 글꼴 파일을 `Assets/UI/Fonts/`에 넣고 알려 주면 된다.

UI는 모두 각 씬의 **`Canvas`** 오브젝트 아래에 있다. 위치나 크기는 Hierarchy에서 `Canvas` 안의 UI를 고르고 Scene 창(2D 보기)에서 끌어서 바꿀 수 있다.

## 오브젝트 겉모습 바꾸기 (머티리얼)

오브젝트마다 전용 머티리얼이 `Assets/Materials/`에 있다.

- **그림(텍스처)만 입히기:** 머티리얼을 클릭 → Inspector의 **Base Map** 왼쪽 네모 칸에 그림 파일을 끌어다 놓는다. Base Map 옆 색은 흰색으로 바꿔야 그림 색이 그대로 나온다.
- **모델로 바꾸기 (Phase 11):** 아래 "겉모습 오브젝트" 자리에 직접 만든 모델을 넣는다. 기능 스크립트는 위쪽 오브젝트에 있어서 영향이 없다.

| 머티리얼 | 쓰는 곳 | 겉모습 오브젝트 (모델 교체 자리) |
|---|---|---|
| `Materials/Player.mat` | 주인공 | `Player/Body`, `Player/Front` |
| `Materials/Gun.mat` | 총 | `Player/Gun` (바꾸면 `Player/Muzzle`을 총구 끝으로 옮긴다) |
| `Materials/Enemy_Charger.mat` | 돌진형 적 (회색 조각상) | 프리팹 `Prefabs/Enemy_Charger`의 `Body`, `Head` (Hitbox 콜라이더 크기도 맞춘다) |
| `Materials/Bullet.mat` | 떨어진 탄환 | 프리팹 `Prefabs/DroppedBullet`의 `Model` |
| `Materials/LightPillar.mat` | 탄환 위 빛 기둥 (반투명) | 프리팹 `Prefabs/DroppedBullet`의 `LightPillar` |
| `Materials/Tracer.mat` | 탄도선, 맞은 자리 점 | `Player/BulletTracer` |
| `Materials/Ground.mat` | 바닥 | `Ground` |
| `Materials/StageMap.mat` | 미술관 지도 탁자 (전투 장소 선택) | `1_Museum` 씬의 `StageMap` |

## 앞으로 생길 UI (만들 때 이 표에 추가)

| UI | 나오는 Phase | 그림 칸 |
|---|---|---|
| 인벤토리, 종이 합성대 | 5 | 패널, 아이콘 (종이), 버튼 |
| 그림판 버튼 (펜 굵기, 색, 지우개, 완성, 취소) | 6~7 | 버튼 보통/마우스 올림/누름, 아이콘 |
| 확인창, 작품 목록 | 7~8 | 패널, 버튼 |
| 타이틀 메뉴, 일시정지 | 9 | 배경, 로고, 버튼 |

버튼과 패널은 **9-slice**로 만든다: 모서리 장식이 늘어나지 않고 가운데만 늘어난다. 그릴 때 모서리를 네 귀퉁이에 몰아서 그리면 된다.

## 3ds Max 모델 넣기 (주인공 등)

1. 3ds Max에서 **FBX**로 내보낸다. 키는 실제 크기 기준 약 **2m**, 피벗(기준점)은 **발바닥**에 둔다.
2. `Assets/Models/`에 넣는다.
3. 넣었다고 알려 주면 Claude가 크기·축 설정을 맞추고 `Player/Body` 자리에 넣는다.

새 UI나 오브젝트가 생길 때마다 이 표에 추가한다.
