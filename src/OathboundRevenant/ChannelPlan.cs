using System;
using System.Collections.Generic;
using System.Linq;
using WotrClassMod.Common;

namespace OathboundRevenant
{
     
    // 트리(#2·#14): 바깥 AbilityEffectRunAction{Unknown} → Conditional(살아 있는 적) → IfTrue [SavingThrow{Will} → [DealDamage{NegativeEnergy, IsAoE, HalfIfSaved}], ChannelSelfHeal], IfFalse [].
    public sealed class TreeNode
    {
        public readonly string Kind;           // "Run"(바깥) · "Conditional" · "Save" · "Damage" · "Heal"
        public readonly string Detail;         // Run/Save = 내성 형식, Damage = 에너지, Conditional = 조건 목록("IsEnemy,!UndeadType,!ConstructType,!NEA")
        public readonly bool IsAoE, HalfIfSaved;
        public readonly List<TreeNode> Children = new List<TreeNode>();     // Run/Save 의 행동 · Conditional 의 IfTrue
        public readonly List<TreeNode> Otherwise = new List<TreeNode>();    // Conditional 의 IfFalse
        public TreeNode(string kind, string detail, bool aoe = false, bool half = false) { Kind = kind; Detail = detail; IsAoE = aoe; HalfIfSaved = half; }
        public TreeNode Add(params TreeNode[] n) { Children.AddRange(n); return this; }
    }

    public static class ChannelPlan
    {
        // 출처: 설계 §6 「살아 있는 적」 + 계획 #3 — 순서 적 → 비생물(언데드·구성체) → 음에너지 친화(P13). 빌드 되읽기(Channel.VerifyTree)가 생산 조건 배열을 이 문자열과 대조한다.
        public static readonly string LivingCondition = "IsEnemy,!UndeadType,!ConstructType,!NegativeEnergyAffinity";
        public static readonly string ResourceGuid = Guids.Game.LayOnHandsResource;
        public static readonly string Range = "Personal";
        public static readonly string ActionType = "Standard";
        public static readonly string AbilityKind = "Supernatural";
        public static readonly string TargetType = "Any";

        // 생산 명세 트리.
        public static TreeNode SpecTree()
        {
            var cond = new TreeNode("Conditional", LivingCondition);
            cond.Add(new TreeNode("Save", "Will").Add(new TreeNode("Damage", StrikePlan.Negative, true, true)), new TreeNode("Heal", null));
            return new TreeNode("Run", "Unknown").Add(cond);
        }

        // 트리 검사(빌드 되읽기와 시험이 같은 함수). 오류 문자열 목록(빈 목록 = 통과).
        public static IReadOnlyList<string> VerifyTree(TreeNode run)
        {
            var e = new List<string>();
            if (run == null || run.Kind != "Run") { e.Add("root is not the outer run action"); return e; }
            if (run.Detail != "Unknown") e.Add("outer save " + run.Detail + " (must be Unknown — one Will save per target, #2)");
            var all = Flatten(run).ToList();
            int saves = all.Count(n => n.Kind == "Save");
            if (saves != 1) e.Add(saves == 0 ? "no Will save" : "double save (" + saves + " save actions)");
            if (all.Any(n => n.Kind == "Save" && n.Detail != "Will")) e.Add("inner save is not Will");
            var conds = run.Children.Where(n => n.Kind == "Conditional").ToList();
            if (conds.Count != 1) { e.Add("outer run must hold exactly one living-enemy Conditional (got " + conds.Count + ")"); return e; }
            if (run.Children.Count != conds.Count) e.Add("outer run holds actions outside the living-enemy Conditional");
            var c = conds[0];
            if (c.Detail != LivingCondition) e.Add("living condition incomplete (" + c.Detail + ", expected " + LivingCondition + ")");
            if (c.Otherwise.Count != 0) e.Add("living Conditional IfFalse must be empty");
            var heals = all.Where(n => n.Kind == "Heal").ToList();
            if (heals.Count != 1 || !c.Children.Contains(heals.Count == 1 ? heals[0] : null)) e.Add("heal outside living branch (or not exactly one heal)");
            var dmg = all.Where(n => n.Kind == "Damage").ToList();
            if (dmg.Count != 1) e.Add(dmg.Count + " damage actions (expected 1)");
            else
            {
                if (!dmg[0].IsAoE) e.Add("per-target roll (IsAoE false — #14)");
                if (!dmg[0].HalfIfSaved) e.Add("damage not halved on a save");
                if (dmg[0].Detail != StrikePlan.Negative) e.Add("damage energy " + dmg[0].Detail);
                var save = c.Children.FirstOrDefault(n => n.Kind == "Save");
                if (save == null || !save.Children.Contains(dmg[0])) e.Add("damage is not inside the Will save");
            }
            int iSave = c.Children.FindIndex(n => n.Kind == "Save"), iHeal = c.Children.FindIndex(n => n.Kind == "Heal");
            if (iSave >= 0 && iHeal >= 0 && iHeal < iSave) e.Add("heal runs before the save/damage (it must read that cast's AoE roll)");
            return e;
        }

