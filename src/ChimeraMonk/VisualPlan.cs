using System;
using System.Collections.Generic;
using System.Linq;

namespace ChimeraMonk
{
     
    //  - 꼬리: 본편 묶음 KEE_Tail_TailMythicDragon_Any 의 단일 에셋(종족·성별 공용)을 본편 AddEquipmentEntity 로 꼬리 변이 특성에. (추가 꼬리 변이는 2026-11-17 폐지 — 외형 행이 없었고, 폐지 특성에도 붙이지 않는다.)
    //  - 뿔: 본편 묶음 KEE_Horns_HornsMythicDevilSmall(종족 × 성별)을 자체 컴포넌트 ChimeraBundleVisualComponent 로 뿔 변이 특성에.
    //  - 날개: 행 없음(본편 BuffWingsMutagen 이 이미 외형을 낸다 — 정찰 1절).
    // 조용한 실패 방지(C1·C2): 엔진은 에셋 null·빈 종족 칸에서 그냥 지나간다 → 빌드 시 Problems 가 묶음 모양을 검사하고(비어 있지 않으면 빌더가 던짐),
    //  런타임 Pick 은 「칸 없음」을 명시적 NoVisual(사유 포함)로 돌려준다. NoVisual 은 「판정 불가」가 아니라 「외형 없음」 상태다(변이의 규칙 효과는 그대로, WARN 1회).
    public enum VisualKind { SingleAsset, RaceBundle }

    public sealed class VisualRow
    {
        public readonly string Mutation, BundleGuid, BundleName, AssetId; // AssetId = SingleAsset 행의 기대 에셋(RaceBundle 은 null)
        public readonly VisualKind Kind;
        public VisualRow(string mutation, VisualKind kind, string bundleGuid, string bundleName, string assetId)
        {
            if (string.IsNullOrEmpty(mutation) || string.IsNullOrEmpty(bundleGuid) || string.IsNullOrEmpty(bundleName)) throw new ArgumentException("ChimeraMonk: visual row without mutation / bundle guid / bundle name");
            if ((kind == VisualKind.SingleAsset) != (assetId != null)) throw new ArgumentException("ChimeraMonk: visual row " + mutation + ": SingleAsset needs an expected asset id, RaceBundle must not have one");
            Mutation = mutation; Kind = kind; BundleGuid = bundleGuid; BundleName = bundleName; AssetId = assetId;
        }
    }

    // 묶음(KingmakerEquipmentEntity)의 모양 — 에셋 id 문자열만(게임 쪽 Visuals.ShapeOf 가 블루프린트에서 옮긴다, 시험은 blueprints.zip jbp 에서 옮긴다).
    public sealed class BundleSlot
    {
        public readonly IReadOnlyList<string> Male, Female;
        public BundleSlot(IReadOnlyList<string> male, IReadOnlyList<string> female)
        {
            if (male == null || female == null) throw new ArgumentNullException(male == null ? "male" : "female", "ChimeraMonk: bundle slot with a null gender list (use an empty list)");
            Male = male; Female = female;
        }
    }

    public sealed class BundleShape
    {
        public readonly bool RaceDependent;
        public readonly IReadOnlyList<string> Male, Female;   // 종족 무관 배열(m_MaleArray / m_FemaleArray)
        public readonly IReadOnlyList<BundleSlot> Races;      // m_RaceDependentArrays — 색인 = 엔진 Race 번호, 원소 null = 그 칸 자체가 없음
        public BundleShape(bool raceDependent, IReadOnlyList<string> male, IReadOnlyList<string> female, IReadOnlyList<BundleSlot> races)
        {
            if (male == null || female == null || races == null) throw new ArgumentNullException("male/female/races", "ChimeraMonk: bundle shape with a null list (use an empty list)");
            RaceDependent = raceDependent; Male = male; Female = female; Races = races;
        }
    }

    public enum VisualState { Attach, NoVisual }

