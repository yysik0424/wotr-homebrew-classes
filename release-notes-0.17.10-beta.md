# Homebrew Classes 0.17.10-beta

## 한국어

- 골렘술사가 폭주 중 합체하거나 합체를 풀 때, 형태 버프의 추가·제거 알림을 처리하는 도중에 다른 버프를 제거하던 경로를 수정했다. 형태 변경이 끝나면 본체와 골렘의 추가 사지 및 학파 효과를 맞춘다.
- 맹세의 망자가 같은 시각의 별도 피격을 하나로 취급하던 반격 판정을 수정했다. 같은 굴림의 중복 반격과 반격 처리 중 자기 재진입을 막고, 반격이 해결되면 일일 자원 1회를 소모한다.
- 귀검사 수호령의 하체 연기에 쓰이는 파티클 재질이 색조를 지원하면 해당 수호령의 재질 사본에만 흐림을 적용한다. 수호령이 사라지면 사본을 정리한다.
- 직업 진단에서 반격 사거리, 그림자 주문의 2슬롯 비용과 메아리 도착, 동물의 자동 명중, 폭발 준비와 공격 시점, 치명타 확인, 검사 대상의 구름 내성을 개선했다. 진단 코드는 일반 배포 DLL에서 제외한다.

**검증.** 오프라인 전체 검사 44,458건, 배포 ZIP의 기승·소집 검사 672건과 주문서 연동 검사 42건, 21직업의 한영 문자열 1,466쌍을 통과했다. 이 버전의 게임 내 재검은 미실행이다.

## English

- Golemancer no longer reconciles other buffs inside the notification for adding or removing a fusion form during overdrive. The master and golem receive the appropriate limbs and school effects after the form change finishes.
- Vengeful Riposte now tracks individual incoming attack rolls. Separate hits in the same tick can each trigger it. The same roll cannot trigger it twice, and a resolved counterattack spends one daily use.
- The Guardian Phantom fades its lower particle planes using a private material copy when the shader supports tint. Its copies are released when the phantom disappears.
- Developer checks now account for riposte range, two-slot shadow casts, echo impact, automatic pet hits, blast timing, critical confirmation, and the cloud control target’s saving throw. The diagnostic runner is excluded from release DLLs.

**Verification.** All 44,458 offline checks passed, along with 672 mounted companion and 42 spellbook checks on the packaged DLLs and 1,466 bilingual string pairs across all 21 classes. This version has not been retested in-game.
