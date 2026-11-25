using System;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.Kineticist;
using WotrClassMod.Common;

namespace BlastMagus
{
    // 키네티시스트 part 충돌 검출(설계 §11-7, 정찰 §4 「충돌」). BlastFeature 의 컴포넌트 (3) — (2) AddKineticistPart 바로 뒤.
    // 엔진은 part 를 「먼저 붙은 쪽이 이김」으로 두고(AddKineticistPart.OnTurnOn IL 0019~0054), KineticArchetypes 설치 시 그 오류 로그조차 없다(정찰 §0-7).
    // 그래서 우리가 직접 본다: 유닛의 UnitPartKineticist.m_Settings 가 「이 특성의 AddKineticistPart 인스턴스」인가.
    //   - 비교 방식 = 참조 비교. UnitPartKineticist.Setup(AddKineticistPart) 는 인자를 그대로 m_Settings 에 저장하고(IL 0011~0013),
    //     AddKineticistPart.OnTurnOn/OnPostLoad 는 ldarg.0(= 블루프린트 컴포넌트 인스턴스 자신)을 넘긴다(IL 0060~0061, 0016~0017).
    //     UnitFactComponentDelegate 의 this 는 블루프린트 컴포넌트이므로 Fact.Blueprint.GetComponent<AddKineticistPart>() 와 같은 객체다(포장 없음 → GUID 비교 불필요).
    // 실행 순서(dnlib, EntityFact.Activate): 모든 컴포넌트 OnActivate(IL 0121) → TurnOn(IL 0190) → 각 컴포넌트 OnTurnOn(EntityFact.TurnOn IL 00D2, Components 순서).
    //   → 새로 얻을 때 OnActivate 시점에는 우리 part 가 아직 없다(정상). 이때 part 가 이미 있으면 남의 것(키네티시스트 먼저) → 충돌.
    //   → OnTurnOn 시점에는 (2) 가 part 를 만든 뒤다. 여기서 part 가 없으면 (2) 가 건너뛴 것(KA Prefix1 등) → 충돌.
    // 보강(컨트롤러 판정 5 의 목적 범위): 같은 유닛의 다른 사실이 AddKineticistPart 를 가지면(멀티클래스 BurnFeature) m_Settings 가 우연히 우리 것이어도 충돌로 본다
    //   — 로드 시 두 특성의 OnPostLoad 가 모두 Setup 을 불러 마지막 쪽이 m_Settings 를 차지하고(IL 0016~0017), 블라스트 메이거스 → 키네티시스트 순서에선
    //   상대 OnTurnOn 이 조용히 물러나 m_Settings 가 계속 우리 것이기 때문. 나중에 얻는 사실은 IUnitGainFactHandler(UnitLogicCollection.OnFactDidAttach IL 0029)로 다시 본다.
    public class BlastPartGuard : UnitFactComponentDelegate, IUnitGainFactHandler
    {
        static FieldInfo settingsField;

        protected override void OnActivate() { Check(false); }

        protected override void OnTurnOn() { Check(true); }

        public void HandleUnitGainFact(EntityFact fact)
        {
            if (fact == null) throw new ArgumentNullException("fact");
            if (ReferenceEquals(fact, Fact)) return; // legitimate no-op: our own fact attaching — OnActivate/OnTurnOn already checked it
            var bp = fact.Blueprint as BlueprintScriptableObject;
            if (bp == null || bp.GetComponent<AddKineticistPart>() == null) return; // legitimate no-op: the gained fact cannot create a kineticist part
            Check(true);
        }

        void Check(bool partMustExist)
        {
            var owner = Owner;
            if (owner == null) throw new InvalidOperationException("BlastMagus: BlastPartGuard without an owner");
            var mine = Fact.Blueprint.GetComponent<AddKineticistPart>();
            if (mine == null) throw new InvalidOperationException("BlastMagus: BlastPartGuard on " + Fact.Blueprint.name + " which has no AddKineticistPart (wiring error)");
            var part = owner.Get<UnitPartKineticist>();
            string reason = null;
            bool conflictingFact = false;
            if (part == null)
            {
                if (!partMustExist) return; // legitimate no-op: OnActivate runs before AddKineticistPart.OnTurnOn creates the part (EntityFact.Activate IL 0121 < 0190); OnTurnOn decides
                reason = "no UnitPartKineticist after AddKineticistPart.OnTurnOn (another mod skipped it?)";
            }
            else
            {
                var settings = SettingsOf(part);
                var other = owner.Facts.List.FirstOrDefault(f => !ReferenceEquals(f, Fact) && f.Blueprint is BlueprintScriptableObject b && b.GetComponent<AddKineticistPart>() != null);
                conflictingFact = other != null;
                if (!ReferenceEquals(settings, mine))
                    reason = "part settings come from " + (settings == null ? "null" : settings.OwnerBlueprint == null ? settings.GetType().Name : settings.OwnerBlueprint.name);
                else if (other != null)
                    reason = "another fact also adds a kineticist part: " + other.Blueprint.name;
            }
            if (reason == null)
            {
                BlastState.Enable(owner);
                return;
            }
            if (BlastState.Disable(owner, reason))
                {
                string text = "BlastMagus: UnitPartKineticist on " + owner.CharacterName + " is not ours (kineticist multiclass?) — blasts disabled (" + reason + ")";
                if (conflictingFact) ModContext.Log(text);
                else ModContext.LogError(text);
            }
        }

