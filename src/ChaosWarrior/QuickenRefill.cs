using System;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Buffs;
using WotrClassMod.Common;

namespace ChaosWarrior
{
    // 불러오기 뒤 신속 시전 권리 재충전(독립 검토 R4-F2 소유자 결정 2026-11-17 「본편처럼 다시 채움」). 판단은 순수 RageOffRule.ShouldRefillQuicken · MutationCore.RefillQuickenRight.
    //   본편: 그레이터 블러드레이지의 BuffExtraEffects(특성에 달림)가 OnApplyPostLoadFixes · OnActivate 때 CheckAndReapply @0000~0073 으로 권리를 다시 붙인다(Assembly-CSharp IL 직독 2026-11-17, %TEMP%\cw-r4\bee.txt).
    //   그 부품을 그대로 쓰지 못하는 이유(같은 덤프): 분노 「추가」 때도 HandleBuffDidAdded @0000~00DA 가 권리를 붙여 변이 추첨의 부여와 겹치고(분노당 2개), 붙인 권리가 분노의 자식이 아니라 고아 정리 · 분노 종료 대조와 어긋나며,
    //   레벨별 상한 6개를 고르려면 특성 6개 · 예외 사실 사슬이 필요하다. 새 Harmony 패치는 쓰지 않는다(계획 §12-4) — 기존 프레임 감시기(RageOffWatcher)의 파티 순회에 얹었다.
    //   「불러온 분노」의 판별 = 이번 실행에서 처음 보는 분노 버프 인스턴스: 세이브를 불러오면 버프 객체가 새로 만들어진다. 분노를 켤 때 · 억제 뒤 재활성 · 레벨업 재적용은
    //   변이 추첨 행동(ContextActionChaosMutations)이 그 인스턴스를 먼저 적는다(MarkSeen). 특성 재적용(UnitProgressionData.ReapplyFeaturesOnLevelUp @0098~00C8)은 켜진 활성 능력마다
    //   ActivatableAbility.ReapplyBuff @0011~0101 로 분노 버프를 지우고 새로 붙인다 → 굴림 · 권리 부여가 처음부터 다시 돈다(본편의 OnActivate 재충전과 같은 결과 — 여기서 따로 하지 않는다).
    //   시점 = 로딩이 끝난 첫 감시 프레임(본편은 로딩 안). 파티 · 동료 · 펫만 본다(감시기의 순회 범위 — 파티 밖 유닛은 채우지 않는다, README 한계).
    public static class QuickenRefill
    {
        // 이번 실행에서 본 분노 버프 인스턴스(약한 참조 — 지워진 버프를 붙잡지 않는다).
        static readonly WeakKeyTable<Buff, object> seen = new WeakKeyTable<Buff, object>();
        static readonly object Mark = new object();

        public static bool Seen(Buff rage)
        {
            if (rage == null) throw new ArgumentNullException("rage");
            object o;
            return seen.TryGetValue(rage, out o);
        }

        // 변이 추첨 행동이 부른다(문맥의 부모 버프). 부모가 없거나 우리 분노가 아니면 적지 않는다(그 판정은 MutationCore 가 NoParent 로 보고한다).
        internal static void MarkSeen(Buff.Data data)
        {
            var b = data == null ? null : data.Buff;
            if (b == null || b.Blueprint != Rage.Buff) return;
            Add(b);
        }

        static void Add(Buff b)
        {
            object o;
            if (!seen.TryGetValue(b, out o)) seen.Add(b, Mark);
        }

        // 감시기(RageOffWatcher.StopTurnedOff)가 판정 참일 때만 부른다. 먼저 적고(실패해도 매 프레임 다시 시도하지 않는다 — 보고는 1번) 핵심을 돌린다.
        internal static void RefillNow(UnitEntityData unit, Buff rage)
        {
            if (unit == null) throw new ArgumentNullException("unit");
            if (rage == null) throw new ArgumentNullException("rage");
            Add(rage);
            // 결과로 분기하지 않는다(판단은 핵심 — 실패는 핵심이 보고했다).
            MutationCore.RefillQuickenRight(new GameMutationWorld(unit, rage), ChaosErrors.Report, (site, what) => ModContext.Log("ChaosWarrior: " + what + " (" + unit.CharacterName + ")"));
        }
    }
}
