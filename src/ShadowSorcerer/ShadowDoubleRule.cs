using System;
using System.Collections.Generic;
using System.Linq;

namespace ShadowSorcerer
{
    // [순수] 시각 전용 그림자 유닛(분신 형상)의 블루프린트 계획·수명 결정·격리 단언. 게임 형식 없음(modinfo testSources).
     
    public enum Tri { Unknown, No, Yes }

    public static class ShadowDoublePlan
    {
        // ---- 원본(본편 Shadow 유닛)의 알려진 모양. 출처: blueprints.zip Classes/Wizard/Archetypes/ShadowCaster/SummonShadows/Shadow.jbp 직독 2026-11-17. ----
        // 모델 프리팹(귀검사 정찰 기록상 BCT_ShadowHalfElfMale). 다른 값이면 다른 모드가 외형을 바꾼 것 — 손 뼈대·시전 동작 전제가 무너지므로 만들지 않는다.
        public const string SourcePrefabAssetId = "9dc6267b1d7e2a947a0cb7e6328f8905";
        public const string SourceSizeName = "Medium";
        // 원본 사실 6개: ShadowFX(외형) + 규칙 사실 5(Incorporeal · Unlootable · ChannelResistance2 · ShadowPowerFeatureBonusDamage · UndeadType — 전부 규칙 효과, jbp 직독).
        // 사본에는 껍데기 특성 + ShadowFX 만 넣는다. 원본에 ShadowFX 가 없으면 「검은 그림자」 외형 전제가 무너진다 → 만들지 않는다.
        public const string ShadowFxBuffGuid = Guids.Game.ShadowFxBuff; // Buffs/ShadowFX.jbp (FxOnStart b6b3ca16…, FxOnRemove 39802c70…, HiddenInUi)

        public enum ComponentPlan { Keep, Drop, Unknown }

        // 원본 컴포넌트 분류(형식 전체 이름, 정확 일치). Unknown 이 하나라도 있으면 빌드가 그림자 유닛을 만들지 않는다(본편 배치 밖 = 실패, 조용히 따라가지 않는다).
        //   본편 배치(jbp 2026-11-17) = AddClassLevels + ChangeImpatience.
        //   Keep  AddClassLevels   : 언데드 9레벨 — 0레벨 유닛의 생명 상태가 미확인이라 유지(귀검사 령과 같은 판단). 시각 효과 없음.
        //   Drop  ChangeImpatience : AI 조급함 조정. 두뇌 없는 유닛에 쓸 일 없음.
        //   Drop  Experience / AddLoot : 경험치·전리품 — 원본에는 없지만(Experience 는 m_Overrides 에 제거 흔적) 있으면 빼는 것이 곧 요구사항(죽음·전리품 없음).
        public static ComponentPlan Component(string fullTypeName)
        {
            switch (fullTypeName)
            {
                case "Kingmaker.Blueprints.Classes.AddClassLevels": return ComponentPlan.Keep;
                case "Kingmaker.UnitLogic.FactLogic.ChangeImpatience":
                case "Kingmaker.Blueprints.Classes.Experience.Experience":
                case "Kingmaker.UnitLogic.FactLogic.AddLoot":
                    return ComponentPlan.Drop;
            }
            return ComponentPlan.Unknown;
        }

        public enum BodyField { Keep, ClearItem }

