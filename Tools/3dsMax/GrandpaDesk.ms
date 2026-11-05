-- ============================================================
-- 할아버지의 일기장 책상 (GrandpaDesk) 만들기
-- 3ds Max 2026용 MaxScript
--
-- 2026-11-05 사용자 요청으로 다시 만듦: 미술관의 종이 합성대(복원 작업대)와 크기·색·구조를 비슷하게.
--   윗면 높이 120cm, 윗판 210×104cm, 몸통 184×88×91cm, 왼쪽 서랍 3칸 + 오른쪽 문짝, 황동 장식, 네 다리.
--
-- 쓰는 법: 3ds Max 메뉴 Scripting > Run Script... 에서 이 파일을 고른다.
--   (또는 화면 없이: 3dsmaxbatch.exe 이 파일)
-- 하는 일:
--   1) 장면에 이미 GrandpaDesk가 있으면 지우고 새로 만든다 (다른 물체는 건드리지 않는다)
--   2) 책상 + 펼친 일기장 + 책 + 잉크병·깃펜 + 촛대 + 작은 사진 액자를 만든다
--   3) 유니티 프로젝트 Assets/Models/GrandpaDesk/GrandpaDesk.fbx 로 내보낸다
--
-- 크기는 센티미터(cm)로 적었다. 3ds Max 단위 설정이 무엇이든 cm로 맞춰 만든다.
-- 바닥 가운데가 원점(0,0,0)이고, 책상 앞쪽은 -Y 방향(Front 뷰에서 보이는 쪽)이다.
-- 모양을 바꾸고 싶으면 아래 숫자(가로 w, 깊이 d, 높이 h, 위치)를 고치고 다시 실행하면 된다.
-- ============================================================

