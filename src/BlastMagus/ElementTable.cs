using System;
using System.Collections.Generic;
using System.Linq;

namespace BlastMagus
{
     
    // 선택지·원소 판별·시전자 레벨 대상·피해 서술자·SR·20레벨 복합이 전부 이 행에서 나온다(C4). 원소 GUID 리터럴은 이 파일과 Guids.cs 밖에 두지 않는다
    // (시험 `elements: no hard-coded element dispatch`). Checks 의 원소 리터럴은 독립 신탁이라 예외(시험 `elements: checks literals match table`).

    // 피해 종류(설계 §4.3-1 「종류」 열). DescriptorOf 가 이 값으로 Energy/Physical 칸 중 하나만 채운다.
    public enum DamageKind { Energy, Physical }

    // 원소 출처(설계 §4.3-1 「출처」 열). Vanilla 행 = 1단계(BlastBlueprints.Build), Kee 행 = 2단계(KeeStage — StartGameLoader.LoadAllJson 뒤, KEE 가 켜져 있을 때만).
    public enum ElementSource { Vanilla, Kee }

    // 행 하나 = 원소 하나. 불변. 행 단위 규칙(칸 배타·GUID 형식)은 ElementTable.Validate 가 판정한다(생성자는 검사하지 않는다 — 시험이 잘못된 행을 만들어 Validate 에 넣는다).
    public sealed class ElementRow
    {
        public readonly Element Id;
        public readonly ElementSource Source;
        public readonly DamageKind Kind;
        public readonly string EnergyName;                 // Kind == Energy 일 때만(DamageEnergyType 이름), 아니면 null
        public readonly string PhysicalForm;               // Kind == Physical 일 때만(PhysicalDamageForm 플래그 이름, ", " 구분), 아니면 null
        public readonly bool SpellResistance;              // 단순 블라스트 능력의 SpellResistance(계약 7번이 실제 값과 대조)
        public readonly string OurFeatureGuid;             // 우리 원소 특성(새 GUID)
        public readonly string BlastFeatureGuid, BaseGuid, AbilityGuid; // 단순 블라스트 *BlastFeature / *BlastBase / 기본 잎 *BlastAbility
        public readonly string ProjectileWeaponTypeGuid;   // 잎 투사체 무기의 형식(KineticBlastEnergy / KineticBlastPhysical)
        public readonly string CompositeName;              // 20레벨 같은 원소 복합 이름(본편 특성 이름 접두와 대조, BlastBlueprints.BuildComposite)
        public readonly string CompositeFeatureGuid, CompositeBaseGuid, CompositeAbilityGuid;
        public readonly string NameKey, DescriptionKey;    // 우리 원소 특성의 문자열 키(StringsTable)

        public ElementRow(Element id, ElementSource source, DamageKind kind, string energyName, string physicalForm, bool spellResistance,
            string ourFeatureGuid, string blastFeatureGuid, string baseGuid, string abilityGuid, string projectileWeaponTypeGuid,
            string compositeName, string compositeFeatureGuid, string compositeBaseGuid, string compositeAbilityGuid, string nameKey, string descriptionKey)
        {
            Id = id; Source = source; Kind = kind; EnergyName = energyName; PhysicalForm = physicalForm; SpellResistance = spellResistance;
            OurFeatureGuid = ourFeatureGuid; BlastFeatureGuid = blastFeatureGuid; BaseGuid = baseGuid; AbilityGuid = abilityGuid; ProjectileWeaponTypeGuid = projectileWeaponTypeGuid;
            CompositeName = compositeName; CompositeFeatureGuid = compositeFeatureGuid; CompositeBaseGuid = compositeBaseGuid; CompositeAbilityGuid = compositeAbilityGuid;
            NameKey = nameKey; DescriptionKey = descriptionKey;
        }
    }

    // 피해 서술자 명세(게임 형식 없음). ElementModel.NewDamageType 이 이것으로 새 DamageTypeDescription 을 만든다. Kind 와 맞지 않는 칸은 null.
    public sealed class DescriptorSpec
    {
        public readonly DamageKind Kind;
        public readonly string EnergyName, PhysicalForm;
        public DescriptorSpec(DamageKind kind, string energyName, string physicalForm) { Kind = kind; EnergyName = energyName; PhysicalForm = physicalForm; }
    }

