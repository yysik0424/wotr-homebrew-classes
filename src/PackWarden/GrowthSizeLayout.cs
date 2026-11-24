using System;
using System.Collections.Generic;

namespace PackWarden
{
    // 종 성장 특성(AnimalCompanionUpgrade*)에서 「크기를 바꾸는 부품」이 어느 모양으로 들어 있는지의 판정. 게임 형식 없음(문자열·정수만) → 콘솔 시험이 컴파일해 값으로 실행한다.
    // 2026-11-16 실사고: 오프라인 시험은 본편 blueprints.zip 만 보는데, 실제 게임에서는 TabletopTweaks-Base 가 성장 특성의 ChangeUnitSize 를 자기 부품으로 바꿔 놓아
    // 「ChangeUnitSize 가 없다」로 BuildAll 이 실패했다(UMM 로그: "AnimalCompanionUpgradeBear has no ChangeUnitSize but AuxUpgradesWithoutSize lists it").
    //
    // 배치 3가지(그 밖은 전부 던진다 — 모르는 모양을 「크기 변경 없음」으로 넘기지 않는다):
    //   None           = 크기 부품 0개. 본편에서는 말 성장뿐(Guids.AuxUpgradesWithoutSize 에 행이 없다).
    //   Vanilla        = 본편 Kingmaker.Designers.Mechanics.Buffs.ChangeUnitSize 1개(m_Type = Delta, SizeDelta = 1 — blueprints.zip Classes/Pets/AnimalCompanionUpgrade*.jbp).
    //   TabletopTweaks = TabletopTweaks.Core.NewComponents.ChangeUnitBaseSize 1개, 본편 ChangeUnitSize 0개.
    //
    // TabletopTweaks 배치의 출처(설치본 dnlib 직독 2026-11-16 — TabletopTweaks-Base 2.6.18 / TabletopTweaks-Core 0.7.16):
    //   TabletopTweaks-Base.dll  TabletopTweaks.Base.Bugfixes.Abilities.Spells/BlueprintsCache_Init_Patch::PatchAnimalGrowth (BlueprintsCache.Init 후크),
    //   설정 키 Fixes.json "Spells" → "AnimalGrowth"(기본 Enabled). AnimalCompanionSelectionBase(90406c57…)의 모든 동료 특성 → AddPet.m_UpgradeFeature 마다:
    //     본편 ChangeUnitSize 가 있으면 RemoveComponent(그것) 뒤 AddComponent<ChangeUnitBaseSize>(배열 끝에 덧붙임),
    //     IsTypeDelta → m_Type = Delta(0)·SizeDelta = 원본 SizeDelta / IsTypeValue → m_Type = Value(1)·Size = 원본 Size. ChangeUnitSize 가 없으면(말) 아무것도 안 한다.
    //   TabletopTweaks-Core.dll  ChangeUnitBaseSize.OnTurnOn → UnitPartBaseSizeAdjustment.AddEntry, 그 파트의 currentSizeDelta 를
    //     UnitPartBaseSizeAdjustment/UnitState_Size_Patch(Harmony Postfix, UnitState.Size getter)가 State.Size 에 더한다 — 크기 변화는 여전히 일어나고, 이 부품이 일으킨다.
    //   → 보조의 성장 사본에서 빼야 하는 것 = 이 ChangeUnitBaseSize 1개(빼면 파트가 안 생겨 getter 후크가 아무것도 더하지 않는다), 성장 크기 증가 = 그 SizeDelta.
    // 시험(tests\PackRulesTests.cs CheckGrowthSizeLayout)이 아래 상수를 설치된 TabletopTweaks DLL 과 대조한다(설치돼 있지 않으면 건너뜀으로 표시).
    public static class GrowthSizeLayout
    {
        public const string VanillaSizeComponent = "Kingmaker.Designers.Mechanics.Buffs.ChangeUnitSize";
        public const string TabletopTweaksSizeComponent = "TabletopTweaks.Core.NewComponents.ChangeUnitBaseSize";
        // ChangeUnitBaseSize 의 필드(공개 인스턴스 필드, Core dnlib): m_Type = UnitPartBaseSizeAdjustment/ChangeType { Delta = 0, Value = 1 }, SizeDelta = int.
        // 컴파일 의존 없이 리플렉션으로 읽는다(AuxUnits.GrowthSizeDelta). 필드가 없으면 던진다.
        public const string TabletopTweaksTypeField = "m_Type";
        public const string TabletopTweaksDeltaField = "SizeDelta";
        public const string TabletopTweaksChangeTypeEnum = "TabletopTweaks.Core.NewUnitParts.UnitPartBaseSizeAdjustment/ChangeType";
        // 본편·TabletopTweaks 두 부품 모두 형식 필드의 enum 이름이 "Delta" 일 때만 SizeDelta 가 크기 증가다(Value 형식이면 「몇 단계 자라는가」를 모른다 → 던짐).
        public const string DeltaTypeName = "Delta";
        // 지문 대조용(시험만 읽는다): 패치가 있는 형식·메서드·설정 키.
        public const string TabletopTweaksPatchType = "TabletopTweaks.Base.Bugfixes.Abilities.Spells/BlueprintsCache_Init_Patch";
        public const string TabletopTweaksPatchMethod = "PatchAnimalGrowth";
        public const string TabletopTweaksSettingKey = "AnimalGrowth";
        // 모드 로드 로그 한 줄의 머리(README 「게임 안에서 볼 문자열」과 시험이 이 상수를 본다).
        public const string LogPrefix = "growth size layout: ";

