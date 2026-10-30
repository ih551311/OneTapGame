# 세션 인계 요약

작성일: 2026-10-29

## 지금까지의 상태

- (2026-10-31 갱신) 프로젝트는 `C:\Users\ih551\OneTapGame`, 브랜치는 `main` 하나뿐이다. 게임은 LAST BULLET이고 씬은 `0_Title` `1_Museum` `2_Forest` `3_CastleVillage` `4_Desert` 다섯 개다.
- 이 문서 아래쪽 표의 앞부분(Phase 1~)은 2026-10-29 시점의 기록이고, **최신 진행은 문서 맨 아래의 날짜별 섹션과 `docs/claude_project/PROGRESS_SUMMARY.md`가 가장 정확하다.**
- 2026-10-31 전체 검토: 에러·끊어진 연결 없음(5개 씬), 윈도우 빌드 성공(289MB, 경고는 Unity 내부 셰이더 1건)과 빌드 실행·저장 불러오기 정상, 성능 양호(에디터 119~163fps), 깃 가장 큰 추적 파일 4MB. 고친 것: 원거리 적 `ThrowPoint` Enemy 태그, 오래된 문서(PROGRESS_SUMMARY).

## 게임 개발 진행 (LAST BULLET)

기획서: `docs/LAST_BULLET_Plan.md`

| Phase | 상태 |
|---|---|
| 1 프로젝트 설정 | ✅ 완료 (2026-10-29, 브랜치 `phase1/project-setup`) |
| 2 플레이어 | ✅ 완료 (2026-10-29, 사용자가 WASD·Shift·Space·Q·카메라 확인). 2-1 바닥·캡슐·CharacterController ✅. 2-2 3인칭 카메라(Cinemachine ThirdPersonFollow + `ThirdPersonCamera`)·크로스헤어 ✅ (오류 없음, 마우스 회전은 사용자 실제 확인 필요). 2-3 이동·달리기·점프·회피(`PlayerMovement`, 화면 왼쪽 위 테스트 표시) ✅ |
| 3 한 발 탄환 | 진행 중. 3-1 발사·명중 판정(`OneBulletGun`, 탄도선, 맞은 자리 노란 점, 임시 과녁 `TestTargets (임시)`) 작성, 조준점 일치 확인 ✅. 최대 사거리 50m (기본값). 3-2 `Hitbox`(Body/Head), 적 명중 → Loaded, 빗나감 → `Prefabs/DroppedBullet` 바닥에 생성 + Dropped, 임시 허수아비 `Target_Dummy` 작성 ✅. 3-3 `BulletPickup`(E키, 2m 기본값)·`BulletAutoReturn`(15초)·`BulletIndicatorUI`(크로스헤어 둘레 노란 고리 `UI/Ring.png`) 작성, E키 회수 사용자 확인 ✅. 3-4 화면 밖 방향 화살표(`BulletIndicatorUI`, `UI/Arrow.png`)·빛 기둥(프리팹 `LightPillar`, 6m 반투명 `Materials/LightPillar`) 작성, 빛 기둥·화살표 사용자 확인 ✅. 사용자 요청으로 벽 자국 2초 → 0.4초. 2026-10-30 사용자 결정으로 자동 복귀 15초 → **항상 7초** (스크립트·프리팹·기획서 수정). ✅ 완료 (맵 밖 낙하 복귀는 코드상 보장, 사용자 직접 테스트는 생략) |
| 4 전투 (돌진형) | ✅ 완료 (2026-10-30: 헤드샷 즉사·몸샷 탄환 낙하 사용자 확인, 처치 1회 보장, 7대 사망·3마리 클리어 로그 확인). 4-1 `Enemy_Charger` 프리팹(몸 상자·큰 머리 Hitbox, `EnemyHealth` 30, 헤드샷 즉사·몸 15, 맞으면 빨갛게 깜빡, 처치 시 사라짐), Hideout에 `TestEnemies (임시)` 3마리. 4-2 `EnemyAI`(감지 15m·추적 4m/s·공격 거리 4m·예고 0.5초 뒤로 젖힘·돌진 9m/s 0.5초·숨 고르기 0.8초·피격 멈칫 0.25초, 수치 대부분 기본값), NavMesh(`NavMesh` 오브젝트, `Scenes/Hideout/NavMesh-Hideout.asset`, Player는 굽기 제외) 작성, 사용자 확인 ✅. 4-3 `PlayerHealth`(100, 회피 무적 시 무시, 사망 시 조작 정지·쓰러짐·2초 뒤 씬 재시작 — Phase 5에서 RunBag 삭제·Hideout 복귀로 교체), 돌진 피해 15(돌진당 1회, 닿는 거리 1.3m), 체력바 `BattleUI` + `UI/HealthBar_Back·Fill.png`, 사용자 요청으로 `TestTargets` 삭제 + NavMesh 다시 굽기, 빌드 목록에 Hideout. 7대 맞고 사망 → 재시작 로그 확인. 회피 무적 사용자 확인 ✅. 4-4 `Stage_Training` 씬(Hideout 복사, 돌진형 3마리, 전용 NavMesh, `StageManager` 전멸 시 `UI/StageClear.png` 표시 → 3초 뒤 Hideout, 사망 시 Hideout), Hideout에서 적 제거, 빌드 목록 [Hideout, Stage_Training]. UI 캔버스 이름 HUD → `Canvas` (모든 UI는 Canvas 아래 규칙). 클리어 로그 확인. 4-5 (사용자 제안): 게임은 미술관에서 시작, 지도 탁자 `StageMap`에서 E → 전투 장소 선택 팝업(`StageSelectPopup`, 훈련장 버튼, 숲 잠김, 닫기/ESC), `PlayerControlLock`(팝업 중 이동·카메라·사격 정지, 커서 표시), EventSystem. 팝업 그림 전부 `Assets/UI` (ART_SLOTS.md). 메뉴 실행 완료 (E 팝업 동작은 사용자 확인 대기). ThirdPersonCamera의 ESC 커서 해제 제거. 다음: Phase 4 완료 기준 점검 → Phase 5 |

