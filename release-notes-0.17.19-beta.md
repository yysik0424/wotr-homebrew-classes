# Homebrew Classes 0.17.19-beta

## 한국어

스피릿스틸 결속 무기에 고레벨 피트 10종을 추가해 총 40종이 되었습니다. 12·14·16·18·20레벨마다 두 종류가 새로 열립니다. 기존 캐릭터는 조건을 충족한 다음 짝수 레벨이나 재훈련에서 고를 수 있습니다. 20레벨까지 배우는 피트는 총 10개입니다.

| 레벨 | 피트 | 효과 |
|---:|---|---|
| 12 | 상급 무기 집중 | 결속 무기 명중에 +1을 더합니다. 무기나 전사가 해당 범주의 무기 집중을 익혀야 합니다. 전사의 같은 범주 상급 무기 집중과 겹치면 한 번만 적용합니다. |
| 12 | 뿌리내린 자세 | 결속 무기를 든 동안 전법 방어도(CMD)에 +4 역량 보너스를 받습니다. |
| 14 | 거체 사냥꾼 | 결속 무기로 크기가 대형 이상인 적을 공격할 때 명중에 +2 통찰 보너스를 받습니다. |
| 14 | 신령의 요새 | 결속 무기를 든 동안 본편의 25% 요새화를 얻어 치명타와 암습을 일반 명중으로 바꿀 수 있습니다. |
| 16 | 상급 무기 전문화 | 결속 무기의 물리 피해에 +2를 더합니다. 무기나 전사가 해당 범주의 무기 전문화를 익혀야 합니다. 전사의 같은 범주 상급 무기 전문화와 겹치면 한 번만 적용합니다. |
| 16 | 신령의 회피 | 결속 무기를 든 동안 반사 내성 성공 시 피해가 절반이 되는 효과에 내성을 성공하면 피해를 받지 않습니다. |
| 18 | 심판의 일격 | 결속 무기로 악 성향의 적을 명중시키면 신성 피해 2d6을 추가합니다. 치명타에도 한 번만 더합니다. |
| 18 | 주문 방벽 | 결속 무기를 든 동안 주문 저항 32를 얻습니다. 주문 저항이 여럿이면 가장 높은 값을 적용합니다. |
| 20 | 방벽 관통 | 결속 무기의 물리 타격은 적의 피해 감소를 최대 10점 무시합니다. 본편 관통 타격 계열과 겹치면 높은 값만 적용합니다. |
| 20 | 속박 해방 | 결속 무기를 든 동안 마비, 얽힘, 감속 및 험지의 영향을 받지 않습니다. |

전체 회귀 검사 44,523건과 배포 DLL의 스피릿스틸 런타임 검사 305건을 통과했습니다. 기존 30개 선택지와 세이브 식별자 79개를 이전판과 대조했습니다. 이번 버전의 게임 화면 및 실제 세이브를 이용한 레벨업은 미실행입니다.

## English

Spiritsteel gains ten high-level bound-weapon feats, bringing the pool to 40. Two options unlock at each even barbarian level from 12 through 20. Existing characters can choose them at their next eligible even level or when retraining. You can learn ten feats by level 20.

| Level | Feat | Effect |
|---:|---|---|
| 12 | Greater Weapon Focus | Gain an additional +1 on attack rolls with the bound weapon. Requires Weapon Focus from the inscription or for its category. Overlaps with Greater Weapon Focus for that category. |
| 12 | Rooted Stance | Gain a +4 competence bonus to combat maneuver defense (CMD) while wielding the bound weapon. |
| 14 | Giant-Killer | Gain a +2 insight bonus on bound-weapon attack rolls against Large or larger targets. |
| 14 | Spirit Fortification | Gain the standard 25% fortification against critical hits and sneak attacks while wielding the bound weapon. |
| 16 | Greater Weapon Specialization | Gain an additional +2 to physical damage with the bound weapon. Requires Weapon Specialization from the inscription or for its category. Overlaps with Greater Weapon Specialization for that category. |
| 16 | Spirit Evasion | While wielding the bound weapon, take no damage on a successful Reflex save against an effect that normally deals half damage on a success. |
| 18 | Judgment Strike | Bound-weapon hits against evil targets deal an extra 2d6 holy damage, rolled once even on a critical hit. |
| 18 | Spell Ward | Gain spell resistance 32 while wielding the bound weapon. Use the highest applicable spell resistance. |
| 20 | Pierce Defenses | Physical hits with the bound weapon ignore up to 10 points of damage reduction. Use the higher value when combined with the Penetrating Strike feats. |
| 20 | Unfettered Spirit | While wielding the bound weapon, gain immunity to paralysis, entanglement, slow, and difficult terrain. |

The full regression suite passed 44,523 checks. The packaged Spiritsteel DLL passed 305 checks in the game’s managed runtime. All 30 prior choices and 79 persistent identifiers match the previous release. In-game level-up on a player save was not tested for this release.
