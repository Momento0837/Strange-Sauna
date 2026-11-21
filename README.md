# 수상한 찜질방 (Strange-Sauna)

사우나에서 진상을 상대해보세요.

Unity **6.3 LTS / 6000.3.26f1**, **URP 17.3.0의 2D Renderer**로 만든 Windows용 2D 프로토타입입니다.

## 실행

- `실행하기.cmd`를 더블클릭하거나 `Builds/Windows/수상한 찜질방.exe`를 실행하세요. 실행 파일과 같은 폴더의 Data 폴더, DLL은 함께 있어야 합니다.
- 편집하려면 `유니티로 열기.cmd`를 실행하거나 Unity Hub에서 이 프로젝트 폴더를 추가하세요.
- 시작 씬은 `Assets/Scenes/Sauna.unity`입니다. Play를 누르면 메인메뉴가 나옵니다.

## 조작

| 키 | 동작 |
| --- | --- |
| WASD / 방향키 | 이동 |
| F | 테이블 주문 받기 → 손에 든 음식 중 필요한 음식 전달 |
| F | 카운터의 조리 / 계산 / 휴식 / 청소 도구 상호작용 |
| F 길게 / 연타 | 청소 이벤트: 연속 3초 또는 10번 누르기 |
| ESC | 일시정지 / 복귀 / 설정에서 돌아가기 |
| 마우스 휠 | 영수증이 많거나 UI가 클 때 목록 스크롤 |
| 영수증 클릭 | 겹쳐진 주문서를 맨 앞으로 보기 |
| 조리 창 음식 클릭 | 즉시 준비하여 오른손부터 빈손에 들기 |

## 이번 프로토타입의 규칙