| 5 재료 | ✅ 완료 (2026-10-30: 사망 시 그 판 재료만 사라짐, 9색 랜덤 드롭, 재실행 후 유지 — 모두 사용자 확인).
| 6 그림판 | **진행 중 (2026-10-30 사용자 요청으로 재개)**. 6-1: 미술관 `PaintWorkbench`(4, 0.5, 3) + `PaintBoardPopup`(`Drawing/PaintBoardPopup.cs`, 메뉴 `Phase 6-1 그림 작업대와 그림판 만들기`, `Phase6PaintBoardSetup`). 모양은 사용자가 준 참고 그림(크림색 창, 왼쪽 도구 막대, 정사각형 종이, 색 9개, 취소/완성)을 따름. 도구는 기획서대로 펜 굵기 4단계(2/5/10/20) + 지우개(참고 그림은 3개). 도구·색 선택, 종이 수·물감 잔량 표시, 흰 종이(256×256, 화면에 2배), 취소=닫기까지. 완성 버튼은 Phase 7까지 잠금. 그림은 `Assets/UI/Paint_*.png`(임시). UI 모양 사용자 확인 ✅. 6-2: `PaintBoardPopup`이 마우스로 그림(Color32[] 256×256, 1px 간격 보간 + 동그란 점 찍기, 프레임당 `Apply` 1회), 물감 소비 = 선 길이×굵기×0.01(소수 누적, `InventoryManager.SpendPaint`로 정수 차감, 0이면 차단), 지우개는 흰색·물감 없음, 5초 자동 저장·닫을 때 저장(`CommitToSave`). 사용자 그리기·완료 기준 확인 ✅ (물감 0이면 점도 못 찍게 수정). **Phase 6 ✅ 완료**. 7-1: 완성 버튼(종이 있을 때만 활성) → `PaintingLibrary.Add`(PNG + paintings.json) 성공 후 `InventoryManager.UseDrawingPaper()`로 종이 1장 차감 → 팝업 닫기. 저장 실패 시 종이 유지. 사용자 확인 ✅. 7-2: 취소 버튼·ESC → 확인창(`ConfirmDialog`, 메뉴 `Phase 7-2 그림판 취소 확인창 만들기`) [계속 그리기]/[그만두기]. 그만두기 = 그림 버리고 닫기(물감 미반환·종이 미사용), 확인창 중 ESC = 계속 그리기, 확인창 동안 그리기 차단. `PopupBase.RequestClose()` 추가(다른 팝업은 바로 닫힘). 컴파일·씬 연결 확인, **사용자 테스트 대기**. (확인창이 안 뜬 건 사용자가 씬에서 Panel을 끄고 ConfirmDialog를 켠 채 Play해서였음, 씬 저장본은 정상.) 사용자 요청 추가(2026-10-30): ① **ESC 일시정지 창** `PauseMenu`(미술관·훈련장 `Canvas/PauseMenu`, 메뉴 `ESC 일시정지 창 만들기`): 열린 팝업이 없을 때 ESC → [계속하기]/[타이틀로 나가기], 창이 뜬 동안 `Time.timeScale` 0, 훈련장에서 나가면 RunBag.Clear(사망과 같음, 안내 문구 표시). 훈련장에 EventSystem 추가. `InteractDevice`는 일시정지 중 E 무시. 컴파일·씬 연결 확인, **사용자 테스트 대기**. ② 그림판 아래 줄이 [취소][내 그림][완성]이 됨. [내 그림] → 미술관 `GalleryPopup`(타이틀 그림 보기 재사용, `viewOnly`: 걸기 버튼 숨김, 그림을 누르면 `LargeView`로 크게 보기, 메뉴 `그림판 내 그림 보기 만들기`). `PopupBase`가 팝업 겹침을 지원(열린 팝업 스택: 맨 위 팝업만 ESC 처리, 모두 닫혀야 조작 복구). 사용자 확인 ✅(내 그림 보기, ESC 일시정지, 확인창). 7-3 점검: 재생 중 물감 1 소비 → 5초 뒤 save.json에 반영 확인(에디터 창이 뒤에 있으면 게임 프레임이 멈춰 자동 저장이 안 돌므로 점검 때 run-in-background를 잠시 켰다 복구함), 그림판에서 그리기만 한 상태에서는 Paintings 폴더 PNG가 늘지 않음(완성 때만 저장) 확인, 테스트로 쓴 물감 1은 되돌림. **Phase 7 ✅ 완료**. Phase 8 액자 전시: 미술관 `PictureFrame_01`(0, 0, 12, 정사각형 2.4 / 그림 면 2.0, 기능은 위·`Frame`·`Picture` 자식, 메뉴 `Phase 8 미술관 액자 만들기`) 앞에서 E → `FramePickerPopup`(`GalleryPopup`의 `frameMode`, 그림을 누르면 바로 걸림·지금 걸린 그림에 표시) → `GalleryStore`(gallery.json, 9칸 `frames`, 지금은 0번 칸만, 임시 파일 후 교체 저장) → 면의 `_BaseMap` 교체. 재생 중 걸기→gallery.json 기록, 재실행 후 같은 그림·텍스처 복구, 화면에 그림이 보이는 것까지 확인(테스트용 gallery.json은 지움). **사용자 테스트 대기** (E→목록→선택). 사용자 확인 ✅(M1). **Phase 9-1**: 액자 9개 `PictureFrames/PictureFrame_01~09`(X -12.8~12.8 간격 3.2, Z 12, 메뉴 `Phase 9-1 액자 9개와 진행도 만들기`; Phase 8 메뉴는 삭제), `GalleryManager`(가장 가까운 액자 하나만 안내·E로 `FramePickerPopup` 열기, `PictureFrame`은 그림 표시만), `GalleryStore.DistinctCount()`/`Changed`, 화면 위 가운데 `FrameProgress` "서로 다른 작품 N / 9"(같은 그림은 한 번만 셈). 재생 중 3종 + 중복 1칸으로 3/9, 9개 액자 줄·그림 표시 확인(테스트용 gallery.json은 지움). **사용자 테스트 대기**. 참고: 에디터에서 씬에 저장 안 된 변경이 있으면 메뉴 스크립트가 "저장할까요?" 창을 띄워 자동 실행이 멈춘다. 다음: 9-2 서로 다른 ID 9개 감지 → 확인창 → 엔딩. **(2026-10-30 사용자 요청으로 전투 변경을 먼저 진행)**: 훈련장 없이 바로 숲으로, 에셋 RPG Poly Pack - Lite 사용. 에셋을 `Assets/RPGPP_LT`에 가져옴(사용자), 머티리얼 3개를 URP로 변환(메뉴 `에셋 RPG Poly Pack URP로 바꾸기`, 핑크 없이 초록·갈색 정상 확인), `Assets/Credits.txt` 기록. 적 물감 드롭 50→30. **숲 완료(2026-10-30, 사용자가 자리를 비운 사이 진행)**: `2_Forest`(메뉴 `Phase 10 숲 씬 만들기`: 훈련장 복사 → 땅 교체) = 풀 타일 6×6 + 아래 평평한 `Floor` + `FloorFar`, 길 7조각, 나무(가장자리 2겹 80 + 안쪽 55), 바위 18, 덤불·꽃·풀·작은 돌, 마을(건물 2·창고 2·우물·수레·울타리·상자·통), 야영지, 보이지 않는 벽 4면, 바깥 언덕 10·산 6·하늘 돔·구름 12, 해 방향·색. 에셋 프리팹엔 Collider가 없어 나무=캡슐, 바위·건물=박스를 붙임. 작은 장식·배경은 NavMesh에서 제외, 전용 NavMesh 데이터(`Scenes/2_Forest/`). 돌진형 7마리(플레이어 시작 (0,-44) 기준 30m+ 떨어짐), 전부 NavMesh 위·경로 확인. `SceneNames.Forest`, `StageSelectPopup`은 숲 버튼 하나(처음부터 열림), `StageManager.unlockOnClear` 비움. **훈련장 씬·NavMesh·글자 그림 삭제**(메뉴 `Phase 10 훈련장을 숲으로 바꾸기`), 빌드 목록 [0_Title, 1_Museum, 2_Forest]. 재생 확인: 미술관→지도 탁자 팝업(숲 하나)→숲 로드(적 7)→7마리 처치 시 클리어·재료 정산(종이조각 7, 물감 210 = 7×30)→미술관 복귀(테스트로 바뀐 save.json은 원래대로 복구). 하늘 돔이 검게 나오던 문제(Unlit 변환 시 색 0) 수정. **사용자 확인 대기**(직접 걸으며 보기·적 7마리 전투·분위기). 알려진 점: 원거리형·방어형 적 없음(Phase 10 나머지), 건물·소품은 에셋 그대로(모델 교체 자리는 `Environment` 묶음). 다음: 사용자 확인 후 9-2 엔딩. **(사용자 결정 2026-10-30: 엔딩은 나중으로 미루고, 다른 것을 계속 추가한다. 9-2~9-5 대기.)** **Phase 10 적 2종 완료**(사용자 선택 "원거리형·방어형"): ① 방어형 `Enemy_Shield`(돌진형 복사, 메뉴 `Phase 10 방어형·원거리형 적 만들기`): `EnemyShield`(정면 ±60°면 방패가 막음, 머리·몸 모두, 막힌 탄환은 `HitResult.Blocked` → `OneBulletGun`이 탄환을 떨어뜨리지 않고 즉시 Loaded, 방패 번쩍), 옆·뒤 헤드샷은 즉사. `Hitbox.TakeHit(Vector3 shooterPosition)`가 `HitResult`(Hurt/Killed/Blocked)를 돌려주도록 바뀜. `EnemyAI`에 `turnSpeed` 추가(방어형 55°/초, 돌진형 0=즉시), 방어형 수치: 3m/s·공격 20·예고 0.6초·돌진 7m/s·숨 고르기 1초. ② 원거리형 `Enemy_Thrower`(소형, `RangedEnemyAI`): 거리 9~14m 유지(가까우면 물러나고 멀면 다가옴), 2.5초마다 0.5초 예고 뒤 돌(`EnemyProjectile`, 12m/s, 피해 12, 던질 때 플레이어 자리로, SphereCast, 다른 적은 통과) 던짐. ③ 숲 = 돌진형 3 + 원거리형 2 + 방어형 2(자리는 기존 7곳). 재생 확인: 방어형 정면 머리·50° 몸 → Blocked, 70° 몸 → Hurt, 뒤 머리 → Killed / 원거리형 12.9m 유지·2.5초마다 -12 / 3.9m에서 9.2m로 물러남 / 18m에서 13.8m까지 다가옴. 직접 쏘는 탄환 경로(방패 막힘 → 즉시 재사용)는 코드로만 확인, **사용자 플레이 확인 대기**. 아직 없음: 이 두 적의 애니메이션·효과음·3ds Max 모델. **추가(2026-10-30)**: ① 처치 연출 `PaintBurst`(메뉴 `처치 연출(물감 터짐) 만들기`, `Effects/PaintBurstEffect`): 머리(`Head`)에서 떨어뜨린 물감 색의 방울 46개(ParticleSystem, 공 메쉬, 땅에 튕김) + 바닥 자국 `Puddle`(0.2초 번짐→3.5초 뒤 줄어 5.5초에 사라짐), `EnemyDrop`의 `burstPrefab`/`burstPoint`로 적 3종 연결. 재생에서 방울과 자국이 보이는 것 확인. ② **적 스포너**(사용자 요청, 메뉴 `Phase 10 적 스포너 만들기`): `Enemy/EnemySpawner`(`DefaultExecutionOrder(-100)`이라 StageManager보다 먼저 Start에서 적을 만듦, NavMesh 위로 맞춤, 플레이어 쪽 바라보기 옵션, Scene 창 기즈모 표시) — 숲 `Spawners/Spawner_Charger_01~03, Spawner_Shield_01~02, Spawner_Thrower_01~02`, 만들어진 적은 `Enemies` 아래 `Enemy_종류_번호`. 재생: 적 7마리 생성·전부 NavMesh 위·StageManager가 7로 셈. ③ 원거리형 탄속 12 → 16(프리팹·코드 기본값). ④ 적 감지 범위(사용자 요청): 돌진형·방어형 15 → 25m, 원거리형 22 → 30m(코드 기본값 + 3개 프리팹), 재생에서 22m 거리 돌진형이 추적 시작하는 것 확인. ⑤ **적 수 2배(사용자 요청 "적이 너무 없다")**: 메뉴 `Phase 10 적 스포너 늘리기`(`Phase10MoreSpawnersSetup`)로 스포너 7개 추가 → 14마리(돌진형 6·방어형 4·원거리형 4). 새 자리 규칙: NavMesh 위, 플레이어 시작에서 30m+, 다른 스포너에서 9m+, 숲 안쪽(고정 시드라 결과 같음). 재생 확인: 14마리 전부 생성·NavMesh 위, 시작에서 가장 가까운 적 32.9m, 5초 뒤 체력 100. 더 늘리려면 코드 상단 `AddCharger/AddShield/AddThrower`를 바꾸고 메뉴를 다시 누르면 이어서 번호가 붙음. 클리어 시간·난이도는 사용자 플레이 확인 대기. ⑥ **스테이지 클리어 시스템 폐지·자유 파밍(사용자 결정 2026-10-30, "지역마다 얻는 색을 나눌 예정, 지금은 없음")**: `StageManager.cs`·`Canvas/StageClear`·`StageClear.png` 삭제(메뉴 `Phase 10 스테이지 클리어 없애고 자유 파밍으로`, `Phase10FreeFarmSetup`), 전투 가방은 `RunBag` 오브젝트로 남김. `EnemySpawner`가 적 사망 후 `respawnDelay` 60초 뒤 다시 만듦(`respawnSafeDistance` 20m 안에 플레이어가 있으면 멀어질 때까지 대기, 스포너별 조정 가능). ESC 창(숲)에 **[미술관으로 귀환]**(`PauseMenu.homeButton`, 재료 `RunBag.Settle()` 후 1_Museum, 창 800×700) — 타이틀로 나가기는 재료 손실. 낡은 `Phase5BagSetup`의 만들기 메뉴는 삭제하고 도우미(CreateIcon/CreateCount)만 남김. 재생 확인: 스포너 리스폰(3초 설정, 플레이어 10m일 땐 대기 → 멀어지자 생성), 귀환 시 창고 종이조각 +2·물감 반영·timeScale 1 복구(테스트로 바뀐 save.json은 복구). 참고: `2_Forest.unity`는 처음부터 유니티가 바이너리로 저장함(텍스트 diff 불가, 동작엔 문제 없음). 다음 예정: 지역별 물감 색 분배(사용자 설계 대기). **버그 수정(사용자 보고 "ESC 창 버튼이 안 눌려, ESC 누르면 씬이 멈추게")**: 숲 Player에 `PlayerControlLock`이 없었음(훈련장은 미술관에 이 컴포넌트가 추가되기 전에 복사된 것이라 빠짐) → ESC 창이 떠도 마우스 커서가 잠긴 채라 버튼을 못 누르고, 플레이어 이동·카메라·사격이 멈추지 않았음. 숲 Player에 `PlayerControlLock` 추가(씬 저장, `Phase10ForestSetup`에도 반영). 재생 확인: ESC(Pause) 시 timeScale 0·커서 풀림·이동/카메라/사격 꺼짐·적과 플레이어 위치 3초간 그대로, [계속하기]로 timeScale 1·커서 잠김·조작 복구. UI 레이캐스트는 세 버튼 모두 정상. **성 마을(사용자 요청 "3D Cartoon Village로 성이 있는 마을 맵, 물감은 3등분")**: 에셋 `Assets/3D Cartoon Village`(사용자가 가져옴, 모델이 원래 크기가 커서 0.25배로 씀), 머티리얼 31개 Standard→URP 변환(메뉴 `에셋 3D Cartoon Village URP로 바꾸기`). 씬 `3_CastleVillage`(메뉴 `Phase 11 성 마을 씬 만들기`, `Phase11CastleSetup`, 2_Forest 복사 후 교체, 다시 만들려면 씬과 `Assets/Scenes/3_CastleVillage/`를 지우고 메뉴 재실행): 남쪽 마을(집·우물·수레·천막·가로등·울타리, 지붕 없는 House2/4에는 `Roof` 얹음) → 성 앞뜰(천막·망루·큰 돌) → 성벽+`Gats`(성문, 문짝 Mesh2·7·12 제거해 열어 둠)+탑 → 성 안(`TownHall` 시계탑·집·우물·돌바닥). 벽·탑·집은 MeshCollider, 나무=캡슐, 소품=박스. 바깥 언덕·산·하늘·구름은 RPG Poly Pack 것 재사용. 조명: 숲의 구운 빛 데이터 없이 `AmbientMode.Trilight`로 직접 지정(그림자가 새까매지던 문제 해결). **물감 3등분**: `Stage/PaintZone`(위에서 본 직사각형 지역 + 색 3개, Scene 창 색 상자 기즈모) — 마을 Red/Orange/Yellow, 성 앞뜰 Green/Blue/Indigo, 성 안 Purple/White/Black. `EnemyDrop`이 **적이 만들어진 자리**(spawnPosition)의 지역으로 `PaintZone.PickColor`(지역 없으면 9색 랜덤 = 숲). 적 18마리(`Spawners/Zone1_Village` 돌진3·원거리2, `Zone2_Field` 돌진2·방어2·원거리2, `Zone3_Castle` 돌진2·방어3·원거리2, 시작에서 28m+·서로 9m+, 전부 NavMesh 위). 장소 선택 팝업에 "성 마을" 버튼 추가(`StageSelectPopup.castleButton`, `SceneNames.CastleVillage`, 메뉴 `Phase 11 장소 선택에 성 마을 추가`), 빌드 목록 4개. 재생 확인: 미술관→성 마을 로드, 시작→성 안 NavMesh 경로 PathComplete(문 열림), 지역별 처치 시 드롭 색이 정확히 해당 지역 3색만(마을 R/O/Y, 앞뜰 G/B/Indigo, 성 안 P/W/K), 화면 확인(집 지붕·성문·성 안). 아직 없음: 지역 이름/색 HUD 표시, 지역 경계 표시. 사용자 플레이 확인 대기. **미술관 꾸미기(사용자 요청 2026-10-30, 에셋 Minimalist ArchViz Bedroom `Assets/MinimalistBedroom`, 머티리얼은 이미 URP Lit)**: 메뉴 `Phase 12 미술관 꾸미기`(`Phase12MuseumDecorSetup`)가 `1_Museum`에 `MuseumHall`(Shell/Decor/Lights)을 만듦 — 막힌 전시장 x ±18 · z -8~15 · 높이 4.5(`Wall_*` 큐브+전용 회반죽 머티리얼 `MuseumWall_Long/Short`, `Ceiling`(그림자 끔), 에셋 `Floor_01` 나무 바닥 5×5), 천장 발광 판 6, 점광원 8(`RoomLight`), **액자 9개를 북쪽 벽(z 14.91)에 붙이고 액자마다 스포트라이트 `FrameLight_01~09`**, 장식(카펫 `Rug_Center`, 서 있는 조명 6, 화분 6, 받침대+조각 4(조각 6배), 벤치 2(`TvStand_01`), 옆벽 그림 6, 남쪽 벽 거울 2, `InfoStand`+`InfoScreen`, `DisplayCabinet`). 주변광은 `Trilight`로 밝게, 해 세기 0.4. 기존 작업대·지도 탁자·플레이어 자리는 그대로. 재생 확인: 시작 지점·액자 줄·장치 안내가 정상 표시, 하얀 전시장 느낌. 에셋 크기 173MB 중 예제 씬(약 70MB, 구운 빛 데이터)은 `.gitignore`로 GitHub에서 제외. 추가 에셋 제안은 PROGRESS_SUMMARY의 "추가로 있으면 좋은 에셋". 사용자 확인 대기. 다음: 사용자 확인 대기. 풀·장식이 공중에 뜬 듯하던 문제: 풀 타일이 울퉁불퉁(±0.2)한데 장식을 같은 높이에 놓아서였음 → `SnapToGround`(메뉴 `숲 장식 땅에 붙이기 (다시 맞추기)`): 장식마다 그 자리 땅 높이를 재서 맨 아래가 묻히게(나무 0.12·바위 0.2·작은 장식 0.06·마을 0.25·야영지 0.08) 맞추고, 길은 아래 땅의 최고점+0.05에 맞춤, NavMesh 재굽기. 측정으로 모든 장식의 간격이 설계값과 정확히 일치함을 확인.
| 타이틀 (기획서 Phase 9 일부, 사용자 요청으로 먼저) | ✅ `0_Title` 씬 (빌드 0번): 시작 → 1_Museum, 도움말, 옵션(소리 `AudioListener.volume`·마우스 감도 배율 → `GameSettings`/PlayerPrefs, 게임 나가기). 사용자 요청 추가: **"그림 보기" 버튼 + 양쪽 액자 2개(`TitleFrames`)**, 그림 보기 팝업에서 작품을 골라 "왼쪽에 걸기/오른쪽에 걸기" → save.json `titleFrames`. 작품 읽기·등록은 `PaintingLibrary`(paintings.json, Paintings/painting_<ID>.png — Phase 7 "완성"이 `Add`를 쓴다). 테스트 메뉴 "샘플 그림 3장 만들기". 아직 없음: 미술관 → 타이틀 돌아가기, 이어하기/새 게임 구분(Phase 9). 5-1 `PaintColor`(9색), `RunBag`(전투 가방, StageManager에), `EnemyDrop`(종이조각 1 + 랜덤 물감 30 — 2026-10-30 사용자 요청으로 50에서 줄임, 값은 `Enemy_Charger` 프리팹의 Paint Amount), `RunBagUI`(오른쪽 위, `BagPanel.png`·`Icon_PaperScrap.png`·`Icon_Paint_*.png`, 숫자는 TMP) 코드 커밋. TMP 기본 파일 가져옴. 메뉴 실행 완료. UI 그림이 Multiple 방식이라 9-slice가 안 먹던 문제 → `UiSpriteFix` 메뉴로 27개를 Single로 바꾸고 씬 연결 고침 (옛 연결 0개 확인), `ArtSlots.LoadSprite`가 앞으로 Single로 설정. 가방 숫자 증가 사용자 확인 ✅. 5-2 `InventoryManager`(자동 생성·DontDestroyOnLoad, 색별 최대 500, 넘침 이벤트), `RunBag.Settle()`(클리어)·`Clear()`(사망), `InventoryUI`(미술관 오른쪽 위, `Icon_Paper.png`), `PaintFullToast`(훈련장, `Toast_PaintFull.png`), 테스트 메뉴(물감 490) 코드 커밋. **메뉴 `Phase 5-2 창고 표시와 가득 참 알림 만들기` 실행 대기** (화면 조작으로 메뉴 위치를 잘못 눌러 `씬 정리`가 한 번 더 실행됨 — 변화 없음). 메뉴 실행 ✅, 정산·사망 사용자 확인 ✅. 5-3 `PopupBase`(공통 팝업: 조작 잠금·ESC·닫기), `InteractDevice`(공통 E 장치, `StageSelectDevice`는 이를 상속), `InventoryManager.CraftPaper()`, `PaperCraftPopup`, 미술관 `PaperCraftTable`(-4, 0.5, 3) 배치, 테스트 메뉴 종이조각 +10. 합성 사용자 확인 ✅. 5-4 `SaveManager`(`persistentDataPath/SavedData/save.json`, 기획서 3.5 형식, 임시 파일 후 교체), InventoryManager가 생성 시 불러오고 수량 변경마다 저장, 훈련장 클리어 시 "Forest" 해금 저장(숲 버튼은 숲 씬이 생길 때까지 잠금), 테스트 메뉴(저장 폴더 열기, 저장 파일 지우기). 재실행 후 유지는 사용자 확인 대기. 다음: Phase 5 완료 기준 점검 → Phase 6 그림판 |

