using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace BulwarkMage
{
     
    // 행 = (블루프린트 GUID, 컴포넌트 형식, 필드 경로, 기대값, 종류, 모드). 런타임 되읽기(RuntimeReadback → ReadbackCore)가 모든 모드의 BlueprintsCache.Init 뒤
    // 실제 값을 읽어 Judge 에 넘긴다. 「정확히 N개 컴포넌트」 가드는 두지 않는다(무령 주술사 2026-11-14 사고): 컴포넌트 집합은 행이 컴포넌트를 읽는 블루프린트에서만,
    // 행·허용 행이 이름 붙인 형식 밖의 형식이 있을 때만 거부한다(개수가 아니라 형식). T1 행은 전부 블루프린트 자체 필드라 컴포넌트 집합을 보지 않는다.
    public enum ToleranceKind
    {
        MustEqual,                 // 실제 == 기대
        AllowExtraComponentOfType, // 값 검사가 아니라 선언: 그 블루프린트에 이 형식의 외부 컴포넌트가 있어도 된다(모드 이름 필수)
        KnownForeignValue,         // 실제 == 기대 또는 실제 == 그 모드의 알려진 값(모드 이름·값 필수)
    }

    // 기대값의 출처. Vanilla = blueprints.zip 의 본편 값(시험 `jbp: readback rows vs jbp` 가 대조). OurWrite = 우리가 쓴 값(등록 예외 — jbp 에는 없다).
    public enum RowOrigin { Vanilla, OurWrite }

    // 필드 경로 문법(작고 닫힌 문법 — 판독기는 이것만 안다):
    //   <field>                  필드 값(참조면 GUID, null 이면 "null", 수·열거형은 불변 문화 문자열)
    //   <field>.<field>…         안쪽 객체의 필드(T2: ContextValue.Value 등 — 중간 값이 null 이면 읽기 실패)
    //   <field>#count=<guid>     블루프린트 참조 배열/목록에서 그 GUID 의 개수
    //   <field>@<n>#count=<guid> LevelEntry 배열에서 Level == n 인 항목들의 특성 중 그 GUID 의 개수
    //   Components#type=<형식 전체 이름>  블루프린트 자체 행 전용: 그 형식(정확히 같은 런타임 형식)의 컴포넌트 개수(T2 — 같은 형식이 하나 더 붙는 외부 변형도 드러난다)
    public sealed class ReadbackPath
    {
        public const string ComponentsField = "Components";
        static readonly Regex Rx = new Regex(@"^(?<f>[A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z_][A-Za-z0-9_]*)*)(?:@(?<l>[0-9]+))?(?:#count=(?<g>[0-9a-f]{32})|#type=(?<t>[A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z_][A-Za-z0-9_]*)+))?$");
        public readonly string Field;          // 첫 필드
        public readonly IReadOnlyList<string> Segments; // 점으로 나눈 전체(길이 >= 1, Segments[0] == Field)
        public readonly int? Level;
        public readonly string CountGuid;
        public readonly string TypeName;       // Components#type= 의 형식 전체 이름
        ReadbackPath(string[] segs, int? l, string g, string t) { Segments = segs; Field = segs[0]; Level = l; CountGuid = g; TypeName = t; }

        public static ReadbackPath Parse(string path)
        {
            if (path == null) throw new ArgumentNullException("path");
            var m = Rx.Match(path);
            if (!m.Success) throw new ArgumentException("BulwarkMage: bad readback field path '" + path + "'", "path");
            var segs = m.Groups["f"].Value.Split('.');
            int? level = null;
            if (m.Groups["l"].Success)
            {
                int l = int.Parse(m.Groups["l"].Value);
                if (l < BulwarkRules.Levels.MinLevel || l > BulwarkRules.Levels.MaxLevel) throw new ArgumentException("BulwarkMage: readback field path level " + l + " outside 1..20 in '" + path + "'", "path");
                if (!m.Groups["g"].Success) throw new ArgumentException("BulwarkMage: readback field path '" + path + "' filters a level but counts nothing (add #count=<guid>)", "path");
                if (segs.Length != 1) throw new ArgumentException("BulwarkMage: readback field path '" + path + "' filters a level on a nested field (level filters read a LevelEntry array field directly)", "path");
                level = l;
            }
            string type = m.Groups["t"].Success ? m.Groups["t"].Value : null;
            if (type != null && (segs.Length != 1 || segs[0] != ComponentsField)) throw new ArgumentException("BulwarkMage: readback field path '" + path + "' counts by type on a field other than " + ComponentsField, "path");
            if (type == null && segs.Length == 1 && segs[0] == ComponentsField) throw new ArgumentException("BulwarkMage: readback field path '" + path + "' reads " + ComponentsField + " without #type= (only component counts are defined)", "path");
            return new ReadbackPath(segs, level, m.Groups["g"].Success ? m.Groups["g"].Value : null, type);
        }

        public override string ToString() { return string.Join(".", Segments.ToArray()) + (Level.HasValue ? "@" + Level.Value : "") + (CountGuid != null ? "#count=" + CountGuid : "") + (TypeName != null ? "#type=" + TypeName : ""); }
    }

    // 컴포넌트 지정 문법(T2): "" = 블루프린트 자체 · <형식 전체 이름> = 그 형식 컴포넌트가 정확히 1개일 때 그것 · <형식 전체 이름>@<i> = 그 형식 컴포넌트 중 i번째(0부터, 배열 순서).
    // 같은 형식이 여럿인 블루프린트(TowerShieldTrainingBuff 의 MaxDexBonusIncrease ×3 등)는 색인을 쓰고, 개수 자체는 Components#type= 행이 잠근다.
    public sealed class ComponentSpec
    {
        static readonly Regex Rx = new Regex(@"^(?<t>[A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z_][A-Za-z0-9_]*)+)(?:@(?<i>[0-9]+))?$");
        public readonly string TypeName;
        public readonly int? Index;
        ComponentSpec(string t, int? i) { TypeName = t; Index = i; }

        public static ComponentSpec Parse(string componentType)
        {
            if (componentType == null) throw new ArgumentNullException("componentType");
            if (componentType == "") return null; // 블루프린트 자체
            var m = Rx.Match(componentType);
            if (!m.Success) throw new ArgumentException("BulwarkMage: bad readback component type '" + componentType + "' (expected a full type name, optionally @<index>)", "componentType");
            return new ComponentSpec(m.Groups["t"].Value, m.Groups["i"].Success ? int.Parse(m.Groups["i"].Value) : (int?)null);
        }
    }

    public sealed class Row
    {
        public readonly string Guid, Name, ComponentType, FieldPath, Expected, Mod, ForeignValue, Source;
        public readonly ToleranceKind Kind;
        public readonly RowOrigin Origin;
        public readonly ReadbackPath Path;
        public readonly ComponentSpec Component; // null = 블루프린트 자체

        static readonly Regex Hex32 = new Regex("^[0-9a-f]{32}$");

        public Row(string guid, string name, string componentType, string fieldPath, string expected, ToleranceKind kind, RowOrigin origin, string source, string mod = null, string foreignValue = null)
        {
            if (guid == null || !Hex32.IsMatch(guid)) throw new ArgumentException("BulwarkMage: readback row GUID '" + guid + "' is not 32 lowercase hex", "guid");
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("BulwarkMage: readback row " + guid + " has no name", "name");
            if (componentType == null) throw new ArgumentNullException("componentType", "BulwarkMage: readback row " + name + ": use \"\" for the blueprint itself");
            if (!Enum.IsDefined(typeof(ToleranceKind), kind)) throw new ArgumentException("BulwarkMage: readback row " + name + " has undefined kind " + (int)kind);
            if (!Enum.IsDefined(typeof(RowOrigin), origin)) throw new ArgumentException("BulwarkMage: readback row " + name + " has undefined origin " + (int)origin);
            if (string.IsNullOrEmpty(source)) throw new ArgumentException("BulwarkMage: readback row " + name + " has no source", "source");
            Component = ComponentSpec.Parse(componentType);
            if (kind == ToleranceKind.AllowExtraComponentOfType)
            {
                if (componentType == "" || fieldPath != "" || expected != "" || string.IsNullOrEmpty(mod) || foreignValue != null)
                    throw new ArgumentException("BulwarkMage: allowance row " + name + " needs a component type and a mod, and no field path / expected / foreign value");
                if (Component.Index.HasValue) throw new ArgumentException("BulwarkMage: allowance row " + name + " names a type, not one indexed component");
            }
            else
            {
                if (fieldPath == null || expected == null) throw new ArgumentException("BulwarkMage: value row " + name + " needs a field path and an expected value");
                Path = ReadbackPath.Parse(fieldPath);
                if (Path.TypeName != null && componentType != "") throw new ArgumentException("BulwarkMage: value row " + name + " counts components by type but reads a component (Components#type= is a blueprint-itself row)");
                if (kind == ToleranceKind.MustEqual && (mod != null || foreignValue != null)) throw new ArgumentException("BulwarkMage: MustEqual row " + name + " must not name a mod or a foreign value");
                if (kind == ToleranceKind.KnownForeignValue && (string.IsNullOrEmpty(mod) || foreignValue == null || foreignValue == expected))
                    throw new ArgumentException("BulwarkMage: KnownForeignValue row " + name + " needs a mod and a foreign value different from the expected value");
            }
            Guid = guid; Name = name; ComponentType = componentType; FieldPath = fieldPath; Expected = expected; Kind = kind; Origin = origin; Source = source; Mod = mod; ForeignValue = foreignValue;
        }

        public bool IsValueRow { get { return Kind != ToleranceKind.AllowExtraComponentOfType; } }
         
        public string Key { get { return Guid + "|" + ComponentType + "|" + FieldPath; } }
        public override string ToString() { return Name + " [" + Key + "] " + Kind + " expected '" + Expected + "'"; }
    }

    public static class ForeignTolerance
    {
        // 판정. actual = 판독기가 읽은 값(읽지 못했으면 null — 판정 불가는 통과가 아니다). extraComponentTypes = 그 블루프린트에서 어떤 행·허용 행도 이름 붙이지 않은
        // 컴포넌트 형식(컴포넌트를 읽는 행에서만 채운다; 블루프린트 자체 필드 행은 빈 목록).
        public static Tuple<bool, string> Judge(Row row, string actual, IEnumerable<string> extraComponentTypes)
        {
            if (row == null) throw new ArgumentNullException("row");
            if (extraComponentTypes == null) throw new ArgumentNullException("extraComponentTypes", "BulwarkMage: Judge needs the extra component list (empty when the row reads the blueprint itself)");
            if (!row.IsValueRow) throw new ArgumentException("BulwarkMage: Judge called with allowance row " + row.Name + " (allowance rows are not values)");
            var extras = extraComponentTypes.ToArray();
            if (extras.Any(string.IsNullOrEmpty)) return Tuple.Create(false, row.Name + ": a component on the blueprint has no type name (null component?) - cannot judge");
            if (actual == null) return Tuple.Create(false, row.Name + ": value at " + row.FieldPath + " could not be read - cannot judge");
            if (extras.Length > 0) return Tuple.Create(false, row.Name + ": unknown component type(s) on the blueprint: " + string.Join(", ", extras.Distinct().ToArray()) + " (no row or allowance names them)");
            if (actual == row.Expected) return Tuple.Create(true, row.Name + ": ok (" + actual + ")");
            if (row.Kind == ToleranceKind.KnownForeignValue && actual == row.ForeignValue)
                return Tuple.Create(true, row.Name + ": known foreign value '" + actual + "' from " + row.Mod);
            return Tuple.Create(false, row.Name + ": value differs at " + row.FieldPath + ": expected '" + row.Expected + "', got '" + actual + "'" + (row.Kind == ToleranceKind.KnownForeignValue ? " (known foreign value from " + row.Mod + " is '" + row.ForeignValue + "')" : ""));
        }

         
        //   WizardClass.m_Archetypes 에 우리가 정확히 1번(등록 예외의 결과가 다른 모드의 나중 대입으로 사라지지 않았는가 — 유일한 OurWrite 행).
        //   WizardClass 의 진행표·주문서가 본편 그대로(다른 진행표로 바뀌면 우리 RemoveFeatures 가 엉뚱한 표를 겨눈다).
        //   WizardProgression 의 제거 대상 8쌍이 각 레벨에 정확히 1번(없어지면 아키타입 선택 불가, 둘이 되면 제거 의미가 달라진다).
        // 외부 모드 후보 조사(dnlib, 2026-11-14)는 이 값들에 쓰는 설치 모드를 찾지 못했다 — 그래서 KnownForeignValue·허용 행이 없다(보고서 §Step 0-F).
        public static readonly IReadOnlyList<Row> Rows = BuildRows();

        static Row[] BuildRows()
        {
            const string Recon = "blueprint recon (jbp 2026-11-14, blueprints.zip md5 5da11edf2659)";
            var rows = new List<Row>
            {
                new Row(Guids.Game.WizardClass, "WizardClass.m_Archetypes has ours once", "", "m_Archetypes#count=" + Guids.Archetype, "1", ToleranceKind.MustEqual, RowOrigin.OurWrite, "Archetype.Register (plan Global Constraints, registration exception)"),
                new Row(Guids.Game.WizardClass, "WizardClass.m_Progression", "", "m_Progression", Guids.Game.WizardProgression, ToleranceKind.MustEqual, RowOrigin.Vanilla, Recon),
                new Row(Guids.Game.WizardClass, "WizardClass.m_Spellbook", "", "m_Spellbook", Guids.Game.WizardSpellbook, ToleranceKind.MustEqual, RowOrigin.Vanilla, Recon),
            };
            foreach (var r in BodyTable.Removed)
                rows.Add(new Row(Guids.Game.WizardProgression, "WizardProgression L" + r.level + " has " + r.guid + " once", "", "LevelEntries@" + r.level + "#count=" + r.guid, "1", ToleranceKind.MustEqual, RowOrigin.Vanilla, Recon + " / recon section 8"));
            AddRebukeReuseRows(rows);
            AddTowerShieldRows(rows);
            AddBondRows(rows);
            return rows.ToArray();
        }

         
        // 외부 모드 조사(dnlib 2026-11-14): 이 7개를 쓰는 모드 없음(TTT-Base DynamicItemNames 는 ArmorEnhancementBonus1~4 를 이름 표시에 읽기만, TTT-Base 는 MagicalVestmentShieldBuff 를 자기 컴포넌트로 바꾸지만 우리는 그 블루프린트를 쓰지 않는다) → 전부 MustEqual.
        static void AddBondRows(List<Row> rows)
        {
            const string S = "foreign writer survey, bonded shield (jbp 2026-11-14, blueprints.zip md5 5da11edf2659)";
            const string Arl = "Kingmaker.UnitLogic.Abilities.Components.AbilityResourceLogic", Arss = "Kingmaker.UnitLogic.Abilities.Components.AbilityRestoreSpellSlot",
                Aeb = "Kingmaker.Blueprints.Items.Ecnchantments.ArmorEnhancementBonus", Aas = "Kingmaker.Designers.Mechanics.Facts.AdvanceArmorStats";
            string g = Guids.Game.ItemBondAbility;
            rows.Add(V(g, "ItemBondAbility AbilityResourceLogic.m_RequiredResource", Arl, "m_RequiredResource", Guids.Game.ItemBondResource, S));
            rows.Add(V(g, "ItemBondAbility AbilityResourceLogic.Amount", Arl, "Amount", "1", S));
            rows.Add(V(g, "ItemBondAbility AbilityResourceLogic.m_IsSpendResource", Arl, "m_IsSpendResource", "True", S));
            rows.Add(V(g, "ItemBondAbility AbilityResourceLogic.CostIsCustom", Arl, "CostIsCustom", "False", S));
            rows.Add(V(g, "ItemBondAbility AbilityRestoreSpellSlot.AnySpellLevel", Arss, "AnySpellLevel", "True", S));
            rows.Add(V(g, "ItemBondAbility AbilityRestoreSpellSlot.CheckSpellBook", Arss, "CheckSpellBook", "True", S));
            rows.Add(V(g, "ItemBondAbility AbilityRestoreSpellSlot.SpellbooksReference has WizardSpellbook", Arss, "SpellbooksReference#count=" + Guids.Game.WizardSpellbook, "1", S));
            rows.Add(N(g, "ItemBondAbility", Arl, 1, S));
            rows.Add(N(g, "ItemBondAbility", Arss, 1, S));
            g = Guids.Game.ItemBondResource;
            rows.Add(V(g, "ItemBondResource.m_MaxAmount.BaseValue", "", "m_MaxAmount.BaseValue", "1", S));
            rows.Add(V(g, "ItemBondResource.m_MaxAmount.IncreasedByLevel", "", "m_MaxAmount.IncreasedByLevel", "False", S));
            rows.Add(V(g, "ItemBondResource.m_MaxAmount.IncreasedByStat", "", "m_MaxAmount.IncreasedByStat", "False", S));
            rows.Add(V(g, "ItemBondResource.m_MaxAmount.IncreasedByLevelStartPlusDivStep", "", "m_MaxAmount.IncreasedByLevelStartPlusDivStep", "False", S));
            rows.Add(V(g, "ItemBondResource.m_Min", "", "m_Min", "0", S));
            rows.Add(V(g, "ItemBondResource.m_UseMax", "", "m_UseMax", "False", S));
            for (int n = 1; n <= BondTable.EnhancementGuids.Count; n++)
            {
                g = BondTable.EnhancementGuid(n);
                string bp = "ArmorEnhancementBonus" + n;
                rows.Add(V(g, bp + " ArmorEnhancementBonus.EnhancementValue", Aeb, "EnhancementValue", n.ToString(System.Globalization.CultureInfo.InvariantCulture), S));
                rows.Add(V(g, bp + " AdvanceArmorStats.ArmorCheckPenaltyShift", Aas, "ArmorCheckPenaltyShift", "1", S));
                rows.Add(V(g, bp + " AdvanceArmorStats.MaxDexBonusShift", Aas, "MaxDexBonusShift", "0", S));
                rows.Add(V(g, bp + " AdvanceArmorStats.ArcaneSpellFailureShift", Aas, "ArcaneSpellFailureShift", "0", S));
                rows.Add(N(g, bp, Aeb, 1, S));
                rows.Add(N(g, bp, Aas, 1, S));
            }
        }

        // T3 행 — 타워실드 스페셜리스트 기능(그대로 참조 6 + 복제 원본 2 + 접촉 방패 버프) · 갑옷 훈련 · TowerShieldType · Quarterstaff(H1 이 읽는 무기 형식)
         
        // 외부 모드 조사(dnlib 2026-11-14): TTT-Base EnableAdvancedArmorTraining · PrestigePlus PatchArmorTraining 은 FighterClass 진행표/아키타입만 고친다(ArmorTraining 블루프린트 쓰기 없음),
        //   PrestigePlus OneHandGripFix 는 같은 CanTakeOneHand 의 Postfix(블루프린트 쓰기 아님). 이 값들에 쓰는 모드 없음 → 전부 MustEqual.
        static void AddTowerShieldRows(List<Row> rows)
        {
            const string S = "foreign writer survey, tower shield (jbp 2026-11-14, blueprints.zip md5 5da11edf2659)";
            const string Hsfu = "Kingmaker.Designers.Mechanics.Buffs.HasShieldFeatureUnlock", Crc = "Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig",
                Recalc = "Kingmaker.Designers.Mechanics.Facts.RecalculateOnStatChange", Fct = "Kingmaker.Designers.Mechanics.Facts.FactsChangeTrigger",
                Sfr = "Kingmaker.Designers.Mechanics.Facts.SavesFixerRecalculate", Dex = "Kingmaker.Designers.Mechanics.Facts.MaxDexBonusIncrease",
                Acp = "Kingmaker.Designers.Mechanics.Facts.ArmorCheckPenaltyIncrease", Afca = "Kingmaker.UnitLogic.Mechanics.Components.AddFactContextActions",
                Evasion = "Kingmaker.Designers.Mechanics.Facts.EvasionWithTowerShield";
            string g = Guids.Game.BurstBarrier;
            rows.Add(V(g, "BurstBarrier.Ranks", "", "Ranks", "5", S));
            rows.Add(N(g, "BurstBarrier", "Kingmaker.Designers.Mechanics.Buffs.BurstBarrier", 1, S));
            rows.Add(N(Guids.Game.TowerShieldNegatePenalty, "TowerShieldNegatePenalty", "Kingmaker.Designers.Mechanics.Buffs.LiquidateTowerShieldPenalty", 1, S));
            g = Guids.Game.TowerShieldTouchShield;
            rows.Add(V(g, "TouchShield HasShieldFeatureUnlock.m_NewFact", Hsfu, "m_NewFact", Guids.Game.TowerShieldTouchShieldBuff, S));
            rows.Add(V(g, "TouchShield HasShieldFeatureUnlock.FilterByShieldProficiencyGroup", Hsfu, "FilterByShieldProficiencyGroup", "True", S));
            rows.Add(V(g, "TouchShield HasShieldFeatureUnlock.m_ShiledProficiencyGroupEntries", Hsfu, "m_ShiledProficiencyGroupEntries", "TowerShield", S));
            rows.Add(N(g, "TouchShield", Hsfu, 1, S));
            rows.Add(N(g, "TouchShield", Recalc, 1, S));
            g = Guids.Game.TowerShieldTouchShieldBuff;
            rows.Add(V(g, "TouchShieldBuff ContextRankConfig.m_BaseValueType", Crc, "m_BaseValueType", "ShieldBonus", S));
            rows.Add(V(g, "TouchShieldBuff ContextRankConfig.m_ArmorTypes has TowerShieldType", Crc, "m_ArmorTypes#count=" + Guids.Game.TowerShieldType, "1", S));
            rows.Add(V(g, "TouchShieldBuff ACTouchBonus.m_Bonus.ValueType", "Kingmaker.Designers.Mechanics.Facts.ACTouchBonus", "m_Bonus.ValueType", "Rank", S));
            rows.Add(N(g, "TouchShieldBuff", "Kingmaker.Designers.Mechanics.Facts.ACTouchBonus", 1, S));
            rows.Add(N(g, "TouchShieldBuff", Crc, 1, S));
            rows.Add(N(g, "TouchShieldBuff", Recalc, 1, S));
            rows.Add(N(Guids.Game.TowerShieldTotalCover, "TowerShieldTotalCover", "Kingmaker.Designers.Mechanics.Buffs.TowerShieldSpecialistTotalCover", 1, S));
            rows.Add(V(Guids.Game.TowerShieldEvasion, "TowerShieldEvasion.Improved", Evasion, "Improved", "False", S));
            rows.Add(N(Guids.Game.TowerShieldEvasion, "TowerShieldEvasion", Evasion, 1, S));
            rows.Add(V(Guids.Game.TowerShieldImprovedEvasion, "TowerShieldImprovedEvasion.Improved", Evasion, "Improved", "True", S));
            rows.Add(N(Guids.Game.TowerShieldImprovedEvasion, "TowerShieldImprovedEvasion", Evasion, 1, S));

            // 복제 원본 1: TowerShieldTraining — 복제가 복사하는 값과 바꾸는 참조의 원래 값.
            g = Guids.Game.TowerShieldTraining;
            rows.Add(V(g, "TowerShieldTraining.Ranks", "", "Ranks", "5", S));
            rows.Add(V(g, "TowerShieldTraining HasShieldFeatureUnlock.m_NewFact", Hsfu, "m_NewFact", Guids.Game.TowerShieldTrainingBuff, S));
            rows.Add(V(g, "TowerShieldTraining HasShieldFeatureUnlock.FilterByShieldProficiencyGroup", Hsfu, "FilterByShieldProficiencyGroup", "True", S));
            rows.Add(V(g, "TowerShieldTraining HasShieldFeatureUnlock.m_ShiledProficiencyGroupEntries", Hsfu, "m_ShiledProficiencyGroupEntries", "TowerShield", S));
            rows.Add(V(g, "TowerShieldTraining ContextRankConfig.m_BaseValueType", Crc, "m_BaseValueType", "FeatureRank", S));
            rows.Add(V(g, "TowerShieldTraining ContextRankConfig.m_Feature", Crc, "m_Feature", Guids.Game.TowerShieldTraining, S));
            rows.Add(V(g, "TowerShieldTraining ContextRankConfig.m_Progression", Crc, "m_Progression", "BonusValue", S));
            rows.Add(V(g, "TowerShieldTraining ContextRankConfig.m_StepLevel", Crc, "m_StepLevel", "-1", S));
            rows.Add(V(g, "TowerShieldTraining FactsChangeTrigger.m_CheckedFacts has the buff", Fct, "m_CheckedFacts#count=" + Guids.Game.TowerShieldTrainingBuff, "1", S));
            rows.Add(V(g, "TowerShieldTraining SavesFixerRecalculate.Version", Sfr, "Version", "1", S));
            foreach (var t in new[] { Hsfu, Crc, Fct, Sfr }) rows.Add(N(g, "TowerShieldTraining", t, 1, S));

            // 복제 원본 2: TowerShieldTrainingBuff — 복사하는 MaxDex 3 · ACP 2 의 값(최대 민첩 공식 = Bonus + BonesPerRank × 랭크, UseContextInstead 거짓 — MaxDexBonusIncrease.OnEventAboutToTrigger @001B~0073).
            g = Guids.Game.TowerShieldTrainingBuff;
            rows.Add(V(g, "TowerShieldTrainingBuff.Stacking", "", "Stacking", "Rank", S));
            rows.Add(V(g, "TowerShieldTrainingBuff.Ranks", "", "Ranks", "5", S));
            var dex = new[] { Tuple.Create("1", "0", "Medium"), Tuple.Create("1", "0", "Heavy"), Tuple.Create("1", "1", "TowerShield") };
            for (int i = 0; i < dex.Length; i++)
            {
                string c = Dex + "@" + i, n = "TowerShieldTrainingBuff MaxDexBonusIncrease@" + i + ".";
                rows.Add(V(g, n + "Bonus", c, "Bonus", dex[i].Item1, S));
                rows.Add(V(g, n + "BonesPerRank", c, "BonesPerRank", dex[i].Item2, S));
                rows.Add(V(g, n + "CheckCategory", c, "CheckCategory", "True", S));
                rows.Add(V(g, n + "Category", c, "Category", dex[i].Item3, S));
                rows.Add(V(g, n + "UseContextInstead", c, "UseContextInstead", "False", S));
            }
            var acp = new[] { Tuple.Create("1", "True"), Tuple.Create("0", "False") };
            for (int i = 0; i < acp.Length; i++)
            {
                string c = Acp + "@" + i, n = "TowerShieldTrainingBuff ArmorCheckPenaltyIncrease@" + i + ".";
                rows.Add(V(g, n + "Bonus.ValueType", c, "Bonus.ValueType", "Simple", S));
                rows.Add(V(g, n + "Bonus.Value", c, "Bonus.Value", "2", S));
                rows.Add(V(g, n + "BonesPerRank", c, "BonesPerRank", acp[i].Item1, S));
                rows.Add(V(g, n + "CheckCategory", c, "CheckCategory", acp[i].Item2, S));
                rows.Add(V(g, n + "Category", c, "Category", "TowerShield", S));
            }
            rows.Add(N(g, "TowerShieldTrainingBuff", Dex, 3, S));
            rows.Add(N(g, "TowerShieldTrainingBuff", Acp, 2, S));
            rows.Add(N(g, "TowerShieldTrainingBuff", Afca, 1, S));

            // 갑옷 훈련(그대로 참조, 3·7·11·15): 랭크 상한 5, 범주별 최대 민첩 +1/랭크 6개, ACP +1/랭크, 속도 페널티 제거.
            g = Guids.Game.ArmorTraining;
            rows.Add(V(g, "ArmorTraining.Ranks", "", "Ranks", "5", S));
            var cats = new[] { "Light", "Medium", "Heavy", "LightBarding", "MediumBarding", "HeavyBarding" };
            for (int i = 0; i < cats.Length; i++)
            {
                rows.Add(V(g, "ArmorTraining MaxDexBonusIncrease@" + i + ".BonesPerRank", Dex + "@" + i, "BonesPerRank", "1", S));
                rows.Add(V(g, "ArmorTraining MaxDexBonusIncrease@" + i + ".Category", Dex + "@" + i, "Category", cats[i], S));
                // T3 검토 I-1: 최대 민첩 공식 = Bonus + BonesPerRank × 랭크 의 Bonus 항도 잠근다(jbp 0).
                rows.Add(V(g, "ArmorTraining MaxDexBonusIncrease@" + i + ".Bonus", Dex + "@" + i, "Bonus", "0", S));
            }
            rows.Add(V(g, "ArmorTraining ArmorCheckPenaltyIncrease.BonesPerRank", Acp, "BonesPerRank", "1", S));
            rows.Add(V(g, "ArmorTraining ArmorCheckPenaltyIncrease.Bonus.Value", Acp, "Bonus.Value", "0", S)); // T3 검토 I-1: ACP 공식의 상수 항(jbp 0)
            rows.Add(N(g, "ArmorTraining", Dex, 6, S));
            rows.Add(N(g, "ArmorTraining", Acp, 1, S));
            rows.Add(N(g, "ArmorTraining", "Kingmaker.Designers.Mechanics.Facts.ArmorSpeedPenaltyRemoval", 1, S));

            // 타워실드 형식(모든 TSS 「타워실드 사용 중」 판정 = 보조손 방패 숙련군 TowerShield, 접촉 방패 랭크 = 기본 방패 AC 4) · 쿼터스태프 형식(H1 판정).
            rows.Add(V(Guids.Game.TowerShieldType, "TowerShieldType.m_ProficiencyGroup", "", "m_ProficiencyGroup", "TowerShield", S));
            rows.Add(V(Guids.Game.TowerShieldType, "TowerShieldType.m_ArmorBonus", "", "m_ArmorBonus", "4", S));
            rows.Add(V(Guids.Game.QuarterstaffType, "Quarterstaff.Category", "", "Category", "Quarterstaff", S));
            rows.Add(V(Guids.Game.QuarterstaffType, "Quarterstaff.m_IsTwoHanded", "", "m_IsTwoHanded", "True", S));
            rows.Add(V(Guids.Game.QuarterstaffType, "Quarterstaff.m_AttackType", "", "m_AttackType", "Melee", S));
        }

        // 블루프린트 자체 값 행 / 컴포넌트 값 행 / 컴포넌트 형식 개수 행(Components#type=) 만들기.
        static Row V(string guid, string name, string component, string path, string expected, string source)
        {
            return new Row(guid, name, component, path, expected, ToleranceKind.MustEqual, RowOrigin.Vanilla, source);
        }

        static Row N(string guid, string bpName, string typeFullName, int count, string source)
        {
            return new Row(guid, bpName + " has " + count + " x " + typeFullName.Substring(typeFullName.LastIndexOf('.') + 1), "", ReadbackPath.ComponentsField + "#type=" + typeFullName, count.ToString(System.Globalization.CultureInfo.InvariantCulture), ToleranceKind.MustEqual, RowOrigin.Vanilla, source);
        }

         
        //   Staggered·DazeBuff = BuffStatusCondition.Condition(Staggered / Dazed), SlowBuff = AddCondition 2개(Staggered·Slowed), DispelMagicTarget = 적 대상 가능 + 주문 형식.
        //   컴포넌트 값 행이 있는 블루프린트는 본편 컴포넌트 형식마다 개수 행을 둔다(이름 없는 외부 형식 = 차이, 같은 형식 추가 = 개수 차이).
        // 외부 모드 조사(dnlib 2026-11-14): TTT-Base Conditions.PatchStaggered 가 Staggered 의 SpellDescriptorComponent.Descriptor 값을 바꾼다(설정 StaggeredDescriptors) —
        //   우리는 그 서술자 값에 기대지 않으므로 값 행이 없고(개수 행은 그대로 1), 그 쓰기는 이 표에 드러나지 않는다(보고서 Step 0-F). 그 밖의 쓰기 후보 없음.
        static void AddRebukeReuseRows(List<Row> rows)
        {
            const string S = "foreign writer survey, rebuke reuse (jbp 2026-11-14, blueprints.zip md5 5da11edf2659)";
            const string BuffStatus = "Kingmaker.Designers.Mechanics.Buffs.BuffStatusCondition", Descriptor = "Kingmaker.Blueprints.Classes.Spells.SpellDescriptorComponent",
                RemoveOnCombatEnd = "Kingmaker.UnitLogic.Buffs.Components.RemoveWhenCombatEnded", AddCond = "Kingmaker.UnitLogic.FactLogic.AddCondition";
            string g = Guids.Game.Staggered;
            rows.Add(V(g, "Staggered BuffStatusCondition.Condition", BuffStatus, "Condition", "Staggered", S));
            rows.Add(N(g, "Staggered", BuffStatus, 1, S));
            rows.Add(N(g, "Staggered", Descriptor, 1, S));
            rows.Add(N(g, "Staggered", RemoveOnCombatEnd, 1, S));
            g = Guids.Game.SlowBuff;
            rows.Add(V(g, "SlowBuff AddCondition[0].Condition", AddCond + "@0", "Condition", "Staggered", S));
            rows.Add(V(g, "SlowBuff AddCondition[1].Condition", AddCond + "@1", "Condition", "Slowed", S));
            rows.Add(N(g, "SlowBuff", "Kingmaker.UnitLogic.FactLogic.AddStatBonus", 3, S));
            rows.Add(N(g, "SlowBuff", Descriptor, 1, S));
            rows.Add(N(g, "SlowBuff", AddCond, 2, S));
            rows.Add(N(g, "SlowBuff", "Kingmaker.UnitLogic.FactLogic.ChangeImpatience", 1, S));
            rows.Add(N(g, "SlowBuff", "Kingmaker.UnitLogic.Buffs.Components.AddBuffActions", 1, S));
            g = Guids.Game.DazeBuff;
            rows.Add(V(g, "DazeBuff BuffStatusCondition.Condition", BuffStatus, "Condition", "Dazed", S));
            rows.Add(N(g, "DazeBuff", BuffStatus, 1, S));
            rows.Add(N(g, "DazeBuff", Descriptor, 1, S));
            rows.Add(N(g, "DazeBuff", "Kingmaker.UnitLogic.Mechanics.Components.AddFactContextActions", 1, S));
            rows.Add(N(g, "DazeBuff", RemoveOnCombatEnd, 1, S));
            g = Guids.Game.DispelMagicTarget;
            rows.Add(V(g, "DispelMagicTarget.CanTargetEnemies", "", "CanTargetEnemies", "True", S));
            rows.Add(V(g, "DispelMagicTarget.Type", "", "Type", "Spell", S));
            rows.Add(V(g, "DispelMagicTarget AbilityEffectRunAction.SavingThrowType", "Kingmaker.UnitLogic.Abilities.Components.AbilityEffectRunAction", "SavingThrowType", "Unknown", S)); // 컴포넌트 행 1개 = 이 블루프린트의 형식 집합 검사를 켠다
            rows.Add(N(g, "DispelMagicTarget", "Kingmaker.Blueprints.Classes.Spells.SpellComponent", 1, S));
            rows.Add(N(g, "DispelMagicTarget", "Kingmaker.UnitLogic.Abilities.Components.AbilityEffectRunAction", 1, S));
            rows.Add(N(g, "DispelMagicTarget", "Kingmaker.UnitLogic.Abilities.Components.Base.AbilitySpawnFx", 1, S));
            rows.Add(N(g, "DispelMagicTarget", "Kingmaker.Craft.CraftInfoComponent", 1, S));
        }
    }
}
