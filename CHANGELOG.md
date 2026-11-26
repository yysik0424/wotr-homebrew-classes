# Changelog

## 0.17.33-beta

턴제에서 일반 공격으로 적을 가리키면 캐릭터 시트의 첫 타격 명중 보너스, 확인된 대상 방어도(AC)에 대한 기본 명중률, 피해 감소 전 무기 피해 범위가 표시됩니다. 무기로 공격하는 기술과 접촉 공격에도 해당 판정을 표시합니다. 기본 명중률은 은폐와 대상별 추가 효과를 적용하기 전의 추정치입니다.

주문은 읽을 수 있는 피해 주사위와 1회 기본 범위를 표시합니다. 예를 들어 매직 미사일은 발당 1d4+1(2~5), 기본 시전자 레벨 10의 파이어볼은 10d6(10~60)이며 반사 내성 성공 시 절반입니다. 판정이 끝나면 아군과 적의 실제 d20, 명중 보너스, 방어도 및 적용된 피해가 잠깐 표시됩니다. 전투 로그 위에서 직전 결과와 다음 행동의 예상치를 함께 볼 수 있습니다.

In turn-based combat, hover an enemy for the character sheet’s first weapon attack bonus, its base hit chance against revealed AC, and its damage range before mitigation. Weapon attacks from abilities and touch attacks also show their rolls. Base chances are estimates before concealment and target-specific effects.

Spells show readable damage dice and their base range per damage instance. Magic Missile deals 1d4+1 (2–5) per missile. At base caster level 10, Fireball shows 10d6 (10–60), with a Reflex save for half. After an attack, the panel briefly shows the actual d20, bonus, AC and damage dealt for both party and enemy attacks. The latest result can appear alongside the next action’s preview.

## 0.17.32-beta

턴제 판정 표시가 우측 전투 로그 바로 위에서 로그 창과 함께 움직입니다. 표시 폭은 로그 창을 따르고, 글자는 게임 글자 크기 설정을 적용한 전투 로그의 80% 크기입니다. 긴 문구는 줄을 바꿔 모두 표시합니다. 로그를 화면 상단까지 키우면 판정 표시가 로그 상단에 일부 겹쳐 화면 안에 머뭅니다.

Turn-based attack odds, defenses and incoming results now appear directly above the combat log and move with it. The panel matches the log width, wraps longer text and uses 80% of the scaled log font size. If there is too little room above the log, the panel overlaps its top edge to stay on screen.

## 0.17.31-beta

- 지옥의 기수는 1·9·17레벨의 전술가 팀워크 재주와 전술가의 재주 공유 능력을 잃습니다. 기사단, 도전, 6·12·18레벨의 보너스 전투 재주는 유지합니다.
- 팔코너는 첫 선호 지형과 이후 강화, 12레벨 위장을 모두 잃습니다. 선호 적과 사냥감은 유지합니다.
- 키메라 몽크는 2·6·10레벨의 몽크 보너스 특기를 되찾아 총 여섯 번 고릅니다. 5·9·13·17레벨의 타격 자세 선택 네 번은 사라집니다.
- 블라스트 메이거스는 13레벨 비전 중장 갑옷 숙련을 잃고 7레벨 비전 평장 갑옷 숙련을 유지합니다.
- 스피릿스틸은 3·6·9·12·15·18레벨 위험 감지를 모두 잃습니다. 2레벨에는 팀워크 재주, 6레벨에는 신령의 넘어뜨리기를 얻습니다.
- 배포 DLL의 파일 시각을 보존해 Unity Mod Manager가 업데이트 후 옛 로더 캐시를 다시 고르던 문제를 수정했습니다. 주문서 연동은 블루프린트 목록을 읽는 도중 목록이 바뀌면 메인 메뉴에서 다시 확인합니다.
- 직업 분류와 버프 재시전 비교는 새 로더로 재시작한 게임 화면에서 확인이 남아 있습니다.

- Hell Rider loses the tactician teamwork feats at levels 1, 9 and 17 and the ability to share them through tactician. Order, challenge and bonus combat feats at levels 6, 12 and 18 remain.
- Falconer loses the first favored terrain, all later terrain selections and increases, and level 12 Camouflage. Favored enemy and quarry remain.
- Chimera Monk regains monk bonus feats at levels 2, 6 and 10, for six selections in total. It gives up the four Style Strike selections at levels 5, 9, 13 and 17.
- Blast Magus gives up level 13 heavy armor proficiency and armored casting in heavy armor. Its level 7 medium armor feature remains.
- Spiritsteel loses all six ranks of Danger Sense at levels 3, 6, 9, 12, 15 and 18. Its level 2 teamwork feat and level 6 Spirit Trip remain.
- Release archives preserve DLL timestamps so Unity Mod Manager can distinguish the updated loader from an old cached copy. Spellbook integration retries discovery at the main menu if the blueprint collection changes during its initial scan.
- The class list controls and buff recast comparison still need confirmation on screen after a fresh game launch with the new loader.