**Enemy 태그 (2026-10-30, 사용자 요청)**: `Enemy_Charger` 프리팹 맨 위·Body·Head에 `Enemy` 태그, TagManager에 추가 (`EnemyTagSetup` 메뉴). 규칙은 CLAUDE.md "적 규칙". |

**헤드샷 원탭 전용 (2026-10-30, 사용자 결정)**: 몸샷은 피해 0, 깜빡임·멈칫만, 탄환은 빗나감처럼 떨어짐. 적 HP 없앰 (`EnemyHealth.TakeHit(part)`). 기획서 2.4·2.5·Phase 4 완료 기준 수정. 방어형 방패 규칙(명중 처리·즉시 재사용)은 그대로 — Phase 10에서 사용자에게 다시 확인. |

**씬 이름 변경 (2026-10-30, 사용자 요청)**: Hideout → `1_Museum`, Stage_Training → `2_Training`, SampleScene 삭제, 빌드 목록 [1_Museum, 2_Training]. 게임 코드는 `SceneNames` 사용 (PlayerHealth 사망·StageManager 클리어 → 1_Museum). 아래 기록의 Hideout/Stage_Training은 옛 이름이다. |

그림 넣는 곳 (2026-10-30): UI는 `Assets/UI/*.png`, 오브젝트는 전용 머티리얼. 목록 `docs/ART_SLOTS.md`, 규칙 CLAUDE.md. 메뉴 `그림 넣는 곳 정리` 실행 완료. |

