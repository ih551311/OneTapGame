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

새 UI나 오브젝트가 생길 때마다 이 표에 추가한다.
