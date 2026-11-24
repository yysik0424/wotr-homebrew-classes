namespace WotrClassMod.Common
{
    // 모드가 리플렉션(GetField/GetMethod/GetProperty/Set/GetF/SetField)으로 찾는 게임 멤버 원장의 형식.
    // 게임 타입을 참조하지 않는다(문자열만) → 콘솔 시험(tests\CommonTests.cs)이 이 파일을 컴파일해 dnlib 으로 Assembly-CSharp.dll 과 대조한다(오프라인 잠금).
    // 이 파일에 Kingmaker 등 게임 using 을 더하지 말 것: 시험 exe 컴파일이 깨진다.
    // Walk = 코드가 기반 형식까지 올라가며 찾는다(BlueprintUtil.FindField/SetField). Exact = 질의한 바로 그 형식에 선언돼 있어야 한다(GetField 직접 호출, 기반 형식은 찾지 않음).
    public enum LookupKind { Field, StaticField, Method, Property }
    public enum LookupMode { Walk, Exact }

    public sealed class ReflectionLookup
    {
        public readonly string Assembly;   // "Assembly-CSharp" 또는 "mscorlib"(= 실제 리플렉션으로 확인)
        public readonly string TypeFullName;
        public readonly string Member;
        public readonly LookupKind Kind;
        public readonly LookupMode Mode;
        public ReflectionLookup(string assembly, string type, string member, LookupKind kind, LookupMode mode)
        { Assembly = assembly; TypeFullName = type; Member = member; Kind = kind; Mode = mode; }
    }

    // src\Common 코드가 리플렉션으로 찾는 멤버. 모드 원장(<모드>.ReflectionLookups.All)과 합쳐서 시험한다(CommonLookups.All ∪ ReflectionLookups.All).
    // 코드에 새 이름 리터럴을 더하면 여기에도 더해야 한다: 시험이 소스를 읽어 원장에 없는 리터럴을 실패시킨다.
    public static class CommonLookups
    {
        public static readonly ReflectionLookup[] All =
        {
            // BlueprintUtil.IsCached (GetField 직접 호출 = Exact). 선언 형식은 dnlib 확인 2026-11-07 (Assembly-CSharp.dll).
            new ReflectionLookup("Assembly-CSharp", "Kingmaker.Blueprints.JsonSystem.BlueprintsCache", "m_LoadedBlueprints", LookupKind.Field, LookupMode.Exact),
            // BlueprintUtil.CloneUnit (GetField 직접 호출 = Exact)
            new ReflectionLookup("Assembly-CSharp", "Kingmaker.Blueprints.BlueprintUnit", "m_AddFacts", LookupKind.Field, LookupMode.Exact),
            // BlueprintUtil.CloneUnit (typeof(object).GetMethod = Exact)
            new ReflectionLookup("mscorlib", "System.Object", "MemberwiseClone", LookupKind.Method, LookupMode.Exact),
             
            new ReflectionLookup("Assembly-CSharp", "Kingmaker.Blueprints.Classes.AddClassLevels", "m_CharacterClass", LookupKind.Field, LookupMode.Walk),
            new ReflectionLookup("Assembly-CSharp", "Kingmaker.Blueprints.Classes.AddClassLevels", "m_Archetypes", LookupKind.Field, LookupMode.Walk),
            new ReflectionLookup("Assembly-CSharp", "Kingmaker.Blueprints.Classes.AddClassLevels", "m_SelectSpells", LookupKind.Field, LookupMode.Walk),
            new ReflectionLookup("Assembly-CSharp", "Kingmaker.Blueprints.Classes.AddClassLevels", "m_MemorizeSpells", LookupKind.Field, LookupMode.Walk),
            new ReflectionLookup("Assembly-CSharp", "Kingmaker.Blueprints.Classes.SelectionEntry", "m_Selection", LookupKind.Field, LookupMode.Walk),
            new ReflectionLookup("Assembly-CSharp", "Kingmaker.Blueprints.Classes.SelectionEntry", "m_Features", LookupKind.Field, LookupMode.Walk),
        };
    }
}