- 상단에 카운터, 하단에 **가로 4 × 세로 2, 총 8개의 4인용 테이블**을 배치했습니다. 테이블 사이와 양쪽 외곽으로 이동할 수 있습니다. 플레이어 기본 이동속도는 **8.4**로, 직전 속도 12의 0.7배입니다.
- 맵 외곽은 **24×23**으로, 기존 30×34 대비 면적을 약 46% 줄였습니다. 테이블 중심 간격은 가로·세로 모두 **5**(기존 6 / 11.5)입니다. 테이블과 캐릭터의 크기는 유지하고 카운터·입구·잡화 진열대·대기열을 함께 옮겼습니다. 체크무늬 바닥도 새 크기에 맞춰 배치하여 타일을 늘이거나 찌그러뜨리지 않습니다.
- 손님은 하단 입구에서 1~4명씩 와서 빈 테이블을 예약하고, 중복되지 않는 임의의 좌석으로 이동합니다. **주문·영수증·제한시간·응대 결과는 테이블당 하나**이며, 일반 주문에는 착석 인원수만큼 음식이 담깁니다. 같은 음식을 여러 명이 고를 수 있습니다.
- 손님이 도착했을 때 **착석할 수 있는 깨끗한 빈 테이블이 없으면**, 입구 안쪽까지 들어왔다가 먼지 말풍선을 띄우며 돌아갑니다. 한 테이블을 이용할 일행을 한 단위로 계산하여 **1~4명 모두 감점은 한 번, 기본 −0.25점**입니다. 이때 주문/영수증은 생성하지 않으며, 퇴장이 끝날 때 추가로 감점하지 않습니다. 테이블을 청소하면 다음 일행을 다시 받을 수 있습니다. 깨끗한 빈 테이블이 남아 있고 단순히 설정된 동시 이용 상한에 도달한 경우에는 이 감점을 적용하지 않습니다.
- 테이블 가까이에서 F로 주문을 받습니다. 일반 음식 주문은 필요한 음식 전체를 배달해야 완료됩니다. 일부만 전달한 진행도는 유지되며, 완료하면 일행 전원이 함께 일어나 입구로 퇴장합니다. 알파벳 특수 주문은 두 번째 F로 완료하는 임시 동작입니다.
- 테이블마다 대표 손님 머리 위에 말풍선 하나를 표시합니다. 느낌표는 주문 대기, 숫자/알파벳은 받은 주문입니다. 화면 밖 테이블의 일행은 정사각형 가장자리에서 방향을 표시하며, 겹치는 방향 표시는 묶습니다.
- 영수증은 **플레이 영역 바깥 좌측 상단**에 위에서 내려와 겹쳐 쌓이고, 완료/실패 시 위로 올라갑니다. 글자 크기를 유지한 작은 종이(기본 230×210)에 높이의 절반 간격으로 쌓여, 뒤쪽 주문의 메뉴 일부·테이블 번호·원형 타이머도 보입니다. 주문이 많아도 간격을 줄이지 않고 화면 아래로 이어지며, 완료/실패 시 남은 영수증이 위로 당겨집니다. 노출된 영수증을 클릭하면 전체 주문 내용을 맨 앞으로 가져옵니다. 세로로 긴 창에서는 상단 외부 영역에 압축 표시합니다. 큰 배율에서는 가로·세로로 스크롤할 수 있습니다.
- 원형 타이머는 남은 비율이 50% 초과일 때 초록, 30% 초과~50%일 때 주황, 30% 이하일 때 빨강입니다. 중앙 숫자는 남은 초입니다. 0초가 되면 일행이 떠나며 테이블당 실패가 한 번 적용됩니다.
- 손님이 떠난 테이블은 더러워집니다. 남은 손님이 모두 퇴장하고 테이블에서 떨어진 후 청소할 수 있습니다.
- 카운터 **맨 오른쪽**에서 F로 청소 도구를 듭니다. 청소 도구를 가지고 더러운 테이블에서 F를 누르면 두 청소 방식 중 하나가 무작위로 정해집니다. 테이블에서 멀어지거나 일시정지하면 청소 진행이 초기화됩니다. 도구는 같은 위치에서 F로 내려놓을 수 있습니다.
- 기본 주문 대기는 **100초**, 주문 후 완료 제한시간도 **100초**입니다. 입장하며 이동하는 동안에는 대기시간이 줄지 않습니다.
- 착석한 손님마다 머리 위에 **가로 대기 게이지**가 표시됩니다. 일행은 테이블의 같은 시간을 공유합니다. 주문을 받기 전에는 주문 대기시간, 받은 뒤에는 음식 배달 제한시간을 표시하며, 50% 초과는 초록, 30% 초과~50%는 주황, 30% 이하는 빨강입니다. 일시정지 중에는 게이지도 멈춥니다.
- 주문 접수나 음식 배달이 늦어 시간이 0이 되면 일행 전체가 일어나 입구로 돌아갑니다. 게이지 대신 **먼지 말풍선을 약 2.2초** 띄우고 몸을 작게 흔들며 툴툴거립니다. 별점은 인원수와 관계없이 해당 테이블에 한 번만 −0.25점 적용됩니다.
- 별점은 5점으로 시작합니다. 전체 5점 기준 **실패한 테이블당 5% = −0.25점**, **응대 완료 테이블당 3% = +0.15점**입니다. 1인과 4인 일행 모두 한 번만 적용합니다. 범위는 1~5점이며 1점이 되면 게임오버입니다.
- 숫자/알파벳 확률은 별점 **3점 초과 70:30**, **2점 초과~3점 이하 50:50**, **1점 초과~2점 이하 20:80**입니다. 소수 경계 사이에 빈 구간이 없도록 연속된 기준을 사용합니다.
- Day 시스템은 없습니다. 18초 간격으로 손님이 들어오고, 동시에 최대 7개 테이블에 손님이 배정됩니다. 모두 데이터에서 수정할 수 있습니다.
- 카메라가 보는 정사각형은 창의 짧은 변 × 0.9입니다. 카메라는 맵 밖이 보이지 않는 범위에서 플레이어를 중심으로 따라옵니다. 월드 좌표 범위는 24×23, 카메라 가시 폭은 17.2입니다.
- 메인메뉴에서 창 비율을 자유롭게 변경할 수 있습니다. Windows 빌드에서는 플레이 중(일시정지와 플레이 중 설정 포함) 창 테두리 크기 조절 및 최대화 버튼을 잠급니다. 에디터 자체 창은 잠그지 않습니다.
- 설정의 배경음·효과음 및 UI 배율은 저장됩니다. UI 배율은 100/200/300%이며 플레이 중에는 변경할 수 없습니다. 큰 배율에서 설정과 조리 목록은 세로로 스크롤합니다. 플레이 영역 바깥의 HUD는 제한된 공간에 맞춰 배율 상한을 적용해 원형 타이머가 잘리지 않게 합니다.
- 창이 포커스를 잃으면 자동 일시정지합니다.

