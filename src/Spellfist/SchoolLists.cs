using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.DLC;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace Spellfist
{
    // 학파별 주문 목록 8개 + 빈 기본 목록(설계 §11-1, 정찰 §0-1·§0-2·§1-2).
    // 본편 타실로니아 학파 목록은 「메이거스 − 반대 학파 2개」라 못 쓰고, BlueprintSpellList 의 Filter* 필드는 에디터 전용이라
    // 우리가 메이거스 목록을 학파로 갈라 m_Spells 를 직접 채운다.
    // 런타임 GetSpells = SpellsByLevel[lvl].SpellsFiltered = m_Spells 중 !IsDlcRestricted(DlcCondition 미충족 제외, null 은 통과)이고, 개수>0 이면 영구 캐시(핫픽스 2 정찰).
    // → 빌드(BlueprintsCache.Init) 중에는 GetSpells 를 부르지 않는다: 그때는 DLCCache 상태가 없어 DLC 주문이 걸러지고, 그 보기가 캐시에 굳는다.
    public static class SchoolLists
    {
        // 주문서의 기본 목록: 0개(정찰 §3 — 첫 레벨 캔트립·레벨업 선택·두루마리 복사가 모두 이 목록을 본다 → 학파 밖 주문 차단).
        public static BlueprintSpellList Empty { get; private set; }
        static BlueprintSpellList[] lists; // School 순서

        // School 순서와 같은 순서(Build 가 Enum 이름으로 짝을 확인).
        static readonly string[] ListGuids =
        {
            Guids.SchoolListAbjuration, Guids.SchoolListConjuration, Guids.SchoolListDivination, Guids.SchoolListEnchantment,
            Guids.SchoolListEvocation, Guids.SchoolListIllusion, Guids.SchoolListNecromancy, Guids.SchoolListTransmutation,
        };

        public static BlueprintSpellList For(School s)
        {
            if (lists == null) throw new InvalidOperationException("Spellfist: SchoolLists.For before SchoolLists.Build()");
            int i = (int)s;
            if (i < 0 || i >= lists.Length || !Enum.IsDefined(typeof(School), s)) throw new ArgumentOutOfRangeException("s", s, "Spellfist: unknown school");
            return lists[i];
        }

        public static void Build()
        {
            if (Empty != null) throw new InvalidOperationException("Spellfist: SchoolLists.Build() called twice");
            var schools = Schools();
            int nLevels = SpellfistRules.SpellListLevels;
            var magus = Get<BlueprintSpellList>(Guids.Game.MagusSpellList);
            List<(BlueprintGuid guid, string school, int level, string name)> placed;
            List<(BlueprintAbilityReference r, int level)> dups;
            var buckets = Split(magus, schools, nLevels, out placed, out dups);
            // 같은 레벨 안 같은 GUID 중복(다른 모드가 두 번 넣은 경우)은 첫 등장만 쓴다(SchoolListRules.DedupeKeepFirst, Split 안) — 버린 것은 INFO 로 남긴다.
            foreach (var d in dups)
            {
                var ds = d.r.Get();
                ModContext.Log("Spellfist: INFO duplicate magus spell reference removed (rule: keep first per level) " + (ds == null ? "<unresolved>" : ds.name) + " " + d.r.Guid + " L" + d.level);
            }
            var ledgerKeys = CheckLedger(placed);
            foreach (var p in placed.Where(p => !ledgerKeys.Contains(p.guid)))
                ModContext.Log("Spellfist: INFO magus spell not in SchoolLedger (another mod's addition) " + p.name + " " + p.guid + " " + p.school + " L" + p.level
                    + (p.school == "Universalist" ? " - dropped (universalist)" : " - added to our " + p.school + " list"));

            var built = new BlueprintSpellList[schools.Length];
            for (int s = 0; s < schools.Length; s++)
            {
                int si = s;
                built[s] = MakeList("SpellfistSchoolList" + schools[s], ListGuids[s], l => buckets[si, l]);
            }
            var empty = MakeList("SpellfistEmptySpellList", Guids.EmptySpellList, l => new List<BlueprintAbilityReference>());
            // BlueprintSpellList.get_MaxLevel(IL @0x0000~0x0060) = m_MaxLevel>0 이면 그 값, 아니면 SpellsFiltered.Count>0 인 레벨의 Max → 전부 빈 목록이면
            // Enumerable.Max 가 InvalidOperationException. HasSpells(= SpellsByLevel.Length>0)가 true 라 AbilityData.get_VoiceIntensity(@0x003B, 주문 시전 때
            // UnitUseAbility.SpawnPrecastFx 가 부름)가 이 값을 읽는다 → 미리 채운다. 값 = 아카니스트/메이거스 목록의 최고 레벨과 같은 9(목소리 세기 판정만 쓴다).
            SetField(empty, "m_MaxLevel", SpellfistRules.MaxSpellLevel);
            if ((int)Util.GetF(empty, "m_MaxLevel") != SpellfistRules.MaxSpellLevel || empty.MaxLevel != SpellfistRules.MaxSpellLevel)
                throw new InvalidOperationException("Spellfist: Empty spell list m_MaxLevel readback failed");

            // 되읽기(핫픽스 2): 만든 것과 같은 것끼리 — 저장된 m_Spells 를 해석해 고유 GUID 집합·각 주문의 실제 SpellComponent.School 을 대조(SchoolListRules.CompareLevel).
            // GetSpells(=DLC 로 거른 보기)는 부르지 않는다: Init 시점엔 DLCCache 상태가 없어 DLC 주문이 빠지고(두 번째 게임 실행: Abjuration L6 Repulsion DLC5), 그 보기가 m_SpellsFiltered 에 굳는다.
            var sourceOf = new Dictionary<string, string>(SchoolListRules.GuidComparer);
            foreach (var p in placed)
                sourceOf[p.guid.ToString()] = "MagusSpellList L" + p.level + " " + p.school + (ledgerKeys.Contains(p.guid) ? " (base game, SchoolLedger)" : " (not in SchoolLedger: another mod's addition)");
            Func<string, string> describe = g => { string d; return sourceOf.TryGetValue(g, out d) ? d : "not from MagusSpellList"; };
            var dlcConditioned = new List<string>();
            for (int s = 0; s < schools.Length; s++)
                for (int l = 0; l < nLevels; l++)
                {
                    var lvl = built[s].SpellsByLevel[l];
                    var raw = Util.GetF(lvl, "m_Spells") as List<BlueprintAbilityReference>;
                    if (raw == null) throw new InvalidOperationException("Spellfist: " + built[s].name + " level " + l + " m_Spells is null or not List<BlueprintAbilityReference> (cannot read back)");
                    var actual = new List<SchoolListRules.ReadbackEntry>();
                    foreach (var r in raw)
                    {
                        var a = r == null ? null : r.Get();
                        var sc = a == null ? null : a.GetComponent<SpellComponent>();
                        actual.Add(new SchoolListRules.ReadbackEntry { Guid = a == null ? null : a.AssetGuid.ToString(), Name = a == null ? null : a.name, School = sc == null ? null : sc.School.ToString() });
                        // DlcCondition 은 평가하지 않고 있는지만 센다(평가 = IsFullfilled 는 Init 시점에 DLCCache 오류 로그 + 보상 캐시).
                        if (a != null && a.GetComponent<DlcCondition>() != null) dlcConditioned.Add(a.name + " " + schools[s] + " L" + l);
                    }
                    var mismatch = SchoolListRules.CompareLevel(schools[s].ToString(), buckets[s, l].Select(r => r.Guid.ToString()).ToList(), actual, describe);
                    if (mismatch.Count > 0)
                        throw new InvalidOperationException("Spellfist: " + built[s].name + " level " + l + " read-back (m_Spells vs built, " + buckets[s, l].Count + " built / " + raw.Count + " stored): " + string.Join("; ", mismatch));
                    if (Util.GetF(lvl, "m_SpellsFiltered") != null)
                        throw new InvalidOperationException("Spellfist: " + built[s].name + " level " + l + " m_SpellsFiltered was filled during build (DLC-filtered view would be frozen at BlueprintsCache.Init)");
                }
            for (int l = 0; l < nLevels; l++)
            {
                var raw = Util.GetF(empty.SpellsByLevel[l], "m_Spells") as List<BlueprintAbilityReference>;
                if (raw == null || raw.Count != 0) throw new InvalidOperationException("Spellfist: Empty spell list level " + l + " m_Spells is " + (raw == null ? "null" : raw.Count + " entries") + ", expected an empty list");
                if (Util.GetF(empty.SpellsByLevel[l], "m_SpellsFiltered") != null) throw new InvalidOperationException("Spellfist: Empty spell list level " + l + " m_SpellsFiltered was filled during build");
            }
            if (dlcConditioned.Count > 0)
                ModContext.Log("Spellfist: INFO " + dlcConditioned.Count + " school-list spells carry DlcCondition (kept; the engine hides them at use time when the DLC is unavailable, same as the magus list): " + string.Join(", ", dlcConditioned));

            lists = built;
            Empty = empty;
            // 최종 개수 INFO(정찰 C6: 우리 빌드 뒤에 메이거스 목록에 더해진 주문은 여기 없다 → 첫 프레임 Resync 가 다시 맞추고, 점검 S1 school-list-resync·S2/S3/S7 이 현재 메이거스 목록과 대조한다).
            int total = 0;
            var lines = new List<string>();
            for (int s = 0; s < schools.Length; s++)
            {
                var row = Enumerable.Range(0, nLevels).Select(l => buckets[s, l].Count).ToArray();
                total += row.Sum();
                lines.Add(schools[s] + "=" + string.Join(",", row));
            }
            ModContext.Log("Spellfist: INFO school lists built (levels 0.." + (nLevels - 1) + ") " + string.Join(" | ", lines) + " | total " + total);
        }

        static School[] Schools()
        {
            var schools = (School[])Enum.GetValues(typeof(School));
            if (schools.Length != ListGuids.Length) throw new InvalidOperationException("Spellfist: School enum has " + schools.Length + " values, ListGuids " + ListGuids.Length);
            for (int i = 0; i < schools.Length; i++)
                if ((int)schools[i] != i) throw new InvalidOperationException("Spellfist: School enum values are not 0..7 in order");
            return schools;
        }

        // 메이거스 목록 m_Spells(SpellsRefs — DLC 로 거른 보기 아님)를 학파 × 레벨로 가른다. Build 와 첫 프레임 Resync 가 같은 규칙을 쓴다(한 곳).
        // 판독 불가(레벨 배열 길이·레벨 번호·null 참조·해석 안 되는 참조·SpellComponent 없음·모르는 학파)는 던진다. Universalist 는 placed 에만 적고 버린다.
        static List<BlueprintAbilityReference>[,] Split(BlueprintSpellList magus, School[] schools, int nLevels,
            out List<(BlueprintGuid guid, string school, int level, string name)> placed, out List<(BlueprintAbilityReference r, int level)> dups)
        {
            if (magus.SpellsByLevel == null || magus.SpellsByLevel.Length != nLevels)
                throw new InvalidOperationException("Spellfist: MagusSpellList.SpellsByLevel length " + (magus.SpellsByLevel == null ? "null" : magus.SpellsByLevel.Length.ToString()) + ", expected " + nLevels);
            var buckets = new List<BlueprintAbilityReference>[schools.Length, nLevels];
            for (int s = 0; s < schools.Length; s++) for (int l = 0; l < nLevels; l++) buckets[s, l] = new List<BlueprintAbilityReference>();
            // 메이거스 목록의 모든 주문이 어디에 놓였는지(학파 이름 = SpellSchool 이름, Universalist 포함) — 원장 대조용.
            placed = new List<(BlueprintGuid guid, string school, int level, string name)>();
            dups = new List<(BlueprintAbilityReference r, int level)>();
            for (int l = 0; l < nLevels; l++)
            {
                if (l > SpellfistRules.NativeMaxSpellLevel) continue;
                var lvl = magus.SpellsByLevel[l];
                if (lvl == null || lvl.SpellLevel != l) throw new InvalidOperationException("Spellfist: MagusSpellList.SpellsByLevel[" + l + "] is null or has SpellLevel " + (lvl == null ? "-" : lvl.SpellLevel.ToString()));
                // SpellsRefs = m_Spells 그대로(캐시 SpellsFiltered 를 건드리지 않는다).
                var refsRaw = lvl.SpellsRefs;
                if (refsRaw == null) throw new InvalidOperationException("Spellfist: MagusSpellList level " + l + " m_Spells is null");
                if (refsRaw.Any(r => r == null)) throw new InvalidOperationException("Spellfist: MagusSpellList level " + l + " has an unresolved spell reference null");
                List<BlueprintAbilityReference> dupRefs;
                var refs = SchoolListRules.DedupeKeepFirst(refsRaw, r => r.Guid.ToString(), out dupRefs);
                foreach (var d in dupRefs) dups.Add((d, l));
                foreach (var r in refs)
                {
                    var spell = r.Get();
                    if (spell == null && SchoolLedger.IsDlcSpell(r.Guid.ToString())) continue;
                    if (spell == null) throw new InvalidOperationException("Spellfist: MagusSpellList level " + l + " has an unresolved spell reference " + r.Guid);
                    var sc = spell.GetComponent<SpellComponent>();
                    if (sc == null) throw new InvalidOperationException("Spellfist: 학파 미상 — " + spell.name + " (" + spell.AssetGuid + ") has no SpellComponent");
                    placed.Add((spell.AssetGuid, sc.School.ToString(), l, spell.name));
                    if (sc.School == SpellSchool.Universalist) continue;
                    School ours;
                    // 이름으로 짝짓는다(SpellSchool None/새 값은 실패).
                    if (!Enum.TryParse(sc.School.ToString(), false, out ours) || !Enum.IsDefined(typeof(School), ours))
                        throw new InvalidOperationException("Spellfist: 학파 미상 — " + spell.name + " (" + spell.AssetGuid + ") school " + sc.School);
                    buckets[(int)ours, l].Add(Ref<BlueprintAbilityReference>(spell.AssetGuid.ToString()));
                }
            }
            return buckets;
        }

        // 원장 대조(SchoolLedger — 본편 메이거스 주문 281개). (a) 원장의 모든 주문이 같은 학파·같은 레벨에 있어야 한다: 빠지거나 옮겨지면 throw(C1/C5).
        // (b) 원장 밖 주문(다른 모드가 추가)은 허용 — 돌려주는 원장 키 집합으로 호출자가 INFO 를 남긴다.
         
        static HashSet<BlueprintGuid> CheckLedger(List<(BlueprintGuid guid, string school, int level, string name)> placed)
        {
            var ledgerKeys = new HashSet<BlueprintGuid>();
            var problems = new List<string>();
            foreach (var e in SchoolLedger.Entries)
            {
                var g = BlueprintGuid.Parse(e.guid);
                ledgerKeys.Add(g);
                var at = placed.Where(p => p.guid == g).ToList();
                if (at.Count == 0 && SchoolLedger.IsDlcSpell(e.guid) && ResourcesLibrary.TryGetBlueprint(g) == null) continue;
                if (at.Count == 0) problems.Add(e.guid + " (" + e.school + " L" + e.level + ") missing from MagusSpellList");
                else if (!at.Any(p => p.school == e.school && p.level == e.level))
                    problems.Add(at[0].name + " " + e.guid + " moved: ledger " + e.school + " L" + e.level + ", now " + string.Join("/", at.Select(p => p.school + " L" + p.level)));
            }
            if (problems.Count > 0) throw new InvalidOperationException("Spellfist: MagusSpellList differs from SchoolLedger (base-game magus spells): " + string.Join("; ", problems));
            return ledgerKeys;
        }

        // ---- 첫 프레임 재동기화(2026-11-17 소유자 결정 「게임 시작 뒤 다시 맞춤」, 근거·순서 = SchoolListRules 「첫 프레임 재동기화」 주석) ----
        // Main.Load 가 modEntry.OnUpdate += SchoolLists.FirstFrameTick 으로 건다(점검 모드와 무관하게 늘). 매 프레임 불리지만 일은 한 번(ResyncGate).
        static readonly SchoolListRules.ResyncGate gate = new SchoolListRules.ResyncGate();







        public static void FirstFrameTick(UnityModManagerNet.UnityModManager.ModEntry entry, float dt)
        {
            if (ModContext.BuildSkipReasonEn != null) return;
            gate.Run(ModContext.BuildAttempted, ModContext.BuildFailed, Resync, ModContext.Log, ModContext.LogError);
        }

        // 지금 메이거스 목록을 Build 와 같은 규칙(Split · CheckLedger)으로 다시 갈라, 우리 학파 목록 8개 × 11레벨의 m_Spells 를 그 결과로 바꾼다. 돌려주는 값 = 로그 1줄.
        // 원자성: ① 계산(판독 불가면 던짐 — 아무것도 바꾸지 않았다) → ② 적용(각 m_Spells 리스트를 제자리에서 바꾼다: m_SpellsProxy 가 같은 리스트 객체를 감싸므로 객체를 갈아 끼우지 않는다)
        // → ③ 되읽기(CompareLevel). ②·③ 에서 던지면 원래 내용·캐시로 되돌린 뒤 다시 던진다(호출자 ResyncGate 가 Failed + 오류 줄).
        // 캐시: m_SpellsFiltered(DLC 로 거른 보기, 개수>0 이면 영구 — get_SpellsFiltered IL_0000~0013)·m_SpellsProxy(get_Spells IL_0000~0018)·목록의 m_MaxLevel(get_MaxLevel IL_0058~005A 가 처음 계산값을 저장)
        // 을 비운다 → 다음 접근이 새 m_Spells 로 다시 만든다. Empty 목록(m_MaxLevel 9 고정)은 건드리지 않는다.
        internal static string Resync()
        {
            if (lists == null || Empty == null) throw new InvalidOperationException("Spellfist: SchoolLists.Resync before SchoolLists.Build()");
            var schools = Schools();
            int nLevels = SpellfistRules.SpellListLevels;
            var magus = Get<BlueprintSpellList>(Guids.Game.MagusSpellList);
            List<(BlueprintGuid guid, string school, int level, string name)> placed;
            List<(BlueprintAbilityReference r, int level)> dups;
            var buckets = Split(magus, schools, nLevels, out placed, out dups);
            CheckLedger(placed);

            // ① 계산: 지금 목록(전)과 새로 가른 것(후)을 칸 목록으로.
            var stores = new List<BlueprintAbilityReference>[schools.Length, nLevels];
            var levelLists = new SpellLevelList[schools.Length, nLevels];
            var before = new List<SchoolListRules.SpellAt>();
            var after = new List<SchoolListRules.SpellAt>();
            for (int s = 0; s < schools.Length; s++)
            {
                if (lists[s].SpellsByLevel == null || lists[s].SpellsByLevel.Length != nLevels)
                    throw new InvalidOperationException("Spellfist: " + lists[s].name + " SpellsByLevel length " + (lists[s].SpellsByLevel == null ? "null" : lists[s].SpellsByLevel.Length.ToString()) + ", expected " + nLevels);
                for (int l = 0; l < nLevels; l++)
                {
                    var lvl = lists[s].SpellsByLevel[l];
                    var raw = lvl == null ? null : Util.GetF(lvl, "m_Spells") as List<BlueprintAbilityReference>;
                    if (raw == null) throw new InvalidOperationException("Spellfist: " + lists[s].name + " level " + l + " m_Spells is null or not List<BlueprintAbilityReference> (cannot re-sync)");
                    stores[s, l] = raw;
                    levelLists[s, l] = lvl;
                    foreach (var r in raw)
                    {
                        var a = r == null ? null : r.Get();
                        if (a == null) throw new InvalidOperationException("Spellfist: " + lists[s].name + " level " + l + " has an unresolved reference (cannot compare before re-sync)");
                        before.Add(new SchoolListRules.SpellAt { Guid = a.AssetGuid.ToString(), Name = a.name, School = schools[s].ToString(), Level = l });
                    }
                    foreach (var r in buckets[s, l])
                    {
                        var a = r.Get();
                        if (a == null) throw new InvalidOperationException("Spellfist: re-split reference " + r.Guid + " does not resolve");
                        after.Add(new SchoolListRules.SpellAt { Guid = a.AssetGuid.ToString(), Name = a.name, School = schools[s].ToString(), Level = l });
                    }
                }
            }
            var diff = SchoolListRules.Diff(before, after);

            // ② 적용 + ③ 되읽기. 되돌림용 사본.
            var oldSpells = new List<BlueprintAbilityReference>[schools.Length, nLevels];
            var oldFiltered = new object[schools.Length, nLevels];
            var oldProxy = new object[schools.Length, nLevels];
            var oldMax = new int[schools.Length];
            for (int s = 0; s < schools.Length; s++)
            {
                oldMax[s] = (int)Util.GetF(lists[s], "m_MaxLevel");
                for (int l = 0; l < nLevels; l++)
                {
                    oldSpells[s, l] = new List<BlueprintAbilityReference>(stores[s, l]);
                    oldFiltered[s, l] = Util.GetF(levelLists[s, l], "m_SpellsFiltered");
                    oldProxy[s, l] = Util.GetF(levelLists[s, l], "m_SpellsProxy");
                }
            }
            try
            {
                for (int s = 0; s < schools.Length; s++)
                {
                    for (int l = 0; l < nLevels; l++)
                    {
                        stores[s, l].Clear();
                        stores[s, l].AddRange(buckets[s, l]);
                        SetField(levelLists[s, l], "m_SpellsFiltered", null);
                        SetField(levelLists[s, l], "m_SpellsProxy", null);
                    }
                    SetField(lists[s], "m_MaxLevel", 0);
                }
                for (int s = 0; s < schools.Length; s++)
                    for (int l = 0; l < nLevels; l++)
                    {
                        var lvl = levelLists[s, l];
                        var raw = Util.GetF(lvl, "m_Spells") as List<BlueprintAbilityReference>;
                        if (!ReferenceEquals(raw, stores[s, l])) throw new InvalidOperationException("Spellfist: " + lists[s].name + " level " + l + " m_Spells object changed during re-sync");
                        var actual = raw.Select(r =>
                        {
                            var a = r == null ? null : r.Get();
                            var sc = a == null ? null : a.GetComponent<SpellComponent>();
                            return new SchoolListRules.ReadbackEntry { Guid = a == null ? null : a.AssetGuid.ToString(), Name = a == null ? null : a.name, School = sc == null ? null : sc.School.ToString() };
                        }).ToList();
                        var mismatch = SchoolListRules.CompareLevel(schools[s].ToString(), buckets[s, l].Select(r => r.Guid.ToString()).ToList(), actual, g => "re-sync");
                        if (mismatch.Count > 0) throw new InvalidOperationException("Spellfist: " + lists[s].name + " level " + l + " re-sync read-back: " + string.Join("; ", mismatch));
                        if (Util.GetF(lvl, "m_SpellsFiltered") != null || Util.GetF(lvl, "m_SpellsProxy") != null)
                            throw new InvalidOperationException("Spellfist: " + lists[s].name + " level " + l + " caches not cleared after re-sync");
                    }
            }
            catch
            {
                for (int s = 0; s < schools.Length; s++)
                {
                    for (int l = 0; l < nLevels; l++)
                    {
                        stores[s, l].Clear();
                        stores[s, l].AddRange(oldSpells[s, l]);
                        SetField(levelLists[s, l], "m_SpellsFiltered", oldFiltered[s, l]);
                        SetField(levelLists[s, l], "m_SpellsProxy", oldProxy[s, l]);
                    }
                    SetField(lists[s], "m_MaxLevel", oldMax[s]);
                }
                throw;
            }
            return SchoolListRules.DescribeResync(diff, before.Count, after.Count);
        }

        // 레벨 0..SpellListLevels-1 의 SpellLevelList 를 m_Spells 를 채운 상태로 만든 뒤 목록에 단다(SpellsFiltered 캐시는 첫 접근 때 m_Spells 로 채워진다 — 정찰 §0-2).
        static BlueprintSpellList MakeList(string name, string guid, Func<int, List<BlueprintAbilityReference>> spellsAt)
        {
            var levels = new SpellLevelList[SpellfistRules.SpellListLevels];
            for (int l = 0; l < levels.Length; l++)
            {
                var sl = new SpellLevelList(l);
                var spells = spellsAt(l);
                SetField(sl, "m_Spells", spells);
                if (!ReferenceEquals(Util.GetF(sl, "m_Spells"), spells) || Util.GetF(sl, "m_SpellsFiltered") != null || sl.SpellLevel != l)
                    throw new InvalidOperationException("Spellfist: " + name + " level " + l + " m_Spells readback failed (or m_SpellsFiltered already cached)");
                levels[l] = sl;
            }
            var list = Create<BlueprintSpellList>(name, guid, b =>
            {
                b.IsMythic = false;
                b.SpellsByLevel = levels;
            });
            if (!ReferenceEquals(list.SpellsByLevel, levels)) throw new InvalidOperationException("Spellfist: " + name + " SpellsByLevel readback failed");
            return list;
        }
    }
}
