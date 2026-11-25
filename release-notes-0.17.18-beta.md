# Homebrew Classes 0.17.18-beta

## 한국어

스피릿스틸은 지혜 수정치로 일일 분노 라운드를 계산합니다. 기본 라운드는 **2 + 바바리안 레벨 × 2 + 지혜 수정치**입니다. 지혜 18이면 1레벨에 8라운드, 20레벨에 46라운드입니다. 기본 분노와 신령의 분노를 함께 켜면 라운드당 1회를 소모합니다. 추가 격노는 같은 자원을 늘리고, 끝없는 격노는 소모를 없앱니다. 기존 캐릭터는 다음 휴식 때 새 한도까지 회복합니다.

배포 DLL을 본편 런타임에서 실행해 스피릿스틸 검사 258건을 통과했습니다. 분노 라운드는 일반 바바리안과 비교했고, 다중 직업, 추가 격노, 임시 지혜 보너스, 소모 후 재로드와 휴식 회복을 점검했습니다.

## English

Spiritsteel now uses Wisdom for daily rage rounds. Base rounds equal **2 + twice your barbarian level + your Wisdom modifier**. Wisdom 18 gives 8 rounds at level 1 and 46 at level 20. Keeping barbarian rage and Spirit Rage active together spends one round per round. Extra Rage extends the shared pool, and Limitless Rage removes the cost. Existing characters refill to the new limit on their next rest.

The packaged Spiritsteel DLL passed 258 checks in the game’s managed runtime. Rage checks compared Spiritsteel with a standard barbarian and covered multiclass levels, Extra Rage, temporary Wisdom bonuses, spent rounds across resource reloads, and rest.