## 퀘스트 직접 입력

Project 창에서 **`Assets/Resources/QuestCatalog.asset`**을 선택하세요.

1. **숫자 섹션**: 1~20, 총 20개.
2. **알파벳 섹션**: a~p, 총 16개.
3. 항목마다 `Id`는 그대로 두고 `Title`(이름), `Description`(내용), `Delivery Seconds`(주문 후 제한시간)를 바꾸세요.
4. 직접 입력한 제목과 설명은 영수증에 나옵니다. 기본 설명은 편집용 안내이므로 게임 화면에서 생략합니다.

`Kind = BySection`은 숫자 퀘스트를 음식 주문, 알파벳 퀘스트를 임시 특수 주문으로 처리합니다. 개별 항목을 `FoodOrder` 또는 `Placeholder`로 바꿀 수도 있습니다. 일반 주문의 영수증 본문은 실제 메뉴와 전달 수량을 표시합니다. 특수 주문의 제목·설명은 입력한 내용을 표시합니다. `SaunaGame.OrderAccepted`와 `OrderResolved` 이벤트로 추가 퀘스트 로직을 연결할 수 있습니다. 계산·휴식 카운터는 아직 임시 상호작용입니다.

## 음식 준비와 양손 소지

- 메뉴: 맥반석계란, 식혜, 아이스커피, 바나나우유, 라면, 만두, 핫바, 미역국, 제육덮밥.
- 왼쪽 조리대에서 F를 누르면 전체 메뉴 창이 열립니다. 클릭하면 즉시 오른손, 다음에는 왼손에 듭니다. 빈손이 생기면 그 슬롯을 다시 사용하며, 다른 손의 물건은 자동으로 옮기지 않습니다. 두 손이 차면 창이 닫힙니다. 창을 열고 있는 동안 손님 제한시간은 계속 줄어듭니다.
- 손이 가득 찬 상태에서 조리대에 F를 누르면 캐릭터가 잠깐 떨며 땀을 흘리고, 상단 중앙에 “양 손이 꽉 찼어요!”가 나타났다가 사라집니다.
- 주문받은 테이블에서 F를 누르면 **그 테이블이 아직 필요로 하는 수량만** 전달합니다. 다른 테이블에서 받은 음식도 메뉴가 맞으면 전달할 수 있습니다. 잘못 가져온 음식이나 초과 수량은 손에 남습니다.
- 음식과 청소 도구는 함께 들 수 없습니다. 청소 도구는 오른쪽 카운터에서 F로 반납합니다.
- 조리대 바로 오른쪽의 음식물통에서 F를 누르면 “양 손의 음식을 전부 버립니다!”가 표시됩니다. 같은 위치에서 F를 한 번 더 눌러야 양손을 비웁니다. 멀어지거나 다른 상호작용을 하면 확인 상태가 취소됩니다.
- `Assets/Data/Items/*.asset`에서 이름, 아이콘, 손에 표시할 프리팹을 교체할 수 있습니다. `Assets/Resources/SaunaItemCatalog.asset`이 조리 메뉴 목록입니다.
- `HandInventory`는 음식 전용이 아닌 공용 2칸 인벤토리입니다. `SaunaItemDefinition`은 Food/Reading/Hygiene/Miscellaneous 분류와 선택적 Held Prefab을 지원하므로, 이후 잡지·샴푸·린스를 추가할 수 있습니다. 이 물품들의 획득 장소와 사용 규칙은 추후 연결하면 됩니다.

**`Assets/Resources/SaunaTuning.asset`**에서는 이동속도, 입장 간격, 주문 대기시간, 최대 사용 테이블 수, 별점 변화량, 청소 조건을 조정할 수 있습니다.

## 잡화 구매와 포스기