    public struct VisualPick
    {
        public readonly VisualState State;
        public readonly IReadOnlyList<string> Assets; // Attach: 1개 이상 / NoVisual: 빈 목록
        public readonly string Reason;                // NoVisual 사유(로그용) / Attach: null
        public VisualPick(VisualState state, IReadOnlyList<string> assets, string reason) { State = state; Assets = assets; Reason = reason; }
    }

    // 같은 키의 경고를 한 번만 내기 위한 장부(뿔 외형 「칸 없음」 WARN — 뷰가 다시 붙을 때마다 되풀이하지 않는다).
    public sealed class WarnOnce
    {
        readonly HashSet<string> seen = new HashSet<string>();
        public bool First(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("ChimeraMonk: WarnOnce without a key", "key");
            return seen.Add(key);
        }
    }

    // 수명 주기 판단(본편 AddEquipmentEntity OnTurnOn/OnTurnOff 의 가드와 같은 순서): 붙임 / 뗌 / 건너뜀(사유).
    public enum VisualAction { Attach, Detach, Skip }

    public struct LifecycleDecision
    {
        public readonly VisualAction Action;
        public readonly string Reason; // Skip 사유 / Attach·Detach: null
        public readonly bool Report;   // 참 = 건너뛰되 오류로 보고(판정 불가). Skip 에만 있다.
        public LifecycleDecision(VisualAction action, string reason, bool report)
        {
            if ((action == VisualAction.Skip) == string.IsNullOrEmpty(reason)) throw new ArgumentException("ChimeraMonk: a visual Skip needs a reason, Attach / Detach must not have one", "reason");
            if (report && action != VisualAction.Skip) throw new ArgumentException("ChimeraMonk: only a Skip can be reported", "report");
            Action = action; Reason = reason; Report = report;
        }
    }

    // 주인 유닛의 펫 여부(소유자 결정 2026-11-16: 키메라 몽크 레벨을 가진 유닛이 동물 동료(펫)이면 꼬리·뿔 외형을 붙이지 않는다).
    //  Unknown = 엔진 값을 읽지 못했거나 서로 어긋남 — 「펫 아님」으로 읽지 않는다(C1).
    public enum PetState { NotPet, Pet, Unknown }

    public static class VisualPlan
    {
        public const string Site = "visual"; // 오류 보고 위치 이름(ChimeraErrors.Report* 의 where)

        // 엔진 Kingmaker.EntitySystem.Persistence.SaveManager.State: None 0 · Saving 1 · Loading 2(dnlib 2026-11-16 직독).
        // 본편 AddEquipmentEntity.OnTurnOn/OnTurnOff IL @0012~0018 은 CurrentState 를 1(= Saving)과 비교해 같으면 건너뛴다. 시험이 엔진 열거형 값과 대조한다.
        public const int SaveStateSaving = 1;
        // 엔진 Kingmaker.Blueprints.Gender: Male 0 · Female 1(dnlib 2026-11-16 직독). 시험이 엔진 열거형 값과 대조한다.
        public const int GenderMale = 0, GenderFemale = 1;

        // 펫 건너뜀 사유(소유자 결정 2026-11-16). 판정 불가는 보고한다(Report).
        public const string ReasonPet = "Pet", ReasonPetUnknown = "PetUnknown";