## 0.17.30-beta

- 이미 걸린 버프의 상태 아이콘에서는 본편 타이머가 남은 시간을 표시합니다. 턴제에서 버프를 다시 시전할 때는 대상 미리보기에서 새 효과의 기본 지속시간과 기존 효과의 남은 시간을 함께 확인할 수 있습니다.
- Active buff icons use the game’s own timer. When you select a buff in turn-based combat, the target preview shows its base duration alongside the time remaining on the current effect.

## 0.17.29-beta

- 턴제에서 버프를 선택하고 아군을 가리키면 효과별 기본 지속시간과 이미 걸린 효과의 남은 시간을 함께 볼 수 있습니다. 자신에게 즉시 사용하는 버프는 기술 아이콘에서도 남은 시간을 확인할 수 있습니다. 예를 들어 시전자 레벨 7의 레벨당 1분 버프는 기본 7분이며, 주문 연장이 적용되면 기본 14분입니다. 주사위로 정하는 시간은 범위로, 실제 시전 때 결정되는 효과는 공식이나 기술 설명으로 표시합니다. 광역기는 가리킨 대상이 범위에 들 경우의 정보를 보여줍니다. 회복·해제·소환·토글도 같은 턴제 툴팁에서 확인할 수 있습니다.
- In turn-based combat, select a buff and point at an ally to see the base duration of each effect and the time remaining on a matching active buff. Instant self buffs show the remaining time on the ability icon tooltip. At caster level 7, a one-minute-per-level buff shows a base of 7 minutes, or 14 with Extend Spell when eligible. Random durations show a range. Effects resolved during casting show the available formula or ability description. For area abilities, the preview describes the hovered unit if affected. Healing, dispels, summons and toggles also have turn-based summaries.

## 0.17.28-beta

- PC 캐릭터 생성과 레벨업의 직업 목록이 자동으로 재배치될 때 분류 버튼과 첫 직업 행이 겹치는 문제를 수정했습니다.
- Fixed the overlap between the category buttons and the first class row when the PC character creation or level-up screen rebuilds its list.

## 0.17.27-beta

- 턴제 전투에서 기술을 가리키면 접촉 공격, 내성, 주문 저항 등 확인된 판정을 볼 수 있습니다. 기술을 선택하고 적을 가리키면 조사로 밝혀진 방어 수치가 표시됩니다. 접촉 공격의 기본 명중 추정치는 거리 보너스와 무방비 방어의 조건별 차이를 보여줍니다. 적이 아군을 공격하면 공격자, 대상과 실제 판정 결과가 잠시 남습니다.
- Bubble Buffs 5.2.3에서 무리 강화, 골렘 폭주와 스피릿 웨폰 빙의를 묶음에 넣을 수 있습니다. 무리 강화는 살아 있는 무리 구성원 각각의 효과를 확인합니다. 골렘 폭주와 빙의는 단기 버프 필터에서 볼 수 있습니다.
- In turn-based combat, hover an ability to see its attack, save and spell resistance checks. Select it and point at an enemy to see revealed defenses. Touch attacks show a base hit estimate and the effects of distance bonuses and flat-footed armor class where applicable. Incoming attacks briefly show the attacker, party target and resolved checks.
- Bubble Buffs 5.2.3 recognizes Pack Empowerment, Golem Overdrive and spirit possession. Pack Empowerment checks the living pack members individually. Overdrive and possession use the short-buff filter.

## 0.17.26-beta

- 스피릿스틸의 1레벨 주적 선택과 결속 무기의 주적 명중·피해 보너스를 제거했습니다. 1레벨에는 결속 무기를 정하고 신령의 분노를 얻습니다.
- Removed Spiritsteel’s favored enemy choice and its bound weapon attack and damage bonuses against the chosen enemy. At level 1, choose your bound weapon and gain Spirit Rage.

## 0.17.25-beta

- PC에서 분류 버튼을 직업 선택 화면에 직접 연결했습니다. 코어·베이스·하이브리드·얼터네이트·오컬트·프레스티지 중 하나를 누르면 해당 직업과 아키타입이 표시되고, 같은 분류를 다시 누르면 접힙니다.
- 스피릿스틸의 결속 무기 피트가 43종에서 50종으로 늘었습니다. 20레벨까지 고르는 횟수는 10회입니다. 6레벨에 은폐 재굴림, 8레벨에 추가 기회공격, 10레벨에 근접 사격 숙련과 시전 방해, 12레벨에 뻗어 찌르기, 14레벨에 무장 해제의 일격, 16레벨에 마법을 가르는 일격을 고를 수 있습니다.
- The six category buttons now attach directly to the PC class selection screen in character creation and level-up. Click a category to show its classes and archetypes. Click it again to collapse it.
- Spiritsteel has 50 bound-weapon feats, up from 43, with ten picks by level 20. The new options are Strike in Darkness at 6, Spirit Vigil at 8, Close-Quarters Shot and Disruptive Spirit at 10, Spirit Lunge at 12, Disarming Critical at 14 and Spellcleaving Strike at 16.

