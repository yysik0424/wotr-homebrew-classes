using System;
using System.Collections.Generic;
using System.Linq;

namespace WeaponSpiritShaman
{
    // [순수] 복사 원본(본편 블루프린트)에 다른 모드가 우리 Build 전에 붙인 컴포넌트를 복사본에서 어떻게 다룰지(핫픽스 2026-11-14,
     
    //  - 알려진 것 = (원본 GUID, 컴포넌트 형식 전체 이름, 참조 필드, 참조 GUID) 넷이 모두 규칙과 같은 것. 형식만 같고 참조가 다르면 모르는 것이다.
    //  - 규칙에 맞은 것은 규칙의 처분(Drop)대로 뺀다. 나머지(Kept)는 호출자가 기존의 정확 계약(PrerequisiteIsPet 하나 / 기준 아이템 0개·DlcCondition)으로 본다.
    //  - 판정 불가는 통과가 아니다: 컴포넌트 null · 규칙이 있는 형식인데 참조가 비었음 · 참조가 규칙에 없음 · 같은 규칙에 두 번 맞음 → 전부 throw.
    public enum ForeignDisposition
    {
        // 복사본에 넣지 않는다. 원본(본편 블루프린트)은 건드리지 않는다(읽기만 — Isolation.VerifyAfterBuild 가 원본 값 불변을 본다).
        Drop,
    }

    // 처분(Drop)이 영병 유닛에 대해 무엇을 가정하는가(후속 2026-11-14 「가정을 시끄럽게」). 기본값 없음 — 규칙마다 생성자 인자로 적는다.
    //  RefAbsentOnSpiritPet: 「영병은 참조 특성(RefGuid)을 절대 갖지 않으므로 빼도 결과가 같다」 — 런타임에 SpiritPetFactGuard 가 영병 사실 목록으로 확인한다.
    //  NoPetFactAssumption: 처분 이유가 영병의 사실과 무관(TTW ThrownCrossref = 무기 짝 참조) — 확인할 것이 없다.
    public enum SpiritPetAssumption
    {
        NoPetFactAssumption,
        RefAbsentOnSpiritPet,
    }

    // 「영병에 없어야 할 특성」 블루프린트가 이 게임에서 풀리는가 — BuildAll(PetClass.Build) 에서 한 번 정한다.
    //  NotInstalled = 블루프린트 캐시에 없다(다른 모드 미설치) → 그 특성은 어떤 유닛의 사실도 될 수 없으므로 판정 대상 아님(명시 상태).
    public enum RefResolution
    {
        Resolved,
        NotInstalled,
    }

    public enum PetFactVerdict
    {
        Absent,         // 풀리는 블루프린트, 영병 사실에 없음 — 가정 성립
        Present,        // 풀리는 블루프린트, 영병 사실에 있음 — 가정 위반(LogError)
        NotApplicable,  // 블루프린트가 풀리지 않음(모드 미설치) — 그 특성은 존재할 수 없다
    }

    public sealed class PetFactCheck
    {
        public readonly ForeignComponentRule Rule;
        public readonly PetFactVerdict Verdict;
        public PetFactCheck(ForeignComponentRule rule, PetFactVerdict verdict) { Rule = rule; Verdict = verdict; }
    }

    public sealed class ForeignComponentRule
    {
        public readonly string OwnerGuid;       // 그 컴포넌트가 붙는 본편 블루프린트
        public readonly string OwnerName;
        public readonly string TypeFullName;    // 컴포넌트 형식 전체 이름(다른 모드 형식은 참조할 수 없어 이름으로 본다)
        public readonly string RefField;        // 그 형식에서 무엇을 가리키는지 읽을 필드
        public readonly string RefGuid;         // 그 필드가 가리키는 블루프린트 GUID
        public readonly string RefName;
        public readonly string Mod;             // 붙이는 모드·판
        public readonly string Source;          // 붙이는 코드(IL 위치)·게임 로그 증거
        public readonly ForeignDisposition Disposition;
        public readonly string Reason;          // 그 처분이 옳은 이유(추적 요약)
        public readonly SpiritPetAssumption Assumption; // Reason 이 영병 사실에 대해 가정하는 것(RefAbsentOnSpiritPet 이면 RefGuid 가 「영병에 없어야 할 특성」)

