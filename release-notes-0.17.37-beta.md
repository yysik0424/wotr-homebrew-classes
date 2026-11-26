# Homebrew Classes 0.17.37-beta

턴제에서 적을 가리키면 현재 상태로 **본체를 맞힐 예상 확률**을 표시합니다. 알려진 방어도와 무방비, 협공, 주적, 은폐와 은폐 재굴림, 환영 분신, 고정 d20과 해석 가능한 명중 재굴림을 계산합니다. 일반 무기는 전력공격 시의 후속타와 보조 무기·자연 무기의 확률도 현재 상태 기준으로 보여줍니다. 예를 들어 명중 +12 대 AC 20은 65%, 은폐 20%를 적용하면 52%, 거기에 분신이 세 개면 본체 적중은 13%입니다.

수치는 가리킨 시점의 추정치입니다. 받아넘기기, 시전 중 발동하는 반응이나 모드 전용 코드, 앞선 타격으로 소모되는 효과는 실제 확률을 바꿀 수 있습니다. 해석할 수 없는 조건부 효과를 발견하면 미확정으로 표시합니다. 실제 판정에는 엔진이 적용한 명중 보너스와 방어도 기준으로 일반 d20 한 번이 성공할 확률도 표시합니다.

In turn-based combat, hovering a target shows the **estimated chance to hit the real target**. The estimate uses revealed AC, flat-footed and flanking conditions, favored enemy bonuses, concealment and its rerolls, mirror images, and supported fixed or rerolled attack dice. Weapon full-attack follow-ups, off-hand weapons and natural attacks show their own chances at the current state. For example, +12 against AC 20 gives 65%, falling to 52% against 20% concealment and to 13% against three additional mirror images.

These estimates use the state at hover time. Parries, scripted reactions or mod-specific rules at execution, and effects spent by earlier attacks can change the outcome. Detected conditional rules that cannot be evaluated are marked pending. The resolved attack also shows the single-d20 chance against the attack bonus and AC that the engine actually used.

게임을 열지 않고 배포 DLL을 게임 Mono에서 검증했습니다. Bubble Buffs 5.2.3과 함께 226/226, 단독 214/214, 패키지 83/83을 통과했고 공개 소스로 다시 빌드한 DLL 22개의 내용을 대조했습니다. 실제 전투 화면은 수동 검증 전입니다.

The packaged DLL passed 226/226 checks with Bubble Buffs 5.2.3 and 214/214 standalone under the game’s Mono runtime. All 83 package checks passed, and all 22 publicly rebuilt DLLs matched in compiled content. Live combat HUD verification is pending.
