using System;
using System.Collections.Generic;
using WotrClassMod.Common;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.Items.Slots;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Parts;
using TurnBased.Controllers;
using UnityEngine;

namespace PackWarden
{
     
    // 메인 펫의 무기 휘두름(RuleAttackWithWeapon) 하나마다 살아 있는 보조가 같은 대상에게 같은 프레임에 1타씩 진짜 RuleAttackWithWeapon 을 굴린다.
    //
    // 엔진 경로(dnlib 2026-11-14, Assembly-CSharp SHA256 2CB7160B…8953):
    //   EventBus.Subscribe(runtime) @0045 → RulebookEventBus.Subscribe · RulebookSubscribersList<T>.OnEventDidTrigger(TEvent, object)
    //   @0018 ISubscriptionProxy.RequestEventContext(= ComponentRuntime → ComponentEventContext.Setup, Owner 문맥) → @0030 이 처리기 → @003B Dispose.
    //   바깥 OnEventDidTrigger(RulebookEvent) 가 구독자 호출을 try[@002C,@003A) 로 감싸 @003B~0047 PFLog.Default.Exception 으로 **예외를 삼킨다**.
    //   → 이 처리기의 실패는 FailCount 로 센다(아래 catch — FailCount 계약의 유일한 정의). 점검(Scenarios.ShadowHealth)이 실행 시작 대비 증가 0 을 단언한다.
    // 이 블루프린트 컴포넌트 인스턴스는 모든 지도자가 공유한다 → 인스턴스 필드 금지(시험 잠금). 공격 기록은 부모 규칙에, 경고 이력은 지도자별 약한 키에 둔다.
    public class PackShadowStrikes : UnitFactComponentDelegate, IGlobalRulebookHandler<RuleAttackWithWeapon>
    {
        public sealed class Decision
        {
            public UnitEntityData Aux;
            public ShadowRules.StrikeVerdict Verdict;
            public int TargetHpBefore;
            public bool InRange, CanAct;
            public string WeaponTypeGuid;
            public int LimbPos;
        }

        public sealed class SlotEntry
        {
            public int Slot;
            public ShadowRules.SlotStatus Status; // 생성 시 Unknown(R3-03)
            public string AuxId;                  // Live·NotLive = 비지 않음, Empty = null
            public Decision Decision;             // Live 일 때만
        }

        public sealed class ParentSnapshot
        {
            public int Frame;
            public UnitEntityData Leader;
            public ShadowRules.ShadowVerdict Verdict; // 생성 시 Unknown
            public bool Complete;
             
            // 생성 시 Unknown(무기 단계까지 가지 않은 판정 — 관문·원거리 무동작).
            public ShadowRules.SlotMatch SlotSource;
            public WeaponSlot MainSlot;               // 따라친 메인 슬롯(SlotSource 가 정해진 경우만)
            public SlotEntry[] Slots;                 // Verdict == Shadow 일 때만(길이 PackRules.AuxSlotCount)
        }

        sealed class LeaderState
        {
            public bool WarnedRanged;
            public readonly HashSet<string> WarnedAnimAux = new HashSet<string>();
        }

        // 부모 규칙 객체 키(R2-03): 같은 메인 규칙을 두 번 처리하면(A→B→A 포함) 던진다. GC 와 함께 사라진다.
        static readonly RulebookEvent.CustomDataKey snapshotKey = new RulebookEvent.CustomDataKey("PackWarden.ParentSnapshot");
        static readonly ShadowRules.ActiveContext<RuleAttackWithWeapon> s_Active = new ShadowRules.ActiveContext<RuleAttackWithWeapon>();
        static readonly WeakKeyTable<UnitEntityData, LeaderState> s_Leaders = new WeakKeyTable<UnitEntityData, LeaderState>();

