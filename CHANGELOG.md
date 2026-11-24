# Changelog

## 0.17.5-beta

- Falconer finds the hawk's bite in its native primary-hand slot and both talons in its additional limbs.
- Golemancer finishes fusion cleanup after the game's buff removal notification returns.
- Shadow Sorcerer checks the opposite shoulder when terrain blocks the first position for a single echo.

## 0.17.4-beta

- Refreshes the master's Overdrive limbs and effects after golem fusion ends.
- Projects shadow movement onto the navigation mesh before tracing the route from the caster.

## 0.17.3-beta

- Registers the wood golem summer aura in the Golemancer blueprint inventory so the class can finish initializing.

## 0.17.2-beta

- Gives the wood golem summer aura a valid area shape and refreshes Overdrive after fusion changes the master's natural weapons.
- Targets the drawn weapon mesh for the Blood Berserker's translucent red blade effect.

## 0.17.1-beta

- Handles unequipped bonded shields, the last Chaos Warrior rage use, and natural attack animations for character models with different rigs.
- Repairs Spiritsteel registration and initializes visual-free area effects for the stone golem and Oathbound Revenant.

## 0.17.0-beta

- Adds the Prophecy Breaker oracle with separate divine and arcane spellbooks, capped research and cancellable inscription replacement.

## 0.16.0-beta

- Adds the Hell Rider cavalier with a nightmare mount, devil charges, an infernal standard and four spell-like abilities.

## 0.15.0-beta

- Adds the Falconer ranger as the nineteenth archetype, with an independent hawk and spell sharing from its animal companion.

## 0.14.1-beta

- Restores Spiritsteel's second rage burst and five elemental weapon visuals.
- Dream Thief throws a returning starknife model. Deathdealer plays a weapon swing for attack and spellcasting reactions.
- Repairs Phantom Blade spirit registration and view replacement, Shadow Sorcerer echo stances and a missing Crystal Mind target effect.

## 0.14.0-beta

- Adds the Deathdealer slayer archetype with studied reactions and nine personal arcane abilities.

## 0.13.0-beta

- Spiritsteel barbarians gain an ancestral weapon, ten weapon feat choices and a spirit that assists with teamwork feats during rage.
- Dream Thief rogues devoted to Desna gain a thrown starknife, starlight, Dreamwalk and a choice of dream talents.

## 0.12.0-beta

Added Sorrow Inquisitor, its rosary, bane-weapon sentences and level 16 divine damage.

## 0.11.1-beta

Added a translucent red blade effect to the Blood Berserker’s bloodrage.

## 0.11.0-beta

Added the Blood Berserker bloodrager archetype, blood chains and blood-coated weapons.

## 0.10.0-beta

- Added Grey Seer, a sword-and-staff arcanist with guarded casting.
- 아카니스트 아키타입 그레이 시어가 장검과 지팡이, 안전 검격을 얻습니다.

## 0.9.6-beta

- Combat records now follow the lifetime of their attacks, spells, units and active echo stages. This reduces memory retained across repeated fights and reloads.
- 반복 전투와 불러오기에서 남던 공격·시전·유닛 기록을 정리합니다. 그림자 메아리의 연출 기록은 해당 무대가 닫힐 때 해제합니다.

## 0.9.5-beta

- Chaos Warrior no longer reads the party list while a save is loading, removing the engine's "Unit is null" load warnings.
- 카오스 워리어는 세이브를 불러오는 동안 파티 명단을 읽지 않아 엔진의 「Unit is null」 경고가 나지 않습니다.

- Shadow Sorcerer applies a delayed combat stance before the cast animation, releases finished echo stages from memory, and clears its echo records even if a reload log line fails.
- 그림자 소서러는 늦어진 전투 자세를 시전 동작 전에 적용하고, 끝난 메아리 연출을 메모리에서 해제하며, 재로드 로그가 실패해도 메아리 기록을 비웁니다.

## 0.9.4-beta

- Golemancer can call their bonded golem as a standard action, even in combat, and dismiss it outside combat. The same animal companion keeps its hit points, level and material.
- 골렘술사는 필요할 때 표준 행동으로 골렘을 호출하고 전투 밖에서 귀환시킬 수 있습니다. 골렘은 동물 동료 자리를 유지하며 호출 전의 체력·레벨·재질을 보존합니다.