    public static class ElementTable
    {
        // 행 출처: 설계 §4.3-1 표 + §8 참조표(GUID), blueprints.zip 직독(정찰 11-08 §1·§2; SR = 각 *BlastAbility.jbp SpellResistance,
        // 에너지 이름 = ContextActionDealDamage.DamageType.Energy, 투사체 = AbilityDeliverProjectile.m_Weapon 의 m_Type). 시험이 jbp 모양을 계약 검사기에 넣어 대조한다.
        // T2 = 본편 에너지 3행, T4 = 본편 물리 3행(설계 §4.3-1 표: Air/Water = Bludgeoning, Earth = B·P·S, SR false, 투사체 KineticBlastPhysical — 정찰 11-08 §1·§0-4;
        // 20레벨 복합 = §4.3-3: Air→Thunderstorm, Earth→Metal, Water→Ice). T5 = KEE 5행(아래 — 2단계에서만 제공, 1단계 Build 는 Vanilla 만 읽는다).
        static readonly ElementRow[] rows =
        {
            new ElementRow(Element.Fire, ElementSource.Vanilla, DamageKind.Energy, "Fire", null, true,
                Guids.ElementFire, Guids.Game.FireBlastFeature, Guids.Game.FireBlastBase, Guids.Game.FireBlastAbility, Guids.Game.KineticBlastEnergy,
                "BlueFlame", Guids.Game.BlueFlameBlastFeature, Guids.Game.BlueFlameBlastBase, Guids.Game.BlueFlameBlastAbility, "ElementFire.Name", "ElementFire.Description"),
            new ElementRow(Element.Cold, ElementSource.Vanilla, DamageKind.Energy, "Cold", null, true,
                Guids.ElementCold, Guids.Game.ColdBlastFeature, Guids.Game.ColdBlastBase, Guids.Game.ColdBlastAbility, Guids.Game.KineticBlastEnergy,
                "Ice", Guids.Game.IceBlastFeature, Guids.Game.IceBlastBase, Guids.Game.IceBlastAbility, "ElementCold.Name", "ElementCold.Description"),
            new ElementRow(Element.Electric, ElementSource.Vanilla, DamageKind.Energy, "Electricity", null, true,
                Guids.ElementElectric, Guids.Game.ElectricBlastFeature, Guids.Game.ElectricBlastBase, Guids.Game.ElectricBlastAbility, Guids.Game.KineticBlastEnergy,
                "Thunderstorm", Guids.Game.ThunderstormBlastFeature, Guids.Game.ThunderstormBlastBase, Guids.Game.ThunderstormBlastAbility, "ElementElectric.Name", "ElementElectric.Description"),
            new ElementRow(Element.Air, ElementSource.Vanilla, DamageKind.Physical, null, "Bludgeoning", false,
                Guids.ElementAir, Guids.Game.AirBlastFeature, Guids.Game.AirBlastBase, Guids.Game.AirBlastAbility, Guids.Game.KineticBlastPhysical,
                "Thunderstorm", Guids.Game.ThunderstormBlastFeature, Guids.Game.ThunderstormBlastBase, Guids.Game.ThunderstormBlastAbility, "ElementAir.Name", "ElementAir.Description"),
            new ElementRow(Element.Earth, ElementSource.Vanilla, DamageKind.Physical, null, "Bludgeoning, Piercing, Slashing", false,
                Guids.ElementEarth, Guids.Game.EarthBlastFeature, Guids.Game.EarthBlastBase, Guids.Game.EarthBlastAbility, Guids.Game.KineticBlastPhysical,
                "Metal", Guids.Game.MetalBlastFeature, Guids.Game.MetalBlastBase, Guids.Game.MetalBlastAbility, "ElementEarth.Name", "ElementEarth.Description"),
            new ElementRow(Element.Water, ElementSource.Vanilla, DamageKind.Physical, null, "Bludgeoning", false,
                Guids.ElementWater, Guids.Game.WaterBlastFeature, Guids.Game.WaterBlastBase, Guids.Game.WaterBlastAbility, Guids.Game.KineticBlastPhysical,
                "Ice", Guids.Game.IceBlastFeature, Guids.Game.IceBlastBase, Guids.Game.IceBlastAbility, "ElementWater.Name", "ElementWater.Description"),
            // T5 = KEE 5행(설계 §4.3-1 표 + 정찰 11-08 §3: 피해 = KEE IL RunActionDealDamage(form, energy) 인자 — 텔레키네틱 form 7·중력 form 1·나무 form 7·음 energy 5·양 energy 6,
            // SR = 텔레키네틱 false(IL 0051)·음/양 true(IL 00CA/00E0)·중력/나무 미설정(기본 false — 계약 7번이 런타임 값과 대조), 투사체 = 본편 두 무기(Blast::Projectile),
            // 20레벨 복합 = §4.3-3 KEE IL: 텔레키네틱 → Force, 중력·음 → Void, 나무·양 → Verdant; 이름 접두 = blueprints.txt 의 ForceBlast*/VoidBlast*/VerdantBlast*).
            new ElementRow(Element.Telekinetic, ElementSource.Kee, DamageKind.Physical, null, "Bludgeoning, Piercing, Slashing", false,
                Guids.ElementTelekinetic, Guids.Kee.TelekineticBlastFeature, Guids.Kee.TelekineticBlastBase, Guids.Kee.TelekineticBlastAbility, Guids.Game.KineticBlastPhysical,
                "Force", Guids.Kee.ForceBlastFeature, Guids.Kee.ForceBlastBase, Guids.Kee.ForceBlastAbility, "ElementTelekinetic.Name", "ElementTelekinetic.Description"),
            new ElementRow(Element.Gravity, ElementSource.Kee, DamageKind.Physical, null, "Bludgeoning", false,
                Guids.ElementGravity, Guids.Kee.GravityBlastFeature, Guids.Kee.GravityBlastBase, Guids.Kee.GravityBlastAbility, Guids.Game.KineticBlastPhysical,
                "Void", Guids.Kee.VoidBlastFeature, Guids.Kee.VoidBlastBase, Guids.Kee.VoidBlastAbility, "ElementGravity.Name", "ElementGravity.Description"),
            new ElementRow(Element.Wood, ElementSource.Kee, DamageKind.Physical, null, "Bludgeoning, Piercing, Slashing", false,
                Guids.ElementWood, Guids.Kee.WoodBlastFeature, Guids.Kee.WoodBlastBase, Guids.Kee.WoodBlastAbility, Guids.Game.KineticBlastPhysical,
                "Verdant", Guids.Kee.VerdantBlastFeature, Guids.Kee.VerdantBlastBase, Guids.Kee.VerdantBlastAbility, "ElementWood.Name", "ElementWood.Description"),
            new ElementRow(Element.Negative, ElementSource.Kee, DamageKind.Energy, "NegativeEnergy", null, true,
                Guids.ElementNegative, Guids.Kee.NegativeBlastFeature, Guids.Kee.NegativeBlastBase, Guids.Kee.NegativeBlastAbility, Guids.Game.KineticBlastEnergy,
                "Void", Guids.Kee.VoidBlastFeature, Guids.Kee.VoidBlastBase, Guids.Kee.VoidBlastAbility, "ElementNegative.Name", "ElementNegative.Description"),
            new ElementRow(Element.Positive, ElementSource.Kee, DamageKind.Energy, "PositiveEnergy", null, true,
                Guids.ElementPositive, Guids.Kee.PositiveBlastFeature, Guids.Kee.PositiveBlastBase, Guids.Kee.PositiveBlastAbility, Guids.Game.KineticBlastEnergy,
                "Verdant", Guids.Kee.VerdantBlastFeature, Guids.Kee.VerdantBlastBase, Guids.Kee.VerdantBlastAbility, "ElementPositive.Name", "ElementPositive.Description"),
        };

