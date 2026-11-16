# Homebrew Classes for Pathfinder: Wrath of the Righteous

**English** | [한국어](README.ko.md)

Twelve new archetypes for *Pathfinder: Wrath of the Righteous*, packed into a single Unity Mod Manager mod. Each one changes how its base class plays rather than just shuffling numbers: a hunter who fights through a pack of animals, an alchemist who wears a golem, a witch who trades hexes for blasts, and more.

> **Status: beta, first release in preparation.**
> There is no download yet. The first build (`0.9.0-beta`) will appear on the [Releases](../../releases) page. This page describes what that build contains.

## The classes

| Archetype | Base class | What it does | Needs |
|---|---|---|---|
| [Pack Warden](#pack-warden) | Hunter | Commands a pack: extra animals of your companion's kind join each fight | — |
| [Phantom Blade](#phantom-blade) | Fighter | A guardian phantom strikes alongside you and absorbs half the damage you take | — |
| [Blast Magus](#blast-magus) | Magus | Picks an element, fires blasts with Intelligence and detonates them through weapon hits | *KineticistElementsExpanded* (optional: 5 more elements) |
| [Spellfist](#spellfist) | Brawler | Unarmed brawler with full arcane casting from three chosen schools | **BrawlerClassWrath** |
| [Golemancer](#golemancer) | Alchemist | Builds a golem tank, changes its material, and later wears it | — |
| [Warlock](#warlock) | Witch | Unlimited patron blasts that build Overheat and call summons | — |
| [Weapon-Spirit Shaman](#weapon-spirit-shaman) | Shaman | A floating spirit weapon fights on its own and borrows the magic of stored weapons | — |
| [Bulwark Mage](#bulwark-mage) | Wizard | Full caster in heavy armor behind a tower shield that strikes back | — |
| [Chaos Warrior](#chaos-warrior) | Warpriest | Rages, mutates at random and drives enemies mad with stacking pain | — |
| [Chimera Monk](#chimera-monk) | Monk | Reshapes the body with a new mutation every two levels | **DarkCodex** |
| [Oathbound Revenant](#oathbound-revenant) | Paladin | An undead paladin whose smites knit the body back together | — |
| [Shadow Sorcerer](#shadow-sorcerer) | Sorcerer | Shadow doubles recast your spells as illusions | — |

Full details for every archetype (what is removed, what is gained at each level, every choice) are in [Class details](#class-details) below.

## Requirements

- *Pathfinder: Wrath of the Righteous* (Enhanced Edition, current Steam/GOG version)
- [Unity Mod Manager](https://www.nexusmods.com/site/mods/21) 0.27.11 or newer
- Optional mods, only for the classes that use them:
  - **BrawlerClassWrath**, required by Spellfist
  - **DarkCodex**, required by Chimera Monk
  - **KineticistElementsExpanded**, adds five more elements to Blast Magus

If a required mod is missing, only that one class is left out. The rest of the mod and the game itself load normally, and the Unity Mod Manager window tells you which class was skipped and why.

## Installation

1. Install Unity Mod Manager and point it at the game.
2. Download `HomebrewClasses-<version>.zip` from the Releases page.
3. Drag the zip onto the Unity Mod Manager window, or extract it so that the folder `Mods\HomebrewClasses` sits inside the game folder.
4. Start the game. The archetypes appear in character creation under their base classes.

## Settings

Open the Unity Mod Manager window in game (Ctrl+F10). Every class has its own on/off switch. All are on by default; changes apply after a restart.

## Language

The mod ships Korean and English text in the same file and picks one automatically. If your game is running a Korean translation, you get Korean. Every other game language gets English.

## Compatibility and warnings

- **Saves:** once a character has levels in one of these archetypes, that save needs the mod. Turning a class off, or removing the mod, makes saves that contain that class fail to load.
- **Beta:** the classes are covered by automated tests, but in-game play time is limited. Expect rough edges and please report them.
- **DLC:** a few pieces use DLC content (noted in the class details). Play without those DLCs has not been tested in game.
- Notes about specific mods (TabletopTweaks, PrestigePlus, ExpandedContent and others) are listed under each class.

## Uninstalling

Remove the mod in the Unity Mod Manager window or delete `Mods\HomebrewClasses`. Read the save warning above first.

## Reporting bugs

Open an [issue](../../issues) and include: the class, what you did, what happened, your mod list, and the files `Player.log` and `GameLogFull.txt` from `%USERPROFILE%\AppData\LocalLow\Owlcat Games\Pathfinder Wrath Of The Righteous`.

## License and credits

- Licensed under the [GNU GPL v3.0](LICENSE), with an additional permission for linking with the game and other mods; see [NOTICE.md](NOTICE.md).
- Thanks to the authors of Unity Mod Manager, Harmony, BrawlerClassWrath, DarkCodex and KineticistElementsExpanded.
- *Pathfinder: Wrath of the Righteous* belongs to Owlcat Games; Pathfinder belongs to Paizo Inc. This is an unofficial fan mod and is not affiliated with either.

---

# Class details

Click a class to expand it.

## Pack Warden

<details>
<summary><b>Pack Warden</b></summary>

### At a glance

| | |
|---|---|
| Base class | Hunter |
| Key ability | Wisdom. The archetype recommends Wisdom, Dexterity and Constitution, and marks Charisma and Intelligence as not recommended |
| Spellcasting | None. The Hunter spellbook and orisons are removed entirely |
| Required mods | None |
| Optional integrations | None |
| Alignment / deity restrictions | None added by the archetype |

### Overview

The Pack Warden is a Hunter who travels with several animals at once. You give up all spellcasting; in exchange, auxiliary animals of the same species as your main animal companion appear whenever a fight starts. The auxiliaries do not pick targets of their own. They trail the main companion, and every time it swings in melee each of them bites the same target once. Your own job is command: mark a studied target, then flip between the Fierce and Tough aspects to buff the whole pack. From levels 1 to 6 a single toggle calls one auxiliary; from level 7 you have to fight from the saddle of your main companion, and two auxiliaries follow.

### What you give up

| Level | Base feature | Notes |
|---|---|---|
| 1 | Hunter spellcasting (the whole spellbook) | All 1st- to 6th-level spells are gone, with no way to get them back |
| 1 | Orisons | No 0-level spells either |
| 1 | Animal Companion selection | Replaced by Pack's Main Animal Companion, which offers fewer species (see Choices) |
| 1 | Animal Focus | Replaced by Aspects of the Pack. The focus you could apply to yourself is gone too |
| 8 | Second Animal Focus | Not replaced. The archetype grants nothing at level 8 |

Every Hunter feature not listed here is kept as in the base game, including Hunter Tactics at level 3, the teamwork feats, Raise Animal Companion at level 10 and Master Hunter at level 20.

### What you gain (by level)

| Level | Feature | Effect |
|---|---|---|
| 1 | Pack's Main Animal Companion | Choose an animal companion from the species of Medium size or larger that can serve as mounts. Its advancement, the mount toggle and the animal companion archetype choice work as for a base Hunter |
| 1 | Studied Target | The level 1 Studied Target of the base game's Tandem Executioner hunter archetype, unchanged |
| 1 | Aspects of the Pack | Two toggles, Fierce Aspect and Tough Aspect. The active aspect gives the main companion and every auxiliary a bonus equal to your Wisdom modifier. Switching is a free action with no limit |
| 1 | Call the Pack | If the call condition is met when you enter combat, auxiliary animals of your main companion's species appear. 1 auxiliary at level 1, 2 from level 7. Auxiliary level = Hunter level − 4 (minimum 1). The pack disperses when combat ends |
| 1 | Call the Pack: Toggle | The call condition for levels 1–6: the Call the Pack toggle is on (it is on by default) |
| 4 | Tandem Executioner base feature | The base feature a Tandem Executioner receives at level 4, which the techniques below build on |
| 4 | Tandem Executioner technique (1st) | Pick one technique from the Tandem Executioner list |
| 7 | Mounted Call | The call condition changes: you must be riding your main animal companion when combat starts. The Call the Pack toggle is removed. The pack grows to 2 auxiliaries |
| 7 | Swift Studied Target | The base game's Slayer feature |
| 7 | Tandem Executioner technique (2nd) | |
| 10 | Tandem Executioner technique (3rd) | |
| 13 | Tandem Executioner technique (4th) | |
| 16 | Tandem Executioner technique (5th) | |
| 19 | Tandem Executioner technique (6th) | |
| 20 | Both Aspects | Fierce Aspect and Tough Aspect can be active at the same time |

### Abilities in detail

#### Call the Pack

- **Activation**: automatic. The moment you enter combat with the call condition met, the auxiliaries appear next to you. It costs no action and no resource.
- **Call condition**
  - Levels 1–6: the Call the Pack toggle is on. It starts on and resting does not turn it off. Turning it on in the middle of a fight calls the pack at once. Turning it off in the middle of a fight does not dismiss a pack that is already out.
  - Level 7 and up: you are riding **your own main animal companion**. No other mount counts. Mounting up in the middle of a fight calls the pack at once.
- **Pack size**: 1 auxiliary at Hunter levels 1–6, 2 at level 7 and up. Never more than 2.
- **What an auxiliary is**
  - Species: the same as your main animal companion.
  - Level: Hunter level − 4, minimum 1. Only Hunter class levels count.
  - Species upgrade: once the auxiliary's level reaches that species' upgrade level (the same value the base game uses for animal companions), it receives the same upgrade feature. The pre-order bonus horse has no upgrade feature in the base game and so gets none.
  - Pack's Quarry: +2 on attack rolls against a foe you have marked as your studied target.
  - It receives the bonus of whichever aspect is active as soon as it appears.
  - It counts as a summoned creature, and it dies outright at 0 hit points instead of dropping into a dying state.
- **When the pack disperses**
  - Always when the party's combat ends.
  - When you leave combat while conscious. While you are down, the pack stays and keeps fighting (ordinary summons vanish when their summoner falls; auxiliaries are exempt).
  - Level 7 and up: when you dismount by choice. Mounting again brings the pack straight back. Entering an area where riding is not allowed also disperses it.
  - Level 7 and up: being thrown off because you or the mount went down, or being dismounted by a cutscene, does **not** disperse the pack.
- **Recall delay**: when an auxiliary dies, its place can be refilled after 1 hour of game time. Each place is tracked separately. Completing a rest clears the delay; an interrupted rest does not. An auxiliary that dispersed without dying has no delay.

#### How the auxiliaries fight

- While the main companion is conscious, the auxiliaries **do not attack on their own**. They only move to keep up with it.
- Each time the main companion makes one melee weapon swing (natural weapons included), every living auxiliary makes **one attack against the same target**. These are real attacks with their own attack and damage rolls.
  - Requirements: the target is alive, the auxiliary can act, and the target is within the auxiliary's melee reach. An auxiliary that fails any of these skips that swing.
  - The auxiliary uses a natural weapon of the same type the main companion used, or its primary weapon if it has none of that type.
  - It takes the same attack penalty that applied to the main companion's swing, such as the penalty on later iterative attacks.
  - Ability attacks such as Cleave or Vital Strike are followed as well.
  - Attacks of opportunity and extra attacks made by the main companion are not followed, and neither are ranged weapon attacks.
  - Automatic hits, automatic critical threats and charge bonuses on the main companion's attack do not carry over.
- Auxiliaries still make their own attacks of opportunity as normal.
- While the main companion is down or absent, the auxiliaries **fight on their own** the way a base-game animal companion does. They go back to following once it is on its feet again.
- If the main companion is hidden (removed from the active party, for example) the auxiliaries are hidden with it and no new ones are called in the meantime. If it leaves for good, the pack disperses.

#### Aspects of the Pack

- **Activation**: two toggles. Turning one on or off is a free action with no limit on uses. They cost no resource and resting does not turn them off.
- **Fierce Aspect**: the main companion and the auxiliaries gain a bonus on attack and damage rolls equal to your Wisdom modifier.
- **Tough Aspect**: the main companion and the auxiliaries gain an AC bonus equal to your Wisdom modifier. Each auxiliary also receives temporary hit points equal to your Wisdom modifier × Hunter level, once, when it appears.
- A Wisdom modifier of 0 or lower gives a bonus of 0. It never becomes a penalty.
- The bonuses are untyped and stack.
- You yourself gain nothing from either aspect.
- Through level 19 only one aspect can be active; turning one on turns the other off. Both Aspects at level 20 lets you run the two together.
- The Tough Aspect's temporary hit points are granted only if Tough Aspect is active at the moment the auxiliary appears. Switching to Tough Aspect in the middle of a fight does not grant them, and lost temporary hit points are never refilled.
- If the main companion is revived or joins in the middle of a fight, the active aspect is applied to it again.

#### Studied Target and Tandem Executioner techniques

- Studied Target at level 1, the base feature at level 4 and the technique picks at levels 4, 7, 10, 13, 16 and 19 are taken unchanged from the base game's Tandem Executioner. Their effects and numbers are the base game's.
- Swift Studied Target at level 7 is the base game's Slayer feature.
- What the auxiliaries get out of your studied target is the +2 attack bonus from Pack's Quarry.

### Choices

**Pack's Main Animal Companion** — choose one at level 1. "No companion" is not offered.

| Available species | Notes |
|---|---|
| Bear | |
| Boar | |
| Centipede | |
| Dog | |
| Elk | |
| Horse | The pre-order bonus variant is also in the list |
| Mammoth | |
| Smilodon | The pre-order bonus variant is also in the list |
| Triceratops | The pre-order bonus variant is also in the list |
| Wolf | |

- Base-game species that are not offered: Leopard, Monitor Lizard and Velociraptor. They start at Small size.
- Species that other mods add to the Hunter's companion list do not appear in this archetype's list.
- Each species has the same statistics and advancement as the base-game animal companion. Auxiliaries appear as the species you chose.

**Tandem Executioner techniques** — one at each of levels 4, 7, 10, 13, 16 and 19, from the base game's Tandem Executioner list. The list and its effects are unchanged.

### Feats

- The archetype grants no feats automatically.
- The Hunter's bonus feat slots (teamwork feats and so on) are unchanged.
- No feat becomes unselectable. Without a spellbook, though, Hunter levels cannot satisfy feats or prestige class requirements that call for spellcasting.
- No feats were added specifically for this archetype.

### Resources

- There is no resource pool and no daily limit. Call the Pack and both aspects are unlimited.
- The only limit is the **recall delay**: a dead auxiliary's place is refilled after 1 hour of game time, or after a completed rest.

### Compatibility and notes

- No other mod is required. If the game data is not what the archetype expects (a patch or another mod has changed the Hunter progression or the animal companion list), the archetype does not appear in character creation and the rest of the game runs normally.
- **Mods that add animal companions (ExpandedContent and the like)**: the added species stay available to a base Hunter but are left out of the Pack Warden's list.
- **Pre-order bonus animals**: the pre-order bonus variants of the Horse, Smilodon and Triceratops are part of the selection list.
- **Multiclassing and prestige classes**: the number and level of auxiliaries, the switch to Mounted Call at level 7 and the Tough Aspect's temporary hit points all use Hunter class levels only. With no spellbook, Hunter levels contribute nothing to the requirements or spell progression of caster prestige classes.
- **Two Pack Wardens in one party** each keep their own pack and their own aspects.
- **Known limitations**
  - From level 7 you cannot call the pack unless you can ride your main companion. The base-game rule that a mount must be larger than its rider still applies.
  - Auxiliaries exist only during combat. Outside combat you travel with the main companion alone.
  - For an ability attack that does not go through a weapon slot, if it cannot be determined which natural weapon the main companion used, that swing is not followed.
  - The vanishing effect when auxiliaries disperse can look cut short, and it does not play for auxiliaries that are neither Medium nor Large (under a size-changing effect, for instance). Rules are unaffected.


</details>

## Phantom Blade

<details>
<summary><b>Phantom Blade</b></summary>

### At a glance

| | |
|---|---|
| Base class | Fighter archetype |
| Key ability | Charisma (recommended: Charisma, Strength, Constitution; not recommended: Intelligence) |
| Spellcasting | None (same as fighter) |
| Required mods | None |
| Optional integrations | TabletopTweaks-Base — if its Advanced Armor Training option is on, the archetype recognises that layout and trades it away correctly |
| Alignment / deity restrictions | None |

### Overview

The Phantom Blade is a fighter bound to a Guardian Phantom. You fight in melee as usual while a shadowy figure overlapping your body drives force fists into whatever you attack. The same phantom soaks half of the damage you take into its own absorption pool. That pool is both your defence and the fuel for Ghost Slash, your ranged attack, so every fight is a choice between keeping the pool for protection and spending it on offence. In exchange you give up bravery, armor training and armor mastery; bonus feats, weapon training and weapon mastery are untouched.

### What you give up

| Level | Base feature | Notes |
|---|---|---|
| 2, 6, 10, 14, 18 | Bravery | All five steps removed |
| 3, 7, 11, 15 | Armor Training | All four steps removed. Under the TabletopTweaks-Base Advanced Armor Training layout, the armor training selections that sit there (levels 7, 11, 15) and the armor speed feature (levels 3, 7, 11, 15) are removed as well |
| 19 | Armor Mastery | Removed |

Fighter bonus feats (levels 1, 2, 4, 6, … 20), Weapon Training (5, 9, 13, 17), Weapon Mastery (20) and all weapon and armor proficiencies are unchanged.

### What you gain (by level)

| Level | Feature | Effect |
|---|---|---|
| 2 | Phantom Blade — the Guardian Phantom and its absorption pool | Grants the Manifest Guardian Phantom toggle (free action, no cost, on by default). While it is manifest, the pool takes 50% of the damage you would suffer. Pool maximum = Phantom Blade level × Charisma modifier (minimum 1) |
| 3 | Guardian Phantom Rush — once per round | When one of your melee attacks hits, the phantom strikes the same target once with its fist. Up to 1 time per round. Damage = fist dice + Charisma modifier, force |
| 5 | Ghost Slash: 1 Projectile | Standard action, 30 ft., 1 force projectile (ranged touch). Costs half your level (rounded up) from the absorption pool |
| 6 | Guardian Phantom Ranged Guard | +2 AC against ranged attacks while the phantom is manifest |
| 7 | Guardian Phantom Rush — twice per round | The per-round limit rises to 2 (still one blow per attack) |
| 10 | Guardian Phantom Refill | The absorption pool refills completely when combat ends |
| 10 | Ghost Slash: 2 Projectiles | Ghost Slash fires 2 projectiles |
| 10 | (Rush rule change — no separate feature) | The phantom now follows up even when your attack **misses**. It makes its own melee touch attack (base attack bonus + Charisma modifier against touch AC). A missed attack still uses up one of that round's follow-ups |
| 11 | Guardian Phantom Rush — 3 times per round | Per-round limit of 3 |
| 14 | Guardian Phantom Ranged Guard (second step) | The AC bonus against ranged attacks rises to +4 in total |
| 15 | Guardian Phantom Rush — 4 times per round | Per-round limit of 4 |
| 15 | Guardian Phantom Full Assault | Swift action, 1 round. For that round Rush has no per-round limit, and the phantom makes **2** touch blows for each melee attack you make. Usable Charisma modifier times per day (minimum 1) |
| 15 | Ghost Slash: 3 Projectiles | Ghost Slash fires 3 projectiles |
| 18 | Guardian Phantom Greater Absorption | The absorbed share rises from 50% to 75% |
| 19 | Guardian Phantom Rush — 5 times per round | Per-round limit of 5 |
| 20 | Ghost Slash: 4 Projectiles | Ghost Slash fires 4 projectiles |

### Abilities in detail

#### Manifest Guardian Phantom

- A toggle. Turning it on or off is a free action, it costs nothing, and it stays on through rest. It is already on when you gain it at level 2.
- Rush, absorption, Ranged Guard, Ghost Slash and Full Assault all work **only while the phantom is manifest**.
- While manifest, a shadowy figure overlaps your character, follows you, and swings along with Rush, Ghost Slash and Full Assault. It is purely cosmetic: it cannot be selected or attacked, takes no part in combat, does not block movement and is never written to your save. It hides when you are stealthed or invisible.

#### Absorption pool (Phantom Blade Absorption Pool)

- While the phantom is manifest, the pool takes **50%** of the damage you would suffer (**75%** from level 18). The absorbed amount is rounded down and can never exceed what is left in the pool.
- Absorption applies to the damage that remains **after** damage reduction, resistances and immunities.
- When the pool is empty, only absorption stops. The phantom stays manifest and Rush keeps working.
- Damage that ignores damage reduction is not absorbed.
- Every absorption writes a line to the combat log — "The phantom absorbed N damage (pool left M)" — and shows "Absorb N" over your character's head.

#### Guardian Phantom Rush

Automatic. There are only two rules.

1. **Each time you make one melee attack, the phantom hits the same target once with its Phantom Fist.** It never lands several blows off a single attack.
2. **There is a limit on how many times per round this can happen.** The phantom follows up on your first attacks of the round, up to the limit, and not on any attack after that.

| Fighter level | Follow-ups per round (limit) |
|---|---|
| 3–6 | 1 |
| 7–10 | 2 |
| 11–14 | 3 |
| 15–18 | 4 |
| 19–20 | 5 |

In game these steps are named "Rush: 1 Strike" through "Rush: 5 Strikes". The number is the per-round limit from the table above, not the number of blows per attack.

What gets counted changes at level 10.

| | Levels 3–9 | Level 10 and up |
|---|---|---|
| The phantom follows up when | your attack **hits** | you attack, **hit or miss** |
| What counts against the limit | only attacks that hit (a miss uses nothing) | every attack (a miss counts as one) |
| The phantom's attack roll | its own attack roll at your full base attack bonus, with no iterative penalty | a melee touch attack: full base attack bonus + Charisma modifier against touch AC |

Examples:

- Level 7 (limit 2), two attacks, both hit: 2 phantom blows.
- Level 7 (limit 2), first attack misses, second hits: 1 phantom blow.
- Level 11 (limit 3), four attacks under haste: the phantom follows up on the first three (rolling on its own even if they miss) and not on the fourth.
- Level 3 (limit 1), your regular attack and an attack of opportunity both hit: 1 phantom blow, on whichever hit first.

A fighter's number of attacks grows at about the same levels as the limit does, so in ordinary play nearly every attack gets one phantom blow. The limit only bites when haste, two-weapon fighting or attacks of opportunity give you more attacks than it allows.

- Ranged attacks never trigger it.
- Damage per blow: fist dice + Charisma modifier, **force** damage. Confirmed critical hits apply the fist's multiplier.
- The fist dice follow your fighter level at the moment of the strike and match the monk's unarmed strike table for a Medium creature, regardless of your own size.
- From level 10, your sneak attack and other precision damage are not added to the touch blows.
- The phantom's fists never trigger Rush themselves.
- Attacks of opportunity count as melee attacks and use up that round's follow-ups.
- Switching the phantom off and on does not give back follow-ups already used that round.
- In a round where you use Full Assault (level 15), the limit is gone and each attack gets 2 blows. That is the only case where one attack produces more than one blow.
- Dismissing and re-manifesting the phantom does not give back blows already used that round.

| Fighter level | Blows per round | Fist dice | Mode |
|---|---|---|---|
| 3 | 1 | 1d6 | Rides on your hits |
| 4–6 | 1 | 1d8 | Rides on your hits |
| 7 | 2 | 1d8 | Rides on your hits |
| 8–9 | 2 | 1d10 | Rides on your hits |
| 10 | 2 | 1d10 | Touch attack per attack |
| 11 | 3 | 1d10 | Touch attack per attack |
| 12–14 | 3 | 2d6 | Touch attack per attack |
| 15 | 4 | 2d6 | Touch attack per attack |
| 16–18 | 4 | 2d8 | Touch attack per attack |
| 19 | 5 | 2d8 | Touch attack per attack |
| 20 | 5 | 2d10 | Touch attack per attack |

#### Ghost Slash

- Standard action. A supernatural ability, so spell resistance does not apply. Usable only while the phantom is manifest.
- Hurls force projectiles at one enemy within 30 feet. When it fires several, they all go to the same target.
- Each projectile makes its own **ranged touch attack** with base attack bonus + **Charisma** modifier (not Dexterity).
- Damage per projectile: the same fist dice as Rush + Charisma modifier, force. It crits like a ray (20 / ×2).
- Sneak attack and other precision damage apply to only one projectile per use.
- Cost: each use spends **half your fighter level (rounded up)** from the absorption pool. It cannot be used if the pool holds less.
- Ghost Slash does not trigger Rush.

| Fighter level | Projectiles | Damage per projectile | Pool cost |
|---|---|---|---|
| 5–7 | 1 | 1d8 + Cha | 3–4 |
| 8–9 | 1 | 1d10 + Cha | 4–5 |
| 10–11 | 2 | 1d10 + Cha | 5–6 |
| 12–14 | 2 | 2d6 + Cha | 6–7 |
| 15 | 3 | 2d6 + Cha | 8 |
| 16–19 | 3 | 2d8 + Cha | 8–10 |
| 20 | 4 | 2d10 + Cha | 10 |

#### Guardian Phantom Ranged Guard

- While the phantom is manifest, you gain +2 AC against ranged attacks at level 6, rising to +4 in total at level 14.
- The bonus is untyped and stacks with everything. It applies to ranged weapon attacks and to ranged touch attacks such as rays, but not to melee attacks.

#### Guardian Phantom Refill

- From level 10, the absorption pool returns to its maximum every time combat ends.

#### Guardian Phantom Full Assault

- Swift action, lasts 1 round. Usable only while the phantom is manifest.
- For that round the per-round limit on Rush is lifted, and the phantom makes **2** touch blows on **every** melee attack you make, hit or miss. These blows do not count against the normal Rush limit.
- Usable a number of times per day equal to your Charisma modifier (minimum 1).

### Choices

None

### Feats

- The archetype grants no feats automatically.
- All fighter bonus feat slots are kept.
- No new feats are added for this archetype.
- If you play with TabletopTweaks-Base Advanced Armor Training, the Phantom Blade gives up its armor training slots entirely, so it does not get the advanced armor training selections that would sit there.

### Resources

| Resource | Maximum | Restored |
|---|---|---|
| Phantom Blade Absorption Pool | Phantom Blade (fighter) level × Charisma modifier (minimum 1) | On rest and on level-up; from level 10 also whenever combat ends |
| Guardian Phantom Full Assault uses | Charisma modifier (minimum 1) | On rest and on level-up |

Damage absorption and Ghost Slash draw on the same pool. You can read what is left from the "pool left" number in the absorption log line and, from level 5, from the remaining-uses counter on the Ghost Slash button. The Manifest Guardian Phantom button itself shows no number.

### Compatibility and notes

- **Required mods and DLC**: none. Only base-game assets are used.
- **TabletopTweaks-Base**: if its Advanced Armor Training option has rearranged the fighter progression, the Phantom Blade recognises that layout and removes everything that sits in those slots. If it meets a fighter progression that is neither the base game's nor TabletopTweaks' (another mod has moved or removed armor training at levels 3, 7, 11 or 15, bravery, or armor mastery), the archetype is not registered and will not appear in the archetype list.
- **Multiclassing and prestige classes**: The Rush limit, fist dice, Ghost Slash, the absorbed share, Ranged Guard and the pool size all look at **fighter levels** only. Levels in other classes help only through base attack bonus, which the phantom uses for its attack rolls.
- **Beyond level 20**: if your fighter level somehow exceeds 20, Rush, fist dice and Ghost Slash stay at their level-20 values.
- **Difficulty multipliers**: the pool is charged in damage **before** the difficulty's damage multiplier. On difficulties where incoming damage is scaled, the points drained from the pool and the hit points actually saved can differ.
- **Combat log**: absorption lines appear on the log's "All" tab, not on the "Combat" tab.
- **Damage previews**: predicted damage in tooltips is shown before absorption.
- **Other stat-replacement effects**: at levels 3–9 the Rush fist adds Charisma to damage, but if you have another effect that swaps in a higher ability score for weapon damage, the engine may apply that one instead, as it would for any attack.
- **Turn-based mode**: a "round" for Rush is your own action cycle in real time, and begins at the start of your turn in turn-based mode.


</details>

## Blast Magus

<details>
<summary><b>Blast Magus</b></summary>

### At a glance

| Item | Details |
|---|---|
| Base class | Magus archetype |
| Key ability | Intelligence (recommended: Intelligence, Strength, Dexterity; not recommended: Charisma) |
| Spellcasting | Changed: the magus spellbook is kept, but you lose cantrips (0-level spells) and spell recall |
| Required mods | Unity Mod Manager |
| Optional integrations | KineticistElementsExpanded: when enabled, the element choice grows from 6 to 11 elements |
| Restrictions | No alignment or deity restriction. Multiclassing with kineticist disables the blast features (see Compatibility and notes) |

### Overview

The Blast Magus fits elemental detonations between blade and spell. At 1st level you pick one element, fire a kineticist-style ranged blast driven by Intelligence, and keep your melee weapon charged with extra damage of the same element. From 4th level you can spend 1 arcane pool point to load your next melee hit with a full blast's worth of damage. Spell Combat and Spellstrike are untouched, so a single hit can carry weapon damage, Energize Weapon, a touch spell and Blast Strike at once. The price is your cantrips, spell recall and the arcane pool's weapon enhancement.

### What you give up

| Level | Base feature | Notes |
|---|---|---|
| 1 | Cantrips | You cannot cast 0-level spells |
| 1 | Detect Magic | |
| 1 | Arcane Pool | Replaced by Blast Pool: the same pool points, but no ability to add enhancement bonuses or properties to your weapon |
| 3, 6, 9, 12, 15, 18 | Magus Arcana selection | Replaced at the same levels by Blast Magus Arcana, which omits the four weapon-enhancement-only arcana |
| 4 | Spell Recall | |
| 5, 9, 13, 17 | Arcane Weapon +2, +3, +4, +5 | The four upgrades of the arcane pool weapon enhancement |
| 11 | Improved Spell Recall | |

Everything else is unchanged, including Spell Combat, Spellstrike and spellcasting from 1st-level spells up.

### What you gain (by level)

| Level | Feature | Effect |
|---|---|---|
| 1 | Blast Pool | The same resource as the arcane pool (half magus level + Intelligence modifier). Fuels Blast Strike and magus arcana. No weapon enhancement ability |
| 1 | Element Choice | Fire, cold or electricity (energy) or air, earth or water (physical). Permanent |
| 1 | Elemental Blast | Fire the simple blast of your element at no cost: a ranged touch attack for energy elements, a ranged attack for physical elements. Attack uses the higher of Dexterity and Intelligence; caster level equals magus level |
| 1 | Energize Weapon | Free-action toggle. Every attack with a melee manufactured weapon deals +1d6 damage of your element |
| 1, 3, 5, 7, 9, 11, 13, 15, 17, 19 | Blast Rank +1 | Blast rank rises by 1 (rank 1 at 1st level, rank 10 at 19th). Each rank adds to your blast damage dice |
| 3, 6, 9, 12, 15, 18 | Blast Magus Arcana | Choose one magus arcana (the four weapon-enhancement-only arcana are not offered) |
| 4 | Blast Strike | Swift action, 1 arcane pool point. Your next melee manufactured-weapon hit this round carries full blast damage (d6s equal to your blast rank, plus a bonus) |
| 7 | Energize Weapon: 2d6 | Energize Weapon damage becomes 2d6 |
| 13 | Energize Weapon: 3d6 | Energize Weapon damage becomes 3d6 |
| 19 | Energize Weapon: 4d6 | Energize Weapon damage becomes 4d6 |
| 20 | Composite Element | You gain your element's composite blast (its burn cost of 2 is reduced to 0, so it is free to use) and Blast Strike rolls twice the dice |

### Abilities in detail

#### Elemental Blast

- Activation: you use the base game's kineticist simple blast for your element.
- Attack: energy elements (fire, cold, electricity) make a ranged touch attack and are subject to spell resistance. Physical elements (air, earth, water) make a normal ranged attack and ignore spell resistance.
- Attack ability: Intelligence is used whenever your Intelligence modifier is at least as high as your Dexterity modifier. The damage bonus follows Intelligence.
- Caster level: your magus level (not kineticist level).
- Burn: your maximum burn is 0, so anything that costs burn is unavailable. You use only the zero-cost simple blast (the composite blast at 20th level is the exception, see below).
- Blast rank rises on every odd level:

| Magus level | 1-2 | 3-4 | 5-6 | 7-8 | 9-10 | 11-12 | 13-14 | 15-16 | 17-18 | 19-20 |
|---|---|---|---|---|---|---|---|---|---|---|
| Blast rank | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 |

#### Energize Weapon

- Activation: a free-action toggle. Clicking Energize Weapon on the action bar opens a menu with one entry per element you have; only one can be on at a time. With a single element it is on from the moment you gain it. It costs nothing and stays on through rest.
- Effect: while it is on, every attack with a **melee manufactured weapon** deals extra damage of that element: energy damage for energy elements, physical damage for physical elements (bludgeoning for air and water; bludgeoning, piercing and slashing for earth). It is handled as part of the weapon damage.
- Not affected: unarmed strikes, natural attacks, ranged weapons, and non-weapon attacks such as touch attacks, rays and blasts.
- Extra damage by magus level:

| Magus level | 1-6 | 7-12 | 13-18 | 19-20 |
|---|---|---|---|---|
| Extra damage | 1d6 | 2d6 | 3d6 | 4d6 |

- The extra dice are not multiplied on a critical hit (the same treatment as bonus dice from a weapon property such as flaming).
- The 20th-level composite element does not increase this damage.

#### Blast Strike

- Activation: swift action, targets yourself, costs 1 arcane pool point. It applies an effect that lasts 1 round.
- Effect: the next hit you land that round with a **melee manufactured weapon** deals a separate packet of blast damage right after the weapon damage. If you miss, the effect stays and waits for your next hit in the same round; it ends when the round ends. In a full attack only the first hit triggers it.
- Hits with unarmed strikes, natural attacks or ranged weapons do not trigger it (the effect remains).
- A Spellstrike touch spell can ride the same hit.
- Damage: a number of d6 equal to your blast rank (doubled once you have the 20th-level composite element).

| Element type | Damage bonus | Spell resistance | Damage reduction |
|---|---|---|---|
| Energy (fire, cold, electricity; negative and positive energy) | Half your Intelligence modifier (rounded toward zero) | Applies. If the target resists, no damage is dealt and the effect is used up | Normal energy resistance applies |
| Physical (air, earth, water; telekinetic, gravity, wood) | Number of Blast Strike dice + your Intelligence modifier | Does not apply | Counts as magic (bypasses DR/magic) |

- Examples: at 8th level (rank 4) with a +4 Intelligence modifier, fire deals 4d6+2 fire damage and earth deals 4d6+8 physical damage. At 20th level (rank 10, composite), fire deals 20d6 + half Intelligence and earth deals 20d6 + 20 + Intelligence.
- Critical hits: if the triggering hit is a confirmed critical, the Blast Strike damage is doubled (always x2, regardless of the weapon's critical multiplier).
- The caster level used for the spell resistance check is your magus level.
- Blast Strike damage is not weapon damage. Effects that add damage to weapon hits, and precision damage, are not applied to it a second time.

#### Blast Pool

- Grants the same resource as the arcane pool (same points, same recovery). It pays for Blast Strike and for magus arcana that spend pool points.
- It does not include the ability to add enhancement bonuses or properties to your weapon.

#### Composite Element

- At 20th level you gain the composite blast of your element. Its burn cost is reduced by 2, to 0, so you can use it freely as a ranged blast.
- Blast Strike keeps your element's damage type but rolls twice the dice (rank 10 becomes 20d6). Energize Weapon does not increase.

| Element | Composite element |
|---|---|
| Fire | Blue flame |
| Cold, water | Ice |
| Electricity, air | Thunderstorm |
| Earth | Metal |
| Telekinetic | Force |
| Gravity, negative energy | Void |
| Wood, positive energy | Verdant |

### Choices

#### Element (1st level, one, permanent)

| Element | Type | Damage of blast, Energize Weapon and Blast Strike | Spell resistance on Blast Strike | Composite at 20th |
|---|---|---|---|---|
| Fire | Energy | Fire | Applies | Blue flame |
| Cold | Energy | Cold | Applies | Ice |
| Electricity | Energy | Electricity | Applies | Thunderstorm |
| Air | Physical | Bludgeoning | Does not apply | Thunderstorm |
| Earth | Physical | Bludgeoning, piercing and slashing | Does not apply | Metal |
| Water | Physical | Bludgeoning | Does not apply | Ice |

Offered only while the KineticistElementsExpanded mod is enabled:

| Element | Type | Damage | Spell resistance on Blast Strike | Composite at 20th |
|---|---|---|---|---|
| Telekinetic | Physical | Bludgeoning, piercing and slashing | Does not apply | Force |
| Gravity | Physical | Bludgeoning | Does not apply | Void |
| Wood | Physical | Bludgeoning, piercing and slashing | Does not apply | Verdant |
| Negative Energy | Energy | Negative energy; undead may be unaffected or even healed | Applies | Void |
| Positive Energy | Energy | Positive energy; living creatures may be unaffected or even healed | Applies | Verdant |

#### Blast Magus Arcana (levels 3, 6, 9, 12, 15, 18)

- Choose one arcana from the base magus arcana list. Arcana that other mods add to the magus arcana list are offered as well.
- Not offered (they only affect the arcane weapon enhancement): Bane Blade, Devoted Blade, Enduring Blade, Ghost Blade.

### Feats

- Feats granted automatically: none.
- Feats you can no longer take:
  - Extra Arcane Pool and Abundant Arcane Pool, because they require the Arcane Pool feature.
  - Extra Arcana (a DLC feat), because it requires the base Magus Arcana selection.
- Feats added specifically for this archetype: none.

### Resources

- Blast Pool (the arcane pool resource): half magus level + Intelligence modifier, recovered the same way as the base arcane pool. Blast Strike costs 1 point.
- Elemental Blast and Energize Weapon: no cost.
- Burn: maximum 0. You cannot accept burn.

### Compatibility and notes

- **Without KineticistElementsExpanded**: only the six base-game elements are offered; everything else works the same. The same is true if the mod is installed but disabled. If the mod is enabled but its elements cannot be confirmed, the five extra elements are not offered and an error is written to the mod log.
- **Kineticist multiclass**: not supported. If one character has both Blast Magus and kineticist (or any other class or mod feature that sets up a kineticist burn pool), the Blast Magus blast features switch off: Energize Weapon adds no damage, Blast Strike is greyed out, and the reason is written to the mod log.
- **Combining with other magus archetypes** (when a mod allows multiple archetypes): archetypes that remove the same features cannot be taken together. That covers archetypes that change the arcane pool, the magus arcana selection, spell recall or the arcane weapon upgrades.
- **Mods that change the magus progression**: if a feature this archetype removes is no longer at its level in the magus progression (moved or removed by another mod), the archetype is not registered and an error is written to the mod log.
- **Energize Weapon toggle group**: it shares a toggle group with the base game's Energized Wild Shape. A character who has both will switch one off when turning the other on.
- **Characters with more than one element** (granted by other mods): Energize Weapon starts with everything off and you must pick one entry from the menu yourself. Blast Strike works only with exactly one element.
- **Prestige classes and multiclassing**: blast rank, Energize Weapon dice, blast caster level and pool points all count magus levels only. Levels in other classes add nothing.
- **DLC**: no DLC is required. The Extra Arcana feat is DLC content and is not available to this archetype.
- If another mod also changes the caster level of blasts, which value wins is not guaranteed.


</details>

## Spellfist

<details>
<summary><b>Spellfist</b></summary>

### At a glance

| Item | Details |
|---|---|
| Base class | Brawler — a class added by the external mod BrawlerClassWrath |
| Key ability | Intelligence (spellcasting, reservoir, exploit DCs). Recommended abilities are Intelligence, Strength, Dexterity |
| Spellcasting | Changed — the non-casting brawler gains an arcanist-style arcane spellbook (Intelligence, up to 9th-level spells) |
| Required mods | BrawlerClassWrath (without it this archetype does not appear) |
| Optional integrations | None |
| Alignment, deity and other restrictions | None. It cannot be combined with the brawler mod's eight other archetypes (Wild Child, Venomfist, Snakebite Striker, Steel-Breaker, Turfer, Beast Wrestler, Exemplar, Mutagenic Mauler) |

### Overview

The Spellfist is a brawler who carries arcane spells in the same body as the fists. At 1st level you pick three arcane schools, then prepare and cast only wizard spells of those schools the way an arcanist does, reaching 9th-level spells at 18th level. A single arcane reservoir lets you pull spells forward as swift actions and powers a set of arcanist exploits. From 2nd level you hold a touch spell in your hand and discharge it through your unarmed flurry. You keep the brawler's bonus combat feats and flurry, and give up maneuver training, Knockout and Awesome Blow.

### What you give up

| Level | Base feature | Notes |
|---|---|---|
| 3 | Maneuver Training (1st) | Replaced by an exploit choice |
| 4 | Knockout | No replacement |
| 7 | Maneuver Training (2nd) | Replaced by an exploit choice |
| 11 | Maneuver Training (3rd) | Replaced by an exploit choice |
| 15 | Maneuver Training (4th) | Replaced by an exploit choice |
| 16 | Awesome Blow | The price of Fist Spellstrike |
| 19 | Maneuver Training (5th) | Replaced by an exploit choice |
| 20 | Improved Awesome Blow | The price of Fist Spellstrike |

Nothing else is removed from the brawler.

### What you gain (by level)

| Level | Feature | Effect |
|---|---|---|
| 1 | Arcane School ×3 | Choose three different schools out of eight. Only wizard spells of those schools enter your spellbook. The choice is permanent. |
| 1 | Spellfist spellbook | Arcanist-style arcane spellbook. Intelligence-based, cantrips included, caster level equals brawler level. |
| 1 | Arcane Reservoir | Maximum = 3 + brawler level + Intelligence modifier. Refills fully on rest. |
| 1 | Reservoir Quicken | Toggle. In combat, spells are cast as swift actions; each successful cast drains points equal to the spell level (minimum 1). |
| 2 | Fist Spellstrike | Hold a touch spell from your Spellfist spellbook and deliver it with an unarmed strike (flurry included) or a melee weapon attack. |
| 3 | Spellfist Exploits | Choose one exploit (1st) |
| 4 | — | 2nd-level spells |
| 6 | — | 3rd-level spells |
| 7 | Spellfist Exploits | Choose one exploit (2nd) |
| 8 | — | 4th-level spells |
| 10 | — | 5th-level spells |
| 11 | Spellfist Exploits | Choose one exploit (3rd) |
| 11 | Greater Exploits | Greater Spell Resistance becomes available |
| 12 | — | 6th-level spells |
| 14 | — | 7th-level spells |
| 15 | Spellfist Exploits | Choose one exploit (4th) |
| 16 | — | 8th-level spells |
| 18 | — | 9th-level spells |
| 19 | Spellfist Exploits | Choose one exploit (5th) |

### Abilities in detail

#### Spellfist spellbook

- It works like the base game's arcanist spellbook: you prepare spells each day and then spend spell slots freely among the spells you prepared. Spell slots per day and the number of spells you can prepare follow the arcanist tables for the same level.
- It is an arcane spellbook, the casting ability is Intelligence, and your caster level equals your brawler level.
- You do not pick spells known. You automatically know every wizard spell of your three schools, up to the highest spell level you can cast. When a new spell level opens, all spells of that level from your three schools arrive at once.
- There are no spell picks on level-up and you cannot copy scrolls into this spellbook. Spells outside your three schools, and universal spells, never enter it.
- The archetype grants nothing that removes arcane spell failure from armor.

When spell levels open:

| Brawler level | 1–3 | 4–5 | 6–7 | 8–9 | 10–11 | 12–13 | 14–15 | 16–17 | 18–20 |
|---|---|---|---|---|---|---|---|---|---|
| Highest spell level | 1st | 2nd | 3rd | 4th | 5th | 6th | 7th | 8th | 9th |

Cantrips are available from 1st level, but only if one of your schools has any (see the table under Choices).

#### Arcane Reservoir

- A resource you gain automatically. Maximum = 3 + brawler level + Intelligence modifier.
- Examples: 7 points at level 1 with a +3 modifier, 18 at level 10 with +5, 31 at level 20 with +8.
- It refills to the maximum when you rest, and also when you level up.
- It pays for two things: Reservoir Quicken and exploits.

#### Reservoir Quicken

- A toggle. Switching it costs no action, it starts off, and it stays on through a rest.
- While it is on and you are in combat, any spell whose level is no higher than your remaining reservoir points (9th at most) is cast as a swift action. With 4 points left, for example, spells up to 4th level are quickened.
- Each successful cast drains points equal to the spell level. A cantrip costs 1 point; a 9th-level spell costs 9. A failed cast drains nothing.
- You have one swift action per round, so a second spell in the same round is cast with its normal action and costs no reservoir points. The same happens when the reservoir holds fewer points than the spell's level.
- It does nothing out of combat: spells use their normal action and the reservoir is untouched.
- Never charged: spells that are swift actions to begin with, spells prepared with the Quicken Spell metamagic, and spells quickened by another source such as a metamagic rod.
- Turning the toggle off cancels any reservoir-quickened cast that has not started yet.
- Only spells from the Spellfist spellbook drain the reservoir. See Compatibility and notes for multiclass characters.

#### Fist Spellstrike

- Gained automatically at 2nd level, together with the magus Spellstrike toggle. Touch spells are delivered through your unarmed strikes whether the toggle is on or off; the toggle only decides whether they are also delivered through melee weapon attacks.
- When you cast a touch spell from your Spellfist spellbook you hold the charge. If an unarmed strike (flurry included) or a melee weapon attack then hits, the spell discharges on that target.
- You hold one charge at a time. There is no once-per-round limit: cast one touch spell as a quickened swift action and another with a standard action, and you can discharge twice in a round.
- If the attack misses, the charge stays and rides on your next attack that hits.
- There is no extra cost beyond the spell slot (and the reservoir points if you quickened the spell).
- It pairs naturally with Reservoir Quicken: take the touch spell as a swift action, then deliver it with your flurry in the same round.

#### Spellfist Exploits

- You choose one at levels 3, 7, 11, 15 and 19, five in total. Each exploit can be chosen only once.
- They do what the base game's arcanist exploits do, with different scaling:
  - damage and effect rank use brawler level instead of arcanist level;
  - saving throw DCs use Intelligence instead of Charisma;
  - ranges and durations that depend on caster level use character level.
- Their cost is paid from the arcane reservoir.
- The full list is in the exploit table under Choices.

### Choices

#### Arcane School (level 1, choose three)

You make the same choice three times at 1st level and cannot pick a school twice. The universal school is not offered. The numbers below are for the base game's wizard spell list with no other mods, and show how many spells each school brings at each spell level.

| School | 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | Total |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Abjuration | 1 | 4 | 3 | 5 | 4 | 4 | 2 | 2 | 2 | 2 | 29 |
| Conjuration | 2 | 5 | 7 | 4 | 5 | 5 | 4 | 4 | 4 | 3 | 43 |
| Divination | 0 | 1 | 2 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 10 |
| Enchantment | 1 | 2 | 2 | 4 | 3 | 5 | 3 | 4 | 2 | 5 | 31 |
| Evocation | 3 | 5 | 4 | 5 | 5 | 3 | 5 | 4 | 4 | 3 | 41 |
| Illusion | 0 | 2 | 4 | 1 | 4 | 2 | 1 | 2 | 2 | 2 | 20 |
| Necromancy | 2 | 3 | 8 | 3 | 7 | 3 | 5 | 6 | 5 | 2 | 44 |
| Transmutation | 1 | 7 | 7 | 7 | 6 | 8 | 13 | 7 | 4 | 2 | 62 |

- Divination and illusion have no cantrips in this game, so those two schools grant none.
- Universal spells (one cantrip in the base game) belong to no school and cannot be learned.
- The choice cannot be undone.

#### Exploits (levels 3, 7, 11, 15, 19 — choose five)

| Exploit | Cost | Effect | Requirements |
|---|---|---|---|
| Dimensional Slide | 1 reservoir point | Teleport a short distance as a move action. The distance grows with level. | — |
| Arcane Barrier | 1 reservoir point | As a swift action, wrap yourself in a barrier that absorbs damage. The amount grows with level, and using it again while a barrier remains costs more. | — |
| Energy Shield | 1 reservoir point | Gain resistance to acid, cold, electricity or fire (your choice). The resistance grows with level. | — |
| Force Strike | 1 reservoir point | Fire a missile of force. Like magic missile it always hits one foe, and its damage grows with level. | — |
| Spell Resistance | 1 reservoir point | Gain spell resistance equal to 6 + your level for a number of rounds. | — |
| Greater Spell Resistance | — | Whenever you use the Spell Resistance exploit, the spell resistance is 11 + your level instead. | Level 11, Spell Resistance exploit |
| Shadow Veil | 1 reservoir point | Wrap yourself in shadows for a number of rounds, gaining a bonus on Stealth. | — |
| Wooden Flesh | 1 reservoir point | Gain a natural armor bonus for a number of rounds. | — |
| Armored Mask | 1 reservoir point | Don a mask of force that grants a bonus to AC. | — |
| Ice Missile | 1 reservoir point | Ranged touch attack dealing cold damage. The damage dice grow with level, and a target that fails its save is staggered. | — |
| Lightning Lance | 1 reservoir point | Ranged touch attack dealing electricity damage. The damage dice grow with level, and a target that fails its save is blinded. | — |

These eleven are the whole list. Other arcanist exploits (the ones that convert spell slots into reservoir points, Potent Magic and so on) are not offered.

### Feats

- No feats are granted automatically.
- The brawler's bonus combat feat slots are unchanged.
- The maneuver training slots (levels 3, 7, 11, 15, 19) become exploit choices.
- Spellfist exploits exist only in this archetype's own selection. They do not show up in the base game's extra-exploit feat lists.
- No feats are added specifically for this archetype.

### Resources

| Resource | Maximum | Recovery | Spent on |
|---|---|---|---|
| Arcane Reservoir | 3 + brawler level + Intelligence modifier | Fully on rest, fully on level-up | Reservoir Quicken (spell level, minimum 1), exploits (1 point for most) |
| Spell slots | Arcanist table for the same level | Rest | Casting spells |

The reservoir is the same resource as the base game arcanist's arcane reservoir. Unlike the arcanist's, it refills completely on rest rather than halfway.

### Compatibility and notes

- **Without BrawlerClassWrath**: the game starts normally and only this archetype is left unregistered (it does not appear at character creation). One error line is written to the mod log.
- **Other mods that add brawler archetypes**: this archetype knows, and blocks combination with, only the brawler mod's eight original archetypes. If the brawler class has any other archetype added to it, this archetype is not registered.
- **Mods that change the wizard spell list**: spells added to the wizard list by a mod that loads before this one are sorted into the Spellfist school lists by their school (universal spells are skipped). If a mod removes a base-game wizard spell from the list or moves it to another level or school, this archetype is not registered.
- **DLC spells**: DLC spells on the wizard list are included in the school lists. Without the DLC the game hides them, exactly as it does for a wizard.
- **Multiclassing and Reservoir Quicken**: the quicken effect applies to the caster as a whole. A multiclass character with another spellbook gets that book's spells quickened too while the toggle is on, and those casts do not drain the reservoir. The drain and the spell-level cap line up exactly only on a single-class character.
- **Magus multiclass**: not supported. With magus levels, Fist Spellstrike is not attached to the Spellfist spellbook and the magus's own Spellstrike is used as is.
- **Other mods that alter Spellstrike**: when another mod has already allowed a spell for Spellstrike, that result is kept. This mod only adds spells from the Spellfist spellbook on top.
- **Arcanist and Exploiter Wizard**: the base game's exploits are left untouched. Spellfist exploits are separate copies, so they do not satisfy prerequisites of other options that require the original exploits (for example the higher exploits that build on Ice Missile or Lightning Lance).
- **Known limitation**: if combat ends, or the reservoir drops and lowers the cap, before you turn the toggle off, a queued quickened cast may stay queued instead of being cancelled.


</details>

## Golemancer

<details>
<summary><b>Golemancer</b></summary>

### At a glance

| Item | Details |
|---|---|
| Base class | Alchemist (archetype) |
| Key ability | Intelligence (same as the base alchemist) |
| Spellcasting | Changed — the extract list and slots per day are untouched, but extracts from the three opposition schools you pick at level 1 take two slots to prepare |
| Required mods | None |
| Optional integrations | None |
| Alignment / deity restrictions | None. Cannot be combined with anything that grants an animal companion (the golem uses the animal companion slot) |

### Overview

The Golemancer is an alchemist who builds a golem and sends it to the front. From level 1 a wood golem fights at your side, and enemies near it take a penalty on attacks against anyone but the golem. At levels 5, 10 and 15 you pick a new material, which replaces the golem's look, size, immunities and special abilities wholesale. From level 10 you can wear the golem as a swift action and tank in person, with the golem's hit points soaking damage before yours. You pay for it with fewer bombs per day, three opposition schools, and the loss of poison resistance, poison immunity and the cognatogens.

### What you give up

| Level | Base class feature | Notes |
|---|---|---|
| 1 | Bombs (uses per day) | Reduced, not removed. The base of your daily bombs (alchemist level + Intelligence modifier) is halved, rounded down. Bomb damage dice are unchanged |
| 1 | Extracts (three schools) | Three of the seven schools other than transmutation become opposition schools; their extracts take two slots to prepare |
| 2 | Poison Resistance | Removed |
| 5 | Poison Resistance (second step) | Removed |
| 8 | Poison Resistance (third step) | Removed |
| 10 | Poison Immunity | Removed |
| Every discovery pick | Cognatogen discovery | Cannot be selected. Greater Cognatogen and Grand Cognatogen require it, so they are locked out too. The Extra Discovery feat cannot pick it either |

Bomb damage dice, mutagens (including Persistent Mutagen), the extract slot table, discoveries and grand discovery, and every other alchemist feature are unchanged.

### What you gain (by level)

| Level | Feature | Effect |
|---|---|---|
| 1 | Golemancer Opposition School ×3 | Pick three of abjuration, conjuration, divination, enchantment, evocation, illusion and necromancy (each school once). Extracts of those schools take two slots to prepare |
| 1 | Halved Bombs | Bombs per day = (alchemist level + Intelligence modifier) ÷ 2, rounded down (minimum 0), plus any extra bombs from feats, equipment and mythic sources |
| 1 | Golem | A wood golem companion. Hit Dice = alchemist level; construct. 10-ft taunting aura (−2 on attack rolls against targets other than the golem). Broken at 0 hit points, fully repaired by a successful rest |
| 1 | Golem Material — Wood | The golem's material for levels 1–4. Medium, one slam |
| 1 | Emergency Repair | Standard action, spends one prepared extract. The golem regains max HP × (extract level × 10)% and is no longer broken |
| 5 | Golem Material | Replace the material with stone, iron or brass |
| 10 | Golem Material | Replace the material with gold, autumn or summer |
| 10 | Golem Fusion | Swift-action toggle. Wear the golem: gain its shape, size, two slams, natural armor, DR and Strength, and take damage on the golem's hit points first |
| 15 | Golem Material | Replace the material with adamantine, greater gold or mithral |
| 20 | Shared Immunity | While fused, you also gain the current material's additional immunities (not the common construct immunities) |

### Abilities in detail

#### Golem

- **Gained**: level 1, automatically. The golem is a companion unit that occupies the animal companion slot and levels up with you (Hit Dice = alchemist level).
- **Type**: construct. It has the common construct immunities (sleep, paralysis, fear, mind-affecting effects, poison and so on). Alchemical healing does not mend it. It does not have the base game wood golem's blanket spell immunity.
- **Chassis**: d10 Hit Dice and full base attack bonus, like base game constructs. The golem does not pick feats.
- **Attacks**: one slam (two slams for you while fused).
- **Taunting aura — Golem's Taunt**: enemies within 10 feet of the golem take a −2 penalty on attack rolls against targets other than the golem. Attacks against the golem itself are unpenalized. While fused, the aura moves to you.
- **Broken**: a golem reduced to 0 hit points is broken. It stays down and cannot be fused with. Only two things bring it back: a successful rest (revived at full hit points) or Emergency Repair. An interrupted rest leaves it broken.

**Golem baseline table (before material ratios)**

The golem's natural armor, DR, slam and Strength are the baseline below multiplied by a ratio that depends on the material. Hit points and Hit Dice have no ratio.

| Level | Natural armor | DR | Slam | Str | Level | Natural armor | DR | Slam | Str |
|---|---|---|---|---|---|---|---|---|---|
| 1 | 6 | 5 | 2d6 | 18 | 11 | 18 | 12 | 2d10 | 30 |
| 2 | 7 | 5 | 2d6 | 19 | 12 | 19 | 13 | 2d10 | 31 |
| 3 | 8 | 5 | 2d6 | 20 | 13 | 20 | 14 | 2d10 | 32 |
| 4 | 9 | 6 | 2d6 | 21 | 14 | 21 | 14 | 2d10 | 33 |
| 5 | 11 | 8 | 2d8 | 23 | 15 | 24 | 15 | 3d10 | 36 |
| 6 | 12 | 8 | 2d8 | 24 | 16 | 25 | 15 | 3d10 | 37 |
| 7 | 13 | 9 | 2d8 | 25 | 17 | 26 | 15 | 3d10 | 38 |
| 8 | 14 | 10 | 2d8 | 26 | 18 | 27 | 15 | 3d10 | 39 |
| 9 | 15 | 10 | 2d8 | 27 | 19 | 28 | 15 | 3d10 | 40 |
| 10 | 17 | 12 | 2d10 | 29 | 20 | 30 | 15 | 4d8 | 42 |

- Value = baseline × material ratio, rounded. The slam becomes the dice expression (1–6 dice of d6, d8 or d10) whose average is closest to the baseline average × ratio.
- Golem hit points = 5 × level + (level ÷ 2, rounded up) + size bonus (Medium 20, Large 30, Huge 40).

**Actual values by material** (natural armor / DR / slam / Strength)

Levels 1–4 — wood (Medium, DR/adamantine):

| Level | Wood | HP |
|---|---|---|
| 1 | 6 / 5 / 2d6 / 18 | 26 |
| 2 | 7 / 5 / 2d6 / 19 | 31 |
| 3 | 8 / 5 / 2d6 / 20 | 37 |
| 4 | 9 / 6 / 2d6 / 21 | 42 |

Levels 5–9 — all Large:

| Level | Stone | Iron | Brass | HP |
|---|---|---|---|---|
| 5 | 10 / 6 / 2d8 / 21 | 11 / 9 / 2d8 / 24 | 12 / 9 / 2d8 / 24 | 58 |
| 6 | 11 / 6 / 2d8 / 22 | 12 / 9 / 2d8 / 25 | 13 / 9 / 2d8 / 25 | 63 |
| 7 | 12 / 7 / 2d8 / 23 | 13 / 10 / 2d8 / 26 | 14 / 10 / 2d8 / 26 | 69 |
| 8 | 13 / 8 / 2d8 / 24 | 14 / 11 / 2d8 / 27 | 15 / 11 / 2d8 / 27 | 74 |
| 9 | 14 / 8 / 2d8 / 25 | 15 / 11 / 2d8 / 28 | 17 / 11 / 2d8 / 28 | 80 |

Levels 10–14 — all Large (autumn and summer share the same numbers):

| Level | Gold | Autumn / Summer | HP |
|---|---|---|---|
| 10 | 15 / 15 / 4d6 / 33 | 18 / 10 / 2d8 / 27 | 85 |
| 11 | 16 / 15 / 4d6 / 34 | 19 / 10 / 2d8 / 28 | 91 |
| 12 | 17 / 17 / 4d6 / 35 | 20 / 11 / 2d8 / 29 | 96 |
| 13 | 18 / 18 / 4d6 / 36 | 21 / 12 / 2d8 / 30 | 102 |
| 14 | 19 / 18 / 4d6 / 37 | 22 / 12 / 2d8 / 31 | 107 |

Levels 15–20 — adamantine is Huge, the others Large:

| Level | Adamantine | Greater Gold | Mithral | HP (adamantine / others) |
|---|---|---|---|---|
| 15 | 22 / 15 / 4d6 / 34 | 19 / 15 / 3d10 / 36 | 31 / 15 / 4d8 / 38 | 123 / 113 |
| 16 | 23 / 15 / 4d6 / 35 | 20 / 15 / 3d10 / 37 | 32 / 15 / 4d8 / 39 | 128 / 118 |
| 17 | 24 / 15 / 4d6 / 36 | 21 / 15 / 3d10 / 38 | 34 / 15 / 4d8 / 40 | 134 / 124 |
| 18 | 25 / 15 / 4d6 / 37 | 21 / 15 / 3d10 / 39 | 35 / 15 / 4d8 / 41 | 139 / 129 |
| 19 | 26 / 15 / 4d6 / 38 | 22 / 15 / 3d10 / 40 | 36 / 15 / 4d8 / 42 | 145 / 135 |
| 20 | 28 / 15 / 3d10 / 40 | 24 / 15 / 5d6 / 42 | 39 / 15 / 6d6 / 44 | 150 / 140 |

#### Golem Fusion

- **Activation**: level 10, swift-action toggle. It can be turned on outside combat.
- **Effect**: the golem disappears and you polymorph into the shape and size of your golem's current material.
  - Two slams, using the golem's slam dice for its current material and level.
  - You gain the golem's natural armor and DR from the tables above (the DR is bypassed by the same material as the golem's).
  - You gain a Strength bonus equal to the golem's Strength modifier. Examples: gold at level 10 (Str 33) gives +12 Strength; mithral at level 20 (Str 44) gives +17.
  - The taunting aura moves to you.
- **Shared hit points**: damage you take while fused is applied to the golem's hit points first. When they run out the fusion ends, the golem is broken, and the remaining damage hits you. If you end the fusion yourself, the golem returns with whatever hit points it has left.
- **Unavailable while fused**: spells, extracts, bombs and mutagens. A mutagen you drank before fusing keeps working. Emergency Repair can still be used.
- **Cannot be turned on** while the golem is broken or missing, while the game is saving, or while you are already polymorphed by something else.
- **Ends when** you turn it off, rest, save (including autosaves), change area, are polymorphed by an outside effect, die, or the golem runs out of hit points.
- **Does not end when** you are stunned, paralyzed, dying or unconscious, or when combat ends.
- If you level up or change material while fused, your form and numbers are updated to match.

#### Emergency Repair

- **Activation**: from level 1, standard action. It has six variants, one per extract level (1st–6th).
- **Cost**: one prepared, unspent extract of that level. A two-slot opposition-school extract counts as one.
- **Effect**: the golem regains max HP × (extract level × 10)%, rounded down, and is no longer broken. A broken golem returns to life with that many hit points.

| Extract level | 1st | 2nd | 3rd | 4th | 5th | 6th |
|---|---|---|---|---|---|---|
| Healing (of max HP) | 10% | 20% | 30% | 40% | 50% | 60% |

- **Requirements**: the golem must be adjacent (5 feet). While fused, it repairs the worn golem regardless of distance. It cannot be used when there is nothing to repair (full hit points and not broken) or when you have no prepared extract of that level.

#### Halved Bombs

- Bombs per day = (alchemist level + Intelligence modifier) ÷ 2, rounded down, minimum 0. Extra bombs from feats, equipment and mythic sources are added on top, unchanged.
- Temporary Intelligence bonuses do not count toward the base, as in the base game.
- Examples: level 4 with Intelligence 18 gives 4 bombs (8 for a base alchemist). Level 1 with Intelligence 10–11 gives 0; Intelligence 12 gives 1.

#### Golemancer Opposition School

- Chosen three times at level 1 from abjuration, conjuration, divination, enchantment, evocation, illusion and necromancy (transmutation is not an option). Each school can be picked once.
- Extracts of a chosen school take two slots to prepare. They are not forbidden.

#### Shared Immunity

- Level 20, automatic. Only while fused, you share the current material's additional immunities:
  - Adamantine: immunity to critical hits and to precision damage
  - Greater Gold: immunity to insanity (confusion) and petrification
  - Mithral: immunity to electricity, evasion
- The common construct immunities (sleep, paralysis, fear, mind-affecting, poison and so on) are not shared. Immunities you already have are left alone and are not removed when the fusion ends.

### Choices

#### Golem Material

At each of levels 5, 10 and 15 you choose one of three materials. The new material replaces the old one: the golem's look, size, immunities, special abilities and numbers all change. The ratios multiply the baseline table (natural armor / DR / slam / Strength).

| Level | Material | Size | DR bypassed by | Ratios | Additional immunities | Special abilities |
|---|---|---|---|---|---|---|
| 1 | Wood | Medium | Adamantine | 1.00 / 1.00 / 1.00 / 1.00 | — | The base game wood golem's splintering ability (save DC based on the golem's level) |
| 5 | Stone | Large | Adamantine | 0.90 / 0.75 / 0.97 / 0.91 | Petrification, acid | Slowing Aura: enemies entering within 10 feet are slowed for 7 rounds on a failed Will save |
| 5 | Iron | Large | Adamantine | 1.00 / 1.12 / 0.97 / 1.04 | Electricity | Poison breath (the base game iron golem's breath weapon). Fire Repair: takes no fire damage and heals one third of the damage it would have taken |
| 5 | Brass | Large | Adamantine | 1.10 / 1.12 / 1.06 / 1.04 | Fire | Incendiary cloud breath (the base game brass golem's breath weapon), see invisibility |
| 10 | Gold | Large | Adamantine | 0.89 / 1.29 / 1.35 / 1.13 | Blindness, insanity | Prismatic surge (melee) |
| 10 | Autumn | Large | Nothing (DR/—) | 1.05 / 0.86 / 0.82 / 0.93 | Cold, freedom of movement | Despair aura, entangling grab — save DCs based on the golem's level |
| 10 | Summer | Large | Nothing (DR/—) | 1.05 / 0.86 / 0.82 / 0.93 | Fire, freedom of movement | Fatigue aura, entangling grab — save DCs based on the golem's level |
| 15 | Adamantine | Huge | Nothing (DR/—) | 0.93 / 1.00 / 0.88 / 0.95 | Critical hits, precision damage | Fast healing 5, Improved Critical (slam) |
| 15 | Greater Gold | Large | Adamantine | 0.79 / 1.00 / 0.96 / 1.00 | Insanity, petrification | Prismatic surge (ranged), Cleave |
| 15 | Mithral | Large | Nothing (DR/—) | 1.29 / 1.00 / 1.17 / 1.05 | Electricity, evasion | Combat Reflexes, fast healing 20, speed 50 feet (+20 feet) |

- The breath weapons, prismatic surges, auras and grab are the corresponding base game golems' abilities. The wood golem's splintering and the autumn and summer auras and grab have been adjusted so their save DCs scale with your golem's level.
- While the golem is broken it loses what the material granted through ongoing effects (fast healing, freedom of movement, see invisibility, auras); they return when it is repaired.

#### Opposition schools

Three of Abjuration, Conjuration, Divination, Enchantment, Evocation, Illusion and Necromancy. Every option has the same effect: extracts of that school take two slots to prepare.

Opposition schools apply only to your Golemancer extracts. They do not affect a spellbook gained from another class through multiclassing, your bombs, mutagens or discoveries, or the use of scrolls, wands and potions. You can still use opposition-school extracts; they just take two slots to prepare.

### Feats

- Feats granted automatically: none. Bonus feat slots are unchanged.
- Options you can no longer take: the Cognatogen discovery, and with it Greater Cognatogen and Grand Cognatogen, which require it. This applies to discovery selections and to the Extra Discovery feat.
- Feats added specifically for this archetype: none.
- The golem does not choose feats. Depending on its material it gains Improved Critical (slam), Cleave or Combat Reflexes.

### Resources

- **Bombs**: (alchemist level + Intelligence modifier) ÷ 2, rounded down, plus extra bombs from other sources, per day. Restored by resting, as in the base game.
- **Emergency Repair**: no pool of its own. It spends prepared extracts of 1st–6th level, one per use.
- **Golem Fusion**: no uses per day and no duration. The practical resource is the golem's hit points.
- **Golem hit points**: fully restored by a successful rest, which also revives a broken golem. Some materials add fast healing (adamantine 5, mithral 20), and the iron golem heals from fire damage.

### Compatibility and notes

- **No animal companions**: the golem uses the animal companion slot. Combining it with a class or feature that grants an animal companion (druid, hunter and so on) leaves the golem non-functional and logs a conflict error in the mod log.
- **DLC content**: the brass material uses the brass golem unit and model added by DLC 3 (The Treasure of the Midnight Isles). All other materials (wood, stone, iron, gold, autumn, summer, adamantine, greater gold, mithral) use base game golems. The cognatogen lockout also covers the Extra Discovery feat selection that ships with DLC 5 (The Lord of Nothing) content.
- **Other mods**: if another mod moves or removes Poison Resistance at alchemist levels 2, 5 or 8 or Poison Immunity at level 10, this archetype is not created at all, rather than being created half-working. The golem's level is computed directly from your alchemist level, so mods that change animal companion levels do not affect it.
- **Multiclassing and prestige classes**: the golem's Hit Dice and numbers, the bomb base and the fusion numbers all read alchemist level only. Levels in other classes do not advance them.
- **Known limitations**
  - At level 1 with Intelligence 10–11 you may have 0 bombs per day.
  - Opposition schools cannot be undone once chosen (a full respec, which rebuilds the character, is the exception).
  - The fusion ends whenever a save starts, including autosaves on area transitions and at the start of dialogues. Hit points, material and broken state are kept; you have to fuse again after loading.
  - Respeccing while fused is not supported.
  - Damage soaked by the golem is measured before the difficulty's damage multiplier is applied. On difficulties where that multiplier is not 1, the total taken by you and the golem differs from what you would have taken unfused.
  - When combined with other mods' damage absorption or damage cap effects, the golem may take more than its share.
  - If the golem is removed entirely by something outside the mod, it does not reappear until the next area load or level-up.
  - If the fusion ends while you are dominated or charmed, the golem may not return right away.


</details>

## Warlock

<details>
<summary><b>Warlock</b></summary>

### At a glance

| Item | Details |
|---|---|
| Base class | Witch archetype |
| Key ability | Charisma (spellcasting, blast attack rolls, blast damage bonus) |
| Spellcasting | Changed — uses the Stigmatized Witch spellbook as is: spontaneous, Charisma-based casting, with Charisma-based cantrips |
| Required mods | None |
| Optional integrations | TabletopTweaks-Base, PrestigePlus, ExpandedContent, CharacterOptionsPlus (none required; see "Compatibility and notes") |
| Alignment / deity restrictions | None. Cannot be combined with the Stigmatized Witch archetype |

### Overview

The Warlock gives up every hex in exchange for an at-will elemental ray, the blast, shaped by her patron. The patron you pick at level 1 places you in one of six groups (Hell, Abyss, Fey, Storm, Death, Star), and the group decides your blast element, the creatures you can call and your defensive ward. Every blast that hits builds 1 Heat (up to 9), and Heat is spent as a swift action to summon the patron's servants. When Heat reaches 9, the strongest creature you can call appears on its own. You cast spontaneously with Charisma like a Stigmatized Witch, and you keep your patron spells and familiar.

### What you give up

| Level | Base game feature | Notes |
|---|---|---|
| 1 | Hex selection | Removed |
| 1 | Witch cantrips (Intelligence-based) | Replaced by Charisma-based cantrips |
| 1 | Improved hex progression (the hidden progression that unlocks major and grand hexes) | Removed |
| 2, 4, 6, 8, 10, 12, 14, 16, 18, 20 | Hex selection | Every hex selection is removed (11 in total, counting level 1) |
| 20 | Any other level 20 selection that can grant a hex | If another mod adds a hex-granting selection at level 20, it is removed too |
| All levels | Witch spellbook (Intelligence-based, prepared) | Replaced by the Stigmatized Witch spellbook (Charisma-based, spontaneous) |

Kept: patron selection (including patron spells), familiar, simple weapon proficiency.

### What you gain (by level)

| Level | Feature | Effect |
|---|---|---|
| 1 | Warlock Pact | Your patron decides your warlock group; the pact grants that group's blast and Heat summoning and keeps the group's abilities matched to your level |
| 1 | Warlock Group | One of Hell, Abyss, Fey, Storm, Death or Star. Decides your blast element, summon table and ward |
| 1 | Blast | Standard action, close-range ranged touch ray (Charisma to hit). 1d6 + half your Charisma modifier, +1d6 at every odd level from 3rd. Usable at will |
| 1 | Heat | +1 each time a blast hits (maximum 9). Drops to 0 when combat ends or after a full rest |
| 1 | Heat Summoning I–IX | Swift action. Spend N Heat to summon one tier N creature of your group (N cannot exceed your highest spell level). Lasts 1 round per witch level. At 9 Heat the highest tier is summoned automatically, for free |
| 1 | Charisma cantrips | The Stigmatized Witch's Charisma-based cantrips |
| 2 | Group Ward I | Resistance 5 to your group's element (Star: +1 deflection bonus to AC) |
| 4 | Warlock Augment Summoning | Grants the Augment Summoning feat — your summons gain +4 Strength and Constitution |
| 6 | Blast Reach | Your blast reaches medium range |
| 8 | Group Ward II | Resistance 10 to your group's element (Star: +2 deflection bonus to AC) |
| 10 | Double Blast | Two rays at the same target in one standard action (medium range). Each ray takes −2 to hit, deals blast damage separately, and raises Heat by 1 on a hit |
| 12 | Heat Acceleration | A confirmed critical blast hit raises Heat by 2 |
| 14 | Group Ward III | Resistance 20 to your group's element (Star: +3 deflection bonus to AC) |
| 16 | Extended Summons | Heat summons last twice as long (2 rounds per witch level) |
| 18 | Group Ward IV | Immunity to your group's element (Star: +3 deflection bonus to AC and spell resistance 10 + witch level) |
| 20 | Grand Summoner | Tier IX Heat summons remain until combat ends |

### Abilities in detail

#### Warlock Pact and groups

The patron you choose at level 1 decides your group (see the table under "Choices"). The group follows the first patron you picked and does not change if you later gain another patron from some other source. The pact re-aligns your group abilities at every level-up: at level 6 the close-range blast is swapped for the medium-range one, at level 10 Double Blast is added, and you only ever hold the ward of your current tier.

A warlock whose only patron is not on the table (the Dark Sister's Dark Pact, or a patron added by another mod) receives no group, and therefore no blast, Heat summoning or ward.

#### Blast

- Use: standard action, supernatural ability. A ranged touch ray against one enemy.
- Attack roll: uses your Charisma modifier instead of Dexterity.
- Range: close; medium from level 6.
- Damage: (1 + (witch level − 1) / 2)d6 of your group's element + half your Charisma modifier (rounded down).
- Spell resistance applies.
- No resource cost.

| Witch level | 1 | 3 | 5 | 7 | 9 | 11 | 13 | 15 | 17 | 19 |
|---|---|---|---|---|---|---|---|---|---|---|
| Dice | 1d6 | 2d6 | 3d6 | 4d6 | 5d6 | 6d6 | 7d6 | 8d6 | 9d6 | 10d6 |

#### Double Blast

Level 10. One standard action fires two rays at the same target (medium range). Each ray takes a −2 penalty on its attack roll, and each ray that hits deals full blast damage on its own. Each ray that hits raises Heat.

#### Heat

- +1 for every blast or Double Blast ray that hits. From level 12, a confirmed critical hit gives +2.
- Only the hit matters. Heat rises even if resistance or immunity reduces the damage to 0, or if spell resistance stops the blast.
- Weapon attacks, spells and your summons' attacks do not build Heat.
- Maximum 9. At 0 Heat there is no buff; from 1 upward the buff icon shows the current value.
- Heat drops to 0 when combat ends (when the warlock leaves combat — in real-time mode this includes falling unconscious) and after a completed rest (an interrupted rest changes nothing).

#### Heat Summoning

- Use: swift action, supernatural ability, close range, targeted at a point. Each group has its own "<Group> Heat Summoning" ability with nine variants, I to IX.
- Requirements: Heat at least equal to the tier, and the tier no higher than the highest spell level of your warlock spellbook (for example up to tier III at witch level 6, and tier IX from level 18). Otherwise the variant is unavailable.
- Cost: Heat equal to the tier.
- Effect: summons one creature of that tier. Any warlock summon you already have vanishes (one at a time per warlock; other warlocks' summons and ordinary summoning spells are unaffected).
- Duration: 1 round per witch level; doubled from level 16. From level 20, a tier IX summon stays until combat ends and then vanishes.
- Patron's Servant: warlock summons cannot be directly controlled. They fight on your side on their own.
- From level 4 they benefit from Augment Summoning (+4 Strength and Constitution).
- Patron's Favor: the tier IX summons of the Hell, Storm and Death groups gain a further +4 Strength and Constitution (stacks with other bonuses).

#### Automatic summon

When a blast hit brings Heat to 9, the highest tier you can summon (the lower of 9 and your highest spell level) is summoned at once without spending Heat, and Heat resets to 0. Anything above 9 is lost. The automatic summon also replaces your current warlock summon. If you reach 9 again while an automatic summon is still resolving, Heat stays at 9 and the summon happens on your next blast hit.

#### Group Ward

| Tier | Level | Hell, Abyss, Fey, Storm, Death | Star |
|---|---|---|---|
| I | 2 | Resistance 5 to the group's element | +1 deflection bonus to AC |
| II | 8 | Resistance 10 to the group's element | +2 deflection bonus to AC |
| III | 14 | Resistance 20 to the group's element | +3 deflection bonus to AC |
| IV | 18 | Immunity to the group's element | +3 deflection bonus to AC, spell resistance 10 + witch level |

### Choices

#### Patron → group

| Group | Blast element | Patrons |
|---|---|---|
| Hell | Fire | Deception, Devotion |
| Abyss | Acid | Insanity, Strength |
| Fey | Cold | Winter, Transformation |
| Storm | Electricity | Elements, Agility, Endurance |
| Death | Negative energy | Shadow, Ancestors |
| Star | Force | Healing, Mercy |

#### Blast element versus demons

| Group | Element | Against demons |
|---|---|---|
| Hell | Fire | Demons resist fire 10 — damage is reduced |
| Abyss | Acid | Demons resist acid 10 — damage is reduced |
| Fey | Cold | Demons resist cold 10 — damage is reduced |
| Storm | Electricity | Demons are immune to electricity — the blast deals 0 damage |
| Death | Negative energy | Demons have no negative energy resistance |
| Star | Force | Force damage is not reduced by energy resistance |

In every case a hit still raises Heat.

#### Heat summon table by group

"+ Fiendish I/II" and "+ Celestial I/II" are the fiendish and celestial templates used by the base game's Summon Monster spells.

| Tier | Hell | Abyss | Fey | Storm | Death | Star |
|---|---|---|---|---|---|---|
| I | Dog + Fiendish I | Dog + Fiendish I | Mite | Small Air Elemental | Dog + Fiendish I | Dog + Celestial I |
| II | Wolf + Fiendish I | Wolf + Fiendish I | Small Water Elemental | Small Water Elemental | Wolf + Fiendish I | Wolf + Celestial I |
| III | Hell Hound | Abrikandilu (DLC 5 unit) | Medium Water Elemental | Medium Air Elemental | Skeletal Champion | Monitor Lizard + Celestial I |
| IV | Dire Wolf + Fiendish II | Dire Wolf + Fiendish II | Medium Water Elemental | Large Air Elemental | Skeleton Abomination | Dire Wolf + Celestial II |
| V | Manticore | Babau | Redcap | Large Water Elemental | Spectre | Bralani |
| VI | Huge Fire Elemental | Vrock | Will-o'-Wisp | Huge Air Elemental | Fallen | Lillend |
| VII | Greater Fire Elemental | Gibrileth | Bogeyman | Greater Air Elemental | Greater Shadow | Movanic Deva |
| VIII | Elder Fire Elemental | Nabasu | Nereid | Elder Air Elemental | Graveknight | Monadic Deva |
| IX | Elder Fire Elemental + Patron's Favor | Oolioddroo | Wild Hunt Archer | Elder Water Elemental + Patron's Favor | Graveknight + Patron's Favor | Ghaele |

Cells that use a DLC unit: only Abyss III (Abrikandilu). The other 53 cells use base game summon units.

### Feats

- At level 4 you gain the Augment Summoning feat automatically, ignoring prerequisites.
- No change to bonus feat slots.
- Because you lose all hexes, feats and options that require a hex are unavailable.
- No feats exclusive to this class are added.

### Resources

| Resource | Maximum | Gained by | Reset |
|---|---|---|---|
| Heat | 9 | +1 per blast hit (+2 on a confirmed critical hit from level 12) | 0 when combat ends, 0 after a completed rest, 0 on an automatic summon |

- Blast and Double Blast are usable at will.
- Heat Summoning has no daily limit; it only costs Heat.
- Spell slots match the Stigmatized Witch (spontaneous, Charisma-based).

### Compatibility and notes

**Archetype overlap**
- Warlock and Stigmatized Witch cannot be taken together, and neither can any other witch archetype that replaces the same features.
- Only the Stigmatized Witch's spellbook and cantrips are borrowed; you do not receive the Stigmatized Witch's curse.

**Prestige classes (base game)**
- A warlock cannot pick the "witch spellbook" option of the base game's prestige classes.
- Arcane Trickster, Mystic Theurge: the spellbook selection is mandatory, so a warlock cannot finish leveling up in these classes.
- Eldritch Knight, Hellknight Signifer, Loremaster, Winter Witch: you gain class levels, but your spellbook does not advance.

**Other mods**
- TabletopTweaks-Base: in a game where it replaces Loremaster's spellbook selection with its own, a warlock should be able to advance her own (Stigmatized Witch) spellbook through Loremaster (this follows from how that mod builds its selection; it has not been tested in game). Its alternate witch capstone selection at level 20 contains no hexes, so warlocks keep it. A patron gained through its Second Patron option does not change your group.
- PrestigePlus: the prestige classes it adds offer a Stigmatized Witch spellbook option, so a warlock should be able to advance her own spellbook through them (not tested in game).
- ExpandedContent: the Stargazer's "witch spellbook" option is blocked for warlocks (picking it would create a separate witch spellbook instead of advancing the warlock's own). Nothing happens if ExpandedContent is not installed.
- CharacterOptionsPlus: works alongside the Winter Witch archetype restriction that mod adds to the prestige spellbook options.
- If another mod adds witch patrons, those patrons are not on the group table; a warlock whose only patron is one of them gets no blast, summons or ward.
- If another mod changes the base game's prestige "witch spellbook" options in an unrecognized way, the Warlock may fail to be created (a `Warlock:` error appears in the mod log).

**DLC**
- The Abyss group's tier III summon (Abrikandilu) uses a unit that belongs to DLC 5 (The Lord of Nothing). DLC ownership is not checked, so this cell is not guaranteed to work in a game without that DLC.

**Multiclassing**
- Blast dice, caster level, summon duration and the Star group's spell resistance count witch levels only. Levels in other classes add nothing.

**Known limitations**
- The Storm group's blast deals no damage to demons, which are immune to electricity (Heat still builds).
- Tier IX of the Hell, Storm and Death groups is a creature of the same rank as tier VIII; in exchange it gains Patron's Favor (+4 Strength and Constitution a second time).
- Fey tiers III and IV summon the same creature (Medium Water Elemental).
- Saving during the brief moment an automatic summon is resolving can lose that summon and your Heat.
- Loading a save that contains a warlock after removing the mod is not supported.


</details>

## Weapon-Spirit Shaman

<details>
<summary><b>Weapon-Spirit Shaman</b></summary>

### At a glance

| Item | Details |
|---|---|
| Base class | Shaman archetype |
| Key ability | Wisdom (same as the base shaman) |
| Spellcasting | Unchanged — shaman spells, spirit and hexes as in the base class |
| Required mods | None |
| Optional integrations | None (see "Compatibility and notes" for TabletopTweaks and others) |
| Alignment / deity restrictions | None. The Weapon Spirit cannot be chosen if you already have an animal companion |

### Overview

The Weapon-Spirit Shaman trades the spirit animal for a spirit dwelling in a weapon: the Weapon Spirit. It is a floating weapon that fights beside you on its own, much like an animal companion. Put magic weapons into the Weapon Spirit's spare weapon sets and their magic is mirrored onto the weapon it is currently wielding, so spare magic weapons become fuel rather than vendor trash. From level 5 you can call on Spirit Possession a few times per day to take that mirrored power into your own weapon for a short while, and at level 20 you can fuse with the spirit and use its hit points as a shield. Your shaman spells, spirit and hexes are untouched.

### What you give up

| Level | Base game feature | Notes |
|---|---|---|
| 1 | Spirit Animal | Removed. The Weapon Spirit takes its place |
| 4 | Wandering Spirit | Removed |
| 6 | Wandering Hex | Removed |
| 14 | Wandering Hex (second) | Removed |

### What you gain (by level)

| Level | Feature | Effect |
|---|---|---|
| 1 | Weapon Spirit | Choose one of 59 weapon categories. You gain Weapon Focus with it (ignoring prerequisites), and a Weapon Spirit wielding a masterwork weapon of that category becomes your companion |
| 1 | Spirit Mirror | An ability of the Weapon Spirit. The magic of weapons in its spare weapon sets is mirrored onto the weapon it wields. Mirror count B = shaman level / 4 (rounded down) + 1 |
| 5 | Spirit Possession I | Swift action, 1/day, 3 rounds. The power mirrored onto the Weapon Spirit's weapon moves to your own primary weapon (or empty hand) |
| 10 | Spirit Possession II | 2/day, 5 rounds. Adds flight (immune to ground effects and trip) during possession |
| 15 | Spirit Possession III | 3/day, 7 rounds. Adds +2 to hex DCs during possession |
| 20 | Spirit Possession IV (Fusion) | 4/day, 10 rounds. You can spend a possession use on Spirit Fusion — 10 rounds of the Possession III effects, with damage you take going to the Weapon Spirit's hit points first |

### Abilities in detail

#### Weapon Spirit

You must choose one weapon category at level 1.

- Shaman: gains Weapon Focus with the chosen category (ignoring prerequisites). You do not gain proficiency with that weapon.
- Weapon Spirit: appears wielding a masterwork weapon of the chosen category. For double weapons, both heads are masterwork.

Weapon Spirit traits:

| Item | Value |
|---|---|
| Type | A companion that occupies the animal companion slot. It advances with your shaman level the same way an animal companion does |
| Base ability scores | Str 13, Dex 15, Con 15, Int 2, Wis 12, Cha 6 |
| Size / speed | Medium / 50 ft. |
| Flight | Always airborne — immune to ground effects and trip |
| Natural armor | +5 |
| Proficiencies | Every weapon category in the selection (all 59), light and medium armor. No shield proficiency |
| Equipment slots | Uses all four weapon sets. Cannot use the boots, two ring, glasses and shirt slots |
| Automatic feats | The Weapon Focus and Improved Critical that animal companions take for their bite are taken for the chosen weapon category instead |
| Death | If it falls, it returns after you rest |

Differences from a regular animal companion: it does not gain Multiattack, it is not of the animal type, and it cannot take animal companion archetypes.

#### Spirit Mirror

The magic of weapons in the Weapon Spirit's spare weapon sets (the three sets not currently in use, up to six slots) is mirrored onto the weapon it is currently wielding. This is automatic and costs nothing.

Mirror count B:

| Shaman level | 1–3 | 4–7 | 8–11 | 12–15 | 16–19 | 20 |
|---|---|---|---|---|---|---|
| B | 1 | 2 | 3 | 4 | 5 | 6 |

Rules:

- Enhancement bonus: only the single highest enhancement among the spare weapons is mirrored. It is capped at the lower of B and 5, and applies only when it is higher than the wielded weapon's own enhancement (they do not stack).
- Special properties: up to B of them, taken in weapon set order and, within a set, main hand before off hand. Each property is mirrored once, and properties the wielded weapon already has are skipped. Anything past B is left out.
- Ghost touch: always mirrored, and does not count against B.
- What is mirrored: properties that only change the weapon's own rules (for example extra energy damage, critical range or multiplier, alignment or material).
- What is not: effects that trigger separately on attack, effects that depend on the wielder's or caster's state, equipment effects, and masterwork quality. Temporary enhancements from spells and the like vanish when a weapon is moved to a spare set, so they are not mirrored either.
- Where it goes: the main-hand weapon of the active set; if there is no main-hand weapon, the off-hand weapon; if both hands are empty, the unarmed strike. If the hands hold only non-weapons (such as a shield), nothing is mirrored.
- When it is recalculated: when the Weapon Spirit's equipment changes, when it switches weapon sets, and when the shaman levels up.
- Each recalculation writes one line to the mod log listing what was mirrored and why anything was left out.

#### Spirit Possession

- Use: swift action, supernatural ability, targets yourself. Costs one possession use.
- Requirements: the Weapon Spirit must be alive, visible and within 30 feet.
- Effect: the power mirrored onto the weapon the Weapon Spirit currently wields (mirrored enhancement, mirrored special properties and ghost touch) is placed on your primary weapon. If your main hand is empty, it goes to your unarmed strike.
  - Enhancements do not stack. Properties your weapon already has are skipped.
  - The spirit weapon's own built-in enchantments are not copied — only what was mirrored onto it.
  - Changing weapons ends the borrowed power.
  - If your main hand holds a non-weapon (such as a shield), you receive nothing and the use is still spent.
- The Weapon Spirit keeps fighting during the possession.

| Shaman level | Tier | Uses per day | Duration | Effect |
|---|---|---|---|---|
| 5–9 | Spirit Possession I | 1 | 3 rounds | Mirrored power on your primary weapon |
| 10–14 | Spirit Possession II | 2 | 5 rounds | + flight (immune to ground effects and trip) |
| 15–19 | Spirit Possession III | 3 | 7 rounds | + hex DC +2 |
| 20 | Spirit Possession IV | 4 | 10 rounds | Possession III effects; Spirit Fusion becomes available |

#### Spirit Fusion

Level 20. A separate ability that draws on the same possession uses.

- Use: swift action, one possession use. Same requirements as possession (Weapon Spirit alive, visible and within 30 feet), and no possession or fusion may already be in progress.
- Effect: the Weapon Spirit vanishes (hidden), and for 10 rounds you gain the Spirit Possession III effects (mirrored power, flight, hex DC +2).
- Damage absorption: while fused, damage you take is taken first by the Weapon Spirit's current hit points. When its hit points reach 0, the remaining damage is yours, the spirit falls (it returns after resting) and the fusion ends.
- Ending: when the duration runs out, the Weapon Spirit reappears with its remaining hit points. Leaving the area or saving ends the fusion, as does the shaman's death.

### Choices

The level 1 Weapon Spirit weapon category (59 options). Natural weapons and categories without a weapon model are excluded. In game the options appear under the base game's weapon names.

| Group | Categories |
|---|---|
| Melee | Dagger, Punching Dagger, Kukri, Starknife, Shortsword, Handaxe, Sickle, Kama, Sai, Nunchaku, Light Mace, Light Hammer, Light Pick, Club, Longsword, Rapier, Scimitar, Dueling Sword, Estoc, Falcata, Sawtooth Sabre, Bastard Sword, Battleaxe, Dwarven Waraxe, Heavy Mace, Flail, Warhammer, Heavy Pick, Tongi, Trident, Shortspear, Greatsword, Greataxe, Greatclub, Falchion, Elven Curved Blade, Earth Breaker, Heavy Flail, Scythe, Spear, Longspear, Glaive, Bardiche, Fauchard, Quarterstaff |
| Double weapons | Double Axe, Double Sword, Hooked Hammer, Urgrosh |
| Shield spikes | Spiked Light Shield, Spiked Heavy Shield |
| Thrown | Dart, Javelin, Throwing Axe |
| Ranged | Longbow, Shortbow, Light Crossbow, Heavy Crossbow, Sling Staff |

Every option does the same thing: Weapon Focus with that category for the shaman, plus a Weapon Spirit wielding a masterwork weapon of that category. Whatever you pick, the Weapon Spirit is proficient with all 59 categories, so you can hand it a different weapon later.

### Feats

- Weapon Focus with the weapon category chosen at level 1, gained automatically and ignoring prerequisites.
- The Weapon Spirit takes Weapon Focus and Improved Critical with the chosen category as automatic feats.
- No change to bonus feat slots.
- The Weapon Spirit occupies the animal companion slot. If you already have an animal companion, the Weapon Spirit selection is unavailable.
- Because you lose Wandering Spirit and Wandering Hex, options that require them are unavailable.
- No feats exclusive to this class are added.

### Resources

| Resource | Formula | Recovery |
|---|---|---|
| Spirit Possession uses | 1 at shaman level 5, +1 every 5 levels after that (1 at level 5 / 2 at 10 / 3 at 15 / 4 at 20) | Resting |

- Spirit Possession and Spirit Fusion share the same uses.
- Only shaman levels count.
- Spirit Mirror uses no resource.

### Compatibility and notes

**Animal companions**
- The Weapon Spirit uses the animal companion slot. It cannot be combined with classes or feats that grant an animal companion some other way (if you already have one, the Weapon Spirit cannot be chosen).

**Other mods**
- This archetype removes Spirit Animal (level 1), Wandering Spirit (level 4) and Wandering Hex (levels 6 and 14) from the shaman progression. If another mod changes the shaman progression so that these features are no longer at those levels, the class is not created.
- If TabletopTweaks or a similar mod changes the animal companion progression, the Weapon Spirit follows the changed progression.
- Known additions that other mods attach to the animal companion class or to basic weapons (ExpandedContent, MAKING FRIENDS, ToggleableThrowingWeapons) are recognized and not carried over to the Weapon Spirit. An unrecognized addition may prevent the class from being created (a `WeaponSpiritShaman:` error appears in the mod log).

**DLC**
- The Sawtooth Sabre Weapon Spirit's starting weapon is built from a DLC reward weapon in the base game data and keeps its DLC condition marker (a DLC 6, A Dance of Masks, reward). How this option and weapon appear in a game without that DLC is not guaranteed.
- The other 58 categories are built from standard base game weapons.

**Multiclassing**
- Mirror count B and the possession tier, uses and duration count shaman levels only.

**Known limitations**
- The shaman does not gain proficiency with the chosen weapon. You get Weapon Focus, but need proficiency from elsewhere to wield that weapon yourself.
- When possession or fusion is unavailable, the tooltip shows all requirements as a single sentence; it does not tell you which one failed.
- Saving during a fusion ends the fusion just before the save.
- The damage absorbed during fusion is calculated before the difficulty's damage multiplier is applied.
- Damage that lands at the exact moment a fusion ends may not be deducted from the Weapon Spirit's hit points.
- If the fused Weapon Spirit can no longer be found (dismissed, replaced and so on), the fusion may fail to end properly and fusion may become unusable afterwards.
- A shaman who is merely unconscious does not end the fusion (only death does).


</details>

## Bulwark Mage

<details>
<summary><b>Bulwark Mage</b></summary>

### At a glance

| | |
|---|---|
| Base class | Wizard archetype |
| Key ability | Intelligence (same as wizard) |
| Spellcasting | Kept — the full wizard spellbook up to 9th-level spells, but with no arcane school |
| Required mods | None |
| Optional integrations | None (works alongside PrestigePlus and TabletopTweaks — see Compatibility and notes) |
| Alignment / deity restrictions | None |

### Overview

The Bulwark Mage is a front-line wizard who casts in heavy armor behind a tower shield. Armor and shields cause no arcane spell failure, and as you level you pick up the shield features of the fighter's Tower Shield Specialist along with armor training. The signature ability is Shield Rebuke: when a melee weapon hits you, your shield answers the attacker. The intended stance is a quarterstaff in one hand and a tower shield in the other. The price is steep — you give up your arcane school, arcane bond, Scribe Scroll and all five wizard bonus feats.

### What you give up

| Level | Base feature | Notes |
|---|---|---|
| 1 | Arcane Bond selection | Neither a bonded item nor a familiar. You get Bonded Shield instead |
| 1 | Arcane school selection | The selection is removed outright. You are neither a specialist nor a universalist, so you have no school powers, no bonus school spell slots and no opposition schools |
| 1 | Scribe Scroll | Removed |
| 1, 5, 10, 15, 20 | Wizard bonus feat | All five removed |

The wizard spellbook, spell learning and the wizard's weapon proficiencies are unchanged.

### What you gain (by level)

| Level | Feature | Effect |
|---|---|---|
| 1 | Bulwark Proficiency | Proficiency with light, medium and heavy armor and with all shields (bucklers, light, heavy and tower shields). None of them cause arcane spell failure |
| 1 | One-Handed Staff | You can wield a quarterstaff in one hand (without the two-handed Strength bonus) |
| 1 | Shield Rebuke | Learn one rebuke. The rebuke you keep active hits any attacker who strikes you with a melee weapon. Once per your round, 3 + Intelligence modifier times per day (minimum 1) |
| 1 | Bonded Shield | While you hold a tower shield in your off hand, once per day you can regain one wizard spell you have already cast |
| 2, 6, 10, 14, 18 | Burst Barrier | The base game's Tower Shield Specialist feature, unchanged. Five ranks |
| 3, 7, 11, 15 | Armor Training | The fighter's Armor Training, ranks 1–4. Each rank raises your armor's maximum Dexterity bonus by 1 and reduces its armor check penalty by 1; armor no longer slows you |
| 3, 7, 11, 15 | Tower Shield Training | The same values as the Tower Shield Specialist's Tower Shield Training, ranks 1–4 (a better maximum Dexterity bonus and a smaller armor check penalty with a tower shield) |
| 5 | Tower shield attack penalty negated | The Tower Shield Specialist's level 5 feature, unchanged (removes the attack penalty for carrying a tower shield) |
| 5 | Iron Concentration | While wearing heavy armor, you automatically succeed on concentration checks caused by damage |
| 5 | Learn Shield Rebuke (2nd) · Bonded Shield +1 | Learn another rebuke. +1 enhancement to the tower shield in your off hand |
| 9 | Touch shield | The Tower Shield Specialist's level 9 feature, unchanged (your tower shield's shield bonus applies to touch AC) |
| 10 | Learn Shield Rebuke (3rd) · Bonded Shield +2 | |
| 13 | Total cover | The Tower Shield Specialist's level 13 feature, unchanged |
| 15 | Learn Shield Rebuke (4th) · Bonded Shield +3 | |
| 16 | Tower shield evasion | The Tower Shield Specialist's level 16 feature, unchanged (evasion while carrying a tower shield) |
| 20 | Improved tower shield evasion | The Tower Shield Specialist's level 20 feature, unchanged (improved evasion while carrying a tower shield) |
| 20 | Learn Shield Rebuke (5th) · Bonded Shield +4 | |

Levels 4, 8, 12, 17 and 19 grant nothing from the archetype beyond normal spell progression.

### Abilities in detail

#### Bulwark Proficiency

- Proficiencies: light armor, medium armor, heavy armor, bucklers, light shields, heavy shields, tower shields.
- Armor and shields of those seven kinds cause no arcane spell failure.
- The effect belongs to the character, not to the wizard spellbook: any other arcane spellbook you gain by multiclassing benefits too.
- Other armor statistics, such as the armor check penalty, still apply (Armor Training and Tower Shield Training ease them).

#### One-Handed Staff

- Applies to quarterstaves only. Every other two-handed weapon still needs both hands.
- A quarterstaff held in one hand does not get the two-handed Strength bonus.
- With your off hand free, you can carry a tower shield alongside it.

#### Shield Rebuke

**Trigger** — it fires automatically when all of the following are true:

1. One of the rebukes you know is switched on (a toggle, free action; only one can be active at a time, and it stays on through rest).
2. You are holding a tower shield in your off hand.
3. A melee weapon attack **hits** you (whether or not it deals damage).
4. You have not rebuked yet during your current round.
5. You have at least one daily use left.

**Rules**

- Once per your round. Re-equipping the shield or switching toggles in the same round does not let it fire again.
- Firing spends one daily use. **The use is spent even if the rebuke has no effect** — because the attacker is already down, saved, or is immune. The combat log then shows "Shield rebuke fizzles: (rebuke) — (reason)".
- Save DC = 10 + half your wizard level (rounded down) + Intelligence modifier.
- Rebuke damage is never multiplied on a critical hit.
- Damage dice: one die per two wizard levels, rounded up — 1 die at level 1, 5 at level 10, 10 at level 20.
- Switching from one rebuke to another is not instant: the rebuke that was active stays in effect until your next round, and the new one starts after that.

The seven rebukes are listed under Choices below.

#### Bonded Shield

- **Spell recall**: while you hold a tower shield in your off hand, once per day as a free action you can restore the slot of one wizard spell you have already cast, of any spell level. It works like the base game's bonded-item recall: use it by clicking a spent spell slot in your spellbook. Without a tower shield in hand the ability is not available at all.
- **Enhancement**: at levels 5, 10, 15 and 20, the **tower shield currently in your off hand** gains +1 enhancement, up to +4.
  - It does not stack with the shield's own enhancement bonus; only the higher of the two applies.
  - Shields stowed in your other weapon sets get nothing. The bonus drops off when you put the shield away or switch weapon sets, and returns when you take it up again.
  - Each enhancement step also reduces that shield's armor check penalty by 1.

#### Iron Concentration

- While you are wearing **heavy armor**, you automatically succeed on concentration checks forced by taking damage while casting.
- Concentration checks from any other cause (casting defensively and so on) are rolled normally.
- Mithral heavy armor counts as medium armor, so it does not qualify. Your shield does not matter.

#### Tower Shield Specialist features and Armor Training

- Burst Barrier, the negated tower shield attack penalty, touch shield, total cover, tower shield evasion and improved tower shield evasion are the fighter Tower Shield Specialist's own features, granted at the same levels. All of them work only while a **tower shield is in your off hand**.
- Tower Shield Training gives the same values as the base game's version, but is a separate copy built so that putting your shield away never knocks your Armor Training ranks back down.
- Armor Training is the fighter's feature, unchanged.

### Choices

Shield Rebuke: at levels 1, 5, 10, 15 and 20 you learn one of the seven rebukes below, five in all. You cannot learn the same one twice. (N = one die per two wizard levels, rounded up.)

| Rebuke | Effect | Save |
|---|---|---|
| Force Rebuke | N d6 force damage to the attacker | None |
| Shock Rebuke | N d6 electricity damage | Reflex half |
| Frost Rebuke | N d4 cold damage, and the attacker is staggered for 1 round | No save against the damage; Fortitude negates the stagger |
| Repulse | Pushes the attacker 10 feet away (does not provoke attacks of opportunity) | None |
| Slowing Rebuke | Slowed for 1 round + 1 per five wizard levels (1 round at level 1, 5 rounds at level 20) | Will negates |
| Dazing Rebuke | Dazed for 1 round | Will negates |
| Dispelling Rebuke | Attempts a targeted dispel magic on the attacker. Caster level = character level | None (dispel check) |

### Feats

- **Granted automatically**: none. Armor and shield proficiencies come directly from Bulwark Proficiency rather than from feats.
- **Bonus feat slots**: all five wizard bonus feats (levels 1, 5, 10, 15, 20) are gone. Anything another mod adds to that selection list is lost with them.
- **No longer available**: arcane bond (bonded item or familiar), arcane school (including universalist), Scribe Scroll.
- **Archetype-only feats**: none.

### Resources

| Resource | Maximum | Restored |
|---|---|---|
| Shield Rebuke uses | 3 + Intelligence modifier (minimum 1) | On rest |
| Bonded Shield spell recall | 1 per day | On rest |

The "once per round" limit on Shield Rebuke is not a resource. It clears when your own round starts again — your personal 6-second cycle in real time, or the start of your turn in turn-based mode.

### Compatibility and notes

- **Required mods and DLC**: none. Only base-game assets are used.
- **PrestigePlus**: One-Handed Staff touches the same spot as PrestigePlus's one-handed grip fix, but the two do not override each other. A quarterstaff can be held in one hand if either mod's condition is met.
- **TabletopTweaks-Base**: its Advanced Armor Training option only changes the fighter, so it has no effect here. The Bulwark Mage always gets plain Armor Training ranks 1–4 and never the advanced armor training selections.
- **Mods that change the wizard progression**: if the features this archetype removes (arcane bond selection, school selection, Scribe Scroll, bonus feats) are not at their usual levels in the wizard progression, the archetype is not registered and will not appear in the list.
- **Multiclassing and prestige classes**: rebuke dice, the slow duration and the save DC all look at **wizard levels** only; levels in other classes do not advance them. Only Dispelling Rebuke's caster level uses character level. Bonded Shield's recall restores spells from the wizard spellbook.
- **Side effects of a one-handed quarterstaff**: the quarterstaff now counts as a one-handed weapon, so other abilities that require one — a magus's Spell Combat, for instance — can be satisfied with it.
- **Losing One-Handed Staff**: if you lose the feature through a respec, a quarterstaff and shield you already have equipped are not removed automatically.
- **Repulse distance**: the push is converted to the game's internal metres and comes out slightly short of 10 feet (about 9.8 feet).
- **Bonded Shield enhancement**: a shield that leaves your body without passing through a hand slot may keep the enhancement. It is cleaned up the next time you take that shield in hand.
- **Iron Concentration**: it works by forcing the check's DC extremely low, so another mod that overwrites the DC of the same check afterwards could break the automatic success.


</details>

## Chaos Warrior

<details>
<summary><b>Chaos Warrior</b></summary>

### At a glance

| Item | Details |
|---|---|
| Base class | Warpriest archetype |
| Key abilities | Wisdom (spells, rage uses, pain DC); Strength and Constitution for melee. Attribute recommendations are the base warpriest's |
| Spellcasting | Kept (same spellbook and spell list as the base warpriest, Wisdom-based) |
| Required mods | Unity Mod Manager |
| Optional integrations | None |
| Restrictions | Alignment must be chaotic neutral or chaotic evil. No deity restriction |

### Overview

The Chaos Warrior is a warrior priest touched by divine madness. You trade away fervor, channel energy and sacred armor, and in return you can fly into a chaos rage from 1st level. Every rage warps your body at random with a mix of helpful and harmful mutations, and every melee hit you land while raging stacks pain on your enemy. At five stacks the enemy must make a Will save or become confused. Because you can still cast spells while raging, you buff up, wade into the front line and grind the enemy formation into chaos.

### What you give up

| Level | Base feature | Notes |
|---|---|---|
| 1 | Second blessing selection | You choose one blessing instead of two. The first selection and the blessing uses are unchanged |
| 2 | Fervor | The fervor resource and its base ability |
| 2 | Fervor: heal/harm selection | The touch heal / touch damage use of fervor |
| 2 | Fervor: swift casting | Spending fervor to cast a self-targeted spell as a swift action |
| 4 | Channel Energy selection | Removed along with the fervor resource it would spend |
| 5, 8, 11, 14, 17, 20 | Fervor heal/harm progression | All six steps |
| 7, 10, 13, 16, 19 | Sacred Armor +1 to +5 | All five steps |

Kept: spellcasting, the first blessing, bonus feats (levels 3, 6, 9, 12, 15, 18), Sacred Weapon (levels 4, 8, 12, 16, 20), Aspect of War at 20th level, and all other warpriest features such as the weapon focus selection.

### What you gain (by level)

| Level | Feature | Effect |
|---|---|---|
| 1 | Chaos Rage | Free action, in combat only. +4 Strength and Constitution, +2 Will saves, -2 AC. Uses per day: max(1, half warpriest level + Wisdom modifier). You can cast spells while raging and are not fatigued afterwards |
| 1 | Mutations of Madness | Each time you start a rage, 1-3 of 16 mutations (up to 3-6 at higher levels) are applied at random; all end with the rage |
| 1 | Pain | Each melee weapon hit while raging adds 1 stack to an enemy: -1 attack rolls, saves and skill checks per stack for 3 rounds. At 5 stacks the target makes a Will save (DC 10 + half warpriest level + Wisdom modifier) or is confused for 1d4 rounds |
| 10 | Spreading Madness | When pain reaches 5 stacks, other enemies within 15 ft of the target make the same Will save |
| 11 | Greater Chaos Rage | Rage bonuses become +6 Strength and Constitution, +3 Will |
| 20 | Mighty Chaos Rage | Rage bonuses become +8 Strength and Constitution, +4 Will |

### Abilities in detail

#### Chaos Rage

- Activation: a toggle, switched on as a free action and only while in combat. It starts switched off.
- Cost: one daily use each time you switch it on, with no upkeep. A rage started with your last use still lasts until you end it or combat ends.
- Ending: the rage ends automatically when combat ends or if you fall unconscious. Switching it off ends it immediately. Starting it again in the same fight costs another use and rolls new mutations.
- Values (rage bonuses):

| Warpriest level | Strength and Constitution | Will saves | AC |
|---|---|---|---|
| 1-10 | +4 | +2 | -2 |
| 11-19 | +6 | +3 | -2 |
| 20 | +8 | +4 | -2 |

- You can cast spells while raging, and you are not fatigued when the rage ends.
- Chaos Rage belongs to the same activatable group as barbarian rage and bloodrage, so it cannot be active together with them.
- Starting the rage rolls your Mutations of Madness, and while it is active your melee hits apply Pain.

#### Mutations of Madness

- Triggers automatically whenever you start a chaos rage. First a number of mutations is rolled uniformly from the range below, then that many are drawn without repeats from the 16 mutations (11 good, 5 bad). Every mutation is equally likely.

| Warpriest level | Number of mutations |
|---|---|
| 1-5 | 1-3 |
| 6-10 | 1-4 |
| 11-15 | 2-5 |
| 16-20 | 3-6 |

- All mutations end when the rage ends. They do not change during a rage (a rage that is suppressed and then resumes keeps the mutations it already has).
- The full list is in the Choices section below. You do not pick them; they are random.

#### Pain

- When it stacks: every time the chaos warrior **hits with a melee weapon attack while raging** (attacks of opportunity count, ranged attacks do not) and the creature hit is an **enemy**. Hitting an ally applies nothing.
- How much: 1 stack per hit, or 2 stacks per hit while you have the Amplified Pain mutation.
- Effect per stack: -1 on attack rolls, -1 on all saving throws and -1 on skill checks (Athletics, Mobility, Persuasion, Trickery, Stealth, Perception, Use Magic Device, Knowledge (Arcana), Knowledge (World), Lore (Nature), Lore (Religion)).
- Duration: 3 rounds after the most recent stack; every new stack resets it to 3 rounds. Pain already applied stays for its remaining duration even after your rage ends.
- Stacking rules:
  - Pain is tracked per chaos warrior. Two chaos warriors hitting the same enemy build separate stacks and reach the threshold separately.
  - If a creature carries pain from several chaos warriors, the penalties do not add together; only the largest applies.
- Threshold (5 stacks): the moment one chaos warrior's pain reaches 5 or more stacks (going from 4 to 6 with Amplified Pain still triggers only once):
  1. That chaos warrior's pain is removed from the target, whether or not the save succeeds.
  2. The target makes a Will save. DC = 10 + half warpriest level (rounded down) + Wisdom modifier.
  3. On a failure it is confused for 1d4 rounds (the base game's confusion condition).
- Creatures immune to mind-affecting effects: pain still builds but stops at 5 stacks. There is no save, no confusion and the pain is not removed (the -5 penalty lasts under the normal 3-round rule).
- As a result, a creature that is not immune to mind-affecting effects never carries more than 4 stacks.

#### Spreading Madness

- Automatic from 10th level. When pain reaches its threshold and the target makes its Will save, every **other enemy** within 15 ft of the target (alive and not immune to mind-affecting effects) makes a Will save against the same DC. Each one that fails is confused for 1d4 rounds.
- Allies are never affected. Nearby enemies do not need any pain stacks of their own.

### Choices

There is nothing to choose beyond the base warpriest's first blessing. Below are the 16 mutations that are applied at random when you start a rage.

Good mutations (11)

| Mutation | Effect |
|---|---|
| Enlarge | Size increases by one category; +2 size bonus to Strength |
| Tough Hide | DR 2/-, increasing by 1 at levels 5, 10, 15 and 20 (DR 6/- at 20th) |
| Regeneration | Fast healing 2, increasing by 1 at levels 5, 10, 15 and 20 (fast healing 6 at 20th) |
| Swiftness | +10 ft speed, +2 Reflex saves |
| Thorn Skin | Creatures that hit you with a melee attack take 1d6 piercing damage |
| Chaos Blood | +2 on all saving throws |
| Elemental Shell | Resistance 10 to one random energy type: fire, cold, acid or electricity |
| Amplified Pain | Your hits apply 2 stacks of pain instead of 1 |
| Extra Arm | One additional 1d6 slam secondary attack in a full attack |
| Horrific Form | Enemies within 5 ft are shaken (-2 on attack rolls, saves and skill checks; no save) |
| Long Limbs | +5 ft reach |

Bad mutations (5)

| Mutation | Effect |
|---|---|
| Twisted Legs | -10 ft speed |
| Blurred Eyes | -2 on attack rolls |
| Exposed Nerves | -2 AC |
| Bursting Veins | You take 1 damage every round |
| Seething Mind | -2 on Will saves |

### Feats

- Feats granted automatically: none.
- The warpriest bonus feats (levels 3, 6, 9, 12, 15, 18) are unchanged.
- Feats added specifically for this archetype: none.
- Because you lose fervor and channel energy, feats that require or improve those features have nothing to work with.

### Resources

- Chaos Rage: max(1, warpriest level / 2 (rounded down) + Wisdom modifier) uses per day. One use is spent each time you start a rage. Restored by resting.
- Blessings: the base game's blessing resource, granted by the first blessing selection.
- There is no fervor resource.

### Compatibility and notes

- No other mod is needed. If another mod has moved or removed the warpriest progression entries this archetype replaces (second blessing, fervor, the channel energy selection, sacred armor), the archetype is not registered and an error is written to the mod log.
- Chaos Rage counts as raging for base-game effects that check for barbarian rage: Cloak of Blood Scent, Call to Violence, Mangling Frenzy, Demonic Resentment, Bad Temper and Raging Brutality all respond to it. Inspire Tranquility suppresses Chaos Rage and Crystal Mind ends it. Some conditional parts of Bad Temper are not linked.
- Abilities outside that list that specifically require barbarian rage or bloodrage (rage powers, for example) may not treat Chaos Rage as a rage.
- Rage uses, the pain DC and the number and scaling of mutations count warpriest levels only. Levels in other classes or prestige classes do not add to them.
- The alignment restriction is checked when you take the archetype (at your first character level the alignment choices are limited to chaotic neutral and chaotic evil).
- No Rage or Confusion spells are added to the warpriest spell list.
- No DLC content is required.


</details>

## Chimera Monk

<details>
<summary><b>Chimera Monk</b></summary>

### At a glance

| Item | Details |
|---|---|
| Base class | Monk (archetype) |
| Key ability | Wisdom (mutation save DCs and mutagen uses are Wisdom-based; recommended attributes are the base monk's) |
| Spellcasting | None (same as the base monk) |
| Required mods | DarkCodex — the archetype uses its Feral Combat Training feat |
| Optional integrations | None |
| Alignment / deity restrictions | Nothing beyond the base monk's own restrictions |

### Overview

The Chimera Monk reshapes the body through practice. Every two levels from level 2 you pick one of sixteen mutations, adding natural attacks such as a tail, bite or claw, or scales, wings, a breath weapon or extra organs. Drinking the chimeric mutagen in combat raises an ability score and your natural armor, and pushes every mutation you own one step further. Flurry of blows, unarmed strike, the Wisdom bonus to AC and ki powers all work exactly as they do for a base monk; the price is three bonus feats.

### What you give up

| Level | Base class feature | Notes |
|---|---|---|
| 2 | Monk bonus feat | Removed |
| 6 | Monk bonus feat | Removed |
| 10 | Monk bonus feat | Removed |

The monk bonus feats at levels 1, 14 and 18 are kept. Flurry of blows, unarmed damage dice, the AC bonus, ki powers and every other monk feature are unchanged.

### What you gain (by level)

| Level | Feature | Effect |
|---|---|---|
| 1 | Feral Combat Training (a DarkCodex feat) | Granted automatically. DarkCodex's feat that lets natural weapons be used like unarmed strikes |
| 1 | Chimeric Mutagen | Standard action. +4 alchemical bonus to Strength, Dexterity or Wisdom (+6 at level 10, +8 at level 16) and +2 natural armor (+4, +6). Lasts 1 round per monk level, 3 + Wisdom modifier uses per day (minimum 1). Empowers every mutation while it lasts |
| 1 | Chimeric Body | The damage dice of natural weapon mutations grow with monk level (1d3 up to 2d6). Not shown on the character sheet |
| 2, 4, 6, 8, 10, 12, 14, 16, 18 | Chimeric Mutation | Choose one of sixteen mutations. Nine picks in total, each mutation once |

Numbers that rise automatically with level: the mutagen bonuses (levels 10 and 16), natural weapon dice (levels 4, 8, 12, 16 and 20), Scales (levels 5, 10, 15 and 20) and breath dice (every two levels).

### Abilities in detail

#### Chimeric Mutagen

- **Activation**: standard action, supernatural. It comes as three abilities — Strength, Dexterity and Wisdom — and you choose one each time you drink. It cannot be used while you are polymorphed.
- **Effect**: an alchemical bonus to the chosen ability score and a natural armor bonus to AC. Unlike the alchemist's mutagen, there is no penalty to a mental ability score.

| Monk level | Ability score (alchemical) | Natural armor |
|---|---|---|
| 1–9 | +4 | +2 |
| 10–15 | +6 | +4 |
| 16–20 | +8 | +6 |

- **Duration**: 1 round per monk level.
- **One at a time**: drinking again replaces the previous chimeric mutagen.
- **Uses**: 3 + Wisdom modifier per day, minimum 1. Restored by resting.
- **Empowered mutations**: while a chimeric mutagen is active, every mutation you have gains its "while mutated" effect (see the table under Choices). The boosts end with the mutagen.
- The ability bonus is alchemical, so it does not stack with the base game mutagens or other alchemical bonuses; only the higher applies.

#### Natural weapon mutations and Chimeric Body

Tail, Bite, Claw, Extra Arm and Extra Tail each add one natural attack. Your hands remain unarmed strikes and your monk fist dice are untouched. All five use the same damage progression.

| Monk level | 1–3 | 4–7 | 8–11 | 12–15 | 16–19 | 20 |
|---|---|---|---|---|---|---|
| Natural weapon mutation | 1d3 | 1d4 | 1d6 | 1d8 | 1d10 | 2d6 |
| While mutated | 1d4 | 1d6 | 1d8 | 1d10 | 2d6 | 2d8 |
| (For reference) base monk fist | 1d6 | 1d8 | 1d10 | 2d6 | 2d8 | 2d10 |

- The Extra Arm also gains +1 on its attack roll while mutated, on top of the die step.
- Feral Combat Training (DarkCodex), granted at level 1, is what lets these natural weapons count as unarmed strikes.

#### Venom

- **Activation**: automatic. When a natural weapon or unarmed strike hits, the target is afflicted with Chimeric Venom. Once per round (it becomes available again when your next round begins).
- **Effect**: for 6 rounds the target makes a Fortitude save each round or takes 1d2 Constitution damage. It is a poison effect.
- **DC**: 10 + half monk level + Wisdom modifier. The DC is 2 higher if you are mutated at the moment the save is rolled.

#### Breath

- **Requirement**: monk level 10. You choose one element (fire, cold, acid or electricity) when you take the mutation, and it cannot be changed later.
- **Activation**: standard action, 30-ft cone.
- **Damage**: 1d6 per two monk levels (5d6 at level 10, up to 10d6 at level 20) of the chosen energy type. Reflex half.
- **DC**: 10 + half monk level + Wisdom modifier.
- **Uses**: 3 per day. In addition there is a separate Surge breath with 1 use, usable only while mutated and refilled on rest. The Extra Lung mutation raises it to 2 uses. Drinking another mutagen does not refill it.
- It does not use ki.

#### Scales

- A natural armor bonus of the form type. It stacks with the mutagen's natural armor, with natural armor enhancement from amulets, and with barkskin.

| Monk level | 1–4 | 5–9 | 10–14 | 15–19 | 20 |
|---|---|---|---|---|---|
| Scales | +2 | +3 | +4 | +5 | +6 |
| While mutated | +3 | +4 | +5 | +6 | +7 |

#### Partial Aspect

- Choose one of the shifter's ten minor aspects (one only; it cannot be changed later). You gain a toggle that turns that aspect on and off, with no uses per day and no resource.
- The aspect's numbers are computed from monk level instead of shifter level. The tiers are tier 1 at monk levels 1–7, tier 2 at 8–14 and tier 3 at 15–20.
- While mutated you get the values of the next tier up (a fourth tier's worth from level 15).
- The per-aspect numbers are in the table under Choices.

### Choices

#### Chimeric Mutation — sixteen options

One pick at each of levels 2, 4, 6, 8, 10, 12, 14, 16 and 18, nine in all. Each mutation can be taken once.

| Mutation | Requirement | Constant effect | While mutated |
|---|---|---|---|
| Tail | — | One tail natural attack. Damage 1d3 up to 2d6 (see the progression) | Die one step higher |
| Bite | — | One bite natural attack. Damage 1d3 up to 2d6 | Die one step higher |
| Claw | — | One claw natural attack (hands stay unarmed). Damage 1d3 up to 2d6 | Die one step higher |
| Venom | — | Natural and unarmed hits poison the target (once per round): Fortitude save each round for 6 rounds or 1d2 Constitution damage. DC 10 + half monk level + Wisdom | DC +2 |
| Scales | — | +2 natural armor (form) bonus, +1 more at levels 5, 10, 15 and 20 (maximum +6). Stacks with other natural armor | +1 |
| Partial Aspect | — | One of the ten shifter minor aspects as an at-will toggle (scaled by monk level) | Aspect values one tier higher |
| Wings | Monk level 10 | You grow wings (the base game wings effect) | +10 ft speed |
| Breath | Monk level 10 | Choose an element: 30-ft cone, 1d6 per two monk levels, Reflex half, 3 per day, standard action | One extra breath usable only while mutated (refills on rest) |
| Extra Arm | Monk level 10 | One extra-arm natural attack. Damage 1d3 up to 2d6 | Die one step higher, +1 on that attack roll |
| Extra Legs | — | +10 ft speed, +4 CMD against trip and bull rush | Another +10 ft speed |
| Extra Tail | Tail mutation | One more tail natural attack. Damage 1d3 up to 2d6 | Die one step higher |
| Extra Eyes | — | Cannot be flanked, +2 Perception | Another +2 Perception |
| Extra Ears | — | Blind-Fight feat, +2 Perception | Another +2 Perception |
| Extra Senses | Monk level 10 | Blindsense 30 ft | Blindsense 60 ft |
| Extra Heart | — | +2 hit points, immunity to bleed | Fast healing 2 |
| Extra Lung | — | Immunity to fatigue and to the area effects of four cloud spells (stinking cloud, cloudkill, mind fog, acid fog) | If you have Breath, +1 extra breath usable while mutated |

#### Breath element

Breath: Fire, Breath: Cold, Breath: Acid or Breath: Electricity. Only the energy type differs; dice, area, DC and uses are identical. You can take only one.

#### Partial Aspect forms

You can take only one. Tiers correspond to monk levels 1–7 / 8–14 / 15–20; while mutated you use the next tier's value.

| Aspect | Main effect | Tier 1 / 2 / 3 | While mutated |
|---|---|---|---|
| Bear | Constitution (inherent bonus) | +2 / +4 / +6 | +4 / +6 / +8 |
| Boar | Fortitude saves (inherent bonus) | +2 / +4 / +6 | +4 / +6 / +8 |
| Dinosaur | Initiative | +2 / +4 / +6 | +4 / +6 / +8 |
| Elephant | Strength (inherent bonus) | +2 / +4 / +6 | +4 / +6 / +8 |
| Horse | Speed, plus the base aspect's Endurance | +5 / +10 / +15 ft | +10 / +15 / +20 ft |
| Lizard | Reflex saves (inherent bonus) | +2 / +4 / +6 | +4 / +6 / +8 |
| Spider | AC (dodge bonus) | +2 / +4 / +6 | +4 / +6 / +8 |
| Tiger | Dexterity (inherent bonus) | +2 / +4 / +6 | +4 / +6 / +8 |
| Wolf | Extra damage, plus scent (blindsense) | +2 / +4 / +6 | +4 / +6 / +8 |
| Wolverine | Hit points and Constitution (see below) | See below | See below |

- **Wolverine**: bonus hit points = monk level × 2 × tier (for example +14 at level 7, +32 at level 8, +56 at level 14, +90 at level 15, +120 at level 20). While mutated it is monk level × 2 × (tier + 1) (for example +48 at level 8, +160 at level 20). The Constitution bonus is +2 at levels 1–14 and +4 from level 15 (+4 / +6 while mutated).
- **Wolf**: the scent blindsense is 20 ft at levels 8–14 and 30 ft from level 15. The base game shifter loses it at level 15; the Chimera Monk keeps the 30-ft version.
- Any other side effects of an aspect are the same as the base game shifter's corresponding minor aspect.

### Feats

- **Feats granted automatically**: Feral Combat Training (a DarkCodex feat) at level 1, regardless of that feat's prerequisites.
- **Bonus feat slots**: the monk bonus feats at levels 2, 6 and 10 are gone; those at levels 1, 14 and 18 remain.
- **Feats gained through mutations**: Extra Ears grants Blind-Fight.
- **Feats you can no longer take**: none.
- **Feats added specifically for this archetype**: none.

### Resources

| Resource | Maximum | Recovery |
|---|---|---|
| Chimeric Mutagen | 3 + Wisdom modifier (minimum 1) | Rest |
| Breath | 3 | Rest |
| Breath — Surge | 1 (2 with Extra Lung); usable only while mutated | Rest |

Venom, the Partial Aspect toggle and the other mutations use no resource. The ki pool is the base monk's, and no mutation spends ki.

### Compatibility and notes

- **DarkCodex is required**: without DarkCodex installed this mod does not load. If DarkCodex is installed but Feral Combat Training is turned off in its settings, the whole Chimera Monk archetype is not created and the missing piece is reported as an error in the mod log — you never get a half-working class.
- **What it uses from DarkCodex**: a single feat, Feral Combat Training, granted automatically at level 1 so that the natural weapon mutations work alongside the monk's unarmed techniques. The mod builds its content after DarkCodex has created that feat.
- **Shifter content**: Partial Aspect copies the base game shifter's minor aspects (the features, abilities and effects of the ten aspects, the wolf's scent, Diehard and Endurance) and rescales them to monk level. If any of those is missing from the game, the whole archetype is not created. The shifter originals are not modified, and the shifter's aspect uses per day are not involved.
- **Breath**: built from the base game Scaled Fist's dragon breath, but it uses neither Charisma nor ki. The base game has no dedicated electricity cone visual, so the electricity breath uses a generic cone effect.
- **TabletopTweaks**: with the setting that disables natural armor stacking, Scales is unaffected because it is a form-type bonus (only the mutagen's natural armor stops stacking with other natural armor). The chimeric mutagen's ability bonus is alchemical, so against a base game mutagen only the higher applies.
- **Other mods**: if another mod moves or removes the monk bonus feats at levels 2, 6 or 10, this archetype is not created.
- **Multiclassing and prestige classes**: the mutagen's bonuses and duration, natural weapon dice, Scales, breath dice and DC, the venom DC and the aspect tiers all read monk level only. Mutations with a level 10 requirement need monk level 10.
- **Known limitations**
  - It is not guaranteed that the extra natural attacks from mutations receive flurry's bonus attacks or iterative attacks from a high base attack bonus. Each mutation adds one natural attack.
  - The chimeric mutagen cannot be drunk while you are polymorphed.
  - The Surge breath refills only on rest and cannot be used without an active mutagen.
  - Extra Lung is not a general immunity to poison. It covers only the four cloud spell areas listed above, and fatigue.


</details>

## Oathbound Revenant

<details>
<summary><b>Oathbound Revenant</b></summary>

### At a glance

| Item | Details |
|---|---|
| Base class | Paladin |
| Key ability | Charisma (as an undead it also sets your hit points, and it drives the save DCs of your abilities). Recommended abilities are the same as the base paladin's |
| Spellcasting | None — the paladin spellbook is removed entirely |
| Required mods | None |
| Optional integrations | None |
| Alignment, deity and other restrictions | The base paladin's restrictions, unchanged by the archetype |

### Overview

The Oathbound Revenant is a paladin who came back from death to keep an oath. You are undead from 1st level: healing spells hurt you, negative energy mends you, and every weapon hit on the target of your smite renews your body. You lay on hands only for living allies, and from 4th level you channel negative energy instead of positive, wearing down living enemies while refilling yourself. You cast no spells at all. In their place you pick six of twenty death knight talents and build toward fear, draining or sheer refusal to fall.

### What you give up

| Level | Base feature | Notes |
|---|---|---|
| 2 | Lay on Hands | Both the self-targeted version and the base version for others are removed and replaced by Revenant's Hands, which works on allies only. The pool of uses stays |
| 3 | Divine Health | Replaced by undead immunities (1st level) |
| 3 | Aura of Courage | Replaced by Aura of Death |
| 3 | Mercy | Replaced by a death knight talent |
| 4 | Channel Positive Energy | Replaced by Channel Negative Energy |
| 4 onward | Paladin spellcasting | There is no spellbook. No replacement |
| 6 | Mercy | Replaced by a death knight talent |
| 9 | Mercy | Replaced by a death knight talent |
| 12 | Mercy | Replaced by a death knight talent |
| 15 | Mercy | Replaced by a death knight talent |
| 18 | Mercy | Replaced by a death knight talent |

Everything else stays: Smite Evil, Divine Grace, Divine Bond, the auras of Resolve, Justice, Faith and Righteousness, Holy Champion at 20th level, and so on.

### What you gain (by level)

| Level | Feature | Effect |
|---|---|---|
| 1 | Undead Body | You gain the undead type: undead immunities, healed by negative energy and harmed by positive energy, hit points based on Charisma instead of Constitution. |
| 1 | Oath Regeneration | When you hit your smite target with a weapon, heal hit points equal to your paladin level. Once per your round. |
| 1 | Smite: Negative Energy | Weapon hits on your smite target deal +1d6 negative energy damage, +1d6 more at 7th, 13th and 19th level (4d6 maximum). Not multiplied on a critical hit. Against creatures with negative energy affinity the same dice are divine damage. |
| 2 | Revenant's Hands | You can lay on hands on allies only — not on yourself, enemies, or allies with negative energy affinity. |
| 3 | Graveyard Dread | The target of your smite is shaken for as long as the smite lasts. |
| 3 | Aura of Death | Enemies within 10 feet take −4 on saving throws against fear and receive only half healing. |
| 3 | Death Knight Talent | Choose one talent (1st) |
| 4 | Channel Negative Energy | Standard action, costs 2 uses of lay on hands. Living enemies within 30 feet take 1d6 negative energy damage per two paladin levels (Will half). If any living enemy is caught, you heal the amount rolled. |
| 5 | Revenant's Steed | If you chose a mount for your divine bond, it becomes undead. Your Charisma modifier is added to its Charisma score, and your negative channel heals it by the same roll. |
| 5 | Revenant's Weapon Bond | If you chose a weapon for your divine bond, the bond can also grant necrotic, vampiric, ghost touch or dread blade. |
| 6 | Death Knight Talent | Choose one talent (2nd) |
| 7 | Smite: Negative Energy | Increases to 2d6 |
| 9 | Death Knight Talent | Choose one talent (3rd) |
| 12 | Death Knight Talent | Choose one talent (4th) |
| 13 | Smite: Negative Energy | Increases to 3d6 |
| 15 | Death Knight Talent | Choose one talent (5th) |
| 18 | Death Knight Talent | Choose one talent (6th) |
| 19 | Smite: Negative Energy | Increases to 4d6 |
| 20 | Revenant's Hands Maximized | Holy Champion: your Revenant's Hands lay on hands is maximized. |

### Abilities in detail

#### Undead Body

- Gained automatically at 1st level. You receive the game's standard undead type.
- You gain the full set of undead immunities and negative energy affinity. Negative energy heals you; positive energy (cure spells, channel positive energy and the like) damages you.
- Your hit points use Charisma instead of Constitution.
- Undead immunities include immunity to mind-affecting effects, so morale-type buffs from allies (bless, heroism, inspire courage and similar) may not affect you.

#### Oath Regeneration

- Automatic. Whenever a weapon attack of yours hits a creature you have smitten, you heal hit points equal to your paladin level. Melee and ranged attacks both count.
- Once per your round. It becomes available again at the start of your next round.
- It costs nothing, but it only matters while a smite is active.

#### Smite: Negative Energy

- Automatic. Every weapon hit on your smite target deals extra negative energy damage, with no limit on the number of hits.

| Paladin level | 1–6 | 7–12 | 13–18 | 19–20 |
|---|---|---|---|---|
| Extra damage | 1d6 | 2d6 | 3d6 | 4d6 |

- The extra damage is not multiplied on a critical hit.
- If the target has negative energy affinity (undead, for example), the same dice are dealt as divine damage, so you never heal an enemy with it.

#### Revenant's Hands

- The same ability as the base game's lay on hands for others, restricted to allies.
- It cannot target yourself, enemies, or allies with negative energy affinity (your own undead steed included).
- It costs one use of lay on hands. The amount healed and the number of uses are those of the base paladin's lay on hands.
- At 20th level this lay on hands is maximized (Revenant's Hands Maximized).
- You cannot heal yourself with it. Your own healing comes from Oath Regeneration, Channel Negative Energy and talents.

#### Graveyard Dread

- When you use Smite Evil, the target is marked and becomes shaken.
- There is no saving throw. The mark ends when the smite ends.
- The talents Oath of Pursuit and Cursed Strike ride on this same mark: their effects start and end with it.

#### Aura of Death

- An always-on aura affecting enemies within 10 feet.
- −4 on saving throws against fear.
- Healing received is halved.
- With the Dense Fear talent this aura is replaced by a 20-foot, −6 version. The two never stack.

#### Channel Negative Energy

- Standard action, supernatural ability. Costs two uses of lay on hands.
- Damages living enemies within 30 feet of you. Undead, constructs and enemies with negative energy affinity are not affected, and neither are allies.
- Damage is 1d6 per two paladin levels, +2d6 with the Empowered Channel talent. The dice are rolled once and the same result applies to every target.
- A successful Will save halves the damage. DC = 10 + half paladin level + Charisma modifier.
- If at least one living enemy is in the area, you heal once for the amount rolled (before halving).
- If your undead steed (Revenant's Steed) is in the area, it heals by the same amount.
- The ability carries the same alignment requirement as the base paladin's channel energy.

| Paladin level | 4 | 6 | 8 | 10 | 12 | 14 | 16 | 18 | 20 |
|---|---|---|---|---|---|---|---|---|---|
| Dice | 2d6 | 3d6 | 4d6 | 5d6 | 6d6 | 7d6 | 8d6 | 9d6 | 10d6 |
| With Empowered Channel | 4d6 | 5d6 | 6d6 | 7d6 | 8d6 | 9d6 | 10d6 | 11d6 | 12d6 |

#### Revenant's Steed

- Applies only if you chose a mount for your divine bond at 5th level.
- The steed gains the undead type (immunities, negative energy affinity, Charisma-based hit points).
- Your Charisma modifier is added to the steed's Charisma score. If your Charisma changes, the steed catches up within a round.
- Whenever your negative channel heals you, the steed heals by the same amount if it is inside the area.
- Oath Regeneration applies to the paladin only.

#### Revenant's Weapon Bond

- Applies only if you chose a weapon for your divine bond at 5th level.
- Four properties are added to the list you choose from when the divine weapon bond is active. The base options remain. Each property uses up the listed amount of the bond's enhancement allowance.

| Property | Cost | Effect |
|---|---|---|
| Necrotic | +1 | +1d6 negative energy damage on a hit. Divine damage against creatures with negative energy affinity. No effect on constructs. |
| Vampiric | +1 | Each hit heals you by half your paladin level. |
| Ghost Touch | +1 | The weapon deals full damage to incorporeal creatures. |
| Dread Blade | +2 | A creature you hit is shaken for 1d4 rounds on a failed Will save. DC = 10 + half paladin level + Charisma modifier. Counts as a fear effect. |

### Choices

#### Death Knight Talent

You choose one at levels 3, 6, 9, 12, 15 and 18 — six out of twenty. Each talent can be taken only once. "Level" is the lowest paladin level at which the talent can be chosen. Below, "paladin DC" means 10 + half paladin level + Charisma modifier.

| Level | Talent | Effect |
|---|---|---|
| 3 | Oath of Pursuit | Your smite target moves at half speed for as long as the smite lasts. No save. |
| 3 | Terrible Gaze | 3/day, standard action. One enemy at close range is frightened for 1d4 rounds on a failed Will save (paladin DC). Fear, mind-affecting. |
| 3 | Death's Touch | Standard action, costs 1 use of lay on hands. Touch an enemy to deal 1d6 negative energy damage per two paladin levels. Divine damage against negative energy affinity. No save. |
| 3 | Ghost Stride | Ignore difficult terrain; +10 feet to speed. |
| 3 | Death's Resolve | Adjacent allies (within 5 feet) are immune to fear. |
| 6 | Dense Fear | Your Aura of Death grows to 20 feet and its fear save penalty becomes −6. Healing is still halved. |
| 6 | Grave Armor | Damage reduction 5/good. |
| 6 | Oath Shield | When an adjacent ally (within 5 feet) takes melee weapon damage, you take up to 5 of the damage actually dealt in their place. |
| 6 | Soul Harvest | When you kill an enemy with a weapon, heal hit points equal to your paladin level. Kills with touch or ray attacks do not count. |
| 9 | Life Drain | Hitting your smite target with a weapon bestows one negative level for 1 minute. Once per your round. |
| 9 | Command Undead | 1/day. Take control of one enemy undead; Will negates (paladin DC). A supernatural ability with the effect of the command undead spell. |
| 9 | Wail of the Dead | 1/day, standard action. Enemies within 30 feet are frightened for 1d4 rounds on a failed Will save (paladin DC). Fear, mind-affecting. |
| 9 | Cursed Strike | Your smite target cannot be healed for as long as the smite lasts. |
| 9 | Iron Bones | 25% chance to negate critical hits and precision damage. |
| 12 | Death Blade | All melee weapon hits deal +1d6 negative energy damage. Divine damage against negative energy affinity. Not multiplied on a critical hit. The target does not have to be smitten. |
| 12 | Empowered Channel | Your negative channel deals +2d6. Your self-healing from it rises by the same amount. |
| 12 | Vengeful Riposte | 3/day. When a melee attack hits you, make one melee attack against the attacker. You must be wielding a melee weapon (or be unarmed) and the attacker must be within reach. A use is spent only when the riposte actually happens. |
| 12 | Mark of Death | If your smite target dies while smitten, the smite passes for free to the nearest other evil enemy within 30 feet. |
| 15 | Unyielding Oath | 1/day. When damage would reduce you to 0 hit points or below, you stay at 1 instead. Automatic. |
| 18 | Last Breath | When Unyielding Oath triggers, a burst of negative energy hits enemies within 30 feet. It deals twice your negative channel dice (Empowered Channel included — 18d6 at 18th level, 22d6 with Empowered Channel), Will half (paladin DC). Divine damage against negative energy affinity. Requires Unyielding Oath. |

### Feats

- No feats are granted automatically.
- The archetype neither adds nor removes bonus feat slots.
- The six Mercy selections (levels 3, 6, 9, 12, 15, 18) become six death knight talent selections.
- No feats are added specifically for this archetype.

### Resources

| Resource | Uses | Recovery | Spent on |
|---|---|---|---|
| Lay on hands | Same as the base paladin's lay on hands | Rest | Revenant's Hands (1), Channel Negative Energy (2), Death's Touch (1) |
| Terrible Gaze | 3 per day | Rest | The Terrible Gaze talent |
| Wail of the Dead | 1 per day | Rest | The Wail of the Dead talent |
| Command Undead | 1 per day | Rest | The Command Undead talent |
| Vengeful Riposte | 3 per day | Rest | The Vengeful Riposte talent (spent only on an actual riposte) |
| Unyielding Oath | 1 per day | Rest | The Unyielding Oath talent (Last Breath costs nothing extra) |

Channel Negative Energy draws on lay on hands uses, not on the base paladin's channel energy uses. Oath Regeneration and Life Drain have no pool, only the once-per-your-round limit.

### Compatibility and notes

- **Required mods**: none. The mod adds one archetype to the base paladin and leaves the paladin's other archetypes in place.
- **Mods that change the paladin progression**: this archetype removes Lay on Hands at level 2, Divine Health, Aura of Courage and Mercy at level 3, Channel Energy at level 4, and Mercy at levels 6, 9, 12, 15 and 18. If another mod has moved or removed any of these in the paladin's progression, this archetype is not registered.
- **Which smites count**: Oath Regeneration, Smite: Negative Energy, Graveyard Dread, Oath of Pursuit, Life Drain, Cursed Strike and Mark of Death react only to targets of the paladin's Smite Evil. Smites from other classes or mythic abilities do not trigger them.
- **Ally buffs may bounce**: because of undead immunities, mind-affecting buffs from allies may not land on you. Plan your party around it.
- **Healing**: cure spells and channel positive energy harm this character. Negative energy (inflict spells, channel negative energy) heals it.
- **Undead allies and lay on hands**: Revenant's Hands cannot target allies with negative energy affinity, so undead companions and your own undead steed cannot be healed with it.
- **Divine bond**: only the 5th-level feature matching your choice (mount or weapon) does anything; the other one has no effect.
- **Multiclassing**: talent prerequisites and all scaling use paladin level, not character level.
- **DLC**: the visual effect of the Aura of Death area uses a negative-energy area effect that is labelled as DLC 3 content in the game data. The numbers and behavior of every ability use base-game elements only.


</details>

## Shadow Sorcerer

<details>
<summary><b>Shadow Sorcerer</b></summary>

### At a glance

| | |
|---|---|
| Base class | Sorcerer |
| Key ability | Charisma. Recommended abilities are the same as the base Sorcerer's |
| Spellcasting | Kept. The Sorcerer spellbook is unchanged except that you get one fewer spell per day of each spell level from 1st to 9th, and you learn nine extra shadow and illusion spells |
| Required mods | None |
| Optional integrations | None |
| Alignment / deity restrictions | None added by the archetype |

### Overview

The Shadow Sorcerer casts with shadow doubles standing at their side. Cast one harmful spell at an enemy and the same spell lands again on the same target or the same spot, once per double, while only the original uses a spell slot. The doubles' spells are illusions, though, and an enemy that sees through them with a Will save takes only half. The price is one spell per day at every spell level and all three bloodline feats. You cast less often, and every cast hits several times over.

### What you give up

| Level | Base feature | Notes |
|---|---|---|
| 1 | Some spells per day | One fewer spell per day of each spell level from 1st to 9th. Shown on the sheet as Spells Given to Shadow |
| 7 | Bloodline Feat Selection | Removed |
| 13 | Bloodline Feat Selection | Removed |
| 19 | Bloodline Feat Selection | Removed |

Your bloodline choice, bloodline powers, bloodline spells and bloodline arcana are all kept, and so is the Sorcerer bonus feat at level 1.

### What you gain (by level)

| Level | Feature | Effect |
|---|---|---|
| 1 | Shadow Echo | When you cast a harmful spell at an enemy from your Sorcerer spellbook, the first such spell each round is recast at the same target or point once per shadow double. No spell slot is used. Each target may disbelieve with a Will save against the original DC; on a success it takes half damage and buffs and debuffs apply only 50% of the time |
| 1 | Shadow Doubles 1 | 1 double — 1 echo |
| 1 | Spells Given to Shadow | One fewer spell per day of each spell level from 1st to 9th. Ability bonus spells are unaffected |
| 3 | Shadow Arcana: Vanish | You learn Vanish (1st level) as an extra spell |
| 5 | Shadow Arcana: Mirror Image | You learn Mirror Image (2nd level) as an extra spell |
| 7 | Shadow Doubles 2 | 2 doubles — 2 echoes |
| 7 | Shadow Arcana: Displacement | You learn Displacement (3rd level) as an extra spell |
| 9 | Shadow Arcana: Shadow Conjuration | You learn Shadow Conjuration (4th level) as an extra spell |
| 11 | Shadow Arcana: Shadow Evocation | You learn Shadow Evocation (5th level) as an extra spell |
| 13 | Shadow Doubles 3 | 3 doubles — 3 echoes |
| 13 | Shadow Arcana: Phantasmal Putrefaction | You learn Phantasmal Putrefaction (6th level) as an extra spell |
| 15 | Shadow Arcana: Greater Shadow Conjuration | You learn Greater Shadow Conjuration (7th level) as an extra spell |
| 17 | Shadow Arcana: Greater Shadow Evocation | You learn Greater Shadow Evocation (8th level) as an extra spell |
| 19 | Shadow Arcana: Shades | You learn Shades (9th level) as an extra spell |

### Abilities in detail

#### Shadow Echo

- **Activation**: automatic. There is nothing to toggle or click. Cast a qualifying spell and the echoes follow.
- **Which spells echo** — all of the following must hold.
  - The spell is cast from your Sorcerer spellbook. Mythic spells merged into that spellbook count. Other classes' spellbooks, items such as scrolls, wands and potions, and abilities that are not spells do not.
  - It is cast by a direct casting command (yours or the AI's). Spells that some other effect casts automatically do not echo.
  - The cast succeeded.
  - It is not a cantrip.
  - It is harmful to enemies: a spell that can target enemies and either has a harmful effect on them or cannot target allies. Buffs, healing and summoning spells are excluded.
  - If it targets a single creature, that creature is your enemy at the moment of casting. A spell cast on an ally does not echo.
  - You have not already echoed a spell this round.
- **Number of echoes**: one per shadow double. 1 at Sorcerer level 1, 2 at level 7, 3 at level 13. Only Sorcerer class levels count.
- **One spell per round**: only one spell can be echoed each round. In turn-based combat this goes by round number; in real-time combat and outside combat, 6 seconds must have passed since the last echoed spell. A new combat makes it available again.
- **Timing**: once the original spell's effect has finished, the echoes go off one at a time, 0.5 seconds of game time apart.
- **Where the echoes land**
  - Single-target spells: on the same target.
  - Area spells aimed at a point: on the original point. Even if you aimed by clicking an enemy, the echoes land where the spell was first placed, not where that enemy has moved to.
  - Area spells centred on the caster or on a target: on the centre the original cast used.
- **Strength of an echo**: caster level, spell level, saving throw DC and metamagic are those of the original cast. Any metamagic on the original is on every echo. Each echo rolls the spell's own saving throws afresh.
- **Costs**: echoes use no spell slot. Effects that trigger "when you cast a spell" and metamagic buffs that apply "to your next spell" are triggered and spent once, by the original cast only; echoes neither trigger nor consume them.
- **Echoes never produce echoes.**
- **Disbelief (illusion)**: every creature affected by an echo makes a Will save against the original spell's DC.
  - Failure: the echo applies in full.
  - Success: damage is halved, and buffs and debuffs take hold only 50% of the time. Effects that are neither damage nor a buff, such as instant death or dispelling, still apply in full.
  - If the spell is already a shadow spell (Shadow Evocation and the like), the echo halves that spell's own reality percentage: 20% becomes 10%, 60% becomes 30%.
- **Touch spells that hold the charge**: the echo is triggered when the touch is **delivered**, not when you charge the spell.
- **Fading echoes**: in the cases below the remaining echoes are lost, and the combat log shows "Shadow echo fades" with the reason.

| Reason | What is lost |
|---|---|
| target already down | all remaining echoes |
| target no longer valid | all remaining echoes |
| caster down | all remaining echoes |
| original spell did not finish (more than 12 seconds of game time) | all remaining echoes |
| echo cast failed | that one echo |
| reloaded | all pending echoes |

#### Spells Given to Shadow

- Your Sorcerer spellbook has one fewer spell per day at each spell level from 1st to 9th.
- Only the base number from the Sorcerer table is reduced. Bonus spells from your casting ability and extra slots from other effects are untouched.
- The reduction never takes more than the base number, and the result never goes below 0.
- Cantrips (level 0) and spell levels you have not unlocked yet are unaffected.
- It applies only to this archetype's Sorcerer spellbook, not to any other spellbook the character has.

#### Shadow Arcana

- At Sorcerer levels 3, 5, 7, 9, 11, 13, 15, 17 and 19 you automatically learn one spell of spell level 1 through 9. The spells are fixed, not chosen.
- They are added on top of your bloodline spells.
- If you already know the spell, nothing further is added.

| Sorcerer level | Spell level | Spell |
|---|---|---|
| 3 | 1 | Vanish |
| 5 | 2 | Mirror Image |
| 7 | 3 | Displacement |
| 9 | 4 | Shadow Conjuration |
| 11 | 5 | Shadow Evocation |
| 13 | 6 | Phantasmal Putrefaction |
| 15 | 7 | Greater Shadow Conjuration |
| 17 | 8 | Greater Shadow Evocation |
| 19 | 9 | Shades |

### Choices

The archetype adds no choices of its own. You pick a bloodline exactly as a base Sorcerer does and receive its powers, spells and arcana. Only the bloodline feat selections at levels 7, 13 and 19 are missing.

### Feats

- The archetype grants no feats automatically.
- The three bloodline feat slots at levels 7, 13 and 19 are removed.
- The Sorcerer bonus feat at level 1 and your general feat slots are unchanged.
- No feat becomes unselectable, and no feats were added specifically for this archetype.

### Resources

- Shadow Echo uses no resource. Its only limit is one echoed spell per round.
- Spells per day = the base Sorcerer's number minus 1 at each spell level from 1st to 9th, plus ability bonus spells as usual. They are restored by resting, as in the base game.

### Compatibility and notes

- No other mod is required. If the game data is not what the archetype expects (another mod has moved the bloodline feat slots in the Sorcerer progression, or one of the nine extra spells is not on the Sorcerer spell list at the expected level), the archetype does not appear in character creation and the rest of the game runs normally.
- **TabletopTweaks, ExpandedContent, BrawlerClassWrath**: these mods add entries to the bloodline feat selection. The Shadow Sorcerer loses that selection altogether, so there is no conflict.
- **Merged mythic spellbooks**: mythic spells merged into the Sorcerer spellbook (the base game's Lich merge, and Angel, Aeon, Azata, Demon and Trickster spells merged by mods that extend merging) are Sorcerer spellbook spells, so they echo when they qualify.
- **ToyBox**: if you use its spells-per-day multiplier, the reduction of 1 is applied first and the multiplier afterwards.
- **Multiclassing and prestige classes**: the number of doubles depends on Sorcerer class levels only. Advancing the Sorcerer spellbook through a prestige class does not add doubles. Spells gained that way still echo, since they are cast from the Sorcerer spellbook, and the reduction of 1 per day still applies.
- **Spells that do not echo**: teleports and utility spells that do no harm (Dimension Door and the like) are excluded. A few area spells that travel with the caster or a target cannot keep their original centre and are excluded as well: Zone of Predetermination, the Angel spell Repel the Profane and the Trickster spell Breath of Money.
- **Known limitations**
  - Pending echoes are not saved. If the area is reloaded before they go off, they are lost.
  - If the original spell's effect has not finished within 12 seconds of game time, no echoes are fired.
  - A successful disbelief does not reduce effects such as instant death or dispelling.


</details>
