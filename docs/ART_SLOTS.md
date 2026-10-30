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
| `Assets/UI/StageLabel_Forest.png` | 전투 장소 선택 버튼 글자 "숲" (훈련장을 없애서 숲 하나뿐) | 512×128 | |
| `Assets/UI/Prompt_PaperCraft.png` | 종이 합성대 근처 안내 "[E] 종이 합성" | 768×96 | |
| `Assets/UI/PaperCraft_Title.png` | 종이 합성 팝업 제목 | 768×128 | |
| `Assets/UI/PaperCraft_Owned.png` | 팝업의 "가진 것" 글자 | 384×96 | |
| `Assets/UI/Prompt_Paint.png` | 그림 작업대 근처 안내 "[E] 그림 그리기" | 768×96 | |
| `Assets/UI/Paint_Panel.png` | 그림판 창 배경 (9-slice, 모서리 40) | 128×128 | 크림색 둥근 사각형 |
| `Assets/UI/Paint_Title.png` | 그림판 제목 "나의 그림판" (왼쪽 정렬) | 512×96 | |
| `Assets/UI/Paint_PaperLabel.png` / `Paint_PaperUnit.png` | "종이" / "장" 글자 (가운데 숫자는 TMP) | 128×64 / 64×64 | |
| `Assets/UI/Paint_Toolbar.png` | 왼쪽 도구 막대 (9-slice, 모서리 28) | 96×96 | |
| `Assets/UI/Paint_Tool_Pen1~4.png` | 펜 굵기 2/5/10/20px 버튼 그림 | 96×96 | 굵기를 알아볼 수 있게 |
| `Assets/UI/Paint_Tool_Eraser.png` | 지우개 버튼 그림 | 96×96 | |
| `Assets/UI/Paint_Tool_Selected.png` | 고른 도구 표시 테두리 | 96×96 | 선택된 버튼 위에 겹침 |
| `Assets/UI/Paint_CanvasFrame.png` | 종이 테두리 (9-slice, 모서리 24) | 128×128 | 정사각형 고정 |
| `Assets/UI/Paint_Color_<색>.png` (9장) | 색 고르는 동그라미 (Red…Black) | 64×64 | 색 이름은 `PaintColor`와 같게 |
| `Assets/UI/Paint_Color_Selected.png` | 고른 색 표시 테두리 | 80×80 | |
| `Assets/UI/Paint_Button_Cancel_*.png`, `Paint_Button_Done_*.png` | 취소/완성 버튼 (Normal·Highlighted·Pressed, 완성만 Disabled 추가) | 96×96 | 9-slice 모서리 32 |
| `Assets/UI/Paint_Label_Cancel.png` / `Paint_Label_Done.png` | 버튼 글자 "취소" / "완성" | 256×96 | |
| `Assets/UI/Paint_Confirm_Message.png` | 그림판 취소 확인창 문구 (두 줄) | 640×160 | 창·버튼 배경은 `Paint_Panel`, `Paint_Button_*`를 같이 씀 |
| `Assets/UI/Paint_Label_Continue.png` / `Paint_Label_Discard.png` | 확인창 버튼 글자 "계속 그리기" / "그만두기" | 256×96 | |
| `Assets/UI/Pause_Title.png` | ESC 일시정지 창 제목 "일시정지" | 768×128 | 창 배경은 `Popup_Panel`, 버튼은 `Button_*`를 같이 씀 |
| `Assets/UI/Pause_LossWarning.png` | 숲 일시정지 창의 안내 "타이틀로 나가면 이번 판 재료가 사라져요" | 768×96 | 숲에서만 보임 |
| `Assets/UI/ButtonLabel_Home.png` | 숲 일시정지 창의 버튼 글자 "미술관으로 귀환" (재료를 창고로 정산하고 돌아간다) | 512×128 | 버튼 배경은 `Button_*` |
| `Assets/UI/ButtonLabel_Resume.png` / `ButtonLabel_ToTitle.png` | 버튼 글자 "계속하기" / "타이틀로 나가기" | 512×128 | |
| `Assets/UI/Paint_Label_Gallery.png` | 그림판 버튼 글자 "내 그림" | 256×96 | 버튼 배경은 `Paint_Button_Cancel_*`. 누르면 보기 전용 작품 팝업(그림·창은 타이틀 그림 보기와 같은 `Gallery_*`, `Popup_Panel`)이 뜬다 |
| `Assets/UI/Prompt_Frame.png` | 액자 근처 안내 "[E] 그림 걸기" | 768×96 | |
| `Assets/UI/Frame_Title.png` | 액자 고르기 팝업 제목 "액자에 걸 그림" | 768×128 | 팝업의 나머지 그림은 타이틀 그림 보기와 같은 `Gallery_*`, `Popup_Panel` |
| `Assets/UI/FrameProgress_Label.png` | 화면 위 가운데 진행도의 글자 "서로 다른 작품" (옆의 "3 / 9" 숫자는 TMP) | 384×96 | 배경은 `BagPanel.png`를 같이 씀 |
| `Assets/UI/StageLabel_Castle.png` | 전투 장소 선택 버튼 글자 "성 마을" | 512×128 | |
| `Assets/UI/StageLabel_Desert.png` | 전투 장소 선택 버튼 글자 "사막" | 512×128 | |
| `Assets/UI/StageColors_Forest.png`, `StageColors_Castle.png`, `StageColors_Desert.png` | 전투 장소 선택의 버튼 아래 안내 "얻는 물감 ● 빨강 ● 주황 ● 노랑" (글자와 색 점이 모두 그림 안에 있다) | 720×64 | 맵의 물감 색을 바꾸면(`StageColors` 표) 이 그림도 같이 고쳐야 한다 |
| `Assets/UI/ButtonLabel_Delete.png`, `ButtonLabel_DeleteYes.png`, `ButtonLabel_DeleteNo.png` | 그림 보기의 [삭제] 버튼 글자와 확인창의 [지우기] [취소] 글자 | 256×128 / 320×128 / 256×128 | 버튼 틀은 `Button_*.png`를 같이 씀 |
| `Assets/UI/Gallery_DeleteConfirm_Message.png` | 그림 삭제 확인창 문구 "이 그림을 지울까요? 지운 그림은 되살릴 수 없어요." (두 줄) | 720×200 | 창 배경은 `Popup_Panel.png` |
| `Assets/UI/Scrollbar_Track.png`, `Scrollbar_Handle.png` | 그림 목록 스크롤바의 길(어두운 반투명)과 손잡이(크림색, 갈색 테두리). 둥근 사각형 9-slice(모서리 12px) | 48×48 | 타이틀 `GalleryPopup`, 미술관 `GalleryPopup`·`FramePickerPopup`의 `Panel/Scroll/Scrollbar` |
| `Assets/UI/Profile_Empty.png` | 프로필 사진이 없을 때 보이는 빈 사람 모양 (회색 배경 + 머리·어깨) | 256×256 | 타이틀 `ProfileCard`·`ProfilePopup`의 `Empty` |
| `Assets/UI/Profile_NamePlate.png` | 프로필 카드 아래 닉네임 이름표 배경 (둥근 사각형 9-slice, 모서리 12px) | 96×96 | 타이틀 `ProfileCard/NamePlate` |
| `Assets/UI/Profile_Hint.png` | 프로필 카드 아래 "눌러서 프로필 바꾸기" 안내 글자 | 420×48 | `ProfileCard/Hint` |
| `Assets/UI/Profile_Title.png`, `ProfileDraw_Title.png`, `ProfilePick_Title.png` | 창 제목 "프로필" / "프로필 그리기" / "프로필 사진 고르기" | 600×100, 600×100, 700×100 | `ProfilePopup`, `ProfileDrawPopup`, `ProfilePickerPopup` |
| `Assets/UI/ButtonLabel_ProfileDraw.png`, `ButtonLabel_ProfilePick.png` | 프로필 창의 [직접 그리기] [그림에서 고르기] 버튼 글자 | 480×100 | 버튼 틀은 `Button_*.png` |
| `Assets/UI/Label_Nickname.png` | 옵션의 "닉네임" 글자 | 260×64 | 타이틀 `OptionsPopup` |
| `Assets/UI/Input_Background.png` | 닉네임 입력 칸 배경 (어두운 칸 + 밝은 테두리, 9-slice 12px). 입력 글자는 TextMeshPro 한글 글꼴 `font1 SDF` | 96×96 | 옵션의 `NicknameInput` |
| `Assets/UI/Vignette_Hit.png` | 맞았을 때 번쩍이는 화면 가장자리 빨간 테두리 (가운데는 투명, 가장자리일수록 진함. 화면 전체로 늘려 씀) | 256×256 | 숲·성 마을·사막 `Canvas/HitFeedback/Flash` |
| `Assets/UI/Vignette_LowHealth.png` | 체력이 낮을 때(35% 아래) 계속 덮이는 진한 붉은 테두리 (가운데 투명) | 256×256 | `Canvas/HitFeedback/LowHealth` |
| `Assets/UI/PaintDrop_<Red|Orange|Yellow|Green|Blue|Indigo|Purple|White|Black>.png` (9장) | 적을 잡으면 가방 아이콘으로 날아가는 물감 방울 (색마다 한 장, 눈물 모양) | 64×64 | 숲·성 마을·사막 `Canvas/PaintCollect` (도착 지점은 가방 패널의 `Icon_Paint_*`) |
| `Assets/UI/ResultTitle_Clear.png`, `ResultTitle_Dead.png` | 결과 창 제목 "이번 판 결과" / "쓰러졌어요" | 640×96 | 창 배경은 `Popup_Panel.png`. 숲·성 마을·사막 `Canvas/RunResultPopup` |
| `Assets/UI/ResultCaption_Gained.png`, `ResultCaption_Lost.png` | 결과 창 재료 줄 제목 "얻은 재료" / "잃은 재료" | 400×64 | 물감 아이콘은 가방과 같은 `Icon_Paint_*.png` |
| `Assets/UI/ResultLabel_Kills.png` | 결과 창 "잡은 적" 글자 (숫자는 TMP) | 240×64 | 종이조각 아이콘은 `Icon_PaperScrap.png` |
| `Assets/UI/ButtonLabel_ToMuseum.png` | 결과 창 [미술관으로] 버튼 글자 | 420×128 | 버튼 틀은 `Button_*.png` |
| `Assets/UI/Fonts/font1.ttf` + `font1 SDF.asset` | 글꼴 "이서윤체(LeeSeoyun)" (사용자가 넣은 파일). 한글 11,172자 모두 들어 있다. TextMeshPro 글자의 Font Asset 칸에 `font1 SDF`를 끼우면 그 글꼴로 나온다. 다른 글꼴을 넣으면 메뉴 `LastBullet/글꼴 에셋 만들기`로 같은 에셋을 만든다 | 아직 쓰는 곳 없음 (지금 한글은 PNG 그림, 숫자는 기본 글꼴) |
| 에셋 `Assets/FreeWeaponSFX_v1.1/Handgun/Gunshots/*.wav` | 총소리: 사격 소리 변형 3개(`gunshot_01~03`)와 여운 2개(`tail_01~02`). 플레이어 사격은 Player의 `SfxPlayer`(소리 0.8, 2D, 여운 같이), 원거리 적 공격은 `Enemy_Thrower` 프리팹의 `SfxPlayer`(소리 0.3, 3D, 6~45m에서 멀수록 작게). 다른 소리로 바꾸려면 `SfxPlayer`의 Clips 칸에 끼운다 | `2_Forest`·`3_CastleVillage`·`4_Desert`·`1_Museum`의 Player, `Prefabs/Enemy_Thrower` |
| 에셋 `Assets/Free UI Click Sound Effects Pack/AUDIO/Wooden/*.wav` (3개) | 마우스 클릭 소리 (따뜻한 나무 소리 변형 3개, 소리 0.6, 2D). 총을 쏠 때(커서가 잠겼을 때)는 나지 않는다. 다른 소리로 바꾸려면 `ClickSound` 오브젝트의 `SfxPlayer` Clips 칸에 끼운다 | 모든 씬(`0_Title` `1_Museum` `2_Forest` `3_CastleVillage` `4_Desert`)의 `ClickSound` |
| 에셋 `Assets/VOiD1 Gaming - HALFTONE SFX PACK LITE/.../3. Movement/*.wav` | 플레이어 움직임 소리(발소리는 에셋 Footstep(Snow and Grass)에서 다듬은 것): 걷기·달리기는 맵마다 바닥이 다르다 — 숲·성 마을 `Assets/Audio/Footsteps/footstep_01~08.wav`(풀밭), 사막 `sand_01~06.wav`(모래, 코드로 만든 임시 소리), 미술관 `wood_*.wav`(나무 바닥, 코드로 만든 임시 소리). 걷기 0.5, 달리기 0.65·더 높게, 점프 `Assets/Audio/Jump/jump_01~03.wav`(코드로 만든 임시 소리: 툭 + 슉, 음높이 없음), 착지 `Bounce_2·3`, 회피(Q) `Jump_14·15`. 발소리는 걸은 거리(걷기 2.0m, 달리기 2.6m)마다 한 걸음. 소리를 바꾸려면 Player 아래 `MovementSfx`의 각 `SfxPlayer` Clips 칸에 끼운다 | 미술관·숲·성 마을·사막의 `Player/MovementSfx` |
| 에셋 `Assets/Models/Character/` (JellyFishGirl 모델 `Models/JellyFishGirl.fbx`, 재질 7개, 그림 `Textures/`, 동작 5개 `Animations/Mot_*.fbx`) + 프리팹 `Assets/Prefabs/PlayerCharacter.prefab` + 컨트롤러 `Assets/Animations/PlayerCharacter.controller` | **주인공 플레이어 모델** (임시. 나중에 3ds Max로 만든 모델로 바꿀 때는 Player 아래 `Model`만 교체하고 `PlayerAnimator`의 Animator 칸을 맞춘다). 동작: 대기·걷기·달리기(이동 속도 `Speed`로 전환)·사격·피격. 점프·회피 동작은 없다. 총구(`Muzzle`)는 모델의 총 앞(-0.3, 1.05, 0.75)에 둔다. 재질은 모델 전용 `Kurage_*` 7개(URP Lit) | 4개 씬의 `Player/Model` |
| 에셋 `Assets/PolyOne/Free Stickman/` (스틱맨 `Model/Free Pack - Stick Man.fbx`, 프리팹, 동작 `Animation/*.anim`) + 컨트롤러 `Assets/Animations/EnemyStickman.controller` | **적 3종의 모양** (임시. 3ds Max 모델로 바꿀 때는 각 적 프리팹의 `Model`만 교체하고 `Head`·`Body`(맞는 부위 충돌)와 `EnemyAnimator`의 Animator 칸을 맞춘다). 크기 돌진형 1.25·방어형 1.35·원거리형 0.95배. 몸 색은 코드(`EnemyDrop`)가 적마다 "잡으면 주는 물감 색"으로 칠한다(재질 `Enemy_Charger/Shield/Thrower.mat`은 흰색 바탕). 동작: 대기·걷기·달리기·빨리 달리기를 이동 속도로 전환 | `Prefabs/Enemy_*.prefab`의 `Model` |
| `Materials/EnemyShieldRim.mat`, `EnemyShieldPlate.mat`, `EnemyShieldBoss.mat` | 방어형의 둥근 방패(어두운 테두리 + 밝은 판 + 금빛 가운데 장식, 3D 도형 3개). 충돌 없음 | `Prefabs/Enemy_Shield.prefab`의 `Shield` |
| 에셋 `Assets/FREE Adventure RPG Fantasy Game Music Pack/Core Gameplay/` 의 `Happy Clouds LOOP BPM110.wav`(타이틀, 0.35), `Harping On LOOP BPM98.wav`(미술관, 0.3) | 배경음악. 서서히 커지며 시작해 끝없이 되풀이. 다른 곡으로 바꾸려면 씬의 `BackgroundMusic`(MusicPlayer)의 Music 칸에 끼운다. 전투 맵에는 음악이 없다 | `0_Title`, `1_Museum`의 `BackgroundMusic` |
| `Assets/UI/StageTitle_<Forest|Castle|Desert>_<Day|Night>.png` (6장) | 맵 입장 안내의 맵 이름과 낮/밤 글자 "사막 · 밤" 등 | 640×96 | 안내 창 배경은 `Popup_Panel.png`, 아래 "얻는 물감" 줄은 `StageColors_*.png`를 같이 씀. 숲·성 마을·사막 씬의 `Canvas/MapIntro` |
| `Assets/UI/Arrow_Right.png` | 만드는 법 화살표 (재료 → 결과) | 96×64 | 오른쪽을 가리키게 그린다 |
| `Assets/UI/ButtonLabel_Craft.png` | 버튼 글자 "합성" | 512×128 | 버튼 배경은 Button_*.png |
| `Assets/UI/Title_Background.png` | 타이틀 배경 (화면 전체) | 1920×1080 | 화면 크기에 맞게 늘어난다 |
| `Assets/UI/Title_Logo.png` | 타이틀 로고 "LAST BULLET 마지막 탄환" | 1024×320 | |
| `Assets/UI/ButtonLabel_Start.png` | 버튼 글자 "시작" | 512×128 | |
| `Assets/UI/ButtonLabel_Help.png` | 버튼 글자 "도움말" | 512×128 | |
| `Assets/UI/ButtonLabel_Options.png` | 버튼 글자 "옵션" | 512×128 | |
| `Assets/UI/ButtonLabel_Quit.png` | 버튼 글자 "게임 나가기" | 512×128 | |
| `Assets/UI/Help_Title.png` | 도움말 팝업 제목 | 768×128 | |
| `Assets/UI/Help_Content.png` | 도움말 내용 (조작, 규칙) | 1024×576 | |
| `Assets/UI/Options_Title.png` | 옵션 팝업 제목 | 768×128 | |
| `Assets/UI/Label_Volume.png` | "소리 크기" | 384×96 | |
| `Assets/UI/Label_Sensitivity.png` | "마우스 감도" | 384×96 | |
| `Assets/UI/Slider_Background.png` | 슬라이더 바탕 | 128×32 | **9-slice**, 모서리 12px |
| `Assets/UI/Slider_Fill.png` | 슬라이더 채움 | 128×32 | **9-slice**, 모서리 12px |
| `Assets/UI/Slider_Handle.png` | 슬라이더 손잡이 | 64×64 | |
| `Assets/UI/Gallery_Title.png` | 그림 보기 팝업 제목 "내 그림" | 768×128 | |
| `Assets/UI/Gallery_Empty.png` | "아직 그림이 없어요" | 768×128 | |
| `Assets/UI/ButtonLabel_Gallery.png` | 버튼 글자 "그림 보기" | 512×128 | |
| `Assets/UI/ButtonLabel_HangLeft.png` | 버튼 글자 "왼쪽에 걸기" | 512×128 | |
| `Assets/UI/ButtonLabel_HangRight.png` | 버튼 글자 "오른쪽에 걸기" | 512×128 | |
| `Assets/UI/Gallery_Selected.png` | 그림 보기에서 고른 그림 표시 (테두리) | 128×128 | **9-slice**, 모서리 24px. 가운데는 투명 |
| `Assets/UI/TitleFrame.png` | 타이틀 양쪽 액자 (빈 액자일 때 안쪽이 보인다) | 128×128 | **9-slice**, 모서리 32px. 화면에서는 정사각형 360×360, 그림은 안쪽 30px 안에 걸린다 |
| `Assets/UI/Gallery_Frame.png` | 그림 보기의 그림 한 칸 액자 | 128×128 | **9-slice**, 모서리 24px. 그림은 안쪽 26px 안에 들어간다 |
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
| `Materials/Tracer.mat` | 탄도선 | `Player/BulletTracer` |
| `Materials/ImpactMarker.mat` | 맞은 자리에 0.4초 보이는 점 | 프리팹 `Prefabs/ImpactMarker`의 `Model` (총 `OneBulletGun`의 `Impact Marker Prefab`) |
| `Materials/Ground.mat` | 바닥 | `Ground` |
| `Materials/StageMap.mat` | 미술관 지도 탁자 (전투 장소 선택) | `1_Museum` 씬의 `StageMap` |
| `Materials/PaperCraftTable.mat` | 미술관 종이 합성대 | `1_Museum` 씬의 `PaperCraftTable` |
| `Materials/PaintWorkbench.mat` | 미술관 그림 작업대 | `1_Museum` 씬의 `PaintWorkbench` |
| `Materials/PaintBurst.mat` | 처치 때 터지는 물감 방울(작은 공 입자, 색은 떨어뜨린 물감 색으로 코드가 입힌다) | 프리팹 `Prefabs/PaintBurst`의 ParticleSystem (공 모양은 Renderer의 Mesh) |
| `Materials/PaintSplat.mat` | 처치한 자리 바닥에 남는 물감 자국(납작한 원, 약간 젖은 느낌, 색은 코드가 입힌다) | 프리팹 `Prefabs/PaintBurst`의 `Puddle` |
| `Materials/Enemy_Shield.mat` | 방어형 적 몸 (푸르스름한 회색 돌) | 프리팹 `Prefabs/Enemy_Shield`의 `Body`, `Head` (Hitbox 콜라이더 크기도 맞춘다) |
| `Materials/EnemyShieldPlate.mat` | 방어형의 방패 판 (정면 ±60°를 막는다. 충돌 없는 겉모습) | 프리팹 `Prefabs/Enemy_Shield`의 `Shield` |
| `Materials/Enemy_Thrower.mat` | 원거리형 적 몸 (소형 조각, 갈색 돌) | 프리팹 `Prefabs/Enemy_Thrower`의 `Body`, `Head`. 돌이 나가는 자리는 `ThrowPoint` |
| `Materials/EnemyProjectile.mat` | 원거리형이 던지는 돌 | 프리팹 `Prefabs/EnemyProjectile`의 `Model` |
| `Textures/Museum/Museum_Floor_Planks.png` (512×512) | 오래된 미술관 **바닥 그림** (어두운 나무 판, 이어 붙여도 자연스럽게 반복). 임시 그림이라 직접 그린 것으로 덮어쓰면 된다 | `Materials/MuseumFloor_Old.mat` (2m마다 반복) → `1_Museum/MuseumHall/Shell/Floor` |
| `Textures/Museum/Museum_Wall_Panels.png` (512×512) | 오래된 미술관 **벽 그림** (어두운 판벽, 4m마다 반복) | `Materials/MuseumWall_Old_Long.mat`(긴 벽), `MuseumWall_Old_Short.mat`(짧은 벽) → `MuseumHall/Shell/Wall_*` |
| `Materials/MuseumCeiling.mat` | 미술관 천장 (어두운 나무색) | `MuseumHall/Shell/Ceiling` |
| `Materials/MuseumCarpet.mat` | 입구에서 액자 쪽으로 깔린 붉은 카펫 | `MuseumHall/Decor/Carpet` |
| `Materials/MuseumLampShade.mat`, `MuseumLampCord.mat` | 천장에 매달린 등의 갓(따뜻한 불빛, 발광)과 줄 | `MuseumHall/Lights/LampShade`, `LampCord` |
| 에셋 `Assets/Creation_Forge Projects/Old Furniture/**` (옷장·유리 탁자·옛날 의자 2종) | 미술관 가구 — 에셋 제공 그대로 (URP). 3ds Max 모델로 바꿀 때는 `MuseumHall/Decor` 아래 이름별(`Wardrobe_*`, `GlassTable`, `OldChair`, `StrawChair`)로 교체 | `1_Museum/MuseumHall/Decor` |
| `Materials/CastleFloor.mat` | 성 마을 바닥(밝은 풀색 평면, 멀리 깔린 것도 같이 씀) | `3_CastleVillage/Environment/Ground/Floor`, `Backdrop/FloorFar` |
| `Materials/CastleRoad.mat` | 성 마을의 흙길과 마을 광장 바닥 | `Environment/Ground/Road`, `Plaza` |
| `Materials/CastleCourtyard.mat` | 성 안 돌바닥 | `Environment/Ground/Courtyard` |
| 에셋 `Assets/3D Cartoon Village/Materials/*.mat` (31개) | 성벽·탑·집·소품의 색별 머티리얼 (에셋 제공, URP로 변환). 모델을 3ds Max로 바꿀 때는 `Environment/Castle`, `Village`, `Field` 묶음별로 교체 | `3_CastleVillage/Environment` 아래 에셋 프리팹들 |
| `Materials/DesertFloor.mat` | 사막 바닥(모래색 평면, 멀리 깔린 것도 같이 씀) | `4_Desert/Environment/Ground/Floor`, `Backdrop/FloorFar` |
| 에셋 `Assets/Tiny Teacup Studio/Low Poly Desert Environment/Materials/Mat_01.mat` | 사막의 절벽·바위·선인장·고목 전부가 같이 쓰는 색 팔레트 머티리얼 (에셋 제공, URP로 변환). 색을 바꾸려면 `Palette.png`를 고친다 | `4_Desert/Environment` 아래 에셋 프리팹들 (`Cliffs`, `Rocks`, `Plants`, `SmallDecor` 묶음별로 교체) |
| 에셋 `Assets/AllSkyFree/*/*.mat` (하늘 머티리얼 3개) | 맵의 하늘. 숲 `Cartoon Base BlueSky/Day_BlueSky_Nothing`, 성 마을 `Epic_BlueSunset/Epic_BlueSunset`, 사막 `Epic_GloriousPink/Epic_GloriousPink` (에셋 제공 6면 큐브맵). 직접 그린 하늘로 바꾸려면 씬의 Lighting 설정(Window > Rendering > Lighting)에서 Skybox Material만 바꾼다. 맵을 다시 만든 뒤에는 메뉴 `LastBullet/Phase 15 맵마다 하늘 바꾸기`를 다시 실행 | `2_Forest`, `3_CastleVillage`, `4_Desert`의 RenderSettings.skybox |
| 에셋 `Assets/AllSkyFree/*/*.mat` 중 밤하늘 3개 | 밤의 하늘. 숲 `Cartoon Base NightSky`, 성 마을 `Night MoonBurst`, 사막 `Cold Night`. 직접 그린 하늘로 바꾸려면 씬의 `DayNight` 오브젝트의 Night Sky 칸에 머티리얼을 끼운다 | `2_Forest`, `3_CastleVillage`, `4_Desert`의 `DayNight` |
| `Materials/ForestFloor.mat` | 숲 바닥(풀색 평면, 풀 타일 아래·멀리 깔림) | `2_Forest/Environment/Ground/Floor`, `Backdrop/FloorFar` |
| 에셋 `Assets/RPGPP_LT/Materials/rpgpp_lt_mat_a.mat` | 숲의 나무·바위·건물·풀 타일 전부가 같이 쓰는 색 팔레트 머티리얼 (에셋 제공, URP로 변환). 색을 바꾸려면 `Textures/rpgpp_lt_tex_a.tga` 팔레트를 고친다 | `2_Forest/Environment` 아래 에셋 프리팹들 (나무·바위·건물 모델 교체는 `Environment` 묶음별로) |
| `Materials/PictureFrame.mat` | 미술관 액자 테두리 (금색) | `PictureFrame_01/Frame` (모델로 바꿀 때 `Picture` 면 위치·크기만 맞춘다) |
| `Materials/FramePicture.mat` | 액자의 그림 면 (정사각형, Unlit). 걸린 그림이 코드로 이 면에 들어간다 | `PictureFrame_01/Picture` |