        // BlueprintUnit.UnitBody 의 필드별 처리. 표에 없는 이름은 던진다(게임이 장비 칸을 늘리면 아이템이 조용히 새지 않고 빌드가 멈춘다).
        // 필드 목록 = Assembly-CSharp BlueprintUnit/UnitBody 인스턴스 필드(시험 `exec: double body fields vs dll` 이 같은 DLL 과 대조).
        // 배경: Player 진영 유닛은 파티 공용 인벤토리를 쓰고, 장착이 거부된 손 무기가 파티 가방에 쌓인다(귀검사 실사고 2026-11-14). 원본 Shadow 는 주 손에 ShadowTouch 를 든다.
        public static BodyField Body(string fieldName)
        {
            switch (fieldName)
            {
                case "DisableHands":
                case "ActiveHandSet":
                case "m_EmptyHandWeapon": // 맨손 무기: 엔진이 만들 뿐 어떤 인벤토리에도 넣지 않는다(귀검사 정찰 — UnitBody.SetEmptyHandWeapon)
                    return BodyField.Keep;
                case "m_PrimaryHand": case "m_SecondaryHand":
                case "m_PrimaryHandAlternative1": case "m_SecondaryHandAlternative1":
                case "m_PrimaryHandAlternative2": case "m_SecondaryHandAlternative2":
                case "m_PrimaryHandAlternative3": case "m_SecondaryHandAlternative3":
                case "m_AdditionalLimbs": case "m_AdditionalSecondaryLimbs":
                case "m_Armor": case "m_Shirt": case "m_Belt": case "m_Head": case "m_Glasses": case "m_Feet": case "m_Gloves":
                case "m_Neck": case "m_Ring1": case "m_Ring2": case "m_Wrist": case "m_Shoulders": case "m_QuickSlots":
                    return BodyField.ClearItem;
            }
            throw new InvalidOperationException("ShadowSorcerer: unknown BlueprintUnit.UnitBody field '" + fieldName + "' - classify it (Keep / ClearItem) after checking whether UnitBody.Initialize creates an item from it");
        }
    }

    // 그림자 유닛을 둘 수 있는가(본체·게임 상태). 입력 하나라도 Unknown = 판정 불가(호출자가 보고, 만들지 않는다).
    // Dormant = 본체가 같은 지역의 게임 상태에 있지만 지금 게임 밖이거나 죽었다(파티 교체·컷신 숨김·사망 — 2차 독립 검토 경미-5):
    //   새로 만들지는 않지만 있는 묶음은 숨긴 채 둔다(다시 게임 안에 돌아오거나 부활하면 그대로 쓴다 — 재생성 예산을 쓰지 않는다).
    public enum DoubleEligibility { Unknown, Eligible, NotNow, Dormant }

    public struct DoubleFacts
    {
        public Tri AreaLoaded;        // 불러온 지역과 그 주 상태가 있다
        public Tri AreaHidesParty;    // 전역 지도처럼 지역이 파티를 숨긴다
        public Tri OwnerInGame;
        public Tri OwnerInState;      // 본체가 게임 상태(지역 주 상태·파티 상태)에 등록돼 있다 — 레벨업 화면의 미리보기 유닛·방금 만들어 아직 등록 전인 유닛은 아니오
        public Tri OwnerDead;
        public Tri HasEchoFeature;
        public int WantCount;         // 분신 수(ShadowRules.EchoCount) — 0 이하 = 표 밖(판정 불가)
        public string UnknownName()
        {
            if (AreaLoaded == Tri.Unknown) return "AreaLoaded";
            if (AreaHidesParty == Tri.Unknown) return "AreaHidesParty";
            if (OwnerInGame == Tri.Unknown) return "OwnerInGame";
            if (OwnerInState == Tri.Unknown) return "OwnerInState";
            if (OwnerDead == Tri.Unknown) return "OwnerDead";
            if (HasEchoFeature == Tri.Unknown) return "HasEchoFeature";
            return null;
        }
    }

    public enum PoolStep { Nothing, Remove, Create, Replace, Keep }
    // 격리 감시선의 판정. Unreadable = 입력을 읽을 수 없다(새지 않았다는 뜻이 아니다 — 호출자가 그 입력 이름으로 한 번 보고한다).
    public enum IsolationLeakKind { None, Damage, Combat, Buffs, Exposed, Unreadable }