## 0.17.24-beta

- 캐릭터 생성·레벨업의 직업 목록을 여섯 분류로 나눕니다. 마우스로 분류를 접고 펼칠 수 있습니다.
- 스피릿스틸은 1레벨에 신령에게 맹세해 주적 하나를 고릅니다. 결속 무기의 명중과 피해 보너스는 +2에서 +10까지 성장합니다. 종족별 사냥 무기 피트 7종을 제거했습니다.
- The class picker groups classes into six categories. Mouse users can expand and collapse them.
- Spiritsteel swears an oath against one favored enemy at level 1. Its bound weapon gains +2 to +10 on attack and damage rolls against that enemy. Seven species-specific weapon feats have been removed.

## 0.17.23-beta

- 스피릿스틸은 신령의 분노 중 12레벨부터 기절 면역, 16레벨부터 혼란 면역을 자동으로 얻습니다. 16레벨 무기 피트로 신속과 천둥 치명타를 추가했습니다.
- Spiritsteel automatically gains stun immunity during Spirit Rage at level 12 and confusion immunity at level 16. Speed and Thundering Critical take their places as level-16 weapon feats.

## 0.17.22-beta

- 스피릿스틸의 16레벨 무기 피트를 10종 추가했습니다. 전체 50종 중 20레벨까지 10개를 익힙니다.
- Spiritsteel gains ten level-16 bound-weapon feats. Choose ten of 50 by level 20.

## 0.17.21-beta

- 스피릿스틸의 주문 방벽·방벽 관통·속박 해방을 16레벨에 해금한다.
- Spiritsteel unlocks Spell Ward, Pierce Defenses, and Unfettered Spirit at level 16.

## 0.17.20-beta

- 스피릿스틸의 심판의 일격 해금 레벨을 18에서 12로 낮췄다.
- Spiritsteel unlocks Judgment Strike at level 12 (previously 18).

## 0.17.19-beta

- 스피릿스틸 결속 무기에 고레벨 피트 10종을 추가해 총 40종이 되었습니다.
- Spiritsteel gains ten high-level bound-weapon feats, bringing the pool to 40.

## 0.17.18-beta

- Spiritsteel uses Wisdom for daily barbarian rage rounds.
- 스피릿스틸의 일일 분노 라운드를 지혜 수정치로 계산한다.

## 0.17.17-beta

- Spiritsteel now chooses from 30 bound-weapon feats. The ten additions cover materials, scouting, charges, attacks of opportunity, and four creature types. Pick one every even level for ten total by level 20.
- 스피릿스틸의 결속 무기 피트를 20종에서 30종으로 늘렸다. 재질·정찰·돌격·기회공격·사냥꾼 각인을 추가했다. 짝수 레벨마다 하나씩 골라 20레벨까지 총 10개를 배운다.

## 0.17.16-beta

- A Shadow Sorcerer pays one extra slot for an echo. With only one slot left, the original spell still fires on its own.
- 그림자 소서러의 메아리를 발사할 때 같은 단계 슬롯을 1칸 더 쓴다. 한 칸만 남으면 원본 주문만 시전한다.

## 0.17.15-beta

- Oathbound Revenant tracks death transfers per mark and replaces the same caster’s lesser fear aura on upgrade. Pack Warden uses actual melee reach to judge arrival. Loading a Warlock with an ambiguous first patron preserves the pact and logs a warning.
- 맹세의 망자는 각 낙인의 사망 이전을 개별로 처리하고 공포 오라를 강화하면 같은 시전자의 하위 효과를 제거한다. 무리 조련사는 실제 사거리로 도착을 판단하고, 기존 워락의 후원자가 모호하면 불러올 때 경고와 함께 계약을 보존한다.

## 0.17.14-beta

- 그림자 소서러의 매직 미사일은 그림자를 켜도 1레벨 슬롯 1칸을 쓴다. 메아리는 추가 슬롯을 쓰지 않는다.
- Magic Missile costs one 1st-level slot even with the shadow on. Its echo costs no additional slot.

## 0.17.13-beta