(
    -- 1cm가 3ds Max 내부 단위로 얼마인지 (단위 설정을 바꾸지 않으려고)
    local CM = units.decodeValue "1cm"

    -- 내보낼 곳 (유니티 프로젝트)
    local exportDir  = @"C:\Users\ih551\OneTapGame\Assets\Models\GrandpaDesk"
    local exportPath = exportDir + @"\GrandpaDesk.fbx"

    -- ---------- 예전 것 지우기 ----------
    fn collectTree o arr =
    (
        append arr o
        for c in o.children do collectTree c arr
        arr
    )
    local old = getNodeByName "GrandpaDesk"
    if old != undefined do delete (collectTree old #())

    -- ---------- 머티리얼 (색은 종이 합성대와 같은 호두나무색·황동색) ----------
    fn mkMat nm c selfIllum:0 =
    (
        local m = StandardMaterial name:nm
        m.diffuse = c
        m.ambient = c
        m.specularLevel = 8
        m.glossiness = 15
        m.selfIllumAmount = selfIllum
        m
    )
    local matWood     = mkMat "GrandpaDesk_Wood"      (color 163 135 108)   -- 합성대 호두나무
    local matWoodDark = mkMat "GrandpaDesk_WoodDark"  (color 102 93 86)     -- 서랍 둘레 어두운 틈
    local matBrass    = mkMat "GrandpaDesk_Brass"     (color 198 174 126)   -- 합성대 황동
    local matLeather  = mkMat "GrandpaDesk_Leather"   (color 112 54 42)     -- 일기장 가죽
    local matMat      = mkMat "GrandpaDesk_Mat"       (color 150 104 104)   -- 책상 위 가죽 깔개 (합성대와 같은 색)
    local matPaper    = mkMat "GrandpaDesk_Paper"     (color 232 222 196)
    local matRibbon   = mkMat "GrandpaDesk_Ribbon"    (color 168 42 42)
    local matBookA    = mkMat "GrandpaDesk_BookGreen" (color 72 96 72)
    local matBookB    = mkMat "GrandpaDesk_BookBlue"  (color 62 78 108)
    local matInk      = mkMat "GrandpaDesk_Ink"       (color 34 38 54)
    local matFeather  = mkMat "GrandpaDesk_Feather"   (color 236 234 226)
    local matCandle   = mkMat "GrandpaDesk_Candle"    (color 240 232 212)
    local matFlame    = mkMat "GrandpaDesk_Flame"     (color 255 196 90) selfIllum:100

    -- ---------- 만들기 도우미 ----------
    -- 상자: 가운데 아래(바닥면 가운데)가 pos. fillet > 0 이면 모서리를 살짝 깎은 상자(ChamferBox)
    fn addBox nm w d h pos mat par fillet:0.5 rot:[0,0,0] =
    (
        local o
        if fillet > 0 then
        (
            o = ChamferBox width:(w*CM) length:(d*CM) height:(h*CM) Fillet:(fillet*CM) Fillet_Segments:1 mapCoords:true
            try (o.smooth = false) catch ()
        )
        else
            o = Box width:(w*CM) length:(d*CM) height:(h*CM) mapCoords:true
        o.name = nm
        o.material = mat
        if rot != [0,0,0] do rotate o (eulerAngles rot.x rot.y rot.z)
        o.pos = pos * CM
        o.parent = par
        o
    )
    -- 원기둥: 바닥면 가운데가 pos
    fn addCyl nm r h pos mat par sides:10 =
    (
        local o = Cylinder radius:(r*CM) height:(h*CM) sides:sides heightsegs:1 capsegs:1 smooth:false mapCoords:true
        o.name = nm
        o.material = mat
        o.pos = pos * CM
        o.parent = par
        o
    )
    -- 묶음(빈 물체). 여러 조각을 함께 돌리고 옮길 때 쓴다
    fn addGroup nm pos par =
    (
        local d = Dummy name:nm boxsize:([4,4,4]*CM)
        d.pos = pos * CM
        d.parent = par
        d
    )

    -- ---------- 맨 위 묶음 ----------
    local root = Dummy name:"GrandpaDesk" boxsize:([10,10,10]*CM)
    root.pos = [0,0,0]

    -- ============ 책상 본체 (종이 합성대처럼) ============
    -- 윗면 높이 120cm. 윗판 210×104×10, 몸통 184×88×91 (바닥에서 18cm 띄움), 네 다리.
    local desk = addGroup "Desk" [0,0,0] root
    local TOP = 120.0
    local FY = -44.0          -- 몸통 앞면 y 위치

    -- 윗판 + 앞 두 모서리 황동 덮개
    addBox "Worktop" 210 104 10 [0,0,TOP-10] matWood desk fillet:1.0
    addBox "WorktopCorner_L" 11 11 10 [-99,-46,TOP-10] matBrass desk fillet:0.4
    addBox "WorktopCorner_R" 11 11 10 [ 99,-46,TOP-10] matBrass desk fillet:0.4

    -- 몸통
    addBox "Body" 184 88 91 [0,0,18] matWood desk fillet:0.8
    addBox "BottomRail" 184 4 8 [0,FY-0.5,18] matWood desk fillet:0.4

    -- 네 다리 + 황동 신발
    for p in #([-83,-35],[83,-35],[-83,35],[83,35]) do
    (
        addBox "Leg" 18 18 21 [p[1],p[2],0] matWood desk fillet:0.8
        addBox "LegShoe" 19 19 5 [p[1],p[2],0] matBrass desk fillet:0.3
    )

    -- 앞면: 어두운 틈(안쪽) 위에 서랍 3칸과 문짝이 얹힌다
    addBox "FrontRecess" 170 2 82 [0,FY-0.2,25] matWoodDark desk fillet:0.3
    -- 앞 세로 기둥 (왼쪽 끝, 가운데, 오른쪽 끝)
    addBox "Stile_L"   9 4 83 [-88,FY-1,25] matWood desk fillet:0.4
    addBox "Stile_Mid" 9 4 83 [ 14,FY-1,25] matWood desk fillet:0.4
    addBox "Stile_R"   9 4 83 [ 88,FY-1,25] matWood desk fillet:0.4

    -- 왼쪽 서랍 3칸 (이름표 꽂이 + 종이 이름표 + 둥근 손잡이)
    for i = 1 to 3 do
    (
        local z = 27 + (i-1)*26
        local s = i as string
        addBox ("Drawer_0" + s) 94 2.5 23 [-39,FY-1.4,z] matWood desk fillet:0.5
        addBox ("DrawerLabelHolder_0" + s) 17 1.4 9 [-62,FY-3.2,z+7] matBrass desk fillet:0.3
        addBox ("DrawerLabelInsert_0" + s) 12 0.6 5.4 [-62,FY-4.1,z+8.8] matPaper desk fillet:0
        addCyl ("DrawerKnobStem_0" + s) 1.3 1.6 [-28,FY-3.6,z+11] matBrass desk sides:8
        addBox ("DrawerKnob_0" + s) 5.2 2.6 5.2 [-28,FY-5.0,z+9.5] matBrass desk fillet:1.2
    )

    -- 오른쪽 문짝 + 안쪽 판 + 손잡이 + 경첩 두 개
    addBox "Door" 66 2.5 78 [51,FY-1.4,27] matWood desk fillet:0.5
    addBox "DoorInsetShadow" 42 0.8 60 [51,FY-3.0,36] matWoodDark desk fillet:0.2
    addBox "DoorInsetPanel" 38 1.2 56 [51,FY-3.6,38] matWood desk fillet:0.4
    addBox "DoorKnob" 5.2 2.6 5.2 [27,FY-5.0,70] matBrass desk fillet:1.2
    addBox "DoorHinge_1" 3 1.6 10 [83,FY-3.0,44] matBrass desk fillet:0.3
    addBox "DoorHinge_2" 3 1.6 10 [83,FY-3.0,90] matBrass desk fillet:0.3

    -- ============ 책상 위 가죽 깔개 (합성대의 버건디 깔개처럼) ============
    addBox "DeskMat" 96 56 0.6 [-34,-6,TOP] matMat desk fillet:0.2

    -- ============ 펼친 일기장 (Diary) ============
    -- 유니티에서 상호작용(E키로 읽기)을 붙일 수 있게 따로 묶어 둔다.
    -- DiaryPage_L / DiaryPage_R 윗면에는 나중에 글씨 그림(텍스처)을 입힐 수 있다.
    local diaryZ = TOP + 0.6
    local diary = addGroup "Diary" [-34,-6,diaryZ] root
    addBox "DiaryCover_L" 22 30 0.8 [-45,-6,diaryZ] matLeather diary fillet:0.3
    addBox "DiaryCover_R" 22 30 0.8 [-23,-6,diaryZ] matLeather diary fillet:0.3
    addBox "DiarySpine"   3 30 1.0 [-34,-6,diaryZ] matLeather diary fillet:0.3
    addBox "DiaryPage_L"  20.5 28 1.4 [-44.6,-6,diaryZ+0.8] matPaper diary fillet:0.25
    addBox "DiaryPage_R"  20.5 28 1.4 [-23.4,-6,diaryZ+0.8] matPaper diary fillet:0.25
    -- 빨간 책갈피 끈 (앞쪽으로 늘어짐)
    addBox "DiaryRibbon"  0.8 18 0.15 [-30,-12,diaryZ+2.2] matRibbon diary fillet:0
    addBox "DiaryRibbonTail" 0.8 0.15 3 [-30,-21.1,diaryZ-0.8] matRibbon diary fillet:0
    rotate diary (eulerAngles 0 0 -6)

    -- ============ 덮어 둔 책 두 권 (오른쪽) ============
    -- 책 한 권: 아래 표지 + 종이 + 위 표지 + 왼쪽 책등 (종이 옆면이 보이게)
    fn addBook nm w d h p mat par =
    (
        local g = addGroup nm p par
        addBox (nm + "_CoverBottom") w d 0.4 p mat g fillet:0.15
        addBox (nm + "_Pages") (w-1.0) (d-1.0) (h-0.8) (p + [0.5,0,0.4]) matPaper g fillet:0.1
        addBox (nm + "_CoverTop") w d 0.4 (p + [0,0,h-0.4]) mat g fillet:0.15
        addBox (nm + "_Spine") 0.8 d h (p + [-(w/2.0)+0.4,0,0]) mat g fillet:0.2
        g
    )
    local books = addGroup "BookStack" [52,-4,TOP] root
    local b1 = addBook "Book_01" 20 27 4 [52,-4,TOP] matBookA books
    local b2 = addBook "Book_02" 17 23 3.5 [52,-4,TOP+4] matLeather books
    rotate b2 (eulerAngles 0 0 14)

    -- ============ 잉크병과 깃펜 (가운데 앞) ============
    local ink = addGroup "Inkwell" [12,-22,TOP] root
    addCyl "Inkwell_Bottle" 3.2 4.0 [12,-22,TOP] matInk ink sides:8
    addCyl "Inkwell_Neck"   1.6 1.4 [12,-22,TOP+4.0] matInk ink sides:8
    addCyl "Inkwell_Rim"    1.9 0.5 [12,-22,TOP+5.2] matBrass ink sides:8
    -- 깃펜: 원점에서 만들고 기울여서 잉크병에 꽂는다
    local quill = addGroup "Quill" [0,0,0] ink
    addCyl "Quill_Shaft" 0.25 20 [0,0,0] matFeather quill sides:5
    addBox "Quill_Feather" 3.0 0.2 12 [0.6,0,7.5] matFeather quill fillet:0
    rotate quill (eulerAngles 0 28 -20)
    quill.pos = [12,-22,TOP+2.5] * CM

    -- ============ 촛대 (오른쪽 뒤) ============
    local candle = addGroup "Candle" [86,26,TOP] root
    addCyl "Candle_Base"   6 1.2 [86,26,TOP] matBrass candle sides:10
    addCyl "Candle_Stem"   1.2 6 [86,26,TOP+1.2] matBrass candle sides:8
    addCyl "Candle_Cup"    2.8 1.2 [86,26,TOP+7.2] matBrass candle sides:10
    addCyl "Candle_Wax"    1.8 11 [86,26,TOP+8.4] matCandle candle sides:8
    local flame = Cone radius1:(0.9*CM) radius2:0 height:(2.6*CM) sides:6 heightsegs:1 smooth:false
    flame.name = "Candle_Flame"
    flame.material = matFlame
    flame.pos = [86,26,TOP+19.6] * CM
    flame.parent = candle

    -- ============ 작은 사진 액자 (왼쪽 뒤, 정사각형) - 할아버지의 추억 ============
    local photo = addGroup "PhotoFrame" [-86,28,TOP] root
    addBox "PhotoFrame_Border" 12 1.4 12 [-86,28,TOP+0.6] matBrass photo fillet:0.3
    addBox "PhotoFrame_Picture" 9 0.5 9 [-86,27.2,TOP+2.1] matPaper photo fillet:0
    addBox "PhotoFrame_Stand" 1.6 7 1 [-86,32,TOP] matBrass photo fillet:0.1
    rotate photo (eulerAngles 0 0 -12)

    -- ---------- 유니티로 내보내기 ----------
    makeDir exportDir all:true
    clearSelection()
    select (collectTree root #())

    FBXExporterSetParam "ResetExport"
    FBXExporterSetParam "UpAxis" "Y"
    FBXExporterSetParam "ConvertUnit" "cm"
    FBXExporterSetParam "Animation" false
    FBXExporterSetParam "Cameras" false
    FBXExporterSetParam "Lights" false
    FBXExporterSetParam "EmbedTextures" false
    FBXExporterSetParam "SmoothingGroups" true
    FBXExporterSetParam "FileVersion" "FBX202000"

    local ok = exportFile exportPath #noPrompt selectedOnly:true using:FBXEXP
    clearSelection()

    if ok then
        format "[GrandpaDesk] 만들기와 내보내기 완료: %\n" exportPath
    else
        format "[GrandpaDesk] 내보내기 실패: %\n" exportPath
)