        static AddKineticistPart SettingsOf(UnitPartKineticist part)
        {
            if (settingsField == null)
            {
                settingsField = typeof(UnitPartKineticist).GetField("m_Settings", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (settingsField == null) throw new MissingFieldException("BlastMagus: UnitPartKineticist.m_Settings not found");
            }
            return (AddKineticistPart)settingsField.GetValue(part);
        }
    }

    // 유닛별 「블라스트 사용 가능」 상태(참조 동일성 약한 키 표, 인스턴스 필드 없음 → 세이브에 아무것도 안 들어간다; 로드 뒤엔 BlastPartGuard.OnTurnOn 이 다시 정한다).
    // 상태 3개: 알 수 없음 / 켜짐 / 꺼짐. 알 수 없음은 IsEnabled 를 통과하지 못하고 유닛당 1회 오류 로그를 남긴다(조용한 거짓 금지, C1).
    public static class BlastState
    {
        enum S { Unknown, Enabled, Disabled }

        sealed class Box
        {
            public S State;
            public string Reason;
            public bool UnknownLogged;
        }

        static readonly WeakKeyTable<UnitEntityData, Box> table = new WeakKeyTable<UnitEntityData, Box>();

        public static void Enable(UnitEntityData u)
        {
            if (u == null) throw new ArgumentNullException("u");
            var b = table.GetValue(u, _ => new Box());
            b.State = S.Enabled;
            b.Reason = null;
        }

        // 새로 꺼졌거나 이유가 바뀌었으면 true(호출자가 그때만 오류 로그 → OnActivate·OnTurnOn 이 같은 충돌로 두 번 찍지 않는다).
        public static bool Disable(UnitEntityData u, string reason)
        {
            if (u == null) throw new ArgumentNullException("u");
            if (string.IsNullOrEmpty(reason)) throw new ArgumentException("BlastMagus: BlastState.Disable needs a reason", "reason");
            var b = table.GetValue(u, _ => new Box());
            bool changed = b.State != S.Disabled || b.Reason != reason;
            b.State = S.Disabled;
            b.Reason = reason;
            return changed;
        }

        public static bool IsEnabled(UnitEntityData u)
        {
            if (u == null) throw new ArgumentNullException("u");
            var b = table.GetValue(u, _ => new Box());
            if (b.State == S.Unknown)
            {
                if (!b.UnknownLogged)
                {
                    b.UnknownLogged = true;
                    ModContext.LogError("BlastMagus: blast state of " + u.CharacterName + " is unknown (BlastPartGuard has not run) — treating blasts as disabled");
                }
                return false;
            }
            return b.State == S.Enabled;
        }

         
        // 발동 첫머리와 OnDeactivate 가 내린다. 소유자 키로 충분하다: Stacking=Replace 재시전은 옛 버프를 먼저 떼고(BuffCollection.PrepareFactForAttach
        // switch 0 → EntityFactsManager.Remove @0x0153) 새 버프를 붙이므로 내림 → 올림 순서가 지켜진다.
        // 알 수 없음(이 유닛에 대해 한 번도 정해지지 않음)은 통과하지 못하고 유닛당 1회 오류 로그(C1).
        sealed class PendingBox
        {
            public bool? Value;
            public bool UnknownLogged;
        }

        static readonly WeakKeyTable<UnitEntityData, PendingBox> pending = new WeakKeyTable<UnitEntityData, PendingBox>();

        public static void SetPending(UnitEntityData u, bool value)
        {
            if (u == null) throw new ArgumentNullException("u");
            pending.GetValue(u, _ => new PendingBox()).Value = value;
        }

        public static bool Pending(UnitEntityData u)
        {
            if (u == null) throw new ArgumentNullException("u");
            var b = pending.GetValue(u, _ => new PendingBox());
            if (b.Value == null)
            {
                if (!b.UnknownLogged)
                {
                    b.UnknownLogged = true;
                    ModContext.LogError("BlastMagus: blast strike pending state of " + u.CharacterName + " is unknown (BlastStrikeOnHit OnActivate/OnPostLoad has not run) — not firing");
                }
                return false;
            }
            return b.Value.Value;
        }

        // 꺼진 이유. 꺼지지 않은 유닛에 물으면 던진다(이유 없는 꺼짐·켜진 유닛의 이유는 없다).
        public static string Reason(UnitEntityData u)
        {
            if (u == null) throw new ArgumentNullException("u");
            Box b;
            if (!table.TryGetValue(u, out b) || b.State != S.Disabled)
                throw new InvalidOperationException("BlastMagus: BlastState.Reason for " + u.CharacterName + " which is " + (b == null ? "unknown" : b.State.ToString()) + ", not disabled");
            return b.Reason;
        }
    }
}