        public const string FailLogHead = ShadowRules.StrikeFailLogHead;
        // 엔진 기회 공격 동작과 같은 속도(UnitAttackOfOpportunity.OnRun @0087~009A: 턴제 아니면 ldc.r4 3, 턴제면 ldc.r4 1).
        public const float AnimSpeedRealTime = 3f, AnimSpeedTurnBased = 1f;

        // 진단(점검이 읽는다). FailCount 계약은 OnEventDidTrigger 의 catch 한 곳.
        public static int StrikeCount, SkipDeadTarget, SkipCannotAct, SkipOutOfRange, AnimWarnCount, AnimBusyCount, RangedWarnCount, FailCount;
        public static string LastWarning;
         
        // 찾은 수(진단 — 점검 S12 가 합성 슬롯 없는 휘두름으로 증가를 본다). 판정 불가(못 찾음·모호)는 FailCount 계약으로 센다(아래 catch).
        public static int SlotlessResolvedCount;
        // 판정 불가 오류 로그는 (펫, 원천)별 첫 1회(FailCount 는 매번 증가 — 시험 exe 가 FirstSeen·StrikeFailureLogged 실행).
        static readonly ShadowRules.FirstSeen s_SlotUnknownNoted = new ShadowRules.FirstSeen();

        // 슬롯 판정 불가 = 실패(던짐). Key = (펫 UniqueId, 원천) — 로그 1회 규칙의 열쇠.
        public sealed class MainSlotUnknownException : InvalidOperationException
        {
            public readonly string Key;
            public MainSlotUnknownException(string key, string message) : base(message)
            {
                if (string.IsNullOrEmpty(key)) throw new ArgumentException("PackWarden: MainSlotUnknownException needs a key", "key");
                Key = key;
            }
        }

         
        static readonly ShadowRules.FirstSeen s_PackUnreadableNoted = new ShadowRules.FirstSeen();
        public sealed class PackUnreadableException : InvalidOperationException
        {
            public readonly string Key;
            public PackUnreadableException(string key, string message) : base(message)
            {
                if (string.IsNullOrEmpty(key)) throw new ArgumentException("PackWarden: PackUnreadableException needs a key", "key");
                Key = key;
            }
        }

        // 규칙의 원천(로그·열쇠용). 결측은 결측이라고 적는다(빈 문자열 금지).
        static string SourceOf(RuleAttackWithWeapon evt)
        {
            var reason = evt.Reason;
            return reason == null ? "(no reason)"
                : reason.Ability != null ? "ability " + (reason.Ability.Blueprint != null ? reason.Ability.Blueprint.name : "(no blueprint)")
                : reason.Fact != null ? "fact " + (reason.Fact.Blueprint != null ? reason.Fact.Blueprint.name : "(no blueprint)")
                : "(reason without ability/fact)";
        }

        // 메인 슬롯 정하기: 규칙에 WeaponSlot 이 있으면 그것, 없으면 펫 몸의 무기 있는 슬롯(주손, 보조손, 추가 팔다리 — HasWeapon 으로 거름)에서
        // evt.Weapon 과 같은 객체 → 같은 블루프린트 순으로 하나를 고른다(판정 = 순수 ShadowRules.ResolveMainSlot). 판정 불가면 slot = null.
        static ShadowRules.SlotPick ResolveMainSlot(UnitEntityData pet, RuleAttackWithWeapon evt, out WeaponSlot slot)
        {
            slot = evt.WeaponSlot;
            if ((object)slot != null) return ShadowRules.ResolveMainSlot(true, null, null);

            var pb = pet.Body;
            if (pb == null) throw new InvalidOperationException("main companion has no Body");
            var cand = new List<WeaponSlot>();
            var sameInst = new List<bool>();
            var sameBp = new List<bool>();
            if (pb.PrimaryHand != null && pb.PrimaryHand.HasWeapon) cand.Add(pb.PrimaryHand);
            if (pb.SecondaryHand != null && pb.SecondaryHand.HasWeapon) cand.Add(pb.SecondaryHand);
            if (pb.AdditionalLimbs != null)
                for (int k = 0; k < pb.AdditionalLimbs.Count; k++)
                    if (pb.AdditionalLimbs[k] != null && pb.AdditionalLimbs[k].HasWeapon) cand.Add(pb.AdditionalLimbs[k]);
            foreach (var s in cand)
            {
                var w = s.MaybeWeapon;
                sameInst.Add(ReferenceEquals(w, evt.Weapon));
                sameBp.Add(w.Blueprint != null && w.Blueprint.AssetGuid == evt.Weapon.Blueprint.AssetGuid);
            }
            var pick = ShadowRules.ResolveMainSlot(false, sameInst, sameBp);
            if (ShadowRules.SlotResolved(pick.Match)) slot = cand[pick.Index];
            return pick;
        }