        public ForeignComponentRule(string ownerGuid, string ownerName, string typeFullName, string refField, string refGuid, string refName,
            string mod, string source, ForeignDisposition disposition, string reason, SpiritPetAssumption assumption)
        {
            if (!Enum.IsDefined(typeof(SpiritPetAssumption), assumption)) throw new ArgumentException("ForeignComponentRule: undefined assumption " + (int)assumption);
            foreach (var kv in new[] { Tuple.Create("ownerGuid", ownerGuid), Tuple.Create("ownerName", ownerName), Tuple.Create("typeFullName", typeFullName), Tuple.Create("refField", refField),
                Tuple.Create("refGuid", refGuid), Tuple.Create("refName", refName), Tuple.Create("mod", mod), Tuple.Create("source", source), Tuple.Create("reason", reason) })
                if (string.IsNullOrEmpty(kv.Item2)) throw new ArgumentException("ForeignComponentRule: " + kv.Item1 + " is empty");
            if (!IsGuid(ownerGuid) || !IsGuid(refGuid)) throw new ArgumentException("ForeignComponentRule: owner/ref GUID must be 32 lowercase hex (" + ownerGuid + ", " + refGuid + ")");
            OwnerGuid = ownerGuid; OwnerName = ownerName; TypeFullName = typeFullName; RefField = refField; RefGuid = refGuid; RefName = refName;
            Mod = mod; Source = source; Disposition = disposition; Reason = reason; Assumption = assumption;
        }

        internal static bool IsGuid(string g) { return g != null && g.Length == 32 && g.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f')); }
    }

    // 게임 쪽 어댑터(ForeignComponentViews.Of)가 만든 컴포넌트 한 개의 모습. TypeFullName == null 은 null 컴포넌트.
    // RefGuid = 규칙이 있는 형식일 때 그 규칙의 RefField 값(비었거나 Empty 면 null). 규칙이 없는 형식이면 늘 null.
    public sealed class ComponentView
    {
        public readonly string TypeFullName;
        public readonly string TypeName;
        public readonly string RefGuid;
        public ComponentView(string typeFullName, string typeName, string refGuid) { TypeFullName = typeFullName; TypeName = typeName; RefGuid = refGuid; }
        public override string ToString() { return TypeFullName == null ? "null" : TypeName + (RefGuid == null ? "" : "{" + RefGuid + "}"); }
    }

    public sealed class ForeignPlan
    {
        public string OwnerName;
        public ComponentView[] All;
        public int[] Kept;                       // 규칙에 맞지 않은 컴포넌트의 원래 위치(순서 보존) — 호출자가 정확 계약으로 본다
        public int[] DroppedAt;                  // 규칙에 맞아 뺀 컴포넌트의 원래 위치
        public ForeignComponentRule[] DroppedBy; // DroppedAt 과 같은 순서

        public string Describe() { return "[" + string.Join(", ", All.Select(v => v.ToString()).ToArray()) + "]"; }

        // 로그 한 줄: 무엇을 보았고 무엇을 했나.
        public string Report()
        {
            if (DroppedAt.Length == 0) return "WSS foreign components on " + OwnerName + ": none (components " + Describe() + ")";
            var parts = DroppedAt.Select((at, k) => "#" + at + " " + DroppedBy[k].TypeFullName + "{" + DroppedBy[k].RefField + "=" + DroppedBy[k].RefGuid + " " + DroppedBy[k].RefName + "} from "
                + DroppedBy[k].Mod + " -> " + DroppedBy[k].Disposition).ToArray();
            return "WSS foreign components on " + OwnerName + ": " + Describe() + "; " + string.Join("; ", parts) + "; kept for the exact contract: [" + string.Join(", ", Kept.Select(i => All[i].ToString()).ToArray()) + "]";
        }
    }

    public static class ForeignComponents
    {
        public const string PrerequisiteNoFeatureType = "Kingmaker.Blueprints.Classes.Prerequisites.PrerequisiteNoFeature";
        public const string ThrownCrossrefType = "ToggleableThrowingWeapons.Component.ThrownCrossrefComponent";

