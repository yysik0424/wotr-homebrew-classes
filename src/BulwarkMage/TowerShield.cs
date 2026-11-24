using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace BulwarkMage
{
     
    //   그대로 참조 6종 = BodyTable.TowerShieldGrants(폭발 차단 ×5 · 페널티 무효 · 접촉 방패 · 완전 엄폐 · 회피 · 향상된 회피) — 모두 클래스 참조 없음(정찰 §3).
    //   복제 2 = 타워실드 훈련 버프(본편 e8564788… 의 MaxDexBonusIncrease ×3 · ArmorCheckPenaltyIncrease ×2 를 얕은 복제로 같은 값 — AddFactContextActions 는 뺀다:
    //     그 Deactivated 가 ArmorTraining 사실을 지우고 1랭크로 다시 넣어 우리 갑옷 훈련 2~4랭크를 1로 떨어뜨린다, 정찰 §3 M3) +
    //     타워실드 훈련(본편 d2dbc71b… 의 HasShieldFeatureUnlock · ContextRankConfig · FactsChangeTrigger · SavesFixerRecalculate 얕은 복제, 참조 4곳 = 복제본) ×(3,7,11,15).
    // 복제 = 본편 객체의 얕은 복사(MemberwiseClone) 뒤 우리 새 객체의 참조 필드만 바꾼다(본편 객체에는 쓰지 않는다 — 시험 exec: base-blueprint writes).
    //   Element(GameAction)는 공유하지 않는다: 반복 행동·버프 부여 행동도 복제해 새 ActionList 에 담고 Element.Owner 를 우리 복제 특성으로 바꾼다
    //   (MemberwiseClone 은 Owner 를 본편 그대로 복사하고, 우리 블루프린트의 OnEnable 은 ElementsArray 에 없는 이 요소들의 Owner 를 바꾸지 않는다 — T3 검토 m-1. Owner 의 쓰임은 오류 로그 문구).
    //   하위 객체(ContextValue·ContextDurationValue·배열·리스트·참조 객체)는 Copy 가 CloneKit.DetachFields 로 새 사본으로 떼어낸다(T3 검토 m-1 — 공유하면 나중 코드가 본편을 바꿀 수 있다).
    //   공유로 남기는 것 = 블루프린트·Unity 객체·다른 컴포넌트·Element(KeepShared) — Element 는 위처럼 따로 복제한다. VerifyClones 가 CloneKit.Shared 로 공유 0 을 되읽는다.
    // 복제 특성에는 우리 부품 하나가 더 붙는다(2026-11-18): TowerTrainingRankSync — 레벨업 뒤 버프 랭크를 엔진의 「새로 얻을 때」 값으로 맞춘다(그 머리말).
    // 본편 원본에 우리가 복사하지 않는 형식의 컴포넌트가 더 있으면 복제본에는 없다 — 그 차이는 런타임 되읽기(ForeignTolerance 형식 개수 행)가 ERROR 로 드러낸다.
    public static class TowerShield
    {
        public static BlueprintBuff TrainingBuff;
        public static BlueprintFeature Training;

        static readonly MethodInfo MemberwiseCloneMethod = typeof(object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance);

        public static void Build()
        {
            if (TrainingBuff != null || Training != null) throw new InvalidOperationException("BulwarkMage: TowerShield.Build() called twice");
            var grants = BodyTable.TowerShieldGrants;
            if (grants == null || grants.Count == 0) throw new InvalidOperationException("BulwarkMage: BodyTable.TowerShieldGrants is empty");
            foreach (var g in grants) Archetype.AddAt(g.level, Get<BlueprintFeature>(g.guid));

            BuildTrainingBuff();
            BuildTraining();
            var levels = BulwarkRules.Levels.TowerTrainingLevels;
            if (levels == null || levels.Count == 0 || Training.Ranks < levels.Count) throw new InvalidOperationException("BulwarkMage: tower shield training levels empty or more than its Ranks");
            foreach (int l in levels) Archetype.AddAt(l, Training);
            VerifyClones();
        }

        // 공유로 남겨도 되는 하위 객체(소유·불변 객체): 블루프린트(OwnerBlueprint 등)·Unity 객체·다른 컴포넌트·Element(행동은 CloneGainedActions 가 따로 복제).
        internal static bool KeepShared(object v)
        {
            return v is SimpleBlueprint || v is UnityEngine.Object || v is BlueprintComponent || v is Element;
        }

        // 복제 = 얕은 복제 + 가변 하위 객체 떼어내기(T3 검토 m-1). 깊이 6 = ContextRankConfig 배열 → 참조 객체 → 필드까지의 여유(넘으면 CloneKit 이 던진다 — BuildFailed).
        internal static T Copy<T>(T src) where T : class
        {
            var c = ShallowCopy(src);
            CloneKit.DetachFields(c, KeepShared, 6);
            return c;
        }

        // 얕은 복제(MemberwiseClone) — 최상위 객체만 새것, 필드 값(하위 객체)은 원본과 공유(그래서 Copy 가 이어서 떼어낸다). 시험 WriteScan 원천 규칙이 이 메서드의 결과를 「최상위만 깨끗」으로 본다.
        internal static T ShallowCopy<T>(T src) where T : class
        {
            if (src == null) throw new ArgumentNullException("src");
            if (MemberwiseCloneMethod == null) throw new MissingMethodException("BulwarkMage: object.MemberwiseClone not found");
            var copy = MemberwiseCloneMethod.Invoke(src, null) as T;
            if (copy == null || ReferenceEquals(copy, src) || copy.GetType() != src.GetType()) throw new InvalidOperationException("BulwarkMage: MemberwiseClone of " + src.GetType().Name + " failed");
            return copy;
        }

        // 그 형식 컴포넌트를 정확히 n개(우리가 복사해 기대는 개수) — 아니면 빌드 실패. 다른 형식은 세지 않는다(「정확히 N개 컴포넌트」 금지 규칙).
        internal static T[] Exactly<T>(BlueprintScriptableObject bp, int n) where T : BlueprintComponent
        {
            var hits = (bp.ComponentsArray ?? new BlueprintComponent[0]).Where(c => c != null && c.GetType() == typeof(T)).Cast<T>().ToArray();
            if (hits.Length != n) throw new InvalidOperationException("BulwarkMage: " + bp.name + " has " + hits.Length + " x " + typeof(T).Name + ", expected " + n + " (vanilla layout changed?)");
            return hits;
        }

        static void CopyFactUi(BlueprintScriptableObject src, BlueprintScriptableObject dst)
        {
            SetField(dst, "m_DisplayName", GetF(src, "m_DisplayName"));
            SetField(dst, "m_Description", GetF(src, "m_Description"));
            SetField(dst, "m_DescriptionShort", GetF(src, "m_DescriptionShort")); // 2026-11-17: 짧은 설명도 본편 인스턴스(원본 jbp = 빈 키 — 시험 tooltip: 이 대조, null 이면 빌드 검사가 던진다)
            SetField(dst, "m_Icon", GetF(src, "m_Icon"));
        }

        static void BuildTrainingBuff()
        {
            var src = Get<BlueprintBuff>(Guids.Game.TowerShieldTrainingBuff);
            var dex = Exactly<MaxDexBonusIncrease>(src, 3);
            var acp = Exactly<ArmorCheckPenaltyIncrease>(src, 2);
            Exactly<AddFactContextActions>(src, 1); // 빼는 대상이 실제로 하나 있다(없으면 원본이 바뀐 것 — 복제 이유가 달라진다)
            TrainingBuff = Create<BlueprintBuff>("BulwarkTowerShieldTrainingBuff", Guids.TowerShieldTrainingBuff, b =>
            {
                CopyFactUi(src, b);
                SetField(b, "m_Flags", GetF(src, "m_Flags"));
                b.Stacking = src.Stacking;
                b.Ranks = src.Ranks;
                b.IsClassFeature = src.IsClassFeature;
                b.Frequency = src.Frequency;
                b.TickEachSecond = src.TickEachSecond;
                // 2026-11-17(GameLogFull: 이 훈련 버프 제거 → Buff.OnRemove @00B0 NRE, S15 후보): 원본 FX 칸은 복사하지 않으므로(FxOnStart 도 없음) 빈 FxOnRemove(BlueprintUtil.EmptyFxOnRemove 머리말).
                EmptyFxOnRemove(b);
            });
            // 원본 순서 그대로(MaxDex 3 → ACP 2 — 본편 배열에서 AddFactContextActions 만 뺀 순서; 시험이 형식 순서를 잠근다).
            var kept = src.ComponentsArray.Where(c => c is MaxDexBonusIncrease || c is ArmorCheckPenaltyIncrease).Select(c => (BlueprintComponent)Copy(c)).ToArray();
            if (kept.Length != dex.Length + acp.Length) throw new InvalidOperationException("BulwarkMage: tower shield training buff copy count " + kept.Length);
            AddComponents(TrainingBuff, kept);
        }

        static void BuildTraining()
        {
            var src = Get<BlueprintFeature>(Guids.Game.TowerShieldTraining);
            var hsfu = Copy(Exactly<HasShieldFeatureUnlock>(src, 1)[0]);
            var crc = Copy(Exactly<ContextRankConfig>(src, 1)[0]);
            var fctSrc = Exactly<FactsChangeTrigger>(src, 1)[0];
            var fct = Copy(fctSrc);
            var sfr = Copy(Exactly<SavesFixerRecalculate>(src, 1)[0]);
            Training = Create<BlueprintFeature>("BulwarkTowerShieldTraining", Guids.TowerShieldTraining, f =>
            {
                CopyFactUi(src, f);
                f.IsClassFeature = src.IsClassFeature;
                f.Ranks = src.Ranks;
                f.Groups = new FeatureGroup[0];
                f.ReapplyOnLevelUp = src.ReapplyOnLevelUp;
            });
             
            SetField(hsfu, "m_NewFact", Ref<BlueprintUnitFactReference>(Guids.TowerShieldTrainingBuff));
            SetField(crc, "m_Feature", Ref<BlueprintFeatureReference>(Guids.TowerShieldTraining));
            SetField(fct, "m_CheckedFacts", new[] { Ref<BlueprintUnitFactReference>(Guids.TowerShieldTrainingBuff) });
            fct.OnFactGainedActions = new ActionList { Actions = CloneGainedActions(fctSrc.OnFactGainedActions) };
            var lost = fctSrc.OnFactLostActions == null ? null : fctSrc.OnFactLostActions.Actions;
            if (lost != null && lost.Length != 0) throw new InvalidOperationException("BulwarkMage: vanilla TowerShieldTraining OnFactLostActions is not empty (" + lost.Length + ") - the clone would need to copy unknown actions");
            fct.OnFactLostActions = new ActionList { Actions = new GameAction[0] };
            // 2026-11-18: 레벨업 뒤 버프 랭크 맞춤 부품(TowerTrainingRankSync 머리말 — Feature.AddRank 의 껐다 켜기가 버프를 1랭크로 되돌린다). 복사 4종 뒤 마지막 칸.
            AddComponents(Training, hsfu, crc, fct, sfr, new TowerTrainingRankSync());
        }

        // 본편: [ContextActionRepeatedActions{Actions = [ContextActionApplyBuff{TowerShieldTrainingBuff}], Value = 랭크}](jbp 직독). 모양이 다르면 빌드 실패.
        static GameAction[] CloneGainedActions(ActionList src)
        {
            var acts = src == null ? null : src.Actions;
            if (acts == null || acts.Length != 1 || !(acts[0] is ContextActionRepeatedActions)) throw new InvalidOperationException("BulwarkMage: vanilla TowerShieldTraining OnFactGainedActions is not [ContextActionRepeatedActions] (" + (acts == null ? "null" : acts.Length + " action(s)") + ")");
            var rep = Copy((ContextActionRepeatedActions)acts[0]);
            var inner = rep.Actions == null ? null : rep.Actions.Actions;
            if (inner == null || inner.Length != 1 || !(inner[0] is ContextActionApplyBuff)) throw new InvalidOperationException("BulwarkMage: vanilla TowerShieldTraining repeated actions are not [ContextActionApplyBuff]");
            var apply = Copy((ContextActionApplyBuff)inner[0]);
            SetField(apply, "m_Buff", Ref<BlueprintBuffReference>(Guids.TowerShieldTrainingBuff));
            rep.Actions = new ActionList { Actions = new GameAction[] { apply } };
            // Element.Owner = 우리 복제 특성(본편 TowerShieldTraining 그대로 남기지 않는다 — T3 검토 m-1).
            rep.Owner = Training;
            apply.Owner = Training;
            return new GameAction[] { rep };
        }

        // 복제 되읽기(빌드 시 — 틀리면 BuildFailed): 값 = 원본과 같음, 참조 = 우리 복제본, 버프에 AddFactContextActions 없음, 본편 버프·훈련을 가리키는 참조 없음.
        static void VerifyClones()
        {
            var srcBuff = Get<BlueprintBuff>(Guids.Game.TowerShieldTrainingBuff);
            var a = TrainingBuff.ComponentsArray;
            var srcKept = srcBuff.ComponentsArray.Where(c => c is MaxDexBonusIncrease || c is ArmorCheckPenaltyIncrease).ToArray();
            if (a.Length != srcKept.Length || a.Any(c => c is AddFactContextActions)) throw new InvalidOperationException("BulwarkMage: tower shield training buff clone has " + a.Length + " component(s) / an AddFactContextActions");
            for (int i = 0; i < a.Length; i++)
            {
                if (ReferenceEquals(a[i], srcKept[i]) || a[i].GetType() != srcKept[i].GetType() || !ReferenceEquals(a[i].OwnerBlueprint, TrainingBuff)) throw new InvalidOperationException("BulwarkMage: training buff clone component " + i + " is shared / of another type / not owned");
                var d = a[i] as MaxDexBonusIncrease;
                if (d != null)
                {
                    var s = (MaxDexBonusIncrease)srcKept[i];
                    if (d.Bonus != s.Bonus || d.BonesPerRank != s.BonesPerRank || d.CheckCategory != s.CheckCategory || d.Category != s.Category || d.UseContextInstead != s.UseContextInstead || d.ReplaceContextIfGreater != s.ReplaceContextIfGreater)
                        throw new InvalidOperationException("BulwarkMage: training buff clone MaxDexBonusIncrease " + i + " values differ from vanilla");
                    continue;
                }
                var p = (ArmorCheckPenaltyIncrease)a[i];
                var q = (ArmorCheckPenaltyIncrease)srcKept[i];
                if (p.Bonus == null || q.Bonus == null || p.Bonus.ValueType != q.Bonus.ValueType || p.Bonus.Value != q.Bonus.Value || p.BonesPerRank != q.BonesPerRank || p.CheckCategory != q.CheckCategory || p.Category != q.Category)
                    throw new InvalidOperationException("BulwarkMage: training buff clone ArmorCheckPenaltyIncrease " + i + " values differ from vanilla");
            }
            if (TrainingBuff.Stacking != srcBuff.Stacking || TrainingBuff.Ranks != srcBuff.Ranks) throw new InvalidOperationException("BulwarkMage: training buff clone stacking/ranks readback failed");

            var t = Training.ComponentsArray;
            var hsfu = t.OfType<HasShieldFeatureUnlock>().Single();
            var crc = t.OfType<ContextRankConfig>().Single();
            var fct = t.OfType<FactsChangeTrigger>().Single();
            if (t.Length != 5 || t.OfType<SavesFixerRecalculate>().Count() != 1 || t.OfType<TowerTrainingRankSync>().Count() != 1 || !(t[t.Length - 1] is TowerTrainingRankSync))
                throw new InvalidOperationException("BulwarkMage: tower shield training clone has " + t.Length + " component(s), expected the 4 copied types + TowerTrainingRankSync last");
            var refs = new List<string>
            {
                ((BlueprintReferenceBase)GetF(hsfu, "m_NewFact")).Guid.ToString(),
                ((BlueprintReferenceBase)GetF(crc, "m_Feature")).Guid.ToString(),
            };
            var checkedFacts = (BlueprintUnitFactReference[])GetF(fct, "m_CheckedFacts");
            if (checkedFacts == null || checkedFacts.Length != 1) throw new InvalidOperationException("BulwarkMage: training clone m_CheckedFacts is not a single reference");
            refs.Add(checkedFacts[0].Guid.ToString());
            var rep = fct.OnFactGainedActions.Actions.Single() as ContextActionRepeatedActions;
            var apply = rep == null ? null : rep.Actions.Actions.Single() as ContextActionApplyBuff;
            if (apply == null) throw new InvalidOperationException("BulwarkMage: training clone gained actions are not [repeat [apply buff]]");
            refs.Add(((BlueprintReferenceBase)GetF(apply, "m_Buff")).Guid.ToString());
            var want = new[] { Guids.TowerShieldTrainingBuff, Guids.TowerShieldTraining, Guids.TowerShieldTrainingBuff, Guids.TowerShieldTrainingBuff };
            if (!refs.SequenceEqual(want)) throw new InvalidOperationException("BulwarkMage: training clone references [" + string.Join(",", refs.ToArray()) + "], expected [" + string.Join(",", want) + "]");
            var srcFct = Get<BlueprintFeature>(Guids.Game.TowerShieldTraining).ComponentsArray.OfType<FactsChangeTrigger>().Single();
            var srcRep = (ContextActionRepeatedActions)srcFct.OnFactGainedActions.Actions[0];
            if (ReferenceEquals(fct.OnFactGainedActions, srcFct.OnFactGainedActions) || ReferenceEquals(rep, srcRep) || ReferenceEquals(rep.Actions, srcRep.Actions) || ReferenceEquals(apply, srcRep.Actions.Actions[0]))
                throw new InvalidOperationException("BulwarkMage: training clone shares a vanilla action list / action object");
            if (ReferenceEquals(fct.OnFactLostActions, srcFct.OnFactLostActions)) throw new InvalidOperationException("BulwarkMage: training clone shares the vanilla OnFactLostActions list");
            if (!ReferenceEquals(rep.Owner, Training) || !ReferenceEquals(apply.Owner, Training)) throw new InvalidOperationException("BulwarkMage: cloned training actions still name another owner blueprint");
            // 가변 하위 객체 공유 0(T3 검토 m-1): 복사한 컴포넌트 6 + 행동 2, 각각 본편 짝과.
            var pairs = new List<Tuple<object, object, string>>();
            for (int i = 0; i < a.Length; i++) pairs.Add(Tuple.Create((object)a[i], (object)srcKept[i], "training buff component " + i));
            var srcT = Get<BlueprintFeature>(Guids.Game.TowerShieldTraining).ComponentsArray;
            pairs.Add(Tuple.Create((object)hsfu, (object)srcT.OfType<HasShieldFeatureUnlock>().Single(), "HasShieldFeatureUnlock"));
            pairs.Add(Tuple.Create((object)crc, (object)srcT.OfType<ContextRankConfig>().Single(), "ContextRankConfig"));
            pairs.Add(Tuple.Create((object)fct, (object)srcFct, "FactsChangeTrigger"));
            pairs.Add(Tuple.Create((object)t.OfType<SavesFixerRecalculate>().Single(), (object)srcT.OfType<SavesFixerRecalculate>().Single(), "SavesFixerRecalculate"));
            pairs.Add(Tuple.Create((object)rep, (object)srcRep, "repeated actions"));
            pairs.Add(Tuple.Create((object)apply, (object)srcRep.Actions.Actions[0], "apply buff"));
            foreach (var p in pairs)
            {
                var shared = CloneKit.Shared(p.Item1, p.Item2, KeepShared, 6);
                if (shared.Count != 0) throw new InvalidOperationException("BulwarkMage: " + p.Item3 + " clone shares mutable sub-objects with vanilla: " + string.Join(", ", shared.ToArray()));
            }
        }

        static object GetF(object o, string name)
        {
            if (o == null) throw new ArgumentNullException("o");
            var f = FindField(o.GetType(), name);
            if (f == null) throw new MissingFieldException("BulwarkMage: " + o.GetType().Name + "." + name + " not found");
            return f.GetValue(o);
        }
    }
}
