using System;
using System.Collections.Generic;
using System.Linq;

namespace ChimeraMonk
{
     
    // 단일 원장(C4): 게임 쪽 MutagenFx(빌드 시 본편 원본 능력의 AbilitySpawnFx·동작을 읽어 이 표와 대조) · Readback.MutagenDiff(되읽기) · 시험 `fx:`(blueprints.zip 직독 + 배선)가 모두 이 표를 읽는다.
    // 원본이 없거나 · 이름이 다르거나 · AbilitySpawnFx 가 정확히 1개가 아니거나 · 칸이 비었거나 · 값이 표와 다르면 빌드가 던진다(BuildFailed) — 엔진은 빈 PrefabLink 를 조용히 건너뛴다.
    // 엔진: AbilitySpawnFx(Time = OnApplyEffect, Anchor = Caster)는 능력이 효과를 낼 때 시전자에게 한 번 FX 를 띄운다(본편 MutagenStrength 와 같은 칸 값).
    // FX·동작만 더한다 — 변이약 수치·지속·자원은 건드리지 않는다. 변이약 버프 3 에는 아무것도 넣지 않는다(본편 MutagenStrengthBuff 도 FX 칸이 비어 있다 — 시험이 확인).
    public static class FxTable
    {
        // 원본(유일). 값 = blueprints.zip 직독(2026-11-15, scratchpad fx-recon/fxdump.py).
        public const string SourceGuid = "7af42862f58edcc4cb825862ff2a0d10";
        public const string SourceName = "MutagenStrength";
        public const string SourceJbp = "Classes/Alchemist/Mutagens/MutagenStrength.jbp";
        public const string Slot = "AbilitySpawnFx.PrefabLink";
        public const string AssetId = "3b42d149d55dfe5469d9967f22d7e020";
        public const string SpawnTime = "OnApplyEffect";   // 같은 컴포넌트의 Time(jbp) — 효과가 날 때
        public const string SpawnAnchor = "Caster";        // 같은 컴포넌트의 Anchor(jbp) — 마신 사람(= 시전자, Personal)
        public const string Animation = "Potion";          // 원본 능력의 Animation(UnitAnimationActionCastSpell.CastAnimationStyle)
        public const string AnimationStyle = "CastActionPotion"; // 원본 능력의 AnimationStyle(Kingmaker.View.Animation.CastAnimationStyle)
        public const string SourceReason = "연금술사 힘 변이약을 마시는 FX·동작 — 키메라 변이약도 변이약을 마신다(승인 2026-11-15). 민첩·지혜도 같은 원본(승인 범위 그대로)";
        public const string SourceBuffJbp = "Classes/Alchemist/Mutagens/MutagenStrengthBuff.jbp"; // 원본의 버프 — FxOnStart/FxOnRemove 가 비어 있음(그래서 우리 버프에도 넣지 않는다)

        // FX 를 받는 우리 능력(변이약 3).
        public static readonly IReadOnlyList<string> Targets = new[] { Guids.MutagenAbilityStrength, Guids.MutagenAbilityDexterity, Guids.MutagenAbilityWisdom };

        static bool Hex32(string s) { return s != null && s.Length == 32 && s.All(c => c >= '0' && c <= '9' || c >= 'a' && c <= 'f'); }

        // 표 무결성(C2) — 빈 목록 = 일치. own = 우리 블루프린트 GUID.
        public static List<string> Problems(IEnumerable<string> own)
        {
            var p = new List<string>();
            var ownSet = new HashSet<string>(own ?? new string[0]);
            if (ownSet.Count == 0) p.Add("own GUID set is empty");
            if (!Hex32(SourceGuid) || !Hex32(AssetId)) p.Add("FX source / asset id is not 32 lowercase hex");
            if (ownSet.Contains(SourceGuid)) p.Add("FX source is one of our own blueprints (needs a vanilla source)");
            if (Targets.Count == 0) p.Add("no FX targets");
            if (Targets.Distinct().Count() != Targets.Count) p.Add("duplicate FX target");
            foreach (var t in Targets) if (!ownSet.Contains(t)) p.Add("FX target " + t + " is not one of our blueprints");
            foreach (var s in new[] { SpawnTime, SpawnAnchor, Animation, AnimationStyle, SourceName, SourceJbp, SourceReason }) if (string.IsNullOrEmpty(s)) p.Add("FX table has an empty value");
            return p;
        }
    }
}