        // 본편 Traits/Races/Common/DragonType.jbp(blueprints.zip md5 5da11edf2659 직독 2026-11-14).
        public const string DragonTypeGuid = "455ac88e22f55804ab87c2467deff1d6";
        // WOTR_MAKING_FRIENDS 0.2.4 의 EidolonSubtypeFeature: Mods\WOTR_MAKING_FRIENDS\GUIDs\GUIDs.json:3-4 ("a529544e-54f8-416d-9d99-5de6f905d70b"),
        // 이름 조회 GetGUID.GUIDByName("EidolonSubtypeFeature") @ BlueprintPatchAnimalCompanionClass::Patch @000B.
        public const string EidolonSubtypeFeatureGuid = "a529544e54f8416d9d995de6f905d70b";
        // ToggleableThrowingWeapons 1.3.1 이 TTT 파생 GUID 로 만든 던지기 형태 복사본(Player.log 2026-11-14 00:24 7383·7486행
        // 「Created ... 94d4be4e9a39251f641c0e4a7fd5f216 - TTWThrownModeStandardDagger」·「... 552f62384f741a16c17d6ed1cef37485 - TTWThrownModeStandardStarknife」).
        public const string TtwThrownStandardDaggerGuid = "94d4be4e9a39251f641c0e4a7fd5f216";
        public const string TtwThrownStandardStarknifeGuid = "552f62384f741a16c17d6ed1cef37485";

        const string PetClassDropReason =
            "the vanilla AnimalCompanionClass gets this so that the other mod's own pets cannot take vanilla animal-companion levels; " +
            "our clone is not in ProgressionRoot.m_PetClasses (no class picker offers it) and only our spirit units (AddClassLevels{our class}) take it, " +
            "and they never have the forbidden feature (AnimalCompanionUnit m_AddFacts is empty - SpiritBlueprints.CheckSourceUnit; our pet progression is the vanilla one minus the slot feature), " +
            "so carrying it would not change who can level our class; dropping keeps the clone's contract = [PrerequisiteIsPet] and no reference to the other mod's blueprints";
        const string ThrownDropReason =
            "m_OtherForm points to ToggleableThrowingWeapons' thrown copy of the vanilla item (not of our spirit weapon); carried onto our start weapon it would pair " +
            "the spirit weapon with that vanilla copy, so toggling would swap our item for a plain vanilla thrown weapon; dropping leaves the spirit weapon without the toggle";

        // 원본 기준 아이템 GUID = SpiritCategories.cs 19·68행 ItemGuid 와 같은 값(Rows 는 SpiritBlueprints.Build 만 읽는다 — 시험 `wiring: one loop builds all`).
        // 복제이므로 시험 `exec: foreign components: every rule owner is ... one of our base items, with the table's name` 가 어긋나면 실패한다(C4).
        public const string StandardDaggerGuid = "aa514dbf4c3d61f4e9c0738bd4d373cb";
        public const string StandardStarknifeGuid = "d19662d357d752447a801951b7bec798";

