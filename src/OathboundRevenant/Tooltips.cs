using System;
using System.Linq;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Localization;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace OathboundRevenant
{
    // 2026-11-16 결함 수리(툴팁 `<null>`): 코드로 만든 블루프린트의 LocalizedString 필드를 null 로 두지 않는다.
    // 엔진 근거 = src\Common\BlueprintUtil.cs 「LocalizedString 필드」 주석(null 참조는 "<null>" 로 찍히고, 빈 키 인스턴스만 칸이 숨는다).
    // 값의 원장 = TooltipPlan(순수). 여기는 그 값을 넣고(Ability·NoShortDescription) 빌드 끝에 전수로 되읽는다(Require — BuildAll 의 Register 바로 앞).
    public static class Tooltips
    {
        public static bool Checked { get; private set; }
        public static int CheckedCount { get; private set; }

        // 팩 키 → LocalizedString. TooltipPlan.Hidden = 빈 키 인스턴스(칸 숨김), 그 밖 = 본편 팩 키(형식은 GameText 가 검사).
        static LocalizedString Text(string key)
        {
            if (key == null) throw new ArgumentNullException("key");
            return key == TooltipPlan.Hidden ? NoText() : GameText(key);
        }

        // 새로 만드는 능력의 지속·내성 칸(Create 의 init 안에서 부른다). 복사 능력(TooltipPlan.Copied)에 부르면 던진다 — 그쪽은 본편 값이 복사된다.
        internal static void Ability(BlueprintAbility a)
        {
            if (a == null) throw new ArgumentNullException("a");
            var row = TooltipPlan.Row(a.AssetGuid.ToString());
            if (row.Origin != TooltipPlan.Made) throw new InvalidOperationException("OathboundRevenant: Tooltips.Ability on " + a.name + " (TooltipPlan origin " + row.Origin + ", expected " + TooltipPlan.Made + ")");
            a.LocalizedDuration = Text(row.Duration);
            a.LocalizedSavingThrow = Text(row.Save);
        }

        // 짧은 설명 칸: 본편 jbp 는 전부 빈 키다(채널·안수·공포 주문·유대 버프/선택지 직독 2026-11-16). 우리도 「문구 없음」을 명시한다.
        internal static void NoShortDescription(BlueprintUnitFact f)
        {
            if (f == null) throw new ArgumentNullException("f");
            SetField(f, "m_DescriptionShort", NoText());
        }

        // BuildAll 의 마지막 빌더(Register 바로 앞): ① 우리가 Create 로 만든 모든 블루프린트의 LocalizedString 필드 null 금지(형식 사슬 전수),
        // ② 우리 능력 집합 == TooltipPlan, ③ 만든 능력의 두 칸 == 표(키 일치·Shared 없음). 어긋나면 던진다 → 아키타입을 등록하지 않는다.
        public static void Require()
        {
            if (Checked) throw new InvalidOperationException("OathboundRevenant: Tooltips.Require() called twice");
            int n = RequireOwnLocalizedStrings();
            var abilities = OwnBlueprints.OfType<BlueprintAbility>().ToList();
            var problems = TooltipPlan.Problems(abilities.Select(a => a.AssetGuid.ToString()));
            if (problems.Count != 0) throw new InvalidOperationException("OathboundRevenant: TooltipPlan does not match the built abilities: " + string.Join(" | ", problems.ToArray()));
            foreach (var a in abilities)
            {
                var row = TooltipPlan.Row(a.AssetGuid.ToString());
                if (row.Origin != TooltipPlan.Made) continue;   // 복사 능력: null 아님은 ① 이 봤다. 값은 본편 것.
                var d = a.LocalizedDuration;
                var s = a.LocalizedSavingThrow;
                if (d == null || s == null || d.Key != row.Duration || s.Key != row.Save || d.Shared != null || s.Shared != null)
                    throw new InvalidOperationException("OathboundRevenant: " + a.name + " tooltip string readback failed (LocalizedDuration key '" + (d == null ? "null" : d.Key) + "', expected '" + row.Duration
                        + "'; LocalizedSavingThrow key '" + (s == null ? "null" : s.Key) + "', expected '" + row.Save + "'; both non-null without a shared asset)");
            }
            CheckedCount = n;
            Checked = true;
        }
    }
}
