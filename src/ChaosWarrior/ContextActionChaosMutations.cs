using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Root;
using Kingmaker.Dungeon.Blueprints;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using WotrClassMod.Common;



using static WotrClassMod.Common.BlueprintUtil;

namespace ChaosWarrior
{
     
    // 주입 무장이면 던지지 않고 장부만(ActionGuard) → ActionList.Run 의 예외 기록·공용 포착에 닿지 않는다.
    public class ContextActionChaosMutations : ContextAction
    {
        public override string GetCaption() { return "Chaos Warrior: roll mutations"; }

        public override void RunAction()
        {
            ActionGuard.Run(ErrorLedger.SiteMutationRoll, () =>
            {
                string[] applied;
                // 독립 검토 R4-F2: 이 분노 인스턴스는 이번 실행에서 「굴림 경로를 지났다」 — 불러오기 재충전(QuickenRefill)은 처음 보는 인스턴스에만 돈다. 굴림 결과와 무관하게 먼저 적는다.
                QuickenRefill.MarkSeen(ContextData<Buff.Data>.Current);
                GameMutationWorld.BeginRecord();
                MutationCore.Roll(new GameMutationWorld(Context), ChaosErrors.Report, ChaosErrors.Dud, out applied);
                GameMutationWorld.Record(applied);
            }, ChaosErrors.Sink);
        }
    }

    // IMutationWorld 의 게임 구현 — 엔진 값을 옮길 뿐 판단하지 않는다(판단은 MutationCore).
    internal sealed class GameMutationWorld : IMutationWorld
    {
        readonly UnitEntityData caster;
        readonly Buff parent;

        internal GameMutationWorld(MechanicsContext context)
        {
            caster = context == null ? null : context.MaybeCaster;
            var data = ContextData<Buff.Data>.Current;
            parent = data == null ? null : data.Buff;
        }

        // 행동 문맥 밖(불러오기 재충전 — QuickenRefill)에서 쓰는 세계: 시전자 = 분노 버프의 주인, 부모 = 그 분노 버프. 판단은 하지 않는다(ParentIsActiveRage 등은 핵심이 본다).
        internal GameMutationWorld(UnitEntityData owner, Buff rage)
        {
            caster = owner;
            parent = rage;
        }

        public bool QuickenSpent { get { return caster.Buffs.HasFact(Quicken.Spent); } }
        public bool HasCaster { get { return caster != null; } }
        public bool ParentIsActiveRage { get { return parent != null && parent.Blueprint == Rage.Buff && parent.IsAttached && !parent.IsDisposed && parent.Owner == caster.Descriptor; } }
        public int WarpriestLevel { get { return caster.Descriptor.Progression.GetClassLevel(Get<BlueprintCharacterClass>(Guids.Game.WarpriestClass)); } }
        public int RangeInclusive(int min, int max) { return UnityEngine.Random.Range(min, max + 1); }
        public int[] ForcedPicks { get { return CheckHooks.Honored(ErrorLedger.SiteMutationRoll) ? CheckHooks.ForcePicks : null; } }
        public int ForcedElement { get { return CheckHooks.Honored(ErrorLedger.SiteMutationRoll) ? CheckHooks.ForceShellElement : -1; } }