Phase 1에서 한 일 (2026-10-29):
- [x] Unity 6 URP 프로젝트, New Input System 활성화 (`activeInputHandler: 1` 확인)
- [x] 3.3 폴더 구조, `Assets/Credits.txt`
- [x] `Hideout` 씬 (`SampleScene` 복사본: 카메라, 조명, Global Volume만 있음)
- [x] Cinemachine 3.1.7을 `Packages/manifest.json`에 추가. TextMeshPro는 uGUI 2.0.0에 포함
- [x] 사용자 확인: `Hideout` 재생 시 Console 빨간 오류 없음, Cinemachine 설치됨 (`packages-lock.json`)

## 해결되지 않은 문제: Ultracode가 회색으로 표시됨

**증상**: 앱의 모델 표시줄에 `Sonnet 5.5 · medium effort · Ultracode`가 보이는데, `Ultracode`만 회색이다. 사용자는 이것이 켜지기(반짝이기)를 원한다.

**지금까지 확인한 것**

- Opus 5.5로 모델을 바꾼 뒤에도 사용자는 "안 된다"고 했다.
- Claude가 설정할 수 있는 노력 수준은 `low`, `medium`, `high`, `xhigh`, `max` 다섯 가지이고, `Ultracode`는 그 목록에 없다. 노력 수준과는 별개의 기능으로 보인다.
- Claude는 자기 세션의 노력 수준을 직접 바꿀 수 없다. 사용자가 앱 표시줄에서 직접 바꿔야 한다.