        // GetPet reads UnitPartPetMaster.Pets in order. An unresolved reference or a
        // pet with no UnitPartPet is skipped. The first matching pet owns the swing.
        static UnitEntityData CurrentAnimalCompanion(UnitEntityData leader)
        {
            var master = leader.Get<UnitPartPetMaster>();
            var pets = master == null ? null : master.Pets;
            if (pets == null) return null;
            for (int i = 0; i < pets.Count; i++)
            {
                var pet = pets[i];
                var part = pet.EntityPart;
                if (part != null && part.Type == PetType.AnimalCompanion) return pet.Entity;
            }
            return null;
        }





        // 처리기가 이 규칙을 우리 펫 규칙으로 받아 만든 스냅샷. null = 받은 적 없음(호출자가 판정 불가로 처리).
        public static ParentSnapshot SnapshotOf(RuleAttackWithWeapon main)
        {
            if (main == null) throw new ArgumentNullException("main");
            ParentSnapshot s;
            return main.TryGetCustomData(snapshotKey, out s) ? s : null;
        }

        // 정당한 무동작: 판정은 DidTrigger(명중·대상 생사가 정해진 뒤)에서 한다.
        public void OnEventAboutToTrigger(RuleAttackWithWeapon evt) { }

        public void OnEventDidTrigger(RuleAttackWithWeapon evt)
        {
            UnitEntityData leader = null;
            try
            {
                leader = Owner;
                if ((object)leader == null) throw new InvalidOperationException("no Owner in the component event context (F4)");
                if (evt == null) throw new ArgumentNullException("evt");
                var pet = CurrentAnimalCompanion(leader);
                // 정당한 무동작: 우리 펫의 규칙이 아님(남의 펫·보조·파티·적). 역참조 없음, 스냅샷 없음.
                if ((object)pet == null || !ReferenceEquals(evt.Initiator, pet)) return;
                // 2026-11-16: 교전 판정 근거(최근 휘두름 대상·시각) — 보조의 교전 대상 추종(ShadowBrains.Engagement 후보 2)이 읽는다.
                ShadowBrains.NoteMainSwing(pet, evt.Target);
                if (SnapshotOf(evt) != null) throw new InvalidOperationException("same main rule handled twice");
                var snap = new ParentSnapshot { Frame = Time.frameCount, Leader = leader, Complete = false };
                evt.SetCustomData(snapshotKey, snap);
                var part = leader.Get<UnitPartPack>();
                 
                var tally = part == null ? null : part.Tally();
                var pr = ShadowRules.PackReadGate(part != null, tally != null && tally.Complete);
                if (pr != ShadowRules.ShadowVerdict.Shadow)
                {
                    snap.Verdict = pr; // 스냅샷은 미완성으로 남는다(감사가 (UNKNOWN) FAIL)
                    var slots = tally.UnresolvedSlots;
                    var labels = new string[slots.Length];
                    for (int i = 0; i < slots.Length; i++)
                    {
                        int slot = slots[i];
                        labels[i] = slot + "=" + part.Auxiliaries[slot].UniqueId;
                    }
                    string ids = string.Join(",", labels);
                    throw new PackUnreadableException(leader.UniqueId + "\u0001" + ids,
                        ShadowRules.PackUnreadableHead + leader.CharacterName + ": pack slot(s) " + ids + " hold an auxiliary id that does not resolve to a unit — skipped as UNDETERMINED (not 'no pack'), no shadow strike this swing");
                }
                bool live = tally != null && tally.CompleteLiveCount() > 0;
                // 무기를 읽지 않는 관문(R2-01): 엔진 AoO 규칙엔 WeaponSlot 이 없다(UnitAttackOfOpportunity.OnAction @002F~0047).
                var g = ShadowRules.Gate(true, live, evt.ExtraAttack, evt.IsAttackOfOpportunity);
                if (g != ShadowRules.ShadowVerdict.Shadow)
                {
                    snap.Verdict = g;
                    snap.Complete = true;
                    return;
                }
                // 여기부터 따라칠 메인 근접 휘두름 후보: 결측은 던진다.
                var weapon = evt.Weapon;
                if (weapon == null) throw new InvalidOperationException("main swing without a weapon");
                if (weapon.Blueprint == null) throw new InvalidOperationException("main weapon has no blueprint");
                if ((object)evt.Target == null) throw new InvalidOperationException("main swing without a target");
                if (evt.AttackRoll == null) throw new InvalidOperationException("main swing without an attack roll");
                var w = ShadowRules.WeaponGate(weapon.Blueprint.IsRanged);
                if (w != ShadowRules.ShadowVerdict.Shadow)
                {
                    // 허용된 약한 경로 ③: 원거리 메인 = 무동작 + 지도자당 경고 1회.
                    var ls = s_Leaders.GetValue(leader, _ => new LeaderState());
                    if (!ls.WarnedRanged)
                    {
                        ls.WarnedRanged = true;
                        RangedWarnCount++;
                        LastWarning = "PackWarden: " + leader.CharacterName + "'s main companion attacked with a ranged weapon; auxiliaries do not shadow ranged swings";
                        Main.Entry.Logger.Warning(LastWarning);
                    }
                    snap.Verdict = w;
                    snap.Complete = true;
                    return;
                }
                 
                WeaponSlot mainSlot;
                var pick = ResolveMainSlot(pet, evt, out mainSlot);
                snap.SlotSource = pick.Match;
                if (!ShadowRules.SlotResolved(pick.Match))
                {
                    // 판정 불가 = 그림자 없음 + 실패(스냅샷은 미완성으로 남아 감사가 FAIL). 열쇠 = (펫, 원천).
                    snap.Verdict = ShadowRules.ShadowVerdict.SlotUnknown;
                    string src = SourceOf(evt);
                    throw new MainSlotUnknownException(pet.UniqueId + "\u0001" + src,
                        ShadowRules.SlotUnknownHead + pick.Match + " (unit=" + pet.CharacterName + " source=" + src + " weapon=" + weapon.Blueprint.name + ") — no shadow strike");
                }
                if (pick.Match != ShadowRules.SlotMatch.RuleSlot) SlotlessResolvedCount++;
                snap.MainSlot = mainSlot;
                snap.Verdict = ShadowRules.ShadowVerdict.Shadow;
                snap.Slots = new SlotEntry[PackRules.AuxSlotCount];
                s_Active.Push(evt);
                try
                {
                    for (int i = 0; i < PackRules.AuxSlotCount; i++)
                    {
                        var e = new SlotEntry { Slot = i };
                        snap.Slots[i] = e;
                        UnitEntityData aux;
                        var st = part.SlotAt(i, out aux);
                        if (st == PackRules.SlotState.Empty) { e.Status = ShadowRules.SlotStatus.Empty; continue; }
                        e.AuxId = part.Auxiliaries[i].UniqueId;
                        // Gone = 해석된 유닛이 죽음·파괴·게임 밖(확인된 「대상 아님」). Unresolved 는 위 판독 관문이 이미 막았다 — 여기 오면 같은 프레임 안에서 판독이 갈린 것(던진다).
                        if (st == PackRules.SlotState.Gone) { e.Status = ShadowRules.SlotStatus.NotLive; continue; }
                        if (st != PackRules.SlotState.Live) throw new InvalidOperationException("pack slot " + i + " is " + st + " after the pack read gate passed");
                        e.Status = ShadowRules.SlotStatus.Live;
                        e.Decision = Strike(leader, pet, aux, evt, mainSlot, Fact);
                    }
                    snap.Complete = true;
                    LogSkips(leader, snap);
                }
                finally
                {
                    s_Active.Pop(evt);
                }
            }
            catch (Exception ex)
            {
                // FailCount 계약(유일한 정의 — 계획 C1 둘째 줄): ① 증가 ② 로그 ③ 재throw. 엔진이 삼켜 PFLog 로 남기고 메인 공격은 계속된다.
                 
                FailCount++;
                var su = ex as MainSlotUnknownException;
                var pu = ex as PackUnreadableException;  
                bool deduped = su != null || pu != null;
                bool first = su != null ? s_SlotUnknownNoted.First(su.Key) : pu != null && s_PackUnreadableNoted.First(pu.Key);
                if (ShadowRules.StrikeFailureLogged(deduped, first))
                    Main.Entry.Logger.Error(FailLogHead + ((object)leader == null ? "?" : leader.CharacterName) + ": " + ex);
                throw;
            }
        }