        // 붙이고 부모 분노에 「한 번만」 저장한다(개편 O3, 계획 K2 — Assembly-CSharp IL 직독 2026-11-17, md5 앞 12 748b3a143b60; 덤프 %TEMP%\cw-o3\k2.txt·k2b.txt):
        //   순위형(Stacking Rank)에 같은 블루프린트를 다시 붙이면 엔진은 기존 인스턴스에 AddRank(1) 하고 「기존 인스턴스」를 돌려준다
        //     (BuffCollection.PrepareFactForAttach @025F~0269 → EntityFactsManager.Add @00E5~00F9: 이미 m_Facts 에 있으면 그대로 반환).
        //   Buff.StoreFact 는 중복을 막지 않는다(@0038~003F: List.Add 뿐) → 같은 인스턴스를 또 저장하면 저장 칸이 둘이 되고, 분노 종료 때 Buff.OnRemove @005D~0078 이 그 사실을 두 번 RemoveFact 한다.
        //     두 번째 제거는 EntityFactsManager.Remove @001F~002B(m_Facts.Remove 가 false → 건너뜀)에서 끝나는 것으로 읽었다(정찰 미확인 1 의 IL 판독 — 게임 안 미확인). 그래도 저장 칸 수 == 인스턴스 수를 지킨다(되읽기의 순위 합이 이것에 기댄다).
        //   순위 > 1 인 자식의 제거: BuffCollection.PrepareFactForDetach @0000~001C 는 RemoveByRank 문맥(ContextActionRemoveBuff{RemoveRank = true} · CorruptionProtection 휴식 처리) 안에서는 순위만 1 내리고 버프를 남긴다.
        //     분노 버프가 그 문맥 안에서 지워지면 순위형 자식이 순위 n−1 로 남는다 — 본편 Crystal Mind 의 분노 해제(RageAlias 가 복제하는 행동)는 RemoveRank false(zip 직독) — RageAlias.Build 가 복제본의 값을 검사하고 참이면 던진다(AliasPlan.RequireWholeRemoval, 독립 검토 R1-I3). 그래도 남으면 분노 종료 직후 고아 정리가 지우고 기록한다(OrphanMutationSweep).
        //   그래서 돌려받은 인스턴스가 이미 저장돼 있으면 다시 저장하지 않는다. 인스턴스형(Stacking Stack)은 매번 새 인스턴스(@00FE~0105 → @0270, 첫 순위 1 = Buff..ctor @0000~0002·@0023~0025)라 매번 저장한다.
        //   StoreFact 는 소유자가 다르면 기록만 남기고 저장하지 않는다(@0000~0022) — 그 경우는 핵심의 되읽기(순위 합 0)가 잡는다.
        // 「붙지 않음」 = AddBuff 가 null(면역·사망 — PrepareFactForAttach @0008·@0038·@0069) 이거나, 돌려받은 버프가 시전자에 붙어 있지 않음(붙는 도중 스스로 지워진 경우 — 저장 목록에만 남아 순위 합이 맞아 보이는 것을 막는다).
        public bool ApplyChild(string buffId)
        {
            var bp = Get<BlueprintBuff>(buffId);
            var b = caster.AddBuff(bp, parent.Context, null);
            if (b == null || !b.IsAttached || b.IsDisposed || b.Owner != caster.Descriptor) return false;
            if (!Mutations.IsStoredBy(parent, b)) parent.StoreFact(b);
            return true;
        }

        public IReadOnlyList<ChildFact> ChildrenOfParent() { return Mutations.ChildFacts(parent); }

        // 시전자의 버프 목록에서 우리 변이 버프(표식 · 원소 껍질)를 인스턴스마다 한 행(GUID, Buff.Rank)으로(독립 검토 R1-M3) — 부모가 저장했는지와 무관하게 「실제로 붙어 있는 것」.
        // 버프 목록이 없거나 블루프린트 없는 버프가 섞여 있으면 null(우리 것인지 알 수 없다 — 건너뛰지 않는다). 핵심이 저장 목록과 대조한다.
        public IReadOnlyList<ChildFact> AttachedMutationBuffs()
        {
            var buffs = caster.Buffs;
            if (buffs == null) return null;
            var r = new List<ChildFact>();
            foreach (var b in buffs.RawFacts)
            {
                if (b == null || b.Blueprint == null) return null;
                string guid = b.Blueprint.AssetGuid.ToString();
                if (MutationSpecs.IsMarker(guid) || MutationSpecs.ShellBuffIds.Contains(guid)) r.Add(new ChildFact(guid, b.Rank));
            }
            return r;
        }

