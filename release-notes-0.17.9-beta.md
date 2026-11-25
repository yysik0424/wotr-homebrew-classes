# Homebrew Classes 0.17.9-beta

## 한국어

- 팔코너의 16레벨 성장표에서 향상된 회피를 제거했다. 9레벨 회피와 16레벨 세 갈래 사냥은 그대로 얻는다. 회피가 적용되는 공격에서 반사 내성에 실패하면 전체 피해를 받는다. 기존 팔코너도 지역을 불러올 때 옛 16레벨 지급분을 확인하며, 별도 습득 기록이 있으면 보존한다.
- 예언 파괴자의 기본 공격 보너스는 오라클 레벨의 절반(내림)이다. 순수 20레벨은 +10/+5다. 기존 캐릭터의 기본값이 옛 직업 합계와 일치하면 지역 로드 때 보정한다. 다른 모드에서 바꾼 기본값은 보존한다.
- 주문 격투가의 비전 저수지는 3 + 브롤러 레벨의 절반(내림) + 지능 수정치다. 20레벨·지능 28에서 22점이며 일반 주문 슬롯은 하루 42회다.
- 귀검사는 수호령 발현 중 모든 레벨에서 대상 피해의 25%를 흡수한다. 풀 최대치는 전사 레벨 × 매력 수정치(최소 1) × 2다. 20레벨·매력 수정치 +5이면 200점이다. 귀신참과 수호령 전력 공격은 하루 매력 수정치 회(최소 1)를 공유하고 각각 사용당 1회 소모한다. 귀신참의 피해와 투사체 수는 유지한다.

- 카오스 워리어는 본편 무한한 격노의 선행 조건을 읽어 혼돈의 분노를 선택 자격에 추가한다. 분노 재진입 시 변이 재추첨과 전투당 한 번의 광란의 시전을 적용한다.

## English

- Falconers gain Threefold Hunt in place of Improved Evasion at ranger level 16. They keep Evasion at level 9 and take full damage on a failed Reflex save when Evasion applies. On area load, existing Falconers lose the old level-16 grant unless there is evidence of a separate source.
- Prophecy Breakers gain half base attack bonus per oracle level, rounded down, reaching +10/+5 at level 20. Loading an area updates an old class total and preserves a custom base value.
- The Spellfist's Arcane Reservoir holds 3 + half brawler level (rounded down) + Intelligence modifier. At level 20 with Intelligence 28, that is 22 points alongside 42 daily spell slots.
- A manifested Guardian Phantom absorbs 25% at every level. Its pool holds twice fighter level times Charisma modifier (minimum modifier 1), or 200 points at level 20 with a +5 modifier. Ghost Slash and Guardian Phantom Full Assault share daily uses equal to the Charisma modifier (minimum 1). Either technique spends one use. Ghost Slash keeps its damage and projectile count.
- Fixed Chaos Warrior registration against the native Limitless Rage prerequisite. Re-entering rage rerolls mutations, and Frenzied Casting remains available once per combat.