**아직 모르는 것**

- Ultracode가 정확히 무엇인지, 어떤 조건에서 켜지는지 확인되지 않았다.

**다음에 할 일**

1. 사용자에게 모델 표시줄의 현재 화면 캡처를 받는다.
2. 노력 수준을 `high` 이상으로 올린 뒤 Ultracode 상태가 바뀌는지 확인한다.
3. 그래도 회색이면 Claude Code 공식 문서나 앱 도움말에서 Ultracode를 찾아본다.

## 참고: 혼동했던 부분

- 처음에 "울트라 코드"를 `/code-review ultra`(클라우드 코드 리뷰 명령)로 잘못 이해했다. 사용자가 말한 것은 모델 표시줄의 **Ultracode** 항목이다. 둘은 다른 것이다.


## 2026-10-30 오래된 미술관 (Phase 12, 사용자 요청)
- 침실 에셋(Minimalist ArchViz Bedroom)은 그림풍이 안 맞아 `Assets/MinimalistBedroom`을 지웠다. 에셋 Old LowPoly premium Furniture(Creation Forge)로 오래된 미술관을 만들었다: 메뉴 `LastBullet/Phase 12 오래된 미술관 만들기` (`Assets/Editor/LastBullet/Phase12OldMuseumSetup.cs`, 다시 실행하면 `MuseumHall`을 지우고 새로 만든다).
- 구성: 나무 판벽·바닥(임시 그림 `Assets/Textures/Museum/*.png`), 천장 매달린 등 8개, 액자 9개 스포트라이트, 옷장 6개, 유리 탁자 2 + 의자, 액자 앞 의자 5개, 붉은 카펫. 자세한 칸은 `docs/ART_SLOTS.md`.
- 새 에셋의 8K 텍스처(파일당 128MB) 폴더는 쓰지 않아 `.gitignore`에 넣고 유니티 가져오기 크기를 256으로 줄였다 (파일 자체는 그대로).
- 남은 확인: 사용자가 숲·성 마을·미술관을 직접 플레이해 보기, ESC 메뉴 마우스 클릭. 엔딩(9-2~9-5)은 계속 미룸. 성 마을에 구역 이름/색 안내 HUD는 아직 없음.