        // [순수] 수명 주기 판단. 컴포넌트는 엔진 값을 읽어 넘기고 결과만 실행한다(Run).
        //  재적용 중 → 건너뜀 · 저장 중 → 건너뜀 · 뷰 없음 → 건너뜀(붙을 때 HandleUnitViewAttached 가 다시 부른다) · 아바타 없음(변신 중) → 건너뜀(여기까지 본편 가드 순서)
        //  · 끔 = 뗌(펫이어도 — 남는 경로를 만들지 않는다) · 켬: 펫 → 건너뜀(Pet) · 판정 불가 → 건너뜀 + 보고(PetUnknown) · 펫 아님 → 붙임.
        //  펫 판정은 본편 가드 뒤라 불러오는 중(뷰 없음) 주인이 아직 안 붙은 펫을 판정 불가로 세지 않는다.
        public static LifecycleDecision Lifecycle(bool turnOn, bool reapplying, int saveState, bool hasView, bool hasAvatar, PetState pet)
        {
            if (hasAvatar && !hasView) throw new ArgumentException("ChimeraMonk: visual lifecycle with an avatar but no view", "hasAvatar");
            if (pet != PetState.NotPet && pet != PetState.Pet && pet != PetState.Unknown) throw new ArgumentOutOfRangeException("pet", pet, "ChimeraMonk: unknown pet state");
            if (reapplying) return new LifecycleDecision(VisualAction.Skip, "fact is reapplying", false);
            if (saveState == SaveStateSaving) return new LifecycleDecision(VisualAction.Skip, "game is saving", false);
            if (!hasView) return new LifecycleDecision(VisualAction.Skip, "unit has no view yet", false);
            if (!hasAvatar) return new LifecycleDecision(VisualAction.Skip, "view has no character avatar (polymorphed)", false);
            if (!turnOn) return new LifecycleDecision(VisualAction.Detach, null, false);
            if (pet == PetState.Pet) return new LifecycleDecision(VisualAction.Skip, ReasonPet, false);
            if (pet == PetState.Unknown) return new LifecycleDecision(VisualAction.Skip, ReasonPetUnknown, true);
            return new LifecycleDecision(VisualAction.Attach, null, false);
        }

        // [순수] 펫 판정: 엔진 UnitEntityData.IsPet(UnitPartPet 부품 유무)·Master(UnitPartPet.Master)를 읽었는가(read) · 그 값.
        //  읽음 ∧ IsPet ∧ 주인 있음 → Pet · 읽음 ∧ ¬IsPet ∧ 주인 없음 → NotPet · 그 밖(못 읽음 · 둘이 어긋남) → Unknown.
        public static PetState PetStatus(bool read, bool isPet, object master)
        {
            if (!read) return PetState.Unknown;
            if (isPet && master != null) return PetState.Pet;
            if (!isPet && master == null) return PetState.NotPet;
            return PetState.Unknown;
        }

        // [순수] 판단 결과 실행: Attach → attach() 한 번, Detach → detach() 한 번, Skip → 아무것도 안 부름(Report 면 report(사유) 한 번).
        public static void Run(LifecycleDecision d, Action attach, Action detach, Action<string> report)
        {
            if (attach == null) throw new ArgumentNullException("attach");
            if (detach == null) throw new ArgumentNullException("detach");
            if (report == null) throw new ArgumentNullException("report");
            switch (d.Action)
            {
                case VisualAction.Attach: attach(); return;
                case VisualAction.Detach: detach(); return;
                case VisualAction.Skip: if (d.Report) report(d.Reason); return;
                default: throw new InvalidOperationException("ChimeraMonk: unknown visual action " + d.Action);
            }
        }

        // [순수] 본편 UnitEntityView.GetActualRace(IL @0000~0031)와 같은 우선순위: 활성 변신(Polymorph)의 종족 → 없으면 본 종족.
        //  둘 다 없으면 본편은 인간(0)으로 떨어지지만 우리는 −1(= Pick 이 「외형 없음」)을 돌려준다 — 모르는 종족에 인간 뿔을 붙이지 않는다.
        public static int ActualRaceIndex(int? polymorphRace, int? ownRace)
        {
            if (polymorphRace.HasValue && polymorphRace.Value < 0) throw new ArgumentOutOfRangeException("polymorphRace", polymorphRace.Value, "ChimeraMonk: negative race number");
            if (ownRace.HasValue && ownRace.Value < 0) throw new ArgumentOutOfRangeException("ownRace", ownRace.Value, "ChimeraMonk: negative race number");
            if (polymorphRace.HasValue) return polymorphRace.Value;
            if (ownRace.HasValue) return ownRace.Value;
            return -1;
        }

