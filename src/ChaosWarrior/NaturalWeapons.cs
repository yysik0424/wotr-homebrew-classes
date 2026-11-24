using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.ElementsSystem;
using Kingmaker.Localization;
using System.Reflection;
using Kingmaker.Dungeon.Blueprints;
using Kingmaker.RuleSystem;
using WotrClassMod.Common;
using static ChaosWarrior.BuildKit;
using static WotrClassMod.Common.BlueprintUtil;

namespace ChaosWarrior
{
    // 자연 무기 복제(개편 계획 2026-11-16 Task O4): NaturalWeaponPlan.Rows 의 행마다 본편 자연 무기를 우리 GUID 로 복제한다(원본 무쓰기 — BlueprintClone.Clone 의 공유 검사).
    // Mutations.Build 보다 먼저 돈다(Blueprints.Builders) — 표식의 AddAdditionalLimb 가 이 복제본을 참조한다(MutationSpecs.EffectRows 의 Extra 칸).
    // 원본이 1d4 인 행(촉수 · 갈고리발톱)은 복제본에만 m_OverrideDamageDice = true · m_DamageDice = 1d6 을 쓴다(본편 Sting1d6 · Wing1d6 과 같은 모양, 정찰 §Q3).
    // 끝에 되읽기(VerifyNow) — 어긋나면 던진다(BuildFailed). 조용한 통과 없음: 원본 이름·형식·자연 무기 여부가 원장과 다르면 복제 전에 던진다(다른 모드가 원본을 바꾼 경우 포함).
    public static class NaturalWeapons
    {
        static readonly Dictionary<string, BlueprintItemWeapon> weapons = new Dictionary<string, BlueprintItemWeapon>();
        // 변이 Id → 우리 복제 무기.
        internal static bool Owns(BlueprintItemWeapon weapon) { return weapons.ContainsValue(weapon); }
        public static IReadOnlyDictionary<string, BlueprintItemWeapon> Weapons { get { return weapons; } }

        public static bool Built { get { return weapons.Count == NaturalWeaponPlan.Rows.Count; } }

        public static void Build()
        {
            if (weapons.Count != 0) throw new InvalidOperationException("ChaosWarrior: NaturalWeapons.Build() called twice");
            NaturalWeaponPlan.Validate();
            foreach (var row in NaturalWeaponPlan.Rows) weapons.Add(row.MutationId, CloneWeapon(row));
            var diff = VerifyNow();
            if (diff.Count != 0) throw new InvalidOperationException("ChaosWarrior: natural weapon clone read-back differs from NaturalWeaponPlan.Rows: " + string.Join(" | ", diff.ToArray()));
            // 개편 O5(계획 Task O5 Interfaces 끝 줄): 엔진의 범주 상한이 문서 전사(ChaosRules.LimbCapsExpected)와 다르면 정보 줄 1회. 다른 모드가 상한을 바꾼 경우다 —
            // 다시 뽑기 판정은 실행 때 엔진 값을 따르므로 오류가 아니고, 설명문 · README 의 숫자(물기 1 · 발톱 2 …)가 이 설치본과 다르다는 표시다. 표를 읽을 수 없으면 던진다(「같다」로 넘기지 않는다).
            var capLine = EngineLimbCaps.InfoLine();
            if (capLine != null) ModContext.Log(capLine);
        }

