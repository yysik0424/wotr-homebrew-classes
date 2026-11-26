# Homebrew Classes 0.17.28-beta

## 한국어

PC 캐릭터 생성과 레벨업의 직업 선택창에서 코어·베이스·하이브리드·얼터네이트·오컬트·프레스티지 버튼을 위한 공간을 목록 상단에 확보했습니다. 분류를 누르면 해당 직업과 아키타입을 볼 수 있고, 열린 분류를 다시 누르면 접힙니다. 화면을 다시 열 때도 버튼 두 줄의 공간을 한 번만 예약합니다.

## English

The PC class picker now reserves space above the list for the Core, Base, Hybrid, Alternate, Occult and Prestige buttons in both character creation and level-up. Click a category to show its classes and archetypes. Click it again to collapse it. Reopening the screen reserves the button space once.

## 검증 / Verification

게임을 실행하지 않고 두 화면의 프리팹 치수와 게임의 세로 목록 배치 코드를 사용해 검증했습니다. 접기·펼치기와 재배치 검사 23/23건에서 버튼과 목록이 겹치지 않았습니다. 패키지 검사는 81/81건, 턴제 표시·Bubble Buffs 5.2.3 호환 검사는 20/20건 통과했습니다. 실제 화면 렌더링과 마우스 입력은 게임 내 확인이 남아 있습니다.

Offline checks used both shipped screen prefabs and the game's vertical layout code. All 23 class picker checks passed, including collapse, reopen and placement after rebuilding the list. Package checks passed 81/81 and turn-based preview and Bubble Buffs 5.2.3 checks passed 20/20. The on-screen rendering and physical mouse input still require in-game confirmation.
