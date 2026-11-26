# 0.17.29-beta

턴제에서 버프를 선택하고 아군을 가리키면 효과별 기본 지속시간과 이미 걸린 효과의 남은 시간을 함께 볼 수 있습니다. 자신에게 즉시 사용하는 버프는 기술 아이콘에서도 남은 시간을 확인할 수 있습니다. 예를 들어 시전자 레벨 7의 레벨당 1분 버프는 기본 7분이며, 주문 연장이 적용되면 기본 14분입니다. 주사위로 정하는 시간은 범위로, 실제 시전 때 결정되는 효과는 공식이나 기술 설명으로 표시합니다. 광역기는 가리킨 대상이 범위에 들 경우의 정보를 보여줍니다. 회복·해제·소환·토글도 같은 턴제 툴팁에서 확인할 수 있습니다.

골렘 폭주, 스피릿 웨폰 빙의, 무리 강화는 각 직업이 실제로 사용하는 지속시간 규칙을 읽습니다. 대상의 현재 버프 시간에는 이미 적용된 지속시간 변경이 반영됩니다. 전투 중 추가되는 규칙은 시전 전 기본값과 차이가 날 수 있습니다. 게임을 실행하지 않고 설치된 게임 어셈블리와 모드 DLL로 검증했습니다. 실제 턴제 화면에서의 배치와 입력 확인은 남아 있습니다.

In turn-based combat, select a buff and point at an ally to see the base duration of each effect and the time remaining on a matching active buff. Instant self buffs show the remaining time on the ability icon tooltip. At caster level 7, a one-minute-per-level buff shows a base of 7 minutes, or 14 with Extend Spell when eligible. Random durations show a range. Effects resolved during casting show the available formula or ability description. For area abilities, the preview describes the hovered unit if affected. Healing, dispels, summons and toggles also have turn-based summaries.

Golem Overdrive, spirit possession and Pack Empowerment use the duration rules of their archetypes. Remaining times reflect adjustments already applied to active buffs. Cast-time modifiers can change the base preview. Offline verification used the installed game assemblies and mod DLLs. On-screen layout and targeting input still require an in-game check.