        // 본편 자연 무기 → 우리 GUID 복제. 원본의 이름·무기 형식·자연 무기 여부가 원장과 다르면 던진다. 새 GUID 가 이미 캐시에 있으면(본편·다른 모드와 충돌) BlueprintUtil.Create 가 던진다.
        static BlueprintItemWeapon CloneWeapon(NaturalWeaponPlan.WeaponRow row)
        {
            var src = Get<BlueprintItemWeapon>(row.SourceGuid);
            if (src.name != row.SourceName) throw new InvalidOperationException("ChaosWarrior: natural weapon source " + row.SourceGuid + " is named '" + src.name + "', the ledger says '" + row.SourceName + "'");
            if (src.Type == null || src.Type.AssetGuid.ToString() != row.TypeGuid) throw new InvalidOperationException("ChaosWarrior: " + src.name + " weapon type is " + (src.Type == null ? "null" : src.Type.AssetGuid.ToString()) + ", the ledger says " + row.TypeGuid);
            if (!src.IsNatural) throw new InvalidOperationException("ChaosWarrior: " + src.name + " is not a natural weapon");
            var clone = BlueprintClone.Clone(src, "ChaosWarriorWeapon" + row.MutationId, row.Guid, c => true);
            if (row.OverrideDice)
            {
                SetField(clone, "m_OverrideDamageDice", true);
                SetField(clone, "m_DamageDice", WantDice());
            }
            return clone;
        }

        // 자연 공격 주사위(개편 설계 §3-1 「전부 1d6」) — 수치는 ChaosRules.Dials 에서만.
        static DiceFormula WantDice()
        {
            return new DiceFormula(ChaosRules.Dials.NaturalAttackDiceCount, E<DiceType>("D" + ChaosRules.Dials.NaturalAttackDiceSides));
        }

        static bool SameDice(DiceFormula a, DiceFormula b) { return a.Rolls == b.Rolls && a.Dice == b.Dice; }

        // 복제본과 원본의 값 대조에서 보지 않는 최상위 칸. 정체성(일부러 다르다 — 아래에서 「다름」을 따로 확인한다) · 복제가 옮기지 않는 칸 · 따로 대조하는 칸.
        //   AssetGuid · name      우리 GUID · 우리 이름(BlueprintUtil.Create)
        //   m_AllElements         BlueprintScriptableObject 의 Element 캐시 — BlueprintClone.Clone 이 복사하지 않는 칸
        //   Components            부품 배열 — 복제본의 부품은 CloneBoundary.CopyComponent 로 만든 다른 인스턴스라 인스턴스 동일성으로 보면 언제나 차이다(독립 검토 R2-F2). 아래 ComponentPaths 가 쌍마다 값으로 대조한다.
        static readonly string[] DiffSkip = { "AssetGuid", "name", "m_AllElements", "Components" };
        // 부품 쌍 대조에서 보지 않는 부품의 최상위 칸: name — BlueprintUtil.AddComponents 가 "$형식$우리GUID#색인" 으로 바꾼다(src\Common\BlueprintUtil.cs AddComponents). 소유자 역참조(OwnerBlueprint · 행동 Element.Owner)는 건너뛰지 않고 「복제본 ↔ 원본」 짝으로 맞춘다.
        static readonly string[] ComponentDiffSkip = { "name" };
        // 부품 대조의 공유 판정: 블루프린트 · Unity 객체 · 다른 부품은 인스턴스로, 행동 Element 는 값으로(BlueprintClone.CloneElements 가 깊게 새로 만든다 — 인스턴스로 보면 거짓 차이).
        static bool KeepSharedForComponents(object v) { return v is SimpleBlueprint || v is UnityEngine.Object || v is BlueprintComponent; }

