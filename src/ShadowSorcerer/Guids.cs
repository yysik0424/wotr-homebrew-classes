namespace ShadowSorcerer
{
    // 주의: 아래 새 GUID 는 커밋되는 순간 영구 고정이다(세이브 호환). 절대 바꾸거나 재발급하지 말 것.
    // 이 파일은 게임 타입 없이 문자열만 둔다(콘솔 시험이 컴파일한다, modinfo testSources). 발급: [guid]::NewGuid().ToString('N') (2026-11-14).
    public static class Guids
    {
         
        public const string Archetype = "cb36f23480474120ac8a4f6e1cf5d660";
        public const string EchoFeature = "6d18e67971f04792a355c6eb557d85f6";
        public const string Doubles1 = "92868a6752ad4dfcbbd255385ed820ee";
        public const string Doubles2 = "f24346c80c354b0a9328a0488558c566";
        public const string Doubles3 = "1cc892c92289461fbc6c8751467f1a4e";
         
        public const string Bonus1 = "6949caadce5949e580965f6f145ec6b8";
        public const string Bonus2 = "ac5d20feb01b4e0683299535fccf2909";
        public const string Bonus3 = "4f5b2bca8c6d4bb094fb8f42ab486d33";
        public const string Bonus4 = "1233dfe9c90d495894317039f727d2ce";
        public const string Bonus5 = "fa27b5a78e804333a9c9551495ae5bb9";
        public const string Bonus6 = "3f24337af04d4996844d16073848020c";
        public const string Bonus7 = "63ac9cb82cde40038676f2b4c8a22fc9";
        public const string Bonus8 = "a111bbad0f004ab891bc6957befcdd6a";
        public const string Bonus9 = "6a05747fe7684766a6602b32f87883fe";
        public const string SlotsFeature = "3d8bc0c6c8fb43e78e57a65fa0f0e01f";
        public static readonly string[] BonusFeatures = { Bonus1, Bonus2, Bonus3, Bonus4, Bonus5, Bonus6, Bonus7, Bonus8, Bonus9 };
        // 2026-11-17 그림자 메아리 연출: 시각 전용 그림자 유닛의 껍데기 특성(숨김 — 대상 불가·AI 없음·전투 제외). 발급 2026-11-17.
        public const string ShadowDoubleShell = "249d4c90016e410d86fa4ad36fc38586";

        // BlueprintUtil.Create 로 만들지 않는 우리 블루프린트(본편 유닛의 사본 — BlueprintUtil.CloneUnit). 아이콘 원장·본편 쓰기 스캐너의 「Create 로 만든 것」 집합에 들지 않으므로 따로 둔다.
        public static class Visual
        {
            // 시각 전용 그림자 유닛(본편 Shadow 13e32ff7… 의 사본). 저장되지 않는 유닛이지만 GUID 는 고정한다. 발급 2026-11-17.
            public const string ShadowDoubleUnit = "8a5ab8dcb9bd481292fa41f802a59b96";
        }

        // 본편 GUID. 출처: blueprints.zip 2026-11-14(md5 5da11edf2659) 직독 — 시험 `jbp: game guids` 가 32자리·존재·$type 을 대조한다.
        public static class Game
        {
            public const string SorcererClass = "b3a505fb61437dc4097f43c3f8f9a4cf";              // Classes/Sorcerer/SorcererClass.jbp
            public const string SorcererProgression = "997665565ca80a649aedd72455c4df1f";        // Classes/Sorcerer/SorcererProgression.jbp
            public const string SorcererSpellbook = "b3db3766a4b605040b366265e2af0e50";          // BlueprintSpellbook
            public const string SorcererSpellsDailyTable = "dbff16956df2eda48a1da5c9617cc836";   // BlueprintSpellsTable
            public const string SorcererFeatSelection = "3a60f0c0442acfb419b0c03b584e1394";      // L7/13/19
            public const string SorcererBloodlineSelection = "24bef8d1bee12274686f6da6ccbc8914"; // L1
            public const string WizardSpellList = "ba0401fdeb4062f40a7aa95b6f07fe89";            // SorcererSpellbook.m_SpellList
            public const string BloodlineArcaneProgression = "4d491cf9631f7e9429444f4aed629791"; // 점검 혈통(Classes/Sorcerer/ArcaneBloodline)
            // 2026-11-15 생성 불변식 ③: 엔진 기본값으로 채워지던 L1 선택을 점검 유닛(ShadowTestUnit.Create)에서 명시(blueprints.zip 직독, 각 선택의 m_AllFeatures 첫 항목).
            public const string BackgroundsBaseSelection = "f926dabeee7f8a54db8f2010b323383c";           // Backgrounds/BackgroundsSelection/BackgroundsBaseSelection.jbp
            public const string BackgroundNone = "7d300f497584d9245ac24c062dce0bd6";                     // Backgrounds/BackgroundsSelection/BackgroundNone.jbp(효과 없음)
            public const string BloodlineArcaneArcaneBondFeature = "1740a701ac0a14c4394a7f76f0b07799";   // Classes/Sorcerer/ArcaneBloodline/BloodlineArcaneArcaneBondFeature.jbp(선택, 비전 혈통 L1)
            public const string BloodlineArcaneItemBondFeature = "473fca42a1f049ef88ba8824c9827d87";     // .../BloodlineArcaneItemBond/BloodlineArcaneItemBondFeature.jbp(물건 결속 — 사역마 아님 = 펫 없음)
            public const string BloodlineArcaneClassSkillSelection = "19ab16dba857d1a4ba617074f203f975"; // Classes/Sorcerer/ArcaneBloodline/BloodlineArcaneClassSkillSelection.jbp(L1)
            public const string BloodlineArcaneClassSkill1 = "3e833ef2e85a82a4395516cfc55113b7";         // Classes/Sorcerer/ArcaneBloodline/BloodlineArcaneClassSkill1.jbp(AddClassSkill 종교 지식)
            public const string SorcererBonusFeat = "d6dd06f454b34014ab0903cb1ed2ade3";                  // Classes/Sorcerer/SorcererBonusFeat.jbp(선택, SorcererProgression L1)
            public const string CombatCasting = "06964d468fde1dc4aa71a92ea04d930d";                      // Feats/CombatCasting.jbp(집중 보너스만, 선행 = 지능 3)
            // 점검용 주문.
            public const string MagicMissile = "4ac47ddb9fa1eaf43a1b6809980cfbd2";
            public const string Fireball = "2d81362af43aeac4387a3d4fced489c3";
            public const string LightningBolt = "d2cff9243a7ee804cb6d5be47af30c73"; // Spells/Level3/LightningBolt.jbp(AbilityDeliverProjectile Type = Line, 120 ft · 폭 5 ft — S15 선 메아리 단계)
            public const string MageArmor = "9e1ad5d6f87d19e4d8883d63a6e35568";
            public const string CureLightWounds = "47808d23c67033d4bbab86a1070fd62f";
            public const string SummonMonsterISingle = "8fd74eddd9b6c224693d9ab241f25e84";
            public const string AcidSplash = "0c852a2405dd9f14a8bbcfaf245ff823";
            public const string HoldPerson = "c7104f7526c4c524f91474614054547e";
            public const string PhantasmalKiller = "6717dbaef00c0eb4897a1c908a75dfe5";
            public const string RayOfEnfeeblement = "450af0402422b0b4980d9c2175869612";
            public const string RayOfEnfeeblementBuff = "7dc2c05760ed40a49af10c1ce6ded033";
            public const string StinkingCloud = "68a9e6d7256f1354289a39003a46d826";
            public const string CausticEruption = "8c29e953190cc67429dc9c701b16b7c2";
            public const string ShockingGraspCast = "ab395d2335d3f384e99dddee8562978f";
            public const string ShockingGraspEffect = "17451c1327c571641a1345bd31155209";
            public const string ScrollOfMagicMissile = "63caf94a780472b448f50d0bc183c38f";
            // Shadow Evocation 은 보너스 주문 5행(ShadowRules.BonusSpells[4])에서 읽는다 — 같은 guid 를 두 곳에 두지 않는다(C4).
            // 2026-11-17 그림자 메아리 연출(blueprints.zip 직독 2026-11-17).
            public const string ShadowUnit = "13e32ff7f7a54b6b95cb36366bf5dc00";    // Classes/Wizard/Archetypes/ShadowCaster/SummonShadows/Shadow.jbp (BlueprintUnit — 분신 형상의 원본)
            public const string ShadowFxBuff = "8caafe9dc0ff21041b36ad225569d164";  // Buffs/ShadowFX.jbp (BlueprintBuff — 검은 그림자 외형)
            public const string PlayerFaction = "72f240260881111468db610b6c37c099"; // Factions/Player.jbp (BlueprintFaction)
        }
    }
}
