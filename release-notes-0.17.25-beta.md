# Homebrew Classes 0.17.25-beta

## 한국어

PC에서 분류 버튼을 직업 선택 화면에 직접 연결했습니다. 코어·베이스·하이브리드·얼터네이트·오컬트·프레스티지 중 하나를 누르면 해당 직업과 아키타입이 표시되고, 같은 분류를 다시 누르면 접힙니다.

스피릿스틸의 결속 무기 피트가 43종에서 50종으로 늘었습니다. 20레벨까지 고르는 횟수는 10회입니다. 6레벨에 은폐 재굴림, 8레벨에 추가 기회공격, 10레벨에 근접 사격 숙련과 시전 방해, 12레벨에 뻗어 찌르기, 14레벨에 무장 해제의 일격, 16레벨에 마법을 가르는 일격을 고를 수 있습니다.

0.17.24-beta에서 분류가 표시되지 않았다는 보고로 연결 방식을 변경했습니다. 본편의 목록 재구성과 접기·다시 펼치기는 오프라인 검사 대상이며, 실제 화면의 배치와 클릭은 플레이 확인 대상입니다.

게임 Mono에서 배포 ZIP의 스피릿스틸 검사 1,076건이 통과했습니다. 전체 회귀 검사에 사용하는 과거 무리 조련사 기준 DLL 두 개는 원래 커밋에서 재빌드했습니다. 보관 당시 파일과 재빌드 파일의 SHA-256이 달라 원본 파일 일치 검사는 미통과입니다.

## English

The six category buttons now attach directly to the PC class selection screen in character creation and level-up. Click a category to show its classes and archetypes. Click it again to collapse it.

Spiritsteel has 50 bound-weapon feats, up from 43, with ten picks by level 20. The new options are Strike in Darkness at 6, Spirit Vigil at 8, Close-Quarters Shot and Disruptive Spirit at 10, Spirit Lunge at 12, Disarming Critical at 14 and Spellcleaving Strike at 16.

This change addresses a report that the controls were missing in 0.17.24-beta. Offline checks cover rebuilding the native list, folding it and reopening another category. In-game layout and mouse input still require a play check.

All 1,076 Spiritsteel checks passed against the packaged DLL under the game’s Mono runtime. Two historical Pack Warden reference DLLs used by the full regression suite were rebuilt from their original commits. Their file hashes differ from the archived hashes, so the historical binary identity checks do not pass.