        // 실행 중 엔진이 늦게 채우는 캐시 칸(독립 검토 R2-F3 · R3-F2) — 어느 깊이에서든 대조하지 않는다. 목록의 근거 = 무기 블루프린트(BlueprintItemWeapon)에서 필드로 닿는 형식의 비직렬화 칸
        // (Assembly-CSharp dnlib 직독 2026-11-17, md5 앞 12 748b3a143b60). 정정(R3-F2): 앞 판의 「전수 11칸」(%TEMP%\cw-r2-o7\caches2.ps1)은 이름에 Blueprint 가 든 형식(참조 래퍼 BlueprintReference`1)과
        //   제네릭 인자(WeakResourceLink`1)를 건너뛰어 전수가 아니었다. 넓힌 스캔(%TEMP%\cw-r3\caches3.ps1 — 공유 형식 SimpleBlueprint · UnityEngine.Object 에서만 멈춤; 복제 원본 11종은 부품 0 이라 부품 하위 형식은 닿지 않는다)의
        //   비직렬화 칸 17개(BundledResourceHandle`1 의 3칸은 m_Handle 아래라 제외): 런타임에 늦게 쓰이는 9칸이 여기, 나머지 8칸은 이유와 함께 대조하거나 따로 다룬다 —
        //   SimpleBlueprint.m_ValidationStatus(생성자 @0006 에서만 씀) · name · m_AllElements(최상위 DiffSkip) · BlueprintComponent.OwnerBlueprint · Element.Owner(소유자 짝으로 맞춤) · WeaponVisualParameters.Prototype(ClonePolicy.TypeOwnedPrototype 이 따로 허용)
        //   · BlueprintReferenceBase.deserializedGuid 와 BlueprintGuid.m_Guid(참조 GUID 그 자체 — 직렬화 문자열에서 OnAfterDeserialize @000C 가 채우는 값이라 대조해야 한다).
        // 칸이 엔진에서 사라지면 GetField 가 null → 던진다(낡은 건너뛰기를 조용히 두지 않는다).
        static IReadOnlyCollection<string> RuntimeCacheKeys() { return CloneRuntimeCaches.Keys(); }
        // 주사위를 우리가 쓰는 행(OverrideDice)에서 원본과 달라야 하는 칸 전부 — 원본은 재정의 없음 · 주사위 칸 비어 있음(0 · Zero, zip 직독). 그 밖의 행은 다른 칸이 하나도 없어야 한다.
        static readonly string[] OverrideDicePaths = { ".m_OverrideDamageDice", ".m_DamageDice.m_Rolls", ".m_DamageDice.m_Dice" };