        // R-PF11: 정적 검사는 순수 Validate 하나(시험이 잘못된 행을 직접 넣는다). 실패하면 형식 초기화가 실패한다(TypeInitializationException) — 빌드 전체가 시끄럽게 멈춘다.
        static ElementTable() { Validate(rows); }

        // 매번 새 배열(호출자가 표를 바꾸지 못하게). 행 객체는 불변.
        public static ElementRow[] Vanilla { get { return rows.Where(r => r.Source == ElementSource.Vanilla).ToArray(); } }
        public static ElementRow[] Kee { get { return rows.Where(r => r.Source == ElementSource.Kee).ToArray(); } }
        public static ElementRow[] All { get { return rows.ToArray(); } }

         
        // BlastBlueprints 가 AttackStatReplacement.m_WeaponTypes 에 그대로 넣고 「배열 집합 == 이 집합, 모든 행 투사체 ∈ 이 집합」을 되읽는다(C4). 매번 새 배열.
        public static string[] BlastProjectileWeaponTypes { get { return new[] { Guids.Game.KineticBlastEnergy, Guids.Game.KineticBlastPhysical }; } }

        public static ElementRow Row(Element e)
        {
            foreach (var r in rows) if (r.Id == e) return r;
            throw new ArgumentOutOfRangeException("e", e, "BlastMagus: no element row for " + e);
        }