## 2026-10-31 사막 맵 + 맵별 물감 3등분 (Phase 13, 사용자 요청)
- 에셋 Low-Poly Desert Environment Pack(Coil Studio)을 `Assets/Tiny Teacup Studio`로 가져왔다 (유니티 CLI `AssetDatabase.ImportPackage`, 데모 씬은 라이트맵 22MB라 지움). 머티리얼 1개는 URP Lit으로 변환.
- 메뉴 `LastBullet/Phase 13 사막 씬 만들기` (`Phase13DesertSetup.cs`): 2_Forest를 복사해 `4_Desert` 생성. 130m 모래 평면 + 둘레 절벽(Cliff_01 ×3배, 모서리 조각은 안 씀) + 바위 26·식물 46·작은 돌 70, 투명 벽 ±60m, NavMesh 새로 굽기, 스포너 20개(돌진 8·방어 6·원거리 6). 다시 만들려면 `Assets/Scenes/4_Desert.unity`와 `Assets/Scenes/4_Desert/`를 지우고 다른 씬을 연 뒤 메뉴를 다시 실행.
- **물감 3등분 규칙 변경**: 지역 단위(PaintZone) → 맵 단위. `Assets/Scripts/Stage/StageColors.cs` 표 (숲 R/O/Y, 성 마을 G/B/I, 사막 P/W/K). `PaintZone.PickColor`가 지역이 없으면 현재 씬의 표를 쓴다. 성 마을 씬의 `PaintZones` 오브젝트는 지웠다 (`Phase13StageSelectSetup` 메뉴가 처리).
- 전투 장소 선택 팝업: 버튼 3개(숲·성 마을·사막) + 버튼 아래 "얻는 물감 ● ○ ●" 그림(`Assets/UI/StageColors_*.png`, 한글 글자는 TMP 기본 글꼴에 없어 PNG로). 창 높이 680. 메뉴 `LastBullet/Phase 13 장소 선택에 사막과 물감 안내 넣기`.
- 확인한 것: 사막 플레이 화면(절벽·바위·선인장·적 20마리 = Enemy 태그 66개), 사막에서 `PickColor` 300번 → 보라/하양/검정만, 팝업 화면. 아직 못 한 것: 사막 직접 플레이(적 처치·귀환), 숲·성 마을에서 새 색 확인.



## 2026-10-31 그림 삭제 (Phase 14, 사용자 요청)
- 그린 그림을 하나씩 골라 지우는 기능. 타이틀의 "그림 보기": 그림을 고르면 [삭제]가 눌린다 ([왼쪽에 걸기] [오른쪽에 걸기] [삭제] 한 줄). 미술관 그림판의 [내 그림]: 그림을 눌러 크게 본 창 아래에 [삭제]. 둘 다 "이 그림을 지울까요?" 확인창 → [지우기] [취소]. ESC는 확인창만 닫는다. 액자 고르기 팝업에는 삭제가 없다.
- 코드: `PaintingLibrary.Delete(id)` (목록에서 빼고 PNG 삭제, 텍스처 캐시 정리) → `SaveManager.ClearTitleFramesWith`로 타이틀 액자, `GalleryStore.ClearPainting`으로 미술관 액자 9칸의 같은 그림을 비운다. `PictureFrame`은 `GalleryStore.Changed`를 들어 바로 빈 액자로 바뀐다. UI는 `GalleryPopup`의 `deleteButton`·`deleteConfirm…` 칸.
- 메뉴 `LastBullet/Phase 14 그림 삭제 버튼 넣기`(`Phase14GalleryDeleteSetup.cs`)가 타이틀과 미술관 씬의 GalleryPopup에 버튼과 확인창을 만든다.
- 확인한 것: 테스트 그림을 만들어 타이틀(선택 → 삭제 → 확인 → 목록 12→11, 타이틀 액자 비워짐, PNG 삭제)과 미술관(크게 보기 → 삭제, 미술관 액자 칸 비워짐, 진행도 1/9 → 0/9) 모두 동작. 테스트 뒤 사용자 저장 데이터는 백업에서 되돌렸다. 못 한 것: 마우스로 직접 누르는 실제 클릭 (코드로 버튼을 눌러 확인).


## 2026-10-31 맵마다 하늘 (Phase 15, 사용자 요청)
- 에셋 AllSky Free(rpgwhitelock)에서 6면 큐브맵 3종만 남겼다 (`Assets/AllSkyFree`, 24MB; 원본 320MB·가로형 그림은 지움. 필요하면 Package Manager에서 다시 가져옴). 숲 = Cartoon Base BlueSky, 성 마을 = Epic_BlueSunset, 사막 = Epic_GloriousPink.
- 메뉴 `LastBullet/Phase 15 맵마다 하늘 바꾸기`(`Phase15SkySetup.cs`): 각 씬의 `RenderSettings.skybox` 지정, 카메라 Clear Flags를 Skybox로, 예전 하늘 돔(`SkyDome`)과 RPG Poly Pack 구름 삭제. 맵 빌더(Phase10Forest·Phase11Castle·Phase13Desert)를 다시 돌리면 예전 하늘이 돌아오므로 이 메뉴를 다시 실행.
- 확인한 것: 세 맵 플레이 화면에서 새 하늘이 보임. 못 한 것: 햇빛(Directional Light) 방향을 하늘의 해 위치에 맞추는 것, 미술관(실내)은 변경 없음.


## 2026-10-31 낮과 밤 (Phase 16, 사용자 요청: 능력 없이 겉모습만 무작위)
- 숲·성 마을·사막이 들어갈 때마다 낮 또는 밤이 50% 확률로 정해진다. 게임에는 영향이 없다 (적·물감·피해 그대로).
- `Assets/Scripts/Stage/DayNight.cs`: 밤이면 밤하늘 머티리얼로 바꾸고, 해 Directional Light를 푸른 달빛(0.5)으로, 주변광을 어둡고 푸르게(트라이라이트는 색 곱하기, 하늘 기준은 세기 0.55). `Mode`(Random/AlwaysDay/AlwaysNight)와 밤 확률은 Inspector에서 바꾼다 (확인용으로 고정할 때). 밤하늘: 숲 Cartoon Base NightSky, 성 마을 Night MoonBurst, 사막 Cold Night.
- 메뉴 `LastBullet/Phase 16 낮과 밤 넣기`(`Phase16DayNightSetup.cs`)가 세 씬에 `DayNight` 오브젝트를 만든다. 맵을 다시 만들면 Phase 15(하늘) → Phase 16 순서로 다시 실행.
- 확인한 것: 세 맵의 밤 화면(적이 보일 만큼 밝음), 8번 연속 플레이에서 낮/밤이 섞여 나옴. 못 한 것: 밤 전투 직접 플레이(가독성), 낮과 밤 전환 연출(없음, 입장 때만 정해짐).