        public static readonly ForeignComponentRule[] Rules =
        {
            new ForeignComponentRule(Guids.Game.AnimalCompanionClass, "AnimalCompanionClass", PrerequisiteNoFeatureType, "m_Feature", DragonTypeGuid, "DragonType",
                "ExpandedContent 0.13.69",
                "ExpandedContent.Tweaks.Classes.DrakeClass.DrakeCompanionClass::AddDrakeCompanionClass @1153-1169 AddComponent<PrerequisiteNoFeature>(lambda b__91: HideInUI=true, m_Feature=DragonType) - appended",
                ForeignDisposition.Drop, PetClassDropReason, SpiritPetAssumption.RefAbsentOnSpiritPet),
            new ForeignComponentRule(Guids.Game.AnimalCompanionClass, "AnimalCompanionClass", PrerequisiteNoFeatureType, "m_Feature", EidolonSubtypeFeatureGuid, "EidolonSubtypeFeature",
                "WOTR_MAKING_FRIENDS 0.2.4",
                "WOTR_MAKING_FRIENDS.BlueprintPatches.BlueprintPatchAnimalCompanionClass::Patch @0001-0044 CharacterClassConfigurator.For(AnimalCompanionClass).AddPrerequisiteNoFeature(EidolonSubtypeFeature) - BlueprintCore prepends (CommonTool.Append(new, existing)); Player.log 9517 'Patching AnimalCompanionClass'",
                ForeignDisposition.Drop, PetClassDropReason, SpiritPetAssumption.RefAbsentOnSpiritPet),
            new ForeignComponentRule(StandardDaggerGuid, "StandardDagger", ThrownCrossrefType, "m_OtherForm", TtwThrownStandardDaggerGuid, "TTWThrownModeStandardDagger",
                "ToggleableThrowingWeapons 1.3.1",
                "ToggleableThrowingWeapons.Content.PairCreation::AttachComponentsToPair @0045-0067 (lambda b__2: m_OtherForm = thrown); Player.log 7382-7387 'Patched altMode onto:: aa514dbf... - StandardDagger'",
                ForeignDisposition.Drop, ThrownDropReason, SpiritPetAssumption.NoPetFactAssumption),
            new ForeignComponentRule(StandardStarknifeGuid, "StandardStarknife", ThrownCrossrefType, "m_OtherForm", TtwThrownStandardStarknifeGuid, "TTWThrownModeStandardStarknife",
                "ToggleableThrowingWeapons 1.3.1",
                "ToggleableThrowingWeapons.Content.PairCreation::AttachComponentsToPair @0045-0067 (lambda b__2: m_OtherForm = thrown); Player.log 7485-7490 'Patched altMode onto:: d19662d3... - StandardStarknife'",
                ForeignDisposition.Drop, ThrownDropReason, SpiritPetAssumption.NoPetFactAssumption),
        };

        // ---- 「영병은 그 특성을 갖지 않는다」 가정의 런타임 확인(후속 2026-11-14). 게임 어댑터 = SpiritPetFactGuard. ----

        // BuildAll 때 한 번: RefAbsentOnSpiritPet 규칙의 참조 GUID 마다 풀리는가. resolves 가 그 GUID 를 판정한다(게임 = BlueprintsCache.Load != null).
        public static Dictionary<string, RefResolution> ResolveAbsenceRefs(IList<ForeignComponentRule> rules, Func<string, bool> resolves)
        {
            if (rules == null) throw new ArgumentNullException("rules");
            if (resolves == null) throw new ArgumentNullException("resolves");
            var r = new Dictionary<string, RefResolution>();
            foreach (var rule in rules)
            {
                if (rule == null) throw new ArgumentException("ForeignComponents.ResolveAbsenceRefs: null rule");
                if (rule.Assumption != SpiritPetAssumption.RefAbsentOnSpiritPet || r.ContainsKey(rule.RefGuid)) continue;
                r[rule.RefGuid] = resolves(rule.RefGuid) ? RefResolution.Resolved : RefResolution.NotInstalled;
            }
            return r;
        }

        // [순수 판정] 영병 한 마리의 사실 GUID 집합 → RefAbsentOnSpiritPet 규칙마다 판정. 위반 = Verdict Present.
        //  판정 불가는 통과가 아니다: null 인자 · 사실 GUID 가 null/형식 아님(블루프린트 없는 사실) · 해결표에 없는 참조 · 「풀리지 않는데 사실에 있음」(모순) → throw.
        public static PetFactCheck[] JudgeSpiritPetFacts(IList<ForeignComponentRule> rules, ICollection<string> factGuids, IDictionary<string, RefResolution> resolution)
        {
            if (rules == null) throw new ArgumentNullException("rules");
            if (factGuids == null) throw new ArgumentNullException("factGuids");
            if (resolution == null) throw new ArgumentNullException("resolution");
            foreach (var g in factGuids)
                if (!ForeignComponentRule.IsGuid(g)) throw new InvalidOperationException("ForeignComponents.JudgeSpiritPetFacts: spirit pet fact GUID '" + (g ?? "null") + "' is not 32 lowercase hex (fact without a blueprint?) - cannot judge");
            var set = new HashSet<string>(factGuids);
            var r = new List<PetFactCheck>();
            foreach (var rule in rules)
            {
                if (rule == null) throw new ArgumentException("ForeignComponents.JudgeSpiritPetFacts: null rule");
                if (rule.Assumption != SpiritPetAssumption.RefAbsentOnSpiritPet) continue;
                RefResolution res;
                if (!resolution.TryGetValue(rule.RefGuid, out res))
                    throw new InvalidOperationException("ForeignComponents.JudgeSpiritPetFacts: " + rule.RefName + " " + rule.RefGuid + " (" + rule.Mod + " rule) has no build-time resolution - cannot judge");
                bool has = set.Contains(rule.RefGuid);
                switch (res)
                {
                    case RefResolution.Resolved: r.Add(new PetFactCheck(rule, has ? PetFactVerdict.Present : PetFactVerdict.Absent)); break;
                    case RefResolution.NotInstalled:
                        if (has) throw new InvalidOperationException("ForeignComponents.JudgeSpiritPetFacts: spirit pet has fact " + rule.RefName + " " + rule.RefGuid + " but the build found that blueprint unresolved (" + rule.Mod + " rule) - contradiction");
                        r.Add(new PetFactCheck(rule, PetFactVerdict.NotApplicable));
                        break;
                    default: throw new ArgumentException("ForeignComponents.JudgeSpiritPetFacts: undefined resolution " + (int)res);
                }
            }
            return r.ToArray();
        }

