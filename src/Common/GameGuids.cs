namespace WotrClassMod.Common
{
    // 공용 코드(Strings 한국어 판별, 점검 하네스)가 읽는 본편 블루프린트 GUID. 게임 타입 없이 문자열만(콘솔 시험이 컴파일한다).
    // 모드 전용 본편 GUID 는 각 모드의 Guids.Game 에 둔다(같은 값을 두 곳에 두지 않는다 — 시험이 중복을 실패시킨다).
    public static class GameGuids
    {
        // 출처: blueprints.zip 2026-11-06 대조 (원 설계 문서 §8).
        public const string HunterClass = "34ecd1b5e1b90b9498795791b0855239";

        // 출처: blueprints.zip 2026-11-07 덤프 Classes/Fighter/FighterClass.jbp AssetId (재확인 완료, 2026-11-07).
        public const string FighterClass = "48ac8db94d5de7645906c7d0ad3bcfbd";

        // 출처: blueprints.zip 2026-11-07 덤프 Classes/Magus/MagusClass.jbp AssetId (2026-11-07 엔진 정찰 보고서 「읽은 본편 블루프린트」 표).
        public const string MagusClass = "45a4607686d96a1498891b3286121780";

        // --- 게임 안 점검 전용. 일반 플레이 경로는 읽지 않는다. ---
        // 용병 템플릿: blueprints.zip Root/CustomCompanion.jbp AssetId = BlueprintRoot.m_CustomCompanion (Root/BlueprintRoot.jbp, 2026-11-07).
        // m_Faction = Player(72f24026…), m_Race = null → 점검 유닛 생성 때 HumanRace 를 넣는다. 실행 때 BlueprintRoot.CustomCompanion 과 같은지 검사.
        public const string CustomCompanionUnit = "baaff53a675a84f4983f1e2113b24552";
        // blueprints.zip Races/HumanRace.jbp AssetId (2026-11-07). 용병 템플릿에 종족이 없어서 레벨업 전에 SetRace 로 넣는다.
        public const string HumanRace = "0a5d473ead98b0646b94495af250fdc4";
        // 적대 저레벨 유닛: blueprints.zip Units/Monsters/Kobold/CR0.5_KoboldMeleeWarriorLevel1.jbp AssetId (2026-11-07).
        // Experience.CR = 1(정수 필드, 이름은 CR0.5), m_Faction = Factions/Mobs.jbp(0f539bab…, m_AttackFactions 에 Player 포함).
        public const string HostileKobold = "d0ed34c1f6600e04592e7921c1dd6ff1";
    }
}