        // 2026-11-16(보조 최대 4): 이번 메인 휘두름에서 살아 있는 보조 중 몇 마리가 따라치지 못했는지 UMM 로그 한 줄(진단용 — 전투 기록에는 남지 않는다).
        // 건너뛴 보조가 없으면 쓰지 않는다. 건수는 방금 채운 스냅샷에서 센다(판정을 다시 하지 않는다). 사유 이름 = ShadowRules.StrikeVerdict.
        static void LogSkips(UnitEntityData leader, ParentSnapshot snap)
        {
            int live = 0, struck = 0, outOfRange = 0, cannotAct = 0, targetDead = 0;
            foreach (var e in snap.Slots)
            {
                if (e.Status != ShadowRules.SlotStatus.Live) continue;
                if (e.Decision == null) throw new InvalidOperationException("live slot " + e.Slot + " has no strike decision");
                live++;
                switch (e.Decision.Verdict)
                {
                    case ShadowRules.StrikeVerdict.Strike: struck++; break;
                    case ShadowRules.StrikeVerdict.OutOfRange: outOfRange++; break;
                    case ShadowRules.StrikeVerdict.AuxCannotAct: cannotAct++; break;
                    case ShadowRules.StrikeVerdict.TargetDead: targetDead++; break;
                    default: throw new InvalidOperationException("slot " + e.Slot + " strike decision is " + e.Decision.Verdict);
                }
            }
            if (struck == live) return;
            Main.Log(SkipLogHead + leader.CharacterName + ": " + struck + " of " + live + " auxiliaries struck (skipped: out of range " + outOfRange + ", cannot act " + cannotAct + ", target already dead " + targetDead + ")");
        }