        // 설계 §4.3-1 「피해 서술자」 열. 칸 배타가 깨진 행이면 던진다(Validate 와 같은 행 규칙).
        public static DescriptorSpec DescriptorOf(ElementRow r)
        {
            if (r == null) throw new ArgumentNullException("r");
            var bad = RowProblems(r);
            if (bad.Count > 0) throw new ArgumentException("BlastMagus: element row " + r.Id + " is malformed: " + string.Join("; ", bad), "r");
            return r.Kind == DamageKind.Energy ? new DescriptorSpec(DamageKind.Energy, r.EnergyName, null) : new DescriptorSpec(DamageKind.Physical, null, r.PhysicalForm);
        }

        // GUID 를 담는 칸(이름 = 공유 집합의 열쇠 접두). 공유·유일성 규칙이 이 목록을 돈다.
        public static readonly string[] GuidFields =
        {
            "OurFeatureGuid", "BlastFeatureGuid", "BaseGuid", "AbilityGuid", "ProjectileWeaponTypeGuid", "CompositeFeatureGuid", "CompositeBaseGuid", "CompositeAbilityGuid",
        };

        public static string GuidOf(ElementRow r, string field)
        {
            if (r == null) throw new ArgumentNullException("r");
            switch (field)
            {
                case "OurFeatureGuid": return r.OurFeatureGuid;
                case "BlastFeatureGuid": return r.BlastFeatureGuid;
                case "BaseGuid": return r.BaseGuid;
                case "AbilityGuid": return r.AbilityGuid;
                case "ProjectileWeaponTypeGuid": return r.ProjectileWeaponTypeGuid;
                case "CompositeFeatureGuid": return r.CompositeFeatureGuid;
                case "CompositeBaseGuid": return r.CompositeBaseGuid;
                case "CompositeAbilityGuid": return r.CompositeAbilityGuid;
                default: throw new ArgumentOutOfRangeException("field", field, "BlastMagus: not an element GUID field");
            }
        }

        // 설계 §13-B A5: 공유 원장 = 행 자체. 같은 칸에서 2개 이상 행이 같은 GUID 를 가진 것만 「<칸>:<guid>」 → 원소(정렬) 로 계산한다. 리터럴 허용 목록 없음.
        public static SortedDictionary<string, Element[]> SharedGroups(IEnumerable<ElementRow> rowSet)
        {
            if (rowSet == null) throw new ArgumentNullException("rowSet");
            var list = rowSet.ToList();
            if (list.Any(r => r == null)) throw new ArgumentException("BlastMagus: SharedGroups got a null row", "rowSet");
            var result = new SortedDictionary<string, Element[]>(StringComparer.Ordinal);
            foreach (var f in GuidFields)
                foreach (var g in list.GroupBy(r => GuidOf(r, f), StringComparer.Ordinal))
                {
                    if (g.Key == null) throw new ArgumentException("BlastMagus: SharedGroups: a row has a null " + f, "rowSet");
                    var ids = g.Select(r => r.Id).OrderBy(x => x).ToArray();
                    if (ids.Length >= 2) result[f + ":" + g.Key] = ids;
                }
            return result;
        }