        // 시전자에게 붙어 있는 우리 신속 시전 권리 버프(개편 O8) — 인스턴스마다 GUID 한 행. 버프 목록이 없거나 블루프린트 없는 버프가 섞여 있으면 null(우리 것인지 알 수 없다 — 건너뛰지 않는다).
        // 권리 버프는 분노의 자식으로 ApplyChild 가 붙인다(위 — 변이 표식과 같은 길). Once 로 시전 뒤 지워진 권리는 이 목록에 없다(엔진이 사실을 떼어 낸다 — Quicken.cs 머리말).
        public IReadOnlyList<string> QuickenRightsOnCaster()
        {
            var buffs = caster.Buffs;
            if (buffs == null) return null;
            var r = new List<string>();
            foreach (var b in buffs.RawFacts)
            {
                if (b == null || b.Blueprint == null) return null;
                string guid = b.Blueprint.AssetGuid.ToString();
                if (QuickenSpec.IsRight(guid)) r.Add(guid);
            }
            return r;
        }

        // 권리 전부 지우기(독립 검토 R4-F2·F4): 시전자에게 붙은 우리 권리 버프를 Buff.Remove 로 떼고, 부모 분노의 m_StoredFacts 에서 권리 참조를 뺀다
        //   (Once 로 이미 떨어진 참조도 — 남겨 두면 되읽기 ChildFacts 가 「저장된 권리」로 센다. 분노 종료 때 엔진이 빼 둔 참조를 다시 지우려 하지 않을 뿐이고, 그 재제거는 원래 무동작이다 — Quicken.cs 머리말 K10).
        // 버프 목록 · 저장 목록이 없거나 블루프린트 없는 사실이 섞여 있으면 false(무엇을 지울지 알 수 없다 — 건너뛰지 않는다). 지운 뒤에도 붙어 있으면 false.
        public bool RemoveQuickenRights()
        {
            var buffs = caster.Buffs;
            if (buffs == null || parent == null) return false;
            var mine = new List<Buff>();
            foreach (var b in buffs.RawFacts)
            {
                if (b == null || b.Blueprint == null) return false;
                if (QuickenSpec.IsRight(b.Blueprint.AssetGuid.ToString())) mine.Add(b);
            }
            foreach (var b in mine)
            {
                b.Remove();
                if (buffs.RawFacts.Contains(b)) return false;
            }
            // 엔진 목록 자체를 고친다(Mutations.StoredFacts 는 사본을 준다). null = 아직 저장 0개(Buff.StoreFact @0023~0033 가 첫 저장 때 만든다) — 뺄 것이 없다.
            var stored = BuildKit.GetF(parent, "m_StoredFacts") as List<EntityFact>;
            if (stored == null) return BuildKit.GetF(parent, "m_StoredFacts") == null;
            if (stored.Any(f => f == null || f.Blueprint == null)) return false;
            stored.RemoveAll(f => QuickenSpec.IsRight(f.Blueprint.AssetGuid.ToString()));
            return true;
        }

        // 무기가 든 추가 사지 전부(모든 출처) — 개편 O4, 계획 K3(Assembly-CSharp IL 직독 2026-11-17, md5 앞 12 748b3a143b60; 덤프 %TEMP%\cw-o4\k3.txt·body.txt·sync.txt):
        //   사지는 AddBuff 「안에서」 붙는다: BuffCollection.AddBuff → EntityFactsManager.Add @0114(fact.Attach) → EntityFact.Attach @00CA~00D2(Activate) → Activate @0183~0190(Manager.IsTurnedOn 이면 TurnOn)
        //     → EntityFactComponent.TurnOn @001F(OnTurnOn) → AddAdditionalLimb.OnTurnOn @0007~0029(Body.AddAdditionalLimb(Weapon, Fact, false) → LimbIndex) → UnitBody.AddLimbToIndex @0039~0046(칸에 무기 삽입).
        //     그래서 ApplyChild 가 돌아온 직후의 이 목록에 그 사지가 있다. 붙지 않는 경우(전부 대조가 「어긋남」으로 보고한다 — 조용히 넘어가지 않는다):
        //       버프가 억제돼 있음(Buff.IsEnabled @0008~0011 = !IsSuppressed → Activate @0001~0006 에서 바로 반환) · 엔진이 범주 상한 초과분을 뺌(UnitBody.AddAdditionalLimb @0018~0031 MarkWorstLimbItemAsUnused, 기록 없음)
        //       · 변신 중(아래).
        //   읽는 목록 = UnitBody.AdditionalLimbs = m_PolymorphAdditionalLimbs ?? m_AdditionalLimbs(@0001~000B) — 전체 공격의 사지 공격이 세는 바로 그 목록(RuleCalculateAttacksCountLimbs.OnTrigger @0006~000B).
        //     변신(폴리모프) 중에는 변신 쪽 목록이 온다: 우리 사지는 m_AdditionalLimbs 에만 들어가고(AddLimbToIndex @0058~0066 — KeepInPolymorph 인 무기만 변신 쪽에도, 복제 원본 11종은 전부 false · zip 직독 `jbp: limb caps root`)
        //     → 변신 중에 자연 공격 변이를 붙이면 「사지 0 vs 뽑힌 n」이 된다(실제로도 그 사지로는 공격하지 못한다). 엔진의 범주 상한은 m_AdditionalLimbs 로 센다(FindAdditionalLimbsWith @0006~000C).
         