## 앞으로 생길 UI (만들 때 이 표에 추가)

| UI | 나오는 Phase | 그림 칸 |
|---|---|---|
| 그림판 버튼 (펜 굵기, 색, 지우개, 완성, 취소) | 6~7 | 버튼 보통/마우스 올림/누름, 아이콘 |
| 확인창, 작품 목록 | 7~8 | 패널, 버튼 |
| 일시정지 | 9 | 패널, 버튼 |

버튼과 패널은 **9-slice**로 만든다: 모서리 장식이 늘어나지 않고 가운데만 늘어난다. 그릴 때 모서리를 네 귀퉁이에 몰아서 그리면 된다.

## 3ds Max 모델 넣기 (주인공 등)

1. 3ds Max에서 **FBX**로 내보낸다. 키는 실제 크기 기준 약 **2m**, 피벗(기준점)은 **발바닥**에 둔다.
2. `Assets/Models/`에 넣는다.
3. 넣었다고 알려 주면 Claude가 크기·축 설정을 맞추고 `Player/Body` 자리에 넣는다.

## 3ds Max 적 모델과 헤드 판정

보이는 모델과 맞는 판정을 따로 둔다. 판정은 보이지 않는 단순한 도형(콜라이더)이다.

```
Enemy_Charger            ← 기능 (EnemyHealth, EnemyAI, EnemyDrop, NavMeshAgent), Enemy 태그
 ├ Model                 ← 3ds Max FBX. 보이기만 함 (콜라이더 없음)
 ├ Body                  ← 상자/캡슐 콜라이더 + Hitbox(Body), 겉모습 끔
 └ Head                  ← 구(Sphere) 콜라이더 + Hitbox(Head), 겉모습 끔
```

3ds Max에서:
1. 머리를 몸과 다른 오브젝트로 나누고 이름을 `Head`, `Body`로 한다.
2. 머리를 몸보다 약간 크게 만든다 (기획서 2.11).
3. 실제 크기, 기준점(피벗)은 발바닥.
4. 애니메이션이 있으면 머리 뼈 이름도 `Head`로 한다. 머리 판정을 그 뼈 아래에 붙여 함께 움직이게 한다.
5. FBX로 `Assets/Models/`에 넣고 알려 주면, Claude가 크기·축을 맞추고 판정 크기를 모델에 맞춘다.

복잡한 모델 모양(MeshCollider)으로 판정하지 않는다: 느리고 판정이 들쭉날쭉하다.

새 UI나 오브젝트가 생길 때마다 이 표에 추가한다.





















