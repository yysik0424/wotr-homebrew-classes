# Homebrew Classes 0.17.39-beta

## 그레이 시어

- 주문을 적중시키면 같은 적에 대한 다음 선택 무기 명중이 오릅니다. 선택 무기를 맞히면 같은 적에 대한 다음 아카니스트 주문 명중이 오르거나 그 적의 다음 주문 내성이 내려갑니다.
- 보정은 아카니스트 1·7·13·19레벨에 각각 1·2·3·4입니다. 다음 자기 턴 종료까지 한 번, 실시간에서는 1라운드 동안 적용됩니다. 같은 효과를 거듭 맞혀도 보정은 중첩되지 않습니다.
- 별도 행동으로 번갈아 공격하면 무료입니다. 일반 연계는 비전 저수지 1점으로 주문과 검격 한 타를 묶습니다. **검격 먼저**를 켠 뒤 주문을 고르면 검격부터 하고, 기존 수동 지정이나 자동 검격을 쓰면 주문부터 시작합니다. 유료 연계 안에서도 첫 적중이 두 번째 판정을 도와줍니다. 안전 검격의 방어적 시전 비용에는 검격 1회가 포함됩니다.

## Grey Seer

- Land an arcanist spell to improve your next chosen-weapon attack against that foe. Land a chosen-weapon hit to improve your next arcanist spell attack against that foe or reduce its save against your spell.
- The bonus is 1, 2, 3 or 4 at arcanist levels 1, 7, 13 and 19. Each opening applies once through the end of your next turn (one round in real time). Repeated hits refresh it.
- Alternating separate actions is free. A regular combined action costs 1 reservoir point. Enable **Strike Before Casting** and choose your spell to lead with the weapon. Leave it off and use a designation or automatic stance to lead with the spell. The first hit can help the second roll in the same action. Guarded casting includes the strike in its protection cost.

## 검증 / Verification

게임의 Mono 런타임에서 수동·자동 연계의 순서, 명중·빗나감, 표적별 내성, 저수지 차감과 턴 종료를 검사했습니다. 실제 게임 화면에서의 전투는 확인 대기입니다.

Offline checks in the game's Mono runtime exercised manual and automatic combinations, both orders, hits and misses, target-specific saves, reservoir spending and turn expiry. Live combat still awaits an in-game check.
