using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace Golemancer
{
    // 추가 슬램 사지 버프(소유자 결정 2026-11-16 — 평소 2회(11레벨부터)와 골렘 폭주가 같은 버프 묶음을 쓴다). 판정 원천 = 순수 SlamRules(횟수 표 Dials.SlamCounts → 추가 수 → 버프 id).
    //   버프 27(id = 재질 버프 키 # 추가 수) = HiddenInUi + StayOnDeath, 컴포넌트 = 본편 AddAdditionalLimb × 추가 수(무기 = 그 키의 슬램).
     
    //   (RuleCalculateAttacksCountLimbs @0070~0079), 주 손이 자연 무기면 RuleCalculateWeaponStats.IsSecondaryWeapon = false(명중 −5·힘 절반 없음), 슬램 분류 OtherNaturalWeapons 는 사지 수 한도 없음.
    //   변신(Polymorph, m_KeepSlots = false) 중에는 무기의 KeepInPolymorph 가 true 여야 변신 사지 목록에 들어간다(UnitBody.AddLimbToIndex @0058~0066) → 슬램 13 에 KeepInPolymorph = true(Materials.BuildSlam).
    // 적용 = Reconcile(유닛, 기대 id): 골렘 = 생명주기 Apply 의 재질 단계(Materials.Reconcile — 레벨업·재질 교체·휴식/수리 뒤 재적용이 전부 이 경로) + 폭주 시작·종료,
    //   합체한 본체 = Fusion.ReconcileMaster + 폭주 버프 컴포넌트(형태 버프가 붙고 떨어질 때).
    public static class SlamLimbs
    {
        static readonly Dictionary<string, BlueprintBuff> buffs = new Dictionary<string, BlueprintBuff>(); // 버프 id → 버프
        static bool built;

        static void RequireBuilt() { if (!built) throw new InvalidOperationException("Golemancer: SlamLimbs.Build() has not run"); }

        public static BlueprintBuff BuffOf(string limbId) { RequireBuilt(); BlueprintBuff b; if (!buffs.TryGetValue(limbId, out b)) throw new KeyNotFoundException("Golemancer: no slam limb buff for " + limbId); return b; }
        public static IEnumerable<BlueprintBuff> AllBuffs { get { RequireBuilt(); return buffs.Values; } }

        public static void Build()
        {
            if (built) throw new InvalidOperationException("Golemancer: SlamLimbs.Build() called twice");
            if (Materials.Selections == null) throw new InvalidOperationException("Golemancer: SlamLimbs.Build() before Materials.Build()");
            var flags = Enum.Parse(FindField(typeof(BlueprintBuff), "m_Flags").FieldType, "HiddenInUi, StayOnDeath");
            foreach (var id in SlamRules.LimbIds)
            {
                var kv = SlamRules.ParseLimbId(id);
                string key = kv.Key; int extra = kv.Value;
                var slam = Materials.SlamOf(key);
                // 변신 중 사지 유지 전제(C2): 슬램이 KeepInPolymorph 가 아니면 사지가 조용히 빠진다 → 빌드에서 던진다.
                if (!slam.KeepInPolymorph) throw new InvalidOperationException("Golemancer: slam " + slam.name + " is not KeepInPolymorph — additional slam limbs would be dropped while polymorphed");
                if (!slam.IsNatural || slam.IsUnarmed || slam.AlwaysPrimary) throw new InvalidOperationException("Golemancer: slam " + slam.name + " is not a plain natural weapon (natural " + slam.IsNatural + ", unarmed " + slam.IsUnarmed + ", always primary " + slam.AlwaysPrimary + ")");
                var b = Create<BlueprintBuff>("GolemancerSlamLimbs_" + key.Replace("@", "_") + "_x" + extra, SlamRules.LimbGuid(id), x =>
                {
                    SetField(x, "m_DisplayName", Strings.L("SlamLimbs.Name"));
                    SetField(x, "m_Description", Strings.L("SlamLimbs.Description"));
                    SetField(x, "m_DescriptionShort", NoText()); // 2026-11-16: 짧은 설명 없음 = 본편 jbp 의 빈 키(null 이면 "<null>" 이 찍힌다)
                    SetField(x, "m_Flags", flags); // 숨김(골렘 버프 줄을 채우지 않는다) + 사망 때 유지(교정은 Reconcile 만 한다 — 재질 버프와 같은 방침)
                    x.Stacking = StackingType.Replace;
                    x.FxOnStart = new PrefabLink();  // null 금지: 엔진 Buff.OnRemove 가 null 링크에서 예외(빈 링크 = FX 없음)
                    x.FxOnRemove = new PrefabLink();
                });
                var comps = new BlueprintComponent[extra];
                for (int i = 0; i < extra; i++)
                {
                    var limb = new AddAdditionalLimb();
                    SetField(limb, "m_Weapon", Ref<BlueprintItemWeaponReference>(MaterialCatalog.SlamGuid(key)));
                    comps[i] = limb;
                }
                AddComponents(b, comps);
                var back = b.ComponentsArray;
                if (back.Length != extra || back.Any(c => !(c is AddAdditionalLimb) || !ReferenceEquals(((AddAdditionalLimb)c).Weapon, slam))
                    || !b.IsHiddenInUI || !b.StayOnDeath || b.FxOnStart == null || b.FxOnRemove == null || !string.IsNullOrEmpty(b.FxOnStart.AssetId) || !string.IsNullOrEmpty(b.FxOnRemove.AssetId))
                    throw new InvalidOperationException("Golemancer: slam limb buff " + b.name + " readback failed (components " + back.Length + ", expected " + extra + " AddAdditionalLimb of " + slam.name + ")");
                buffs.Add(id, b);
            }
            if (buffs.Count != SlamRules.LimbIds.Count) throw new InvalidOperationException("Golemancer: SlamLimbs.Build made " + buffs.Count + " buffs for " + SlamRules.LimbIds.Count + " ids");
            built = true;
        }

        // ---------------- 관측 ----------------
        public static List<Buff> OursOn(UnitEntityData unit)
        {
            if (unit == null) throw new ArgumentNullException("unit");
            RequireBuilt();
            return unit.Buffs.Enumerable.Where(b => buffs.ContainsValue(b.Blueprint)).ToList();
        }

        public static string IdOf(Buff b)
        {
            if (b == null) throw new ArgumentNullException("b");
            RequireBuilt();
            var hit = buffs.Where(kv => ReferenceEquals(kv.Value, b.Blueprint)).Select(kv => kv.Key).ToArray();
            if (hit.Length != 1) throw new InvalidOperationException("Golemancer: buff " + b.Blueprint.name + " is not one of our slam limb buffs");
            return hit[0];
        }

        // 그 유닛 몸의 추가 사지 중 이 슬램을 든 칸 수(게임이 공격 수를 세는 목록 — 변신 중이면 변신 사지 목록, UnitBody.get_AdditionalLimbs).
        public static int LimbSlams(UnitEntityData unit, string buffKey)
        {
            if (unit == null) throw new ArgumentNullException("unit");
            var slam = Materials.SlamOf(buffKey);
            return unit.Body.AdditionalLimbs.Count(s => s != null && s.HasWeapon && ReferenceEquals(s.MaybeWeapon.Blueprint, slam));
        }

        // ---------------- 교정 ----------------
        // 유닛의 추가 사지 버프를 기대 id(null = 없음)로 맞춘다. 되읽기: 버프 집합 == 기대, 켜진 버프면 몸의 추가 사지 수 == 추가 수, 시전자 == 그 유닛.
        // 죽은 유닛에는 버프를 더할 수 없다(엔진 BuffCollection.PrepareFactForAttach @0011-0039) → 지우기만 하고 false 를 돌려준다(더하기는 되살아난 뒤 생명주기 Apply 가 한다 — 호출자가 기록).
        public static bool Reconcile(UnitEntityData unit, string wantedId)
        {
            if (unit == null) throw new ArgumentNullException("unit");
            RequireBuilt();
            var ours = OursOn(unit);
            var plan = SlamRules.PlanLimbs(ours.Select(IdOf).ToArray(), wantedId);
            foreach (var b in ours) if (plan.Remove.Contains(IdOf(b))) unit.Buffs.RemoveFact(b);
            if (plan.Add != null)
            {
                if (unit.State.IsDead)
                {
                    if (OursOn(unit).Count != 0) throw new InvalidOperationException("Golemancer: slam limb buffs still on dead " + unit.CharacterName + " after removal");
                    return false;
                }
                var bp = BuffOf(plan.Add);
                var buff = unit.Descriptor.AddBuff(bp, unit, null);
                if (buff == null || buff.Context == null || !ReferenceEquals(buff.Context.MaybeCaster, unit)) throw new InvalidOperationException("Golemancer: AddBuff " + bp.name + " on " + unit.CharacterName + " readback failed (null or caster is not the unit)");
            }
            Readback(unit, wantedId);
            return true;
        }

        public static void Readback(UnitEntityData unit, string wantedId)
        {
            var after = OursOn(unit);
            var ids = after.Select(IdOf).ToArray();
            if (!SlamRules.LimbsSatisfied(ids, wantedId)) throw new InvalidOperationException("Golemancer: slam limb readback on " + unit.CharacterName + ": buffs [" + string.Join(",", ids) + "], expected " + (wantedId ?? "none"));
            if (wantedId == null) return;
            var kv = SlamRules.ParseLimbId(wantedId);
            // 켜진 버프만 몸을 본다(거둔 골렘 = IsInGame false 는 버프가 꺼져 있고 사지도 빠져 있다 — 다시 나오면 엔진이 켠다, UnitEntityData.OnIsInGameChanged).
            if (after[0].IsTurnedOn)
            {
                int n = LimbSlams(unit, kv.Key);
                if (n != kv.Value) throw new InvalidOperationException("Golemancer: slam limb readback on " + unit.CharacterName + ": " + n + " additional " + kv.Key + " slam limb(s), expected " + kv.Value + " (buff " + wantedId + " is on)");
            }
        }

        // 골렘의 기대 id: 키 = 골렘이 지금 단 재질 버프의 키(정확히 1 — 아니면 던진다), 추가 수 = 표(주인의 연금술사 레벨, 폭주 여부).
        public static string WantedForGolem(UnitEntityData golem, int masterLevel, bool overdrive)
        {
            if (golem == null) throw new ArgumentNullException("golem");
            var keys = Materials.OurBuffs(golem).Select(Materials.KeyOfBuff).ToArray();
            if (keys.Length != 1) throw new InvalidOperationException("Golemancer: " + golem.CharacterName + " has " + keys.Length + " material buff(s) [" + string.Join(",", keys) + "] — cannot tell which slam its limbs use");
            int extra = SlamRules.GolemExtraLimbs(masterLevel, overdrive);
            return extra == 0 ? null : SlamRules.LimbId(keys[0], extra);
        }

        // 합체한 본체의 기대 id: 형태 버프가 없으면 없음, 정확히 1이면 그 키 × 표(max(합체 손 슬램, 표 값) − 손 슬램), 2 이상이면 던진다.
        public static string WantedForMaster(UnitEntityData master, int masterLevel, bool overdrive)
        {
            if (master == null) throw new ArgumentNullException("master");
            var forms = Fusion.FormsOn(master);
            if (forms.Count == 0) return null;
            if (forms.Count != 1) throw new InvalidOperationException("Golemancer: " + master.CharacterName + " has " + forms.Count + " fusion forms — cannot tell which slam its limbs use");
            int extra = SlamRules.MasterExtraLimbs(masterLevel, overdrive);
            return extra == 0 ? null : SlamRules.LimbId(Fusion.KeyOfForm(forms[0].Blueprint), extra);
        }

        public static int AlchemistLevel(UnitEntityData master)
        {
            if (master == null) throw new ArgumentNullException("master");
            return master.Progression.GetClassLevel(Get<BlueprintCharacterClass>(Guids.Game.AlchemistClass));
        }
    }
}