        // 복제 12개의 되읽기(빌드 끝 + 점검 S5c weapon-clones-readback). 빈 목록 = 일치.
        //   개수 == 원장 · GUID == 원장 · 캐시가 그 GUID 로 이 복제본을 돌려준다 · 원본과 다른 객체·GUID·이름 · 무기 형식 = 원본과 같은 인스턴스(공유)이고 GUID == 원장 · 자연 무기 · 범주 == 원장
        //   · 실제 피해 주사위(BaseDamage = 재정의 ? m_DamageDice : 형식 주사위, Damage = 크기 보정 뒤) == NaturalAttackDiceCount d NaturalAttackDiceSides
        //   · 원본과 값이 다른 칸 == 의도한 칸(CloneDiff — 그 밖의 칸이 달라졌으면 복제가 뭔가를 잃었거나 바꿨다) · 표시 문구 칸 5개가 null 이 아님(null 이면 화면에 <null>) · 부품 수 == 원본.
        public static List<string> VerifyNow()
        {
            var d = new List<string>();
            var rows = NaturalWeaponPlan.Rows;
            if (weapons.Count != rows.Count) d.Add(weapons.Count + " weapon clone(s) built, the ledger has " + rows.Count + " rows");
            var want = WantDice();
            var caches = RuntimeCacheKeys();
            foreach (var row in rows)
            {
                BlueprintItemWeapon w;
                if (!weapons.TryGetValue(row.MutationId, out w) || w == null) { d.Add(row.MutationId + ": clone not built"); continue; }
                string tag = row.MutationId + " (" + w.name + ")";
                var src = Get<BlueprintItemWeapon>(row.SourceGuid);
                if (w.AssetGuid.ToString() != row.Guid) d.Add(tag + ": GUID " + w.AssetGuid + " != ledger " + row.Guid);
                if (!ReferenceEquals(Get<BlueprintItemWeapon>(row.Guid), w)) d.Add(tag + ": the blueprint cache does not return this clone for " + row.Guid);
                if (ReferenceEquals(w, src) || w.AssetGuid == src.AssetGuid || w.name == src.name) d.Add(tag + ": the clone is not distinct from its source " + src.name + " (same object / GUID / name)");
                if (w.Type == null || !ReferenceEquals(w.Type, src.Type) || w.Type.AssetGuid.ToString() != row.TypeGuid)
                {
                    d.Add(tag + ": weapon type " + (w.Type == null ? "null" : w.Type.AssetGuid.ToString()) + " is not the source's type " + row.TypeGuid + " (shared instance)");
                    continue;   // 형식 없이는 범주·주사위를 읽을 수 없다 — 위 줄이 이미 불일치다
                }
                if (!w.IsNatural) d.Add(tag + ": not a natural weapon");
                if (w.Category.ToString() != row.Category) d.Add(tag + ": category " + w.Category + " != ledger " + row.Category);
                if (!SameDice(w.BaseDamage, want) || !SameDice(w.Damage, want)) d.Add(tag + ": damage dice base " + w.BaseDamage + " / sized " + w.Damage + ", expected " + want);
                // 값 대조의 깊이: 복사와 같은 셈이라 복사 깊이(CloneBoundary.DetachDepth) 안에서 끝난다 — 더 넉넉한 CloneBoundary.ActionDepth 를 준다(넘으면 「같다」가 아니라 던진다).
                // 소유자 짝: 복제본 쪽이 복제본 자신 · 원본 쪽이 원본 자신이면 같다(부품 OwnerBlueprint · 행동 Element.Owner).
                var cloneW = w; var srcW = src;
                Func<object, object, bool> owners = (a, b) => ReferenceEquals(a, cloneW) && ReferenceEquals(b, srcW);
                var paths = CloneDiff.Paths(w, src, BlueprintClone.KeepShared, DiffSkip, caches, owners, CloneBoundary.ActionDepth)
                    .Concat(CloneDiff.ComponentPaths(w.ComponentsArray ?? new BlueprintComponent[0], src.ComponentsArray ?? new BlueprintComponent[0], ".Components", KeepSharedForComponents, ComponentDiffSkip, caches, owners, CloneBoundary.ActionDepth))
                    .OrderBy(x => x, StringComparer.Ordinal).ToArray();
                var expected = (row.OverrideDice ? OverrideDicePaths : new string[0]).OrderBy(x => x, StringComparer.Ordinal).ToArray();
                if (!paths.SequenceEqual(expected)) d.Add(tag + ": differs from " + src.name + " at [" + string.Join(", ", paths) + "], expected exactly [" + string.Join(", ", expected) + "]");
                var blank = new[]
                {
                    Tuple.Create("m_DisplayNameText", GetF(w, "m_DisplayNameText")), Tuple.Create("m_DescriptionText", GetF(w, "m_DescriptionText")), Tuple.Create("m_FlavorText", GetF(w, "m_FlavorText")),
                    Tuple.Create("m_NonIdentifiedNameText", GetF(w, "m_NonIdentifiedNameText")), Tuple.Create("m_NonIdentifiedDescriptionText", GetF(w, "m_NonIdentifiedDescriptionText")),
                }.Where(x => x.Item2 == null).Select(x => x.Item1).ToArray();
                if (blank.Length != 0) d.Add(tag + ": text field(s) " + string.Join(", ", blank) + " are null (the UI would show <null>)");
                int comps = (w.ComponentsArray ?? new BlueprintComponent[0]).Length, srcComps = (src.ComponentsArray ?? new BlueprintComponent[0]).Length;
                if (comps != srcComps) d.Add(tag + ": " + comps + " component(s), the source has " + srcComps);
            }
            return d;
        }
    }