        // Mixed = 전체 판정(Overall)만 낸다 — 성장마다는 None / Vanilla / TabletopTweaks 중 하나다.
        public enum Kind { None, Vanilla, TabletopTweaks, Mixed }

        // 성장 특성 하나의 배치. componentTypes = 그 특성 ComponentsArray 의 형식 전체 이름(순서 무관), tableListsIt = Guids.AuxUpgradesWithoutSize 에 행이 있는가.
        // 던지는 경우: 목록·원소 null, 크기 부품 2개 이상(같은 종류 중복·두 종류 동시), 이름만 같은 다른 네임스페이스의 크기 부품,
        // 표에는 있는데 크기 부품이 없음(= 다른 모드가 모르는 모양으로 바꿨다), 크기 부품이 있는데 표에 없음(사본 GUID 가 없다).
        public static Kind Classify(string upgradeName, IList<string> componentTypes, bool tableListsIt)
        {
            if (string.IsNullOrEmpty(upgradeName)) throw new ArgumentException("PackWarden: growth feature name is missing", "upgradeName");
            if (componentTypes == null) throw new ArgumentNullException("componentTypes", "PackWarden: " + upgradeName + " component list is unreadable");
            int vanilla = 0, ttt = 0;
            foreach (var t in componentTypes)
            {
                if (string.IsNullOrEmpty(t)) throw new ArgumentException("PackWarden: " + upgradeName + " has a component whose type name is unreadable", "componentTypes");
                if (t == VanillaSizeComponent) { vanilla++; continue; }
                if (t == TabletopTweaksSizeComponent) { ttt++; continue; }
                string simple = t.Substring(Math.Max(t.LastIndexOf('.'), t.LastIndexOf('+')) + 1);
                if (simple == SimpleName(VanillaSizeComponent) || simple == SimpleName(TabletopTweaksSizeComponent))
                    throw new InvalidOperationException("PackWarden: " + upgradeName + " has a size component of an unknown type " + t + " (known: " + VanillaSizeComponent + ", " + TabletopTweaksSizeComponent + ")");
            }
            string shape = "components: [" + string.Join(", ", new List<string>(componentTypes).ToArray()) + "]";
            if (vanilla + ttt > 1)
                throw new InvalidOperationException("PackWarden: " + upgradeName + " has " + vanilla + " ChangeUnitSize and " + ttt + " ChangeUnitBaseSize components, expected at most 1 size component in total; " + shape);
            if (vanilla + ttt == 0)
            {
                if (tableListsIt)
                    throw new InvalidOperationException("PackWarden: " + upgradeName + " has no known size component (vanilla ChangeUnitSize or TabletopTweaks ChangeUnitBaseSize) but AuxUpgradesWithoutSize lists it — unknown growth layout; " + shape);
                return Kind.None;
            }
            if (!tableListsIt)
                throw new InvalidOperationException("PackWarden: " + upgradeName + " changes size (" + (vanilla == 1 ? "ChangeUnitSize" : "ChangeUnitBaseSize") + ") but has no row in Guids.AuxUpgradesWithoutSize");
            return vanilla == 1 ? Kind.Vanilla : Kind.TabletopTweaks;
        }