    // 감시선 입력(그림자 유닛 하나를 되읽은 값). null / Tri.Unknown = 읽을 수 없다.
    public struct IsolationFacts
    {
        public int? Damage;
        public Tri InCombat;
        public Tri HiddenByUs;                // 묶음 장부: 우리가 숨겨 둔 유닛인가(의도)
        public Tri InGame;                    // 엔진 EntityDataBase.IsInGame
        public string[] BuffsAtRegistration;  // 등록이 확인된 순간 되읽은 버프 블루프린트 guid(정렬) = 기대 집합(2차 독립 검토 중요-3: 생성 직후가 아니다 — 등록 때 버프를 다는 모드가 있다)
        public string[] BuffsNow;             // 지금 집합. BuffsAtRegistration 과 같은 배열 = 호출자가 참조 비교로 이미 같다고 확인했다(문자열을 만들지 않는다 — 경미-6)
    }

    public static class ShadowDoubleRule
    {
        public static DoubleEligibility Eligible(DoubleFacts f)
        {
            if (f.UnknownName() != null) return DoubleEligibility.Unknown;
            // 특성이 없거나 지역이 없거나 본체가 죽었거나 숨겨졌거나 게임 상태에 없으면(미리보기 유닛) 「지금은 아님」(정당) — 분신 수는 그 뒤에만 본다.
            if (f.HasEchoFeature == Tri.No || f.AreaLoaded == Tri.No || f.AreaHidesParty == Tri.Yes || f.OwnerInState == Tri.No) return DoubleEligibility.NotNow;
            if (f.WantCount <= 0) return DoubleEligibility.Unknown; // 특성은 있는데 분신 수를 정할 수 없다(레벨이 표 밖)
            if (f.OwnerInGame == Tri.No || f.OwnerDead == Tri.Yes) return DoubleEligibility.Dormant; // 같은 지역에서 잠시 게임 밖·사망(경미-5) — 있는 묶음은 숨긴 채 유지
            return DoubleEligibility.Eligible;
        }

        // 한 본체의 그림자 묶음에 할 일.
        //   hasRecord = 묶음 기록이 있다 · sameArea = 그 묶음이 지금 지역의 주 상태에 만들어졌다 · anyGoing = 묶음의 유닛 중 사라지는 것이 있다 · have = 묶음의 유닛 수
        //   failedHere = 이 지역에서 이미 생성에 실패했다(다시 시도하지 않는다 — 매 프레임 재시도·오류 도배 금지. 지역이 바뀌면 다시 시도).
        //   Dormant: 같은 지역의 온전한 묶음이면 Keep(숨긴 채 유지 — 감시선은 계속 돈다), 다른 지역·사라지는 유닛·크기가 다르면 Remove, 묶음이 없으면 Nothing(잠든 동안 만들지 않는다).
        public static PoolStep Plan(DoubleEligibility e, bool hasRecord, bool sameArea, bool anyGoing, int have, int want, bool failedHere)
        {
            if (e == DoubleEligibility.Unknown) return PoolStep.Nothing; // 호출자가 보고한다. 판정 불가일 때 지우지도 만들지도 않는다
            if (e == DoubleEligibility.NotNow) return hasRecord ? PoolStep.Remove : PoolStep.Nothing;
            if (e == DoubleEligibility.Dormant) return !hasRecord ? PoolStep.Nothing : sameArea && !anyGoing && have == want ? PoolStep.Keep : PoolStep.Remove;
            if (!hasRecord) return failedHere ? PoolStep.Nothing : PoolStep.Create;
            if (!sameArea || anyGoing || have != want) return failedHere ? PoolStep.Remove : PoolStep.Replace;
            return PoolStep.Keep;
        }