    // 엔진의 추가 사지 범주 상한 표를 읽어 문서 전사(ChaosRules.LimbCapsExpected)와의 차이를 낸다(개편 O5 — NaturalWeapons.Build 가 빌드 때 1회 정보 줄로 남긴다. 다시 뽑기 판정에는 쓰지 않는다).
    // 출처(독립 검토 R2-F4): 엔진 판정(GetWeaponSlotCount @0000~000A)이 읽는 바로 그 객체 = BlueprintRoot.Instance.PersonageLimits. BlueprintRoot.Instance = Game.Instance.BlueprintRoot(@0000~0005)이고
    //   Game.get_BlueprintRoot @0000 = ResourcesLibrary.GetRoot() — 여기서는 ResourcesLibrary.GetRoot() 를 직접 부른다. 엔진 함수를 그대로 부르지 않는 이유: Game.get_Instance @000D~002B 는
    //   Game 이 아직 없으면 새로 만든다(빌드 시점 — 블루프린트 캐시 초기화 직후 — 에 게임 객체를 만드는 부작용). 그래서 같은 객체를 엔진 함수와 같은 규칙으로 읽는다:
    //   범주가 표에 있으면 그 Count(같은 범주가 두 번이면 첫 행 — GetWeaponSlotCount @000D~003A 는 처음 맞는 행에서 돌아온다), 없으면 Dials.LimbCapNone(엔진 @003C −1). 규칙의 IL 모양은 시험 `wiring: limb cap judged by engine` 이 잠근다.
    // 루트·상한 블루프린트·표가 없거나 null 행이면 던진다(「차이 없음」으로 넘기지 않는다).
    internal static class EngineLimbCaps
    {
        internal static string InfoLine()
        {
            var root = ResourcesLibrary.GetRoot();
            if (root == null) throw new InvalidOperationException("ChaosWarrior: the blueprint root is not loaded (cannot read the engine limb caps)");
            var limits = root.PersonageLimits;
            if (limits == null) throw new InvalidOperationException("ChaosWarrior: BlueprintRoot.PersonageLimits is null (cannot read the engine limb caps)");
            var table = GetF(limits, "m_DopLimbMaxCount") as Array;
            if (table == null) throw new InvalidOperationException("ChaosWarrior: " + limits.name + " has no m_DopLimbMaxCount table (cannot compare the limb caps)");
            var engine = new Dictionary<string, int>();
            foreach (var entry in table)
            {
                if (entry == null) throw new InvalidOperationException("ChaosWarrior: " + limits.name + " has a null limb cap entry");
                string slot = GetF(entry, "Slot").ToString();
                if (!engine.ContainsKey(slot)) engine.Add(slot, (int)GetF(entry, "Count"));
            }
            return NaturalWeaponPlan.CapInfoLine(category => engine.ContainsKey(category) ? engine[category] : ChaosRules.Dials.LimbCapNone);
        }
    }