        public static ForeignComponentRule[] Violations(PetFactCheck[] checks)
        {
            if (checks == null) throw new ArgumentNullException("checks");
            return checks.Where(c => c.Verdict == PetFactVerdict.Present).Select(c => c.Rule).ToArray();
        }

        // LogError 본문. 「WeaponSpiritShaman:」 접두사 = 점검 S20 no-mod-errors 가 세는 표지(LogCapture.IsOurs = ModContext.Id 포함).
        public static string ViolationMessage(string unitName, string unitId, ForeignComponentRule rule)
        {
            if (rule == null) throw new ArgumentNullException("rule");
            return "WeaponSpiritShaman: spirit pet " + unitName + " (" + unitId + ") has " + rule.RefName + " " + rule.RefGuid + " - the dropped " + rule.TypeFullName + " rule from " + rule.Mod
                + " on " + rule.OwnerName + " assumed spirit pets never have it, so our pet class no longer blocks what that mod meant to block (rule source: " + rule.Source + ")";
        }

        public static string ResolutionReport(IDictionary<string, RefResolution> resolution)
        {
            if (resolution == null) throw new ArgumentNullException("resolution");
            return "WSS spirit-pet must-be-absent features: " + string.Join("; ", Rules.Where(x => x.Assumption == SpiritPetAssumption.RefAbsentOnSpiritPet).Select(x =>
            {
                RefResolution res;
                if (!resolution.TryGetValue(x.RefGuid, out res)) throw new InvalidOperationException("ForeignComponents.ResolutionReport: no resolution for " + x.RefName + " " + x.RefGuid);
                return x.RefName + " " + x.RefGuid + " (" + x.Mod + " rule) " + res;
            }).ToArray());
        }

        // 이 원본의 이 형식에 규칙이 있으면 읽을 참조 필드 이름, 없으면 null. 같은 (원본, 형식)의 규칙들이 다른 필드를 말하면 throw(규칙 표 오류).
        public static string RefFieldFor(string ownerGuid, string typeFullName)
        {
            if (string.IsNullOrEmpty(ownerGuid)) throw new ArgumentException("ForeignComponents.RefFieldFor: empty owner GUID");
            if (typeFullName == null) return null;
            var fields = Rules.Where(r => r.OwnerGuid == ownerGuid && r.TypeFullName == typeFullName).Select(r => r.RefField).Distinct().ToArray();
            if (fields.Length > 1) throw new InvalidOperationException("ForeignComponents: rules for " + typeFullName + " on " + ownerGuid + " disagree on the reference field (" + string.Join(",", fields) + ")");
            return fields.Length == 1 ? fields[0] : null;
        }