        // 저장 제외의 실행 단언: 엔진 판정 함수(SceneEntitiesStateConverter.ShouldSerialize)를 그림자와 대조군(본체)에 부른 결과.
        // 대조군이 false 면 이 탐침을 믿을 수 없다(본체는 저장되는 유닛) → 그것도 실패.
        public static void RequireSaveExcluded(bool shadowSerialized, bool ownerSerialized)
        {
            if (!ownerSerialized) throw new InvalidOperationException("ShadowSorcerer: save-exclusion probe is not trustworthy (the control unit reads as not serialized)");
            if (shadowSerialized) throw new InvalidOperationException("ShadowSorcerer: the shadow unit would be written into the save (UnitPartTrapActor did not exclude it)");
        }

        // 인벤토리 격리: 생성 전후 파티 인벤토리(항목 수·수량 합)가 같고, 그림자의 인벤토리는 파티 것이 아니며 비어 있다.
        public static void RequireNoInventoryLeak(int partyEntriesBefore, int partyTotalBefore, int partyEntriesAfter, int partyTotalAfter, bool shadowSharesParty, int shadowEntries)
        {
            foreach (var v in new[] { partyEntriesBefore, partyTotalBefore, partyEntriesAfter, partyTotalAfter, shadowEntries })
                if (v < 0) throw new ArgumentOutOfRangeException("count", v, "ShadowSorcerer: inventory count must be >= 0");
            if (shadowSharesParty) throw new InvalidOperationException("ShadowSorcerer: the shadow unit's inventory is the party inventory (EnsureOwnInventory did not isolate it)");
            if (partyEntriesAfter != partyEntriesBefore || partyTotalAfter != partyTotalBefore)
                throw new InvalidOperationException("ShadowSorcerer: creating a shadow unit changed the party inventory: entries " + partyEntriesBefore + " -> " + partyEntriesAfter + ", total count " + partyTotalBefore + " -> " + partyTotalAfter);
            if (shadowEntries != 0) throw new InvalidOperationException("ShadowSorcerer: the shadow unit owns " + shadowEntries + " item(s) after creation, expected 0");
        }

        // 등록이 끝난 뒤의 「정확히 N」: 이 본체에 묶인 살아 있는 그림자 유닛 수 = 묶음 크기, 전부 묶음에 있는 것.
        public static void RequireExactly(int liveOwned, int inSet, int want, string owner)
        {
            if (liveOwned != want || inSet != want) throw new InvalidOperationException("ShadowSorcerer: " + owner + " has " + liveOwned + " live shadow unit(s), " + inSet + " of them tracked, expected exactly " + want);
        }

