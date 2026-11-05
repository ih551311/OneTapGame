-- ============================================================
-- 할아버지의 일기장 책상 (GrandpaDesk) 만들기
-- 3ds Max 2026용 MaxScript
--
-- 쓰는 법: 3ds Max 메뉴 Scripting > Run Script... 에서 이 파일을 고른다.
--   (또는 이 파일을 3ds Max 화면 위로 끌어다 놓는다)
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

    -- ---------- 머티리얼 (색은 기존 이젤·포털·작업대와 비슷한 차분한 나무색) ----------
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
    local matWood     = mkMat "GrandpaDesk_Wood"      (color 140 116 96)
    local matWoodDark = mkMat "GrandpaDesk_WoodDark"  (color 108 88 73)
    local matBrass    = mkMat "GrandpaDesk_Brass"     (color 196 164 108)
    local matLeather  = mkMat "GrandpaDesk_Leather"   (color 112 54 42)
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

    -- ============ 책상 본체 ============
    -- 크기: 가로 140, 깊이 70, 높이 76 (책상 윗면 높이 76cm)
    local desk = addGroup "Desk" [0,0,0] root
    local TOP = 76.0

    addBox "Top" 140 70 4 [0,0,TOP-4] matWood desk fillet:0.8
    -- 윗면 아래 테두리 띠(위판을 두꺼워 보이게)
    addBox "TopTrim" 136 66 2 [0,0,TOP-6] matWoodDark desk fillet:0.3

    -- 왼쪽 서랍장 (작업대처럼 서랍 3칸)
    addBox "Pedestal" 44 62 64 [-46,0,6] matWood desk fillet:0.6
    for i = 1 to 3 do
    (
        local z = 8 + (i-1)*20.5
        addBox ("Drawer_0" + (i as string)) 40 2 18.5 [-46,-31.6,z] matWoodDark desk fillet:0.4
        -- 서랍 손잡이 (황동)
        addBox ("DrawerHandle_0" + (i as string)) 10 1.2 2 [-46,-33.2,z+12.5] matBrass desk fillet:0.3
        -- 이름표 꽂이 (작업대 서랍처럼)
        addBox ("DrawerLabel_0" + (i as string)) 7 0.6 3.5 [-46,-32.9,z+4] matBrass desk fillet:0.2
    )
    -- 서랍장 발 4개
    for p in #([-64,-27],[-28,-27],[-64,27],[-28,27]) do
        addBox "PedestalFoot" 6 6 6 [p[1],p[2],0] matWoodDark desk fillet:0.5

    -- 오른쪽 다리 2개 + 옆 가로대
    addBox "Leg_Front" 6 6 (TOP-6) [63,-28,0] matWood desk fillet:0.6
    addBox "Leg_Back"  6 6 (TOP-6) [63, 28,0] matWood desk fillet:0.6
    addBox "SideRail"  4 50 6 [63,0,12] matWoodDark desk fillet:0.3

    -- 뒤판 (서랍장과 오른쪽 다리 사이)
    addBox "BackPanel" 84 2 32 [18,31,38] matWoodDark desk fillet:0.3

    -- 가운데 얕은 서랍
    addBox "CenterDrawer" 80 2 10 [18,-31.6,59.5] matWoodDark desk fillet:0.4
    addBox "CenterDrawerHandle" 12 1.2 2 [18,-33.2,63.5] matBrass desk fillet:0.3

    -- ============ 뒤쪽 작은 선반(책상 위) ============
    local hutch = addGroup "Hutch" [0,0,TOP] desk
    addBox "HutchBack"    132 2 28 [0,31,TOP] matWood hutch fillet:0.4
    addBox "HutchSide_L"  2 16 28 [-65,23,TOP] matWood hutch fillet:0.3
    addBox "HutchSide_R"  2 16 28 [ 65,23,TOP] matWood hutch fillet:0.3
    addBox "HutchShelf"   128 15 1.6 [0,23.5,TOP+13] matWoodDark hutch fillet:0.3
    addBox "HutchTop"     134 18 2 [0,22.5,TOP+28] matWood hutch fillet:0.5
    addBox "HutchDivider_L" 1.6 15 13 [-34,23.5,TOP] matWoodDark hutch fillet:0.2
    addBox "HutchDivider_R" 1.6 15 13 [ 34,23.5,TOP] matWoodDark hutch fillet:0.2
    -- 위 모서리 황동 장식 (포털 모서리처럼)
    addBox "BrassCorner_L" 4 0.6 4 [-63.5,14.2,TOP+25] matBrass hutch fillet:0.1
    addBox "BrassCorner_R" 4 0.6 4 [ 63.5,14.2,TOP+25] matBrass hutch fillet:0.1

    -- 선반 위 세워 둔 책들
    local shelfZ = TOP + 14.6
    local standing = #( #(-58,3.0,12,matBookA), #(-54.6,2.6,11,matLeather), #(-51.4,3.2,12.5,matBookB), #(-48,2.4,10.5,matBookA) )
    for i = 1 to standing.count do
    (
        local b = standing[i]
        addBox ("ShelfBook_0" + (i as string)) b[2] 12 b[3] [b[1],24,shelfZ] b[4] hutch fillet:0.3
    )
    -- 선반 위 작은 사진 액자 (정사각형) - 할아버지의 추억
    local photo = addGroup "PhotoFrame" [30,24,shelfZ] hutch
    addBox "PhotoFrame_Border" 10 1.2 10 [30,24,shelfZ+1] matBrass photo fillet:0.3
    addBox "PhotoFrame_Picture" 7.6 0.4 7.6 [30,23.3,shelfZ+2.2] matPaper photo fillet:0
    addBox "PhotoFrame_Stand" 1.5 6 1 [30,27,shelfZ] matBrass photo fillet:0.1
    rotate photo (eulerAngles 0 0 -12)

    -- ============ 펼친 일기장 (Diary) ============
    -- 유니티에서 상호작용(E키로 읽기)을 붙일 수 있게 따로 묶어 둔다.
    -- DiaryPage_L / DiaryPage_R 윗면에는 나중에 글씨 그림(텍스처)을 입힐 수 있다.
    local diary = addGroup "Diary" [0,-8,TOP] root
    addBox "DiaryCover_L" 22 30 0.8 [-11,-8,TOP] matLeather diary fillet:0.3
    addBox "DiaryCover_R" 22 30 0.8 [ 11,-8,TOP] matLeather diary fillet:0.3
    addBox "DiarySpine"   3 30 1.0 [0,-8,TOP] matLeather diary fillet:0.3
    addBox "DiaryPage_L"  20.5 28 1.4 [-10.6,-8,TOP+0.8] matPaper diary fillet:0.25
    addBox "DiaryPage_R"  20.5 28 1.4 [ 10.6,-8,TOP+0.8] matPaper diary fillet:0.25
    -- 빨간 책갈피 끈 (앞쪽으로 늘어짐)
    addBox "DiaryRibbon"  0.8 18 0.15 [4,-14,TOP+2.2] matRibbon diary fillet:0
    addBox "DiaryRibbonTail" 0.8 0.15 3 [4,-23.1,TOP-0.8] matRibbon diary fillet:0
    rotate diary (eulerAngles 0 0 -6)

    -- ============ 덮어 둔 책 두 권 (왼쪽) ============
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
    local books = addGroup "BookStack" [-46,8,TOP] root
    local b1 = addBook "Book_01" 20 27 4 [-46,8,TOP] matBookA books
    local b2 = addBook "Book_02" 17 23 3.5 [-46,8,TOP+4] matLeather books
    rotate b2 (eulerAngles 0 0 14)

    -- ============ 잉크병과 깃펜 (오른쪽 앞) ============
    local ink = addGroup "Inkwell" [30,-10,TOP] root
    addCyl "Inkwell_Bottle" 3.2 4.0 [30,-10,TOP] matInk ink sides:8
    addCyl "Inkwell_Neck"   1.6 1.4 [30,-10,TOP+4.0] matInk ink sides:8
    addCyl "Inkwell_Rim"    1.9 0.5 [30,-10,TOP+5.2] matBrass ink sides:8
    -- 깃펜: 원점에서 만들고 기울여서 잉크병에 꽂는다
    local quill = addGroup "Quill" [0,0,0] ink
    addCyl "Quill_Shaft" 0.25 20 [0,0,0] matFeather quill sides:5
    addBox "Quill_Feather" 3.0 0.2 12 [0.6,0,7.5] matFeather quill fillet:0
    rotate quill (eulerAngles 0 28 -20)
    quill.pos = [30,-10,TOP+2.5] * CM

    -- ============ 촛대 (오른쪽 뒤) ============
    local candle = addGroup "Candle" [52,10,TOP] root
    addCyl "Candle_Base"   6 1.2 [52,10,TOP] matBrass candle sides:10
    addCyl "Candle_Stem"   1.2 6 [52,10,TOP+1.2] matBrass candle sides:8
    addCyl "Candle_Cup"    2.8 1.2 [52,10,TOP+7.2] matBrass candle sides:10
    addCyl "Candle_Wax"    1.8 11 [52,10,TOP+8.4] matCandle candle sides:8
    local flame = Cone radius1:(0.9*CM) radius2:0 height:(2.6*CM) sides:6 heightsegs:1 smooth:false
    flame.name = "Candle_Flame"
    flame.material = matFlame
    flame.pos = [52,10,TOP+19.6] * CM
    flame.parent = candle

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