- 기존 중앙 계산대가 포스기입니다. **카운터 뒤의 응대 위치**에서 F로 맨 앞 손님의 주문을 받고, 같은 위치에서 F로 담은 잡화 전부를 한 번에 전달합니다. 손님 쪽에서 벽 너머로 주문을 받을 수 없습니다.
- 잡화 손님은 한 명씩 들어옵니다. 계산 중인 손님을 포함해 총 3명이 줄을 서며, 앞 손님 처리가 끝나면 차례대로 앞으로 이동합니다. 네 번째 손님은 맵 중앙까지 와서 툴툴거린 뒤 입구로 돌아갑니다. 이때 기본 감점의 0.5배인 **0.125점**이 내려갑니다.
- 메뉴는 일회용 칫솔/치약 세트, 샴푸 소포장, 린스 소포장, 바디워시 소포장, 일회용 면도기, 마스크팩, 때밀이 타올, 때비누, 괄사입니다. 총 1~5개를 무작위로 주문하며 같은 물건을 여러 개 주문할 수 있습니다.
- 주문을 받으면 전체 주문은 손님 머리 위에 **아이콘 × 수량**으로 표시됩니다. 영수증에는 주문의 개별 물건 중 무작위로 뽑은 최대 3개만 기록합니다. 숨긴 항목도 실제 주문에는 포함되며, 영수증을 다시 그리거나 클릭해도 표본은 바뀌지 않습니다. 예를 들어 샴푸가 표본에 두 번 들어가면 샴푸 아이콘 ×2로 합칩니다.
- 잡화 주문은 동시에 하나만 받습니다. 제한시간은 음식 퀘스트들의 평균 배달 제한시간 × `Retail Time Multiplier`(기본 0.5), 현재 **50초**입니다. 조리·잡화 선택 중에도 시간이 흐르고 일시정지 중에는 멈춥니다. 순서를 기다리는 손님은 대기시간을 소모하지 않고, 포스기 맨 앞에 도착한 뒤부터 주문 대기시간이 흐릅니다.
- 포스기 오른쪽 뒤편 **잡화 진열대**에서 F를 누르면 선택 창이 열립니다. 최대 5개를 담을 수 있고, 아래 소지품 칸을 누르면 물건을 진열대로 내려놓습니다. 플레이어에게 봉투와 물건이 표시되며 HUD에서도 5칸 소지품을 볼 수 있습니다.
- 음식을 들고 있으면 “음식을 서빙 중입니다!”, 청소 도구를 들고 있으면 “청소 중입니다!”로 포스기 상호작용을 막습니다. 잡화 주문을 받은 뒤에는 음식 조리와 청소 도구 획득을 “카운터에 기다리는 손님이 있어요!”로 막습니다.
- 전달 시 중복 수량까지 정확하면 기본 성공 보상 **+0.15점**을 한 번 받고, 손님은 음표 말풍선과 통통 튀는 걸음으로 퇴장합니다. 오류가 있으면 잘못 가져온 물건·초과 수량·부족 수량을 각각 세어 **개당 0.075점**(기본 실패 감점 ×0.3)을 감점하며, 손님은 먼지 말풍선과 조용한 걸음으로 나갑니다. 다른 물건으로 대체한 경우 잘못 가져온 1개와 원래 주문의 부족 1개가 각각 계산됩니다. 모든 물건은 전달 판정 시 소지품에서 사라집니다.
- 시간 초과는 기존과 같은 **−0.25점**입니다. 잡화 선택 창과 소지품이 정리되어 다음 손님을 받을 수 있습니다. 별점 1점이면 게임오버, 다시 시작하면 잡화 손님·대기열·주문·소지품도 초기화됩니다.
- `Assets/Data/Items`의 잡화 데이터와 `SaunaItemCatalog.retailMenu`에서 이름·아이콘·판매 목록을 교체할 수 있습니다. `RegisterStation/SaunaRetailLayout`의 응대·대기·퇴장 기준점, `RetailShelf`, `RetailCustomer.prefab`의 `SpeechCanvas`, `UI/RetailPicking`은 씬/프리팹에 저장되어 있습니다. 아이콘과 말풍선 표시 방식은 View에서 바꿀 수 있습니다.

## MVC 구조

Model은 상태와 데이터, View는 그리기와 애니메이션, Controller는 입력과 게임 규칙을 담당합니다. 길찾기·오디오·운영체제 창 제어는 공통 Systems 폴더에 두었습니다. 영수증은 주문 이벤트를 구독하는 View에서 생성하고 애니메이션을 처리합니다. 모델에는 GameObject, SpriteRenderer, 입력 처리 코드가 없습니다.