        // 표 전체 규칙(R-PF11). 위반이 하나라도 있으면 위반 전부를 담아 ArgumentException.
        public static void Validate(IEnumerable<ElementRow> rowSet)
        {
            if (rowSet == null) throw new ArgumentNullException("rowSet");
            var list = rowSet.ToList();
            var bad = new List<string>();
            if (list.Count == 0) bad.Add("no rows");
            for (int i = 0; i < list.Count; i++)
            {
                var r = list[i];
                if (r == null) { bad.Add("row[" + i + "] is null"); continue; }
                foreach (var p in RowProblems(r)) bad.Add("row " + r.Id + ": " + p);
            }
            var ok = list.Where(r => r != null).ToList();
            // 원소 Id: enum 값마다 정확히 1행(enum ↔ rows), 정의되지 않은 값 금지.
            foreach (var r in ok)
                if (!Enum.IsDefined(typeof(Element), r.Id)) bad.Add("row Id " + (int)r.Id + " is not a defined Element");
            foreach (Element e in Enum.GetValues(typeof(Element)))
            {
                int n = ok.Count(r => r.Id == e);
                if (n != 1) bad.Add("Element." + e + " has " + n + " rows, expected exactly 1");
            }
            // 유일성(설계 §13 F7): 우리 특성·단순 블라스트 Feature/Base/Ability 는 행마다 유일. 투사체 형식·복합 3칸은 공유될 수 있다(SharedGroups 가 계산, 시험이 단계별 신탁과 대조).
            foreach (var f in new[] { "OurFeatureGuid", "BlastFeatureGuid", "BaseGuid", "AbilityGuid" })
                foreach (var g in ok.GroupBy(r => GuidOf(r, f), StringComparer.Ordinal).Where(g => g.Count() > 1))
                    bad.Add(f + " " + g.Key + " is shared by " + string.Join(",", g.Select(r => r.Id)));
            // 칸 사이 충돌: 한 GUID 는 한 칸에만(같은 블루프린트가 어떤 행의 Base 이면서 다른 행의 Feature 일 수 없다).
            var fieldOf = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var r in ok)
                foreach (var f in GuidFields)
                {
                    var g = GuidOf(r, f);
                    if (g == null) continue; // RowProblems 가 이미 보고했다
                    string prev;
                    if (fieldOf.TryGetValue(g, out prev) && prev != f) bad.Add("GUID " + g + " appears as both " + prev + " and " + f);
                    else fieldOf[g] = f;
                }
            if (bad.Count > 0) throw new ArgumentException("BlastMagus: element table invalid: " + string.Join("; ", bad), "rowSet");
        }

        // GUID 형식 = BlueprintGuid.ToString() "N"(32자 소문자 16진 — CorePlans.cs ArcanaFilter 와 같은 근거).
        const int GuidLength = 32;

        static List<string> RowProblems(ElementRow r)
        {
            var bad = new List<string>();
            if (!Enum.IsDefined(typeof(ElementSource), r.Source)) bad.Add("Source " + (int)r.Source + " undefined");
            if (r.Kind == DamageKind.Energy)
            {
                if (string.IsNullOrEmpty(r.EnergyName)) bad.Add("Energy row without EnergyName");
                if (r.PhysicalForm != null) bad.Add("Energy row with PhysicalForm '" + r.PhysicalForm + "'");
            }
            else if (r.Kind == DamageKind.Physical)
            {
                if (string.IsNullOrEmpty(r.PhysicalForm)) bad.Add("Physical row without PhysicalForm");
                if (r.EnergyName != null) bad.Add("Physical row with EnergyName '" + r.EnergyName + "'");
            }
            else bad.Add("Kind " + (int)r.Kind + " undefined");
            foreach (var f in GuidFields)
            {
                var g = GuidOf(r, f);
                if (g == null) bad.Add(f + " is null");
                else if (!IsGuidN(g)) bad.Add(f + " '" + g + "' is not a 32-char lowercase hex GUID");
            }
            if (string.IsNullOrEmpty(r.CompositeName)) bad.Add("CompositeName empty");
            if (string.IsNullOrEmpty(r.NameKey)) bad.Add("NameKey empty");
            if (string.IsNullOrEmpty(r.DescriptionKey)) bad.Add("DescriptionKey empty");
            return bad;
        }

        static bool IsGuidN(string g)
        {
            if (g.Length != GuidLength) return false; // 형식 판정 결과(결측 아님): 호출자가 위반으로 보고한다
            foreach (var c in g)
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'))) return false;
            return true;
        }
    }
}