        // 격리 감시선(독립 검토 F2 로 넓힘): 시각 전용 유닛에 남은 흔적을 되읽어 「무언가 이 유닛을 건드렸다」를 드러낸다. 더 구체적인 증거가 먼저.
        //   Damage   피해가 0 이 아니다(음수 포함). 단 엔진 RuleDealDamage 는 대상 불가 유닛의 피해를 0 으로 끝내므로 이것만으로는 범위 주문에 낀 것을 못 본다.
        //   Combat   전투에 들어갔다.
        //   Buffs    버프 집합이 등록 확인 때와 다르다(더해짐·빠짐 — 원인 미상: 범위 주문이 그림자를 골랐을 수도, 다른 모드가 달았을 수도 있다).
        //   Exposed  우리가 숨겨 둔 유닛인데 게임 안(IsInGame)이다 — 숨긴 동안의 격리(유닛 열거에서 빠짐)가 풀렸다(호출자가 보고 뒤 다시 게임 밖으로 돌린다 — 중요-2).
        //   Unreadable  위에 해당하는 것이 없지만 읽지 못한 입력이 있다(detail = 입력 이름) — None 으로 넘기지 않는다.
        // detail: Buffs = 차이, Unreadable = 입력 이름, 그 밖 = null.
        public static IsolationLeakKind IsolationLeak(IsolationFacts f, out string detail)
        {
            detail = null;
            if (f.Damage.HasValue && f.Damage.Value != 0) return IsolationLeakKind.Damage;
            if (f.InCombat == Tri.Yes) return IsolationLeakKind.Combat;
            // 같은 배열 = 호출자가 블루프린트 참조로 이미 같다고 확인했다(매 틱 문자열을 만들지 않는 빠른 길 — 경미-6). 다른 배열이면 guid 로 다시 센다.
            // 엔진이 모든 유닛에 붙이는 기본 토글 버프(EngineDefaultToggleBuffs)의 더해짐·빠짐은 차이로 세지 않는다(2026-11-17 점검 S0·S4·S15 오탐).
            if (f.BuffsAtRegistration != null && f.BuffsNow != null && !ReferenceEquals(f.BuffsAtRegistration, f.BuffsNow))
            {
                detail = IsolationBuffDiff(f.BuffsAtRegistration, f.BuffsNow);
                if (detail != null) return IsolationLeakKind.Buffs;
            }
            if (f.HiddenByUs == Tri.Yes && f.InGame == Tri.Yes) return IsolationLeakKind.Exposed;
            detail = !f.Damage.HasValue ? "Damage" : f.InCombat == Tri.Unknown ? "InCombat" : f.BuffsAtRegistration == null ? "BuffsAtRegistration" : f.BuffsNow == null ? "BuffsNow"
                : f.HiddenByUs == Tri.Unknown ? "HiddenByUs" : f.InGame == Tri.Unknown ? "InGame" : null;
            return detail == null ? IsolationLeakKind.None : IsolationLeakKind.Unreadable;
        }

         
        //   숨김 의도(HiddenByUs = Yes)인데 게임 안(InGame = Yes)이면 다른 누출(피해·전투·버프 차이)이 함께 있어도 복구한다 — 분류가 Buffs 를 먼저 돌려줘도 숨김 복구를 건너뛰지 않는다.
        //   어느 하나라도 Unknown 이면 복구하지 않는다(의도·상태를 모르고 SetHidden 하지 않는다 — 그 판독 불가는 IsolationLeak 이 Unreadable 로 보고한다).
        public static bool NeedsRestore(IsolationFacts f) { return f.HiddenByUs == Tri.Yes && f.InGame == Tri.Yes; }

        // 지금 목록 now 의 열쇠(key) 다중집합이 baseline 과 같은가 — 참조 비교만, 할당 없음(경미-6: 매 틱 guid 문자열을 만들지 않는다).
        //   null = 읽을 수 없다(목록·기준이 없다, 또는 항목·열쇠가 null) — 「같다」로 넘기지 않는다. false = 다르다(호출자가 그때만 guid 로 차이를 만든다).
        public static bool? SameRefs<T>(IList<T> now, Func<T, object> key, object[] baseline)
        {
            if (now == null || key == null || baseline == null) return null;
            bool sameOrder = now.Count == baseline.Length;
            for (int i = 0; i < now.Count; i++)
            {
                var item = now[i];
                if ((object)item == null) return null;
                var current = key(item);
                if (current == null) return null;
                if (sameOrder && !ReferenceEquals(current, baseline[i])) sameOrder = false;
            }
            if (sameOrder) return true;
            for (int i = 0; i < baseline.Length; i++) if (baseline[i] == null) return null;
            if (now.Count != baseline.Length) return false;
            for (int i = 0; i < now.Count; i++)
            {
                object k = key(now[i]);
                int inNow = 0, inBase = 0;
                for (int j = 0; j < now.Count; j++) if (ReferenceEquals(key(now[j]), k)) inNow++;
                for (int j = 0; j < baseline.Length; j++) if (ReferenceEquals(baseline[j], k)) inBase++;
                if (inNow != inBase) return false;
            }
            return true;
        }

