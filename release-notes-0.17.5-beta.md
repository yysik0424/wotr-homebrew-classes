# Homebrew Classes 0.17.5-beta

## 한국어

*Pathfinder: Wrath of the Righteous*에 아키타입 21종을 추가하는 통합 베타입니다. 예언 파괴자는 신성·비전 주문서를 함께 쓰고, 팔코너는 매와 동물 동료를 함께 운용하며, 스피릿스틸은 성장하는 부족 무기와 함께 분노합니다.

**설치** · 아래 Assets에서 `HomebrewClasses-0.17.5-beta.zip`을 받아 Unity Mod Manager에 끌어다 놓으세요. 수동 설치 시 게임의 `Mods/HomebrewClasses/`에 `Info.json`, `HomebrewClasses.dll`, `classes/`가 들어갑니다. 주문 격투가는 BrawlerClassWrath, 키메라 몽크는 DarkCodex가 필요합니다. [한국어 설치 안내와 직업 목록](https://github.com/yysik0424/wotr-homebrew-classes/blob/v0.17.5-beta/README.ko.md)

이번 버전은 팔코너가 실제 장착한 부리와 발톱으로 급강하하도록 고쳤습니다. 골렘술사의 융합 해제 시 효과 정리 순서를 수정했고, 그림자 소서러는 벽이 분신의 첫 위치를 막으면 반대쪽 어깨에서도 자리를 찾습니다.

**검증 범위** · 게임에서 21종의 1~20레벨 성장표를 확인했습니다. 직전 빌드의 마지막 실전 검사에서는 7종의 능력을 시험했고 14종은 그 회차에 재검하지 못했습니다. 수정본의 오프라인 회귀 검사 44,351건이 통과했습니다. 수정 후 팔코너의 급강하, 데스딜러의 개인 가속, 지옥의 기수의 지옥불 광선, 그림자 소서러의 분신 배치는 게임 전투에서 재확인이 남아 있습니다.

## English

This beta adds 21 archetypes to *Pathfinder: Wrath of the Righteous* in one mod. The Prophecy Breaker uses two spellbooks, the Falconer hunts with both a hawk and an animal companion, and the Spiritsteel rages alongside a tribal weapon that grows with its wielder.

**Installation** · Download `HomebrewClasses-0.17.5-beta.zip` from Assets and drop it into Unity Mod Manager. For a manual install, the game's `Mods/HomebrewClasses/` folder should contain `Info.json`, `HomebrewClasses.dll`, and `classes/`. Spellfist requires BrawlerClassWrath. Chimera Monk requires DarkCodex. [English installation guide and class list](https://github.com/yysik0424/wotr-homebrew-classes/blob/v0.17.5-beta/README.md)

This build fixes how the Falconer finds the hawk’s equipped beak and talons during a dive. Golemancer fusion now finishes cleanup after buff removal, and a lone Shadow Sorcerer echo tries the opposite shoulder when terrain blocks its first position.

**Test coverage** · All 21 level progressions were checked in game from level 1 through 20. The last live run on the preceding build exercised abilities for seven archetypes. The other 14 did not reach their turn. The release passed 44,351 offline regression checks. The Falconer’s dive, Deathdealer’s personal Haste, Hell Rider’s Hellfire Ray and Shadow Sorcerer’s echo placement still await in-game combat verification after the fixes.

**ZIP SHA-256** · `C7A9CE4BAF97F3285F648C27A14B00F2BBED944D6D04BD6873B93188F59755F4`
