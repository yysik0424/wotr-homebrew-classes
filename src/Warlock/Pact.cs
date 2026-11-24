using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Newtonsoft.Json;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace Warlock
{
     
    public static class Pact
    {
        public static BlueprintFeature Feature;
        // 빌드 시점 런타임 후견인 선택지(WitchPatronSelection.AllFeatures) 중 매핑표에 없는 것(설계 §4.3 「미매핑 후견인」·§6-11). Build 전 = null(판정 불가).
        public static string[] UnmappedAtBuild { get; private set; }

        public static void Build()
        {
            if (Feature != null) throw new InvalidOperationException("Warlock: Pact.Build() called twice");
            if (!Groups.Built || !Blasts.Built || !Summons.ParentsBuilt || !LevelFeatures.Built)
                throw new InvalidOperationException("Warlock: Pact.Build() before Groups/Blasts/Summons/LevelFeatures (BuildAll order)");
            var candidates = RuntimePatronSelection();
            UnmappedAtBuild = PatronGroups.Unmapped(candidates);
            if (UnmappedAtBuild.Length == 0) ModContext.Log("INFO Warlock: unmapped patrons 0 (WitchPatronSelection.AllFeatures " + candidates.Length + ")");
            else ModContext.LogError("Warlock: unmapped witch patrons [" + string.Join(", ", UnmappedAtBuild) + "] — a warlock with such a patron gets no group (design §4.3)");

            Feature = KitBuild.Feature("WarlockPactFeature", Guids.PactFeature, KitSpec.PactKey, KitBuild.Icon(Guids.Game.MagicMissile));
            if (Heat.Buff == null) throw new InvalidOperationException("Warlock: Pact.Build() before Heat.Build() (BuildAll order)");
            if (!Summons.Built) throw new InvalidOperationException("Warlock: Pact.Build() before Summons.Build() built the 54 variants (BuildAll order)");
            var comp = new WarlockPactComponent();
            var heat = new WarlockHeatComponent();  
            var summon = new WarlockSummonComponent();  
            AddComponents(Feature, comp, heat, summon);
            if (Feature.ComponentsArray.Length != 3 || !ReferenceEquals(Feature.ComponentsArray[0], comp) || !ReferenceEquals(Feature.ComponentsArray[1], heat) || !ReferenceEquals(Feature.ComponentsArray[2], summon) || Feature.ReapplyOnLevelUp)
                throw new InvalidOperationException("Warlock: WarlockPactFeature readback failed (exactly WarlockPactComponent + WarlockHeatComponent + WarlockSummonComponent, ReapplyOnLevelUp false)");
            Archetype.AddAt(1, Feature);
        }

        // 런타임 WitchPatronSelection.AllFeatures(다른 모드가 더한 후견인 포함 — F05). 해석 안 되는 항목 = 판정 불가 → 던진다.
        internal static string[] RuntimePatronSelection()
        {
            var sel = Get<BlueprintFeatureSelection>(Guids.Game.WitchPatronSelection);
            var all = sel.AllFeatures.ToArray();
            if (all.Any(f => f == null)) throw new InvalidOperationException("Warlock: WitchPatronSelection.AllFeatures has an unresolved entry");
            return all.Select(f => f.AssetGuid.ToString()).ToArray();
        }




    }

    // 로그 1회 집합과 엔진 콜백 안 실패 수(진단 전용 런타임 상태 — 저장 안 함). 콜백 안 예외는 엔진이 삼키므로(EntityFact.PostLoad @002F·EventBus) 잡아서 세고 ERROR 로 남긴다:
    // 점검 S15 `no-mod-errors` 가 ERROR 줄을, S3b 단계들이 Failures == 0 을 단언한다.
    public static class PactLog
    {
        static readonly OnceSet once = new OnceSet();
        public static int Failures { get; private set; }

        // 1회 집합의 세대 = 지금 불러온 게임의 Player 개체(세이브를 불러올 때마다 새것 — 리뷰 T3 m-7). 게임이 없으면 판정 불가 → 던진다(호출자 catch 가 센다).
        internal static object Generation()
        {
            var game = Kingmaker.Game.Instance;
            if (game == null || game.Player == null) throw new InvalidOperationException("Warlock: log once-set generation unknown (Game.Instance / Player is null)");
            return game.Player;
        }

        internal static void Emit(PactLine l)
        {
            if (l == null) throw new ArgumentNullException("l");
            if (l.OnceKey != null && !once.Add(Generation(), l.OnceKey)) return; // 정당한 무동작: 같은 키는 이 로드에서 1회만 기록(계획 「UnknownOnce」)
            switch (l.Level)
            {
                case PactLevel.Debug: ModContext.Log("DEBUG " + l.Text); break;
                case PactLevel.Info: ModContext.Log("INFO " + l.Text); break;
                case PactLevel.Warn: ModContext.Log("WARN " + l.Text); break;
                case PactLevel.Error: ModContext.LogError(l.Text); break;
                default: throw new ArgumentOutOfRangeException("l", l.Level, "Warlock: unknown pact log level");
            }
        }

        internal static void Failed(string where, UnitEntityData u, Exception ex)
        {
            Failures++;
            ModContext.LogError("Warlock: pact " + where + " failed for " + (u == null ? "(no unit)" : u.CharacterName + " (" + u.UniqueId + ")") + ": " + ex);
        }
    }

    // 「리스펙 검증 미룸」 상태(O3-1): RespecLoad 로 미룬 유닛 UniqueId(리스펙은 최종 원본의 UniqueId 를 유지 — RespecOnCommit @05B4 제거·@0614 치환).
    // 런타임 진단 상태(저장 안 함). 값의 판정은 PactRule.Route 만 한다. 집합 = 순수 PendingSet(세대 = 불러온 세이브 — 리뷰 T4 m-5).
    public static class PactRespec
    {
        static readonly PendingSet pending = new PendingSet();

        public static bool IsPending(string unit) { return pending.IsPending(PactLog.Generation(), unit); }

        internal static void Set(string unit, bool value) { pending.Set(PactLog.Generation(), unit, value); }
    }

     
    public class WarlockPactComponent : UnitFactComponentDelegate<WarlockPactComponent.PactData>, IUnitReapplyFeaturesOnLevelUpHandler, IUnitChangedAfterRespecHandler, IGlobalSubscriber
    {
        public class PactData
        {
            [JsonProperty] public string GrantedGroupGuid; // 부여한 그룹 특성 GUID(KitRule.GroupFeature)
            [JsonProperty] public string FirstPatronGuid;  // 그 그룹을 정한 첫 후견인 진행표 GUID
        }





        // L1 생성 중(선택 전)에도 불린다 → Activate 모드(NoPatron = 정상 대기).
        protected override void OnActivate()
        {
            if (!Owner.PreviewOf.IsEmpty) return; // 정당한 무동작: 미리보기 유닛(캐릭터 생성 상세 보기·레벨업 미리보기·리스펙 newUnit — 정찰 §5, 계획 Review Focus 1)
            EnsureAutoPart();
            Enter(PactEntry.Activate, false);
        }

         
        // EntityPartsManager.Ensure는 기존 부품을 돌려준다. 옛 세이브에 부품이 없으면 환불 권리 없는 기본 부품을 만든다.
        void EnsureAutoPart()
        {
            try { Owner.Ensure<UnitPartWarlockAuto>(); }
            catch (Exception ex) { PactLog.Failed("Ensure(UnitPartWarlockAuto)", Owner, ex); }
        }

        // 정적 하네스 AddClassLevels.LevelUp @02AE · UI LevelUpController.ApplyLevelUpActions @00D0 → ReapplyFeaturesOnLevelUp @0104(유닛 범위 사건, 무인자 — 정찰 §16-T3).
        public void HandleUnitReapplyFeaturesOnLevelUp()
        {
            if (!Owner.PreviewOf.IsEmpty) return; // 정당한 무동작: 미리보기 유닛
            Enter(PactEntry.LevelUp, false);
        }

        // 리스펙 최종 원본(UnitHelper.RespecOnCommit @0A5C — 인자 = targetUnit, <RespecOnCommit>b__8 @0002). 전역 사건이라 Owner 만. Final 조정 뒤 AfterRespec 검증(PactRule.Route).
        public void HandleUnitChangedAfterRespec(UnitEntityData unit)
        {
            if (!Owner.PreviewOf.IsEmpty) return; // 정당한 무동작: 미리보기 유닛
            if (unit != Owner) return; // 정당한 무동작: 다른 유닛의 리스펙(전역 구독)
            Enter(PactEntry.RespecEnd, false);
        }

        // 로드 = 검증만(사실 추가·제거 금지). 어긋나면 ERROR(PactRule.Plan VerifyOnly).
        // 리스펙 커밋 안의 PostLoad(엔진 신호 ContextData<UnitEntityData.Respec> — RespecOnCommit @06B6~@06CA, 본편 선례 UnitDescriptor.PostLoad @003F~0049)는
        // newUnit 의 기록·키트를 읽으므로 검증을 처리기 뒤로 미룬다(RespecLoad — O3-1, 정찰 §16-T4a).
        protected override void OnPostLoad()
        {
            if (!Owner.PreviewOf.IsEmpty) return; // 정당한 무동작: 미리보기 유닛
            EnsureAutoPart(); // 사실이 아니라 런타임 부품 — 「로드 = 검증만(사실 추가·제거 금지)」 규칙 밖(부품 이전 세이브 복구)
            Enter(PactEntry.PostLoad, ContextData<UnitEntityData.Respec>.Current);
        }

        // 진입점 → PactRule.Route(진입점, 리스펙 신호, 미룬 검증) → 줄 + 미룬 검증 상태 + Run(모드…). 엔진 콜백 안 예외는 삼켜지므로 잡아서 센다.
        void Enter(PactEntry entry, bool respecScope)
        {
            try
            {
                var unit = Owner.UniqueId;
                var route = PactRule.Route(entry, respecScope, PactRespec.IsPending(unit), unit);
                foreach (var line in route.Lines) PactLog.Emit(line);
                PactRespec.Set(unit, route.PendingAfter);
                foreach (var mode in route.Modes) Run(mode);
            }
            catch (Exception ex) { PactLog.Failed("Enter(" + entry + ")", Owner, ex); }
        }

        // 계약 사실이 빠지면(리스펙·제거) 54 도메인 보유분 전부 회수. 일시 OnTurnOff 에는 하지 않는다(F04).
        protected override void OnDeactivate()
        {
            if (!Owner.PreviewOf.IsEmpty) return; // 정당한 무동작: 미리보기 유닛
            try
            {
                Kit.RecallAll(Owner);
                Data.GrantedGroupGuid = null;
                Data.FirstPatronGuid = null;
            }
            catch (Exception ex) { PactLog.Failed("RecallAll", Owner, ex); }
        }

        void Run(PactMode mode)
        {
            try
            {
                var unit = Owner.UniqueId;
                var patrons = Kit.Patrons(Owner);
                var groups = Kit.HeldGroups(Owner);
                Group? recorded = Data.GrantedGroupGuid == null ? (Group?)null : PactRule.GroupOfFeature(Data.GrantedGroupGuid);
                var decision = PatronGroups.Decide(patrons, groups, Data.FirstPatronGuid, recorded);
                var plan = PactRule.Plan(decision, mode, unit);
                foreach (var line in plan.Lines) PactLog.Emit(line);
                if (plan.WriteRecord)
                {
                    Data.FirstPatronGuid = plan.RecordFirst;
                    Data.GrantedGroupGuid = KitRule.GroupFeature(plan.RecordGroup.Value);
                }
                if (plan.Kit == PactKit.Reconcile) Kit.Reconcile(Owner, plan.KitGroup.Value);
                else if (plan.Kit == PactKit.Verify) foreach (var line in PactRule.VerifyLines(unit, plan.KitGroup.Value, Kit.Verify(Owner, plan.KitGroup.Value), mode)) PactLog.Emit(line);
            }
            catch (Exception ex) { PactLog.Failed("Run(" + mode + ")", Owner, ex); }
        }
    }

     
    public static class Kit
    {
        static readonly HashSet<string> domain = new HashSet<string>(KitRule.PactOwnedGuids);

        static string GuidOf(Kingmaker.EntitySystem.EntityFact f)
        {
            var bp = f.Blueprint;
            if (bp == null) throw new InvalidOperationException("Warlock: unit fact without a blueprint");
            return bp.AssetGuid.ToString();
        }

        // 보유 사실 중 54 도메인(중복 보유는 KitRule.RequireDomain 이 던진다).
        public static string[] Owned(UnitEntityData u)
        {
            if (u == null) throw new ArgumentNullException("u");
            return u.Facts.List.Select(GuidOf).Where(domain.Contains).ToArray();
        }

        // 후견인 관측 도메인 = 런타임 WitchPatronSelection.AllFeatures ∪ PatronGroups.Map 키 ∪ {DarkPact}(F05).
        public static string[] Patrons(UnitEntityData u)
        {
            if (u == null) throw new ArgumentNullException("u");
            var dom = new HashSet<string>(Pact.RuntimePatronSelection());
            foreach (var t in PatronGroups.Map) dom.Add(t.Item1);
            dom.Add(PatronGroups.DarkPact);
            return u.Facts.List.Select(GuidOf).Where(dom.Contains).ToArray();
        }

        public static Group[] HeldGroups(UnitEntityData u)
        {
            if (u == null) throw new ArgumentNullException("u");
            return PatronGroups.AllGroups.Where(g => u.HasFact(Groups.Feature(g))).ToArray();
        }

        public static Group GroupOf(UnitEntityData u)
        {
            var g = HeldGroups(u);
            if (g.Length != 1) throw new InvalidOperationException("Warlock: " + u.CharacterName + " holds " + g.Length + " warlock groups, expected exactly 1");
            return g[0];
        }

        // 마커 사실 보유(Reach6·Double10·Ward1~4) — 규칙은 레벨 숫자가 아니라 이것을 읽는다.
        public static KitMarkers Markers(UnitEntityData u)
        {
            if (u == null) throw new ArgumentNullException("u");
            return new KitMarkers
            {
                Reach = u.HasFact(LevelFeatures.Marker(Guids.Reach6Feature)),
                Double = u.HasFact(LevelFeatures.Marker(Guids.Double10Feature)),
                WardTier = KitSpec.WardTierOf(KitSpec.TierMarkerGuids.Select(g => u.HasFact(LevelFeatures.Marker(g))).ToArray()),
            };
        }

        static BlueprintUnitFact Bp(string guid) { return Get<BlueprintUnitFact>(guid); }

        // 제거 전부 → 추가 전부 → 되읽기(Owned == Expected 집합). 레벨업마다 멱등.
        public static void Reconcile(UnitEntityData u, Group g)
        {
            var expected = KitRule.Expected(g, Markers(u));
            var plan = KitRule.Reconcile(expected, Owned(u));
            foreach (var r in plan.Remove) u.RemoveFact(Bp(r));
            foreach (var a in plan.Add) if (u.AddFact(Bp(a), null, null) == null) throw new InvalidOperationException("Warlock: AddFact(" + a + ") returned null for " + u.CharacterName);
            var back = Owned(u);
            if (back.Length != expected.Length || back.Except(expected).Any() || expected.Except(back).Any())
                throw new InvalidOperationException("Warlock: kit readback for " + u.CharacterName + " (" + g + "): owned [" + string.Join(", ", back) + "] != expected [" + string.Join(", ", expected) + "]");
        }

        // VerifyOnly: 차이만(사실 변경 없음).
        public static KitPlan Verify(UnitEntityData u, Group g)
        {
            return KitRule.Reconcile(KitRule.Expected(g, Markers(u)), Owned(u));
        }

        public static void RecallAll(UnitEntityData u)
        {
            var plan = KitRule.RecallAll(Owned(u));
            foreach (var r in plan.Remove) u.RemoveFact(Bp(r));
            var back = Owned(u);
            if (back.Length != 0) throw new InvalidOperationException("Warlock: RecallAll left " + back.Length + " pact facts on " + u.CharacterName + " [" + string.Join(", ", back) + "]");
        }
    }
}