        public const string SkipLogHead = "PackWarden: shadow strike for ";

        // 보조 1마리의 이 타. 어느 판정이든 Decision 을 돌려준다(점검이 기대 보조 수와 대조).
        static Decision Strike(UnitEntityData leader, UnitEntityData pet, UnitEntityData aux, RuleAttackWithWeapon evt, WeaponSlot mainSlot, Kingmaker.EntitySystem.EntityFact callFact)
        {
            if ((object)mainSlot == null) throw new ArgumentNullException("mainSlot", "PackWarden: Strike without a resolved main slot");
            var target = evt.Target;
            int hpBefore = target.HPLeft;
            bool targetAlive = ShadowRules.TargetAlive(target.State.IsDead, hpBefore);
            if (aux.State == null) throw new InvalidOperationException("auxiliary " + aux.CharacterName + " has no State");
            bool canAct = aux.State.CanAct;

            // 팔다리(D5 (다)): 메인 슬롯(규칙 슬롯 또는 펫 몸에서 찾은 슬롯)의 역할·색인 → 보조의 무기 있는 슬롯 압축 목록에서 형식 대응.
            var pb = pet.Body;
            if (pb == null) throw new InvalidOperationException("main companion has no Body");
            int limbIdx = -1;
            if (pb.AdditionalLimbs != null)
                for (int k = 0; k < pb.AdditionalLimbs.Count; k++) if (ReferenceEquals(pb.AdditionalLimbs[k], mainSlot)) { limbIdx = k; break; }
            // 역할·색인 = 순수 ShadowRules.MainLimb(실행 표, T3 검토 R11). 몸에 없는 슬롯은 거기서 던진다.
            var ml = ShadowRules.MainLimb(ReferenceEquals(mainSlot, pb.PrimaryHand), ReferenceEquals(mainSlot, pb.SecondaryHand), limbIdx);
            var role = ml.Role;
            int idx = ml.Index;
            var mainType = evt.Weapon.Blueprint.Type;
            if (mainType == null) throw new InvalidOperationException("main weapon " + evt.Weapon.Blueprint.name + " has no weapon type");
            var ab = aux.Body;
            if (ab == null) throw new InvalidOperationException("auxiliary " + aux.CharacterName + " has no Body");
            var descs = new List<ShadowRules.LimbDesc>();
            var slots = new List<WeaponSlot>();
            // 빈 슬롯 = HasWeapon == false(WeaponSlot.get_MaybeWeapon 은 빈 손에 EmptyHandWeapon 을 준다 — SP-08) → 목록에서 빼고 압축.
            if (ab.PrimaryHand != null && ab.PrimaryHand.HasWeapon) { descs.Add(new ShadowRules.LimbDesc(ShadowRules.LimbRole.Primary, 0, TypeGuid(ab.PrimaryHand))); slots.Add(ab.PrimaryHand); }
            if (ab.SecondaryHand != null && ab.SecondaryHand.HasWeapon) { descs.Add(new ShadowRules.LimbDesc(ShadowRules.LimbRole.Secondary, 0, TypeGuid(ab.SecondaryHand))); slots.Add(ab.SecondaryHand); }
            if (ab.AdditionalLimbs != null)
                for (int k = 0; k < ab.AdditionalLimbs.Count; k++)
                {
                    var l = ab.AdditionalLimbs[k];
                    if (l != null && l.HasWeapon) { descs.Add(new ShadowRules.LimbDesc(ShadowRules.LimbRole.Limb, k, TypeGuid(l))); slots.Add(l); }
                }
            int pos = ShadowRules.MapLimb(role, idx, mainType.AssetGuid.ToString(), descs);
            var auxSlot = slots[pos];
            var auxWeapon = auxSlot.Weapon;

            // 사정거리: 살아 있는 유닛의 지금 값(위치·몸 반경·무기 사거리 — 지네 업그레이드 ChangeUnitSize 같은 크기 변화 반영).
            // 거리 = UnitAttack.GetAttackDistance(private static) 와 같은 식: (기수면 탈것) DistanceTo(대상). 반경 = 공개 UnitAttack.GetApproachRadius(무기, 보조, 대상).
            var from = UnitHelper.GetSaddledUnit(aux) ?? aux;
            float dist = from.DistanceTo(target);
            float radius = UnitAttack.GetApproachRadius(auxWeapon, aux, target);
            bool inRange = ShadowRules.InRange(dist, radius);

            var d = new Decision
            {
                Aux = aux, TargetHpBefore = hpBefore, InRange = inRange, CanAct = canAct,
                WeaponTypeGuid = mainType.AssetGuid.ToString(), LimbPos = pos,
                Verdict = ShadowRules.StrikeDecision(targetAlive, canAct, inRange),
            };
            switch (d.Verdict)
            {
                case ShadowRules.StrikeVerdict.Strike: break;
                case ShadowRules.StrikeVerdict.TargetDead: SkipDeadTarget++; return d;
                case ShadowRules.StrikeVerdict.AuxCannotAct: SkipCannotAct++; return d;
                case ShadowRules.StrikeVerdict.OutOfRange: SkipOutOfRange++; return d;
                default: throw new InvalidOperationException("StrikeDecision returned " + d.Verdict);
            }

            // 규칙 = ShadowRules.Spec(B3): Extra(재귀 차단 표식)·AoO 아님·복사 4(전력 공격·첫 공격·공격 번호·공격 수), 자동 명중·자동 치명 위협·돌격·찢기는 복사 안 함.
            var rule = new RuleAttackWithWeapon(aux, target, auxWeapon, ShadowRules.ShadowPenalty(evt.AttackBonusPenalty))
            {
                ExtraAttack = true,
                IsAttackOfOpportunity = false,
                IsFullAttack = evt.IsFullAttack,
                IsFirstAttack = evt.IsFirstAttack,
                AttackNumber = evt.AttackNumber,
                AttacksCount = evt.AttacksCount,
                WeaponSlot = auxSlot,
            };
            // 출처 = 이 컴포넌트가 붙은 지도자의 CallFeature 런타임 사실(RuleReason(EntityFact) 생성자 — RulebookEvent.set_Reason 공개).
            if (callFact == null) throw new InvalidOperationException("no CallFeature fact in the component context");
            rule.Reason = new RuleReason(callFact);
            Rulebook.Trigger(rule);
            StrikeCount++;
            Animate(leader, aux, auxSlot, target);
            return d;
        }


