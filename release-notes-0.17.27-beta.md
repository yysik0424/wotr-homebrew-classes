# Homebrew Classes 0.17.27-beta

## 한국어

턴제 전투에서 기술을 가리키면 접촉 공격, 내성, 주문 저항 등 확인된 판정을 볼 수 있습니다. 기술을 선택하고 적을 가리키면 조사로 밝혀진 방어 수치가 표시됩니다. 접촉 공격의 기본 명중 추정치는 거리 보너스와 무방비 방어의 조건별 차이를 보여줍니다. 적이 아군을 공격하면 공격자, 대상과 실제 판정 결과가 잠시 남습니다.

예를 들어 일반적인 접촉 공격에서 명중 +12와 접촉 AC 20은 65%입니다. 같은 공격의 무방비 접촉 AC가 18이면 해당 조건에서는 75%입니다. 기본값 옆의 범위는 화면에 나온 조건을 바꾼 결과입니다. 은폐, 재굴림, 명중 순간에 실행되는 모드 보정과 최종 피해는 별도의 판정입니다. 숫자가 공개되지 않은 적에게는 관련 판정과 ‘대상 방어 미확인’만 표시합니다. 별도의 툴팁 열기나 확인 버튼은 필요하지 않습니다.

Bubble Buffs 5.2.3에서 무리 강화, 골렘 폭주와 스피릿 웨폰 빙의를 묶음에 넣을 수 있습니다. 무리 강화는 살아 있는 무리 구성원 각각의 효과를 확인합니다. 골렘 폭주와 빙의는 단기 버프 필터에서 볼 수 있습니다. 귀검사 수호령 전력 공격과 블라스트 메이거스의 폭발 일격은 수동으로 사용합니다. 일반 주문은 Bubble Buffs가 읽는 준비·습득 주문과 실제 주문서별 공급원을 사용합니다.

## English

In turn-based combat, hover an ability to see its attack, save and spell resistance checks. Select it and point at an enemy to see revealed defenses. Touch attacks show a base hit estimate and the effects of distance bonuses and flat-footed armor class where applicable. Incoming attacks briefly show the attacker, party target and resolved checks.

For example, +12 against touch AC 20 yields 65% on an ordinary d20 attack. Against flat-footed touch AC 18 the same attack yields 75%. The range compares the displayed conditions. Concealment, rerolls, modifiers supplied at execution by another mod and damage resolution are separate. Unidentified enemy defenses are labelled unknown. Use the normal targeting cursor to see the panel.

Bubble Buffs 5.2.3 recognizes Pack Empowerment, Golem Overdrive and spirit possession. Pack Empowerment checks the living pack members individually. Overdrive and possession use the short-buff filter. Spirit Full Attack and Blast Strike remain manual combat commands. Standard buffs use Bubble Buffs’ ordinary prepared or known spell discovery and each spellbook’s own provider.

## 21개 아키타입 검토 / Archetype coverage

아래 표는 주문서, 능력과 발동 방식을 코드에서 확인한 범위입니다. 모드가 로드한 블루프린트도 공통 판정 표시의 대상입니다. 개별 아키타입의 턴제 전투를 게임 화면에서 재현하는 검증은 별도입니다.

The table records the source-level mechanics reviewed. Target previews read live blueprints, including abilities supplied by installed mods. Visual checks in a loaded combat encounter are pending.

| Archetype | 확인한 경로 | Mechanic reviewed |
|---|---|---|
| BlastMagus | 본편 주문서. 폭발 일격은 전투용 무기 충전 | Native spellbook. Blast Strike is a combat action. |
| BloodBerserker | 혈분노, 근접 타격과 사슬 반응 | Bloodrage, melee hits and tether reactions. |
| BulwarkMage | 위저드 주문서와 결속 방패의 장비 효과 | Wizard spellbook and bonded shield equipment effects. |
| ChaosWarrior | 주문서와 분노 진입 시 변이 | Spellbook and mutations on entering rage. |
| ChimeraMonk | 변이원은 본편 버프 적용 액션을 사용 | Mutagen uses a native buff action. |
| Deathdealer | 모드 선택과 공격 시 반응 | Mode selections and attack reactions. |
| DreamThief | 꿈 재능, 공격 시 처리되는 효과 | Dream talents and on-attack effects. |
| Golemancer | 폭주 변형은 짧은 버프. 선택한 준비 추출물을 소비 | Overdrive variants are short buffs and spend the chosen prepared extract. |
| GreySeer | 주문과 대상 지정 | Spells and target designation. |
| Falconer | 매 명령과 전투 중 타격 | Hawk commands and combat strikes. |
| HellRider | 기승, 깃발과 위치 지정 행동 | Mounted combat and banner placement. |
| ProphecyBreaker | 주 주문서와 비전 연구 주문서는 개별 공급원 | Main and research spellbooks keep separate providers. |
| OathboundRevenant | 신성 결속, 오라 및 강타 대상 지정 | Divine bond, auras and smite targeting. |
| PackWarden | 무리 강화는 구성원별 기대 버프 확인. 소환과 기승은 수동 | Empowerment checks each member. Summoning and mounting are manual. |
| PhantomBlade | 수호령 전력 공격은 공격 명령으로 취급 | Spirit Full Attack is an offensive command. |
| ShadowSorcerer | 소서러 주문서와 시전 시 메아리 처리 | Sorcerer spellbook and echo on cast. |
| SorrowInquisitor | 주문, 디버프의 수집과 방출 | Spells, harvested conditions and their release. |
| Spellfist | 아키타입 주문서와 주문격투 | Archetype spellbook and spell combat. |
| Spiritsteel | 신령의 분노와 결속 무기 각인 | Spirit Rage and bound weapon inscriptions. |
| Warlock | 폭발과 계약, 소환 | Blasts, pact and summons. |
| WeaponSpiritShaman | 빙의는 현재 주술사 단계의 효과와 일일 자원 확인 | Possession uses the current shaman tier and daily resource. |

## 검증 / Verification

배포 DLL과 설치된 Bubble Buffs 5.2.3으로 20/20건, Bubble Buffs 없이 9/9건, 패키지 81/81건, 무리 조련사 기승 관련 검사 672/672건을 통과했습니다. 21개 직업과 로더의 전체 회귀 44,622건 중 44,601건이 통과했습니다. 나머지 21건은 같은 과거 기준 DLL의 해시가 기록과 달라 직업마다 한 번씩 실패한 항목입니다. 캐릭터 생성·레벨업 직업 분류와 턴제 전투 표시는 실제 게임 화면 확인이 남아 있습니다.

The shipped assemblies passed 20 integration checks against Bubble Buffs 5.2.3, nine without it, 81 package checks and 672 mounted-pack checks. Of 44,622 loader and archetype regression checks, 44,601 passed. Each archetype reported the same historical reference DLL checksum mismatch. The combat HUD and both PC class-selection screens still require visual confirmation in game.
