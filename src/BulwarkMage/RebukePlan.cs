using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BulwarkMage
{
     
    // 단일 원장(C4): 게임 쪽 Rebuke.Build 는 이 표의 노드(역할·효과·이름·GUID)대로 38개를 만들고, 만든 뒤 RebukeReadback.Describe(실제 블루프린트) == ExpectedLines(노드)
    // 를 줄 단위로 대조한다(틀리면 빌드 실패). 시험 `exec: rebuke blueprint plan` 은 이 표를 시험 파일의 독립 전사본(설계 표에서 손으로 옮김)과 대조한다.
    // 줄 문법(되읽기 문법): `type=…` · 블루프린트 필드 `f=…` · `components=T1,T2` · 컴포넌트 필드 `c<i>.f=…` · 행동 목록 `…#=<개수>` + `…[k]=<형식>` + `…[k].f=…`(재귀).
    //   값: 참조 = 32자 소문자 GUID(없으면 null) · bool = True/False · 열거 = 이름 · ContextValue = Simple:<n> / Rank:<ValueRank> · 주사위 = <Dice>x<개수 값>+<보너스 값> ·
    //   지속 = <Rate>:<Dice>x<개수 값>+<보너스 값> · Custom 진행표 = base:value,… · 참조 배열 = GUID,GUID.
     
    public enum RebukeRole { Resource, BaseFeature, Selection, Learn, Toggle, ToggleBuff, Gate, Ready, Spent }

    public sealed class RebukeNode
    {
        public readonly RebukeRole Role;
        public readonly string EffectId;   // 공용 3개는 null
        public readonly string Name;
        public readonly string Guid;
        public readonly string BlueprintType;   // 짧은 형식 이름
        public readonly IReadOnlyList<string> References;   // 이 노드가 가리키는 GUID(그래프 간선)
        public RebukeNode(RebukeRole role, string effectId, string name, string guid, string type, IEnumerable<string> refs)
        {
            Role = role; EffectId = effectId; Name = name; Guid = guid; BlueprintType = type; References = refs.ToArray();
        }
        public override string ToString() { return Role + (EffectId == null ? "" : "/" + EffectId) + " " + Name + " " + Guid; }
    }

    public static class RebukePlan
    {
        // 불발 사유 이름(게임 쪽 DudReason 열거 값 이름 — 설계 §R4.1 「불발 범위」 기본값 = 공격자 쓰러짐 하나).
        public const string DudReasonAttackerDown = "AttackerDown";
        // A4(2026-11-14 아침): 해제 반격의 대상 불가 사전 차단 사유(DudReason.Untargetable 의 이름).
        public const string DudReasonUntargetable = "Untargetable";

        // 공유 노드(효과마다가 아닌 것) — 무결성의 「역할당 1개」 규칙이 이 목록을 본다.
        static readonly RebukeRole[] SharedRoles = { RebukeRole.Resource, RebukeRole.BaseFeature, RebukeRole.Selection, RebukeRole.Spent };
        public static bool IsShared(RebukeRole role) { return Array.IndexOf(SharedRoles, role) >= 0; }

        // 효과 ID → (배움, 토글, 토글 버프, 문, 준비됨) GUID. 효과 순서 = RebukeTable.Effects 순서(E1..E7).
        static readonly Dictionary<string, string[]> EffectGuids = new Dictionary<string, string[]>
        {
            ["E1"] = new[] { Guids.RebukeE1Learn, Guids.RebukeE1Toggle, Guids.RebukeE1ToggleBuff, Guids.RebukeE1Gate, Guids.RebukeE1Ready },
            ["E2"] = new[] { Guids.RebukeE2Learn, Guids.RebukeE2Toggle, Guids.RebukeE2ToggleBuff, Guids.RebukeE2Gate, Guids.RebukeE2Ready },
            ["E3"] = new[] { Guids.RebukeE3Learn, Guids.RebukeE3Toggle, Guids.RebukeE3ToggleBuff, Guids.RebukeE3Gate, Guids.RebukeE3Ready },
            ["E4"] = new[] { Guids.RebukeE4Learn, Guids.RebukeE4Toggle, Guids.RebukeE4ToggleBuff, Guids.RebukeE4Gate, Guids.RebukeE4Ready },
            ["E5"] = new[] { Guids.RebukeE5Learn, Guids.RebukeE5Toggle, Guids.RebukeE5ToggleBuff, Guids.RebukeE5Gate, Guids.RebukeE5Ready },
            ["E6"] = new[] { Guids.RebukeE6Learn, Guids.RebukeE6Toggle, Guids.RebukeE6ToggleBuff, Guids.RebukeE6Gate, Guids.RebukeE6Ready },
            ["E7"] = new[] { Guids.RebukeE7Learn, Guids.RebukeE7Toggle, Guids.RebukeE7ToggleBuff, Guids.RebukeE7Gate, Guids.RebukeE7Ready },
        };

        static readonly RebukeRole[] PerEffectRoles = { RebukeRole.Learn, RebukeRole.Toggle, RebukeRole.ToggleBuff, RebukeRole.Gate, RebukeRole.Ready };

        public static readonly IReadOnlyList<RebukeNode> Nodes = Build();

        public static string GuidOf(RebukeRole role, string effectId)
        {
            switch (role)
            {
                case RebukeRole.Resource: return Guids.RebukeResource;
                case RebukeRole.BaseFeature: return Guids.RebukeBaseFeature;
                case RebukeRole.Selection: return Guids.RebukeSelection;
                case RebukeRole.Spent: return Guids.RebukeSpent;
            }
            string[] g;
            if (effectId == null || !EffectGuids.TryGetValue(effectId, out g)) throw new KeyNotFoundException("BulwarkMage: no rebuke GUIDs for effect '" + effectId + "'");
            int i = Array.IndexOf(PerEffectRoles, role);
            if (i < 0) throw new ArgumentOutOfRangeException("role", role, "BulwarkMage: unknown rebuke role");
            return g[i];
        }

        static string TypeOf(RebukeRole role)
        {
            switch (role)
            {
                case RebukeRole.Resource: return "BlueprintAbilityResource";
                case RebukeRole.Selection: return "BlueprintFeatureSelection";
                case RebukeRole.Toggle: return "BlueprintActivatableAbility";
                case RebukeRole.ToggleBuff: case RebukeRole.Ready: case RebukeRole.Spent: return "BlueprintBuff";
                case RebukeRole.BaseFeature: case RebukeRole.Learn: case RebukeRole.Gate: return "BlueprintFeature";
            }
            throw new ArgumentOutOfRangeException("role", role, "BulwarkMage: unknown rebuke role");
        }

        static RebukeNode[] Build()
        {
            var effects = RebukeTable.Effects;
            if (effects == null || effects.Count == 0) throw new InvalidOperationException("BulwarkMage: RebukeTable.Effects is empty");
            if (!effects.Select(e => e.Id).OrderBy(x => x, StringComparer.Ordinal).SequenceEqual(EffectGuids.Keys.OrderBy(x => x, StringComparer.Ordinal)))
                throw new InvalidOperationException("BulwarkMage: RebukePlan GUID table effects [" + string.Join(",", EffectGuids.Keys.ToArray()) + "] != RebukeTable effects [" + string.Join(",", effects.Select(e => e.Id).ToArray()) + "]");
            var list = new List<RebukeNode>
            {
                new RebukeNode(RebukeRole.Resource, null, "BulwarkRebukeResource", Guids.RebukeResource, TypeOf(RebukeRole.Resource), new string[0]),
                new RebukeNode(RebukeRole.BaseFeature, null, "BulwarkRebukeBaseFeature", Guids.RebukeBaseFeature, TypeOf(RebukeRole.BaseFeature), new[] { Guids.RebukeResource, Guids.RebukeSpent }),
                new RebukeNode(RebukeRole.Selection, null, "BulwarkRebukeSelection", Guids.RebukeSelection, TypeOf(RebukeRole.Selection), effects.Select(e => GuidOf(RebukeRole.Learn, e.Id))),
                new RebukeNode(RebukeRole.Spent, null, "BulwarkRebukeSpent", Guids.RebukeSpent, TypeOf(RebukeRole.Spent), new string[0]),
            };
            foreach (var e in effects)
            {
                string id = e.Id;
                Func<RebukeRole, string> g = r => GuidOf(r, id);
                list.Add(new RebukeNode(RebukeRole.Learn, id, "BulwarkRebuke" + id + "Learn", g(RebukeRole.Learn), TypeOf(RebukeRole.Learn), new[] { g(RebukeRole.Toggle) }));
                list.Add(new RebukeNode(RebukeRole.Toggle, id, "BulwarkRebuke" + id + "Toggle", g(RebukeRole.Toggle), TypeOf(RebukeRole.Toggle), new[] { g(RebukeRole.ToggleBuff), Guids.RebukeResource }));
                list.Add(new RebukeNode(RebukeRole.ToggleBuff, id, "BulwarkRebuke" + id + "ToggleBuff", g(RebukeRole.ToggleBuff), TypeOf(RebukeRole.ToggleBuff), new[] { g(RebukeRole.Gate) }));
                list.Add(new RebukeNode(RebukeRole.Gate, id, "BulwarkRebuke" + id + "Gate", g(RebukeRole.Gate), TypeOf(RebukeRole.Gate), new[] { g(RebukeRole.Ready) }));
                var readyRefs = new List<string> { Guids.Game.WizardClass, Guids.RebukeResource, g(RebukeRole.Ready), Guids.RebukeSpent };
                if (e.BuffGuid != null) readyRefs.Add(e.BuffGuid);
                if (e.CastSpellGuid != null) readyRefs.Add(e.CastSpellGuid);
                list.Add(new RebukeNode(RebukeRole.Ready, id, "BulwarkRebuke" + id + "Ready", g(RebukeRole.Ready), TypeOf(RebukeRole.Ready), readyRefs));
            }
            var arr = list.ToArray();
            var problems = Integrity(arr);
            if (problems.Count != 0) throw new InvalidOperationException("BulwarkMage: rebuke blueprint plan is inconsistent: " + string.Join(" | ", problems.ToArray()));
            return arr;
        }

        // 참조 무결성(C2): GUID·이름 중복 없음, 모든 간선이 표 안의 노드 또는 Guids.Game 의 본편 GUID 를 가리킨다, 역할별 개수 = 공용 1 + 효과마다 1.
        public static List<string> Integrity(IReadOnlyList<RebukeNode> nodes)
        {
            var p = new List<string>();
            if (nodes == null) { p.Add("no nodes"); return p; }
            var guids = new HashSet<string>(nodes.Select(n => n.Guid));
            if (guids.Count != nodes.Count) p.Add("duplicate GUID");
            if (nodes.Select(n => n.Name).Distinct().Count() != nodes.Count) p.Add("duplicate name");
            var vanilla = new HashSet<string>(typeof(Guids.Game).GetFields().Where(f => f.IsLiteral).Select(f => (string)f.GetRawConstantValue()));
            foreach (var n in nodes)
                foreach (var r in n.References)
                    if (!guids.Contains(r) && !vanilla.Contains(r)) p.Add(n.Name + " -> " + r + " dangles");
            int effects = nodes.Where(n => n.EffectId != null).Select(n => n.EffectId).Distinct().Count();
            foreach (RebukeRole role in Enum.GetValues(typeof(RebukeRole)))
            {
                int want = IsShared(role) ? 1 : effects;
                int got = nodes.Count(n => n.Role == role);
                if (got != want) p.Add(role + " x" + got + " (expected " + want + ")");
            }
            return p;
        }

        public static RebukeNode Node(RebukeRole role, string effectId)
        {
            var hit = Nodes.Where(n => n.Role == role && n.EffectId == effectId).ToArray();
            if (hit.Length != 1) throw new KeyNotFoundException("BulwarkMage: rebuke node " + role + "/" + effectId + " is in the plan " + hit.Length + " time(s)");
            return hit[0];
        }

        // ---------------- 효과의 진행표 규칙 ----------------
        // 준비됨 버프의 ContextRankConfig 하나가 무엇을 재는가: 주사위 효과 = 주사위 개수, 아니면 레벨마다 바뀌는 버프 라운드, 둘 다 바뀌면 지원 안 함(던짐).
        public static Func<int, int> RankFunction(RebukeSpec s)
        {
            if (s == null) throw new ArgumentNullException("s");
            bool roundsVary = s.BuffRounds != null && ConstantRounds(s) == null;
            if (s.DiceCount != null && roundsVary) throw new InvalidOperationException("BulwarkMage: rebuke " + s.Id + " needs two rank progressions (dice and rounds) - one ContextRankConfig only");
            if (s.DiceCount != null) return s.DiceCount;
            if (roundsVary) return s.BuffRounds;
            return null;
        }

        // 버프 라운드가 1..20 모두 같으면 그 값, 아니면 null(Rank 로 잰다). 버프 없는 효과는 null.
        public static int? ConstantRounds(RebukeSpec s)
        {
            if (s == null) throw new ArgumentNullException("s");
            if (s.BuffRounds == null) return null;
            var vals = Enumerable.Range(BulwarkRules.Levels.MinLevel, BulwarkRules.Levels.MaxLevel - BulwarkRules.Levels.MinLevel + 1).Select(s.BuffRounds).Distinct().ToArray();
            return vals.Length == 1 ? vals[0] : (int?)null;
        }

        // ---------------- 줄 도우미 ----------------
        public static string Simple(int v) { return "Simple:" + v.ToString(CultureInfo.InvariantCulture); }
        public const string RankDefault = "Rank:Default";
        public static string B(bool v) { return v ? "True" : "False"; }
        public static string Dice(string die, string count, string bonus) { return die + "x" + count + "+" + bonus; }
        public static string Duration(string rate, string die, string count, string bonus) { return rate + ":" + die + "x" + count + "+" + bonus; }
        public static string Progression((int baseValue, int progressionValue)[] rows) { return string.Join(",", rows.Select(r => r.baseValue.ToString(CultureInfo.InvariantCulture) + ":" + r.progressionValue.ToString(CultureInfo.InvariantCulture)).ToArray()); }

        static void ApplyBuff(List<string> l, string p, string buff, bool permanent, string duration, bool asChild, bool toCaster = false)
        {
            l.Add(p + "=ContextActionApplyBuff");
            l.Add(p + ".m_Buff=" + buff);
            l.Add(p + ".Permanent=" + B(permanent));
            l.Add(p + ".UseDurationSeconds=False");
            l.Add(p + ".DurationValue=" + duration);
            l.Add(p + ".IsFromSpell=False");
            l.Add(p + ".IsNotDispelable=False");
            l.Add(p + ".ToCaster=" + B(toCaster));
            l.Add(p + ".AsChild=" + B(asChild));
            l.Add(p + ".SameDuration=False");
        }

        static readonly string NoDuration = Duration("Rounds", "Zero", Simple(0), Simple(0));

        static void DealDamage(List<string> l, string p, RebukeSpec s, bool halfIfSaved)
        {
            l.Add(p + "=ContextActionDealDamage");
            l.Add(p + ".m_Type=Damage");
            if (s.Energy == RebukeEnergy.Force) l.Add(p + ".DamageType.Type=Force");
            else if (s.Energy == RebukeEnergy.Electricity || s.Energy == RebukeEnergy.Cold) { l.Add(p + ".DamageType.Type=Energy"); l.Add(p + ".DamageType.Energy=" + s.Energy); }
            else throw new InvalidOperationException("BulwarkMage: rebuke " + s.Id + " damage energy " + s.Energy + " has no line form");
            l.Add(p + ".Value=" + Dice(s.Die.ToString(), RankDefault, Simple(0)));
            l.Add(p + ".Half=False");
            l.Add(p + ".HalfIfSaved=" + B(halfIfSaved));
            l.Add(p + ".IgnoreCritical=True");
        }

        static void SavingThrow(List<string> l, string p, string type)
        {
            l.Add(p + "=ContextActionSavingThrow");
            l.Add(p + ".Type=" + type);
            l.Add(p + ".HasCustomDC=False");
            l.Add(p + ".UseDCFromContextSavingThrow=False");
            l.Add(p + ".FromBuff=False");
        }

        // 적에게 거는 버프: 실패 시에만(ContextActionConditionalSaved.Failed), AsChild = false(계획 I07), 지속 = 고정 라운드(Simple) 또는 Rank.
        static void BuffOnFailedSave(List<string> l, string p, RebukeSpec s, string saveType)
        {
            SavingThrow(l, p, saveType);
            l.Add(p + ".Actions#=1");
            string q = p + ".Actions[0]";
            l.Add(q + "=ContextActionConditionalSaved");
            l.Add(q + ".Succeed#=0");
            l.Add(q + ".Failed#=1");
            int? fixedRounds = ConstantRounds(s);
            string rounds = fixedRounds.HasValue ? Simple(fixedRounds.Value) : RankDefault;
            ApplyBuff(l, q + ".Failed[0]", s.BuffGuid, false, Duration("Rounds", "Zero", Simple(0), rounds), false);
        }

        // 생존 분기의 효과 목록(A3·A4 판정 2026-11-14): 효과 행동 묶음을 결과 관측 행동 ContextActionRebukeEffect{EffectId, Actions} 하나로 감싼다.
        //   E7 만 앞에 해제 대상 사전 차단: Conditional{ContextConditionRebukeCanTarget(해제 주문)} → [관측(시전)] / [불발 기록 Untargetable]
        //   — 본편 ContextActionCastSpell 이 대상 불가일 때 내는 PFLog 오류(RunAction @0157~01A5)에 닿지 않는다.
        public static List<string> EffectLines(RebukeSpec s, string p)
        {
            if (s == null) throw new ArgumentNullException("s");
            var l = new List<string> { p + "#=1" };
            string w = p + "[0]";
            if (s.CastSpellGuid != null)
            {
                l.Add(w + "=Conditional");
                l.Add(w + ".ConditionsChecker.Operation=And");
                l.Add(w + ".ConditionsChecker.Conditions#=1");
                l.Add(w + ".ConditionsChecker.Conditions[0]=ContextConditionRebukeCanTarget");
                l.Add(w + ".ConditionsChecker.Conditions[0].Not=False");
                l.Add(w + ".ConditionsChecker.Conditions[0].m_Spell=" + s.CastSpellGuid);
                l.Add(w + ".IfTrue#=1");
                Observed(l, w + ".IfTrue[0]", s);
                l.Add(w + ".IfFalse#=1");
                l.Add(w + ".IfFalse[0]=ContextActionLogRebukeDud");
                l.Add(w + ".IfFalse[0].Reason=" + DudReasonUntargetable);
                l.Add(w + ".IfFalse[0].EffectId=" + s.Id);
                return l;
            }
            Observed(l, w, s);
            return l;
        }

        static void Observed(List<string> l, string p, RebukeSpec s)
        {
            l.Add(p + "=ContextActionRebukeEffect");
            l.Add(p + ".EffectId=" + s.Id);
            l.AddRange(InnerEffectLines(s, p + ".Actions"));
        }

         
        public static List<string> InnerEffectLines(RebukeSpec s, string p)
        {
            if (s == null) throw new ArgumentNullException("s");
            var l = new List<string>();
            switch (s.Id)
            {
                case "E1":
                    l.Add(p + "#=1");
                    DealDamage(l, p + "[0]", s, false);
                    break;
                case "E2":
                    l.Add(p + "#=1");
                    SavingThrow(l, p + "[0]", "Reflex");
                    l.Add(p + "[0].Actions#=1");
                    DealDamage(l, p + "[0].Actions[0]", s, true);
                    break;
                case "E3":
                    l.Add(p + "#=2");
                    DealDamage(l, p + "[0]", s, false);
                    BuffOnFailedSave(l, p + "[1]", s, "Fortitude");
                    break;
                case "E4":
                    if (s.PushFeet == null) throw new InvalidOperationException("BulwarkMage: rebuke E4 without a push distance");
                    l.Add(p + "#=1");
                    l.Add(p + "[0]=ContextActionPush");
                    l.Add(p + "[0].Distance=" + Simple(s.PushEngineDistance.Value));
                    l.Add(p + "[0].ProvokeAttackOfOpportunity=False");
                    break;
                case "E5":
                case "E6":
                    l.Add(p + "#=1");
                    BuffOnFailedSave(l, p + "[0]", s, "Will");
                    break;
                case "E7":
                    l.Add(p + "#=1");
                    l.Add(p + "[0]=ContextActionCastSpell");
                    l.Add(p + "[0].m_Spell=" + s.CastSpellGuid);
                    l.Add(p + "[0].OverrideDC=False");
                    l.Add(p + "[0].OverrideSpellLevel=False");
                    l.Add(p + "[0].CastByTarget=False");
                    l.Add(p + "[0].MarkAsChild=False");
                    l.Add(p + "[0].LogIfCanNotTarget=False");
                    break;
                default:
                    throw new InvalidOperationException("BulwarkMage: rebuke effect " + s.Id + " has no action plan");
            }
            // FX(2026-11-15 승인, FxTable 원장): 목록 끝에 ContextActionSpawnFx 하나 — 개수 줄 + 1, 기존 위치는 그대로(Rebuke.WithFx 와 같은 규칙).
            var fx = FxTable.ForRebuke(s.Id);
            if (fx != null)
            {
                string head = p + "#=";
                if (l.Count == 0 || !l[0].StartsWith(head, StringComparison.Ordinal)) throw new InvalidOperationException("BulwarkMage: rebuke " + s.Id + " action plan does not start with its count line");
                int n = int.Parse(l[0].Substring(head.Length), System.Globalization.CultureInfo.InvariantCulture);
                l[0] = head + (n + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
                string q = p + "[" + n.ToString(System.Globalization.CultureInfo.InvariantCulture) + "]";
                l.Add(q + "=ContextActionSpawnFx");
                l.Add(q + ".PrefabLink.AssetId=" + fx.AssetId);
            }
            return l;
        }

         
        // 소모가 효과보다 앞이라 효과 행동이 예외로 끊겨도(ActionList.Run 은 예외를 삼키고 다음으로 간다) 사용 처리가 이미 끝나 있다.
        // 표지(BulwarkRebukeSpent)는 시전자에게 붙고, 1레벨 자원 특성의 NewRound 가 뗀다 = 시전자 자신의 라운드 틱(UnitTicksController.TickNextRound — 실시간 TickOnUnit 6초,
        //   턴제 TurnController.Prepare @025F 자기 턴 시작). 토글 전환·방패 재장착·문 재활성으로 준비됨이 다시 생겨도 같은 자기 라운드 안이면 조건 거짓 → 아무것도 안 한다(준비됨 유지·자원 불변).
        // 조건 순서 = 자원(우리 조건 — 판정 불가 = 거짓 + ERROR) 먼저, 본편 ContextConditionCasterHasFact(Not) 다음(And 는 첫 거짓에서 멈춘다 — ConditionsChecker.Check @0052~0061).
        public static List<string> AttackerActionLines(RebukeSpec s, string p)
        {
            string ready = GuidOf(RebukeRole.Ready, s.Id), res = Guids.RebukeResource;
            var l = new List<string> { p + "#=1", p + "[0]=Conditional" };
            string c = p + "[0]";
            l.Add(c + ".ConditionsChecker.Operation=And");
            l.Add(c + ".ConditionsChecker.Conditions#=2");
            l.Add(c + ".ConditionsChecker.Conditions[0]=ContextConditionCasterHasResource");
            l.Add(c + ".ConditionsChecker.Conditions[0].Not=False");
            l.Add(c + ".ConditionsChecker.Conditions[0].m_Resource=" + res);
            l.Add(c + ".ConditionsChecker.Conditions[0].Amount=" + BulwarkRules.Dials.RebukeUsesPerFiring.ToString(CultureInfo.InvariantCulture));
            l.Add(c + ".ConditionsChecker.Conditions[1]=ContextConditionCasterHasFact");
            l.Add(c + ".ConditionsChecker.Conditions[1].Not=True");
            l.Add(c + ".ConditionsChecker.Conditions[1].m_Fact=" + Guids.RebukeSpent);
            l.Add(c + ".IfTrue#=4");
            l.Add(c + ".IfTrue[0]=ContextActionRemoveBuff");
            l.Add(c + ".IfTrue[0].m_Buff=" + ready);
            l.Add(c + ".IfTrue[0].ToCaster=True");
            l.Add(c + ".IfTrue[0].RemoveRank=False");
            l.Add(c + ".IfTrue[0].OnlyFromCaster=False");
            l.Add(c + ".IfTrue[1]=ContextSpendResource");
            l.Add(c + ".IfTrue[1].m_Resource=" + res);
            l.Add(c + ".IfTrue[1].ContextValueSpendure=False");
            l.Add(c + ".IfTrue[1].Value=" + Simple(BulwarkRules.Dials.RebukeUsesPerFiring));
            ApplyBuff(l, c + ".IfTrue[2]", Guids.RebukeSpent, true, NoDuration, false, true);
            string a = c + ".IfTrue[3]";
            l.Add(a + "=Conditional");
            l.Add(a + ".ConditionsChecker.Operation=And");
            l.Add(a + ".ConditionsChecker.Conditions#=1");
            l.Add(a + ".ConditionsChecker.Conditions[0]=ContextConditionAlive");
            l.Add(a + ".ConditionsChecker.Conditions[0].Not=False");
            l.AddRange(EffectLines(s, a + ".IfTrue"));
            l.Add(a + ".IfFalse#=1");
            l.Add(a + ".IfFalse[0]=ContextActionLogRebukeDud");
            l.Add(a + ".IfFalse[0].Reason=" + DudReasonAttackerDown);
            l.Add(a + ".IfFalse[0].EffectId=" + s.Id);
            l.Add(c + ".IfFalse#=0");
            return l;
        }

        // 노드 하나의 기대 줄(블루프린트 필드 + 컴포넌트 + 행동 트리). 게임 쪽 Describe 가 같은 문법으로 만든 줄과 정확히 같아야 한다(순서 포함).
        public static List<string> ExpectedLines(RebukeNode n)
        {
            if (n == null) throw new ArgumentNullException("n");
            var l = new List<string> { "type=" + n.BlueprintType };
            string res = Guids.RebukeResource;
            switch (n.Role)
            {
                case RebukeRole.Resource:
                    l.Add("m_MaxAmount.BaseValue=" + BulwarkRules.Dials.RebukeDailyBase.ToString(CultureInfo.InvariantCulture));
                    l.Add("m_MaxAmount.IncreasedByLevel=False");
                    l.Add("m_MaxAmount.IncreasedByLevelStartPlusDivStep=False");
                    l.Add("m_MaxAmount.IncreasedByStat=True");
                    l.Add("m_MaxAmount.ResourceBonusStat=Intelligence");
                    l.Add("m_UseMax=False");
                    l.Add("m_Min=" + BulwarkRules.Dials.RebukeDailyMin.ToString(CultureInfo.InvariantCulture));
                    l.Add("components=");
                    break;
                case RebukeRole.BaseFeature:
                    Feature(l, false);
                    l.Add("components=AddAbilityResources,AddFactContextActions");
                    l.Add("c0.m_Resource=" + res);
                    l.Add("c0.RestoreAmount=True");
                    l.Add("c0.UseThisAsResource=False");
                    l.Add("c0.Amount=0");
                    l.Add("c0.RestoreOnLevelUp=False");
                    // A1: 시전자 자신의 새 라운드마다(그리고 특성을 잃을 때) 공유 표지를 뗀다. 이 특성은 1레벨부터 늘 있어 토글·방패 상태와 무관하게 틱이 돈다.
                    l.Add("c1.Activated#=0");
                    RemoveSpent(l, "c1.Deactivated");
                    RemoveSpent(l, "c1.NewRound");
                    l.Add("c1.Dispose#=0");
                    break;
                case RebukeRole.Spent:
                    l.Add("Stacking=Replace");
                    l.Add("IsHiddenInUI=True");
                    l.Add("components=");
                    break;
                case RebukeRole.Selection:
                    Feature(l, false);
                    l.Add("Mode=Default");
                    l.Add("Group=None");
                    l.Add("IgnorePrerequisites=False");
                    l.Add("Obligatory=False");
                    l.Add("m_AllFeatures=" + string.Join(",", n.References.ToArray()));
                    l.Add("m_Features=");
                    l.Add("components=");
                    break;
                case RebukeRole.Learn:
                    Feature(l, false);
                    l.Add("components=AddFacts");
                    l.Add("c0.m_Facts=" + GuidOf(RebukeRole.Toggle, n.EffectId));
                    break;
                case RebukeRole.Toggle:
                    l.Add("m_Buff=" + GuidOf(RebukeRole.ToggleBuff, n.EffectId));
                    l.Add("Group=WizardDivinationAura");
                    l.Add("WeightInGroup=" + BulwarkRules.Dials.RebukeToggleWeight.ToString(CultureInfo.InvariantCulture));
                    l.Add("ActivationType=Immediately");
                    l.Add("m_ActivateWithUnitCommand=Free");
                    l.Add("DeactivateImmediately=False");
                    l.Add("DoNotTurnOffOnRest=True");
                    l.Add("IsOnByDefault=False");
                    l.Add("DeactivateIfCombatEnded=False");
                    l.Add("DeactivateAfterFirstRound=False");
                    l.Add("DeactivateIfOwnerDisabled=False");
                    l.Add("DeactivateIfOwnerUnconscious=False");
                    l.Add("OnlyInCombat=False");
                    l.Add("components=ActivatableAbilityResourceLogic");
                    l.Add("c0.m_RequiredResource=" + res);
                    l.Add("c0.SpendType=Never");
                    break;
                case RebukeRole.ToggleBuff:
                    l.Add("Stacking=Replace");
                    l.Add("IsHiddenInUI=False");
                    l.Add("components=HasShieldFeatureUnlock");
                    l.Add("c0.FilterByShieldProficiencyGroup=True");
                    l.Add("c0.m_ShiledProficiencyGroupEntries=TowerShield");
                    l.Add("c0.FilterByBlueprintShieldTypes=False");
                    l.Add("c0.m_DisableWhenHasArmor=False");
                    l.Add("c0.m_NewFact=" + GuidOf(RebukeRole.Gate, n.EffectId));
                    break;
                case RebukeRole.Gate:
                    {
                        Feature(l, true);
                        string ready = GuidOf(RebukeRole.Ready, n.EffectId);
                        l.Add("components=AddFactContextActions");
                        l.Add("c0.Activated#=1");
                        ApplyBuff(l, "c0.Activated[0]", ready, true, NoDuration, true);
                        l.Add("c0.Deactivated#=1");
                        l.Add("c0.Deactivated[0]=ContextActionRemoveBuff");
                        l.Add("c0.Deactivated[0].m_Buff=" + ready);
                        l.Add("c0.Deactivated[0].ToCaster=False");
                        l.Add("c0.Deactivated[0].RemoveRank=False");
                        l.Add("c0.Deactivated[0].OnlyFromCaster=False");
                        l.Add("c0.NewRound#=1");
                        l.Add("c0.NewRound[0]=Conditional");
                        l.Add("c0.NewRound[0].ConditionsChecker.Operation=And");
                        l.Add("c0.NewRound[0].ConditionsChecker.Conditions#=1");
                        l.Add("c0.NewRound[0].ConditionsChecker.Conditions[0]=ContextConditionHasBuff");
                        l.Add("c0.NewRound[0].ConditionsChecker.Conditions[0].Not=True");
                        l.Add("c0.NewRound[0].ConditionsChecker.Conditions[0].m_Buff=" + ready);
                        l.Add("c0.NewRound[0].IfTrue#=1");
                        ApplyBuff(l, "c0.NewRound[0].IfTrue[0]", ready, true, NoDuration, true);
                        l.Add("c0.NewRound[0].IfFalse#=0");
                        l.Add("c0.Dispose#=0");
                        break;
                    }
                case RebukeRole.Ready:
                    {
                        var s = RebukeTable.Get(n.EffectId);
                        var rank = RankFunction(s);
                        l.Add("Stacking=Replace");
                        l.Add("IsHiddenInUI=True");
                        l.Add("components=ContextCalculateAbilityParamsBasedOnClass," + (rank != null ? "ContextRankConfig," : "") + "AddTargetAttackWithWeaponTrigger");
                        l.Add("c0.m_CharacterClass=" + Guids.Game.WizardClass);
                        l.Add("c0.StatType=Intelligence");
                        l.Add("c0.UseKineticistMainStat=False");
                        int t = 1;
                        if (rank != null)
                        {
                            l.Add("c1.m_Type=Default");
                            l.Add("c1.m_BaseValueType=ClassLevel");
                            l.Add("c1.m_Class=" + Guids.Game.WizardClass);
                            l.Add("c1.m_ExceptClasses=False");
                            l.Add("c1.m_Progression=Custom");
                            l.Add("c1.m_CustomProgression=" + Progression(RebukeTable.CustomProgression(rank)));
                            l.Add("c1.m_UseMin=False");
                            l.Add("c1.m_UseMax=False");
                            t = 2;
                        }
                        string c = "c" + t.ToString(CultureInfo.InvariantCulture);
                        l.Add(c + ".TriggerBeforeAttack=False");
                        l.Add(c + ".WaitForAttackResolve=True");
                        l.Add(c + ".OnlyHit=True");
                        l.Add(c + ".CriticalHit=False");
                        l.Add(c + ".OnlyOnFirstAttack=False");
                        l.Add(c + ".OnAttackOfOpportunity=False");
                        l.Add(c + ".OnlyMelee=True");
                        l.Add(c + ".OnlyRanged=False");
                        l.Add(c + ".NotReach=False");
                        l.Add(c + ".OnlySneakAttack=False");
                        l.Add(c + ".NotSneakAttack=False");
                        l.Add(c + ".CheckCategory=False");
                        l.Add(c + ".DoNotPassAttackRoll=True");
                        l.Add(c + ".Not=False");
                        l.AddRange(AttackerActionLines(s, c + ".ActionsOnAttacker"));
                        l.Add(c + ".ActionOnSelf#=0");
                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException("n", n.Role, "BulwarkMage: unknown rebuke role");
            }
            return l;
        }

        static void RemoveSpent(List<string> l, string p)
        {
            l.Add(p + "#=1");
            l.Add(p + "[0]=ContextActionRemoveBuff");
            l.Add(p + "[0].m_Buff=" + Guids.RebukeSpent);
            l.Add(p + "[0].ToCaster=False");
            l.Add(p + "[0].RemoveRank=False");
            l.Add(p + "[0].OnlyFromCaster=False");
        }

        static void Feature(List<string> l, bool hidden)
        {
            l.Add("Ranks=1");
            l.Add("IsClassFeature=True");
            l.Add("HideInUI=" + B(hidden));
            l.Add("Groups=");
        }

        // 두 줄 목록 비교(빌드 시 되읽기·시험 공용): 같으면 빈 목록, 아니면 첫 차이들(최대 max 개).
        public static List<string> Diff(IReadOnlyList<string> want, IReadOnlyList<string> got, int max)
        {
            var d = new List<string>();
            if (want == null || got == null) { d.Add("null line list"); return d; }
            int n = Math.Max(want.Count, got.Count);
            for (int i = 0; i < n && d.Count < max; i++)
            {
                string w = i < want.Count ? want[i] : "<none>", g = i < got.Count ? got[i] : "<none>";
                if (!string.Equals(w, g, StringComparison.Ordinal)) d.Add("line " + i + ": want '" + w + "' got '" + g + "'");
            }
            return d;
        }
    }
}
