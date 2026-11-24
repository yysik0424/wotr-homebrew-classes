using System;
using System.Collections.Generic;
using System.Linq;

namespace ChimeraMonk
{
     
    // 게임 형식 없음 → 시험 exe 가 실행한다(`exec: dependency gate representative`). 게임 쪽 해석기·보고 출구는 호출자가 넘긴다.
    public sealed class Dependency
    {
        public readonly string Guid, Name, Kind;
        public Dependency(string guid, string name, string kind)
        {
            if (string.IsNullOrEmpty(guid) || guid.Length != 32) throw new ArgumentException("ChimeraMonk: dependency GUID must be 32 hex chars: '" + guid + "'", "guid");
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("ChimeraMonk: dependency without a name (" + guid + ")", "name");
            if (string.IsNullOrEmpty(kind)) throw new ArgumentException("ChimeraMonk: dependency without a kind (" + guid + ")", "kind");
            Guid = guid; Name = name; Kind = kind;
        }
    }

    public static class DependencyGate
    {
        // 종류 이름(시험 전사본과 대조).
        public const string KindFct = "fct";
        public const string KindAspectFeature = "aspect-feature";
        public const string KindAspectAbility = "aspect-ability";
        public const string KindAspectBuff = "aspect-buff";
        public const string KindAspectGraph = "aspect-graph";
        public const string KindVisualBundle = "visual-bundle"; // 2026-11-16 변이 외형 묶음(KingmakerEquipmentEntity 블루프린트)

        static Dependency D(string g, string n, string k) { return new Dependency(g, n, k); }

        // 39행 = FCT 1 + 형태 기능 10 + 형태 능력 10 + 형태 버프 10 + 형태 그래프 하위 6(부록 A-h) + 외형 묶음 2(소유자 결정 2026-11-16 3번, 정찰 6절). 순서 = 형태 표 순서(부록 A-b), 외형은 끝.
        static readonly Dependency[] required =
        {
            D(Guids.External.FeralCombatTraining, Guids.External.FeralCombatTrainingName, KindFct),
            D(Guids.Game.AspectBear, "ShifterAspectBear", KindAspectFeature),
            D(Guids.Game.AspectBoar, "ShifterAspectBoar", KindAspectFeature),
            D(Guids.Game.AspectDinosaur, "ShifterAspectDinosaur", KindAspectFeature),
            D(Guids.Game.AspectElephant, "ShifterAspectElephant", KindAspectFeature),
            D(Guids.Game.AspectHorse, "ShifterAspectHorse", KindAspectFeature),
            D(Guids.Game.AspectLizard, "ShifterAspectLizard", KindAspectFeature),
            D(Guids.Game.AspectSpider, "ShifterAspectSpider", KindAspectFeature),
            D(Guids.Game.AspectTiger, "ShifterAspectTiger", KindAspectFeature),
            D(Guids.Game.AspectWolf, "ShifterAspectWolf", KindAspectFeature),
            D(Guids.Game.AspectWolverine, "ShifterWolverineProgression", KindAspectFeature),
            D(Guids.Game.AspectBearAbility, "ShifterAspectBearAbility", KindAspectAbility),
            D(Guids.Game.AspectBoarAbility, "ShifterAspectBoarAbility", KindAspectAbility),
            D(Guids.Game.AspectDinosaurAbility, "ShifterAspectDinosaurAbility", KindAspectAbility),
            D(Guids.Game.AspectElephantAbility, "ShifterAspectElephantAbility", KindAspectAbility),
            D(Guids.Game.AspectHorseAbility, "ShifterAspectHorseAbility", KindAspectAbility),
            D(Guids.Game.AspectLizardAbility, "ShifterAspectLizardAbility", KindAspectAbility),
            D(Guids.Game.AspectSpiderAbility, "ShifterAspectSpiderAbility", KindAspectAbility),
            D(Guids.Game.AspectTigerAbility, "ShifterAspectTigerAbility", KindAspectAbility),
            D(Guids.Game.AspectWolfAbility, "ShifterAspectWolfAbility", KindAspectAbility),
            D(Guids.Game.AspectWolverineAbility, "ShifterAspectWolverineAbility", KindAspectAbility),
            D(Guids.Game.AspectBearBuff, "ShifterAspectBearBuff", KindAspectBuff),
            D(Guids.Game.AspectBoarBuff, "ShifterAspectBoarBuff", KindAspectBuff),
            D(Guids.Game.AspectDinosaurBuff, "ShifterAspectDinosaurBuff", KindAspectBuff),
            D(Guids.Game.AspectElephantBuff, "ShifterAspectElephantBuff", KindAspectBuff),
            D(Guids.Game.AspectHorseBuff, "ShifterAspectHorseBuff", KindAspectBuff),
            D(Guids.Game.AspectLizardBuff, "ShifterAspectLizardBuff", KindAspectBuff),
            D(Guids.Game.AspectSpiderBuff, "ShifterAspectSpiderBuff", KindAspectBuff),
            D(Guids.Game.AspectTigerBuff, "ShifterAspectTigerBuff", KindAspectBuff),
            D(Guids.Game.AspectWolfBuff, "ShifterAspectWolfBuff", KindAspectBuff),
            D(Guids.Game.AspectWolverineBuff, "ShifterAspectWolverineBuff", KindAspectBuff),
            D(Guids.Game.WolfScentUpgrade, "ShifterAspectWolfScentUpgrade", KindAspectGraph),
            D(Guids.Game.WolfScent10, "ShifterAspectWolfScent10", KindAspectGraph),
            D(Guids.Game.WolfScent20, "ShifterAspectWolfScent20", KindAspectGraph),
            D(Guids.Game.WolfScent30, "ShifterAspectWolfScent30", KindAspectGraph),
            D(Guids.Game.Diehard, "Diehard", KindAspectGraph),
            D(Guids.Game.Endurance, "Endurance", KindAspectGraph),
            D(Guids.Game.KeeTailMythicDragonAny, "KEE_Tail_TailMythicDragon_Any", KindVisualBundle),
            D(Guids.Game.KeeHornsMythicDevilSmall, "KEE_Horns_HornsMythicDevilSmall", KindVisualBundle),
        };

        public static IReadOnlyList<Dependency> Required { get { return required; } }

        public const string Where = "dependency";

        // false = 하나라도 없음. 없는 것마다 report(Where, guid + " " + name) 1회 — 첫 결측에서 멈추지 않는다(모든 결측이 로그에 남는다).
        // 빈 deps·null 원소·null 해석기/보고기 = 던진다(검사할 것이 없는 관문은 「통과」가 아니다, C1).
        public static bool RunOn(IReadOnlyList<Dependency> deps, Func<string, bool> resolves, Action<string, string> report)
        {
            if (deps == null) throw new ArgumentNullException("deps");
            if (resolves == null) throw new ArgumentNullException("resolves");
            if (report == null) throw new ArgumentNullException("report");
            if (deps.Count == 0) throw new ArgumentException("ChimeraMonk: dependency gate with an empty dependency list", "deps");
            if (deps.Any(d => d == null)) throw new ArgumentException("ChimeraMonk: dependency list has a null entry", "deps");
            bool all = true;
            foreach (var d in deps)
            {
                if (resolves(d.Guid)) continue;
                all = false;
                report(Where, d.Guid + " " + d.Name);
            }
            return all;
        }

        public static bool Run(Func<string, bool> resolves, Action<string, string> report) { return RunOn(Required, resolves, report); }
    }
}
