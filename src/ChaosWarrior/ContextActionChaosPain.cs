using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.ElementsSystem;
using Kingmaker.UnitLogic.Mechanics.ContextData;
using Kingmaker.Designers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using static WotrClassMod.Common.BlueprintUtil;

namespace ChaosWarrior
{
     
    public class ContextActionChaosPain : ContextAction
    {
        public override string GetCaption() { return "Chaos Warrior: pain on hit"; }

        public override void RunAction()
        {
            ActionGuard.Run(ErrorLedger.SitePain, () => { PainCore.Hit(new GamePainWorld(Context, Target == null ? null : Target.Unit), ChaosErrors.Report, ChaosErrors.Dud); }, ChaosErrors.Sink);
        }
    }

    // IPainWorld 의 게임 구현 — 엔진 값을 옮길 뿐(판단은 PainCore).
     
    internal sealed class GamePainWorld : IPainWorld
    {
        readonly MechanicsContext ctx;
        readonly UnitEntityData caster, target;

        internal GamePainWorld(MechanicsContext context, UnitEntityData targetUnit)
        {
            ctx = context;
            caster = context == null ? null : context.MaybeCaster;
            target = targetUnit;
        }

        static UnitEntityData U(object o)
        {
            var u = o as UnitEntityData;
            if (u == null) throw new ArgumentException("ChaosWarrior: pain world expected a unit, got " + (o == null ? "null" : o.GetType().Name));
            return u;
        }

        public bool HasCaster { get { return caster != null; } }
        public bool HasTarget { get { return target != null; } }
        public bool CasterRageActive { get { return caster.Descriptor.Buffs.GetBuff(Rage.Buff) != null; } }
        public bool TargetIsEnemyOfCaster { get { return caster.IsEnemy(target); } }
        public int WarpriestLevel { get { return caster.Descriptor.Progression.GetClassLevel(Get<BlueprintCharacterClass>(Guids.Game.WarpriestClass)); } }
        public int WisdomMod { get { return caster.Stats.Wisdom.Bonus; } }
        public object Target { get { return target; } }

        // 시전자에게 붙은 그 변이 표식 인스턴스의 순위들(개편 O7). 버프 목록이 없거나 블루프린트 없는 버프가 섞여 있으면 null(읽을 수 없음 — 핵심이 던진다).
        // 본편 ContextValue{CasterBuffRank} 를 쓰지 않는다: 그 값은 버프를 못 찾으면 조용히 0 이다(정찰 K1) — 판독 불가가 「순위 0」으로 새지 않게 직접 읽는다.
        public IReadOnlyList<int> MarkerRanksOnCaster(string mutationId)
        {
            var marker = Mutations.Get(mutationId);   // 표에 없는 Id 면 던진다
            var buffs = caster.Descriptor.Buffs;
            if (buffs == null) return null;
            var r = new List<int>();
            foreach (var b in buffs.RawFacts)
            {
                if (b == null || b.Blueprint == null) return null;
                if (b.Blueprint == marker) r.Add(b.Rank);
            }
            return r;
        }

        // 명중 무기(개편 O7 Step 0 — Assembly-CSharp IL 직독 2026-11-17, md5 앞 12 748b3a143b60): AbstractWeaponTrigger.RunActions @0029~0037 이 ContextData<ContextAttackData>.Request().Setup(evt.AttackRoll, …) 를 싣고
        //   그 안에서 Action.Run 을 부른다(직접 경로 DidTrigger @0087 · 지연 경로 <DidTrigger>b__0 @0039 둘 다). AttackRoll.Weapon = WeaponStats.Weapon(get_Weapon @0001~0006) = RuleAttackWithWeapon.Weapon(.ctor @002D·@0042~004E)
        //   = AttackHandInfo.Weapon = Hand.Weapon(@0001~0006) — 사지 공격의 Hand 는 그 사지 칸(UnitHelper.CollectAttacks @001E AdditionalLimbs → @00AE new AttackHandInfo(slot)). 그래서 사지 공격이면 그 사지의 무기다.
        // 문맥 · 굴림 · 무기 · 블루프린트 중 하나라도 null 이면 Known = false(핵심이 「판정 불가」로 보고).
        BlueprintItemWeapon HitWeapon()
        {
            var data = ContextData<ContextAttackData>.Current;
            var roll = data == null ? null : data.AttackRoll;
            var weapon = roll == null ? null : roll.Weapon;
            return weapon == null ? null : weapon.Blueprint;
        }
        public bool HitWeaponKnown { get { return HitWeapon() != null; } }
        public string HitWeaponGuid { get { var bp = HitWeapon(); if (bp == null) throw new InvalidOperationException("ChaosWarrior: HitWeaponGuid read without a known hit weapon"); return bp.AssetGuid.ToString(); } }

        MechanicsContext Probe(UnitEntityData u) { return new MechanicsContext(caster, caster.Descriptor, Pain.ProbeBuff, null, u); }
        MechanicsContext SaveContext(UnitEntityData u) { return new MechanicsContext(caster, caster.Descriptor, Pain.SaveSource, null, u); }

        public bool ProbeCanApply(object unit)
        {
            var u = U(unit);
            return Rulebook.Trigger(new RuleCanApplyBuff(u, Pain.ProbeBuff, Probe(u), null)).CanApply;
        }

        public bool SpellImmune(object unit)
        {
            var u = U(unit);
            var part = u.Get<UnitPartSpellResistance>();
            return part != null && part.IsImmune(Probe(u), false);
        }