        // [순수] 유닛에서 읽은 값(변신 종족·본 종족·성별 번호) → Pick 인자. 성별 번호가 Male/Female 이 아니면 던진다.
        public static VisualPick PickFor(BundleShape s, int? polymorphRace, int? ownRace, int gender)
        {
            if (gender != GenderMale && gender != GenderFemale) throw new ArgumentOutOfRangeException("gender", gender, "ChimeraMonk: gender must be " + GenderMale + " (male) or " + GenderFemale + " (female)");
            return Pick(s, ActualRaceIndex(polymorphRace, ownRace), gender == GenderFemale);
        }

        // [순수] 컴포넌트가 붙이거나 뗄 에셋: SingleAsset = 그 에셋 하나(종족·성별 무관), RaceBundle = PickFor.
        public static VisualPick Decide(VisualKind kind, string assetId, BundleShape bundle, int? polymorphRace, int? ownRace, int gender)
        {
            if (kind == VisualKind.SingleAsset)
            {
                if (!IsAssetId(assetId)) throw new ArgumentException("ChimeraMonk: single-asset visual without a 32-hex asset id ('" + assetId + "')", "assetId");
                return new VisualPick(VisualState.Attach, new[] { assetId }, null);
            }
            if (kind == VisualKind.RaceBundle)
            {
                if (assetId != null) throw new ArgumentException("ChimeraMonk: race-bundle visual with a single-asset id", "assetId");
                if (bundle == null) throw new ArgumentNullException("bundle");
                return PickFor(bundle, polymorphRace, ownRace, gender);
            }
            throw new InvalidOperationException("ChimeraMonk: unknown visual kind " + kind);
        }

        // 황금 용 꼬리 에셋(종족·성별 공용). 출처 = blueprints.zip EquipmentEntities/Mythic/KEE_Tail_TailMythicDragon_Any.jbp m_MaleArray[0] = m_FemaleArray[0](정찰 2-1 T1·4절 1순위).
        // 에셋 id 는 블루프린트가 아니라 의존 관문이 못 잡는다 → 빌드 시 그 본편 묶음의 값과 대조한다(SingleAssetProblems).
        public const string DragonTailAsset = "4d431e5c5a1bdf7468ad6bcc0ee79872";

        // 엔진 Kingmaker.Blueprints.Race: Human = 0(정찰 머리말 종족 번호 표, dnlib 직독). 뿔 묶음의 「기대한 형식」 대표 칸.
        public static readonly int HumanRaceIndex = 0;

        public static IReadOnlyList<VisualRow> Rows()
        {
            return new[]
            {
                new VisualRow("Tail", VisualKind.SingleAsset, Guids.Game.KeeTailMythicDragonAny, "KEE_Tail_TailMythicDragon_Any", DragonTailAsset),   // 정찰 4절 꼬리 1순위
                new VisualRow("Horn", VisualKind.RaceBundle, Guids.Game.KeeHornsMythicDevilSmall, "KEE_Horns_HornsMythicDevilSmall", null),          // 소유자 결정 2026-11-16 3번(정찰 3-1 H2 작은 뿔)
            };
        }

        public static VisualRow Row(string mutation)
        {
            var hit = Rows().Where(r => r.Mutation == mutation).ToArray();
            if (hit.Length != 1) throw new ArgumentException("ChimeraMonk: VisualPlan has " + hit.Length + " row(s) for " + mutation, "mutation");
            return hit[0];
        }

        static bool IsAssetId(string s) { return s != null && s.Length == 32 && s.All(c => c >= '0' && c <= '9' || c >= 'a' && c <= 'f'); }

