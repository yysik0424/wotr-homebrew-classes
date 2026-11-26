# Homebrew Classes for Pathfinder: Wrath of the Righteous

**English** | [한국어](README.ko.md)

Twenty-one archetypes for *Pathfinder: Wrath of the Righteous* in one Unity Mod Manager mod. Command a pack of animals as a hunter, wear your golem as an alchemist, or fight alongside an ancestral weapon as a barbarian.

> [Download 0.17.32-beta for Unity Mod Manager](https://github.com/yysik0424/wotr-homebrew-classes/releases/download/v0.17.32-beta/HomebrewClasses-0.17.32-beta.zip) · [Release notes and test coverage](release-notes-0.17.32-beta.md)

Turn-based attack odds, defenses and incoming results now appear directly above the combat log and move with it. The panel matches the log width, wraps longer text and uses 80% of the scaled log font size. If there is too little room above the log, the panel overlaps its top edge to stay on screen.

- Hell Rider loses the tactician teamwork feats at levels 1, 9 and 17 and the ability to share them through tactician. Order, challenge and bonus combat feats at levels 6, 12 and 18 remain.
- Falconer loses the first favored terrain, all later terrain selections and increases, and level 12 Camouflage. Favored enemy and quarry remain.
- Chimera Monk regains monk bonus feats at levels 2, 6 and 10, for six selections in total. It gives up the four Style Strike selections at levels 5, 9, 13 and 17.
- Blast Magus gives up level 13 heavy armor proficiency and armored casting in heavy armor. Its level 7 medium armor feature remains.
- Spiritsteel loses all six ranks of Danger Sense at levels 3, 6, 9, 12, 15 and 18. Its level 2 teamwork feat and level 6 Spirit Trip remain.

Release archives preserve DLL timestamps so Unity Mod Manager can distinguish the updated loader from an old cached copy. Spellbook integration retries discovery at the main menu if the blueprint collection changes during its initial scan. Category switching was observed during PC character creation. The level-up screen and buff recast comparison still need separate confirmation.

Active buff icons use the game’s own timer. When you select a buff in turn-based combat, the target preview shows its base duration alongside the time remaining on the current effect. Instant self buffs show the remaining time on the ability icon tooltip. At caster level 7, a one-minute-per-level buff shows a base of 7 minutes, or 14 with Extend Spell when eligible. Random durations show a range. Effects resolved during casting show the available formula or ability description. For area abilities, the preview describes the hovered unit if affected. Healing, dispels, summons and toggles also have turn-based summaries. When the same buff is already active, the preview shows known recast rules, including replacement, extension and stacking. Golem Overdrive, spirit possession and Pack Empowerment use their own class rules for base duration.

In PC character creation and level-up, the class list reserves space for both rows of category buttons. The first class stays below the buttons when the list rebuilds. Click the open category again to collapse it.

In turn-based combat, hover an ability to see its attack, save and spell resistance checks. Select it and point at an enemy to see revealed defenses. Touch attacks show a base hit estimate and the effects of distance bonuses and flat-footed armor class where applicable. Incoming attacks briefly show the attacker, party target and resolved checks.

Bubble Buffs 5.2.3 recognizes Pack Empowerment, Golem Overdrive and spirit possession. Pack Empowerment checks the living pack members individually. Overdrive and possession use the short-buff filter.

Removed Spiritsteel’s favored enemy choice and its bound weapon attack and damage bonuses against the chosen enemy. At level 1, choose your bound weapon and gain Spirit Rage.

Spiritsteel has 50 bound-weapon feats, up from 43, with ten picks by level 20. The new options are Strike in Darkness at 6, Spirit Vigil at 8, Close-Quarters Shot and Disruptive Spirit at 10, Spirit Lunge at 12, Disarming Critical at 14 and Spellcleaving Strike at 16.

The category buttons attach directly to the PC class selection screen. Character creation and level-up group classes into Core, Base, Hybrid, Alternate, Occult and Prestige. Click a category to expand it and click again to collapse. The controller list shows category labels and sorts by category. Archetypes appear under their parent class. Empty categories are disabled. A modded prestige class appears under Prestige. Unrecognized non-prestige classes appear under Base.

Spiritsteel uses Wisdom for daily rage rounds. With Wisdom 18, you get 8 rounds at level 1 and 46 at level 20. Keeping the spirit awake costs one round per round in total.

Spiritsteel automatically gains stun immunity at level 12 and confusion immunity at level 16 while Spirit Rage is active. Two level-16 weapon feats join the selection. Speed grants one extra full-attack strike and overlaps with haste. Thundering Critical adds sonic damage on confirmed critical hits according to the weapon’s critical multiplier.

While the shadow is on, it echoes the first eligible harmful sorcerer spell each round. If that spell is Magic Missile, the original and its echo each spend one 1st-level slot. With one slot left, only the original fires.

## The classes

| No | Archetype | Base class | What it does | Needs |
|---|---|---|---|---|
| 1 | [Pack Warden](#pack-warden) | Hunter | Commands a pack: riding your main companion in combat calls one auxiliary at level 7 and two at level 13, and pack-only spell effects replace spellcasting | — |
| 2 | [Phantom Blade](#phantom-blade) | Fighter | A guardian phantom strikes alongside you and absorbs 25% of the damage you take | — |
| 3 | [Blast Magus](#blast-magus) | Magus | Picks an element, fires blasts with Intelligence and detonates them through weapon hits | *KineticistElementsExpanded* (optional: 5 more elements) |
| 4 | [Spellfist](#spellfist) | Brawler | Unarmed brawler who casts up to 6th-level magus spells from chosen schools | **BrawlerClassWrath** |
| 5 | [Golemancer](#golemancer) | Alchemist | Builds a golem tank, changes its material, and later wears it | DLC 3 |
| 6 | [Warlock](#warlock) | Witch | Unlimited patron blasts that build Overheat and call summons | — |
| 7 | [Weapon-Spirit Shaman](#weapon-spirit-shaman) | Shaman | A floating spirit weapon fights on its own and borrows the magic of stored weapons | — |
| 8 | [Bulwark Mage](#bulwark-mage) | Wizard | Full caster in heavy armor behind a tower shield that strikes back | — |
| 9 | [Chaos Warrior](#chaos-warrior) | Warpriest | Rages, mutates at random (claws, fangs and more), drives enemies mad with stacking pain, and casts one spell as a swift action while raging each combat | — |
| 10 | [Chimera Monk](#chimera-monk) | Monk | Reshapes the body with a new mutation every two levels | **DarkCodex** |
| 11 | [Oathbound Revenant](#oathbound-revenant) | Paladin | An undead paladin whose smites knit the body back together | — |
| 12 | [Shadow Sorcerer](#shadow-sorcerer) | Sorcerer | Toggle a shadow to echo the first eligible spell each round. The echo costs one extra slot of the same level. With one slot left, only the original fires | — |
| 13 | [Grey Seer](#grey-seer) | Arcanist | Cast on the front line with sword and staff, then strike a designated foe. Spend reservoir points according to the spell slot to secure a threatened cast. | — |
| 14 | [Blood Berserker](#blood-berserker) | Bloodrager | Use a blood chain to reel in prey or pull yourself to a larger foe. Bloodrage coats your melee weapons, and their extra blood damage doubles on criticals from level 13. | — |
| 15 | [Sorrow Inquisitor](#sorrow-inquisitor) | Inquisitor | Lift afflictions from allies into a lead rosary and sentence a foe to bear them. Returning one to its source raises its save DC, and from level 16 each bead that takes effect deals divine damage. | — |
| 16 | [Spiritsteel](#spiritsteel) | Barbarian | Inscribe a new trait on one ancestral weapon every even level. Its spirit joins your rage, helps you flank and lends Wisdom to maneuvers such as trip. | — |
| 17 | [Dream Thief](#dream-thief) | Rogue | Put enemies to sleep with Desna’s starknife, cross the battlefield through dreams and wake them with a critical hit. Spend starlight on sleep, a sudden strike or divine weapon damage. | — |
| 18 | [Deathdealer](#deathdealer) | Slayer | Read a studied foe’s actions to riposte, trip a moving enemy or interrupt a spell. Intelligence improves reactions, and nine personal arcane abilities help you prepare for the fight. | — |
| 19 | [Falconer](#falconer) | Ranger | Send a hawk to open a shot for your bow while an animal companion drives prey into its path. From level 8, the hawk can dive automatically after your weapon attack. | — |
| 20 | [Prophecy Breaker](#prophecy-breaker) | Oracle | Defy a foretelling through study. Cast divine spells with Intelligence and reserve two divine casts per unlocked tier for arcane research. Inscribe four spells and prepare two per tier. | — |
| 21 | [Hell Rider](#hell-rider) | Cavalier | Charge on a nightmare to concentrate devil strikes, or plant a standard to hit several foes. Wield Charisma-based Hellfire Ray, Greater Command, Firebrand and Overwhelming Presence. | — |

Full details for every archetype (what is removed, what is gained at each level, every choice) are in [Class details](#class-details) below.

## Requirements

- *Pathfinder: Wrath of the Righteous* (Enhanced Edition, current Steam/GOG version)
- [Unity Mod Manager](https://www.nexusmods.com/site/mods/21). Built and checked against 0.32.4. The minimum version declared in the mod metadata is 0.27.11.
- Optional mods, only for the classes that use them:
  - **BrawlerClassWrath**, required by Spellfist
  - **DarkCodex**, required by Chimera Monk
  - **KineticistElementsExpanded**, adds five more elements to Blast Magus

If a required mod is missing, only that one class is left out. The rest of the mod and the game itself load normally, and the Unity Mod Manager window tells you which class was skipped and why.

## Installation

1. Close the game. If you installed any of the twelve standalone class mods, move their folders outside the game’s `Mods` directory. Keep these backups to restore your previous installation.
2. Select the game in Unity Mod Manager and locate `HomebrewClasses-0.17.32-beta.zip`.
3. Drag the ZIP into Unity Mod Manager or extract it to the game’s `Mods` directory. Check that `Mods/HomebrewClasses` contains `Info.json`, `HomebrewClasses.dll` and a `classes` folder with 21 DLLs.
4. Start the game. The archetypes appear in character creation under their base classes.

**Loading a save made with the standalone mods:** Keep every class used by that save enabled in Homebrew Classes. ModMenu records the bundle as one mod and may list the old mod IDs as missing. Use the normal **Load** button. If you clicked **Disable extra mods**, choose **Cancel**. Choosing **Proceed** can disable Homebrew Classes on the next start. Save in a fresh slot, restart the game, and load that slot before replacing the original.

## Build from source

In Windows PowerShell, set `WOTR_MANAGED_DIR` to the game’s `Wrath_Data/Managed` directory and `WOTR_CSC` to the Visual Studio Roslyn `csc.exe` executable. Install Unity Mod Manager for the game first. Run this command with a new or empty output directory.

```powershell
.\tools\package.ps1 -Version 0.17.32-beta -Out .\out -NoDevCompare
```

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

Open an [issue](https://github.com/yysik0424/wotr-homebrew-classes/issues) with the affected class, steps to reproduce, active mods, `Player.log` and `GameLogFull.txt` from the game log folder.

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
| Spellcasting | None. The Hunter spellbook and orisons are removed entirely. Instead, the Pack Empowerment picks at levels 4, 10 and 16 let you put the effects of some of ten Hunter spells on your pack (as spell-like abilities) |
| Required mods | None |
| Optional integrations | None |
| Alignment / deity restrictions | None added by the archetype |

### Overview

The Pack Warden gives up Hunter spells to fight alongside an animal companion. At levels 1–6 the main companion is your only animal. Riding it in combat calls one auxiliary of the same species at level 7 and a second at level 13. Each follows up an eligible melee attack by the main companion with one attack against the same foe. You support them with aspects and Pack Empowerment.

### What you give up

The Pack Warden no longer gains Studied Target, Swift Studied Target, or Pack's Quarry.

| Level | Base feature | Notes |
|---|---|---|
| 1 | Hunter spellcasting (the whole spellbook) | All 1st- to 6th-level spells are gone, with no way to get the spellbook back. The effects of some spells can be put on your pack only, through Pack Empowerment |
| 1 | Orisons | No 0-level spells either |
| 1 | Animal Companion selection | Replaced by Pack's Main Animal Companion, which offers fewer species (see Choices) |
| 1 | Animal Focus | Replaced by Aspects of the Pack. The focus you could apply to yourself is gone too |
| 8 | Second Animal Focus | Not replaced. The archetype grants nothing at level 8 |

Every Hunter feature not listed here is kept as in the base game, including Hunter Tactics at level 3, the teamwork feats, Raise Animal Companion at level 10 and Master Hunter at level 20.

### What you gain (by level)

| Level | Feature | Effect |
|---|---|---|
| 1 | Pack's Main Animal Companion | Choose one of the base-game animal companions whose base size is Medium or larger. Its advancement, the mount toggle and the animal companion archetype choice work as for a base Hunter |
| 1 | Aspects of the Pack | Two toggles, Fierce Aspect and Tough Aspect. The active aspect gives the main companion and every auxiliary a bonus equal to your Wisdom modifier. Switching is a free action with no limit |
| 4 | Tandem Executioner base feature | The base feature a Tandem Executioner receives at level 4, which the techniques below build on |
| 4 | Tandem Executioner technique (1st) | Pick one technique from the Tandem Executioner list |
| 4 | Pack Empowerment (1st) | Pick one pack empowerment, at this point from the six that correspond to Hunter spells of levels 1–2. Each one you pick can be used twice per day as a move action |
| 7 | Mounted Call | Riding your main companion in combat calls one auxiliary of the same species |
| 7 | Tandem Executioner technique (2nd) | |
| 10 | Tandem Executioner technique (3rd) | |
| 10 | Pack Empowerment (2nd) | One not yet chosen from the seven up to Hunter spell level 3 |
| 13 | Mounted Call | A second auxiliary joins the pack |
| 13 | Tandem Executioner technique (4th) | |
| 16 | Tandem Executioner technique (5th) | |
| 16 | Pack Empowerment (3rd) | One not yet chosen from all ten, up to Hunter spell level 5 |
| 19 | Tandem Executioner technique (6th) | |
| 20 | Both Aspects | Fierce Aspect and Tough Aspect can be active at the same time |

### Abilities in detail

#### Call the Pack

An auxiliary must reach the target with its own melee weapon before it can join the companion’s attack.

- **Activation**: automatic. The moment you enter combat with the call condition met, the auxiliaries appear next to you. It costs no action and no resource.
- **Call condition**
  - At levels 1–6 only the main animal companion joins you.
  - Level 7 and up: you are riding **your own main animal companion**. No other mount counts. Mounting up in the middle of a fight calls the pack at once.
- **Pack size**: 0 auxiliaries at Hunter levels 1–6, 1 at levels 7–12, and 2 from level 13.
- **What an auxiliary is**
  - Species: the same as your main animal companion.
  - Level: Hunter level − 4, minimum 1. Only Hunter class levels count.
  - Size: Pack Auxiliary. Whatever the species' normal size, an auxiliary appears at **Small** size and never grows. Your main companion's size is not changed.
  - Species upgrade: once the auxiliary's level reaches that species' upgrade level (the same value the base game uses for animal companions), it receives the species upgrade feature, minus the size increase; the ability score bonuses, natural armor and the rest are kept. The pre-order bonus horse has no upgrade feature in the base game and so gets none.
  - Pack's Fangs: despite the Small body, natural weapon damage dice are rolled as for the original size (the size it would have had at that level without being fixed at Small). Attack, AC and reach stay those of a Small creature.
  - Immunity to attacks of opportunity: enemies cannot make attacks of opportunity against auxiliaries.
  - It receives the bonus of whichever aspect is active as soon as it appears.
  - It counts as a summoned creature, and it dies outright at 0 hit points instead of dropping into a dying state.
- **When the pack disperses**
  - Always when the party's combat ends.
  - When you leave combat while conscious. While you are down, the pack stays and keeps fighting (ordinary summons vanish when their summoner falls; auxiliaries are exempt).
  - Level 7 and up: when you dismount by choice. Mounting again brings the pack straight back, except for auxiliaries that died in that combat. Entering an area where riding is not allowed also disperses it.
  - Level 7 and up: being thrown off because you or the mount went down, or being dismounted by a cutscene, does **not** disperse the pack.
- **Fallen auxiliaries**: an auxiliary that dies does not come back during that combat (dismounting and mounting again does not change this). In the next combat it appears again at once, with no waiting time. If you leave combat while conscious, that combat counts as over for you and the whole pack appears in your next one.

#### How the auxiliaries fight

- While the main companion is conscious, the auxiliaries **do not attack on their own**. Normally they move to keep up with it.
- While the main companion is fighting, the auxiliaries move up **beside its target**. The target is the enemy the main companion is currently ordered to attack, or, while you ride it, the enemy you are ordered to attack, or else an enemy the main companion attacked in melee within the last 6 seconds (one round), in that order of priority. They only close in; they still do not strike on their own. If there is no room around the target and they cannot reach it, they go back to following the main companion.
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

#### Pack Empowerment

- **When**: one at each of Hunter levels 4, 10 and 16, three in all. The same one cannot be chosen twice. At level 4 you choose from Hunter spell levels 1–2, at level 10 from up to level 3, and at level 16 from up to level 5 (the list is under Choices).
- **Effect**: applies the effect of a Hunter spell that normally targets one creature to **the main animal companion and every living auxiliary at once**. The base-game spell's effect is used as is, so its values, duration and stacking rules are those of the base-game spell.
- **Use**: a move action. Each chosen ability has its own 2 uses per day, which return on rest. It is a spell-like ability with caster level equal to Hunter level.
- **Targets**: there is no range limit. Auxiliaries that appear after the casting are not affected. It cannot be used when the pack has no member (no main animal companion and no auxiliary).
- It can be used while riding the main animal companion, and using it does not dismount you.
- It does not affect you yourself.

#### Tandem Executioner techniques

You gain the Tandem Executioner’s base feature at level 4 and choose techniques at levels 4, 7, 10, 13, 16 and 19.

### Choices

**Pack's Main Animal Companion** — choose one at level 1. "No companion" is not offered.

| Available species | Notes |
|---|---|
| Bear | |
| Boar | |
| Hissing Centipede | It cannot be ridden in the base game. From level 7 the call condition (riding the main companion) can never be met, so no auxiliaries appear |
| Dog | |
| Elk | |
| Horse | The pre-order bonus variant is also in the list |
| Mammoth | |
| Smilodon | The pre-order bonus variant is also in the list |
| Triceratops | The pre-order bonus variant is also in the list |
| Wolf | |

- Base-game species that are not offered: Leopard, Monitor Lizard and Velociraptor. They start at Small size.
- Species that other mods add to the Hunter's companion list do not appear in this archetype's list.
- Each species has the same statistics and advancement as the base-game animal companion. Auxiliaries appear as the species you chose, at Small size.

**Pack Empowerment** — one at each of levels 4, 10 and 16. Each can also be chosen at any later selection level after the first one listed. Every duration scales with Hunter level.

| Pack empowerment | First selection level | Effect | Duration |
|---|---|---|---|
| Pack Empowerment: Magic Fang | 4 | A +1 enhancement bonus on attack and damage rolls with natural weapons and unarmed strikes | 1 minute per Hunter level |
| Pack Empowerment: Bull's Strength | 4 | A +4 enhancement bonus to Strength. It does not stack with other enhancement bonuses to the same ability score | 1 minute per Hunter level |
| Pack Empowerment: Cat's Grace | 4 | A +4 enhancement bonus to Dexterity. It does not stack with other enhancement bonuses to the same ability score | 1 minute per Hunter level |
| Pack Empowerment: Bear's Endurance | 4 | A +4 enhancement bonus to Constitution. It does not stack with other enhancement bonuses to the same ability score | 1 minute per Hunter level |
| Pack Empowerment: Barkskin | 4 | An enhancement bonus to natural armor (AC): +2 at Hunter levels 4–5, +3 at 6–8, +4 at 9–11, +5 at 12–20. It stacks with the creature's own natural armor, but not with other enhancement bonuses to natural armor | 10 minutes per Hunter level |
| Pack Empowerment: Natural Rhythm | 4 | During a full attack, each consecutive hit with a natural weapon against the same target adds +1 damage to the next natural weapon attack of that full attack. A miss or a change of target starts the count again from 0, and attacks of opportunity are not affected | 1 round per Hunter level |
| Pack Empowerment: Magic Fang, Greater | 10 | An enhancement bonus on attack and damage rolls with natural weapons and unarmed strikes: +2 at Hunter levels 10–11, +3 at 12–15, +4 at 16–19, +5 at 20 | 1 hour per Hunter level |
| Pack Empowerment: Freedom of Movement | 16 | Immunity to the paralyzed, slowed, entangled, difficult terrain and unable-to-move conditions, immunity to grapple combat maneuvers, and immunity to movement-impairing and paralysis effects | 10 minutes per Hunter level |
| Pack Empowerment: Blessing of the Salamander | 16 | Fast healing 5, fire resistance 20 and a +2 competence bonus to Combat Maneuver Defense | 1 round per Hunter level |
| Pack Empowerment: Death Ward | 16 | A +4 morale bonus on saving throws against death effects, and immunity to negative energy damage, energy drain and harmful channeled negative energy | 1 minute per Hunter level |

- Animal Growth is not on the list, because it would break the auxiliaries' fixed Small size.
- The effect is the result of running the base-game spell itself. The values above are the base game's; if another mod changes the spell, the actual effect follows the changed spell (see Compatibility and notes).

**Tandem Executioner techniques** — one at each of levels 4, 7, 10, 13, 16 and 19, from the base game's Tandem Executioner list. The list and its effects are unchanged.

### Feats

- The archetype grants no feats automatically.
- The Hunter's bonus feat slots (teamwork feats and so on) are unchanged.
- No feat becomes unselectable. Without a spellbook, though, Hunter levels cannot satisfy feats or prestige class requirements that call for spellcasting.
- No feats were added specifically for this archetype.

### Resources

- Call the Pack and both aspects are unlimited. A fallen auxiliary returns at once in the next combat, so there is no recall delay either.
- **Pack Empowerment**: 2 uses per day for each chosen ability, counted separately, returning on rest. The number does not grow with Hunter level or ability scores.

### Compatibility and notes

- Pack Warden uses five Tandem Executioner feature and buff blueprints. If the installed game data lacks any of these, the bundle skips Pack Warden and gives the reason in the mod settings.
- No other mod is required. If the game data is not what the archetype expects (a patch or another mod has changed the Hunter progression, the animal companion list or the animal companion upgrades), the archetype does not appear in character creation and the rest of the game runs normally.
- **Mods that add animal companions (ExpandedContent and the like)**: the added species stay available to a base Hunter but are left out of the Pack Warden's list.
- **TabletopTweaks**
  - The archetype recognises the way TabletopTweaks-Base reworks the size increase of animal companion upgrades, and removes only the auxiliaries' size increase, just as it does with the base-game data.
  - Pack Empowerment runs the original spell's effect, so spells that TabletopTweaks-Base fixes (Freedom of Movement, Death Ward and others) apply as fixed. The component TabletopTweaks-Base adds to Death Ward is accepted. The base-game Freedom of Movement also grants immunity to staggering, but TabletopTweaks-Base removes it; since this depends on whether that mod is installed, it is left out of the in-game description and the table above.
- **When a pack empowerment is disabled**: if a spell's game data is not in a shape this mod knows (a game patch or another mod has changed that spell), only that pack empowerment is removed from the selection list, and a character who already chose it cannot use it (a message gives the reason, and the mod log has a detailed line). If so many are disabled that a selection level would have nothing to pick, the whole archetype does not appear in character creation.
- **Pre-order bonus animals**: the pre-order bonus variants of the Horse, Smilodon and Triceratops are part of the selection list.
- **Multiclassing and prestige classes**: the number and level of auxiliaries, the switch to Mounted Call at level 7, the Tough Aspect's temporary hit points, and the selection levels, caster level and durations of Pack Empowerment all use Hunter class levels only. With no spellbook, Hunter levels contribute nothing to the requirements or spell progression of caster prestige classes.
- **Two Pack Wardens in one party** each keep their own pack and their own aspects.
- **Known limitations**
  - From level 7 you cannot call the pack unless you can ride your main companion. The base-game rule that a mount must be larger than its rider still applies, and if you chose the Hissing Centipede, which cannot be ridden in the base game, no auxiliaries appear from level 7.
  - Auxiliaries exist only during combat. Outside combat you travel with the main companion alone.
  - For an ability attack that does not go through a weapon slot, if it cannot be determined which natural weapon the main companion used, that swing is not followed.
  - The pack empowerment descriptions are written from the base-game spell values. If another mod changes a spell's values, the description does not follow.
  - The vanishing effect when auxiliaries disperse can look cut short, and it does not play for auxiliaries that are not Small, Medium or Large (under a size-changing effect, for instance). Rules are unaffected.

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

The Phantom Blade is a fighter bound to a Guardian Phantom. You fight in melee as usual while a shadowy figure overlapping your body drives force fists into whatever you attack. The same phantom soaks 25% of the damage you take into its own absorption pool. The pool refills when you finish a rest or gain a level. Ghost Slash, your ranged attack, does not draw on it: it shares daily uses with Guardian Phantom Full Assault. In exchange you give up bravery, armor training and armor mastery; bonus feats, weapon training and weapon mastery are untouched.

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
| 2 | Phantom Blade — the Guardian Phantom and its absorption pool | Grants the Manifest Guardian Phantom toggle (free action, no cost, on by default). While it is manifest, the pool takes 25% of the damage you would suffer. Pool maximum = Phantom Blade level × Charisma modifier (minimum 1) × 2. The pool refills only when you finish a rest or gain a level |
| 3 | Guardian Phantom Rush — once per round | When one of your melee attacks hits, the phantom strikes the same target once with its fist. Up to 1 time per round. Damage = fist dice + Charisma modifier, force |
| 5 | Ghost Slash: 1 Projectile | Standard action, 30 ft., 1 force projectile (ranged touch). Usable Charisma modifier times per day (minimum 1). Does not spend the absorption pool |
| 6 | Guardian Phantom Ranged Guard | +2 AC against ranged attacks while the phantom is manifest |
| 7 | Guardian Phantom Rush — twice per round | The per-round limit rises to 2 (still one blow per attack) |
| 10 | Ghost Slash: 2 Projectiles | Ghost Slash fires 2 projectiles |
| 10 | (Rush rule change — no separate feature) | The phantom now follows up even when your attack **misses**. It makes its own melee touch attack (base attack bonus + Charisma modifier against touch AC). A missed attack still uses up one of that round's follow-ups |
| 11 | Guardian Phantom Rush — 3 times per round | Per-round limit of 3 |
| 14 | Guardian Phantom Ranged Guard (second step) | The AC bonus against ranged attacks rises to +4 in total |
| 15 | Guardian Phantom Rush — 4 times per round | Per-round limit of 4 |
| 15 | Guardian Phantom Full Assault | Swift action, 1 round. For that round Rush has no per-round limit, and the phantom makes **2** touch blows for each melee attack you make. Shares daily uses with Ghost Slash equal to your Charisma modifier (minimum 1) |
| 15 | Ghost Slash: 3 Projectiles | Ghost Slash fires 3 projectiles |
| 19 | Guardian Phantom Rush — 5 times per round | Per-round limit of 5 |
| 20 | Ghost Slash: 4 Projectiles | Ghost Slash fires 4 projectiles |

### Abilities in detail

#### Manifest Guardian Phantom

- A toggle. Turning it on or off is a free action, it costs nothing, and it stays on through rest. It is already on when you gain it at level 2.
- Rush, absorption, Ranged Guard, Ghost Slash and Full Assault all work **only while the phantom is manifest**.
- While manifest, a shadowy figure overlaps your character, follows you, swings along with Rush and Full Assault, and on a Ghost Slash dashes up to the target, swings and returns. When you walk, it plays a walking animation at your speed. It is purely cosmetic: it cannot be selected or attacked, takes no part in combat, does not block movement and is never written to your save. It hides when you are stealthed or invisible.

#### Absorption pool (Phantom Blade Absorption Pool)

- While the phantom is manifest, the pool takes **25%** of the damage you would suffer. The absorbed amount is rounded down and can never exceed what is left in the pool.
- Absorption applies to the damage that remains **after** damage reduction, resistances and immunities.
- Pool maximum = Phantom Blade level × Charisma modifier (minimum 1) × 2. Example: level 10 with a +4 Charisma modifier gives 80.
- The pool refills only when you finish a rest or gain a level. It does not refill when combat ends.
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
- Uses: **Charisma modifier times per day (minimum 1)**, one per use. They are restored by rest and on level-up. It cannot be used when no uses remain.
- Ghost Slash gains the daily uses at fighter level 5. Guardian Phantom Full Assault draws from the same uses starting at level 15. Each Ghost Slash costs one use regardless of projectile count.
- Visuals: when you use Ghost Slash, the phantom dashes to just in front of the target, swings once per projectile, then snaps straight back to your side. If the target is already down or gone, it swings once in place instead. This is cosmetic and does not affect the rolls.
  - The projectiles themselves are not shown in flight; only their impact effect appears, so it looks as if the phantom delivers the strike itself.
  - The phantom leaves short-lived black cloud afterimages along its path: up to 3 on the way out and up to 3 on the way back, spaced about 1.5 m apart. A shorter path leaves none.
  - Each swing shows a force impact flash on the target. Once the target is down, the remaining swings show no flash.
  - While the phantom is hidden because you are stealthed or invisible, neither the afterimages nor the flashes appear. If one of these effects fails to load, only that effect is skipped; Ghost Slash itself works as normal.
- Ghost Slash does not trigger Rush.

| Fighter level | Projectiles | Damage per projectile |
|---|---|---|
| 5–7 | 1 | 1d8 + Cha |
| 8–9 | 1 | 1d10 + Cha |
| 10–11 | 2 | 1d10 + Cha |
| 12–14 | 2 | 2d6 + Cha |
| 15 | 3 | 2d6 + Cha |
| 16–19 | 3 | 2d8 + Cha |
| 20 | 4 | 2d10 + Cha |

#### Guardian Phantom Ranged Guard

- While the phantom is manifest, you gain +2 AC against ranged attacks at level 6, rising to +4 in total at level 14.
- The bonus is untyped and stacks with everything. It applies to ranged weapon attacks and to ranged touch attacks such as rays, but not to melee attacks.

#### Guardian Phantom Full Assault

- Swift action, lasts 1 round. Usable only while the phantom is manifest.
- For that round the per-round limit on Rush is lifted, and the phantom makes **2** touch blows on **every** melee attack you make, hit or miss. These blows do not count against the normal Rush limit.
- Spend one use from the same daily total as Ghost Slash. When the total reaches zero, both techniques are unavailable until recovery.

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
| Phantom Blade Absorption Pool | Phantom Blade (fighter) level × Charisma modifier (minimum 1) × 2 | On rest and on level-up. Not when combat ends |
| Guardian technique uses (Ghost Slash and Guardian Phantom Full Assault) | Charisma modifier (minimum 1) in total, one per activation of either technique | On rest and on level-up |

With a +5 Charisma modifier, two Ghost Slashes leave three Full Assaults. The separate absorption pool tracks damage prevented and reports its remaining points in the combat log.

### Compatibility and notes

The Guardian Phantom’s lower-body smoke uses a separate fade on particle materials that support tint.

- **Required mods and DLC**: none. However, the black cloud afterimage of Ghost Slash borrows an effect from game data tagged DLC3 (Treasure of the Midnight Isles). If that effect cannot be loaded, only the afterimage is skipped; Ghost Slash and its other visuals are unaffected.
- **Ghost Slash projectile look**: Ghost Slash borrows the projectile of the base-game spell Ray of Enfeeblement and hides only the flight of projectiles fired by a Phantom Blade's Ghost Slash. Ray of Enfeeblement cast by anyone else looks as usual. Because the game reuses projectile visuals, the mod checks every Ray of Enfeeblement projectile once, whoever fired it, and restores any that it left hidden.
- **Engine patch**: for the phantom's walking animation, this archetype adds one Harmony patch (a prefix and a postfix) to the unit animation update. It does nothing for any unit other than the phantom. Another mod that changes the same update may affect the phantom's walking animation only. If the patch is not applied, the mod log shows one error line and only the walking animation stops; the phantom's position and all ability rules are unaffected.
- **TabletopTweaks-Base**: if its Advanced Armor Training option has rearranged the fighter progression, the Phantom Blade recognises that layout and removes everything that sits in those slots. If it meets a fighter progression that is neither the base game's nor TabletopTweaks' (another mod has moved or removed armor training at levels 3, 7, 11 or 15, bravery, or armor mastery), the archetype is not registered and will not appear in the archetype list.
- **Multiclassing and prestige classes**: The Rush limit, fist dice, Ghost Slash, Ranged Guard and the pool size all look at **fighter levels** only. Levels in other classes help only through base attack bonus, which the phantom uses for its attack rolls.
- **Characters from an earlier version**: Guardian Phantom Refill, formerly gained at level 10 (pool refill when combat ends), has been removed. A character who already has it keeps it as a hidden feature with no effect.
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

The Blast Magus fits elemental detonations between blade and spell. At 1st level you pick one element, fire a kineticist-style ranged blast driven by Intelligence, and keep your melee weapon charged with extra damage of the same element. From 4th level you can spend 1 arcane pool point to load your next melee hit with a full blast's worth of damage. Spell Combat and Spellstrike are untouched, so a single hit can carry weapon damage, Energize Weapon, a touch spell and Blast Strike at once. The price is your cantrips, spell recall and the arcane pool's weapon enhancement. From level 20, the blast from Blast Strike deals its ordinary damage even when the weapon critically hits.

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
| 13 | Heavy Armor (Ex) | Retain the level 7 medium armor proficiency and armored casting. |

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
- Action and range (the six base-game elements): standard action, 30 feet. Damage is 1d6 per blast rank, plus half your Intelligence modifier for energy elements or your blast rank + your Intelligence modifier for physical elements. For KineticistElementsExpanded elements, that mod sets the range and damage.
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
- Critical hits: a confirmed weapon critical doubles Blast Strike damage through magus level 19. If fortification negates the critical, the explosion deals ordinary damage. From level 20 the blast deals normal damage. The weapon and any delivered touch spell follow their own critical rules.
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

- Spell Penetration and Greater Spell Penetration each add +2 to Elemental Blast and Blast Strike checks against spell resistance. Mythic Spell Penetration adds half your mythic rank (rounded down), or your full rank with Greater Spell Penetration. Energize Weapon makes no spell resistance check.
- Ascendant Element lets damage of the selected energy type bypass resistance and immunity to that type. It applies to matching damage from ranged blasts, Blast Strike and Energize Weapon. A blast subject to spell resistance must still pass that check.

- Feats granted automatically: none.
- Blast Pool qualifies for Extra Arcane Pool and Abundant Arcane Pool. DarkCodex’s Limitless Arcane Pool also removes Blast Strike’s arcane pool cost.
- Feats you can no longer take:
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
| Spellcasting | Changed — the non-casting brawler gains an arcanist-style arcane spellbook (Intelligence, up to 6th-level spells) |
| Required mods | BrawlerClassWrath (without it this archetype does not appear) |
| Optional integrations | Spellbook Merge 1.7.1 |
| Alignment, deity and other restrictions | None. It cannot be combined with the brawler mod's eight other archetypes (Wild Child, Venomfist, Snakebite Striker, Steel-Breaker, Turfer, Beast Wrestler, Exemplar, Mutagenic Mauler) |

### Overview

The Spellfist is a brawler who carries arcane spells in the same body as the fists. At 1st level you pick three arcane schools, then prepare and cast only magus spells of those schools the way an arcanist does, reaching 6th-level spells at 16th level. A single arcane reservoir lets you pull spells forward as swift actions and powers a set of arcanist exploits. From 2nd level you hold a touch spell in your hand and discharge it through your unarmed flurry. You keep the brawler's bonus combat feats and flurry, and give up maneuver training, Knockout and Awesome Blow.

The main book can merge on the same mythic paths as the Magus book. Select Spellfist to advance arcane casting through a prestige class. Brawler Martial Training counts Brawler levels as Fighter and Monk levels for feat prerequisites.

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
| 1 | Arcane School ×3 | Choose three different schools out of eight. Only magus spells of those schools enter your spellbook. The choice is permanent. |
| 1 | Spellfist spellbook | Arcanist-style arcane spellbook. Intelligence-based, cantrips included, caster level equals brawler level. |
| 1 | Arcane Reservoir | Maximum = 3 + half your brawler level (rounded down) + Intelligence modifier. Refills fully on rest. |
| 1 | Reservoir Quicken | Toggle. In combat, spells are cast as swift actions; each successful cast drains points equal to the spell level (minimum 1). |
| 2 | Fist Spellstrike | Hold a touch spell from your Spellfist spellbook and deliver it with an unarmed strike (flurry included) or a melee weapon attack. |
| 3 | Spellfist Exploits | Choose one exploit (1st) |
| 4 | — | 2nd-level spells |
| 7 | — | 3rd-level spells |
| 7 | Spellfist Exploits | Choose one exploit (2nd) |
| 10 | — | 4th-level spells |
| 11 | Spellfist Exploits | Choose one exploit (3rd) |
| 11 | Greater Exploits | Greater Spell Resistance becomes available |
| 13 | — | 5th-level spells |
| 15 | Spellfist Exploits | Choose one exploit (4th) |
| 16 | — | 6th-level spells |
| 19 | Spellfist Exploits | Choose one exploit (5th) |

### Abilities in detail

#### Spellfist spellbook

- It works like the base game's arcanist spellbook: you prepare spells each day and then spend spell slots freely among the spells you prepared. Spell slots per day and the number of spells you can prepare follow the Magus progression for the same level.
- It is an arcane spellbook, the casting ability is Intelligence, and your caster level equals your brawler level.
- You do not pick spells known. You automatically know every magus spell of your three schools, up to the highest spell level you can cast. When a new spell level opens, all spells of that level from your three schools arrive at once.
- There are no spell picks on level-up and you cannot copy scrolls into this spellbook. Spells outside your three schools, and universal spells, never enter it.
- The archetype grants nothing that removes arcane spell failure from armor.

When spell levels open:

| Brawler level | 1–3 | 4–6 | 7–9 | 10–12 | 13–15 | 16–20 |
|---|---|---|---|---|---|---|
| Highest spell level | 1st | 2nd | 3rd | 4th | 5th | 6th |

Existing characters keep spells they already learned. Retraining and selecting schools again builds the book from the new Magus list.

Cantrips are available from 1st level, but only if one of your schools has any (see the table under Choices).

#### Arcane Reservoir

- A resource you gain automatically. Maximum = 3 + half your brawler level (rounded down) + Intelligence modifier.
- Examples: 6 points at level 1 with a +3 modifier, 13 at level 10 with +5, and 22 at level 20 with +9.
- It refills to the maximum when you rest, and also when you level up.
- It pays for two things: Reservoir Quicken and exploits.

#### Reservoir Quicken

- A toggle. Switching it costs no action, it starts off, and it stays on through a rest.
- In combat, the toggle quickens spells whose level is no higher than your remaining reservoir points. The Spellfist reaches its highest spell level, 6th, at brawler level 16. With 4 points left, you can quicken spells up to 4th level.
- A successfully quickened Spellfist spell spends reservoir points equal to its spell level (minimum 1). A 6th-level spell, the highest tier from ordinary class progression, costs 6 points. At level 20 with Intelligence 28, 22 points pay for three such quickenings with 4 points left.
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
  - values that depend on caster level (Energy Shield resistance, save DCs) use character level.
- Their cost is paid from the arcane reservoir.
- The full list is in the exploit table under Choices.

### Choices

#### Arcane School (level 1, choose three)

You make the same choice three times at 1st level and cannot pick a school twice. The universal school is not offered. The numbers below are for the base game's magus spell list with no other mods, and show how many spells each school brings at each spell level.

| School | 0 | 1 | 2 | 3 | 4 | 5 | 6 | Total |
|---|---|---|---|---|---|---|---|---|
| Abjuration | 0 | 1 | 0 | 1 | 2 | 0 | 1 | 5 |
| Conjuration | 2 | 3 | 4 | 2 | 1 | 2 | 2 | 16 |
| Divination | 0 | 1 | 0 | 0 | 0 | 0 | 1 | 2 |
| Enchantment | 1 | 0 | 0 | 1 | 0 | 0 | 0 | 2 |
| Evocation | 3 | 4 | 3 | 4 | 5 | 2 | 3 | 24 |
| Illusion | 0 | 2 | 3 | 1 | 2 | 0 | 0 | 8 |
| Necromancy | 2 | 1 | 0 | 2 | 0 | 1 | 1 | 7 |
| Transmutation | 0 | 5 | 5 | 7 | 4 | 3 | 9 | 33 |

- The Magus list has no abjuration, divination, illusion or transmutation cantrips.
- Universal spells (one cantrip in the base game) belong to no school and cannot be learned.
- The choice cannot be undone.

#### Exploits (levels 3, 7, 11, 15, 19 — choose five)

Every exploit is paid for from the arcane reservoir. Values use your brawler level unless marked as based on character level. Exploits that allow a saving throw have DC = 10 + half your character level (at most 9) + your Intelligence modifier.

| Exploit | Action and cost | Effect | Requirements |
|---|---|---|---|
| Dimensional Slide | Move action, 1 reservoir point | Teleport to a point within 30 feet. The distance does not grow with level. | — |
| Arcane Barrier | Swift action, 1 reservoir point and up | Gain temporary hit points equal to your brawler level + your Intelligence modifier. Lasts 1 minute per brawler level, or until the temporary hit points are gone. Using it again replaces the temporary hit points and the duration (they do not stack). The cost rises by 1 with each use, up to 3, and returns to 1 when you rest. | — |
| Energy Shield | Standard action, 1 reservoir point | Each use, choose acid, cold, electricity or fire and gain resistance to that energy. Resistance (by character level): 10 at levels 1-4, 15 at 5-9, 20 at 10-14, 25 at 15-19, 30 at 20. Lasts 1 minute per brawler level. | — |
| Force Strike | Standard action, 1 reservoir point | Strike one enemy within 30 feet with force. It always hits: no attack roll and no saving throw. Damage: 1d4 force + your brawler level. | — |
| Spell Resistance | Standard action, 1 reservoir point | Gain spell resistance equal to 6 + your brawler level for a number of rounds equal to your Intelligence modifier. | — |
| Greater Spell Resistance | — | Whenever you use the Spell Resistance exploit, the spell resistance is 11 + your brawler level instead. It is not a separate ability; duration and cost are unchanged. | Brawler level 11, Spell Resistance exploit |
| Shadow Veil | Standard action, 1 reservoir point | Every attack against you has a 20% miss chance, and you gain a +5 bonus on Stealth checks. Lasts your Intelligence modifier + 1 rounds. It does not grow with level. | — |
| Wooden Flesh | Standard action, 1 reservoir point | Gain a +2 natural armor bonus to AC and damage reduction equal to your Intelligence modifier against physical damage that is not slashing (DR N/slashing), and count as a plant creature while it lasts. Lasts 1 minute per brawler level. | — |
| Armored Mask | Standard action, 1 reservoir point | Gain the effect of mage armor (+4 armor bonus to AC for 1 hour per brawler level). If you already have mage armor, you instead gain shield of faith for 1 minute per brawler level (deflection bonus by character level: +2 at levels 1-5, +3 at 6-11, +4 at 12-17, +5 from 18). With the Archmage Armor mythic ability you get its improved version of mage armor instead. The armor bonus does not stack with the armor bonus of worn armor; only the higher one applies. | — |
| Ice Missile | Standard action, 1 reservoir point | Ranged touch attack against one enemy within 30 feet. Cold damage (1 + (brawler level - 1) / 2, rounded down)d6 + your Intelligence modifier: 2d6 at level 3, 4d6 at 7, 6d6 at 11, 8d6 at 15, 10d6 at 19. A target that is hit and fails a Fortitude save is staggered for 1 round. The damage applies regardless of the save. | — |
| Lightning Lance | Standard action, 1 reservoir point | Ranged touch attack against one enemy within 30 feet. Electricity damage with the same dice as Ice Missile. A target that is hit and fails a Fortitude save has a 20% miss chance on all of its attacks for 1 round (it is not blinded). The damage applies regardless of the save. | — |

- Ice Missile and Lightning Lance: if you also have Powerful Exploit (Elemental Focus — Water / Air), which an Elemental Master arcanist gains at level 9 (through multiclassing), the damage dice become d8 (the number of dice stays the same).

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
| Arcane Reservoir | 3 + half your brawler level (rounded down) + Intelligence modifier | Fully on rest, fully on level-up | Reservoir Quicken (spell level, minimum 1), exploits (1 point; Arcane Barrier 1 to 3) |
| Spell slots | Magus table for the same level | Rest | Casting spells |

The reservoir is the same resource as the base game arcanist's arcane reservoir. Unlike the arcanist's, it refills completely on rest rather than halfway.

### Compatibility and notes

- **Without BrawlerClassWrath**: the game starts normally and only this archetype is left unregistered (it does not appear at character creation). One error line is written to the mod log.
- **Other mods that add brawler archetypes**: this archetype knows, and blocks combination with, only the brawler mod's eight original archetypes. If the brawler class has any other archetype added to it, this archetype is not registered.
- **Mods that change the magus spell list**: spells another mod adds to the magus list are sorted into the Spellfist school lists by their school (universal spells are skipped). This includes mods that add spells after this one (for example CharacterOptionsPlus): on the first frame after the game starts, the school lists are rebuilt from the magus list as it is then. The spells added or removed are logged in one line in the mod log; if the magus list cannot be read, the school lists built at load are kept and one error line is written to the mod log. If a mod removes a base-game magus spell from the list or moves it to another level or school, this archetype is not registered.
- **DLC spells**: DLC spells on the magus list are included in the school lists. Without the DLC the game hides them, exactly as it does for a magus.
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

The Golemancer is an alchemist who builds a golem and sends it to the front. From level 1 you can call a wood golem to fight at your side, then dismiss it while travelling. Enemies near the deployed golem take a penalty on attacks against anyone else. At levels 5, 10 and 15 you pick a new material, which replaces the golem's look, size, immunities and special abilities wholesale. From level 5 you can also use Golem Overdrive, injecting a prepared extract into the golem to give it extra slams and a school-based effect for a short time. From level 10 you can wear the golem as a swift action and tank in person, with the golem's hit points soaking damage before yours. You pay for it with fewer bombs per day, three opposition schools, and the loss of poison resistance, poison immunity and the cognatogens.

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
| 5 | Golem Overdrive | Swift action, spends one prepared extract (you pick its level and school). For extract level × 2 rounds the golem makes 2 slams per round (3 from level 11, 4 from level 16) and gains an extra effect based on the spent extract's school |
| 10 | Golem Material | Replace the material with gold, autumn or summer |
| 10 | Golem Fusion | Swift-action toggle. Wear the golem: gain its shape, size, two slams, natural armor, DR and Strength, and take damage on the golem's hit points first |
| 11 | (Golem slam count increase — no separate feature) | The golem's normal slams rise from 1 to 2 per round |
| 15 | Golem Material | Replace the material with adamantine, greater gold or mithral |
| 20 | Shared Immunity | While fused, you also gain the current material's additional immunities (not the common construct immunities) |

### Abilities in detail

#### Golem

**Call Golem and Dismiss Golem** are available from level 1. Your golem waits until called. Call it to your side as a standard action, including during combat. Outside combat, send it away until you need it again. Both commands preserve the same companion, its current hit points, material, level and animal companion slot. Fusion, overdrive and mounted use must finish before calling or dismissing it.

- **Gained**: level 1, automatically. The golem is a companion unit that occupies the animal companion slot and levels up with you (Hit Dice = alchemist level).
- **Type**: construct. It has the common construct immunities (sleep, paralysis, fear, mind-affecting effects, poison and so on). Alchemical healing does not mend it. It does not have the base game wood golem's blanket spell immunity.
- **Chassis**: d10 Hit Dice and full base attack bonus, like base game constructs. The golem does not pick feats.
- **Attacks**: slams. Normally one slam per round at levels 1–10 and two from level 11. This costs nothing and always applies. Golem Overdrive raises the number further. The extra slams use the same slam dice. While fused you make two slams (possibly more during a Golem Overdrive).

| Alchemist level | Golem — normal | Golem — overdrive | Fused Golemancer — normal | Fused Golemancer — overdrive |
|---|---|---|---|---|
| 1–4 | 1 | — | — | — |
| 5–9 | 1 | 2 | — | — |
| 10 | 1 | 2 | 2 | 2 |
| 11–15 | 2 | 3 | 2 | 3 |
| 16–20 | 2 | 4 | 2 | 4 |

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
  - Two slams, using the golem's slam dice for its current material and level. During a Golem Overdrive you use the overdrive number instead (minimum 2 — 3 from level 11, 4 from level 16).
  - You gain the golem's natural armor and DR from the tables above (the DR is bypassed by the same material as the golem's).
  - You gain a Strength bonus equal to the golem's Strength modifier (rounded up when the golem's Strength is odd). Examples: gold at level 10 (Str 33) gives +12 Strength; mithral at level 20 (Str 44) gives +17.
  - The taunting aura moves to you.
  - The material's special abilities (breath weapons, auras, prismatic surge, fast healing and so on) stay with the golem. The material's additional immunities are shared while fused only from level 20, through Shared Immunity.
- **Shared hit points**: damage you take while fused is applied to the golem's hit points first. When they run out the fusion ends, the golem is broken, and the remaining damage hits you. If you end the fusion yourself, the golem returns with whatever hit points it has left.
- **Unavailable while fused**: spells, extracts, bombs and mutagens. A mutagen you drank before fusing keeps working. Emergency Repair and Golem Overdrive can still be used.
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

#### Golem Overdrive

- **Activation**: from level 5, swift action. It has 54 variants, one per extract level (1st–6th) and school (the eight schools plus No School). It can be used outside combat and while fused.
- **Cost**: one prepared, unspent extract of that level and school. A two-slot opposition-school extract counts as one. There is no separate limit on uses per day.
- **Duration**: spent extract level × 2 rounds. The remaining duration keeps running after combat ends.

| Extract level | 1st | 2nd | 3rd | 4th | 5th | 6th |
|---|---|---|---|---|---|---|
| Duration (rounds) | 2 | 4 | 6 | 8 | 10 | 12 |

- **Effect 1 — slam count**: for the duration the golem makes 2 slams per round at levels 5–10, 3 at levels 11–15 and 4 at levels 16–20 (see the table under Golem).
- **Effect 2 — school effect**: for the duration the golem gains the effect below, based on the school of the spent extract.

| School of the spent extract | Effect for the duration |
|---|---|
| Transmutation | Strength +4 (enhancement bonus) |
| Conjuration | Fast healing 5 |
| Abjuration | AC +4 (armor bonus) |
| Divination | Attack rolls +2 (insight bonus), critical confirmation rolls +4 |
| Illusion | 20% concealment (attacks against the golem miss 20% of the time) |
| Necromancy | Slams deal +1d6 negative energy damage on a hit |
| Enchantment | Enemies hit by a slam are shaken for 1 round (no save, mind-affecting) |
| Evocation | Slams deal +2d6 electricity damage on a hit |
| No School | No additional effect (only the slam count rises) |

- **While fused**: you receive the slam count and the school effect instead of the golem. Your slams follow the overdrive number, with a minimum of 2. If you start or end the fusion during an overdrive, the effects move between you and the golem.
- **Cannot be used** while an overdrive is already running (the remaining duration cannot be overwritten or extended), while the golem is missing or broken, while the fusion is in the middle of changing, or when you have no prepared extract of that level and school.
- **Ends when** the duration runs out, or at once if the golem is broken or lost. Repairing the golem afterwards does not bring the ended overdrive back.

#### Halved Bombs

- Bombs per day = (alchemist level + Intelligence modifier) ÷ 2, rounded down, minimum 0. Extra bombs from feats, equipment and mythic sources are added on top, unchanged.
- Temporary Intelligence bonuses do not count toward the base, as in the base game.
- Examples: level 4 with Intelligence 18 gives 4 bombs (8 for a base alchemist). Level 1 with Intelligence 10–11 gives 0; Intelligence 12 gives 1.

#### Golemancer Opposition School

- Chosen three times at level 1 from abjuration, conjuration, divination, enchantment, evocation, illusion and necromancy (transmutation is not an option). Each school can be picked once.
- Extracts of a chosen school take two slots to prepare. They are not forbidden.

#### Shared Immunity

- Level 20, automatic. Only while fused, you share the current material's additional immunities:
  - Adamantine: immunity to critical hits and to precision damage such as sneak attacks
  - Greater Gold: immunity to confusion and petrification
  - Mithral: immunity to electricity, evasion (no damage instead of half on a successful Reflex save)
- The common construct immunities (sleep, paralysis, fear, mind-affecting, poison and so on) and the material's special abilities are not shared. Immunities you already have are left alone and are not removed when the fusion ends. The shared immunities themselves end with the fusion.

### Choices

#### Golem Material

At each of levels 5, 10 and 15 you choose one of three materials. The new material replaces the old one: the golem's look, size, immunities, special abilities and numbers all change. The ratios multiply the baseline table (natural armor / DR / slam / Strength).

| Level | Material | Size | DR bypassed by | Ratios | Additional immunities | Special abilities |
|---|---|---|---|---|---|---|
| 1 | Wood | Medium | Adamantine | 1.00 / 1.00 / 1.00 / 1.00 | — | Splintering (save DC based on the golem's level) |
| 5 | Stone | Large | Adamantine | 0.90 / 0.75 / 0.97 / 0.91 | Petrification, acid | Slowing Aura |
| 5 | Iron | Large | Adamantine | 1.00 / 1.12 / 0.97 / 1.04 | Electricity, fire | Breath weapon (poison cloud) |
| 5 | Brass | Large | Adamantine | 1.10 / 1.12 / 1.06 / 1.04 | Fire | Breath weapon (incendiary cloud), see invisibility |
| 10 | Gold | Large | Adamantine | 0.89 / 1.29 / 1.35 / 1.13 | Blindness, confusion | Prismatic surge (touch) |
| 10 | Autumn | Large | Nothing (DR/—) | 1.05 / 0.86 / 0.82 / 0.93 | Cold, freedom of movement (constant) | Aura of thorny despair, constricting vines — save DCs based on the golem's level |
| 10 | Summer | Large | Nothing (DR/—) | 1.05 / 0.86 / 0.82 / 0.93 | Fire, freedom of movement (constant) | Aura of the summer heat, constricting vines — save DCs based on the golem's level |
| 15 | Adamantine | Huge | Nothing (DR/—) | 0.93 / 1.00 / 0.88 / 0.95 | Critical hits, precision damage such as sneak attacks | Fast healing 5, Improved Critical (Slam) — see Known limitations |
| 15 | Greater Gold | Large | Adamantine | 0.79 / 1.00 / 0.96 / 1.00 | Confusion, petrification | Prismatic surge (ranged), Cleave |
| 15 | Mithral | Large | Nothing (DR/—) | 1.29 / 1.00 / 1.17 / 1.05 | Electricity, evasion | Combat Reflexes, fast healing 20, speed 50 feet (other materials 30 feet) |

**Material special abilities in detail**

| Ability | Material | Effect |
|---|---|---|
| Splintering | Wood | Free action. 6d6 physical damage to enemies within 20 feet of the golem (Reflex half). The save DC follows the golem's level |
| Slowing Aura | Stone | Enemies entering within 10 feet of the golem are slowed for 7 rounds on a failed Will save |
| Breath weapon (poison cloud) | Iron | Free action. Creates a 5-foot-radius poison cloud at a target within touch range for 1 round. Any creature in it, friend or foe, is poisoned on a failed Fortitude save (1d4 Constitution damage each round for 4 rounds, Fortitude negates) |
| Breath weapon (incendiary cloud) | Brass | Swift action. Creates a 10-foot-radius incendiary cloud at a target within touch range for 1d6 rounds. Each round every creature in it except the golem, friend or foe, takes 6d6 fire damage (Reflex half) |
| See invisibility | Brass | Constant |
| Prismatic surge (touch) | Gold | Swift action, touch range, no attack roll. One of six effects at random: 20 fire / 40 acid / 80 electricity damage (Reflex half for each), poison (Fortitude; 1d4 Constitution damage each round for 6 rounds), petrification (Fortitude, permanent), insanity (Will, permanent) |
| Prismatic surge (ranged) | Greater Gold | Swift action, needs a ranged touch attack to hit. Same effects as the touch version |
| Aura of thorny despair | Autumn | Enemies entering within 10 feet of the golem are entangled for 1d4+1 rounds on a failed Reflex save and suffer crushing despair for 1d4+1 rounds on a failed Will save. DCs follow the golem's level |
| Aura of the summer heat | Summer | Enemies within 10 feet of the golem, on entering and every round, are entangled for 1d4+1 rounds on a failed Reflex save and fatigued on a failed Fortitude save (no time limit). DCs follow the golem's level |
| Constricting vines | Autumn, Summer | A slam that hits attempts a grapple (+4 on the check). Success deals 2d10 + 1.5 × Strength modifier and grapples; the grappled enemy cannot move and takes 4d10 + 2 × Strength modifier each round. The golem cannot attack while grappling |
| Fast healing 5 / 20 | Adamantine / Mithral | Regains 5 / 20 hit points each round |
| Improved Critical (Slam) | Adamantine | Base game feat. See Known limitations |
| Cleave | Greater Gold | Base game feat |
| Combat Reflexes | Mithral | Base game feat |

- The breath weapons, prismatic surges, auras and constricting vines are the corresponding base game golems' abilities. The wood golem's splintering and the autumn and summer auras and constricting vines have been adjusted so their save DCs scale with your golem's level.
- While the golem is broken it loses what the material granted through ongoing effects (fast healing, freedom of movement, see invisibility, auras); they return when it is repaired.

#### Extract spent on Golem Overdrive

Each time you use Golem Overdrive you choose the level (1st–6th) and school of the extract to spend. The level sets the duration and the school sets the extra effect. The effects by school are listed in the table under Abilities in detail — Golem Overdrive.

#### Opposition schools

Three of Abjuration, Conjuration, Divination, Enchantment, Evocation, Illusion and Necromancy. Every option has the same effect: extracts of that school take two slots to prepare. An opposition school's Golem Overdrive effect can then only come from an extract prepared in two slots.

For reference, the 71 extracts on the base game alchemist list by school, with representative extracts:

| School | Extracts | Representative extracts (level) | Golem Overdrive effect |
|---|---|---|---|
| Transmutation | 29 | — (cannot be an opposition school) | +4 Strength |
| Conjuration | 14 | Cure Light Wounds (1), Restoration, Lesser (2), Heal (6) and others | Fast healing 5 |
| Abjuration | 12 | Shield (1), Resist Energy (2), Stoneskin (4) and others | +4 AC |
| Divination | 4 | True Strike (1), See Invisibility (2), True Seeing (6) and others | +2 attack, +4 to confirm criticals |
| Illusion | 4 | Blur (2), Invisibility (2), Displacement (3) and others | 20% concealment |
| Necromancy | 4 | False Life (2), Death Ward (4), Eyebite (6) and others | +1d6 negative energy |
| Enchantment | 3 | Aid (2), Heroism (3), Rage (3) | Shaken for 1 round |
| Evocation | 1 | Dragon's Breath (4) | +2d6 electricity |

The counts are for the base game list; extracts added by other mods are not included.

Opposition schools apply only to your Golemancer extracts. They do not affect a spellbook gained from another class through multiclassing, your bombs, mutagens or discoveries, or the use of scrolls, wands and potions. You can still use opposition-school extracts; they just take two slots to prepare.

### Feats

- Feats granted automatically: none. Bonus feat slots are unchanged.
- Options you can no longer take: the Cognatogen discovery, and with it Greater Cognatogen and Grand Cognatogen, which require it. This applies to discovery selections and to the Extra Discovery feat.
- Feats added specifically for this archetype: none.
- The golem does not choose feats. Depending on its material it gains Improved Critical (Slam) (adamantine), Cleave (greater gold) or Combat Reflexes (mithral).

### Resources

- **Bombs**: (alchemist level + Intelligence modifier) ÷ 2, rounded down, plus extra bombs from other sources, per day. Restored by resting, as in the base game.
- **Emergency Repair**: no pool of its own. It spends prepared extracts of 1st–6th level, one per use.
- **Golem Overdrive**: no pool of its own. Each use spends one prepared extract of 1st–6th level and lasts that level × 2 rounds. Only one overdrive can run at a time.
- **Golem Fusion**: no uses per day and no duration. The practical resource is the golem's hit points.
- **Golem hit points**: fully restored by a successful rest, which also revives a broken golem. Some materials add fast healing (adamantine 5, mithral 20).

### Compatibility and notes

During overdrive, changing or losing a fusion form updates the master’s and golem’s limbs and school effects after the form change finishes.

- **No animal companions**: the golem uses the animal companion slot. Combining it with a class or feature that grants an animal companion (druid, hunter and so on) leaves the golem non-functional and logs a conflict error in the mod log.
- **Requires DLC 3.** The bundle skips Golemancer if the brass golem data is unavailable. The brass material uses the brass golem unit and model added by DLC 3 (The Treasure of the Midnight Isles). All other materials (wood, stone, iron, gold, autumn, summer, adamantine, greater gold, mithral) use base game golems. The cognatogen lockout also covers the Extra Discovery feat selection that ships with DLC 5 (The Lord of Nothing) content.
- **Other mods**: if another mod moves or removes Poison Resistance at alchemist levels 2, 5 or 8 or Poison Immunity at level 10, this archetype is not created at all, rather than being created half-working. The golem's level is computed directly from your alchemist level, so mods that change animal companion levels do not affect it.
- **Multiclassing and prestige classes**: the golem's Hit Dice and numbers, its slam count (normal and overdrive), the bomb base and the fusion numbers all read alchemist level only. Levels in other classes do not advance them.
- **Known limitations**
  - At level 1 with Intelligence 10–11 you may have 0 bombs per day.
  - The adamantine golem's Improved Critical (Slam): the base game feat's weapon type list does not include the golem's slam type, so its threat range is not increased at present.
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

The Warlock gives up every hex in exchange for an at-will elemental ray, the blast, shaped by her patron. The patron you pick at level 1 places you in one of six groups (Hell, Abyss, Fey, Storm, Death, Star), and the group decides your blast element, the creatures you can call and your defensive ward. Every blast that hits builds 1 Heat (up to 9), and Heat is spent as a swift action to summon the patron's servants. When Heat reaches 9, the strongest creature you can call appears on its own. When combat ends, half of your Heat (rounded down) remains, and a full rest resets it to 0. You cast spontaneously with Charisma like a Stigmatized Witch, and you keep your patron spells and familiar.

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
| 1 | Heat | +1 each time a blast hits (maximum 9). When combat ends half of it remains (rounded down); a full rest resets it to 0. Heat that remains can also be spent outside combat |
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

Loading an existing character with several patrons and no recorded first choice preserves the current pact and logs a warning.

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
- When combat ends, Heat does not drop to 0; half of it remains (rounded down — for example 7 → 3, 5 → 2, 1 → 0). This applies once each time the warlock herself leaves combat, which in real-time mode includes dropping out of combat by falling unconscious. If she was not in combat to begin with, Heat does not change.
- A completed rest resets Heat to 0 (an interrupted rest changes nothing).
- Heat that remains stays on you as a buff outside combat, and it can be spent on Heat Summoning outside combat as well.

#### Heat Summoning

- Use: swift action, supernatural ability, close range, targeted at a point. Each group has its own "<Group> Heat Summoning" ability with nine variants, I to IX.
- Requirements: Heat at least equal to the tier, and the tier no higher than the highest spell level of your warlock spellbook (for example up to tier III at witch level 6, and tier IX from level 18). Otherwise the variant is unavailable. These are the only two requirements, so if you have Heat left you can use it outside combat too.
- Cost: Heat equal to the tier.
- Effect: summons one creature of that tier. Any warlock summon you already have vanishes (one at a time per warlock; other warlocks' summons and ordinary summoning spells are unaffected).
- Duration: 1 round per witch level; doubled from level 16. From level 20, a tier IX summon stays until combat ends and then vanishes.
- Patron's Servant: warlock summons cannot be directly controlled. They fight on your side on their own.
- From level 4 they benefit from Augment Summoning (+4 Strength and Constitution).
- Patron's Favor: the tier IX summons of the Hell, Storm and Death groups gain a further +4 Strength and Constitution (stacks with other bonuses). A summon without a Constitution score, such as an undead, gains +4 Charisma instead of Constitution (undead hit points are based on Charisma). The Death group's tier IX Graveknight is such a summon, so it gains +4 Strength and Charisma.

#### Automatic summon

When a blast hit brings Heat to 9, the highest tier you can summon (the lower of 9 and your highest spell level) is summoned at once without spending Heat, and Heat resets to 0. Anything above 9 is lost. The automatic summon also replaces your current warlock summon. If you reach 9 again while an automatic summon is still resolving, Heat stays at 9 and the summon happens on your next blast hit.

If you save after Heat resets but before the creature appears, loading that save restores your Heat to 9. You can spend it yourself or land another blast to trigger the automatic summon. A save made after the creature appears keeps the summon and its normal Heat balance.

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

| Resource | Maximum | Gained by | Reduced / reset |
|---|---|---|---|
| Heat | 9 | +1 per blast hit (+2 on a confirmed critical hit from level 12) | Halved (rounded down) when combat ends, 0 after a completed rest, 0 on an automatic summon |

- Heat left over after combat persists outside combat and can be spent on Heat Summoning.

- Blast and Double Blast are usable at will.
- Heat Summoning has no daily limit; it only costs Heat.
- Spell slots match the Stigmatized Witch (spontaneous, Charisma-based).

### Compatibility and notes

- Summons without a dismissal effect still leave on the native removal timer. If a character has conflicting patrons, the existing pact is preserved and the log identifies the character and the reconciliation stage.

**Archetype overlap**
- Warlock and Stigmatized Witch cannot be taken together, and neither can any other witch archetype that replaces the same features.
- Only the Stigmatized Witch's spellbook and cantrips are borrowed; you do not receive the Stigmatized Witch's curse.

**Prestige and mythic spellbooks**

Select the Warlock option to advance your Charisma-based Stigmatized Witch book, including on the arcane side of Mystic Theurge. Each prestige class retains its entry requirements. Spellbook Merge 1.7.1 paths that support the Witch book can also merge the Warlock’s main book.

**Other mods**
- With TabletopTweaks-Base’s Loremaster, select the Stigmatized Witch book. Its alternate witch capstone selection at level 20 contains no hexes, so warlocks keep it. A patron gained through its Second Patron option does not change your group.
- PrestigePlus spellbook selections recognize an owned Stigmatized Witch book.
- ExpandedContent: the Stargazer's "witch spellbook" option is blocked for warlocks (picking it would create a separate witch spellbook instead of advancing the warlock's own). Nothing happens if ExpandedContent is not installed.
- CharacterOptionsPlus: works alongside the Winter Witch archetype restriction that mod adds to the prestige spellbook options.
- If another mod adds witch patrons, those patrons are not on the group table; a warlock whose only patron is one of them gets no blast, summons or ward.
- If another mod changes the base game's prestige "witch spellbook" options in an unrecognized way, the Warlock may fail to be created (a `Warlock:` error appears in the mod log).

**DLC**
- The Abyss group's tier III summon uses the Abrikandilu from DLC 5 (The Lord of Nothing). If that unit is unavailable, the tier III option is omitted. Automatic summoning at this tier uses tier II.

**Multiclassing**
- Blast dice, caster level, summon duration and the Star group's spell resistance count witch levels only. Levels in other classes add nothing.

**Known limitations**
- The Storm group's blast deals no damage to demons, which are immune to electricity (Heat still builds).
- Tier IX of the Hell, Storm and Death groups is a creature of the same rank as tier VIII; in exchange it gains Patron's Favor (+4 Strength and Constitution a second time; +4 Strength and Charisma for the Death group's Graveknight).
- Fey tiers III and IV summon the same creature (Medium Water Elemental).
- Version 0.9.1-beta and earlier did not record pending automatic summons. Heat already lost in an older save cannot be recovered.
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

The Weapon-Spirit Shaman trades the spirit animal for a spirit dwelling in a weapon: the Weapon Spirit. It is a floating weapon that fights beside you on its own, much like an animal companion. Put magic weapons into the Weapon Spirit's spare weapon sets and their magic is mirrored onto the weapon it is currently wielding, so spare magic weapons become fuel rather than vendor trash. From level 5 you can call on Spirit Possession a few times per day to take that mirrored power into your own weapon for a short while, at level 18 you can call a Spirit Barrage once per day, in which the Weapon Spirit attacks one enemy once with each of its spare melee weapons. Your shaman spells, spirit and hexes are untouched.

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
| 1 | Weapon Spirit | Choose one of up to 59 weapon categories. A Weapon Spirit wielding a masterwork weapon of that category becomes your companion and takes weapon feats with that category (below). The shaman gains no weapon feats |
| 1 | Spirit Mirror | An ability of the Weapon Spirit. The magic of weapons in its spare weapon sets is mirrored onto the weapon it wields. Mirror count B = shaman level / 4 (rounded down) + 1 |
| 4 | Weapon Spirit Weapon Specialization | The Weapon Spirit gains Weapon Specialization with the chosen category (ignoring prerequisites) |
| 8 | Weapon Spirit Greater Weapon Focus | The Weapon Spirit gains Greater Weapon Focus with the chosen category (ignoring prerequisites) |
| 5 | Spirit Possession I | Swift action, 1/day, 3 rounds. The power mirrored onto the Weapon Spirit's weapon moves to your own primary weapon (or empty hand) |
| 10 | Spirit Possession II | 2/day, 5 rounds. Adds flight (immune to ground effects and trip) during possession |
| 12 | Weapon Spirit Greater Weapon Specialization | The Weapon Spirit gains Greater Weapon Specialization with the chosen category (ignoring prerequisites) |
| 15 | Spirit Possession III | 3/day, 7 rounds. Adds +2 to hex DCs during possession |
| 18 | Spirit Barrage | Swift action, 1/day. Choose one enemy: the Weapon Spirit attacks it once with each melee weapon in its spare weapon sets (at most 6 attacks). Every attack uses the Weapon Spirit's highest attack bonus, and damage is that spare weapon's own |
| 20 | Spirit Possession IV | 4/day, 10 rounds. Same effects as Spirit Possession III |

### Abilities in detail

#### Weapon Spirit

You must choose one weapon category at level 1.

- Shaman: gains no weapon feats or proficiencies.
- Weapon Spirit: takes Weapon Focus and Improved Critical with the chosen category (automatic feats), plus Weapon Specialization at shaman level 4, Greater Weapon Focus at 8 and Greater Weapon Specialization at 12, ignoring prerequisites.
- Weapon Spirit: appears wielding a masterwork weapon of the chosen category. For double weapons, both heads are masterwork.

Weapon Spirit traits:

| Item | Value |
|---|---|
| Type | A companion that occupies the animal companion slot. It advances with your shaman level the same way an animal companion does |
| Base ability scores | Str 13, Dex 15, Con 15, Int 2, Wis 12, Cha 6 |
| Size / speed | Medium / 50 ft. |
| Flight | Always airborne — immune to ground effects and trip |
| Natural armor | +5 |
| Proficiencies | Every weapon category in the selection (up to 59), light and medium armor. No shield proficiency |
| Equipment slots | Uses all four weapon sets. Cannot use the boots, two ring, glasses and shirt slots |
| Automatic feats | The Weapon Focus and Improved Critical that animal companions take for their bite are taken for the chosen weapon category instead. They keep that target even if the Weapon Spirit wields a weapon of another category |
| Death | If it falls, it returns after you rest |
| Portrait | The base-game Animated Weapons portrait. The companion slot on the party bar shows a small version of the same picture |

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
- What is mirrored: properties that only change the weapon's own rules (for example extra energy damage, critical range or multiplier, alignment or material) and on-hit triggered effects (for example the acid spell of Black Dragon's Fang).
- Triggered effects: only one at a time (the first in set and slot order); it takes one special-property slot, and one that would not fire on the spirit's weapon (wrong weapon type or range) is skipped. The Weapon Spirit is the caster of a mirrored triggered effect, so spell DCs use the Weapon Spirit.
- What is not: effects that depend on the wielder's or caster's state, equipment effects for the wearer (stats, features and so on), and masterwork quality. Temporary enhancements from spells and the like vanish when a weapon is moved to a spare set, so they are not mirrored either.
- Where it goes: the main-hand weapon of the active set; if there is no main-hand weapon, the off-hand weapon; if both hands are empty, the unarmed strike. If the hands hold only non-weapons (such as a shield), nothing is mirrored.
- When it is recalculated: when the Weapon Spirit's equipment changes, when it switches weapon sets, and when the shaman levels up.
- Each recalculation writes one line to the mod log listing what was mirrored and why anything was left out.

#### Spirit Possession

- Use: swift action, supernatural ability, targets yourself. Costs one possession use.
- Requirements: the Weapon Spirit must be alive, visible and within 30 feet.
- Effect: the power of the Weapon Spirit's spare sets is placed on your primary weapon under the mirror's rules (count B, one enhancement, one triggered effect, ghost touch). If your main hand is empty, it goes to your unarmed strike.
  - Unlike the mirror, you also receive wielder-dependent effects and equipment effects for the wearer (stats, features). A triggered effect must suit your weapon's type and range.
  - Effects tied to the original weapon blueprint, features for an animal companion, effects with side effects on removal, and spellbook grants are not taken.
  - Enhancements do not stack. Properties your weapon already has are skipped.
  - Changing weapons ends the borrowed power.
  - If your main hand holds a non-weapon (such as a shield), you receive nothing and the use is still spent.
- The Weapon Spirit keeps fighting during the possession.

| Shaman level | Tier | Uses per day | Duration | Effect |
|---|---|---|---|---|
| 5–9 | Spirit Possession I | 1 | 3 rounds | Mirrored power on your primary weapon |
| 10–14 | Spirit Possession II | 2 | 5 rounds | + flight (immune to ground effects and trip) |
| 15–19 | Spirit Possession III | 3 | 7 rounds | + hex DC +2 |
| 20 | Spirit Possession IV | 4 | 10 rounds | Possession III effects |

#### Spirit Barrage

Level 18. A separate ability from possession, with its own use (1/day) rather than the possession uses.

- Use: swift action, supernatural ability, targets one enemy. Costs one Spirit Barrage use.
- Requirements:
  - The Weapon Spirit must be alive, visible and within 30 feet of the shaman.
  - At least one of the Weapon Spirit's spare slots must hold a melee weapon.
  - The target must be within the melee reach of at least one of those spare melee weapons. Reach is measured from the Weapon Spirit's position. The distance and line of sight between the shaman and the target do not matter.
- Effect: the Weapon Spirit attacks the target once with each melee weapon in its spare weapon sets (the three sets not currently in use, up to six slots). At most 6 attacks, taken in weapon set order and, within a set, main hand before off hand.
  - Each attack is rolled separately, and all of them use the Weapon Spirit's highest attack bonus.
  - Damage comes from that spare weapon's own dice, enhancement and special properties. The magic that Spirit Mirror places on the currently wielded weapon is not added.
  - If an earlier attack drops the target, the remaining weapons do not attack.
  - The Weapon Spirit's normal attacks and actions are unaffected. Its weapon sets and equipment layout do not change either.
- Slots that do not take part: empty slots, ranged weapons, shields, non-weapons, weapons carrying a temporary enchantment, weapons that cannot reach the target, and weapons that leave a trace when wielded (a weapon that leaves an effect on the Weapon Spirit merely by being picked up and put down).
- Usable during a possession.
- Log: one line in the combat log gives the number of attacks, the number of hits and why any slot was left out, and one line in the mod log gives the result for each weapon that attacked and the slots left out.

### Choices

The level 1 Weapon Spirit weapon category (up to 59 options). Natural weapons and categories without a weapon model are excluded. In game the options appear under the base game's weapon names.

| Group | Categories |
|---|---|
| Melee | Dagger, Punching Dagger, Kukri, Starknife, Shortsword, Handaxe, Sickle, Kama, Sai, Nunchaku, Light Mace, Light Hammer, Light Pick, Club, Longsword, Rapier, Scimitar, Dueling Sword, Estoc, Falcata, Sawtooth Sabre, Bastard Sword, Battleaxe, Dwarven Waraxe, Heavy Mace, Flail, Warhammer, Heavy Pick, Tongi, Trident, Shortspear, Greatsword, Greataxe, Greatclub, Falchion, Elven Curved Blade, Earth Breaker, Heavy Flail, Scythe, Spear, Longspear, Glaive, Bardiche, Fauchard, Quarterstaff |
| Double weapons | Double Axe, Double Sword, Hooked Hammer, Urgrosh |
| Shield spikes | Spiked Light Shield, Spiked Heavy Shield |
| Thrown | Dart, Javelin, Throwing Axe |
| Ranged | Longbow, Shortbow, Light Crossbow, Heavy Crossbow, Sling Staff |

Every option does the same thing: a Weapon Spirit wielding a masterwork weapon of that category, and that category becomes the target of the Weapon Spirit's five weapon feats (Weapon Focus, Improved Critical, Weapon Specialization, Greater Weapon Focus, Greater Weapon Specialization). Whatever you pick, the Weapon Spirit is proficient with all available categories, so you can hand it a different weapon later.

On the selection screen each option has its own multi-line description. It lists the starting weapon's melee or ranged type, whether it is one-handed, two-handed, light or a double weapon, its damage dice, critical range and multiplier, and its melee reach or range in feet; double weapons also list the second head's damage and critical. Ranged categories add a line noting that ranged weapons take no part in Spirit Barrage. These numbers are read from the starting weapon values the game actually uses, so if another mod changes that weapon type, the changed values are shown. The selection screen's overall description and the level-table descriptions of Spirit Possession II-IV also state uses per day and duration in rounds.

### Feats

- The shaman gains no automatic weapon feats.
- The Weapon Spirit takes Weapon Focus and Improved Critical with the chosen category as automatic feats.
- At shaman levels 4, 8 and 12 the Weapon Spirit gains Weapon Specialization, Greater Weapon Focus and Greater Weapon Specialization with the chosen category, ignoring prerequisites. These count shaman levels, not the Weapon Spirit's own level.
- No change to bonus feat slots.
- The Weapon Spirit occupies the animal companion slot. If you already have an animal companion, the Weapon Spirit selection is unavailable.
- Because you lose Wandering Spirit and Wandering Hex, options that require them are unavailable.
- No feats exclusive to this class are added.

### Resources

| Resource | Formula | Recovery |
|---|---|---|
| Spirit Possession uses | 1 at shaman level 5, +1 every 5 levels after that (1 at level 5 / 2 at 10 / 3 at 15 / 4 at 20) | Resting |
| Spirit Barrage uses | A fixed 1 per day from shaman level 18 (not increased by level or ability scores) | Resting |

- Spirit Barrage has its own use, separate from the possession uses.
- Only shaman levels count.
- Spirit Mirror uses no resource.

### Compatibility and notes

- The spirit weapon is checked for the eidolon subtype even when Making Friends registers it after the shaman.

**Animal companions**
- The Weapon Spirit uses the animal companion slot. It cannot be combined with classes or feats that grant an animal companion some other way (if you already have one, the Weapon Spirit cannot be chosen).

**Other mods**
- This archetype removes Spirit Animal (level 1), Wandering Spirit (level 4) and Wandering Hex (levels 6 and 14) from the shaman progression. If another mod changes the shaman progression so that these features are no longer at those levels, the class is not created.
- If TabletopTweaks or a similar mod changes the animal companion progression, the Weapon Spirit follows the changed progression.
- If another mod changes a starting weapon into something the option description cannot express (a size other than Medium, a touch attack, damage without dice, and so on), that option alone shows a description stating the reason instead of the numbers, and the mod log gets one error line. Class registration and the other options are unaffected.
- Known additions that other mods attach to the animal companion class or to basic weapons (ExpandedContent, MAKING FRIENDS, ToggleableThrowingWeapons) are recognized and not carried over to the Weapon Spirit. An unrecognized addition may prevent the class from being created (a `WeaponSpiritShaman:` error appears in the mod log).

**DLC**
- The Sawtooth Sabre option requires both its weapon type and reward item to exist in the installed game data. If either is missing, the other weapon choices remain available. When present, the starting weapon retains the original A Dance of Masks reward condition.
- The other 58 categories are built from standard base game weapons.

**Multiclassing**
- Mirror count B and the possession tier, uses and duration count shaman levels only. Spirit Barrage is likewise gained at shaman level 18.

**Known limitations**
- The old level-20 Spirit Fusion has been removed. Characters that already have it lose the ability on load; Spirit Possession IV stays.
- Characters from earlier versions: the Weapon Focus the shaman used to get is removed on load when it can be confirmed as coming from this mod (otherwise it stays), and a Weapon Spirit already past levels 4, 8 or 12 receives the matching feats on load.
- When possession or Spirit Barrage is unavailable, the tooltip shows all requirements as a single sentence; it does not tell you which one failed.

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
| Repulse | Pushes the attacker about 10 feet (3 meters) away (does not provoke attacks of opportunity) | None |
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
- **Repulse distance**: the push is converted to the game's internal meters and comes out at 3 meters, slightly short of 10 feet (about 9.8 feet).
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

The Chaos Warrior is a warrior priest touched by divine madness. You give up one blessing, fervor, channel energy and sacred armor, and in return you can fly into a chaos rage from 1st level. Every rage warps your body at random with several of 32 mutations: fangs, claws, tails and other natural attacks may sprout, mixed with helpful, harmful and two-sided mutations, and a mutation drawn twice stacks. Every melee weapon hit you land on an enemy while raging stacks pain, and at eight stacks the enemy must make a Will save or become confused. You can still cast spells while raging, and once per combat, the first eligible warpriest spell cast while raging is cast as a swift action, so you buff up, wade straight into the front line and grind the enemy formation into chaos.

Chaos Rage qualifies for Limitless Rage. With it, you can enter rage at zero charges and reroll your mutations each time. Frenzied Casting can quicken one eligible spell per combat.

### What you give up

| Level | Base feature | Notes |
|---|---|---|
| 1 | Second blessing selection | You choose one blessing instead of two. The first selection (within your deity's domains) and the blessing uses are unchanged |
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
| 1 | Frenzied Casting (part of Chaos Rage) | Once per combat, the first eligible warpriest spell you cast while raging is cast as a swift action. Spell level cap 1 (2 at 7th level, 3 at 10th, 4 at 13th, 5 at 16th, 6 at 19th) |
| 1 | Pain (part of Chaos Rage) | Each melee weapon hit on an enemy while raging adds 1 stack: -1 attack rolls, saves and skill checks per stack (at most -5) for 3 rounds. At 8 stacks the target makes a Will save (DC 10 + half warpriest level + Wisdom modifier) or is confused for 1d4 rounds |
| 1 | Mutations of Madness | Each time you start a rage, 1-3 mutations (up to 4-8 at higher levels) are drawn at random from 32. A mutation drawn again stacks. All end with the rage |
| 3 | Chaos Rage: magic natural weapons | While raging, your natural weapon attacks count as magic |
| 7 | Chaos Rage: cold iron and silver natural weapons | While raging, your natural weapon attacks also count as cold iron and silver |
| 10 | Chaos Rage: chaotic and evil natural weapons | While raging, your natural weapon attacks also count as chaotic and evil, whatever your own alignment |
| 10 | Spreading Madness | When pain reaches its threshold (8 stacks), other enemies within 15 ft of the target make the same Will save |
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
- Entering rage rolls your Mutations of Madness. If you have not used Frenzied Casting this combat, you can quicken one eligible spell. Your melee hits apply Pain while raging.
- Natural weapons: while raging, your natural weapon attacks (from mutations, and also racial or polymorph natural attacks) count as the weapons below, cumulatively by warpriest level. Manufactured weapons and unarmed strikes (including a monk's fists) are not affected. This only matters for checks that read weapon properties, such as damage reduction, incorporeal creatures and regeneration; there is no bonus on attack or damage rolls. Adamantine, good, lawful and DR x/- are not covered. The tier is set by your level when the rage starts and ends with the rage.

| Warpriest level | Natural weapons count as |
|---|---|
| 3 | Magic |
| 7 | Magic, cold iron, silver |
| 10 | Magic, cold iron, silver, chaotic, evil |

#### Frenzied Casting

- Starting a rage gives you Frenzied Casting if you have not spent it this combat. The first warpriest spell at or below the cap that you cast during this rage is cast as a swift action, and casting it uses up the effect. The effect also ends with the rage.
- Any target is fine (the spell need not target yourself). The spell still uses its spell slot.
- Spell level cap by warpriest level: 1 at levels 1-6, 2 at 7-9, 3 at 10-12, 4 at 13-15, 5 at 16-18, 6 at 19-20. The level is the level of the spell slot (metamagic cost included). A spell with variants counts at the level of the spell you choose the variant from. A spell above the cap is cast normally and the effect remains.
- A warpriest spell is any spell on the warpriest spell list: cast from another spellbook it still counts and uses up the effect. A spell that is already a swift action, or one prepared with Quicken Spell, also uses it up.
- If you have already used your swift action this round and still have your standard action, the spell is cast as a standard action and the effect is still used up (if both are used, it waits for your swift action).
- Ending an unused rage preserves the opportunity. After you spend it, restarting rage or loading a mid-combat save leaves it spent until the next combat.

#### Mutations of Madness

- Triggers automatically whenever you start a chaos rage. First the number of draws is rolled uniformly from the range below, then that many draws are made one at a time. Each draw picks uniformly among the mutations that can be drawn at that moment, so the same mutation can come up again.

| Warpriest level | Number of mutations |
|---|---|
| 1-5 | 1-3 |
| 6-10 | 2-5 |
| 11-15 | 3-6 |
| 16-20 | 4-8 |

- Stacking: most mutations add their effect again when drawn again (see the stacking column in the Choices tables).
- A mutation that would do nothing now is not drawn (that draw is made among the others):
  - Natural attacks whose slots are full: one bite, one gore, one tail, and two claws and pincers combined. The same attacks from your race, a polymorph or other effects count. The other natural attacks (Extra Arm, Tentacle, Stinger, Wings, Hooves, Talons, Neuro-Stinger) have no limit.
  - Tough Hide, Horrific Form and Long Limbs once already drawn (once per rage).
- In a rage begun while polymorphed, the 12 natural attack mutations do not appear (which makes the other mutations more likely).
- Natural attacks: each adds one 1d6 natural attack to your full attack. While you hold a weapon it is a secondary attack (-5 on the attack roll, half the Strength bonus).
- All mutations end when the rage ends. They do not change during a rage (a rage that is suppressed and then resumes keeps the mutations it already has). If you level up during a rage and your features are reapplied, the mutations are drawn again from scratch.

#### Pain

- When it stacks: every time the chaos warrior **hits with a melee weapon attack while raging** (natural attacks and attacks of opportunity count, ranged attacks do not) and the creature hit is an **enemy**. Hitting an ally applies nothing.
- How much per hit: 1 stack, plus 1 for each time Amplified Pain was drawn, plus 1 for each time Pain Resonance was drawn, plus 1 if the hit was with the Neuro-Stinger.
- Effect per stack: -1 on attack rolls, -1 on all saving throws and -1 on skill checks (Athletics, Mobility, Persuasion, Trickery, Stealth, Perception, Use Magic Device, Knowledge (Arcana), Knowledge (World), Lore (Nature), Lore (Religion)). The penalty grows only up to 5 stacks (-5) and stays at -5 at 6-7 stacks.
- Duration: 3 rounds after the most recent stack; every new stack resets it to 3 rounds. Pain already applied stays for its remaining duration even after your rage ends.
- Stacking rules:
  - Pain is tracked per chaos warrior. Two chaos warriors hitting the same enemy build separate stacks and reach the threshold separately.
  - If a creature carries pain from several chaos warriors, the penalties do not add together; only the largest applies.
- Threshold (8 stacks): the moment one chaos warrior's pain reaches 8 or more stacks (it triggers only once even if a single hit jumps past the threshold):
  1. That chaos warrior's pain is removed from the target, whether or not the save succeeds.
  2. The target makes a Will save. DC = 10 + half warpriest level (rounded down) + Wisdom modifier.
  3. On a failure it is confused for 1d4 rounds (the base game's confusion condition).
- Creatures immune to mind-affecting effects: pain still builds but stops at 8 stacks. There is no save, no confusion and the pain is not removed (the -5 penalty lasts under the normal 3-round rule).
- Dispel magic removes only the penalty; the stack count stays, and the next hit restores the penalty to match it.
- The target's buff tooltip does not show the stack count.

#### Spreading Madness

- Automatic from 10th level. When pain reaches its threshold and the target makes its Will save, every **other enemy** within 15 ft of the target (alive and not immune to mind-affecting effects) makes a Will save against the same DC. Each one that fails is confused for 1d4 rounds.
- Allies are never affected. Nearby enemies do not need any pain stacks of their own.

### Choices

There is nothing to choose beyond the base warpriest's first blessing. Below are the 32 mutations (12 natural attacks, 10 beneficial, 5 harmful, 5 two-sided) that are applied at random when you start a rage. The stacking column says what happens when the same mutation is drawn again in the same rage.

Natural attack mutations (12) - each adds one 1d6 natural attack to your full attack. While you hold a weapon it is a secondary attack (-5 to hit, half the Strength bonus). They do not appear in a rage begun while polymorphed.

| Mutation | Attack | Stacking |
|---|---|---|
| Extra Arm | Slam 1d6 | One more attack each time |
| Fangs | Bite 1d6 | One bite per character (race and other effects count); not drawn while full |
| Claws | Claw 1d6 | Claws and pincers share a limit of 2; not drawn while full |
| Horns | Gore 1d6 | One gore; not drawn while full |
| Tail | Tail 1d6 | One tail; not drawn while full |
| Tentacle | Tentacle 1d6 | One more attack each time |
| Stinger | Sting 1d6 (no poison) | One more attack each time |
| Wings | Wing 1d6 (no flight) | One more attack each time |
| Hooves | Hoof 1d6 | One more attack each time |
| Talons | Talon 1d6 | One more attack each time |
| Pincer | Pincer 1d6 | Claws and pincers share a limit of 2; not drawn while full |
| Neuro-Stinger | Sting 1d6; a hit stacks 1 more pain | One more attack each time |

Beneficial mutations (10)

| Mutation | Effect | Stacking |
|---|---|---|
| Enlarge | Size increases by one category; +2 size bonus to Strength | Size stays; +2 Strength more each time |
| Tough Hide | DR 2/-, +1 per 5 warpriest levels (DR 6/- at 20th). Only the highest of this and other damage reduction applies | Once per rage |
| Regeneration | Fast healing 2, +1 per 5 warpriest levels (fast healing 6 at 20th) | Another fast healing effect is added each time |
| Swiftness | +10 ft speed, +2 Reflex saves | Applies again |
| Thorn Skin | Creatures that hit you with a melee attack take 1d6 piercing damage | One more die each time |
| Chaos Blood | +2 on all saving throws | Applies again |
| Elemental Shell | Resistance 10 to one random element: fire, cold, acid or electricity | The element is rolled again each time; the same element adds 10 more |
| Amplified Pain | Hits stack 1 more pain | 1 more each time |
| Horrific Form | Enemies within 5 ft are shaken (no save) | Once per rage |
| Long Limbs | +5 ft melee reach | Once per rage |

Harmful mutations (5)

| Mutation | Effect | Stacking |
|---|---|---|
| Twisted Legs | -10 ft speed | Applies again |
| Blurred Eyes | -2 on attack rolls | Applies again |
| Exposed Nerves | -2 AC | Applies again |
| Bursting Veins | You take 1 damage every round | 1 more damage each time |
| Seething Mind | -2 on Will saves | Applies again |

Two-sided mutations (5) - the upside and the downside come together, and drawing one again adds both.

| Mutation | Effect |
|---|---|
| Bloodthirst | +2 melee damage (natural weapons too), -2 AC |
| Bloated Muscle | +4 Strength, -4 Dexterity |
| Stone Shell | +3 natural armor, -10 ft speed |
| Raw Nerves | +4 Reflex saves, +2 dodge AC, -2 Will saves |
| Pain Resonance | Hits stack 1 more pain; -1 on all your saving throws |

### Feats

- Feats granted automatically: none.
- The warpriest bonus feats (levels 3, 6, 9, 12, 15, 18) are unchanged.
- Feats added specifically for this archetype: none.
- Because you lose fervor and channel energy, feats that require or improve those features have nothing to work with.

### Resources

- Chaos Rage: max(1, warpriest level / 2 (rounded down) + Wisdom modifier) uses per day. One use is spent each time you start a rage. Restored by resting.
- Frenzied Casting is available once per combat. Re-entering rage after spending it rerolls mutations. Your next opportunity to quicken a spell comes in the next combat.
- Blessings: the base game's blessing resource, granted by the first blessing selection.
- There is no fervor resource.

### Compatibility and notes

- No other mod is needed. If another mod has moved or removed the warpriest progression entries this archetype replaces (second blessing, fervor, the channel energy selection, sacred armor), the archetype is not registered and an error is written to the mod log.
- Chaos Rage counts as raging for base-game effects that check for barbarian rage: Cloak of Blood Scent, Call to Violence, Mangling Frenzy, Demonic Resentment, Bad Temper and Raging Brutality all respond to it. Inspire Tranquility suppresses Chaos Rage and Crystal Mind ends it. Some conditional parts of Bad Temper are not linked.
- Abilities outside that list that specifically require barbarian rage or bloodrage (rage powers, for example) may not treat Chaos Rage as a rage.
- While the rage is suppressed (Inspire Tranquility and similar), the mutations, natural weapon properties and Frenzied Casting already granted stay in place.
- Rage uses, the pain DC, the number and scaling of mutations, the natural weapon tiers and the Frenzied Casting cap count warpriest levels only. Levels in other classes or prestige classes do not add to them.
- The natural attack limits (one bite, gore and tail each; two claws and pincers combined) are the base game's values. If another mod changes those limits, the changed values are used.
- The "magic" natural weapon property raises the total enhancement of a +0 natural weapon by 1. A natural weapon whose total is already 4 or 5 from other effects can then bypass good and lawful damage reduction or count as epic. The "evil" property applies even if you are chaotic neutral (it bypasses DR/evil and switches off regeneration that evil weapons suppress).
- Spells that another mod adds to the warpriest spell list after this mod loads are not quickened by Frenzied Casting.
- The pain penalty does not stack with other penalties of the same type; only the largest applies.
- The natural weapons are copies of base-game weapons, so the combat log shows the base-game names (the Neuro-Stinger also appears as "Sting").
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

The Chimera Monk reshapes the body through practice. Every two levels from level 2 you pick one of eighteen mutations, adding natural attacks such as a tail, bite, claw or horns, or scales, wings, a breath weapon or extra organs. Drinking the chimeric mutagen in combat raises an ability score and your natural armor, and pushes every mutation you own one step further. Flurry of blows, unarmed strike, the Wisdom bonus to AC and ki powers all work exactly as they do for a base monk; the price is four Style Strike selections plus Fast Movement and Still Mind.

### What you give up

| Level | Base class feature | Notes |
|---|---|---|
| 3 | Fast Movement | Removed. The whole unarmored speed bonus is gone, including the part that grows with monk level. Nothing replaces it |
| 4 | Still Mind | Removed. Nothing replaces it |
| 5, 9, 13, 17 | Style Strike | Lose all four selections. |

The monk bonus feats at levels 1, 2, 6, 10, 14 and 18 are kept. Flurry of blows, unarmed damage dice, the AC bonus, ki powers and every monk feature not listed above are unchanged.

### What you gain (by level)

| Level | Feature | Effect |
|---|---|---|
| 1 | Feral Combat Training (a DarkCodex feat) | Granted automatically. DarkCodex's feat that lets natural weapons be used like unarmed strikes |
| 1 | Chimeric Mutagen | Standard action. +4 alchemical bonus to Strength, Dexterity or Wisdom (+6 at level 10, +8 at level 16) and +2 natural armor (+4, +6). Lasts 1 round per monk level, 3 + Wisdom modifier uses per day (minimum 1). Empowers every mutation while it lasts |
| 1 | Chimeric Body | The damage dice of natural weapon mutations grow with monk level (1d4 up to 1d8; 1d6 up to 1d10 while mutated). Not shown on the character sheet |
| 2, 4, 6, 8, 10, 12, 14, 16, 18 | Chimeric Mutation | Choose one of eighteen mutations. Nine picks in total, each mutation once |

Numbers that rise automatically with level: the mutagen bonuses (levels 10 and 16), natural weapon dice (levels 8 and 16), Scales (levels 5, 10, 15 and 20) and breath dice (every two levels).

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

Tail, Bite, Claw, Horns and Extra Arm each add one natural attack. Your hands remain unarmed strikes and your monk fist dice are untouched. All five use the same damage progression. The die starts at 1d4, becomes 1d6 at monk level 8 and 1d8 at level 16 (1d8 at most), and is one step higher while mutated (1d10 at most).

| Monk level | 1–3 | 4–7 | 8–11 | 12–15 | 16–19 | 20 |
|---|---|---|---|---|---|---|
| Natural weapon mutation | 1d4 | 1d4 | 1d6 | 1d6 | 1d8 | 1d8 |
| While mutated | 1d6 | 1d6 | 1d8 | 1d8 | 1d10 | 1d10 |
| (For reference) base monk fist | 1d6 | 1d8 | 1d10 | 2d6 | 2d8 | 2d10 |

- **Primary or secondary**: the natural attack is a primary attack while both hands are empty, and a secondary attack (-5 on the attack roll) while you hold a weapon. Flurry of blows and unarmed strikes are kept as they are.
- **Number of attacks**: each mutation gives one natural attack, made once in a full attack in addition to your other attacks.
- **Per-type limits**: the game engine lets a character use at most one tail attack, one bite, one gore and two claws. If another source also grants an attack of the same type, the one with the larger damage die stays.
- The Extra Arm also gains +1 on its attack roll while mutated, on top of the die step.
- Feral Combat Training (DarkCodex), granted at level 1, is what lets these natural weapons count as unarmed strikes.
- **Appearance**: taking the Tail mutation gives your character a tail, and taking Horns gives it horns. Neither is shown while you are polymorphed. On a pet with Chimera Monk levels, such as an animal companion, the tail and horns are not shown (the natural attacks and numbers still apply).

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
- Against a polymorph effect that grants the same kind of natural armor, only the larger applies.

| Monk level | 1–4 | 5–9 | 10–14 | 15–19 | 20 |
|---|---|---|---|---|---|
| Scales | +2 | +3 | +4 | +5 | +6 |
| While mutated | +3 | +4 | +5 | +6 | +7 |

#### Partial Aspect

- Choose one of the shifter's ten minor aspects (it cannot be changed later). You gain a toggle that turns that aspect on and off. Turning it on is a swift action, with no limit on uses or duration.
- **Partial Aspect II** and **Partial Aspect III** are separate mutations: the first needs Partial Aspect and monk level 8 or higher, the second needs Partial Aspect II and monk level 14 or higher. Each grants one more aspect toggle, different from those already chosen (three at most, all different).
- All aspects you have can be active at the same time.
- The aspect's numbers are computed from monk level instead of shifter level. The tiers are tier 1 at monk levels 1–7, tier 2 at 8–14 and tier 3 at 15–20.
- While mutated every active aspect gets the values of the next tier up (a fourth tier's worth from level 15).
- The per-aspect numbers are in the table under Choices.

### Choices

#### Chimeric Mutation — eighteen options

One pick at each of levels 2, 4, 6, 8, 10, 12, 14, 16 and 18, nine in all. Each mutation can be taken once.

| Mutation | Requirement | Constant effect | While mutated |
|---|---|---|---|
| Tail | — | One tail natural attack. Damage 1d4 up to 1d8 (see the progression). Your character grows a tail | Die one step higher (1d6 up to 1d10) |
| Bite | — | One bite natural attack. Damage 1d4 up to 1d8 | Die one step higher (1d6 up to 1d10) |
| Claw | — | One claw natural attack (hands stay unarmed). Damage 1d4 up to 1d8 | Die one step higher (1d6 up to 1d10) |
| Horns | — | One gore natural attack. Damage 1d4 up to 1d8. Your character grows horns (some races have no horn model) | Die one step higher (1d6 up to 1d10) |
| Venom | — | Natural and unarmed hits poison the target (once per round): Fortitude save each round for 6 rounds or 1d2 Constitution damage. DC 10 + half monk level + Wisdom | DC +2 |
| Scales | — | +2 natural armor (form) bonus, +1 more at levels 5, 10, 15 and 20 (maximum +6). Stacks with other natural armor | +1 |
| Partial Aspect | — | One of the ten shifter minor aspects as an at-will toggle (swift action, scaled by monk level). It can be active together with the aspects gained from Partial Aspect II and III | Every active aspect is one tier higher |
| Partial Aspect II | Monk level 8, Partial Aspect | One more aspect, different from the one already chosen, as an at-will toggle. All aspects you have can be active at the same time | Every active aspect is one tier higher |
| Partial Aspect III | Monk level 14, Partial Aspect II | One more aspect, different from the two already chosen, as an at-will toggle. All aspects you have can be active at the same time | Every active aspect is one tier higher |
| Wings | Monk level 10 | You grow wings (the base game wings effect): +3 dodge bonus to AC against melee attacks and immunity to difficult terrain. The wings are visible | +10 ft speed |
| Breath | Monk level 10 | Choose an element: 30-ft cone, 1d6 per two monk levels, Reflex half, 3 per day, standard action | One extra breath usable only while mutated (refills on rest) |
| Extra Arm | Monk level 10 | One extra-arm slam natural attack. Damage 1d4 up to 1d8 | Die one step higher (1d6 up to 1d10), +1 on that attack roll |
| Extra Legs | — | +10 ft speed, +4 CMD against trip and bull rush | Another +10 ft speed (+20 ft in total) |
| Extra Eyes | — | Cannot be flanked, +2 Perception | Another +2 Perception (+4 in total) |
| Extra Ears | — | Blind-Fight feat, +2 Perception | Another +2 Perception (+4 in total) |
| Extra Senses | Monk level 10 | Blindsense 30 ft | Blindsense 60 ft |
| Extra Heart | — | +2 hit points (a flat amount, not per level), immunity to bleed | Fast healing 2 |
| Extra Lung | — | Immunity to fatigue and to the areas created by four cloud spells (stinking cloud, cloudkill, mind fog, acid fog). Not a general immunity to poison | If you have Breath, +1 extra breath usable while mutated (2 in total) |

- The Perception bonuses of Extra Eyes and Extra Ears stack with each other.
- Extra Lung: if you gain it while standing in such a cloud, it applies only after you leave and re-enter the cloud.

#### Breath element

Breath: Fire, Breath: Cold, Breath: Acid or Breath: Electricity. Only the energy type differs; dice, area, DC and uses are identical. You can take only one.

#### Partial Aspect forms

Partial Aspect, Partial Aspect II and Partial Aspect III each pick one, and the aspects chosen must all be different. Tiers correspond to monk levels 1–7 / 8–14 / 15–20; while mutated you use the next tier's value.

| Aspect | Main effect | Tier 1 / 2 / 3 | While mutated |
|---|---|---|---|
| Bear | Constitution (inherent bonus) | +2 / +4 / +6 | +4 / +6 / +8 |
| Boar | Fortitude saves (inherent bonus) | +2 / +4 / +6 | +4 / +6 / +8 |
| Dinosaur | Initiative (untyped bonus) | +2 / +4 / +6 | +4 / +6 / +8 |
| Elephant | Strength (inherent bonus) | +2 / +4 / +6 | +4 / +6 / +8 |
| Horse | Speed (inherent bonus), plus the Endurance feat | +5 / +10 / +15 ft | +10 / +15 / +20 ft |
| Lizard | Reflex saves (inherent bonus) | +2 / +4 / +6 | +4 / +6 / +8 |
| Spider | AC (dodge bonus) | +2 / +4 / +6 | +4 / +6 / +8 |
| Tiger | Dexterity (inherent bonus) | +2 / +4 / +6 | +4 / +6 / +8 |
| Wolf | Weapon damage rolls (untyped bonus), plus scent (blindsense) | +2 / +4 / +6 | +4 / +6 / +8 |
| Wolverine | Hit points and Constitution (see below) | See below | See below |

- **Bonus types**: inherent and racial bonuses do not stack with other bonuses of the same type; only the larger applies. Dodge and untyped bonuses stack.
- **Wolverine**: bonus hit points (untyped) = monk level × 2 × tier (for example +14 at level 7, +32 at level 8, +56 at level 14, +90 at level 15, +120 at level 20). While mutated it is monk level × 2 × (tier + 1) (for example +48 at level 8, +160 at level 20). The Constitution bonus (racial) is +2 at levels 1–14 and +4 from level 15 (+4 / +6 while mutated). From monk level 8 you also gain the Diehard feat.
- **Wolf**: the scent blindsense is 10 ft at levels 1–7, 20 ft at levels 8–14 and 30 ft from level 15. The base game shifter loses it at level 15; the Chimera Monk keeps the 30-ft version.
- Any other side effects of an aspect are the same as the base game shifter's corresponding minor aspect.

### Feats

- **Feats granted automatically**: Feral Combat Training (a DarkCodex feat) at level 1, regardless of that feat's prerequisites.
- Choose six monk bonus feats at levels 1, 2, 6, 10, 14 and 18.
- **Feats gained through mutations**: Extra Ears grants Blind-Fight. Partial Aspect: Horse grants Endurance, Wolverine grants Diehard (from monk level 8, while the aspect is active).
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
- **Shifter content**: Partial Aspect copies the base game shifter's minor aspects (the features, abilities and effects of the ten aspects, the wolf's scent, Diehard and Endurance) and rescales them to monk level. If any of those is missing from the game, the whole archetype is not created. The shifter originals are not modified, and the shifter's aspect uses per day are not involved. The Chimera Monk's aspect toggles can be active together, but the base game shifter's own aspects are still limited to one at a time.
- **Mutation visuals**: the tail uses the look of the base game Gold Dragon mythic tail, and the horns use the base game Devil mythic small horns. If either of those visuals is missing from the game, the whole archetype is not created. The Wings mutation keeps the look of the base game wings effect.
- **Breath**: built from the base game Scaled Fist's dragon breath, but it uses neither Charisma nor ki. The base game has no dedicated electricity cone visual, so the electricity breath uses a generic cone effect.
- **TabletopTweaks**: with the setting that disables natural armor stacking, Scales is unaffected because it is a form-type bonus (only the mutagen's natural armor stops stacking with other natural armor). The chimeric mutagen's ability bonus is alchemical, so against a base game mutagen only the higher applies.
- Mods that move Style Strike, Fast Movement or Still Mind to different levels can prevent this archetype from registering.
- **Retired mutation "Extra Tail"**: the Extra Tail mutation from earlier versions is no longer offered. The game engine allows only one tail attack per character, so taking it never added an attack. Saves of characters who already took it still load; the mutation stays as a hidden feature with no effect (some screens may show it as "Extra Tail (retired)"). Such a character can retrain and choose another mutation instead.
- **Multiclassing and prestige classes**: the mutagen's bonuses and duration, natural weapon dice, Scales, breath dice and DC, the venom DC and the aspect tiers all read monk level only. Mutations with a level 10 requirement need monk level 10, and the level 8 and 14 requirements of Partial Aspect II and III are monk levels too.
- **Known limitations**
  - The natural attacks from mutations are each made once in a full attack, in addition to your other attacks. They do not gain extra attacks the way flurry of blows or iterative attacks from a high base attack bonus do.
  - Natural attacks of one type never exceed the engine limit (one tail, one bite, one gore, two claws). If you already have an attack of that type from another source, only the one with the larger die remains.
  - The chimeric mutagen cannot be drunk while you are polymorphed.
  - The Surge breath refills only on rest and cannot be used without an active mutagen.
  - Extra Lung is not a general immunity to poison. It covers only the four cloud spell areas listed above, and fatigue.
  - The horns are not shown if the base game has no matching visual for your race and gender (you still gain the gore attack). The tail and horns are not shown on pets such as animal companions. The Wings mutation uses the base game wings effect as is, so this restriction does not apply to it.

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
- With the Dense Fear talent this aura is replaced by a 20-foot, −6 version. When Dense Fear takes effect, the same paladin’s −4 effect ends immediately.

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
| 3 | Oath of Pursuit | Your smite target moves at half its current speed (after armor and other effects) for as long as the smite lasts. No save. It uses the base game's half-speed condition, so a target immune to that condition (Freedom of Movement and the like) is not slowed. |
| 3 | Terrible Gaze | 3/day, standard action. One enemy at close range is frightened for 1d4 rounds on a failed Will save (paladin DC). Fear, mind-affecting. |
| 3 | Death's Touch | Standard action, costs 1 use of lay on hands. Touch an enemy to deal 1d6 negative energy damage per two paladin levels. Divine damage against negative energy affinity. No save. |
| 3 | Ghost Stride | Ignore difficult terrain; +10 feet to speed. |
| 3 | Death's Resolve | Adjacent allies (within 5 feet) are immune to fear. |
| 6 | Dense Fear | Your Aura of Death grows to 20 feet and its fear save penalty becomes −6. Healing is still halved. |
| 6 | Grave Armor | Damage reduction 5/good. |
| 6 | Oath Shield | When an adjacent ally (within 5 feet) is hit by a melee attack (a melee weapon attack with an attack roll, melee touch spells included) and takes damage, you take up to 5 of the damage actually dealt in their place. Ranged attacks (ranged touch spells such as rays included) and damage without an attack roll (save-only spells, area effects and the like) are not moved. |
| 6 | Soul Harvest | When you kill an enemy with a weapon, heal hit points equal to your paladin level. Kills with touch or ray attacks do not count. |
| 9 | Life Drain | Hitting your smite target with a weapon bestows one negative level for 1 minute. Once per your round. |
| 9 | Command Undead | 1/day. Take control of one enemy undead; Will negates (paladin DC). A supernatural ability with the effect of the command undead spell. |
| 9 | Wail of the Dead | 1/day, standard action. Enemies within 30 feet are frightened for 1d4 rounds on a failed Will save (paladin DC). Fear, mind-affecting. |
| 9 | Cursed Strike | Your smite target cannot be healed for as long as the smite lasts. |
| 9 | Iron Bones | A critical hit or sneak attack against you has a 25% chance to be negated, so you take normal damage only (the same as 25% fortification). It does not reduce damage by a share. |
| 12 | Death Blade | All melee weapon hits deal +1d6 negative energy damage. Divine damage against negative energy affinity. Not multiplied on a critical hit. The target does not have to be smitten. |
| 12 | Empowered Channel | Your negative channel deals +2d6. Your self-healing from it rises by the same amount. |
| 12 | Vengeful Riposte | 3/day. When a melee attack hits you, make one melee attack against the attacker. You must be wielding a melee weapon (or be unarmed) and the attacker must be within reach. Each incoming hit in a full attack can trigger a riposte, up to the daily limit. A use is spent when the counterattack resolves. |
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
- Aura of Death and its stronger aura use a DLC 3 negative-energy visual effect. If the effect is unavailable, both auras still apply their gameplay effects.

</details>


## Shadow Sorcerer

<details>
<summary><b>Shadow Sorcerer</b></summary>

### At a glance

| | |
|---|---|
| Base class | Sorcerer |
| Key ability | Charisma. Recommended abilities are the same as the base Sorcerer's |
| Spellcasting | Standard sorcerer daily slots and nine additional shadow or illusion spells. An ordinary spell and its echo use two slots of the same level. |
| Required mods | None |
| Optional integrations | None |
| Alignment / deity restrictions | None added by the archetype |

### Overview

With the shadow toggled on, one shadow repeats your first eligible harmful sorcerer spell each round. It recasts the same spell at the same target or point 0.5 seconds after the original effect ends. For example, if you maximize a fireball, the original deals maximum damage and the echo rolls the ordinary fireball dice.

### What you give up

| Level | Base feature | Notes |
|---|---|---|
| 1 | Spells Given to Shadow | The shadow spends one extra slot to fire an echo. With one slot left you can still cast the original. |
| 7 | Bloodline Feat Selection | Removed |
| 13 | Bloodline Feat Selection | Removed |
| 19 | Bloodline Feat Selection | Removed |

Your bloodline choice, bloodline powers, bloodline spells and bloodline arcana are all kept, and so is the Sorcerer bonus feat at level 1.

### What you gain (by level)

| Level | Feature | Effect |
|---|---|---|
| 1 | Shadow Echo | While the toggle is on, one shadow repeats the first eligible harmful sorcerer spell each round after a 0.5-second delay. |
| 1 | Shadow Double | One shadow casts your echo from Sorcerer level 1 through 20 |
| 1 | Spells Given to Shadow | Choose when your spells produce an echo. |
| 3 | Shadow Arcana: Vanish | You learn Vanish (1st level) as an extra spell |
| 5 | Shadow Arcana: Mirror Image | You learn Mirror Image (2nd level) as an extra spell |
| 7 | Shadow Arcana: Displacement | You learn Displacement (3rd level) as an extra spell |
| 9 | Shadow Arcana: Shadow Conjuration | You learn Shadow Conjuration (4th level) as an extra spell |
| 11 | Shadow Arcana: Shadow Evocation | You learn Shadow Evocation (5th level) as an extra spell |
| 13 | Shadow Arcana: Phantasmal Putrefaction | You learn Phantasmal Putrefaction (6th level) as an extra spell |
| 15 | Shadow Arcana: Greater Shadow Conjuration | You learn Greater Shadow Conjuration (7th level) as an extra spell |
| 17 | Shadow Arcana: Greater Shadow Evocation | You learn Greater Shadow Evocation (8th level) as an extra spell |
| 19 | Shadow Arcana: Shades | You learn Shades (9th level) as an extra spell |

### Abilities in detail

#### Shadow Echo

- **Shadow toggle.** Turn it on to echo the first eligible harmful spell each round. Turn it off to cast without an echo.
- **Which spells echo** — all of the following must hold.
  - The spell is cast from your Sorcerer spellbook. Mythic spells merged into that spellbook count. Other classes' spellbooks, items such as scrolls, wands and potions, and abilities that are not spells do not.
  - It is cast by a direct casting command (yours or the AI's). Spells that some other effect casts automatically do not echo.
  - The cast succeeded.
  - It is not a cantrip.
  - It is harmful to enemies: a spell that can target enemies and either has a harmful effect on them or cannot target allies. Buffs, healing and summoning spells are excluded.
  - If it targets a single creature, that creature is your enemy at the moment of casting. A spell cast on an ally does not echo.
  - You have not already echoed a spell this round.
- **Echo count** stays at one from Sorcerer level 1 through 20. If you cast a quickened spell and a standard spell in the same round, the first qualifying cast receives the echo.
- **One spell per round**: only one spell can be echoed each round. In turn-based combat this goes by round number; in real-time combat and outside combat, 6 seconds must have passed since the last echoed spell. A new combat makes it available again.
- **Timing** is 0.5 seconds of game time after the original spell effect finishes, whether or not the shadow can appear.
- **Where the echoes land**
  - Single-target spells: on the same target.
  - Area spells aimed at a point: on the original point. Even if you aimed by clicking an enemy, the echoes land where the spell was first placed, not where that enemy has moved to.
  - Area spells centred on the caster or on a target: on the centre the original cast used.
- **Echo strength** uses the selected spell variant, base spell level, caster level, casting ability and normal bonuses such as Spell Focus. It makes fresh damage and saving throw rolls. Maximize, Heighten and Persistent on the original never maximize the echo’s damage, raise its DC or force its targets to reroll their saves.
- **Slot cost.** The original sorcerer spell uses its normal slot. When the echo fires, it spends one more slot of the same level, including any increase from metamagic. It takes no extra action. On-cast effects and metamagic buffs for the next spell apply once, to the original cast.
- **Echoes never produce echoes.**
- **Ordinary damage spells** roll fresh damage without metamagic. The echo deals 50% on failed disbelief or 25% on successful disbelief.
- **Disbelief** gives each affected target one Will save against the echo’s base spell DC.
  - Failure: damage is half (50%), and effects other than damage apply in full.
  - Success: damage is a quarter (25%). Effects other than damage (buffs and debuffs, ability damage, energy drain, combat maneuvers, healing) do not apply. Nothing is left to a chance roll.
  - Instant death and dispelling: an echo's instant death effect and dispel magic do not apply to a creature that disbelieved either. A dispel that removes effects at a point or in an area, rather than from one creature, still applies.
  - If the spell is already a shadow spell (Shadow Evocation and the like), its echo deals half damage on a failed disbelief and a quarter of that spell's own reality percentage on a successful one: 20% becomes 5%, 60% becomes 15%.
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

#### Shadow Doubles

- **Normally invisible.** The doubles only show themselves while echoes are going off. While hidden they are treated as not being in the fight at all, so nobody's spells (cones, lines and chains included) can catch them. The doubles are not saved, cannot be targeted, do not count as combat participants and carry no items.
- The double takes the form of the base game’s Shadow creature, a black humanoid silhouette.
- **Sequence**
  1. When the original spell finishes, one shadow appears behind the caster.
  2. The shadow faces the target and begins its casting animation 0.35 seconds before the echo fires. The spell effect appears in its hand at release.
  3. The shadow vanishes 0.9 seconds after the echo finishes, waiting at most 8 seconds for a long-running spell.
- **The spells still come from the caster.** Projectiles, cones, lines and chains all start from the caster's position. Who is hit, attack rolls, areas, range and arrival time are exactly as they would be with no doubles shown.
- The echo keeps the same timing, damage and disbelief rules when the shadow cannot appear.
- **When the doubles do not appear** (the echoes still fire from the caster)
  - The caster is unconscious, polymorphed, in stealth, dead or not in the area, or the game is not in normal play or pause (dialogue, cutscenes and the like).
  - A cone or line echo where a double's spot could fall inside the caster's own cone or line (for example when a wall behind the caster pulls the spot in close).
  - If, just before a cone or line echo fires, the caster or the target has moved so that a double's spot falls inside that cone or line, all the doubles vanish on the spot.
  - If a frame hitch keeps a double from starting its animation before its echo fires, only that echo's animation and hand effect are skipped.
- **When shown doubles vanish early**: the caster dies, falls unconscious, is polymorphed or enters stealth; dialogue or a cutscene starts; or combat ends, if they appeared during combat. If the echoes fizzle (the target died and so on), the remaining doubles vanish without casting. Reloading the area removes them along with the area.
- **Casting animation**: spells with a casting style the doubles' animation set lacks (thrown, breath weapon and so on) use a substitute animation that reaches toward the target.

#### Spells Given to Shadow

- Magic Missile and its echo use two 1st-level slots in total, regardless of the number of missiles. With one slot left, only the original fires.
- Support spells and subsequent spells in the same round use their normal slot cost. Cantrips and scrolls use no sorcerer slots.
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
- Daily slots follow the standard sorcerer table. Existing remaining slots are preserved until rest refills the full allowance.

### Compatibility and notes

- No other mod is required. If the game data is not what the archetype expects (another mod has moved the bloodline feat slots in the Sorcerer progression, or one of the nine extra spells is not on the Sorcerer spell list at the expected level), the archetype does not appear in character creation and the rest of the game runs normally.
- **TabletopTweaks, ExpandedContent, BrawlerClassWrath**: these mods add entries to the bloodline feat selection. The Shadow Sorcerer loses that selection altogether, so there is no conflict.
- **Merged mythic spellbooks**: mythic spells merged into the Sorcerer spellbook (the base game's Lich merge, and Angel, Aeon, Azata, Demon and Trickster spells merged by mods that extend merging) are Sorcerer spellbook spells, so they echo when they qualify.
- The **ToyBox** daily-slot multiplier applies as it does to a standard sorcerer.
- When a prestige class advances the same sorcerer spellbook, the echo uses an additional slot from that same book.
- **Spells that do not echo**: teleports and utility spells that do no harm (Dimension Door and the like) are excluded. A few area spells that travel with the caster or a target cannot keep their original centre and are excluded as well: Zone of Predetermination, the Angel spell Repel the Profane and the Trickster spell Breath of Money.
- **Spells excluded from echoing (15)**: the spells below are harmful to enemies but never echo. The original cast is unchanged.
  - 14 spells with an effect that would still reach an enemy that disbelieved: Inflict Moderate Wounds, Mass / Baleful Polymorph / Prismatic Spray / Clashing Rocks / Force Punch / Hideous Laughter / Slow / Crushing Despair / Cause Fear / Scare / Mortal Terror / the Azata spell Water Torrent / the Lich spell Siphon Time / the Aeon spell Relativity.
  - 1 spell with a summoning effect (summoning spells never echo): the Lich spell Pit of Despair.
- **Known limitations**
  - While the shadow doubles are shown (at most about 10 seconds after the original spell ends), they are in the game. During that time a chain spell cast by an enemy or an ally (Chain Lightning and the like) can pick a double as its next target. The double takes no damage, but it uses up one jump of the chain, so a real target may be skipped or the path may change. In the same way, another unit's cone or line, a ricocheting spell, or a spell that picks allies around its caster can pick a shown double (no damage, but a saving throw or an effect may go to the double).
  - If another mod has changed the base game's Shadow creature so that it no longer looks as expected, the doubles are not created. Echoes still fire, without doubles.
  - Pending echoes are not saved. If the area is reloaded before they go off, they are lost.
  - If the original spell's effect has not finished within 12 seconds of game time, no echoes are fired.
  - Even on a successful disbelief, a few direct effects such as a push may still apply. Spells found to have such effects have been excluded from echoing (see "Spells excluded from echoing" above).

</details>


## Grey Seer

<details>
<summary><b>Grey Seer</b></summary>

### At a glance

| Aspect | Details |
|---|---|
| Base class | Arcanist. Intelligence governs spellcasting. Wield a longsword and a quarterstaff together. |
| Spellcasting | Standard arcanist preparation, casts per day and spell progression, reaching 9th-level spells at arcanist level 18. Consume Spells still lets you turn a spell slot into reservoir points. |
| Abilities | Intelligence powers the arcane spellbook. Strength and weapon bonuses support ordinary sword attacks, while Charisma affects Consume Spells uses and many exploits. |
| Class mod requirement | Included in Homebrew Classes. No separate class mod is required. |

### Overview

Pick the enemy you want to fence with before choosing where to cast. You can protect yourself with a spell or aim an area spell at a distant group, then cut the enemy standing beside you. The two targets are independent. When threatened, you decide how much of your arcane reservoir to commit to getting the spell off.

### What you give up

| Level | Exchange |
|---|---|
| 1, 5, 9, 13, 17 | Give up the arcanist exploit at each listed level. Keep the picks at 3, 7, 11, 15 and 19. |

### What you gain (by level)

| Level | Feature | What it does |
|---|---|---|
| 1 | Sword and Staff, longsword proficiency, Two-Weapon Fighting | Wield the staff in one hand. Pair it with a longsword to improve melee accuracy with both and unlock the two ways to designate a foe. |
| 7 | Improved Two-Weapon Fighting | Gain the second feat in the two-weapon chain. |
| 13 | Greater Two-Weapon Fighting | Gain the third feat in the chain. |

#### Prepared spells and base casts

Sword training keeps the native arcanist tables. Read each cell **from tier 1 upward**. The first column of numbers counts distinct preparations, the second counts base casts before ability or equipment bonuses.

| Arcanist level | Preparations per tier | Base casts per tier |
|---|---|---|
| 1 | 2 | 2 |
| 4 | 3 / 1 | 4 / 2 |
| 6 | 4 / 2 / 1 | 4 / 4 / 2 |
| 8 | 5 / 3 / 2 / 1 | 4 / 4 / 4 / 2 |
| 10 | 5 / 4 / 3 / 2 / 1 | 4 / 4 / 4 / 4 / 2 |
| 12 | 5 / 5 / 4 / 3 / 2 / 1 | 4 / 4 / 4 / 4 / 4 / 2 |
| 14 | 5 / 5 / 4 / 4 / 3 / 2 / 1 | 4 / 4 / 4 / 4 / 4 / 4 / 2 |
| 16 | 5 / 5 / 4 / 4 / 4 / 3 / 2 / 1 | 4 / 4 / 4 / 4 / 4 / 4 / 4 / 2 |
| 18 | 5 / 5 / 4 / 4 / 4 / 3 / 3 / 2 / 1 | 4 / 4 / 4 / 4 / 4 / 4 / 4 / 4 / 2 |
| 20 | 5 / 5 / 4 / 4 / 4 / 3 / 3 / 3 / 3 | 4 / 4 / 4 / 4 / 4 / 4 / 4 / 4 / 4 |

Using Consume Spells on a tier 1 slot spends one of today's casts while leaving the prepared spell available. You can cast that preparation again with another slot of its tier.

### Abilities in detail

#### How the accuracy bonus works

When both weapons are equipped, add the difference between a fighter’s and an arcanist’s base attack bonus at your arcanist level to melee attacks with either weapon. That is +5 at level 10 and +10 at level 20. Rays and the number of iterative attacks from base attack bonus still use their normal calculations.

#### One spell, one follow-up

**Designate for Spell and Sword** is a free action. Selecting a new foe replaces the old target. Complete a spell from your own arcanist casting and, if the foe is alive and within longsword reach, spend 1 reservoir point for one attack without another action. Scrolls and wands do not trigger the follow-up.

#### Which hand makes the attack?

You can put the longsword in either hand. If it is in your off hand, that is still the weapon you swing after a spell. On an ordinary full attack the Two-Weapon Fighting feats govern your off-hand attacks. The follow-up to a spell is a single longsword attack, with no staff strike or off-hand sequence.

| Arcanist level | 1 | 5 | 10 | 15 | 20 |
|---|---|---|---|---|---|
| Normal base attack bonus | +0 | +2 | +5 | +7 | +10 |
| Sword and Staff attack adjustment | +1 | +3 | +5 | +8 | +10 |

Apply the weapon's enhancement, ability modifier and normal two-weapon penalties after the values in the table. At level 10 the longsword starts at +10 from +5 base and +5 training. Damage dice and base attack prerequisites for feats still use their normal rules.

#### Aim the spell and sword separately

Mark the enemy beside you and cast at an archer behind it. Even if the archer saves against the spell, a completed cast can still trigger the sword attack against the marked foe in reach. If the spell kills the marked foe, the sword does not switch to another enemy. Designate again for the next cast. Your arcanist cantrips can also trigger the follow-up.

### Choices

Choose **Designate for Guarded Casting** when an enemy threatens your casting position. If the spell calls for defensive casting, pay at the check to succeed automatically. The prices below include the follow-up attack.

| Slot used | 0–3 | 4–6 | 7–9 |
|---|---|---|---|
| Total reservoir cost | 1 | 2 | 3 |

For example, a threatened spell using a 5th-level slot costs 2 points to guard. With only 1 left, roll the normal defensive check and lose the follow-up. If nobody threatens the cast, even a guarded designation costs only 1 point when the strike goes ahead. Once protection has been paid for, a later interruption or a foe moving away does not refund it.

#### Choosing your remaining exploits

Your first normal exploit pick comes at level 3. Greater exploits open at level 11. The following options show how a frontline arcanist can spend the same points that fuel a sword strike.

| Exploit | Use with sword and staff |
|---|---|
| Armored Mask | Spend a standard action and 1 point for mage armor. If mage armor is already active, gain shield of faith instead. You can do this before closing with the enemy. |
| Dimensional Slide | Spend a move action and 1 point to reach a visible location within close range. Leave enough points for a follow-up if you intend to cast with your standard action. |
| Shadow Veil | Spend 1 point for concealment (20% miss chance) and +5 Stealth. Charisma helps determine how long it lasts. |
| Swift Consume | Consume a slot as a swift action, leaving movement and a standard-action spell available on the same turn. |
The other native exploits span direct offense, protection and greater upgrades. An offensive exploit can deal damage, but its activation does not count as the arcanist spell that triggers a sword strike.

| Exploit | Effect |
|---|---|
| Acid Jet / Ice Missile / Lightning Lance | Make a ranged touch attack for acid, cold or electricity. Sword and Staff's melee accuracy does not apply to these rolls. |
| Flame Arc / Holy Water Jet | Send fire down a 30-foot line, or use holy water in a line against foes susceptible to it. |
| Force Strike / Sonic Blast | Force Strike automatically hits one foe within 30 feet like magic missile for 1d4 + level. Sonic Blast makes a ranged touch attack for sonic damage. |
| Arcane Barrier / Energy Shield | The barrier is a swift source of temporary HP equal to level + Charisma modifier. The shield resists one energy type, starting at 10 and reaching 30 at level 20. |
| Wooden Flesh / Spell Resistance | Wooden Flesh gives +2 natural armor and damage reduction bypassed by slashing. Spell Resistance starts at 6 + arcanist level. |
| Potent Magic | When spending 1 reservoir point to raise a spell's caster level or DC, gain +2 in place of +1. Budget for this separately from guarded casting. |
| Lingering Acid / Icy Tomb | Greater choices. The acid upgrade continues dealing damage in later rounds. The ice upgrade can entangle a foe that fails its save and damage Dexterity. |
| Burning Flame / Dancing Electricity | Greater choices. The fire upgrade can ignite targets for 3d6 each turn, while the lightning upgrade damages creatures adjacent to the primary target. |
| Hellfire Ray / Greater Spell Resistance | Greater choices. The former turns a Flame Arc expenditure into split fire and unholy damage. The latter raises spell resistance to 11 + level. |

### Feats

Level 1 grants longsword proficiency and Two-Weapon Fighting together. Improved Two-Weapon Fighting at 7 and Greater Two-Weapon Fighting at 13 are direct class grants, leaving your normal feat picks available. The one-handed exception is specific to a quarterstaff.

### Resources

| Resource | Single-class rules |
|---|---|
| Arcane reservoir | Capacity is 3 + arcanist level. Daily preparation clears yesterday's balance and gives 3 + half arcanist level, rounded down. |
| Consume Spells | As a move action, spend a remaining slot of 1st level or higher to regain points equal to its level, up to capacity. Daily uses equal your Charisma modifier, minimum one. |

At level 10 you start the day with 8 points and can hold 13. Consuming a 5th-level slot fills an 8-point reservoir to 13. If you want that slot for an attack spell, you can instead spend 2 points to cast it defensively under threat. Consuming the slot and casting from it are mutually exclusive choices.

### Compatibility and notes

A wand, scroll or spell from another class's book cannot start the link. On a multiclass character, check which spellbook supplied the spell on your bar. Accuracy scales with arcanist class level. Switching to a weapon set without the pair breaks the link, and leaving combat clears the designation.

Casting mage armor on yourself illustrates three different defenses and rolls. Guarded designation protects the defensive cast, mage armor helps against later attacks, and your own longsword still needs to hit. Raising AC cannot cover a shortfall in reservoir points.

</details>


## Blood Berserker

<details>
<summary><b>Blood Berserker</b></summary>

### At a glance

| Aspect | Details |
|---|---|
| Base class | Bloodrager. Retains bloodrage, bloodline progression and Charisma-based spells. |
| Combat style | A ranged touch attack sets up a pull maneuver and a close-range weapon hit. |
| Abilities and spells | Strength supports melee, the chain needs a ranged touch hit and Constitution helps bloodrage duration. Keeps Charisma-based bloodrager casting through 4th-level spells. |
| Class mod requirement | Included in Homebrew Classes. No separate class mod is required. |

### Overview

The chain changes who gets to choose the distance. Drag a smaller foe into your front line, or use a giant as an anchor to pull yourself into melee. Either hit leaves a Blood Knot that rewards staying on that particular enemy. Drawn blades take on a translucent red coating during bloodrage. Collecting a blood knot grants temporary hit points equal to your bloodrager level for two rounds. Another knot refreshes this protection.

### What you give up

| Level | Exchange |
|---|---|
| 1 | Fast Movement. |
| 7, 10, 13, 16, 19 | All five ranks of bloodrager damage reduction. |

### What you gain (by level)

| Level | Feature | Effect |
|---|---|---|
| 1 | Blood Chain, Blood Knot, Blood-Coated Weapons | A 30-foot chain as a move action. Wielded melee weapons deal +1d6 physical damage of the weapon’s form per hit. |
| 7 | Blood Price | Extend the chain to 60 feet. Weapon damage rises to +2d6. |
| 10 | Blood Conduction | Load Shocking Grasp or Vampiric Touch into the chain. |
| 13 | Blood-Coated Weapons — Critical Wound | +3d6 on a hit, +6d6 on a confirmed critical. |
| 19 | Blood weapon damage | +4d6 on a hit, +8d6 on a confirmed critical. |

#### Spell progression for Blood Conduction

Blood Conduction unlocks both delivery options at level 10, but only a spell you actually know can be loaded. These milestones show the native bloodrager tables before additional grants and Charisma bonus slots. Read each cell from tier 1 upward.

| Bloodrager level | Base spells known | Base daily casts |
|---|---|---|
| 4 | 2 | 1 |
| 7 | 4 / 2 | 1 / 1 |
| 10 | 5 / 4 / 2 | 2 / 1 / 1 |
| 13 | 6 / 5 / 4 / 2 | 3 / 2 / 1 / 1 |
| 16 | 6 / 6 / 5 / 4 | 3 / 3 / 2 / 1 |
| 20 | 6 / 6 / 6 / 5 | 4 / 4 / 3 / 2 |

Shocking Grasp is tier 1 and Vampiric Touch is tier 3. A level 10 bloodrager who knows Vampiric Touch can load it with the single base tier 3 slot. If you cast it normally first, loading it the same day requires another available slot.

### Abilities in detail

#### Choose which side of the chain moves

During bloodrage, fire at a living enemy. A ranged touch hit attempts a pull maneuver against a foe of your size or smaller. A larger foe pulls you toward it. The **chain hit** ties the knot. Land a melee weapon hit on that enemy by the end of your next turn, or within one round in real time, to collect temporary hit points equal to your bloodrager level for two rounds. Collecting another knot replaces and refreshes these hit points. You can collect the knot once. Hitting a different enemy with the chain moves your claim.

#### Reaching farther and loading a spell

Launching from beyond 30 feet costs 5 actual hit points, payable even on a miss. You need more than 5 actual hit points to try. Blood Conduction uses a swift action and a slot for a spell you know from your bloodrager book. The next chain hit delivers the loaded spell once. A miss keeps the charge, and ending bloodrage clears it.

#### Two weapons and critical hits

Both wielded melee weapons can deliver the extra dice. At level 13, confirming a critical with a ×3 weapon applies its ordinary weapon multiplier and adds **6d6** from Blood-Coated Weapons. Before level 13, a critical uses the same blood dice as any other hit.

#### Three outcomes of firing a chain

| What happened? | Movement | Knot and spell |
|---|---|---|
| Ranged touch miss | Both creatures stay in place. | No new knot forms. A loaded spell stays in the chain. |
| Hit an equal or smaller foe | Attempt to pull the foe. Its maneuver defense can keep it where it stands. | The hit ties the knot and discharges the spell even if the pull fails. |
| Hit a larger foe | You travel toward the foe. If you arrive within reach, your remaining standard action can be a melee strike. | Follow through with a melee weapon hit against that foe to collect the knot. |

The chain and weapon make separate rolls. Landing a touch attack on an armored foe still leaves its normal AC to beat with your weapon. Hitting a different foe can deal blood weapon damage while the first foe's knot remains uncollected.

| Bloodrager level | Blood dice on a normal hit | Blood dice on an effective critical |
|---|---|---|
| 1–6 | 1d6 | 1d6 |
| 7–12 | 2d6 | 2d6 |
| 13–18 | 3d6 | 6d6 |
| 19–20 | 4d6 | 8d6 |

### Choices

#### The two spells you can load

| Choice | Effect carried by the chain | When to load it |
|---|---|---|
| Shocking Grasp | 1d6 electricity per caster level, up to 5d6. | Add an immediate electrical hit to your move-action chain. The target's electricity resistance still matters. |
| Vampiric Touch | 1d6 per two caster levels, up to 10d6. Gain temporary hit points equal to damage dealt, with the spell's one-hour duration. | The spell resolves as you close. The knot's two-round temporary hit points have their own trigger on the subsequent melee hit. |

The spell must be known in your bloodrager book. Loading spends a slot for the unmodified spell and requires an empty chain. One turn can contain a swift load, a move-action chain and a standard-action melee attack. Starting beside your foe lets you forgo the chain and make a full attack with both weapons instead.

### Feats

Proficiencies and combat feat choices follow the bloodrager. A two-weapon build can earn blood dice on a hit from either hand and pays the usual two-weapon penalties. A two-handed weapon delivers the extra dice with its heavier blow. Two weapons offer more separate chances to land them.

### Resources

| When you pay | Cost and recovery |
|---|---|
| While bloodrage is active | Uses the normal bloodrage duration. Repeated chains draw on no separate daily chain pool. |
| Launch within 30 feet | No hit point cost. Firing still costs a move action. |
| Launch beyond 30 and up to 60 feet | Pay 5 actual hit points. At 6 HP the shot leaves 1 HP. At 5 HP, use the shorter range. |
| When bloodrage ends | The outstanding knot, temporary hit points from a collected knot and loaded chain spell expire. The spent spell slot stays spent until recovery. |

A single-class bloodrager's base daily bloodrage allowance is **2 + twice bloodrager level + Constitution modifier** rounds. At level 7 with +3 Constitution that is 19 rounds. Paying 5 HP for a long chain and collecting 7 temporary HP from the knot are separate transactions. When Returning Blood expires the paid HP stays spent.

### Compatibility and notes

The bonus takes the physical form of the wielded melee weapon's damage. A slashing blade, piercing spear and bludgeoning hammer all qualify. The translucent red coating is drawn on edged or pointed models. A hammer can therefore deal blood damage without showing a film on its head. Unarmed strikes, bites, claws and bows do not receive the extra dice.

When immunity or fortification negates a critical, use the ordinary hit dice. At bloodrager level 19 that means +4d6 on the hit or +8d6 when the critical takes effect. The dice scale with bloodrager class level.

</details>


## Sorrow Inquisitor

<details>
<summary><b>Sorrow Inquisitor</b></summary>

### At a glance

| Aspect | Details |
|---|---|
| Base class | Inquisitor. Wisdom supports divine spells, daily gathers and sentence save DCs. |
| Retained features | Domain, judgment, bane, greater bane and the inquisitor spellbook. |
| Weapons and spells | Keeps Wisdom-based inquisitor spells through 6th level. Deliver a sentence at range with its own action or tie it to a successful bane-weapon hit. |
| Class mod requirement | Included in Homebrew Classes. No separate class mod is required. |

### Overview

A bead begins with something that actually happened to an ally. Free a paralyzed companion now, carry that paralysis in a lead rosary, and choose its recipient later. Returning it to the original aggressor makes the sentence harder to resist. Loading it onto a bane weapon lets a melee hit deliver the sentence while you keep fighting. A gathered affliction stays in its bead until your next rest.

### What you give up

| Level | Exchange |
|---|---|
| 1 | Stern Gaze. |
| 3 | Solo Tactics. |
| 3, 6, 9, 12, 15, 18 | All six bonus teamwork feat selections. |

### What you gain (by level)

| Level | Beads | Ordinary sentence | Sentence on the blade |
|---|---|---|---|
| 1–4 | 1 | Standard action, 1 bead | — |
| 5 | 2 | Standard action, 1 bead | 1 bead |
| 6–9 | 2 | Move action, up to 2. Gain Reprisal at level 7. | 1 bead |
| 10 | 3 | Move action, up to 2 | 1 bead |
| 11–14 | 3 | Move or swift action, up to 3. Gain Vow to Bear the Burden. | Up to 2 |
| 15 | 4 | Move or swift action, up to 3 | Up to 2 |
| 16–19 | 4 | Move or swift action, up to 4. Gain Reckoning of Sorrows. | Up to 3 |
| 20 | 5 | Up to 4 | Up to 3 |

### Abilities in detail

#### What goes into a bead

**Gather Suffering** is a standard action targeting another ally within 30 feet, including a dominated party member. Blindness and paralysis from separate effects occupy two beads. A single curse that penalizes several rolls occupies one. Gathering several effects together costs one daily use. Direct ability loss, negative levels and dimensional anchor stay with the bearer. Moving poison carries its remaining ticks and leaves damage already suffered in place.

#### Saving throws and divine damage

**Sentence of Suffering** targets one enemy within 30 feet. Each affliction checks immunity and its original save separately, using Will if it had no initial save. The DC is **10 + half inquisitor level (rounded down) + Wisdom modifier**. At level 7, Reprisal adds 2 to the DC of an affliction returned to its original aggressor. Duration is the shorter of its remaining time when gathered and three rounds. Ordinary and blade sentences share one discharge per round.

From level 5, activate bane and use **Sentence on the Blade** to load beads as a free action. The first hit with your bane weapon that turn delivers the sentence. A miss leaves the beads in the rosary.

At level 16, **Reckoning of Sorrows** rolls one pool of divine damage, 2d10 for each affliction that actually takes hold. If you spend four beads and two fail against the enemy’s immunity or saves, roll **4d10**. Four successful ordinary afflictions yield 8d10, and three delivered on a bane weapon yield 6d10. A weapon critical does not multiply these dice.

#### What fits into one bead?

| Affliction on the ally | What gathering and sentencing do |
|---|---|
| Blindness, stagger, nausea, fatigue, exhaustion, paralysis, stun, sleep or confusion | Pick an eligible harmful effect from the list. Separate causes on one ally can be selected individually. |
| Evil Eye penalties to attacks, AC or saves, or a curse that penalizes several rolls | One effect occupies one bead. Its transferable penalties travel together when you sentence a foe. |
| Ongoing bleed or remaining poison stages | Gather the ongoing affliction. Hit points already lost and ability damage already dealt stay with the ally. |
| An affliction combined with dimensional anchoring | Gather the transferable part. The ally retains the dimensional anchor. |

A bead stores the affliction taken from the ally. When released, the new target still has to be eligible for that effect and can resist it through immunity or its save. Releasing a sleep or fear bead into an immune foe spends the bead and contributes no Reckoning damage.

#### Balancing rescue, release and storage

At level 11 you can gather from one endangered ally as a standard action, then use your swift action to sentence a foe. Each gather selects effects from one ally. Helping a second companion takes another gather even if you removed several effects with the first. Vow to Bear the Burden lets you set aside a free bead and protect someone before the next qualifying affliction lands.

### Choices

At level 11, spend a move action on **Vow to Bear the Burden** to guard one ally for a round. The first eligible affliction that lands uses one empty bead and one daily gather. Keep a slot in reserve when an ally is about to face paralysis. **Examine the Rosary** shows remaining times and lets you empty selected beads as a free action.

#### Assigning beads to a target

At level 16 with +6 Wisdom, the base sentence DC is 24. Send two beads at an enemy that originally inflicted only the first affliction. The first bead uses DC 26 and the other uses DC 24. If only the first lands, Reckoning deals 2d10 divine damage. Storage and release limits are separate: at level 20 you can sentence four of five stored beads and keep the fifth.

If the first bane-weapon hit kills the recipient, there is no living enemy to sentence and the beads stay in the rosary. A sentence actually released on a survivor spends its beads even if immunity or a save stops the effects. This matters when carrying a rare paralysis bead into a fight with both fragile minions and a tougher target.

#### Reading the effect picker

On an ally, Gather Suffering lists each eligible effect, its source and time remaining. Permanent effects show an infinity mark, and poison also shows its stage. With room for only one of two afflictions, use the source as a tiebreaker. From level 7, selecting the one caused by your next target can earn Reprisal's higher DC.

### Feats

The bonus teamwork picks and Solo Tactics are both exchanged. You can still spend a regular feat on a teamwork feat you qualify for, observing its normal ally and positioning requirements. Bane and Greater Bane remain available. A weapon-focused inquisitor can deliver a sentence with the first bane-weapon hit, including a ranged weapon hit.

### Resources

Daily gathers equal **3 + Wisdom modifier**, minimum one. Unused beads stay in the rosary between encounters until your next rest. Rest clears the rosary and restores the daily uses.

| Situation | Gather uses | Stored beads |
|---|---|---|
| Gather three effects into three free slots at once | Spend one use | Add three beads |
| The vow expires without a qualifying affliction | No use spent | No change |
| Load the blade and miss for the turn | No further cost | Beads remain available until your next rest. |
| An enemy saves against an ordinary sentence | No further cost | Released beads are spent. The next discharge must wait for the next round. |

### Compatibility and notes

Gather paralysis with one round remaining and you can sentence an enemy in any encounter before your next rest. The paralysis lasts one round on the new target. Long-lasting afflictions also cap at three rounds on the foe. Examine the Rosary lets you choose the bead to discharge or clear space for the next rescue.

</details>


## Spiritsteel

<details>
<summary><b>Spiritsteel</b></summary>

### At a glance

| Aspect | Details |
|---|---|
| Base class | Barbarian. Strength supports weapon attacks. Wisdom determines daily rage rounds and improves offensive maneuvers during Spirit Rage. |
| The bond | One physical ancestral weapon. You also gain proficiency with the category you choose. |
| Spells and equipment | Keeps normal barbarian rage and equipment training. Weapon inscriptions and the warrior’s teamwork feats are separate selections. |
| Class mod requirement | Included in Homebrew Classes. No separate class mod is required. |

### Overview

When the barbarian rages, the spirit in the weapon can rage alongside its bearer. The same weapon grows a stronger enhancement bonus and learns the traits you choose. A front-line fighter can inscribe Dodge and a trip bonus to hold an enemy in place. A damage-focused wielder can inscribe two elements and deliver both with each hit.

### What you give up

| Level | Exchange |
|---|---|
| 1 | Fast Movement gives way to the bound weapon and Spirit Rage. |
| 2, 6 | These rage power picks give way to spirit teamwork and Spirit Trip. |
| 3, 6, 9, 12, 15, 18 | Lose all six ranks of Danger Sense. |

### What you gain (by level)

| Level | Progression |
|---|---|
| 1 | Choose from the Weapon-Spirit Shaman weapon list. Receive the actual weapon, category proficiency, a +1 enhancement and Spirit Rage. |
| 2 | Gain Solo Tactics while wielding the bound weapon. Choose a qualifying teamwork feat and, separately, your first weapon feat. |
| Every even level, 2–20 | Inscribe one weapon feat per even level, for ten by level 20. |
| 5, 9, 13, 17 | The weapon’s enhancement bonus rises to +2, +3, +4 and +5 respectively. |
| 6 | Spirit Trip. During Spirit Rage, the first bound-weapon hit each round attempts a trip. |
| 4, 8, 10, 12, 14, 16, 18, 20 | Choose a rage power or a qualifying teamwork feat at these levels. You still receive that level’s separate weapon feat. |
| 12 | Stun Ward. You automatically gain stun immunity during Spirit Rage. |
| 16 | Lucid Spirit. You also gain confusion immunity during Spirit Rage. |

### Abilities in detail

#### Spirit Rage and fighting without a partner

Entering barbarian rage in combat with your bound weapon equipped awakens its spirit. **Spirit Rage** can last for the entire rage. Your bound weapon deals damage as if one size larger, and you add your positive Wisdom modifier to offensive combat maneuvers. With Wisdom 14, the bonus is +2 while the spirit is awake. You can switch the spirit off and back on while raging. Putting the weapon away or ending barbarian rage ends Spirit Rage.

During Spirit Rage, the spirit counts as an adjacent ally for your teamwork feats and as a flanking partner for your bound melee weapon. A feat triggered by an ally scoring a critical or carrying a shield still needs that ally. If you lend the weapon to a companion, the inscribed equipment effects travel with it. Spirit Rage belongs to the bonded barbarian. Both heads of a double weapon carry its attack effects, and bound shield spikes apply them on a shield bash.

#### Three separate choices on one character

| What receives it? | When is it decided? | Example |
|---|---|---|
| The weapon's enhancement | Rises automatically with barbarian level. | The weapon is +4 at level 13 without spending an inscription on enhancement. |
| An inscription on the weapon | Choose one at every even level. | Inscribe Dodge and the wielder gains +2 AC while holding that weapon. Switching weapons removes the benefit. |
| A teamwork feat learned by the warrior | Take one at level 2, then choose one in place of a rage power at eligible levels. | The spirit helps its owner use a learned teamwork feat. Lending the item does not teach its borrower's character the owner's feats. |

#### Teamwork with the spirit

| Situation | How the spirit contributes |
|---|---|
| Outflank, Precise Strike | During Spirit Rage the bound melee weapon can meet your own flanking condition. An extra opportunity attack that requires a real ally's critical still needs that ally to score one. |
| Tandem Trip | Against a foe you threaten with the bound melee weapon, the spirit can satisfy the partner condition for a trip. |
| Coordinated Maneuvers, Coordinated Defense, Shake It Off | The spirit supplies one adjacent ally for these effects. Other allies and the cap follow the feat's usual rules. |

At level 6 Spirit Trip triggers on the first **hit** of the round. If your first swing misses, the next swing that lands can trigger it. Extra elemental damage from an inscription makes no separate attack roll and cannot trigger a second trip.

#### Recognizing Spirit Rage after a weapon swap

Barbarian rage produces the first burst, followed shortly by a second burst as the weapon's spirit answers. The two pulses accompany two different effects and spend only one activation of barbarian rage. Switching away from the bound weapon ends the spirit effect even during the same underlying rage.

### Choices

Choose from the 50 weapon feats below at or above their minimum barbarian level. Up to two elemental traits can be active together. Each adds **1d6, 2d6, 3d6 or 4d6** on a hit at levels 2, 8, 14 or 20 respectively. The elemental dice apply once on a critical. Choose ten by level 20. When multiple insight bonuses apply to the same roll, use the highest.

| Minimum level | Weapon feat | Effect and requirements |
|---|---|---|
| 2 | Corrosive | Adds acid damage on a hit. |
| 2 | Flaming | Adds fire damage on a hit. |
| 2 | Frost | Adds cold damage on a hit. |
| 2 | Shock | Adds electricity damage on a hit. |
| 2 | Dodge | Gain a +2 dodge bonus to AC while wielding the bound weapon. |
| 2 | Weapon Focus | Gain +1 on attack rolls with the bound weapon. This overlaps with Weapon Focus for its category. |
| 2 | Great Fortitude | Gain +2 on Fortitude saves while wielding the bound weapon. This overlaps with Great Fortitude. |
| 2 | Lightning Reflexes | Gain +2 on Reflex saves while wielding the bound weapon. This overlaps with Lightning Reflexes. |
| 2 | Iron Will | Gain +2 on Will saves while wielding the bound weapon. This overlaps with Iron Will. |
| 2 | Precise Shot | Remove the shooting-into-melee penalty with the bound weapon. Requires a ranged or thrown weapon and Point-Blank Shot. |
| 2 | Shield Focus | Increase the shield bonus from the shield bearing the bound spikes by 1. This overlaps with Shield Focus. |
| 2 | Cold Iron Inscription | Physical hits with the bound weapon count as cold iron for overcoming damage reduction. |
| 2 | Watchful Spirit | Gain a +3 competence bonus on Perception checks while wielding the bound weapon. |
| 2 | Quiet Step | Gain a +3 competence bonus on Stealth checks while wielding the bound weapon. |
| 4 | Ghost Touch | The bound weapon can strike incorporeal targets as a ghost touch weapon. |
| 4 | Agile | With Weapon Finesse, use a higher Dexterity modifier for damage with a finesse-compatible melee bound weapon. |
| 4 | Weapon Specialization | Add +2/+3/+4/+5 to bound weapon damage at barbarian levels 4/8/14/20. Requires bound Weapon Focus or Weapon Focus for this category. Use the higher bonus with ordinary Weapon Specialization. |
| 4 | Combat Mobility | While wielding the bound weapon, gain +4 AC against attacks of opportunity provoked by movement. Requires Dodge from the weapon or the wielder. |
| 4 | Improved Initiative | Gain +4 on initiative rolls while wielding the bound weapon. This overlaps with Improved Initiative. |
| 4 | Vanguard Charge | Gain a +2 insight bonus on charge attack rolls with the bound weapon. |
| 6 | Trip | Gain +2 on trip checks with the bound weapon, including Spirit Rage’s trip. |
| 6 | Adamantine Inscription | Physical hits with the bound weapon count as adamantine for overcoming damage reduction. |
| 6 | Opening Strike | Gain a +2 insight bonus on attacks of opportunity with the bound weapon. |
| 6 | Strike in Darkness | Reroll a failed concealment check once when attacking with the bound melee weapon. This overlaps with Blind-Fight. |
| 8 | Thundering | Adds sonic damage on a hit. |
| 8 | Keen | Double the bound weapon’s base critical threat range. This overlaps with Improved Critical. |
| 8 | Spirit Vigil | Make one extra attack of opportunity per round with the bound melee weapon. |
| 10 | Critical Focus | Gain +4 on confirmation rolls with the bound weapon. This overlaps with Critical Focus. |
| 10 | Close-Quarters Shot | Shooting or throwing the bound weapon does not provoke attacks of opportunity. |
| 10 | Disruptive Spirit | Enemies threatened by the bound melee weapon take a +4 increase to the DC to cast defensively. This overlaps with Disruptive. |
| 12 | Greater Weapon Focus | Gain an additional +1 on attack rolls with the bound weapon. Requires Weapon Focus from the inscription or for its category. Overlaps with Greater Weapon Focus for that category. |
| 12 | Rooted Stance | Gain a +4 competence bonus to combat maneuver defense (CMD) while wielding the bound weapon. |
| 12 | Judgment Strike | Bound-weapon hits against evil targets deal an extra 2d6 holy damage, rolled once even on a critical hit. |
| 12 | Spirit Lunge | Toggle to extend your bound melee weapon attacks by 5 feet on your turn. You take −2 AC until your next turn after switching off (one round in real time). Your reach for attacks of opportunity stays the same. |
| 14 | Giant-Killer | Gain a +2 insight bonus on bound-weapon attack rolls against Large or larger targets. |
| 14 | Spirit Fortification | Gain the standard 25% fortification against critical hits and sneak attacks while wielding the bound weapon. |
| 14 | Disarming Critical | Once per round, a confirmed critical hit with the bound melee weapon triggers a free disarm attempt against an armed foe. Fortification can negate the critical. |
| 16 | Greater Weapon Specialization | Gain an additional +2 to physical damage with the bound weapon. Requires Weapon Specialization from the inscription or for its category. Overlaps with Greater Weapon Specialization for that category. |
| 16 | Spirit Evasion | While wielding the bound weapon, take no damage on a successful Reflex save against an effect that normally deals half damage on a success. |
| 16 | Spell Ward | Gain spell resistance 32 while wielding the bound weapon. Use the highest applicable spell resistance. |
| 16 | Pierce Defenses | Physical hits with the bound weapon ignore up to 10 points of damage reduction. Use the higher value when combined with the Penetrating Strike feats. |
| 16 | Unfettered Spirit | While wielding the bound weapon, gain immunity to paralysis, entanglement, slow, and difficult terrain. |
| 16 | Ground Assault | Gain a +2 insight bonus on bound-weapon attack rolls against prone targets. |
| 16 | Fear Pursuit | Gain a +2 insight bonus on bound-weapon attack rolls against shaken or frightened targets. |
| 16 | Flame Ward | Gain fire resistance 30 while wielding the bound weapon. |
| 16 | Grave Ward | Gain resistance 30 to negative energy damage while wielding the bound weapon. |
| 16 | Speed | On a full attack, make one extra attack with the bound weapon at your highest attack bonus. This does not stack with a haste extra attack. A double weapon gains only one extra attack. |
| 16 | Thundering Critical | On a confirmed bound-weapon critical hit, add 1d8/2d8/3d8 sonic damage for a weapon critical multiplier of ×2/×3/×4. Fortification that negates the critical also prevents the burst. |
| 16 | Spirit Sense | Gain blindsense out to 25 feet while wielding the bound weapon. |
| 16 | Spellcleaving Strike | Once per combat, declare this strike as a free action. Your next bound weapon hit that combat attempts to dispel one beneficial spell on the enemy, choosing the highest caster level. Use your barbarian level for the dispel check. |

When the character feat and inscription apply to the same weapon category, count each tier of Weapon Focus or Weapon Specialization once. At level 12, ordinary longsword specialization (+2) and the bound longsword inscription (+3) give +3 total. The base and Greater tiers add together. At level 20, both tiers of Focus give +2 to hit, and both tiers of Specialization give +7 physical damage with the bound weapon.

#### Receiving the weapon

An exotic choice such as a fauchard is usable from level 1. If a hand is free, the weapon can be equipped immediately. Otherwise, find it in your inventory. Category proficiency lets you use other weapons of that type, but equip the ancestral item itself to use its inscriptions and spirit tactics.

#### Pairing the weapon with its inscriptions

- **Melee control.** Choose a fauchard and receive proficiency at level 1. Inscribe Dodge to hold the line, then Trip from level 6 to add +2 even to Spirit Trip. Both are weapon inscriptions, separate from your teamwork picks.
- **Elemental weapon.** A level 14 weapon with Corrosive and Frost adds 3d6 acid and 3d6 cold on a normal hit. Against an acid-resistant enemy, the cold portion still uses the foe's cold resistance.
- **Archer or shield bearer.** A bow accepts ranged inscriptions, enhancement and elemental damage. Bound shield spikes can take Shield Focus. To flank with the spirit, wield a bound melee weapon that can threaten the foe.

#### The starting weapon list

Pick one of up to 59 categories at level 1. The menu shows the actual weapon's damage dice, critical profile and reach or range. Double weapons show both heads. Sawtooth Sabre follows the same availability condition as the Weapon Spirit Shaman's choice.

| Group | Categories |
|---|---|
| Melee | Dagger, Punching Dagger, Kukri, Starknife, Shortsword, Handaxe, Sickle, Kama, Sai, Nunchaku, Light Mace, Light Hammer, Light Pick, Club, Longsword, Rapier, Scimitar, Dueling Sword, Estoc, Falcata, Sawtooth Sabre, Bastard Sword, Battleaxe, Dwarven Waraxe, Heavy Mace, Flail, Warhammer, Heavy Pick, Tongi, Trident, Shortspear, Greatsword, Greataxe, Greatclub, Falchion, Elven Curved Blade, Earth Breaker, Heavy Flail, Scythe, Spear, Longspear, Glaive, Bardiche, Fauchard, Quarterstaff |
| Double weapons | Double Axe, Double Sword, Hooked Hammer, Urgrosh |
| Shield spikes | Spiked Light Shield, Spiked Heavy Shield |
| Thrown | Dart, Javelin, Throwing Axe |
| Ranged | Longbow, Shortbow, Light Crossbow, Heavy Crossbow, Sling Staff |

### Feats

Read each inscription's stacking and prerequisite rules alongside the character sheet. Weapon Focus applies only once with the normal feat for that category. Keen and Improved Critical widen the threat range only once. Weapon Specialization uses the larger damage bonus. An inscription can also unlock a later inscription, such as Dodge qualifying the weapon for Combat Mobility.

The teamwork selection draws from native lists including hunter, cavalier tactician and pack rager, and offers each feat only once. You must still meet the feat's prerequisites. When fighting alone, a feat such as Shield Wall that checks another ally's equipment behaves differently from Coordinated Maneuvers, which can count the spirit's presence.

### Resources

Barbarian rage and Spirit Rage share the barbarian’s daily rage rounds. Keeping both active costs one round per round. Switching off only the spirit leaves barbarian rage running at its usual cost. Unspent rounds carry into the next encounter and refresh on rest.

A single-class Spiritsteel has **2 + twice their barbarian level + their Wisdom modifier** rounds per day. With Wisdom 18, that gives 8 rounds at level 1, 16 at level 5, and 46 at level 20. Extra Rage extends the time you can spend with the spirit, and Limitless Rage lets you sustain both effects without spending rounds.

### Compatibility and notes

The bond tracks the actual item issued at creation as well as its category. The hand holding it, the head of a double weapon that made the attack, or a bash with bound shield spikes determines what receives the effect. If a companion has been using the weapon, equip that same item on its original warrior before trying to enter Spirit Rage again.

</details>


## Dream Thief

<details>
<summary><b>Dream Thief</b></summary>

### At a glance

| Aspect | Details |
|---|---|
| Base class and faith | A rogue devoted to Desna. Choose Desna at the deity step when creating a new character. |
| Key abilities | Dexterity and Charisma both contribute to daily starlight. Charisma also sets the difficulty of dream talents and sleep effects. |
| Weapon | Gain starknife proficiency. Equip a starknife to use the throws and Dreamwalk attack. |
| Magic and talents | Uses starlight and dream talents without a conventional spellbook. Dream talents and ordinary rogue talents share the same even-level picks. |
| Class mod requirement | Included in Homebrew Classes. No separate class mod is required. |

### Overview

A Dream Thief throws Desna’s star-shaped blade, then steps through a dream to deliver the waking blow. Put an enemy to sleep and let the party attack someone else until your starknife lands. Against a foe your allies are already hitting, sleep can end almost immediately. The same pool can instead sustain a minute of weapon damage or pay for an attack from across the field.

### What you give up

| Level | Exchange |
|---|---|
| 1 | Trapfinding. |
| 3, 6, 9, 12, 15, 18 | Each rank of Danger Sense. |
| Every even level | The rogue talent pick becomes a choice between a normal rogue talent and a dream talent. |

### What you gain (by level)

| Rogue level | Feature | Use |
|---|---|---|
| 1 | Falling Star, Dream Throw | Both are **standard actions** and make a 30-foot weapon attack. The blade returns. Falling Star costs no starlight. Dream Throw costs 1. |
| 1 | Waking Strike | Hit a sleeping foe with a starknife for a confirmed critical and sneak attack. Critical immunity, precision immunity and fortification still apply. |
| 3 | Starlight Shroud | Swift action, 1 point, one minute. Add 1d6 divine damage to the starknife and gain an extra attack at full bonus on a full attack. The extra attack overlaps with haste. |
| 6, 9, 15 | Dreamwalk, Far Dreamwalk, Fleet Dreamwalk | Spend 1 to appear beside the foe, deal sneak damage on a hit and return. Starts as a 30-foot standard action, reaches 50 feet at level 9 and becomes a move action at level 15. |
| 12, 18 | Starlight Burst, Deep Starlight | A confirmed critical under Starlight Shroud adds (critical multiplier − 1)d10 divine damage. Gain 3 additional daily starlight at level 18. |

### Abilities in detail

#### Sleep and Dreamwalk

Dream Throw attempts sleep after the weapon hits. Its save DC is 10 + three quarters of rogue level (rounded down) + Charisma modifier. A failed Will save puts the target to sleep for **1d4 rounds**. Damage wakes it, and a successful save grants one minute of immunity to Dream Thief sleep effects. Dreamwalk also works on an awake enemy if there is space to stand beside it. At level 15 you can spend your move action on a round trip through a dream and still throw at a different enemy as your standard action.

#### Three ways to attack with the starknife

| Attack | How it resolves | Interaction with sleep |
|---|---|---|
| Falling Star or Dream Throw | Make a Dexterity-based ranged weapon attack against normal AC with the equipped starknife. The throw does not consume the knife. | Waking Strike can apply if the target is already asleep. On an awake target, Dream Throw resolves weapon damage before attempting to put it to sleep. |
| Dreamwalk | Teleport to a free spot beside the foe, then make one starknife attack treating it as flat-footed. | An awake enemy can take sneak damage. A sleeping one can also trigger Waking Strike. |
| Ordinary full attack | With Starlight Shroud active, add one starknife attack at your highest attack bonus. | If the first hit wakes the target, later attacks in the sequence resolve against an awake foe. |

#### Counting shroud damage on a critical

From level 12, a starknife critical that takes effect during Starlight Shroud adds divine burst dice. At ×3 the burst is **(3 − 1)d10 = 2d10**, in addition to the shroud's normal 1d6 on hit. A critical from Waking Strike qualifies as well. Fortification that negates the critical also denies the critical-only burst.

### Choices

These dream talents share the even-level pick with ordinary rogue talents. The cost is paid when the ability is used. Caster level equals rogue level and the save DC is **10 + three quarters of rogue level (rounded down) + Charisma modifier**.

| Min. level | Talent | Starlight | Effect |
|---|---|---|---|
| 2 | Lullaby | 1 | Make the target drowsy and weaken its saves against sleep. |
| 2 | Sleep | 1 | Put creatures to sleep, up to a total number of Hit Dice equal to your rogue level. |
| 2 | Slumber | 1 | On a failed Will save, the enemy sleeps for 1 round per rogue level and wakes on damage. |
| 4 | Hypnotism | 1 | Hypnotize creatures in the area, leaving them fascinated. |
| 6 | Deep Slumber | 1 | Put creatures to sleep, up to a total number of Hit Dice equal to your rogue level + 4. |
| 6 | Haze of Dreams | 1 | Slow the target's movement with dreamlike visions. |
| 8 | Rainbow Pattern | 2 | Fascinate creatures in the area with a shifting pattern of light. |
| 8 | Phantasmal Killer | 2 | Confront a target with a potentially lethal illusion of its worst fear. |
| 8 | Lulling Aura | 0 | Toggle a 30-foot aura that penalizes enemy saves against sleep by 2, increasing to 4 at level 12. |
| 10 | Phantasmal Web | 2 | Ensnare enemies in an illusory web filled with spiders. |
| 10 | Restless Slumber | 1 | On a failed Will save, the enemy sleeps for 1 round per rogue level, takes 1d10 damage each round, and is confused upon waking. |
| 12 | Cloak of Dreams | 2 | Surround yourself with a haze that can put nearby creatures to sleep. |
| 12 | Phantasmal Putrefaction | 2 | Weaken enemies with a terrifying illusion of decay. |
| 16 | Nightmare | 3 | Within a 10-foot radius, a failed Will save causes sleep for 1d4 rounds and 2d10 damage each round, followed by 10 rounds of confusion on waking. |
| 18 | Weird | 3 | Confront multiple enemies with potentially lethal apparitions. |

#### Picking a sleep talent

Against one dangerous foe, Slumber offers a Will save and a duration of one round per rogue level. Against a group, check the total Hit Dice that Sleep or Deep Slumber can affect. At rogue level 10 their budgets are 10 HD and 14 HD. Two 8-HD creatures exceed both totals, even though each is individually within them.

Lulling Aura costs no starlight and requires enemies within 30 feet. Nightmare spends 3 points to threaten a small group with sleep, recurring damage and confusion on waking. Against sleep immunity, Dreamwalk still offers a surprise attack and Starlight Shroud still enhances the weapon. To keep a foe asleep, leave it outside the party's area damage.

#### Who can set up Waking Strike?

Waking Strike checks whether the enemy is asleep when you attack. An ally can supply the sleep spell, saving the rogue a standard action and a point of starlight. Stun, paralysis or prone alone do not meet the sleep condition. Even against a sleeping enemy the starknife still has to hit.

### Feats

Starknife proficiency is automatic at level 1. Weapon Focus and Two-Weapon Fighting are separate choices. Each even rogue level offers one ordinary rogue talent or one dream talent, for ten selections in total. Taking Deep Slumber at 6 spends that level's talent pick. Dreamwalk and Starlight Shroud arrive through class progression without consuming talent picks.

### Resources

Rest restores **half your rogue level (rounded down) + Dexterity modifier + Charisma modifier** starlight, minimum 1. A 10th-level rogue with Dexterity +4 and Charisma +3 has 12 points for the day. Add the level-18 bonus after calculating that pool.

| Ways to spend a 12-point pool | Starlight spent | Actions and outcome |
|---|---|---|
| Dream Throw, then Dreamwalk next turn | 1 + 1 = 2 | The first turn needs a hit and a failed Will save. The next turn needs a weapon hit. Ten points remain. |
| Activate shroud and use Falling Star | 1 + 0 = 1 | Spend a swift and a standard action. A single standard-action throw does not receive the extra strike reserved for a full attack. |
| Falling Star or normal attacks with an empty pool | 0 | Available with a starknife equipped. Waking Strike costs no separate point when you hit a sleeping foe. |

At rogue level 10 with +3 Charisma, Dream Throw is Will DC 20 (10 + 7 + 3). An enemy inside Lulling Aura takes −2 to saves against sleep at that level and −4 from level 12. The aura penalizes the enemy's roll without changing your starlight total. Dexterity helps both throw accuracy and pool size, while Charisma helps the sleep DC and pool size.

### Compatibility and notes

A new character picks the archetype first and Desna at the deity step. An existing character's deity must be compatible. Dreamwalk searches for a real landing point and a return point. Units crowding every space around the foe can block entry. If your original square becomes occupied during the attack, the ability looks for a nearby clear return point, which matters in a narrow doorway.

</details>


## Deathdealer

<details>
<summary><b>Deathdealer</b></summary>

### At a glance

| Aspect | Details |
|---|---|
| Base class | Slayer. Retains Studied Target, sneak attack and the normal attack and damage progression against a studied foe. |
| Key abilities | Intelligence and Dexterity. Reactions use the better of Intelligence and the normal attack or maneuver ability. |
| Magic | Nine personal arcane spell-like abilities, each with its own daily use. |
| Equipment and casting | Threaten enemies with a melee weapon to react. Personal arcana share a menu and require neither a spellbook nor prepared slots. |
| Class mod requirement | Included in Homebrew Classes. No separate class mod is required. |

### Overview

Study the enemy whose turn matters most, then stay close enough to threaten it. A weapon swing leaves an opening for a riposte, a step away exposes its footing, and a spell invites a blow before it resolves. Position and an unspent attack of opportunity matter as much as what you do on your own turn.

### What you give up

| Level | Base feature | Replacement |
|---|---|---|
| 2, 6, 10 | Slayer talent at each level | Studied Riposte, Intercept Movement, Disrupt Casting |
| 14 | Quarry | Intensive Study |
| 19 | Improved Quarry | Improved Intensive Study |

### What you gain (by level)

| Level | Trigger | Response |
|---|---|---|
| 2 | A threatened studied foe makes a weapon attack. | **Studied Riposte** makes one counterattack after the attack, whether it hit or missed. |
| 6 | The foe starts moving voluntarily. | **Intercept Movement** attempts a trip before it leaves. Success stops the movement. |
| 10 | The foe casts a spell. | **Disrupt Casting** strikes before the spell resolves. A hit threatens a critical, and damage can break the caster’s concentration. |
| 14 | You attack your own studied target with a weapon. | Gain +2 to hit and automatically confirm critical threats, on top of normal Studied Target bonuses. |
| 19 | You score a weapon critical against the studied target. | Increase the critical multiplier by one. A ×2 weapon becomes ×3 against that enemy. |

### Abilities in detail

#### Opportunity attacks and concentration

The three responses share **one reaction per round** and consume an attack of opportunity. Having more attacks of opportunity in reserve does not grant a second studied reaction that round. Disrupt Casting must hit to threaten a critical. At level 14 that threat confirms automatically against your studied foe, but spell interruption still depends on concentration. Intelligence can improve the reaction attack or maneuver roll. Normal weapon damage keeps its usual ability.

#### Choosing which opening to take

Each learned reaction has its own toggle and starts enabled. The first eligible action in a round spends your shared reaction and one attack of opportunity. If a mage swings a weapon first, Studied Riposte can spend the reaction you wanted for its spell. Switch off the attack and movement responses when you intend to wait specifically for a cast. A failed trip or missed counterattack still spends the opportunity.

| Trigger to watch for | What qualifies |
|---|---|
| Weapon attack | The foe can attack you or someone else, provided you threaten it. Ray and touch attacks do not count as weapon attacks for the riposte. |
| Movement | The opening is the start of voluntary movement. A foe pushed by another ability does not spend your movement reaction. |
| Casting | An enemy casting an actual spell qualifies. Activating a wand or using a spell-like ability does not. |

#### Reading the studied-target bonuses

Normal Studied Target improves both attack and damage. The additional +2 at level 14 applies specifically to weapon **attack rolls**. At level 19 the weapon gains one critical multiplier step against the studied enemy. Its threat range stays the same. Both upgrades apply to ordinary weapon attacks as well as reactions. A higher Intelligence modifier replaces the usual reaction ability rather than adding a second ability modifier.

#### The same longsword at levels 13, 14 and 19

| Situation | After Disrupt Casting hits with a weapon |
|---|---|
| Slayer 13, ordinary ×2 longsword | The hit threatens a critical, then needs normal confirmation. |
| Slayer 14, your studied target | Gain +2 to hit and automatically confirm the threat. The sword still uses ×2. |
| Slayer 19, your studied target | Keep the same attack and confirmation benefits, with the sword's multiplier now ×3. The target's defenses against critical hits still apply. |

In all three cases the damage must challenge the caster's concentration to disrupt its spell. Automatic confirmation does not by itself guarantee disruption. An ordinary weapon attack still needs a threat within its normal range before it can confirm.

### Choices

Open the **Arcane Abilities** icon to select a learned power. Each is a **standard action, self only, once per day**. Caster level equals slayer level. Each effect keeps the original spell’s duration and discharge rules.

| Slayer level | Power | Duration on the slayer | What it offers |
|---|---|---|---|
| 1 | True Strike | Next attack before the end of the next round | Gain +20 insight on the next attack roll and ignore concealment on that attack. |
| 3 | Mirror Image | 1 min/level, or until all images are gone | Creates 1d4 images plus one per three caster levels (maximum eight). A successful strike against an image removes it. |
| 5 | Haste | 1 round/level | Speeds your movement and full attacks. Party members and companions are outside the effect. |
| 7 | Stoneskin | 10 min/level or 10 damage prevented per level (150 maximum) | Gain DR 10/adamantine, limited both per hit and by the total absorbed. |
| 9 | Geniekind | 1 round/level | Choose one of the four forms in its submenu. Their elements are listed below. |
| 11 | Greater Heroism | 1 min/level | Gain +4 morale on attack rolls, saves and skills, immunity to fear and temporary HP equal to caster level (maximum 20). |
| 13 | Legendary Proportions | 1 min/level | Grow in size and gain Strength and Constitution for melee combat. |
| 15 | Seamantle | 1 min/level | Gain improved cover (+8 AC and +4 Reflex) against foes without freedom of movement, and immunity to fire. |
| 17 | Foresight | 10 min/level | Avoid being caught flat-footed and gain +2 insight to AC and Reflex saves. |

Spending Haste leaves Mirror Image and Stoneskin available if their uses remain. Preparing a defense before combat frees your own turn to move into reach of the enemy you studied.

#### The four Geniekind forms

| Form | On a melee weapon or unarmed hit | Matching resistance |
|---|---|---|
| Djinni | +1d6 electricity | 10 |
| Efreeti | +1d6 fire | 10 |
| Marid | +1d6 cold | 10 |
| Shaitan | +1d6 acid | 10 |

All four forms grant a +4 enhancement to natural armor, +2 enhancement to Constitution and +2 on saves against paralysis, poison, sleep and stun. Pick the form that resists incoming damage or adds an element the target is vulnerable to. Taking one form spends access to all four until rest.

### Feats

Seven ordinary slayer talent picks remain at **4, 8, 12, 14, 16, 18 and 20**. Advanced talents unlock at 10. At 14 you receive both the normal talent and Intensive Study. Sneak attack and the level 20 Master Slayer feature remain. Combat Reflexes can give you more ordinary opportunities, while your three studied reactions continue to share one per round.

### Resources

The daily suite starts with one ability at level 1 and gains a separate use with each new odd-level ability. At level 9 you have five distinct uses, and at 17 you have nine. An unused spell cannot buy a second use of another. Cast a long-duration defense before moving into position and save a short-duration effect such as Haste for the approach to combat.

### Compatibility and notes

Another slayer's studied mark belongs to that slayer. Your reactions and intensive study require your own mark. Marking a distant enemy does not allow a ranged countershot: reactions require a threatening melee weapon within opportunity reach. Your personal arcana can be cast before picking a target.

</details>


## Falconer

<details>
<summary><b>Falconer</b></summary>

### At a glance

| Aspect | Details |
|---|---|
| Base class | Ranger. Keeps favored enemy, ranger spells, quarry and improved quarry. |
| Hunting partners | The hawk joins at level 1. Choose an animal companion such as a wolf through Hunter's Bond at level 4 to hunt with both. |
| Key abilities | Wisdom governs the hawk's accuracy and Hawk's Eye duration. Dexterity helps the ranger land bow shots. |
| Spells and weapons | Keeps Wisdom-based ranger casting through 4th level. Longbows and shortbows consume the coordinated-shot mark. |
| Class mod requirement | Included in Homebrew Classes. No separate class mod is required. |

### Overview

Let the wolf drive prey into the open, send the hawk in from its blind side, then loose an arrow as the target turns. The hawk responds to your commands and attacks. You can position your companion on the ground and choose a different enemy for the hawk to dive on this round.

### What you give up

| Level | Feature exchanged | What remains |
|---|---|---|
| 3 | Endurance and the first favored terrain | Choose your hunter’s bond at level 4. |
| 8, 13, 18 | Additional favored terrains and their bonus increases | Favored enemy, quarry and improved quarry follow normal ranger progression. |
| 12 | Camouflage | Lose the ability to hide using favored terrain. |
| 16 | Improved Evasion | You keep Evasion from level 9. When Evasion applies to an attack that allows a Reflex save for half damage, a successful save deals no damage and a failed save deals full damage. |

### What you gain (by level)

| Level | Feature | What it does |
|---|---|---|
| 1 | Hunting Hawk / Command Dive | Send the hawk as a swift action. After it hits, your next longbow or shortbow attack against that enemy gains a +2 circumstance bonus. |
| 4 | Drive Prey | A companion hit grants a +2 circumstance bonus to the next dive against that foe. Beneficial spell effects on the companion also apply to the hawk. |
| 8 | Automatic Dive | After your weapon attack against a surviving enemy, the hawk dives even if you missed. The automatic dive costs no action and shares the manual command's one sortie per round. |
| 10 | Wing Cover | For one round, an enemy hit by the hawk cannot make movement attacks of opportunity against your animal companion. This gives the wolf an opening to change targets. |
| 11, 19 | Hawk's Eye | Activate as a swift action for rounds equal to your Wisdom modifier (minimum one). The first strike of each dive gains a +20 insight bonus and ignores concealment. Once per day, rising to twice at level 19. |
| 16 | Threefold Hunt | If hawk and companion hit the same foe within a round, your next coordinated arrow deals +2d6 piercing precision damage on a hit. This can trigger once per round and a missed arrow spends the opportunity. |

### Abilities in detail

#### Strike count and accuracy

| Ranger level | Attacks per dive | Base die per strike |
|---|---|---|
| 1–7 | Talon | 1d4 |
| 8–14 | Talon, then the other talon | 1d6 |
| 15–20 | Both talons, then beak | 1d8 |

Each strike adds half your ranger level (rounded down) to damage. The hawk grows visibly from level 1, reaching 1.3 times its starting scale at level 10 and 1.6 times at level 20.

The first strike starts at **ranger level + Wisdom modifier + 2 for the dive**. The second and third attacks take −5 and −10. Applicable favored enemy and positive attack buffs on the ranger also contribute, subject to normal stacking rules.

At level 20 with a +8 Wisdom modifier, the three attacks start at **+30 / +25 / +20** before other bonuses. Spend your swift action on Hawk's Eye, then fire your bow to trigger the automatic dive. For eight rounds, each dive opens at +50 under those conditions, while its second talon and beak keep their usual bonuses.

#### Attack order changes which arrow gets the bonus

| Sequence | Where the benefit lands |
|---|---|
| Manual dive, then your first arrow | If the hawk hits, the **first arrow** can use the +2 coordinated shot. The sortie is already spent when that arrow would trigger Automatic Dive. |
| First arrow, automatic dive, next arrow | The first arrow resolves before the hawk flies. A mark created by that dive can benefit the second arrow or a later longbow or shortbow attack. |
| Wolf hit, hawk hit, ranger's arrow | The wolf sets up the hawk, which sets up the arrow. From level 16, hitting the same foe within one round also primes +2d6 piercing precision damage on the arrow. |

Marks belong to individual targets. A wolf hit on the left-hand enemy will not improve a dive on the right-hand enemy. The next longbow or shortbow roll against the marked target consumes the coordinated shot, so the order of bow attacks matters as well.

#### Two ways the hawk benefits from buffs

A beneficial spell cast on the animal companion by an ally is mirrored for its remaining duration. When the source effect ends or is removed, its mirror expires too. Equipment bonuses and hostile spells on the companion are outside this sharing. The ranger's favored enemy and attack buffs reach the hawk separately through its **attack rolls**.

If ranger and companion carry overlapping, nonstacking attack bonuses of the same type, the hawk receives only the difference needed to reach the better value. A single-attack buff inherited from the ranger can be expended by the hawk's roll.

| Ranger level and Wisdom modifier | Dive attack bonuses before other modifiers | Base die + level bonus per strike |
|---|---|---|
| 4 · +3 | +9 | 1d4 + 2 |
| 8 · +4 | +14 / +9 | 1d6 + 4 |
| 15 · +6 | +23 / +18 / +13 | 1d8 + 7 |
| 20 · +8 | +30 / +25 / +20 | 1d8 + 10 |

The last column shows just the base die and ranger-level contribution per strike. Hawk's Eye gives only the opening attack +20 insight and concealment bypass. Against a high-AC foe, estimate the first talon's chance separately from the later attacks.

### Choices

At level 4, choosing an animal companion through Hunter's Bond gives you a ground partner. A melee companion such as a wolf can set up the dive by hitting first. Without an animal companion you still have the hawk, its dive, Hawk's Eye and the coordinated bow shot. The companion-hit trigger, shared spells, Wing Cover and the companion half of Threefold Hunt require an actual animal companion.

At level 10 Wing Cover applies to the specific enemy hit by the hawk. If the wolf withdraws from two enemies and only one was struck, the other can still make its normal movement attack of opportunity.

#### Using quarry alongside the hawk's mark

Native Quarry designates one visible foe as a standard action, granting the ranger +2 insight to hit it and automatic critical confirmation. Improved Quarry at level 19 changes designation to a free action and raises the insight bonus to +4. The hawk's coordinated-shot mark gives +2 circumstance on one longbow or shortbow attack. The two bonus types can therefore benefit the ranger's own arrow against the same enemy.

### Feats

The ranger keeps combat style feat picks. Archery feats support the longbow or shortbow attacks that use the mark. A direct melee weapon attack can call the automatic dive, but later melee hits do not spend the arrow mark. Crossbows are also outside the coordinated-shot weapon pair.

### Resources

| What you want this round | Action or limit |
|---|---|
| Send the hawk first to a chosen foe | Long-range manual command, swift action. Uses the round's sortie. |
| Activate Hawk's Eye and dive this turn | From level 11, spend the swift action on Hawk's Eye and trigger Automatic Dive with your own weapon attack. |
| Sustain Hawk's Eye at high level | At level 19 with +5 Wisdom, use it twice daily for five rounds each. A dive each round makes use of each opening attack. |

### Compatibility and notes

Automatic Dive checks for a surviving enemy after the ranger's own weapon attack resolves in combat. Rays, touch spells and companion attacks do not launch it. The hawk's appearance, strike count, accuracy and base dice follow ranger level. Extra attacks or haste on a companion do not grant another sortie.

</details>


## Prophecy Breaker

<details>
<summary><b>Prophecy Breaker</b></summary>

### At a glance

| Aspect | Details |
|---|---|
| Base attack bonus | Half your oracle level, rounded down. At oracle level 20, your base iterative attacks are +10/+5. |
| Base class | Oracle. Keeps the oracle's curse, the choice of cure or inflict spells, and normal weapon and armor proficiencies. |
| Key ability | Intelligence powers divine spellcasting and sets the save DC of arcane research spells. |
| Spellcasting | Cast divine spells spontaneously as an oracle. For arcane magic, inscribe spells from the wizard list, prepare a small selection at rest, then cast flexibly from that selection. |
| First-level choices | Choose a curse, cure or inflict, divine spells and arcane inscriptions. Orisons belong to the divine side. |
| Class mod requirement | Included in Homebrew Classes. No separate class mod is required. |

### Overview

Your oracle’s curse is the mark the prophecy left on you. You follow the trail of failed predictions and copy what you learn from arcane scrolls into a research book. Before a dungeon, the question is which spells to ready. Once inside, you decide whether the same spell is worth both of that tier's casts.

With Spellbook Merge 1.7.1, the Intelligence-based main spellbook follows normal oracle mythic merge paths and qualifies for divine prestige advancement. The arcane research book advances only with oracle levels. A separate class must supply 2nd-level arcane spells for Mystic Theurge entry. Research spells use a base save DC of 10 + spell level + Intelligence modifier, including the usual Spell Focus and Heighten Spell adjustments.

### What you give up

| Oracle feature surrendered | What changes |
|---|---|
| Level 1 mystery and its benefits | The mystery's bonus spells and class skills go with it. You gain the arcane research book alongside your divine spellbook. |
| Revelations at 1, 3, 7, 11, 15 and 19, and the final revelation at 20 | Each oracle level brings arcane research choices. Your divine spellcasting still progresses through 9th-tier spells. |
| Native orisons and main spellbook | The Intelligence-based oracle book replaces the Charisma-based book. Orisons are regranted with Intelligence as well. |

### What you gain (by level)

Each oracle level offers two arcane spell picks. The fourth column is the **maximum library size** if you fill every open tier. Unlocking a tier does not instantly give you four new spells. Scrolls can fill the space left by level-up choices.

| Oracle level | New tier | Inscribe / prepare / cast at that tier | Total library capacity | Daily casts across all tiers |
|---|---|---|---|---|
| 1 | 1 | 4 / 2 / 2 | 4 | 2 |
| 4 | 2 | 4 / 2 / 2 | 8 | 4 |
| 6 | 3 | 4 / 2 / 2 | 12 | 6 |
| 8 | 4 | 4 / 2 / 2 | 16 | 8 |
| 10 | 5 | 4 / 2 / 2 | 20 | 10 |
| 12 | 6 | 4 / 2 / 2 | 24 | 12 |
| 14 | 7 | 4 / 2 / 2 | 28 | 14 |
| 16 | 8 | 4 / 2 / 2 | 32 | 16 |
| 18 | 9 | 4 / 2 / 2 | 36 | 18 |

Caster level keeps increasing at 19 and 20. At level 20 research DC still depends on spell level, and tier 9 still has two casts. The total assumes you use both casts at every tier. Unspent tier 1 uses cannot pay for another tier 9 spell.

### Abilities in detail

#### Reading the two spellbooks

| Aspect | Divine spellbook | Arcane research book |
|---|---|---|
| Spell list | Oracle orisons and divine spells of tiers 1–9 | Arcane spells of tiers 1–9 from the wizard list |
| Spells available today | Cast spontaneously from known divine spells | Inscribe up to four distinct spells per tier and prepare two of them |
| Daily allowance | Oracle daily slots minus two at each unlocked research tier | Two casts per tier, shared by both prepared spells of that tier |
| Role of Intelligence | Intelligence takes over as the divine casting ability | Raises save DC. The limits of four inscribed, two prepared and two daily casts stay fixed. |

Arcane tiers 1 through 9 unlock at oracle levels **1, 4, 6, 8, 10, 12, 14, 16 and 18**. Arcane caster level equals oracle class level. Base save DC is **10 + spell level + Intelligence modifier**. A level 12 oracle with +5 Intelligence unlocks tier 6 and has base DC 16 for a tier 1 spell and DC 21 for a tier 6 spell. Spell Focus and Heighten Spell follow the usual spell rules.

#### From character creation to the first rest

Choose your oracle curse and cure or inflict option, then make separate divine and research spell choices. Cure or inflict belongs to your divine progression. Your first research picks come from the 1st-tier wizard list. Open the arcane book to set your preparations and rest before heading out. When placing spells on the action bar, keep track of which book supplied the spell.

#### What happens to a prepared spell you replace?

Replacing a spell such as Shield removes preparations based on that inscription and its associated custom metamagic versions in the research book. Choose the newly learned spell for preparation and rest to use it the next day. To change just today's loadout, leave the four inscriptions in place and change the two preparations.

An identically named spell in the divine book keeps its own entry. An arcane replacement acts on the selected research inscription. Inscribing a duplicate at the same tier cannot increase casts. A scroll is eligible only if its spell belongs to the research wizard list and its tier is already open.

#### How large is the divine spellbook?

Each tier unlocked in the research book reserves two of the oracle’s daily divine slots for arcane research. Tiers reached early through prestige advancement or mythic merging keep their full divine allowance until research catches up. Orisons and tier 10 follow their normal rules. The table shows base spells known and remaining divine casts for a single-class oracle. Intelligence adds bonus slots to the divine book.

| Oracle level | Base spells known per tier | Base daily casts per tier |
|---|---|---|
| 1 | 2 | 1 |
| 4 | 3 / 1 | 4 / 1 |
| 6 | 4 / 2 / 1 | 4 / 3 / 1 |
| 8 | 5 / 3 / 2 / 1 | 4 / 4 / 3 / 1 |
| 10 | 5 / 4 / 3 / 2 / 1 | 4 / 4 / 4 / 3 / 1 |
| 12 | 5 / 5 / 4 / 3 / 2 / 1 | 4 / 4 / 4 / 4 / 3 / 1 |
| 14 | 5 / 5 / 4 / 4 / 3 / 2 / 1 | 4 / 4 / 4 / 4 / 4 / 3 / 1 |
| 16 | 5 / 5 / 4 / 4 / 4 / 3 / 2 / 1 | 4 / 4 / 4 / 4 / 4 / 4 / 3 / 1 |
| 18 | 5 / 5 / 4 / 4 / 4 / 3 / 3 / 2 / 1 | 4 / 4 / 4 / 4 / 4 / 4 / 4 / 3 / 1 |
| 20 | 5 / 5 / 4 / 4 / 4 / 3 / 3 / 3 / 3 | 4 / 4 / 4 / 4 / 4 / 4 / 4 / 4 / 4 |

At level 20, tier 9 has four base divine casts and two arcane casts. With Intelligence 28, the tier 9 bonus gives five divine casts and two arcane casts. Across tiers 1–9, that is 51 divine plus 18 arcane casts per day, for a total of 69.

### Choices

1. **Inscribe.** Choose two arcane spells per oracle level or learn from a scroll. When a tier already contains four spells, choose an existing spell of *that tier* to replace. Cancelling a scroll replacement keeps both the old spell and the scroll.
2. **Prepare.** Pick two inscriptions from the tier for the next outing. Preparation changes take effect after rest and leave the other inscriptions available for a later day.
3. **Cast.** If you ready *shield* and *true strike* at tier 1, you can cast *shield* twice or cast each spell once. Either choice spends both tier 1 research casts for the day. Higher arcane tiers and divine spell slots have their own allowances.

### Feats

Mystery-dependent revelations lose their selection slots. Regular feat picks follow the oracle, and arcanist-style preparation applies specifically to the research book. It grants neither arcanist exploits nor an arcane reservoir. Its cast count can therefore be tracked independently of divine slots and reservoir abilities from any other class.

### Resources

| What you change | Result |
|---|---|
| Intelligence modifier rises from +4 to +6 | Research base save DC rises by 2 at the same oracle level. Library capacity, preparations and casts stay fixed. |
| Spend the second tier 1 cast | Both prepared tier 1 spells now share an empty pool. Higher tiers keep their remaining uses. |
| Cancel a scroll swap in a full tier | All four inscriptions and the scroll remain. Select and confirm which spell to remove to complete the replacement. |

### Compatibility and notes

Highest research tier and caster level use **oracle class level**. An oracle 8 / other class 4 has tier 4 research, caster level 8 and a tier 4 base DC of 14 + Intelligence modifier. A single-class level 12 oracle has tier 6 and caster level 12. Normal oracle weapon and armor proficiencies remain, the research book is arcane and orisons remain Intelligence-based divine magic.

</details>


## Hell Rider

<details>
<summary><b>Hell Rider</b></summary>

### At a glance

| Aspect | Details |
|---|---|
| Base class | Cavalier. Retains order, challenge and the level 3 Cavalier's Charge. |
| Key abilities | Strength supports the rider's melee attacks. Charisma governs the devils' save DC, the ray's accuracy and uses, and the standard's daily uses. |
| Mount | The nightmare advances as a horse companion. Each successful hoof attack adds 1d4 fire damage, and it learns to breathe fire at level 8. |
| Magic | Four spell-like abilities arrive by class level with no spellbook or slots. The nightmare spends its own action on its breath. |
| Class mod requirement | Included in Homebrew Classes. No separate class mod is required. |

### Overview

A Hell Rider picks a charge target and a place to plant the standard for different reasons. Charge an isolated enemy to concentrate the devils' blows. Against a group holding a passage, the standard can pressure that ground for five rounds while the nightmare breathes fire across the front line. The devils appear for their strikes, while the nightmare stays at your side as a companion.

### What you give up

| Level | Base feature | Replacement |
|---|---|---|
| 1 | Standard mount selection | Nightmare Mount |
| 1, 9, 17 | Three tactician teamwork feats | Lose the level 1 sharing ability and its level 9 swift-action upgrade. |
| 5, 14 | Banner and Greater Banner | Infernal Standard and Greater Infernal Standard, planted at a chosen point |
| 11, 20 | Mighty Charge and Supreme Charge | The charge and the standard call two devils at level 11 and three at level 20 |

### What you gain (by level)

| Level | Feature | How it plays |
|---|---|---|
| 1, 3 | Nightmare Mount / Infernal Charge | From level 3, the first weapon attack of a mounted or unmounted charge calls devils, hit or miss. They strike other enemies within 10 feet of the charge target first. Any remaining devils strike the original target. |
| 5, 14 | Infernal Standard | Plant it within medium range as a standard action. For five rounds, each devil strikes a different enemy within 20 feet each round. At level 14 the radius grows to 30 feet, and allies inside gain a +2 morale bonus on attacks and immunity to fear. |
| 8 | Nightmare's Fire Breath | The mount spends its own standard action to breathe a 15-foot cone dealing 1d6 fire per two cavalier levels. A Reflex save halves damage. It recharges in 1d4 rounds. |
| 9 | Greater Command | Issue one command to enemies in the area. The four command options share one daily use. |
| 11, 15, 19 | Hellfire Ray | Make a ranged touch attack using Charisma. Fire one ray at level 11, two at 15, and three at 19 at the same foe. Each deals 1d6 per cavalier level, up to 15d6, split between fire and unholy damage. |
| 13 | Firebrand | Allies add 1d6 fire damage to weapon and natural attacks. Each bearer can expend their own brand to fire a ray. |
| 17 | Overwhelming Presence | Enemies that fail their Will save prostrate themselves and cower. Even an enemy that succeeds on the initial save is staggered for one round. |
| 20 | Third Devil | Charge and standard now call three devils. A charge can concentrate all three strikes on an isolated target. The standard can strike up to three different foes per round. |

### Abilities in detail

#### Damage from one apparition

Each devil rolls **1d6 per three cavalier levels** and splits the result between fire and unholy damage. A Reflex save halves the total. Its DC is **10 + half cavalier level (rounded down) + Charisma modifier**. The nightmare's breath uses the same level progression with the mount's own Constitution modifier.

#### How charge and standard assign devils

| Field at level 20 | Infernal Charge | One round of Infernal Standard |
|---|---|---|
| One isolated boss | All three devils strike the boss. | One strike on the boss. The other two do not converge on it. |
| Boss with one guard within 10 feet | One strike goes to the guard and two to the boss. | One strike each if both are within the standard's radius. |
| Boss with three guards within 10 feet | Each of the three nearby guards takes a devil strike. Your own charge attack still targets the boss. | Up to three distinct enemies nearest the center are struck. |

The apparitions strike after the rider's first charge attack. Each apparition forces its own Reflex save and deals its own damage. At level 20 an isolated boss faces three separate 6d6 packets, each with its own save. Fire resistance applies to the fire portion of each packet.

#### Which ability governs each roll?

| Ability | Roll or defense |
|---|---|
| Rider's weapon / nightmare's hooves | Each creature makes its own weapon attack. A nightmare hoof hit adds 1d4 fire. |
| Devil strike / nightmare breath | Reflex halves both. Devils use rider Charisma for DC, while the breath uses the mount's Constitution. |
| Hellfire Ray | Each ray uses Charisma for a ranged touch roll and retains the original spell's spell resistance. All rays aim at one target. |
| Greater Command / Overwhelming Presence | Base save DC is 10 + the ability's spell level + Charisma modifier. Greater Command is level 5 and Overwhelming Presence is level 9. |

At level 17 with +6 rider Charisma and +4 mount Constitution, a devil forces Reflex DC 24 and the breath forces Reflex DC 22. The same rider's Greater Command is Will DC 21 and Overwhelming Presence is Will DC 25. A target's touch AC and its two different saves make the choice between a ray, command and charge meaningful.

### Choices

#### The four Greater Commands

| Order | What the target does |
|---|---|
| Approach | Moves directly toward the rider as quickly as possible. This movement can provoke normally. |
| Fall | Falls prone and takes the usual penalties on actions it can perform while prone. |
| Flee | Spends its turn moving away from the rider as quickly as possible, which can open a path it was guarding. |
| Halt | Stays in place and takes no actions, while retaining defenses against effects that require helplessness. |

All creatures in the 30-foot radius receive the same order. From the round after the first, each can attempt another Will save to break free. Halt can hold enemies near a planted standard. Approach can draw them toward it if the rider is positioned there.

#### Keeping a firebrand or cashing it in

For one round per cavalier level, each firebranded ally adds 1d6 fire on weapon and natural attack hits. A bearer may spend **their own swift action** to fire a 30-foot ranged touch ray for 6d6 fire, ending their own brand. A frontliner can keep the bonus for repeated hits while another ally spends theirs on a distant foe.

#### The first save against Overwhelming Presence

The base duration is one round per cavalier level. A target that makes its initial Will save is staggered for one round. A target that fails prostrates itself and cowers, then gets another save each round to break free. Under the original spell's rules, recovering early after succumbing inflicts 1d6 Wisdom drain and 1d4 rounds of stagger. This differs from succeeding on the initial save.

### Feats

Your order, challenge and the bonus combat feats at levels 6, 12 and 18 follow cavalier progression. A newly selected nightmare grows as your actual animal companion. A multiclass rider who already has a companion can use the native continue-companion option when eligible. The level 8 breath belongs to the nightmare itself, so keeping a different companion does not give that animal Nightmare's Fire Breath.

### Resources

| Ability | Daily uses |
|---|---|
| Infernal Standard | 3 + Charisma modifier |
| Hellfire Ray | One plus one per two points of positive Charisma modifier. A volley spends a single use. |
| Greater Command / Firebrand / Overwhelming Presence | Once per day each. All three and Hellfire Ray take a standard action and use cavalier level as caster level. |

At level 19 with +6 Charisma, you can plant nine standards and cast Hellfire Ray four times per day. One casting makes three ranged touch attacks against the same foe, each hit dealing 15d6. You still have separate uses of Greater Command to control enemies and Firebrand to support allies.

| Cavalier level | Devils × dice per devil | One casting of Hellfire Ray |
|---|---|---|
| 3 | 1 × 1d6 | — |
| 8 | 1 × 2d6 | — |
| 11 | 2 × 3d6 | 1 × 11d6 |
| 15 | 2 × 5d6 | 2 × 15d6 |
| 19 | 2 × 6d6 | 3 × 15d6 |
| 20 | 3 × 6d6 | 3 × 15d6 |

The change from 19 to 20 is the third devil. The ray volley reached three at level 19. The nightmare rolls its own 1d4-round breath recharge without spending any entry in the daily-use table.

### Compatibility and notes

A planted standard holds that point for five rounds. From level 14 its accuracy bonus and fear immunity also depend on allies staying inside the radius. A dismounted charge still calls devils when riding is impractical. The nightmare is a controllable companion, apparitions resolve charge and standard strikes, and an ally's firebrand ray spends that ally's brand without drawing from the rider's Hellfire Ray pool.

</details>
