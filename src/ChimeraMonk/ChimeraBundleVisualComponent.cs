using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.PubSubSystem;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic;
using Kingmaker.Visual.CharacterSystem;
using WotrClassMod.Common;

namespace ChimeraMonk
{
    // 자체 부품 ③(소유자 결정 2026-11-16 3번): 변이 외형을 사실(변이 특성)에 붙인다. 두 모드(VisualKind):
    //   SingleAsset(꼬리) = 에셋 하나를 종족·성별 무관하게 / RaceBundle(뿔) = 종족 × 성별 묶음(KingmakerEquipmentEntity)에서 주인에 맞는 에셋.
    // 본편 AddEquipmentEntity 를 쓰지 않는 이유: 묶음을 못 받고(에셋 1개), 에셋이 로드되지 않으면 조용히 지나간다(Character.AddEquipmentEntity IL @000E~001B) — 여기서는 오류로 센다.
    // 수명 주기는 본편 AddEquipmentEntity 의 IL 과 같다(dnlib 2026-11-16 직독). 판단은 전부 순수 VisualPlan(시험 exe 가 실행) — 이 형식은 엔진 값을 읽어 넘기고 결과를 실행만 한다:
    //   OnTurnOn = Execute(true) · OnTurnOff = Execute(false) · HandleUnitViewAttached = OnTurnOn()(뷰가 다시 만들어질 때 — 불러오기·지역 이동·변신 종료 — 다시 붙는다).
    //   주인이 펫(동물 동료 등)이면 붙이지 않는다(소유자 결정 2026-11-16) — 판정은 VisualPlan.Lifecycle 의 pet 입력, 끔은 펫이어도 뗀다. 펫인지 알 수 없으면 붙이지 않고 오류로 보고.
    //   Execute: VisualPlan.Lifecycle(켬/끔, IsReapplying, SaveManager.CurrentState, 뷰 유무, 아바타 유무) → VisualPlan.Run(결과, AttachAll, DetachAll).
    //   붙일 에셋: VisualPlan.Decide(모드, 에셋, 묶음 모양, 변신 종족, 본 종족, 성별) — 종족은 본편 UnitEntityView.GetActualRace 와 같은 우선순위(변신 종족 먼저).
    //   엔진 KingmakerEquipmentEntity.GetLinks 는 종족 칸이 비면 종족 무관 배열로 조용히 떨어지므로 쓰지 않는다.
    // 주인 종족·성별의 칸이 없음 = 명시적 「외형 없음」: 아무것도 붙이지 않고 WARN 1회(직업·캐릭터·종족 이름) — 오류 계수에 넣지 않는다(변이의 규칙 효과는 그대로).
    // 묶음 블루프린트가 사라짐 · 에셋이 로드되지 않음 = 오류(ChimeraErrors.ReportOnce).
    // 시험 `visual: lock` 이 IL 로 묶는다: OnTurnOn 은 Execute(1), OnTurnOff 는 Execute(0), HandleUnitViewAttached 는 OnTurnOn, Run 의 인자 순서 = (AttachAll, DetachAll),
    //   AttachAll 만 Character.AddEquipmentEntity, DetachAll 만 RemoveEquipmentEntity, 저장 상태·성별은 비교 없이 VisualPlan 으로 넘김.
    public class ChimeraBundleVisualComponent : UnitFactComponentDelegate, IUnitViewAttachedHandler
    {
        // Visuals.Build 가 VisualPlan 행에서 넣는다.
        public VisualKind Kind;
        public string BundleGuid;   // 본편 묶음 블루프린트 GUID(RaceBundle: 런타임에 읽는다 / SingleAsset: 출처 기록 — 빌드 때 에셋 id 를 이 묶음과 대조했다)
        public string AssetId;      // SingleAsset 의 에셋 id(RaceBundle 은 null)

        static readonly WarnOnce warned = new WarnOnce();
        // 에셋 id → 링크. Load 가 링크마다 번들 핸들을 잡으므로 에셋당 링크 하나만 만든다. 이 목록(과 핸들)은 게임 세션 동안 남는다 — 해제하지 않는다.
        //  상한 = 이 모드가 쓰는 외형 에셋 수(꼬리 1 + 뿔 묶음의 서로 다른 에셋 20). 세이브에는 들어가지 않는다.
        static readonly Dictionary<string, EquipmentEntityLink> links = new Dictionary<string, EquipmentEntityLink>();

        protected override void OnTurnOn() { Execute(true); }

        protected override void OnTurnOff() { Execute(false); }

        public void HandleUnitViewAttached() { OnTurnOn(); }

        void Execute(bool turnOn)
        {
            try
            {
                var pet = ReadPet();
                var view = Owner.View;
                bool hasView = view != null;
                bool hasAvatar = hasView && view.CharacterAvatar != null;
                VisualPlan.Run(VisualPlan.Lifecycle(turnOn, IsReapplying, (int)Game.Instance.SaveManager.CurrentState, hasView, hasAvatar, pet), AttachAll, DetachAll, ReportPetUnknown);
            }
            catch (Exception e)
            {
                ChimeraErrors.ReportException(VisualPlan.Site, e);
            }
        }