        // 숨긴 그림자를 게임 밖으로 다시 돌릴 예산(2차 독립 검토 중요-2): 이 본체가 이 지역에서 이미 restoredHere 번 돌렸다. 한도에 닿았으면 던진다 →
        //   호출자(Tick)의 Fail 이 묶음을 지우고 이 지역에 실패 표식을 남긴다(오류 줄 1회 — 다른 모드와 매 틱 줄다리기하지 않는다. 메아리 규칙은 그대로).
        public static void RequireRestoreBudget(int restoredHere, int limit, string owner)
        {
            ShadowVisualLimits.ValidateRestores(limit);
            if (restoredHere < 0) throw new ArgumentOutOfRangeException("restoredHere", restoredHere, "ShadowSorcerer: restore count must be >= 0");
            if (restoredHere >= limit) throw new InvalidOperationException("ShadowSorcerer: a hidden shadow unit of " + owner + " was put back in game " + (restoredHere + 1) + " time(s) in this area (limit " + limit + ") - something keeps exposing it; shadow visual off in this area");
        }

        // 두 버프 guid 목록을 중복 수까지 비교한다(순서 무관). 같으면 null, 다르면 "added [...] removed [...]".
        public static string BuffSetDiff(string[] expected, string[] now)
        {
            if (expected == null) throw new ArgumentNullException("expected");
            if (now == null) throw new ArgumentNullException("now");
            var left = new List<string>(expected);
            var added = new List<string>();
            foreach (var g in now)
            {
                if (g == null) throw new ArgumentException("ShadowSorcerer: a buff guid is null (cannot compare buff sets)");
                if (!left.Remove(g)) added.Add(g);
            }
            if (left.Count == 0 && added.Count == 0) return null;
            return "added [" + string.Join(",", added.ToArray()) + "] removed [" + string.Join(",", left.ToArray()) + "]";
        }

        // 엔진이 등록 뒤 모든 유닛에 붙이는 기본 토글 버프(정확한 guid 집합). 감시선은 이 버프의 더해짐·빠짐을 「다른 버프 집합」으로 세지 않는다.
        //   배경(2026-11-17 점검 S0·S4·S15): 숨긴 그림자의 기준(등록 확인 때)에는 없던 9fd2dd27… 이 처음 드러난 뒤 붙어 오류 1건 + 누출 계수 495(매 틱)가 났다(UMM Log.txt 15168).
        //   출처(blueprints.zip 직독 2026-11-17): Root/BlueprintRoot.jbp Progression.m_FeatsProgression = Classes/Basic/BasicFeatsProgression.jbp(5b72dd2c…) L1
        //     → InspiredRageAllyFeature(e6b9eb30…) AddFacts → InspiredRageAllyToggleSwitch(180e06c0…, BlueprintActivatableAbility IsOnByDefault = true, m_Buff = 9fd2dd27…).
        //     BasicFeatsProgression 아래(AddFacts 사슬 전부) IsOnByDefault 토글은 이것 하나(FightDefensivelyToggleAbility·MobilityUseAbility 는 false).
        //   엔진: ActivatableAbilityCollection.OnFactDidAttach IL_000D~0016(IsOnByDefault → IsOn) · 버프는 UnitActivatableAbilitiesController.TickAbility IL_0152(게임 안 유닛만 도는 컨트롤러)
        //     → 게임 밖에 숨긴 그림자는 기준을 찍을 때 없고, 처음 드러날 때 붙는다.
        //   시험 「jbp: engine default toggle buffs」 가 이 집합을 blueprints.zip 의 BasicFeatsProgression 사슬과 대조한다(C4 — 다르면 FAIL).
        public static readonly IReadOnlyList<string> EngineDefaultToggleBuffs = new[] { "9fd2dd27d838f0049ab56e9da3508b25" }; // Classes/Skald/RagingSong/InspiredRageAllyToggleSwitchBuff.jbp

        static string[] WithoutEngineDefaults(string[] guids)
        {
            if (guids == null) throw new ArgumentNullException("guids");
            return guids.Where(g => g == null || !EngineDefaultToggleBuffs.Contains(g)).ToArray(); // null 은 남겨 BuffSetDiff 가 던지게 한다
        }

