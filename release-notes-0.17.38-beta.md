# Homebrew Classes 0.17.38-beta

그레이 시어는 1레벨에 지팡이와 함께 들 한손 근접 무기 종류를 고르고 숙련을 얻습니다. 선택한 종류의 무기는 주손과 보조손 어느 쪽에서든 수동·자동·안전 검격에 쓰입니다. 같은 종류의 무기로 교체해도 적용됩니다. 이전에 장검 숙련을 받은 그레이 시어는 재훈련하면 다른 무기를 고를 수 있습니다.

Grey Seers choose a light or one-handed melee weapon type at level 1 and gain proficiency with it. Pair any weapon of that type with a quarterstaff in either hand for the accuracy bonus and manual, automatic or guarded follow-ups. Existing Grey Seers can continue using their longsword or respec to choose another weapon.

## 검증 / Verification

2026-11-27 공용 진단과 무리 조련사의 과거 참조 DLL을 독립된 경로에서 재현할 수 있게 수정했습니다. 21종과 통합 로더의 오프라인 회귀 검사 44,469건이 모두 통과했습니다. 이 중 그레이 시어는 1,065건, 무리 조련사는 4,204건입니다. 설치된 그레이 시어 DLL의 무기 조합 검사 17건과 패키지 검사 83건도 통과했습니다. 새 무기 선택의 캐릭터 생성 화면과 실제 전투는 이번 점검에서 실행하지 않았습니다. 이미 0.17.38-beta를 설치했다면 다시 설치할 필요가 없습니다.

On 27 November 2026, we rebuilt both historical test references reproducibly. All 44,469 offline regression checks passed across the 21 archetypes and the loader, including 1,065 for Grey Seer and 4,204 for Pack Warden. The installed Grey Seer DLL passed all 17 weapon-pair checks in the game's Mono runtime, and the package passed all 83 checks. Character creation and live combat with the new weapon choice still await an in-game check. Players who already have 0.17.38-beta installed can keep their current copy.