        // 펫 여부(소유자 결정 2026-11-16): 엔진이 펫 판정에 쓰는 멤버 두 개(UnitEntityData.IsPet = UnitPartPet 부품 · Master = UnitPartPet.Master, dnlib 2026-11-16 직독)를
        //  비교 없이 VisualPlan.PetStatus 로 넘긴다. 읽다가 던지면 「못 읽음」으로 넘긴다(→ Unknown — 펫 아님으로 읽지 않는다). 시험 `visual: lock` 이 비교·분기 없음을 IL 로 잠근다.
        string petReadError;
        PetState ReadPet()
        {
            bool isPet = false;
            object master = null;
            bool read = false;
            try
            {
                isPet = Owner.IsPet;
                master = Owner.Master;
                read = true;
            }
            catch (Exception e)
            {
                petReadError = e.GetType().Name + ": " + e.Message;
            }
            return VisualPlan.PetStatus(read, isPet, master);
        }

        void ReportPetUnknown(string reason)
        {
            ChimeraErrors.ReportOnce(VisualPlan.Site, "pet-unknown|" + Owner.UniqueId, "cannot tell whether " + Owner.CharacterName + " is a pet (" + reason + (petReadError == null ? ": IsPet and Master disagree" : ": " + petReadError) + ") - no " + Kind + " visual attached, the mutation's rules are unchanged");
        }

        void AttachAll()
        {
            var avatar = Owner.View.CharacterAvatar;
            foreach (var link in Links())
            {
                // Character.AddEquipmentEntity 는 에셋이 null 이면 조용히 반환한다 → 먼저 로드해 보고 없으면 오류로 센다.
                if (link.Load(true, false) == null)
                {
                    ChimeraErrors.ReportOnce(VisualPlan.Site, "asset-missing|" + link.AssetId, "visual asset " + link.AssetId + " (" + Kind + ", bundle " + BundleGuid + ") did not load - nothing attached");
                    continue;
                }
                avatar.AddEquipmentEntity(link, false, -1, -1);
            }
        }

        void DetachAll()
        {
            var avatar = Owner.View.CharacterAvatar;
            foreach (var link in Links()) avatar.RemoveEquipmentEntity(link, false);
        }

        // 붙이거나 뗄 링크 목록. 「외형 없음」·오류 = 빈 목록(각각 WARN 1회 / 오류 보고 뒤).
        List<EquipmentEntityLink> Links()
        {
            var list = new List<EquipmentEntityLink>();
            BundleShape shape = null;
            string bundleName = BundleGuid;
            if (Kind == VisualKind.RaceBundle)
            {
                if (string.IsNullOrEmpty(BundleGuid)) throw new InvalidOperationException("ChimeraMonk: ChimeraBundleVisualComponent (RaceBundle) without a bundle GUID");
                var bundle = ResourcesLibrary.TryGetBlueprint(BlueprintGuid.Parse(BundleGuid)) as KingmakerEquipmentEntity;
                if (bundle == null)
                {
                    ChimeraErrors.ReportOnce(VisualPlan.Site, "bundle-missing|" + BundleGuid, "visual bundle " + BundleGuid + " is not a KingmakerEquipmentEntity in the blueprint cache - nothing attached");
                    return list;
                }
                shape = Visuals.ShapeOf(bundle);
                bundleName = bundle.name;
            }
            // 종족: 본편 UnitEntityView.GetActualRace 와 같은 출처 둘(활성 변신의 종족, 본 종족). 어느 것을 쓸지는 VisualPlan.ActualRaceIndex 가 정한다. 없음 = null(인간으로 바꾸지 않는다).
            var polymorph = Owner.GetActivePolymorph().Component;
            var polymorphRace = polymorph == null ? null : polymorph.Race;
            var ownRace = Owner.Progression.Race;
            int? polymorphIndex = polymorphRace == null ? (int?)null : (int)polymorphRace.RaceId;
            int? ownIndex = ownRace == null ? (int?)null : (int)ownRace.RaceId;
            var pick = VisualPlan.Decide(Kind, AssetId, shape, polymorphIndex, ownIndex, (int)Owner.Gender);
            if (pick.State == VisualState.NoVisual)
            {
                string raceName = polymorphRace != null ? polymorphRace.name + " (polymorph)" : ownRace != null ? ownRace.name : "<no race>";
                if (warned.First(Kind + "|" + BundleGuid + "|" + raceName + "|" + Owner.Gender))
                    ModContext.Log("ChimeraMonk: WARN visual: Chimera Monk " + Owner.CharacterName + " (race " + raceName + ", " + Owner.Gender + "): bundle " + bundleName + " has no entry for this race - no visual attached, the mutation's rules are unchanged (" + pick.Reason + ")");
                return list;
            }
            if (pick.State != VisualState.Attach || pick.Assets.Count == 0) throw new InvalidOperationException("ChimeraMonk: VisualPlan.Decide returned " + pick.State + " with " + pick.Assets.Count + " asset(s)");
            foreach (var id in pick.Assets)
            {
                EquipmentEntityLink link;
                if (!links.TryGetValue(id, out link)) { link = new EquipmentEntityLink { AssetId = id }; links.Add(id, link); }
                list.Add(link);
            }
            return list;
        }
    }
}