        static string TypeGuid(WeaponSlot s)
        {
            var w = s.Weapon;
            if (w == null || w.Blueprint == null) throw new InvalidOperationException("weapon slot with HasWeapon=true has no weapon blueprint");
            if (w.Blueprint.Type == null) throw new InvalidOperationException("weapon " + w.Blueprint.name + " has no weapon type");
            return w.Blueprint.Type.AssetGuid.ToString();
        }

        // 공격 동작: 본편 기회 공격 명령과 같은 공개 API(UnitAttackOfOpportunity.OnRun @0006~00BA). 규칙은 이미 굴렸다 — 동작은 보이기만 한다.
        static void Animate(UnitEntityData leader, UnitEntityData aux, WeaponSlot auxSlot, UnitEntityData target)
        {
            var view = aux.View;
            if (view == null) throw new InvalidOperationException("auxiliary " + aux.CharacterName + " has no view");
            var am = view.AnimationManager;
            if (am == null) throw new InvalidOperationException("auxiliary " + aux.CharacterName + " has no AnimationManager");
            // 엔진도 바쁜 유닛에는 기회 공격 동작을 건너뛴다(@002E CanRunIdleAction → ret). 정당한 무동작, 센다.
            if (!am.CanRunIdleAction()) { AnimBusyCount++; return; }
            var hand = new AttackHandInfo(auxSlot, 0);
            hand.CreateAnimationHandleForAttack(null, UnitHelper.GetSaddledUnit(aux) != null);
            var handle = hand.AnimationHandle;
            if (handle == null)
            {
                // 허용된 약한 경로 ②: 보조당 경고 1회.
                var ls = s_Leaders.GetValue(leader, _ => new LeaderState());
                if (ls.WarnedAnimAux.Add(aux.UniqueId))
                {
                    AnimWarnCount++;
                    LastWarning = "PackWarden: no attack animation handle for auxiliary " + aux.CharacterName + " (shadow strike rule still rolled)";
                    Main.Entry.Logger.Warning(LastWarning);
                }
                return;
            }
            aux.LookAt(target.Position);
            handle.SpeedScale = CombatController.IsInTurnBasedCombat() ? AnimSpeedTurnBased : AnimSpeedRealTime;
            am.ExecuteIfIdle(handle);
        }
    }
}
