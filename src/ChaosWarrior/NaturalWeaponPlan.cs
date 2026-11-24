using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ChaosWarrior
{
    // [순수] 자연 무기 복제 원장(개편 계획 2026-11-16 「단일 원장 B」 12행 전사 · Task O4). 게임 형식 없음(modinfo testSources).
    // 자연 공격 변이마다 본편 자연 무기 하나를 「우리 GUID」로 복제한다(개편 설계 §12-4) — 이유: ① 사지 개수를 우리 GUID 로 센다(다른 출처의 같은 무기와 섞이지 않는다 — 정찰 §2.2)
    //   ② 신경독침이 일반 독침과 다른 블루프린트가 된다(O7) ③ 본편 무기를 고치지 않는다. 무기 형식은 복제하지 않고 공유한다(범주·치명 범위·피해 형태는 본편 그대로).
    // NaturalWeapons.Build 는 이 행만 읽는다(무기 GUID 리터럴 없음 — 시험 `wiring: weapons builder uses plan`). 원본 값(GUID·형식·범주·주사위)은 시험 `jbp: natural weapon sources` 가 blueprints.zip 과 대조한다.
    public static class NaturalWeaponPlan
    {
        public sealed class WeaponRow
        {
            // MutationId = 이 무기를 주는 변이(ChaosRules.Mutations 의 Id) · Guid = 우리 복제본 · SourceGuid/SourceName = 본편 원본 · TypeGuid = 본편 무기 형식(공유) · Category = 엔진 WeaponCategory 이름
            public readonly string MutationId, Guid, SourceGuid, SourceName, TypeGuid, Category;
            // 참 = 원본이 1d4 라 복제본에 m_OverrideDamageDice = true · m_DamageDice = 1d6 을 「우리가」 쓴다(촉수·갈고리발톱 — 개편 설계 §12-4).
            public readonly bool OverrideDice;
            public WeaponRow(string mutationId, string guid, string sourceGuid, string sourceName, string typeGuid, string category, bool overrideDice)
            { MutationId = mutationId; Guid = guid; SourceGuid = sourceGuid; SourceName = sourceName; TypeGuid = typeGuid; Category = category; OverrideDice = overrideDice; }
        }

        static WeaponRow W(string mutationId, string guid, string sourceGuid, string sourceName, string typeGuid, string category, bool overrideDice = false)
        {
            return new WeaponRow(mutationId, guid, sourceGuid, sourceName, typeGuid, category, overrideDice);
        }

        // 출처: 계획 2026-11-16 「단일 원장 B」(blueprints.zip Weapons/Items/Standard/NaturalWeapon/ 직독 — Medium · 강화 0 · 부품 0). 순서 = 원장 B 의 행 순서.
        public static readonly IReadOnlyList<WeaponRow> Rows = new[]
        {
            W("ExtraArm", Guids.WeaponSlam, Guids.Game.Slam1d6, "Slam1d6", Guids.Game.SlamType, "Slam"),
            W("Fangs", Guids.WeaponFangs, Guids.Game.Bite1d6, "Bite1d6", Guids.Game.BiteType, "Bite"),
            W("Claws", Guids.WeaponClaws, Guids.Game.Claw1d6, "Claw1d6", Guids.Game.ClawType, "Claw"),
            W("Horns", Guids.WeaponHorns, Guids.Game.Gore1d6, "Gore1d6", Guids.Game.GoreType, "Gore"),
            W("Tail", Guids.WeaponTail, Guids.Game.Tail1d6, "Tail1d6", Guids.Game.TailType, "Tail"),
            W("Tentacle", Guids.WeaponTentacle, Guids.Game.Tentacle1d4, "Tentacle1d4", Guids.Game.TentacleType, "Tentacle", true),
            W("Stinger", Guids.WeaponStinger, Guids.Game.Sting1d6, "Sting1d6", Guids.Game.StingType, "Sting"),
            W("Wings", Guids.WeaponWings, Guids.Game.Wing1d6, "Wing1d6", Guids.Game.WingType, "Wing"),
            W("Hooves", Guids.WeaponHooves, Guids.Game.Hoof1d6, "Hoof1d6", Guids.Game.HoofType, "Hoof"),
            W("Talons", Guids.WeaponTalons, Guids.Game.Talon1d4, "Talon1d4", Guids.Game.TalonsType, "Talon", true),
            // 집게의 범주는 Claw — 발톱과 엔진 상한 2 를 같이 쓴다(정찰 §2.1).
            W("Pincer", Guids.WeaponPincer, Guids.Game.Pincers1d6, "Pincers1d6", Guids.Game.PincersType, "Claw"),
            // 신경독침 = 독침과 같은 원본, 다른 우리 GUID(고통 핵심이 이 GUID 로 식별한다 — O7).
            W("NeuroStinger", Guids.WeaponNeuroStinger, Guids.Game.Sting1d6, "Sting1d6", Guids.Game.StingType, "Sting"),
        };

        // 변이 표의 자연 공격 행(ChaosRules.Mutations 의 Kind == NaturalAttack, 색인 순서) — 개편 O5 가 O4 의 손 전사본을 표에서 파생시켰다(C4: 같은 목록을 두 곳에 두지 않는다).
        // 무기 행의 변이 Id 열은 이것과 같아야 한다(Validate ①). 12 Id 의 손 전사 신탁은 시험 `exec: natural weapon plan` 에 있다.
        public static IReadOnlyList<string> NaturalAttackIds { get { return ChaosRules.Mutations.Where(m => m.Kind == ChaosRules.MutationKind.NaturalAttack).Select(m => m.Id).ToArray(); } }

        public static IReadOnlyList<string> WeaponGuids { get { return Rows.Select(r => r.Guid).ToArray(); } }

        // 변이 → 우리 무기 GUID. 행이 정확히 하나가 아니면 던진다(「없는 무기」를 null 로 넘기지 않는다).
        public static string GuidOf(string mutationId)
        {
            var hit = Rows.Where(r => r.MutationId == mutationId).ToArray();
            if (mutationId == null || hit.Length != 1) throw new ArgumentException("ChaosWarrior: mutation '" + mutationId + "' has " + hit.Length + " natural weapon rows (need exactly 1)", "mutationId");
            return hit[0].Guid;
        }

        // 우리 무기 GUID → 변이. 없으면 null = 「우리 무기가 아님」(다른 출처의 사지 — 대조에서 세지 않는다). 둘 이상이면 표가 깨진 것이다 → 던진다.
        public static string MutationOf(string weaponGuid)
        {
            if (weaponGuid == null) throw new ArgumentNullException("weaponGuid");
            var hit = Rows.Where(r => r.Guid == weaponGuid).ToArray();
            if (hit.Length > 1) throw new InvalidOperationException("ChaosWarrior: weapon GUID " + weaponGuid + " is on " + hit.Length + " natural weapon rows");
            return hit.Length == 1 ? hit[0].MutationId : null;
        }

        // 정적 검사(NaturalWeapons.Build 가 첫머리에서 부른다 — 어긋나면 블루프린트를 만들기 전에 던진다). 생산 표.
        public static void Validate() { Validate(Rows, MutationSpecs.StackRows, MutationSpecs.EffectRows); }

        // 무기 원장의 범주마다 「엔진 상한(engineCap) ≠ 전사 표(ChaosRules.LimbCapsExpected, 없는 범주 = LimbCapNone)」인 것을 한 줄씩(개편 O5 — 빌드 때 정보 줄의 재료).
        // 빈 목록 = 문서(설명문 · README)의 상한 숫자가 엔진과 같다. 판정에는 쓰지 않는다 — 판정은 뽑는 순간의 엔진 값이다. engineCap 이 null 을 주면(읽을 수 없음) 「같다」로 넘기지 않고 던진다.
        public static List<string> CapDifferences(Func<string, int?> engineCap)
        {
            if (engineCap == null) throw new ArgumentNullException("engineCap");
            var d = new List<string>();
            foreach (var category in Rows.Select(r => r.Category).Distinct())
            {
                int? got = engineCap(category);
                if (got == null) throw new InvalidOperationException("ChaosWarrior: the engine limb cap for category '" + category + "' is unreadable");
                int want = ChaosRules.LimbCapExpected(category);
                // 음수는 전부 「상한 없음」으로 같다(엔진 AddAdditionalLimb @0018~001D: cap < 0 이면 검사를 건너뛴다).
                bool same = got.Value < 0 ? want < 0 : got.Value == want;
                if (!same) d.Add(category + " " + CapText(got.Value) + " (documented " + CapText(want) + ")");
            }
            return d;
        }

        static string CapText(int cap) { return cap < 0 ? "none" : cap.ToString(); }

        // 빌드 때 정보 줄(독립 검토 R2-F4): engineCap = 엔진 판정과 같은 출처(게임: BlueprintPersonageLimits.GetWeaponSlotCount — 다시 뽑기 판정이 부르는 그 함수). 차이가 없으면 null(줄 없음), 있으면 한 줄 전체.
        // 순수 함수로 분리해 시험이 문장·조건을 직접 본다(앞 작업자의 생존 변이 B13 — 「차이가 있는데 줄이 안 나옴」을 잡는다). 읽을 수 없으면 CapDifferences 가 던진다.
        public static string CapInfoLine(Func<string, int?> engineCap)
        {
            var d = CapDifferences(engineCap);
            if (d.Count == 0) return null;
            return "ChaosWarrior: limb caps differ from the documented ones: " + string.Join(", ", d.ToArray()) + " (redraws follow the engine values)";
        }

        // 부품 이름: 사지를 주는 효과 행(MutationSpecs.EffectRows 의 Component 칸).
        public const string LimbComponent = "AddAdditionalLimb";

        static bool Hex32(string g) { return g != null && g.Length == Guid.Empty.ToString("N").Length && g.All(c => c >= '0' && c <= '9' || c >= 'a' && c <= 'f'); }

        // ① 변이 Id 열 == 변이 표의 자연 공격 행(ChaosRules.Mutations, 순서까지) ② 우리 GUID 는 32자리 소문자 16진수 · 서로 다름 · 원본·형식 GUID 와 겹치지 않음 ③ 원본·형식·범주 칸이 비어 있지 않고, 같은 원본을 쓰는 행은 형식·범주·이름이 같다
        // ④ 겹 방식 표와의 정합(표에 있는 행에 한해): 사지 행(LimbCategory 있음) ⇔ 무기 행 있음, 범주가 같다 ⑤ 효과 행과의 정합: 사지 부품 행의 무기(Extra) == 그 변이의 우리 무기, 사지 변이마다 사지 부품 행 정확히 1개.
        internal static void Validate(IReadOnlyList<WeaponRow> rows, IReadOnlyList<MutationSpecs.StackRow> stacks, IReadOnlyList<MutationSpecs.EffectRow> effects)
        {
            if (rows == null) throw new ArgumentNullException("rows");
            if (stacks == null) throw new ArgumentNullException("stacks");
            if (effects == null) throw new ArgumentNullException("effects");
            var ids = rows.Select(r => r.MutationId).ToArray();
            if (!ids.SequenceEqual(NaturalAttackIds)) throw new InvalidOperationException("ChaosWarrior: natural weapon rows [" + string.Join(",", ids) + "] != the natural-attack mutations [" + string.Join(",", NaturalAttackIds) + "] (same ids, same order)");
            foreach (var r in rows)
            {
                if (!Hex32(r.Guid) || !Hex32(r.SourceGuid) || !Hex32(r.TypeGuid)) throw new InvalidOperationException("ChaosWarrior: natural weapon row '" + r.MutationId + "' has a GUID that is not 32 lowercase hex (" + r.Guid + " / " + r.SourceGuid + " / " + r.TypeGuid + ")");
                if (string.IsNullOrEmpty(r.SourceName) || string.IsNullOrEmpty(r.Category)) throw new InvalidOperationException("ChaosWarrior: natural weapon row '" + r.MutationId + "' has no source name / category");
            }
            var own = rows.Select(r => r.Guid).ToArray();
            if (own.Distinct().Count() != own.Length) throw new InvalidOperationException("ChaosWarrior: two natural weapon rows share one of our GUIDs [" + string.Join(",", own.GroupBy(g => g).Where(g => g.Count() > 1).Select(g => g.Key)) + "]");
            var vanilla = rows.Select(r => r.SourceGuid).Concat(rows.Select(r => r.TypeGuid)).ToArray();
            var clash = own.Intersect(vanilla).ToArray();
            if (clash.Length > 0) throw new InvalidOperationException("ChaosWarrior: a natural weapon clone GUID equals a source / weapon type GUID (" + string.Join(",", clash) + ") — the clone would be the vanilla blueprint");
            foreach (var g in rows.GroupBy(r => r.SourceGuid))
                if (g.Select(r => r.TypeGuid + "|" + r.Category + "|" + r.SourceName + "|" + r.OverrideDice).Distinct().Count() != 1)
                    throw new InvalidOperationException("ChaosWarrior: natural weapon rows sharing source " + g.Key + " disagree on type / category / name / dice override");
            foreach (var s in stacks)
            {
                var mine = rows.Where(r => r.MutationId == s.Id).ToArray();
                if (s.LimbCategory == null)
                {
                    if (mine.Length != 0) throw new InvalidOperationException("ChaosWarrior: mutation '" + s.Id + "' has a natural weapon row but its stack row has no limb category");
                    continue;
                }
                if (mine.Length != 1) throw new InvalidOperationException("ChaosWarrior: limb mutation '" + s.Id + "' has " + mine.Length + " natural weapon rows (need exactly 1)");
                if (mine[0].Category != s.LimbCategory) throw new InvalidOperationException("ChaosWarrior: limb mutation '" + s.Id + "' says category '" + s.LimbCategory + "' but its weapon row says '" + mine[0].Category + "'");
                var limbRows = effects.Where(e => e.Id == s.Id && e.Component == LimbComponent).ToArray();
                if (limbRows.Length != 1) throw new InvalidOperationException("ChaosWarrior: limb mutation '" + s.Id + "' has " + limbRows.Length + " " + LimbComponent + " effect rows (need exactly 1 — one limb per marker instance)");
            }
            foreach (var e in effects.Where(e => e.Component == LimbComponent))
            {
                var mine = rows.Where(r => r.MutationId == e.Id).ToArray();
                if (mine.Length != 1 || e.Extra != mine[0].Guid)
                    throw new InvalidOperationException("ChaosWarrior: effect row " + e.Id + "/" + LimbComponent + " gives weapon " + e.Extra + " but the natural weapon ledger says " + (mine.Length == 1 ? mine[0].Guid : mine.Length + " rows") + " (a limb of any other weapon is not counted as ours)");
            }
        }
    }

    // [순수] 복제 경계의 값 대조(개편 Task O4): 복제본과 원본을 필드 단위로 내려가며 「값이 다른 경로」를 모은다. CloneKit.Shared 가 「같은 가변 인스턴스를 공유하지 않는다」를 본다면
    // 이것은 「의도한 칸만 다르다」를 본다 — 복제 도중 다른 칸이 바뀌어도(복사 누락 · OnEnable 재정의의 쓰기 · 깊이 잘림) 예외가 없어 드러나지 않기 때문이다.
    //   원시값·열거·문자열 = 값 비교 / keepShared 가 참인 것(블루프린트·Unity 객체 …) = 같은 인스턴스여야 한다 / 배열·리스트·사전 = 길이와 원소 / 그 밖의 객체·구조체 = 필드마다.
    // 깊이 셈은 CloneKit.CopyOf 와 같다(가변 노드를 하나 내려갈 때마다 1) — 다 쓰면 「같다」로 넘기지 않고 던진다.
    public static class CloneDiff
    {
        static IEnumerable<FieldInfo> Fields(Type t)
        {
            for (var cur = t; cur != null && cur != typeof(object); cur = cur.BaseType)
                foreach (var f in cur.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    if (!f.IsLiteral) yield return f;
        }

        // 순환 방지는 「(복제 쪽, 원본 쪽) 쌍」 기준(독립 검토 R2-F8): 복제 쪽 객체 하나가 서로 다른 원본 쪽 객체와 짝지어 다시 나오면 그 짝도 본다(복제 쪽만 기억하면 두 번째 짝의 차이를 놓친다).
        sealed class PairEq : IEqualityComparer<KeyValuePair<object, object>>
        {
            public bool Equals(KeyValuePair<object, object> x, KeyValuePair<object, object> y) { return ReferenceEquals(x.Key, y.Key) && ReferenceEquals(x.Value, y.Value); }
            public int GetHashCode(KeyValuePair<object, object> p)
            {
                return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(p.Key) ^ (System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(p.Value) * 31);
            }
        }

        // 어느 깊이에서든 건너뛰는 칸의 열쇠 = "선언 형식 FullName::필드 이름"(호출자가 이유와 함께 적는다 — NaturalWeapons.RuntimeCaches).
        public static string FieldKey(FieldInfo f) { return f.DeclaringType.FullName + "::" + f.Name; }

        // 최상위 객체의 필드 중 skipTop 에 이름이 있는 것은 보지 않는다(정체성 칸·캐시 — 호출자가 이름을 적고 이유를 댄다). 그 이름의 필드가 없으면 던진다(낡은 예외).
        public static List<string> Paths(object clone, object source, Func<object, bool> keepShared, IReadOnlyList<string> skipTop, int maxDepth)
        {
            return Paths(clone, source, keepShared, skipTop, new string[0], null, maxDepth);
        }

        // skipAnywhere = 어느 깊이에서든 보지 않는 칸(FieldKey — 실행 중 엔진이 늦게 채우는 캐시, 독립 검토 R2-F3. 열쇠가 실제 형식에 있는지는 호출자가 검사한다 — 깊은 곳의 형식은 값이 null 이면 만나지 못하므로).
        // sameIdentity(a, b) 가 참인 짝은 같은 것으로 본다(복제본 ↔ 원본 자신을 가리키는 소유자 역참조 — 부품의 OwnerBlueprint · 행동의 Owner).
        public static List<string> Paths(object clone, object source, Func<object, bool> keepShared, IReadOnlyList<string> skipTop, IReadOnlyCollection<string> skipAnywhere, Func<object, object, bool> sameIdentity, int maxDepth)
        {
            if (clone == null) throw new ArgumentNullException("clone");
            if (source == null) throw new ArgumentNullException("source");
            if (keepShared == null) throw new ArgumentNullException("keepShared");
            if (skipTop == null) throw new ArgumentNullException("skipTop");
            if (skipAnywhere == null) throw new ArgumentNullException("skipAnywhere");
            if (clone.GetType() != source.GetType()) throw new InvalidOperationException("ChaosWarrior: clone diff of " + clone.GetType().Name + " against " + source.GetType().Name);
            var fields = Fields(clone.GetType()).ToList();
            var stale = skipTop.Where(n => !fields.Any(f => f.Name == n)).ToArray();
            if (stale.Length > 0) throw new InvalidOperationException("ChaosWarrior: clone diff skips field(s) " + string.Join(", ", stale) + " that " + clone.GetType().Name + " does not have");
            var found = new List<string>();
            var w = new Walker { Keep = keepShared, SkipAny = skipAnywhere, Same = sameIdentity, Found = found };
            foreach (var f in fields)
            {
                if (skipTop.Contains(f.Name) || skipAnywhere.Contains(FieldKey(f))) continue;
                w.Walk(f.GetValue(clone), f.GetValue(source), "." + f.Name, maxDepth);
            }
            return found;
        }

        // 부품 배열 대조(독립 검토 R2-F2): 복제본의 부품은 CloneBoundary.CopyComponent 로 만든 「다른 인스턴스」라 인스턴스 동일성(keepShared)으로 보면 언제나 차이다.
        //   개수가 다르면 path, 같은 색인의 형식이 다르면 path[i], 같으면 부품 쌍마다 값 대조(Paths — 최상위 skipTop 칸 제외, sameIdentity = 소유자 역참조 짝).
        //   keepShared 는 호출자가 부품용으로 준다(행동 Element 는 복제가 깊게 새로 만들므로 값으로 내려가야 한다 — 인스턴스로 보면 또 거짓 차이다).
        public static List<string> ComponentPaths(IList cloneComps, IList sourceComps, string path, Func<object, bool> keepShared, IReadOnlyList<string> skipTop, IReadOnlyCollection<string> skipAnywhere, Func<object, object, bool> sameIdentity, int maxDepth)
        {
            if (cloneComps == null) throw new ArgumentNullException("cloneComps");
            if (sourceComps == null) throw new ArgumentNullException("sourceComps");
            if (path == null) throw new ArgumentNullException("path");
            var found = new List<string>();
            if (cloneComps.Count != sourceComps.Count) { found.Add(path); return found; }
            for (int i = 0; i < cloneComps.Count; i++)
            {
                object c = cloneComps[i], s = sourceComps[i];
                string at = path + "[" + i + "]";
                if (c == null || s == null || c.GetType() != s.GetType()) { if (!(c == null && s == null)) found.Add(at); continue; }
                if (ReferenceEquals(c, s)) { found.Add(at + " (same instance as the source)"); continue; }
                found.AddRange(Paths(c, s, keepShared, skipTop, skipAnywhere, sameIdentity, maxDepth).Select(p => at + p));
            }
            return found;
        }

        static bool Leaf(object v)
        {
            var t = v.GetType();
            return v is string || v is Type || v is Delegate || v is MemberInfo || t.IsPrimitive || t.IsEnum || t == typeof(decimal);
        }

        sealed class Walker
        {
            public Func<object, bool> Keep;
            public IReadOnlyCollection<string> SkipAny;
            public Func<object, object, bool> Same;
            public List<string> Found;
            readonly HashSet<KeyValuePair<object, object>> seen = new HashSet<KeyValuePair<object, object>>(new PairEq());

            public void Walk(object a, object b, string path, int depth)
            {
                if (a == null || b == null) { if (!(a == null && b == null)) Found.Add(path); return; }
                if (Same != null && Same(a, b)) return;   // 소유자 역참조 짝(복제본 ↔ 원본)
                if (a.GetType() != b.GetType()) { Found.Add(path); return; }
                if (Leaf(a)) { if (!a.Equals(b)) Found.Add(path); return; }
                if (Keep(a)) { if (!ReferenceEquals(a, b)) Found.Add(path); return; }
                if (depth < 0) throw new InvalidOperationException("ChaosWarrior: clone diff depth exhausted at " + path + " (" + a.GetType().FullName + ") — cannot say the two are equal");
                if (!a.GetType().IsValueType)
                {
                    if (ReferenceEquals(a, b)) return;   // 같은 인스턴스 = 같은 값(공유 여부는 CloneKit.Shared 의 몫)
                    if (!seen.Add(new KeyValuePair<object, object>(a, b))) return;
                }
                var da = a as IDictionary;
                if (da != null)
                {
                    var db = (IDictionary)b;
                    if (da.Count != db.Count) { Found.Add(path); return; }
                    foreach (DictionaryEntry e in da)
                    {
                        if (!db.Contains(e.Key)) { Found.Add(path + "[" + e.Key + "]"); continue; }
                        Walk(e.Value, db[e.Key], path + "[" + e.Key + "]", depth - 1);
                    }
                    return;
                }
                var la = a as IList;
                if (la != null)
                {
                    var lb = (IList)b;
                    if (la.Count != lb.Count) { Found.Add(path); return; }
                    for (int i = 0; i < la.Count; i++) Walk(la[i], lb[i], path + "[" + i + "]", depth - 1);
                    return;
                }
                foreach (var f in Fields(a.GetType()))
                {
                    if (SkipAny.Contains(FieldKey(f))) continue;
                    Walk(f.GetValue(a), f.GetValue(b), path + "." + f.Name, depth - 1);
                }
            }
        }
    }
}