- 블라스트 메이거스는 방어 효과로 무효화된 치명타를 폭발에 적용하지 않는다. 키네티시스트와의 번 충돌 시 블라스트 비활성화 사유를 기록한다.
- 워락은 해제 연출이 없는 소환물을 본편의 소멸 시간에 맞추어 해제한다. 복수 후원자를 발견하면 캐릭터와 판정 시점을 기록한다.
- 스피릿 웨폰 샤먼은 직업 등록 후에 추가된 다른 모드의 에이돌론 특성도 보유 여부를 판정한다.

## 0.17.12-beta

- 예언 파괴자는 연구서가 열린 단계마다 신성 주문 2회를 비전에 배정한다. 비전은 지능과 관계없이 단계당 하루 2회이며, 내성 난이도에는 지능을 적용한다.
- Prophecy Breaker reserves two divine casts for arcane research at each unlocked research tier. Research grants two casts per tier at any Intelligence score and still uses Intelligence for save DCs.

## 0.17.11-beta

- 스피릿스틸의 신령의 분노가 바바리안의 일일 분노 라운드를 공유한다. 결속 무기를 들면 기본 분노가 지속되는 만큼 신령도 유지할 수 있고, 미씩 끝없는 격노도 두 효과에 적용된다.
- Spiritsteel’s Spirit Rage now shares the barbarian’s daily rage rounds. With the bound weapon equipped, the spirit can stay active for the full rage, including with Limitless Rage.

## 0.17.10-beta

- 골렘술사의 폭주 중 합체 형태가 바뀌면 버프 알림이 끝난 뒤 추가 사지와 학파 효과를 맞춘다.
- 맹세의 망자의 반격은 같은 게임 시각에 발생한 연속 공격도 피격 굴림별로 한 번씩 판정한다.
- 귀검사 수호령 하체의 파티클 재질은 해당 수호령에만 적용되는 색상으로 흐리게 표시한다.
- During overdrive, Golemancer reconciles extra limbs and school effects after fusion form notifications finish.
- Vengeful Riposte tracks each incoming attack roll separately, including hits in the same game tick.
- The Phantom Blade can fade the Guardian Phantom’s lower particle planes with a tint applied to that spirit alone.

## 0.17.9-beta

- 팔코너는 9레벨 회피를 유지하고 16레벨에 세 갈래 사냥을 얻는다. 향상된 회피는 성장표에서 제외했다.
- 예언 파괴자의 BAB는 1/2, 주문 격투가의 저수지는 3 + 브롤러 레벨 절반(내림) + 지능 수정치다. 귀검사의 피해 흡수는 25%, 풀 배율은 2다. 귀신참과 수호령 전력 공격은 매력 수정치(최소 1)만큼의 하루 사용 횟수를 공유한다.
- 카오스 워리어가 본편 무한한 격노의 선행 조건을 읽을 때 직업 등록이 중단되던 오류를 수정했다.
- Falconers retain Evasion at level 9 and gain Threefold Hunt in place of Improved Evasion at level 16.
- Prophecy Breakers have half BAB. Spellfists have a reservoir of 3 + half brawler level (rounded down) + Intelligence modifier. Phantom Blades absorb 25% and use a pool multiplier of two. Ghost Slash and Guardian Phantom Full Assault share one daily counter equal to the Charisma modifier (minimum one).
- Fixed Chaos Warrior registration when reading the native Limitless Rage prerequisite.

## 0.17.8-beta

- Pack Wardens fight with their main companion at levels 1–6. Mounted Call brings one auxiliary at levels 7–12 and two from level 13 while riding that companion in combat.

## 0.17.7-beta

- Pack Warden now calls up to two auxiliaries. Each still follows every eligible main-companion attack.

- Shadow Sorcerers toggle the echo and pay one additional slot per paid cast while active. Daily slots use the standard sorcerer table.
- Research spell DC now follows normal Intelligence-based spellcasting.
- Sorrow Inquisitors keep stored afflictions until their next rest. Sentencing uses the duration recorded at capture.
- Spellfists prepare Magus spells from three chosen schools and cast flexibly. The sixth spell tier opens at level 16. Daily slots and mythic merge eligibility follow the Magus.
- From magus level 20, Blast Strike deals normal blast damage on a weapon critical.
- Chaos Warriors can quicken one eligible spell per combat with Frenzied Casting. Limitless Rage still permits charge-free rage activations and mutation rerolls.
- Blood Berserkers gain their bloodrager level in temporary hit points for two rounds when they collect a knot. Collecting another knot replaces and refreshes the protection.

## 0.17.6-beta

- Added main spellbook support for mythic merging and prestige advancement across the bundle, including Spellbook Merge 1.7.1 and replacement spellbook selections.
- Added Blast Pool feat eligibility and Limitless Rage support for Chaos Rage.
- Removed Studied Target, Swift Studied Target and Pack’s Quarry from Pack Warden.

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