        // 펫 직업 원본 계약: 알려진 다른 모드 것을 뺀 나머지가 정확히 PrerequisiteIsPet 하나(형식 전체 이름) → 그 위치. 아니면 throw(0개·2개 이상·모르는 형식).
        public const string PrerequisiteIsPetType = "Kingmaker.Blueprints.Classes.Prerequisites.PrerequisiteIsPet";
        public static int PetClassIsPetIndex(ForeignPlan p)
        {
            if (p == null) throw new ArgumentNullException("p");
            if (p.Kept.Length != 1 || p.All[p.Kept[0]].TypeFullName != PrerequisiteIsPetType)
                throw new InvalidOperationException("WeaponSpiritShaman: " + p.OwnerName + " components are not exactly [PrerequisiteIsPet] after removing known foreign components (got " + p.Describe()
                    + ", left [" + string.Join(", ", p.Kept.Select(i => p.All[i].TypeFullName).ToArray()) + "])");
            return p.Kept[0];
        }

        // 기준 아이템 계약: 알려진 다른 모드 것을 뺀 나머지의 형식 이름 == expected(순서 포함). 아니면 throw.
        public static void RequireKeptNames(ForeignPlan p, string[] expected)
        {
            if (p == null) throw new ArgumentNullException("p");
            if (expected == null) throw new ArgumentNullException("expected");
            var comps = p.Kept.Select(i => p.All[i].TypeName).ToArray();
            if (!comps.SequenceEqual(expected))
                throw new InvalidOperationException("WeaponSpiritShaman: base item " + p.OwnerName + " components [" + string.Join(",", comps) + "] != expected [" + string.Join(",", expected) + "] (all " + p.Describe() + ")");
        }

        public static ForeignPlan Strip(string ownerName, string ownerGuid, IList<ComponentView> comps)
        {
            if (string.IsNullOrEmpty(ownerName)) throw new ArgumentException("ForeignComponents.Strip: empty owner name");
            if (!ForeignComponentRule.IsGuid(ownerGuid)) throw new ArgumentException("ForeignComponents.Strip: owner GUID '" + ownerGuid + "' is not 32 lowercase hex");
            if (comps == null) throw new ArgumentNullException("comps", "ForeignComponents.Strip: " + ownerName + " components are null");
            var all = comps.ToArray();
            string listing = "[" + string.Join(", ", all.Select(v => v == null ? "null" : v.ToString()).ToArray()) + "]";
            var kept = new List<int>();
            var droppedAt = new List<int>();
            var droppedBy = new List<ForeignComponentRule>();
            for (int i = 0; i < all.Length; i++)
            {
                var v = all[i];
                if (v == null || v.TypeFullName == null) throw new InvalidOperationException("WeaponSpiritShaman: " + ownerName + " has a null component at #" + i + " (got " + listing + ")");
                var forType = Rules.Where(r => r.OwnerGuid == ownerGuid && r.TypeFullName == v.TypeFullName).ToArray();
                if (forType.Length == 0) { kept.Add(i); continue; }
                if (v.RefGuid == null)
                    throw new InvalidOperationException("WeaponSpiritShaman: " + ownerName + " component #" + i + " " + v.TypeFullName + " has an empty " + forType[0].RefField
                        + " (known foreign type, reference cannot be judged) (got " + listing + ")");
                var rule = forType.Where(r => r.RefGuid == v.RefGuid).ToArray();
                if (rule.Length != 1)
                    throw new InvalidOperationException("WeaponSpiritShaman: " + ownerName + " component #" + i + " " + v.TypeFullName + "{" + forType[0].RefField + "=" + v.RefGuid
                        + "} is a known foreign type with an unknown reference (known: " + string.Join(", ", forType.Select(r => r.RefGuid + " " + r.RefName + " from " + r.Mod).ToArray()) + ") (got " + listing + ")");
                if (droppedBy.Contains(rule[0]))
                    throw new InvalidOperationException("WeaponSpiritShaman: " + ownerName + " has " + v.TypeFullName + "{" + v.RefGuid + "} more than once (each known mod adds it once; a second copy is not understood) (got " + listing + ")");
                droppedAt.Add(i);
                droppedBy.Add(rule[0]);
            }
            return new ForeignPlan { OwnerName = ownerName, All = all, Kept = kept.ToArray(), DroppedAt = droppedAt.ToArray(), DroppedBy = droppedBy.ToArray() };
        }
    }
}