        // 보조의 성장 사본에서 빼야 하는 부품의 자리(componentTypes 의 인덱스). None = -1(뺄 것이 없다 — 판정된 0개), 그 밖 = 그 배치의 크기 부품 1개의 자리.
        // 판정은 Classify 와 같다(먼저 불러 모르는 모양을 던지게 한다). 호출자(AuxUnits.GrowthLayoutOf)는 이 자리의 인스턴스 하나만 사본에서 뺀다.
        public static int StripIndex(string upgradeName, IList<string> componentTypes, bool tableListsIt)
        {
            var kind = Classify(upgradeName, componentTypes, tableListsIt);
            if (kind == Kind.None) return -1;
            string want = SizeComponentOf(kind);
            int found = -1;
            for (int i = 0; i < componentTypes.Count; i++)
            {
                if (componentTypes[i] != want) continue;
                if (found >= 0) throw new InvalidOperationException("PackWarden: " + upgradeName + " has more than one " + want);
                found = i;
            }
            if (found < 0) throw new InvalidOperationException("PackWarden: " + upgradeName + " classified as " + kind + " but has no " + want);
            return found;
        }

        // 그 배치에서 보조의 성장 사본이 빼야 하는 부품의 형식 전체 이름. None 에는 뺄 것이 없다 → 던진다(호출자가 None 을 먼저 가른다).
        public static string SizeComponentOf(Kind kind)
        {
            switch (kind)
            {
                case Kind.Vanilla: return VanillaSizeComponent;
                case Kind.TabletopTweaks: return TabletopTweaksSizeComponent;
                default: throw new ArgumentOutOfRangeException("kind", kind, "PackWarden: a growth layout without a size component has nothing to strip");
            }
        }

        // 성장이 주는 크기 증가(주사위 복원 단계의 입력). changeTypeName = 크기 부품 형식 필드의 enum 이름, sizeDelta = SizeDelta 필드(못 읽었으면 null).
        // None = 증가 0(읽은 값이 없어야 한다 — 있으면 호출자 버그). 그 밖 = 형식이 Delta 이고 1 이상이어야 한다. 못 읽음·Value 형식·0 이하를 0 으로 넘기지 않는다.
        public static int SizeDelta(string upgradeName, Kind kind, string changeTypeName, int? sizeDelta)
        {
            if (string.IsNullOrEmpty(upgradeName)) throw new ArgumentException("PackWarden: growth feature name is missing", "upgradeName");
            if (kind == Kind.None)
            {
                if (changeTypeName != null || sizeDelta != null) throw new ArgumentException("PackWarden: " + upgradeName + " has no size component but a size change was passed", "kind");
                return 0;
            }
            if (kind != Kind.Vanilla && kind != Kind.TabletopTweaks) throw new ArgumentOutOfRangeException("kind", kind, "PackWarden: unknown growth layout");
            if (changeTypeName == null || sizeDelta == null)
                throw new InvalidOperationException("PackWarden: " + upgradeName + " (" + kind + " layout) size change is unreadable (type " + (changeTypeName ?? "?") + ", delta " + (sizeDelta == null ? "?" : sizeDelta.ToString()) + ")");
            if (changeTypeName != DeltaTypeName)
                throw new InvalidOperationException("PackWarden: " + upgradeName + " changes size by " + changeTypeName + ", not " + DeltaTypeName + " — the growth size step is unknown");
            if (sizeDelta.Value <= 0) throw new InvalidOperationException("PackWarden: " + upgradeName + " SizeDelta is " + sizeDelta.Value + ", expected a growth of 1 or more");
            return sizeDelta.Value;
        }