        static IEnumerable<TreeNode> Flatten(TreeNode n)
        {
            yield return n;
            foreach (var c in n.Children.Concat(n.Otherwise)) foreach (var x in Flatten(c)) yield return x;
        }
    }

    public enum ChannelHealVerdict { Heal, Skip, StopSilently, MissingRoll }

    // 회복 표지 키(대상 종류별로 따로 — §8-d 말 확장, 시전자 회복과 말 회복이 서로를 막지 않게).
    public static class HealKeys
    {
        public static readonly string Caster = "caster";
        public static readonly string Mount = "mount";
    }

    public static class ChannelHealRule
    {
        // #13: 상류 실패(같은 시전 문맥의 주사위 부품 실패) → 추가 오류 없이 멈춤 · 이미 회복 → Skip · 굴림 없음 → MissingRoll(Report) · 그 밖 Heal.
        public static ChannelHealVerdict Decide(string upstreamFailure, bool alreadyHealed, bool aoeRollPresent)
        {
            if (upstreamFailure != null) return ChannelHealVerdict.StopSilently;
            if (alreadyHealed) return ChannelHealVerdict.Skip;
            if (!aoeRollPresent) return ChannelHealVerdict.MissingRoll;
            return ChannelHealVerdict.Heal;
        }

        // §8-d 말 확장: 대상 = 시전자 | 말. 말은 자격(언데드 말 ∧ 살아 있음(파괴 아님) ∧ 채널 영역 안 — P20)이 없으면 Skip, 표지는 대상별로 따로(HealOnceLedger 키).
        public static ChannelHealVerdict DecideFor(bool isMount, string upstreamFailure, bool alreadyHealed, bool aoeRollPresent, bool mountEligible)
        {
            if (upstreamFailure != null) return ChannelHealVerdict.StopSilently;
            if (isMount && !mountEligible) return ChannelHealVerdict.Skip;
            return Decide(null, alreadyHealed, aoeRollPresent);
        }
    }

    // [순수] 시전 문맥별 1회 회복 표지(약한 키 — 문맥이 수집되면 사라짐). 대상 종류(시전자 / 말)별로 따로 센다.
    public static class HealOnceLedger
    {
        sealed class Box { public readonly HashSet<string> Done = new HashSet<string>(); }
        static readonly WeakKeyTable<object, Box> table = new WeakKeyTable<object, Box>();

        public static bool IsMarked(object ctx, string who)
        {
            if (ctx == null) throw new ArgumentNullException("ctx");
            Box b;
            return table.TryGetValue(ctx, out b) && b.Done.Contains(who);
        }