        //   「무기가 든 칸」 = slot.HasWeapon(= HasItem && MaybeWeapon != null, WeaponSlot.get_HasWeapon @0000~0011) — 엔진과 같은 조건(OnTrigger @0024~0047). 무기 = MaybeWeapon(@0040).
        //   null 칸은 목록 전체를 읽을 수 없음으로 본다(독립 검토 R2-F5 — 엔진은 그 칸을 건너뛰지만 무엇의 칸인지 모르는 것을 「사지 아님」으로 세지 않는다. 엔진은 칸을 언제나 new WeaponSlot 으로 만든다 — AddLimbToIndex @0012).
        // 몸·목록·든 무기의 블루프린트나 형식을 읽을 수 없으면 null 을 돌려준다(핵심이 LimbsUnreadable 로 보고) — 빈 목록이나 「그 칸만 빼고」로 바꾸지 않는다.
        public IReadOnlyList<LimbFact> Limbs()
        {
            var body = caster.Body;
            if (body == null) return null;
            return LimbFacts(body.AdditionalLimbs);
        }

        // 사람 쪽 목록 = UnitBody.m_AdditionalLimbs(비공개, ReflectionLookups 원장 행). 필드가 없으면 GetF 가 던지고(ActionGuard 가 계수), 형식이 다르거나 null 이면 null(LimbsUnreadable).
        public IReadOnlyList<LimbFact> HumanLimbs()
        {
            var human = HumanSlots();
            return human == null ? null : LimbFacts(human);
        }

        // 공개 AdditionalLimbs(공격 목록)가 사람 쪽 목록과 같은 객체인가 — get_AdditionalLimbs @0001~000B 가 m_PolymorphAdditionalLimbs 가 null 일 때 m_AdditionalLimbs 를 그대로 준다. 어느 쪽이든 읽을 수 없으면 null.
        public bool? LimbListsShared
        {
            get
            {
                var body = caster.Body;
                var human = HumanSlots();
                if (body == null || human == null || body.AdditionalLimbs == null) return null;
                return ReferenceEquals(body.AdditionalLimbs, human);
            }
        }

        // UnitBody.IsPolymorphKeepSlots(공개 속성 — ApplyPolymorphEffect @003F~0042 가 m_PolymorphKeepSlots 에 쓴다, KeepInPolymorph @0004~0009 가 읽는다). 몸이 없으면 null.
        public bool? PolymorphKeepsSlots
        {
            get
            {
                var body = caster.Body;
                if (body == null) return null;
                return body.IsPolymorphKeepSlots;
            }
        }

        List<Kingmaker.Items.Slots.WeaponSlot> HumanSlots()
        {
            var body = caster.Body;
            if (body == null) return null;
            return BuildKit.GetF(body, "m_AdditionalLimbs") as List<Kingmaker.Items.Slots.WeaponSlot>;
        }