        public IReadOnlyList<object> PainInstancesOn(object unit)
        {
            return U(unit).Descriptor.Buffs.RawFacts.Where(b => b.Blueprint == Pain.Buff).Cast<object>().ToList();
        }

        public IReadOnlyList<object> CounterInstancesOn(object unit)
        {
            return U(unit).Descriptor.Buffs.RawFacts.Where(b => b.Blueprint == Pain.CounterBuff).Cast<object>().ToList();
        }

        public IReadOnlyList<object> LegacyOverflowInstancesOn(object unit)
        {
            return U(unit).Descriptor.Buffs.RawFacts.Where(b => b.Blueprint == Pain.LegacyOverflowBuff).Cast<object>().ToList();
        }

        // 인스턴스의 시전자(문맥 없음 = null — 핵심은 어느 시전자와도 같지 않다고 본다).
        public object CasterOf(object inst) { var b = (Buff)inst; return b.Context == null ? null : b.Context.MaybeCaster; }
        public object Caster { get { return caster; } }
        public bool TargetAlive { get { return !target.State.IsDead; } }

        public int RankOf(object inst) { return ((Buff)inst).Rank; }

        public object AddCounter(object unit, int rank, int rounds)
        {
            var b = U(unit).AddBuff(Pain.CounterBuff, ctx, rounds.Rounds().Seconds);
            if (b != null && b.Rank != rank) b.SetRank(rank, true);
            return b;
        }

        // 벌점을 계수의 소유자에게 붙이고 계수의 자식으로 저장한다(소유자 2026-11-17 — 분노 → 변이의 GameMutationWorld.ApplyChild 와 같은 길: Buff.StoreFact @0000~0022 는 같은 소유자일 때만 저장,
        //   계수가 지워질 때 Buff.OnRemove @0047~0095 가 저장 칸마다 RemoveFact). 벌점만 해제돼 계수에 떨어진 참조가 남아도 계수 제거 때 무동작이다(Quicken.cs 머리말 K10 판독 — EntityFactsManager.Remove @001F~002B).
        // 「붙지 않음」 = AddBuff 가 null 이거나 붙은 버프가 그 유닛에 붙어 있지 않음(붙는 도중 스스로 지워짐) → null(핵심이 보고).
        public object AddPenaltyChild(object counter, int rank, int rounds)
        {
            var c = (Buff)counter;
            var owner = c.Owner == null ? null : c.Owner.Unit;
            if (owner == null) throw new InvalidOperationException("ChaosWarrior: the pain counter has no owner unit");
            var b = owner.AddBuff(Pain.Buff, ctx, rounds.Rounds().Seconds);
            if (b == null || !b.IsAttached || b.IsDisposed || b.Owner != c.Owner) return null;
            if (b.Rank != rank) b.SetRank(rank, true);
            if (!Mutations.IsStoredBy(c, b)) c.StoreFact(b);
            return b;
        }

        public void AdoptChild(object counter, object penalty)
        {
            var c = (Buff)counter; var p = (Buff)penalty;
            if (!Mutations.IsStoredBy(c, p)) c.StoreFact(p);
        }

        public bool IsChildOf(object penalty, object counter) { return Mutations.IsStoredBy((Buff)counter, (Buff)penalty); }

        // 엔진 Buff.IsNotDispelable(자동 속성, [JsonProperty] — 세이브에 남는다). 본편 NotDispelable 부품의 OnActivate 가 켠다(@0006~000D). 해제 주문 TryDispelBuff @004E 가 이 값을 본다.
        public bool IsUndispellable(object counter) { return ((Buff)counter).IsNotDispelable; }

        public void SetRank(object inst, int rank) { ((Buff)inst).SetRank(rank, true); }
        public void RefreshRounds(object inst, int rounds) { ((Buff)inst).SetDuration(rounds.Rounds().Seconds); }
        public void RemoveInstance(object inst) { ((Buff)inst).Remove(); }

        public IReadOnlyList<(object Unit, bool IsEnemyOfCaster, bool Alive, int DistFeet)> Around(object unit, int feet)
        {
            var center = U(unit);
            float footMeters = 1.Feet().Meters;
            return GameHelper.GetTargetsAround(center.Position, feet.Feet(), false, true)
                .Select(x => ((object)x, caster.IsEnemy(x), !x.State.IsDead, (int)(UnityEngine.Vector3.Distance(center.Position, x.Position) / footMeters)))
                .ToList();
        }

        public bool SaveWill(object unit, int dc)
        {
            var u = U(unit);
            var rule = new RuleSavingThrow(u, BuildKit.E<SavingThrowType>("Will"), dc) { Reason = new RuleReason(SaveContext(u)) };
            return Rulebook.Trigger(rule).IsPassed;
        }

        public int RollConfusionRounds()
        {
            var dice = new DiceFormula(ChaosRules.Dials.ConfusionDiceCount, BuildKit.E<DiceType>("D" + ChaosRules.Dials.ConfusionDiceSides));
            return Rulebook.Trigger(new RuleRollDice(caster, dice)).Result;
        }

        public bool ApplyConfusion(object unit, int rounds)
        {
            var u = U(unit);
            return u.AddBuff(Get<BlueprintBuff>(Guids.Game.Confusion), SaveContext(u), rounds.Rounds().Seconds) != null;
        }
    }
}