    // 복제 되읽기의 런타임 캐시 칸 목록(독립 검토 R2-F3 — 근거 주석은 NaturalWeapons.RuntimeCacheKeys 위). 리플렉션 플래그(BindingFlags)는 수치 다이얼이 아니라 이 형식에 둔다
    // (`dial: core literals` 의 검사 대상은 규칙 수치를 다루는 핵심·빌더 형식이다 — EngineLimbCaps 와 같은 자리).
    internal static class CloneRuntimeCaches
    {
        internal static IReadOnlyCollection<string> Keys()
        {
            const BindingFlags I = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var fields = new[]
            {
                // BlueprintItem.get_Enchantments @002B 가 채우고 OnEnable @0008 이 null 로 되돌린다(ClonePolicy.OnEnableWrites).
                Tuple.Create("BlueprintItem.m_CachedEnchantments", typeof(BlueprintItem).GetField("m_CachedEnchantments", I)),
                // BlueprintItemEquipment.get_UseWhileCanComponents @0056 이 처음 읽힐 때 채운다.
                Tuple.Create("BlueprintItemEquipment.m_CachedUseWhileCanComponents", typeof(BlueprintItemEquipment).GetField("m_CachedUseWhileCanComponents", I)),
                // WeaponVisualParameters.CacheLinkedModels @010C · @015D · @0164 — 무기 모형을 처음 그릴 때.
                Tuple.Create("WeaponVisualParameters.m_CachedBeltModel", typeof(WeaponVisualParameters).GetField("m_CachedBeltModel", I)),
                Tuple.Create("WeaponVisualParameters.m_CachedSheathModel", typeof(WeaponVisualParameters).GetField("m_CachedSheathModel", I)),
                Tuple.Create("WeaponVisualParameters.m_CachedEquipLinksUpToDate", typeof(WeaponVisualParameters).GetField("m_CachedEquipLinksUpToDate", I)),
                // LocalizedString.op_Implicit @004E(set_ShouldProcess) — 문구를 화면에 낼 때.
                Tuple.Create("LocalizedString.m_ShouldProcess", typeof(LocalizedString).GetField("m_ShouldProcess", I)),
            };
            var keys = new List<string>();
            foreach (var f in fields)
            {
                if (f.Item2 == null) throw new InvalidOperationException("ChaosWarrior: runtime cache field " + f.Item1 + " is not in this game build (the clone diff skip list is stale)");
                keys.Add(CloneDiff.FieldKey(f.Item2));
            }
            // Condition.Check @0057(set_LastResult) — 조건을 평가할 때(부품 행동 안). 자동 속성이라 뒷받침 칸 이름은 속성에서 만든다.
            keys.Add(BackingKey(typeof(Condition), typeof(Condition).GetProperty("LastResult", I), "Condition.LastResult"));
            // 독립 검토 R3-F2: 참조 래퍼의 블루프린트 캐시 — BlueprintReferenceBase.GetBlueprint @0000~0014(Cached 가 null 이면 deserializedGuid 로 ResourcesLibrary.TryGetBlueprint 해 set_Cached).
            //   원본 쪽 참조(형식 · 강화 · 보조 무기 …)가 게임 중에 먼저 풀리면 복제본과 이 칸만 달라 보인다(값은 deserializedGuid 에서 파생 — 그 GUID 는 계속 대조한다).
            keys.Add(BackingKey(typeof(BlueprintReferenceBase), typeof(BlueprintReferenceBase).GetProperty("Cached", I), "BlueprintReferenceBase.Cached"));
            // 독립 검토 R3-F2: 자원 링크의 적재 손잡이 — WeakResourceLink`1.Load @0045~0063(AssetId 로 BundledResourceHandle.Request → set_m_Handle) · ForceUnload @001B(null). 무기 모형 PrefabLink 를 처음 그릴 때.
            //   스캔에서 닿는 인스턴스는 WeakResourceLink<GameObject> 하나(열쇠 = 닫힌 제네릭 형식 FullName — 다른 인자의 링크가 생기면 이 열쇠로는 건너뛰지 않아 차이로 드러난다). AssetId 는 계속 대조한다.
            var link = typeof(Kingmaker.ResourceLinks.WeakResourceLink<UnityEngine.GameObject>);
            keys.Add(BackingKey(link, link.GetProperty("m_Handle", I), "WeakResourceLink<GameObject>.m_Handle"));
            return keys;
        }

        // 자동 속성의 뒷받침 칸 열쇠. 속성 · 칸이 이 게임 판에 없으면 던진다(낡은 건너뛰기를 조용히 두지 않는다).
        static string BackingKey(Type t, PropertyInfo p, string label)
        {
            const BindingFlags I = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            string backing = p == null ? null : "<" + p.Name + ">k__BackingField";
            var f = backing == null ? null : t.GetFields(I).FirstOrDefault(x => x.Name == backing);
            if (f == null) throw new InvalidOperationException("ChaosWarrior: runtime cache " + label + " (auto-property backing field) is not in this game build (the clone diff skip list is stale)");
            return CloneDiff.FieldKey(f);
        }
    }
    [HarmonyLib.HarmonyPatch(typeof(Kingmaker.UnitLogic.Commands.AttackHandInfo), "GetAnimationType")]
    internal static class NaturalAttackAnimationPatch
    {
        static void Postfix(Kingmaker.UnitLogic.Commands.AttackHandInfo __instance,
            Kingmaker.Visual.Animation.Kingmaker.UnitAnimationType __0,
            ref Kingmaker.Visual.Animation.Kingmaker.UnitAnimationSpecialAttackType __1, ref int __2)
        {
            if (__0 == Kingmaker.Visual.Animation.Kingmaker.UnitAnimationType.SpecialAttack &&
                NaturalWeapons.Owns(__instance.Weapon?.Blueprint))
                WotrClassMod.Common.NaturalAttackAnimation.Resolve(__instance, ref __1, ref __2);
        }
    }

}