        // 감시선의 버프 차이: 엔진 기본 토글 버프를 양쪽에서 뺀 뒤의 BuffSetDiff(같으면 null).
        public static string IsolationBuffDiff(string[] expected, string[] now)
        {
            if (expected == null) throw new ArgumentNullException("expected");
            if (now == null) throw new ArgumentNullException("now");
            return BuffSetDiff(WithoutEngineDefaults(expected), WithoutEngineDefaults(now));
        }

        // 두 집합이 다르되, 다른 것이 엔진 기본 토글 버프뿐인가 → 호출자가 기준을 지금 집합으로 옮긴다(매 틱 문자열 비교로 떨어지지 않게 — 경미-6 의 빠른 길 복원).
        public static bool OnlyEngineDefaultsDiffer(string[] expected, string[] now)
        {
            return BuffSetDiff(expected, now) != null && IsolationBuffDiff(expected, now) == null;
        }

        // 묶음 생성 예산(독립 검토 F8): 이 본체가 이 지역에서 이미 madeHere 번 만들었다. 한도에 닿았으면 던진다 → 호출자가 그 지역의 연출을 끄고 한 번 보고한다(메아리 규칙은 그대로).
        public static void RequireCreateBudget(int madeHere, int limit, string owner)
        {
            ShadowVisualLimits.ValidateCore(limit);
            if (madeHere < 0) throw new ArgumentOutOfRangeException("madeHere", madeHere, "ShadowSorcerer: pool creation count must be >= 0");
            if (madeHere >= limit) throw new InvalidOperationException("ShadowSorcerer: the shadow pool of " + owner + " was already created " + madeHere + " time(s) in this area (limit " + limit + ") - something keeps destroying or invalidating it; shadow visual off in this area");
        }

        // 숨김 깃발 장부(유닛 하나): 우리가 Retain 한 횟수는 0 또는 1. 같은 방향으로 두 번 = 무동작(retainDelta 0) — Retain 과 Release 가 늘 짝을 이룬다(깃발이 영영 안 풀리거나 남의 숨김을 풀지 않게).
        // retained = 실제로 건 Retain 장부(의도 장부 HiddenByUs 가 아니다 — 2차 독립 검토 중요-2: 의도를 먼저 적고 도중에 던지면 둘이 어긋날 수 있다).
        public static bool NextHidden(bool retained, bool wantHidden, out int retainDelta)
        {
            retainDelta = retained == wantHidden ? 0 : wantHidden ? 1 : -1;
            return wantHidden;
        }
    }

    // 감시선 누출 계수(2026-11-17 점검 S15: 계속 남는 같은 누출을 매 틱 세어 495 가 됐다). 같은 (유닛, 종류, 세부)는 한 번만 센다.
    //   세부 = IsolationLeak 의 detail(버프 차이 문자열 등) — 차이가 더 커지면 새 항목으로 다시 센다. Exposed 는 복구할 때마다 ExposedRestores 가 따로 센다.
    //   판독 불가(Unreadable)·None 은 셀 수 없다(던진다 — 호출자가 누출일 때만 부른다).
    public sealed class LeakTally
    {
        readonly HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        public int Count { get; private set; }

        public bool Note(string unitId, IsolationLeakKind kind, string detail)
        {
            if (string.IsNullOrEmpty(unitId)) throw new ArgumentException("ShadowSorcerer: leak tally needs a unit id", "unitId");
            if (kind == IsolationLeakKind.None || kind == IsolationLeakKind.Unreadable) throw new ArgumentException("ShadowSorcerer: only a readable leak is tallied (got " + kind + ")", "kind");
            if (!seen.Add(unitId + "|" + kind + "|" + (detail ?? ""))) return false;
            Count++;
            return true;
        }
    }
}
