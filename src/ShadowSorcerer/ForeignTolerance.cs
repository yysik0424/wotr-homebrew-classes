using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ShadowSorcerer
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
            if (!m.Success) throw new ArgumentException("ShadowSorcerer: bad readback field path '" + path + "'", "path");
            var segs = m.Groups["f"].Value.Split('.');
            int? level = null;
            if (m.Groups["l"].Success)
            {
                int l = int.Parse(m.Groups["l"].Value);
                if (l < ShadowRules.MinLevel || l > ShadowRules.MaxLevel) throw new ArgumentException("ShadowSorcerer: readback field path level " + l + " outside 1..20 in '" + path + "'", "path");
                if (!m.Groups["g"].Success) throw new ArgumentException("ShadowSorcerer: readback field path '" + path + "' filters a level but counts nothing (add #count=<guid>)", "path");
                if (segs.Length != 1) throw new ArgumentException("ShadowSorcerer: readback field path '" + path + "' filters a level on a nested field (level filters read a LevelEntry array field directly)", "path");
                level = l;
            }
            string type = m.Groups["t"].Success ? m.Groups["t"].Value : null;
            if (type != null && (segs.Length != 1 || segs[0] != ComponentsField)) throw new ArgumentException("ShadowSorcerer: readback field path '" + path + "' counts by type on a field other than " + ComponentsField, "path");
            if (type == null && segs.Length == 1 && segs[0] == ComponentsField) throw new ArgumentException("ShadowSorcerer: readback field path '" + path + "' reads " + ComponentsField + " without #type= (only component counts are defined)", "path");
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
            if (!m.Success) throw new ArgumentException("ShadowSorcerer: bad readback component type '" + componentType + "' (expected a full type name, optionally @<index>)", "componentType");
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
            if (guid == null || !Hex32.IsMatch(guid)) throw new ArgumentException("ShadowSorcerer: readback row GUID '" + guid + "' is not 32 lowercase hex", "guid");
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("ShadowSorcerer: readback row " + guid + " has no name", "name");
            if (componentType == null) throw new ArgumentNullException("componentType", "ShadowSorcerer: readback row " + name + ": use \"\" for the blueprint itself");
            if (!Enum.IsDefined(typeof(ToleranceKind), kind)) throw new ArgumentException("ShadowSorcerer: readback row " + name + " has undefined kind " + (int)kind);
            if (!Enum.IsDefined(typeof(RowOrigin), origin)) throw new ArgumentException("ShadowSorcerer: readback row " + name + " has undefined origin " + (int)origin);
            if (string.IsNullOrEmpty(source)) throw new ArgumentException("ShadowSorcerer: readback row " + name + " has no source", "source");
            Component = ComponentSpec.Parse(componentType);
            if (kind == ToleranceKind.AllowExtraComponentOfType)
            {
                if (componentType == "" || fieldPath != "" || expected != "" || string.IsNullOrEmpty(mod) || foreignValue != null)
                    throw new ArgumentException("ShadowSorcerer: allowance row " + name + " needs a component type and a mod, and no field path / expected / foreign value");
                if (Component.Index.HasValue) throw new ArgumentException("ShadowSorcerer: allowance row " + name + " names a type, not one indexed component");
            }
            else
            {
                if (fieldPath == null || expected == null) throw new ArgumentException("ShadowSorcerer: value row " + name + " needs a field path and an expected value");
                Path = ReadbackPath.Parse(fieldPath);
                if (Path.TypeName != null && componentType != "") throw new ArgumentException("ShadowSorcerer: value row " + name + " counts components by type but reads a component (Components#type= is a blueprint-itself row)");
                if (kind == ToleranceKind.MustEqual && (mod != null || foreignValue != null)) throw new ArgumentException("ShadowSorcerer: MustEqual row " + name + " must not name a mod or a foreign value");
                if (kind == ToleranceKind.KnownForeignValue && (string.IsNullOrEmpty(mod) || foreignValue == null || foreignValue == expected))
                    throw new ArgumentException("ShadowSorcerer: KnownForeignValue row " + name + " needs a mod and a foreign value different from the expected value");
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
            if (extraComponentTypes == null) throw new ArgumentNullException("extraComponentTypes", "ShadowSorcerer: Judge needs the extra component list (empty when the row reads the blueprint itself)");
            if (!row.IsValueRow) throw new ArgumentException("ShadowSorcerer: Judge called with allowance row " + row.Name + " (allowance rows are not values)");
            var extras = extraComponentTypes.ToArray();
            if (extras.Any(string.IsNullOrEmpty)) return Tuple.Create(false, row.Name + ": a component on the blueprint has no type name (null component?) - cannot judge");
            if (actual == null) return Tuple.Create(false, row.Name + ": value at " + row.FieldPath + " could not be read - cannot judge");
            if (extras.Length > 0) return Tuple.Create(false, row.Name + ": unknown component type(s) on the blueprint: " + string.Join(", ", extras.Distinct().ToArray()) + " (no row or allowance names them)");
            if (actual == row.Expected) return Tuple.Create(true, row.Name + ": ok (" + actual + ")");
            if (row.Kind == ToleranceKind.KnownForeignValue && actual == row.ForeignValue)
                return Tuple.Create(true, row.Name + ": known foreign value '" + actual + "' from " + row.Mod);
            return Tuple.Create(false, row.Name + ": value differs at " + row.FieldPath + ": expected '" + row.Expected + "', got '" + actual + "'" + (row.Kind == ToleranceKind.KnownForeignValue ? " (known foreign value from " + row.Mod + " is '" + row.ForeignValue + "')" : ""));
        }

         
        //   SorcererClass.m_Archetypes 에 우리가 정확히 1번(등록 예외 — 유일한 OurWrite 행) · 진행표·주문서가 본편 그대로.
        //   SorcererProgression L7/L13/L19 에 SorcererFeatSelection 이 정확히 1번(제거 대상 — BodyTable.Removed 에서 파생).
        //   SorcererSpellbook 의 일일 표·주문 목록·클래스(T2 하루 −1·보너스 주문, T3 출처 판정이 읽는다).
        // 외부 모드 조사(Step 0-F, dnlib 2026-11-14): TTT-Core·ExpandedContent·BrawlerClassWrath 의 RegisterSorcererFeatSelection 은 SorcererFeatSelection 의
        //   하위 선택 목록(m_AllFeatures)에 혈통별 항목을 더한다 — 진행표 LevelEntries 는 건드리지 않으므로 이 행들은 그대로다(선택 내용은 우리가 기대지 않는 값).
        public static readonly IReadOnlyList<Row> Rows = BuildRows();

        static Row[] BuildRows()
        {
            const string Recon = "blueprint recon (jbp 2026-11-14, blueprints.zip md5 5da11edf2659)";
            var rows = new List<Row>
            {
                new Row(Guids.Game.SorcererClass, "SorcererClass.m_Archetypes has ours once", "", "m_Archetypes#count=" + Guids.Archetype, "1", ToleranceKind.MustEqual, RowOrigin.OurWrite, "Archetype.Register (plan Global Constraints, registration exception)"),
                new Row(Guids.Game.SorcererClass, "SorcererClass.m_Progression", "", "m_Progression", Guids.Game.SorcererProgression, ToleranceKind.MustEqual, RowOrigin.Vanilla, Recon),
                new Row(Guids.Game.SorcererClass, "SorcererClass.m_Spellbook", "", "m_Spellbook", Guids.Game.SorcererSpellbook, ToleranceKind.MustEqual, RowOrigin.Vanilla, Recon),
                new Row(Guids.Game.SorcererSpellbook, "SorcererSpellbook.m_SpellsPerDay", "", "m_SpellsPerDay", Guids.Game.SorcererSpellsDailyTable, ToleranceKind.MustEqual, RowOrigin.Vanilla, Recon),
                new Row(Guids.Game.SorcererSpellbook, "SorcererSpellbook.m_SpellList", "", "m_SpellList", Guids.Game.WizardSpellList, ToleranceKind.MustEqual, RowOrigin.Vanilla, Recon),
                new Row(Guids.Game.SorcererSpellbook, "SorcererSpellbook.m_CharacterClass", "", "m_CharacterClass", Guids.Game.SorcererClass, ToleranceKind.MustEqual, RowOrigin.Vanilla, Recon),
            };
            foreach (var r in BodyTable.Removed)
                rows.Add(new Row(Guids.Game.SorcererProgression, "SorcererProgression L" + r.level + " has " + r.guid + " once", "", "LevelEntries@" + r.level + "#count=" + r.guid, "1", ToleranceKind.MustEqual, RowOrigin.Vanilla, Recon));
            return rows.ToArray();
        }
    }
}