| 파일 | 역할 |
| --- | --- |
| `Assets/Scripts/Models/SaunaSessionModel.cs` | 세션, 테이블 주문, 손님 이동, 청소 상태 데이터 |
| `Assets/Scripts/Models/QuestCatalog.cs` | 숫자/알파벳 퀘스트 ScriptableObject |
| `Assets/Scripts/Models/SaunaTuning.cs` | 게임 밸런스 ScriptableObject |
| `Assets/Scripts/Models/HandInventory.cs` | 오른손/왼손 공용 아이템 슬롯 |
| `Assets/Scripts/Models/TableFoodOrder.cs` | 음식별 요청/배달 수량 |
| `Assets/Scripts/Models/SaunaItemDefinition.cs` | 음식 및 향후 소지품 ScriptableObject |
| `Assets/Scripts/Views/SaunaWorldView.cs` | 맵·캐릭터·오염 표시, 카메라 표시 갱신 |
| `Assets/Scripts/Views/SaunaUI.cs` | 메인메뉴, HUD, 영수증, 설정, 게임오버 |
| `Assets/Scripts/Views/Receipt.cs` | 영수증 애니메이션 상태 |
| `Assets/Scripts/Views/SaunaArt.cs` | 직접 제작한 픽셀 스프라이트 생성 |
| `Assets/Scripts/Controllers/SaunaGame.cs` | 이동, 상호작용, 테이블 단위 주문 처리, 청소, 별점, 메뉴 전환 |
| `Assets/Scripts/Controllers/SaunaGame.Service.cs` | 조리·배달·버리기·소지 제한 명령 |
| `Assets/Scripts/Systems/SaunaMap.cs` | 맵 배치, 충돌, A* 이동 경로, 카메라 경계 계산 |
| `Assets/Scripts/Systems/SaunaAudio.cs` | 직접 합성한 배경음과 효과음 |
| `Assets/Scripts/Systems/WindowResizeLock.cs` | Windows 빌드의 플레이 중 창 크기 잠금 |
| `Assets/Scripts/Tests/SaunaSmoke.cs` | 실제 실행 통합 검사 |
| `Assets/Editor/SaunaProjectBuilder.cs` | 프로젝트 설정, 검증, 빌드 |

맵, 카메라, 플레이어, Canvas UI는 **씬에 저장된 실제 오브젝트**입니다. Play 전에도 Scene/Game 뷰에서 확인할 수 있습니다. 실행 중에는 손님과 영수증만 연결된 프리팹에서 생성합니다. 기존 `Assets/Art` 이미지는 실제 SpriteRenderer/Image가 참조하며, 실행 코드가 Sprite를 다시 덮어쓰지 않습니다.

## 하이어라키에서 에셋 교체하기

시작 씬: **Assets/Scenes/Sauna.unity**

```text
GameController             MVC Controller, 데이터 연결, Music / Effects
World                      Scene Layout / World View 참조
  Floor                    바닥 SpriteRenderer
  MapBounds                카메라/이동 경계
  SpawnPoints
    Entrance               손님 입구
    PlayerSpawn            플레이어 시작 위치
  CounterArea              Cooking / FoodWasteBin / Register / Rest / Cleaning Station
    RegisterStation        포스기 / StaffInteraction / CustomerQueue / ExitSide
    RetailShelf            잡화 진열대 / ShelfInteraction
  DiningArea
    Table_01 ... Table_08   Table.prefab 인스턴스
      Visual               테이블 SpriteRenderer
      NumberBadge          우측 상단 원형 번호판 SpriteRenderer
        NumberCanvas/Number  테이블 번호 Text
      Seat_1 ... Seat_4    착석 위치와 ChairVisual
      Dirt                 오염 이미지
  Decor                    장식 프리팹
Actors
  Player                   Player.prefab 인스턴스
    Visual                 캐릭터 SpriteRenderer
    CleaningTool           들고 다니는 청소 도구
    RightHand / LeftHand   공용 소지품 표시 앵커
    SweatEffect            소지 불가 반응
  Guests                   실행 중 손님 프리팹 인스턴스
  RetailGuests             잡화 손님 프리팹 인스턴스
Cameras
  BackgroundCamera
  PlayCamera
UI                         Canvas / SaunaUI
  HUD                      Orders / Status / Rating / WorldIndicators
  MainMenu                 영업하기 / 설정 / 종료
  Pause                    일시정지 패널
  Settings                 Slider / Dropdown / 돌아가기
  GameOver                 다시 시작 / 메인메뉴
  Cooking                  실제 음식 버튼 9개와 닫기 버튼
  RetailPicking            실제 잡화 버튼 9개와 5칸 소지품 버튼
EventSystem
```