        // 전체 판정(로그 한 줄용 — 실제 처리는 성장마다의 판정을 쓴다): 크기를 바꾸는 성장이 전부 본편이면 Vanilla, 전부 TabletopTweaks 면 TabletopTweaks, 둘 다 있으면 Mixed.
        // 섞임은 실패가 아니다(검토 2026-11-17 지적 6): 보조 하나는 자기 종의 성장 사본 하나만 받고(AuxUnits.AuxUpgradeOf — 성장마다 그 배치의 크기 부품 1개를 뺀다),
        // 주사위 복원의 성장 크기 증가도 성장마다 그 부품의 SizeDelta 에서 읽으며(AuxUnits.GrowthSizeDelta), 소환 끝 크기·주사위 되읽기가 보조마다 결과를 다시 본다 —
        // 두 배치가 같이 쓰는 상태가 없다. 그래서 섞이면 진단으로 로그에 남기기만 한다(Describe 의 "mixed" 줄).
        // 크기를 바꾸는 성장이 하나도 없으면 던진다(표가 9행인데 전부 None 일 수는 없다 — Classify 가 먼저 던졌어야 한다).
        public static Kind Overall(IDictionary<string, Kind> byUpgrade)
        {
            List<string> vanilla, ttt, none;
            Split(byUpgrade, out vanilla, out ttt, out none);
            if (vanilla.Count == 0 && ttt.Count == 0) throw new InvalidOperationException("PackWarden: no companion growth changes size — the growth size layout cannot be determined");
            if (vanilla.Count > 0 && ttt.Count > 0) return Kind.Mixed;
            return vanilla.Count > 0 ? Kind.Vanilla : Kind.TabletopTweaks;
        }

        static void Split(IDictionary<string, Kind> byUpgrade, out List<string> vanilla, out List<string> ttt, out List<string> none)
        {
            if (byUpgrade == null) throw new ArgumentNullException("byUpgrade");
            vanilla = new List<string>(); ttt = new List<string>(); none = new List<string>();
            foreach (var kv in byUpgrade)
            {
                if (kv.Value == Kind.Vanilla) vanilla.Add(kv.Key);
                else if (kv.Value == Kind.TabletopTweaks) ttt.Add(kv.Key);
                else if (kv.Value == Kind.None) none.Add(kv.Key);
                else throw new ArgumentOutOfRangeException("byUpgrade", kv.Value, "PackWarden: growth " + kv.Key + " has layout " + kv.Value + " (a single growth is None, Vanilla or TabletopTweaks)");
            }
            vanilla.Sort(StringComparer.Ordinal); ttt.Sort(StringComparer.Ordinal); none.Sort(StringComparer.Ordinal);
        }

        // 로그 한 줄(모드 로드 로그): 어느 배치로 판정했는지, 어떤 부품을 사본에서 뺐는지, 크기 성장이 없는 종. 섞였으면 배치마다 어느 성장에서 무엇을 뺐는지 적는다.
        public static string Describe(IDictionary<string, Kind> byUpgrade)
        {
            var overall = Overall(byUpgrade);
            List<string> vanilla, ttt, none;
            Split(byUpgrade, out vanilla, out ttt, out none);
            string noSize = "; no size change: " + (none.Count == 0 ? "-" : string.Join(", ", none.ToArray())) + ")";
            if (overall == Kind.Mixed)
                return LogPrefix + "mixed (" + VanillaSizeComponent + " stripped from " + vanilla.Count + " auxiliary growth copies: " + string.Join(", ", vanilla.ToArray())
                    + "; " + TabletopTweaksSizeComponent + " stripped from " + ttt.Count + " auxiliary growth copies: " + string.Join(", ", ttt.ToArray()) + noSize;
            var sized = overall == Kind.Vanilla ? vanilla : ttt;
            return LogPrefix + (overall == Kind.Vanilla ? "vanilla" : "TabletopTweaks") + " (" + SizeComponentOf(overall) + " stripped from " + sized.Count + " auxiliary growth copies: "
                + string.Join(", ", sized.ToArray()) + noSize;
        }

        static string SimpleName(string fullName) { return fullName.Substring(fullName.LastIndexOf('.') + 1); }
    }
}