        public static void Mark(object ctx, string who)
        {
            if (ctx == null) throw new ArgumentNullException("ctx");
            if (string.IsNullOrEmpty(who)) throw new ArgumentException("OathboundRevenant: HealOnceLedger.Mark without a target kind");
            table.GetValue(ctx, _ => new Box()).Done.Add(who);
        }
    }

     
    public sealed class ResourceRow
    {
        public readonly string AbilityKey, ResourceGuid, GrantFeature, Consumer;
        public readonly int Amount;
        public ResourceRow(string ability, string resource, string grant, int amount, string consumer) { AbilityKey = ability; ResourceGuid = resource; GrantFeature = grant; Amount = amount; Consumer = consumer; }
    }

    public static class ResourcePlan
    {
        // 출처: 계획 Global Constraints 「자원 배선」 행(T5 분: 아군 안수 복제 · 채널 · 죽음의 안수 — 자원 = 본편 안수 자원, 부여 = 본편 LayonHandsFact 를 묶은 망자의 손).
        public static readonly IReadOnlyList<ResourceRow> Rows = new[]
        {
            new ResourceRow("RevenantLayOnHands", Guids.Game.LayOnHandsResource, "RevenantHands", RevenantRules.Dials.HandsCostLoh, "Ability"),
            new ResourceRow("NegativeChannel", Guids.Game.LayOnHandsResource, "RevenantHands", RevenantRules.Dials.ChannelCostLoh, "Ability"),
            new ResourceRow("DeathsTouch", Guids.Game.LayOnHandsResource, "RevenantHands", RevenantRules.Dials.DeathsTouchCostLoh, "Ability"),
            // T6: 공포의 응시(3/일) · 사자의 외침(1/일) — 자원 = 우리 하루 자원, 부여 = 그 재주. 사용 1회당 1.
            new ResourceRow("TerribleGaze", Guids.GazeResource, "TerribleGaze", RevenantRules.Dials.UsesPerActivation, "Ability"),
            new ResourceRow("WailOfTheDead", Guids.WailResource, "WailOfTheDead", RevenantRules.Dials.UsesPerActivation, "Ability"),
            // T7: 언데드 지배(1/일, 능력) · 원한의 반격(3/일, RiposteAction 이 소비) · 꺼지지 않는 맹세(1/일, UnyieldingOathComponent 가 소비).
            new ResourceRow("CommandUndead", Guids.CommandResource, "CommandUndead", RevenantRules.Dials.UsesPerActivation, "Ability"),
            new ResourceRow("VengefulRiposte", Guids.RiposteResource, "VengefulRiposte", RevenantRules.Dials.UsesPerActivation, "RiposteAction"),
            new ResourceRow("UnyieldingOath", Guids.UnyieldingResource, "UnyieldingOath", RevenantRules.Dials.UsesPerActivation, "UnyieldingComponent"),
        };

        public static ResourceRow Row(string ability)
        {
            var r = Rows.FirstOrDefault(x => x.AbilityKey == ability);
            if (r == null) throw new ArgumentException("OathboundRevenant: no resource row for " + ability);
            return r;
        }

        public static IReadOnlyList<string> Validate()
        {
            var e = new List<string>();
            foreach (var r in Rows)
            {
                if (r.Amount < 1) e.Add(r.AbilityKey + " amount " + r.Amount);
                if (string.IsNullOrEmpty(r.ResourceGuid)) e.Add(r.AbilityKey + " without a resource");
                if (string.IsNullOrEmpty(r.GrantFeature)) e.Add(r.AbilityKey + " without a granting feature");
                if (r.Consumer != "Ability" && r.Consumer != "RiposteAction" && r.Consumer != "UnyieldingComponent") e.Add(r.AbilityKey + " consumer " + r.Consumer);
            }
            if (Rows.Select(r => r.AbilityKey).Distinct().Count() != Rows.Count) e.Add("duplicate ability rows");
            return e;
        }
    }
}