테이블 번호는 각 테이블의 **우측 상단 원형 번호판**에 1~8로 표시됩니다. `Table.prefab`의 `NumberBadge`에서 스프라이트·위치·크기를, `NumberCanvas/Number`에서 글꼴·색을 바꿀 수 있습니다. 숫자 내용만 `SaunaTableView.index + 1`로 자동 갱신됩니다.

- **테이블 전체 외형**: `Assets/Prefabs/Table.prefab`을 열고 `Visual`의 **Sprite**를 교체하세요. 8개 테이블에 함께 적용됩니다. 개별 테이블 인스턴스에서만 교체하면 해당 테이블에만 적용됩니다.
- **의자**: Table 프리팹의 `Seat_1~4/ChairVisual`을 수정하세요. 테이블 루트를 움직이면 의자, 착석 위치, 오염 표시, 충돌 영역이 함께 움직입니다.
- **플레이어**: `Assets/Prefabs/Player.prefab`의 `Visual` Sprite를 교체하세요. 캐릭터를 통째로 바꿀 때는 `SaunaCharacterView`의 Body, Visual, Sorting 참조를 연결하세요. Animator를 연결하면 선택한 Walking bool 파라미터를 갱신합니다.
- **손님**: `Guest_1~4.prefab`을 수정하거나 World의 `SaunaWorldView > Guest Prefabs` 목록에 다른 캐릭터 프리팹을 넣으세요.
- **손님 대기 게이지/퇴장 말풍선**: 손님 프리팹의 `GuestStatusCanvas/PatienceGauge`와 `DepartureBubble`에서 위치·크기·이미지를 수정할 수 있습니다. `SaunaGuestStatusView`는 테이블 상태를 받아 표시만 담당합니다.
- **카운터**: `CookingStation / RegisterStation / RestStation / CleaningStation.prefab`의 Visual을 교체하세요.
- **바닥**: World/Floor의 Sprite를 교체하세요. 맵 크기와 카메라 경계는 MapBounds에서 조정합니다.
- **UI**: Canvas 아래의 실제 Image, Text, Button, Slider, Dropdown을 Inspector에서 수정하세요. 비활성 패널을 켜서 편집할 수 있으며, SaunaUI 컴포넌트의 컨텍스트 메뉴에 플레이/메인메뉴 미리보기가 있습니다.
- **영수증/말풍선**: `Receipt.prefab`, `CustomerBubble.prefab`을 편집하세요. HUD/Orders/ReceiptList/Viewport/Content의 ReceiptPreview_EditMode를 켜면 편집 미리보기가 가능합니다.
- **음원**: GameController의 SaunaAudio에서 Background Music 및 효과음 클립을 교체하세요.

배치와 길찾기는 실행 시작 시 Scene Layout의 위치, 좌석, 장애물을 읽습니다. 장애물은 BoxCollider2D와 SaunaObstacle로 정의합니다. 그림 크기를 바꾼 경우 테이블의 BoxCollider2D 크기도 맞춰 주세요. 현재 테이블 ID는 0~7, 좌석은 각각 4개입니다. 런타임 테이블 데이터는 Scene Layout에 연결된 테이블 수를 읽어 구성합니다. 4×2 변경 전 씬과 밸런스 데이터는 Logs/BeforeFourByTwo에 백업했습니다.

맵 외곽과 간격을 줄이기 전 씬은 `Logs/BeforeCompactMap/Sauna.unity`에 백업했습니다. 새 바닥 이미지는 `Assets/Art/floor_compact.png`이며, 이전 바닥 이미지도 보관되어 있습니다. 배치 변경 메뉴는 명시적으로 실행할 때만 위치를 적용하며, 평상시 빌드는 직접 편집한 위치를 유지합니다.

화면 비율에 대응하는 UI 바깥 컨테이너의 위치·크기는 SaunaUI가 조절합니다. 내부 이미지/폰트/색상/버튼 모양은 저장된 씬과 프리팹 값을 사용합니다. 나눔고딕은 SIL OFL 라이선스로 포함했으며 라이선스는 Assets/Fonts/OFL.txt에 있습니다.