        // 꼬리 원본 검사: 종족 무관 · 남·여 각 정확히 1개 · 둘이 같음 · 계획 상수와 같음(C4 — 본편 블루프린트 값과 우리 상수 두 벌이 어긋나면 실패). 빈 목록 = 문제 없음.
        public static List<string> SingleAssetProblems(BundleShape s, string expectedAsset)
        {
            if (string.IsNullOrEmpty(expectedAsset)) throw new ArgumentException("ChimeraMonk: SingleAssetProblems without an expected asset id", "expectedAsset");
            var p = new List<string>();
            if (s == null) { p.Add("bundle missing"); return p; }
            if (s.RaceDependent) p.Add("bundle is race dependent (one asset for every race expected)");
            if (s.Male.Count != 1 || s.Female.Count != 1) { p.Add("bundle has " + s.Male.Count + " male / " + s.Female.Count + " female asset(s), expected exactly 1 / 1"); return p; }
            if (s.Male[0] != s.Female[0]) p.Add("male asset " + s.Male[0] + " != female asset " + s.Female[0]);
            if (!IsAssetId(s.Male[0])) p.Add("asset '" + s.Male[0] + "' is not a 32-hex asset id");
            if (s.Male[0] != expectedAsset) p.Add("bundle asset " + s.Male[0] + " != plan asset " + expectedAsset);
            return p;
        }

        // 뿔 원본 검사: 종족 의존 · 종족 칸이 있음 · 인간 칸(대표)에 남·여 모두 있음 · 들어 있는 값은 전부 32자리 16진 에셋 id. 빈 목록 = 문제 없음.
        // (다른 종족의 빈 칸은 문제가 아니다 — 런타임 Pick 이 NoVisual 로 답한다.)
        public static List<string> BundleProblems(BundleShape s)
        {
            var p = new List<string>();
            if (s == null) { p.Add("bundle missing"); return p; }
            if (!s.RaceDependent) p.Add("bundle is not race dependent (race x gender slots expected)");
            if (s.Races.Count == 0) { p.Add("bundle has no race slots"); return p; }
            if (!s.Races.Any(r => r != null && (r.Male.Count > 0 || r.Female.Count > 0))) p.Add("every race slot is empty");
            var human = HumanRaceIndex < s.Races.Count ? s.Races[HumanRaceIndex] : null;
            if (human == null || human.Male.Count == 0 || human.Female.Count == 0) p.Add("human slot (race " + HumanRaceIndex + ") lacks a male or female entry");
            for (int i = 0; i < s.Races.Count; i++)
            {
                if (s.Races[i] == null) continue;
                foreach (var id in s.Races[i].Male.Concat(s.Races[i].Female))
                    if (!IsAssetId(id)) p.Add("race slot " + i + " entry '" + id + "' is not a 32-hex asset id");
            }
            return p;
        }

        // 런타임 판정: 주인 종족 번호(-1 = 종족 없음) · 성별 → 붙일 에셋. 그 종족·성별 칸이 없으면 NoVisual(사유) — 엔진 GetLinks 처럼 종족 무관 배열로 조용히 떨어지지 않는다.
        public static VisualPick Pick(BundleShape s, int raceIndex, bool female)
        {
            if (s == null) throw new ArgumentNullException("s");
            if (!s.RaceDependent) throw new InvalidOperationException("ChimeraMonk: VisualPlan.Pick on a bundle that is not race dependent");
            string who = "race " + raceIndex + (female ? " female" : " male");
            if (raceIndex < 0) return None("owner has no race");
            if (raceIndex >= s.Races.Count) return None(who + ": bundle has " + s.Races.Count + " race slot(s)");
            var slot = s.Races[raceIndex];
            if (slot == null) return None(who + ": race slot missing");
            var ids = female ? slot.Female : slot.Male;
            if (ids.Count == 0) return None(who + ": race slot has no entry for this gender");
            return new VisualPick(VisualState.Attach, ids, null);
        }

        static VisualPick None(string reason) { return new VisualPick(VisualState.NoVisual, new string[0], reason); }
    }
}