        static IReadOnlyList<LimbFact> LimbFacts(IEnumerable<Kingmaker.Items.Slots.WeaponSlot> slots)
        {
            if (slots == null) return null;
            var r = new List<LimbFact>();
            foreach (var slot in slots)
            {
                if (slot == null) return null;
                if (!slot.HasWeapon) continue;
                var weapon = slot.MaybeWeapon;
                var bp = weapon == null ? null : weapon.Blueprint;
                if (bp == null || bp.Type == null) return null;
                r.Add(new LimbFact(bp.AssetGuid.ToString(), bp.Category.ToString(), bp.KeepInPolymorph));   // 독립 검토 R3-F3: 칸 무기의 KeepInPolymorph(엔진 UnitBody.KeepInPolymorph @000B~0011 이 읽는 그 필드)
            }
            return r;
        }

        // 엔진의 범주 상한(개편 O5, 계획 K4 — Assembly-CSharp IL 직독 2026-11-17, md5 앞 12 748b3a143b60): Kingmaker.Dungeon.Blueprints.BlueprintPersonageLimits.GetWeaponSlotCount(WeaponCategory) : static int.
        //   BlueprintRoot.Instance.PersonageLimits(= 블루프린트 2a609e8c…, Root.jbp m_PersonageLimits)의 m_DopLimbMaxCount 에서 그 범주의 Count 를, 없으면 −1 을 돌려준다(@0000~003D).
        //   엔진 UnitBody.AddAdditionalLimb @0011~0017 이 부르는 바로 그 함수다 — 우리 표(ChaosRules.LimbCapsExpected)가 아니라 이것으로 판정한다(다른 모드가 상한을 바꾸면 그 값을 따른다).
        // 범주 이름이 엔진 WeaponCategory 에 없으면 던진다(BuildKit.E). 루트나 상한 블루프린트가 없으면 null(엔진 함수는 그 경우 NullReferenceException 을 낸다 — 후보 판정이 「읽을 수 없음」으로 던진다).
        public int? LimbCap(string category)
        {
            var cat = BuildKit.E<WeaponCategory>(category);
            var root = BlueprintRoot.Instance;
            if (root == null || root.PersonageLimits == null) return null;
            return BlueprintPersonageLimits.GetWeaponSlotCount(cat);
        }

        // 변신(폴리모프) 중인가(개편 O5 — Assembly-CSharp IL 직독 2026-11-17): UnitBody.IsPolymorphed(공개 속성, [JsonProperty] — 세이브를 넘어 보존).
        //   ApplyPolymorphEffect @0024~0026 이 true 로, CancelPolymorphEffect @001D~001F 가 false 로 쓴다. true 인 동안 GeneratePolymorphAdditionalLimbs @0000~0006 이 만든 변신 쪽 사지 목록이 있고
        //   (ClearPolymorphEquipment 가 지운다), UnitBody.AdditionalLimbs 는 그 목록을 준다 — 위 Limbs() 주석.
        // 몸을 읽을 수 없으면 null(핵심이 PolymorphUnreadable 로 보고) — false 로 바꾸지 않는다.
        public bool? CasterPolymorphed
        {
            get
            {
                var body = caster.Body;
                if (body == null) return null;
                return body.IsPolymorphed;
            }
        }

        // 점검 전용 기록(CheckHooks.LastRoll): 굴림 앞에서 비우고(앞 분노의 값이 남지 않게) 뒤에서 적는다. 굴림이 던지면 null 로 남는다 — 점검은 null 을 「읽을 수 없음」으로 FAIL 처리한다.
        internal static void BeginRecord()
        {



        }

        // 뽑힌 변이 목록을 모드 로그 한 줄로(개편 설계 §12-4 「분노를 켤 때 뽑힌 변이 목록을 전투 기록(또는 모드 로그) 한 줄」 — 모드 로그 쪽, 계획 「판정 대기」 P5). 굴리지 않은 경우(억제 뒤 재활성·실패)는 줄이 없다(목록이 비어 있다).
        internal static void Record(string[] applied)
        {
            if (applied == null) throw new ArgumentNullException("applied");



            if (applied.Length > 0) ModContext.Log("ChaosWarrior: " + MutationCore.RollLine(applied));
        }
    }
}