하이어라키 구성 메뉴는 기존 Scene Layout을 다시 만들지 않고, 없는 기능 오브젝트만 보충합니다. 빌드/Play가 사용자의 배치와 프리팹 편집을 초기화하지 않습니다. 전환 전 원본 씬은 Logs/BeforeHierarchy/Sauna.unity, 음식 기능 전 씬/프리팹은 Logs/BeforeFood에 백업했습니다.

## 빌드와 검사

Unity 메뉴 **수상한 찜질방 → Windows 빌드** 또는 PowerShell에서 `./Build.ps1`을 실행하세요. 프로젝트 준비는 기존 퀘스트 데이터와 생성된 씬을 덮어쓰지 않습니다.

- 편집기 검사: 32개 좌석의 입장/퇴장 경로와 충돌, 별점별 확률, 카메라 경계, 화면 비율.
- 실제 실행 검사: Unity 메뉴 **수상한 찜질방 → 테스트용 빌드** 후 `./Test.ps1`. `Builds/QA`에서 주문·퇴장·청소 두 방식·실패·게임오버·재시작·일시정지·300% UI와 세로 화면을 검사합니다. 1~4인 일행 각각에 대해 주문·보상·실패가 테이블당 한 번인지 검증합니다. 화면 캡처를 위해 검사 창이 잠시 표시됩니다.
- 검사 결과와 실행 캡처는 `Logs` 안에 저장됩니다.
- 잡화 검사: 같은 테스트용 빌드에서 `./Test.ps1 -Retail`. 포스기 뒤/앞 판정, 총 3명 대기열, 만석 이탈, 1~5개 중복 주문, 무작위 영수증 3개, 5칸 담기/반납, 상호작용 차단, 수량 오차별 감점, 두 퇴장 연출, 시간 초과, 일시정지와 재시작을 검증합니다. `Logs/RetailQA`에 결과와 캡처를 저장합니다.

`Builds/Windows`는 실제 플레이용, `Builds/QA`는 자동 검사본입니다. **둘 다 Unity Development Build 및 네트워크 디버깅이 꺼진 일반 빌드**입니다. `--sauna-smoke` 또는 `--sauna-retail-smoke` 인수를 직접 준 경우에만 로컬 자동 검사가 실행됩니다. 게임에는 온라인 기능이 없습니다. 프레임 제한은 60 FPS입니다.

2026-11-22 최종 검증: 편집기 검사 **217개**, 음식·대기 게이지·기존 기능 실행 검사 **112개**, 잡화 실행 검사 **40개** 통과. 실제 맵 24×23과 테이블 간격 5×5, 4×2 테이블과 32개 좌석 동선, 이동속도 8.4, 게이지의 51/50/31/30/1% 색상 경계, 주문 대기·음식 배달 시간 초과, 짧은 먼지 말풍선, 테이블당 한 번의 감점을 확인했습니다. 깨끗한 빈 테이블이 없는 경우에도 1~4인 각각 입장·불만 퇴장·한 번의 감점·청소 후 재입장·게임오버·재시작을 검증했습니다. 잡화 대기·응대·진열대·퇴장 기준점의 이동 경로도 검사했습니다. 실행 예외 없이 통과했으며 Windows/QA 빌드 모두 오류 **0**, 경고 **0**입니다. 결과는 `Logs/editor-validation.txt`, `Logs/retail-authoring-validation.txt`, `Logs/FoodQA/runtime-checks.txt`, `Logs/RetailQA/runtime-checks.txt`, `Logs/build-result.txt`에 있습니다. 최신 게임 검사 캡처 19장은 `Logs/FoodQA`, 잡화 화면 8장은 `Logs/RetailQA`에 저장했습니다. 전체 4×2 배치는 `15-four-by-two.png`, 대기 게이지와 퇴장은 `16-waiting-gauges.png`, `17-wait-timeout.png`, `18-delivery-timeout.png`, 깨끗한 테이블 부족으로 인한 퇴장은 `19-no-clean-table.png`입니다. 맵 축소 전 씬은 `Logs/BeforeCompactMap/Sauna.unity`에 보관했습니다.