## 2026-10-31 맵 입장 안내 (Phase 17, 사용자 요청)
- 숲·성 마을·사막에 들어가면 화면 위쪽 가운데에 "사막 · 밤 / 얻는 물감 ● 보라 ● 하양 ● 검정" 창이 0.6초 나타나 3.5초 머물고 1초 사라진다. 낮/밤은 `DayNight.IsNight`를 따른다.
- 코드 `Assets/Scripts/UI/MapIntroBanner.cs` (CanvasGroup 페이드, 일시정지 중에도 흐르게 unscaled time; 맵 불러온 직후 첫 프레임 시간 튐으로 안내가 바로 지나가는 걸 막으려고 한 프레임에 0.1초까지만 셈). 메뉴 `LastBullet/Phase 17 맵 입장 안내 넣기`(`Phase17MapIntroSetup.cs`)가 세 씬의 Canvas 맨 뒤에 `MapIntro`를 만든다. 글자 그림은 `StageTitle_*`(6장)·`StageColors_*`.
- 확인한 것: 숲 낮, 사막 낮·밤, 성 마을 밤에서 안내가 뜨고 사라짐. 못 한 것: 숲 밤·성 마을 낮 화면(같은 코드·그림 이름만 다름), 안내가 뜬 채로 ESC 일시정지.


## 2026-10-31 피격 효과 (Phase 18, 사용자 요청)
- 맞으면 ① 화면 가장자리가 빨갛게 번쩍이고 0.5초에 걸쳐 사라진다 (피해가 클수록 세다) ② 카메라가 살짝 흔들린다 (크기 0.8도 + 피해량×0.08도, 0.3초, 갈수록 약해짐) ③ 체력이 35% 아래면 테두리가 계속 붉게 덮이고 체력이 낮을수록 짙어지며 심장 뛰듯 살짝 맥동한다.
- 코드: `PlayerHealth.Damaged(int)` 이벤트(새로 추가) → `UI/HitFeedback.cs`(번쩍·저체력 테두리, Inspector에서 시간·세기·임계값 조절) + `ThirdPersonCamera.Shake(도, 초)`(마우스 방향은 안 바뀌게 cameraTarget 회전에만 더함). 메뉴 `LastBullet/Phase 18 피격 효과 넣기`(`Phase18HitFeedbackSetup.cs`)가 숲·성 마을·사막 Canvas 맨 뒤에 `HitFeedback`을 만든다. 그림은 `Vignette_Hit.png`, `Vignette_LowHealth.png`.
- 확인한 것: 숲에서 피해 20 → 빨간 번쩍, 체력 25·10 → 붉은 테두리(낮을수록 짙음), 카메라 흔들림 값이 줄어들며 0으로 돌아옴. 못 한 것: 실제 적에게 맞는 느낌(코드로 피해를 줘서 확인), 사막·성 마을 직접 확인(같은 설정), 느낌 조절(세기·흔들림 크기는 사용자 취향에 맞게 조절 필요).


## 2026-10-31 물감 획득 연출 (Phase 19, 사용자 요청)
- 적을 잡으면 그 자리(머리 높이)에서 그 색 물감 방울 5개가 위로 튀어 흩어지고(0.25초), 곡선을 그리며 화면 오른쪽 위 전투 가방의 그 색 아이콘으로 날아 들어간다(약 0.6초). 도착하면 아이콘이 통통 튄다. 피격 효과(Phase 18)와 짝.
- 코드: `UI/PaintCollectEffect.cs` (방울을 코드로 움직임: 월드→화면→캔버스 좌표, 2차 곡선, 일시정지 중에도 흐르게 unscaled time). `EnemyDrop.OnDied`가 `PaintCollectEffect.Play(색, 머리 위치)`를 부른다 (연출이 없는 씬에서는 무시). 메뉴 `LastBullet/Phase 19 물감 획득 연출 넣기`(`Phase19PaintCollectSetup.cs`)가 숲·성 마을·사막 Canvas의 RunBagPanel 바로 위에 `PaintCollect`를 만든다. 그림 `PaintDrop_*.png` 9장.
- 확인한 것: 숲에서 방울이 처치 자리에서 튀어 가방 쪽으로 곡선을 그리며 날아가 사라짐 (느리게 늘려서 캡처). 못 한 것: 도착 때 아이콘 통통 튀는 모습을 화면으로 못 봄, 가방 숫자는 방울이 도착하기 전(처치 즉시)에 올라감(도착 때 올리는 건 아직 아님), 사막·성 마을 직접 확인.


## 2026-10-31 이번 판 결과 창 (Phase 20, 사용자 요청)
- 숲·성 마을·사막에서 [미술관으로 귀환]을 누르거나 죽으면 "이번 판 결과" 창이 뜬다: 제목(귀환 "이번 판 결과" / 사망 "쓰러졌어요"), 잡은 적 수, 종이조각, 물감 9색 양(귀환은 "얻은 재료", 사망은 "잃은 재료"). 숫자는 0에서 0.7초 동안 올라간다. [미술관으로]를 누르면 미술관으로 간다. 창이 떠 있는 동안 시간이 멈추고 ESC로는 안 닫힌다(일시정지 창도 안 뜸).
- 코드: `RunBag.Kills`와 `RunBag.Summary()`(`RunSummary` 구조체; 정산·삭제로 비워지기 전에 받아 둠), `UI/RunResultPopup.cs`(스크립트 오브젝트는 항상 켜 두고 자식 `Window`만 껐다 켬 — 꺼진 오브젝트는 Awake가 안 불려 Current를 못 정하기 때문), `PauseMenu.GoHome`(요약 → 정산 → 결과 창), `PlayerHealth.Die`(요약 → 비우기 → 2초 뒤 결과 창; 결과 창이 없는 씬은 예전처럼 바로 미술관). 메뉴 `LastBullet/Phase 20 이번 판 결과 창 넣기`(`Phase20RunResultSetup.cs`).
- 확인한 것: 숲에서 귀환·사망 두 경로 모두 창이 뜨고 숫자·제목이 맞음, 버튼으로 미술관에 이동하고 시간·커서가 정상. 테스트 때 귀환이 테스트 재료를 창고에 정산해서 사용자 저장 데이터는 백업에서 되돌렸다. 못 한 것: 사막·성 마을 직접 확인(같은 설정), 실제 적을 잡은 뒤 귀환한 숫자.


## 2026-10-31 미술관 액자 앞 의자 제거 (사용자 요청)
- 액자 앞 의자 줄(`MuseumHall/Decor/ViewingChair` 5개, z≈11.3)이 액자를 가려서 씬에서 지우고 `Phase12OldMuseumSetup.cs`에서도 뺐다. 유리 탁자 곁 의자(`OldChair`, `StrawChair`)는 그대로. 에디터 화면으로 액자 9개가 가려지지 않는 것을 확인.