- Shadow Sorcerer gains one shadow and one echo at all levels. The echo uses the base spell without metamagic and costs no additional action or slot. Each unlocked spell level from 1st to 9th loses two base slots, down to zero, before all bonus slots are added.
- 그림자 소서러는 전 레벨에서 그림자 하나와 메아리 한 번을 얻습니다. 메아리는 추가 행동과 슬롯 없이 메타매직 없는 기본 주문을 시전합니다. 개방된 1~9단계의 하루 기본 슬롯에서 단계마다 2칸(최소 0)을 빼고 보너스 슬롯은 모두 더합니다.

- Pack Warden reduces allocations when identifying its animal companion during attacks and uses the weapon slot supplied by the combat rule directly. Weapon-Spirit Shaman filters unrelated equipment, weapon-set and level-up events before inspecting its spirit weapon's facts.
- 무리 조련사는 공격 시 주 동물을 찾는 과정의 임시 할당을 줄이고 전투 규칙이 제공한 무기 슬롯을 바로 사용합니다. 스피릿 웨폰 샤먼은 장비 교체·무기 세트 변경·레벨업이 영병과 관련 있는지 먼저 확인해 불필요한 특성 검사를 줄입니다.

## 0.9.3-beta

- Golemancer now checks fusion controls, buffs and features directly while tracking a character with fusion. Shadow Sorcerer reuses the sorcerer class reference and the owner list, and checks unchanged shadow buffs in a single pass.
- 골렘술사는 융합 보유자의 토글·버프·특성을 임시 목록 없이 확인합니다. 그림자 소서러는 직업 참조와 감시 명단을 재사용하고 분신 버프의 순서와 구성이 그대로면 한 번만 대조합니다.

## 0.9.2-beta

- Golemancer and Chaos Warrior reduce repeated checks on party members. Oathbound Revenant releases references to former combatants. Loading a new-format save with an unfinished Warlock automatic summon restores its spent heat.
- 골렘술사와 카오스 워리어가 파티원을 반복 검사하는 비용을 줄였습니다. 맹세의 망자는 전투가 끝난 유닛의 메모리 해제를 막지 않습니다. 워락의 자동 소환이 완료되기 전에 저장했다면 새 버전에서 그 저장을 열 때 소모한 과열을 돌려받습니다.
- The bundle settings and installation guides explain how to load standalone saves with ModMenu. 통합본 설정과 설치 안내에 ModMenu를 사용할 때 기존 단독판 세이브를 여는 방법을 추가했습니다.
- If Settings.xml contains a null settings object, the bundle now reports the problem before installing its hooks. Settings.xml의 설정 객체가 비어 있으면 공통 패치를 등록하기 전에 오류를 알립니다.

## 0.9.1-beta

- The bundle shares text lookup and language-change hooks across enabled classes. Ten classes also share the standard blueprint initialization hook. Spellfist, Chimera Monk and Blast Magus retain their ordering relative to their respective companion mods.
- 활성 직업이 번역 조회와 언어 변경 패치를 공유합니다. 10개 직업은 기본 블루프린트 초기화 패치도 공유합니다. 주문 격투가, 키메라 몽크, 블라스트 메이거스는 연동 모드에 필요한 실행 순서를 유지합니다.

## 0.9.0-beta

- Bundles twelve class archetypes for Unity Mod Manager. Individual classes can be enabled or disabled for the next game start.
- Includes English and Korean descriptions. Spellfist uses BrawlerClassWrath, Chimera Monk uses DarkCodex, and Golemancer requires the DLC 3 brass golem data.
- Reduces temporary allocations when Shadow Sorcerer checks spell events and shadow disbelief.

## 변경 기록

- 직업 12종을 Unity Mod Manager 모드 하나로 묶었습니다. 다음 게임 시작 때 적용할 직업을 개별적으로 선택할 수 있습니다.
- 영문과 한글 설명을 제공합니다. 주문 격투가는 BrawlerClassWrath, 키메라 몽크는 DarkCodex, 골렘술사는 DLC 3 황동 골렘 데이터가 필요합니다.
- 그림자 소서러의 주문 이벤트와 불신 판정에서 임시 메모리 할당을 줄였습니다.