## 2026-10-31 한글 글꼴 (사용자가 넣음)
- 사용자가 글꼴 파일을 `Assets/Font/font1.ttf`로 넣었고(패밀리 이름 LeeSeoyun), 프로젝트 규칙 위치 `Assets/UI/Fonts/`로 옮겼다. 메뉴 `LastBullet/글꼴 에셋 만들기 (Assets/UI/Fonts)`(`FontAssetSetup.cs`)가 폴더의 글꼴마다 TextMeshPro 동적(Dynamic) 글꼴 에셋 `<이름> SDF.asset`을 만든다(이미 있으면 건너뜀). 만든 에셋의 글자 캐시는 지워서(ClearFontAssetData) 파일을 작게 두었다.
- 확인한 것: 한글 완성형 11,172자 전부 글꼴에 있음, 타이틀 화면 위에 임시 TMP 글자로 한글·영문·숫자·기호(· ● !?)가 보임(둥근 손글씨 모양). 쓰는 곳은 아직 없다(기존 한글 글자는 PNG 그대로, 숫자 TMP도 기본 글꼴 그대로). 그림 제목 붙이기 등을 만들 때 이 글꼴을 쓴다. 출처·라이선스는 사용자 확인 필요.


## 2026-10-31 총소리 (Phase 21, 사용자 요청: 핸드건 소리, 적은 작게)
- 사용자가 에셋 Free Weapon Sound Effects(SoundLab_1)의 핸드건을 `Assets/FreeWeaponSFX_v1.1`로 가져왔다 (Package Manager의 "Shooting Sound"는 아직 안 가져왔다).
- `Scripts/Audio/SfxPlayer.cs`: 변형 여러 개 중 무작위 재생 + 음높이 ±4% 흔들기 + 여운(tails) 깔기. 플레이어(`OneBulletGun.shotSound`)는 2D·소리 0.8·여운 포함, 원거리 적(`RangedEnemyAI.attackSound`, 던지는 순간)은 3D·소리 0.3(플레이어의 약 40%)·여운 없음·6m 안은 그대로 45m까지 멀수록 작게. 적 중 총을 쏘는 적은 없고 원거리 적이 돌을 던지는 순간에 총소리를 쓴다(사용자 요청 해석). 돌진형·방어형은 소리 없음.
- 메뉴 `LastBullet/Phase 21 총소리 넣기`(`Phase21SfxSetup.cs`): 미술관·숲·성 마을·사막의 플레이어와 `Enemy_Thrower` 프리팹에 붙인다 (이미 있으면 건너뜀). 유니티 오브젝트에는 `??`를 쓰면 안 된다는 걸 이 작업에서 다시 확인(GetOrAdd로 해결).
- 확인한 것: 코드로 재생했을 때 두 AudioSource 모두 재생 중, 실제 사격 Fire() 호출에서도 재생. **못 한 것: 실제로 소리를 들어 보는 것(크기 균형)** — 사용자가 직접 들어 보고 크면 `SfxPlayer`의 Volume으로 조절.


## 2026-10-31 클릭 소리 (Phase 22, 사용자 요청: 마우스 클릭마다, 총 쏠 때는 제외)
- 에셋 Free UI Click Sound Pack(SwishSwoosh)에서 따뜻한 나무 클릭 3개(`Wooden/Thin_1, Thin_2, Select_1`)만 `Assets/Free UI Click Sound Effects Pack`으로 가져왔다 (오래된 미술관 분위기에 맞춰 고름. 패키지 전체 100여 개는 가져오지 않음).
- `Scripts/Audio/ClickSound.cs`: **좌클릭을 눌렀을 때 마우스 커서가 잠겨 있지 않으면** `SfxPlayer`로 클릭 소리를 낸다. 게임 중에는 커서가 잠겨 있고 좌클릭이 사격이므로 사격에는 클릭 소리가 안 난다. 메뉴·팝업·그림판·결과 창·타이틀처럼 커서가 풀린 곳의 클릭은 모두 소리가 난다(버튼이 아닌 곳을 눌러도 난다). 메뉴 `LastBullet/Phase 22 클릭 소리 넣기`(`Phase22ClickSoundSetup.cs`)가 5개 씬 모두에 `ClickSound` 오브젝트를 만든다.
- 확인한 것: 숲(커서 잠김)에서 좌클릭을 넣으면 사격만 되고 클릭 소리는 안 남, 타이틀·미술관·사막에서 클릭 소리 재생기가 재생됨. **못 한 것: 커서가 풀린 상태의 실제 마우스 클릭 시험**(유니티가 창 밖 포인터 입력을 막아 자동 입력이 안 됨)과 직접 들어 보기. 소리가 크거나 작으면 `ClickSound` 오브젝트의 `SfxPlayer` Volume으로 조절.


## 2026-10-31 움직임 소리 (Phase 23, 사용자 요청: 걷기·달리기·점프 전부)
- 에셋 HALFTONE SFX Pack LITE(VOiD1 Gaming)의 `Gameplay/3. Movement` 28개를 사용자가 가져왔다 (이 폴더만 들어 있음). 소리를 들어 볼 수 없어 길이·음높이 변화로 골랐다.
- `PlayerMovement`에 알림 `Jumped`, `Dodged`, `Landed(떨어진 속도)`와 `IsRunning` 추가 (착지는 떨어지는 속도 3m/s 이상일 때만). `Scripts/Audio/MovementSounds.cs`: 발소리는 걸은 거리 기준(걷기 2.0m·달리기 2.6m마다 한 걸음, 멈추면 거리 초기화), 점프·착지·회피는 알림에 맞춰 재생. 메뉴 `LastBullet/Phase 23 움직임 소리 넣기`(`Phase23MovementSoundSetup.cs`)가 플레이어 아래 `MovementSfx`(AudioSource 1개 + SfxPlayer 5개)를 만든다.
- 확인한 것: 점프·착지·회피 알림이 소리를 재생하고 5종 소리 재생기가 모두 재생됨. **못 한 것: 실제 키 입력으로 걸어 보기**(유니티가 창 밖 키 입력을 자꾸 풀어서 자동 시험이 안 됨. 걸을 때 거리가 쌓이고 한 걸음마다 초기화되는 것은 일부 확인)와 **직접 들어 보기**. 크기는 `SfxPlayer` Volume으로 조절.


## 2026-10-31 그림 보기 칸 잘림 수정 (Phase 24, 사용자 요청)
- 증상: 그림 보기(타이틀)·내 그림(미술관)에서 두 번째 줄 아래쪽과, 고른 그림의 노란 테두리 아래쪽이 잘려 보였다. 원인: 그림 칸 두 줄이 차지하는 높이 630(칸 280×2 + 간격 30 + 위아래 여백 20×2)보다 스크롤 창 높이가 600이라 아래 30이 가려졌다.
- 고침: 스크롤 창 높이 620·위치 y=+10, 칸 위아래 여백 10 (두 줄 = 610이 딱 맞게 들어가고 세 줄 이상이면 스크롤). 메뉴 `LastBullet/Phase 24 그림 보기 칸 잘림 고치기`(`Phase24GalleryLayoutFix.cs`)가 타이틀과 미술관의 모든 GalleryPopup(내 그림·액자 고르기 포함)에 적용하고, `TitleSetup.CreateGallery`도 같은 값으로 바꿨다. 에디터 화면으로 타이틀·미술관 모두 확인 (고른 테두리가 네 면 모두 온전함).
