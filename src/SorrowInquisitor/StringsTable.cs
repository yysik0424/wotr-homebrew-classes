using System.Collections.Generic;
namespace SorrowInquisitor
{
    internal static class StringsTable
    {
        internal static readonly Dictionary<string, (string ko, string en)> Table = new Dictionary<string, (string ko, string en)> {
            { "archetype.name", ("비탄의 심문관", "Sorrow Inquisitor") },
            { "archetype.short", ("동료가 받은 고통을 납 묵주에 거두고 적에게 선고하는 인퀴지터.", "An inquisitor who gathers an ally's suffering into a lead rosary and sentences an enemy to bear it.") },
            { "archetype.desc", ("납 묵주의 알마다 동료가 겪은 고통 하나를 새깁니다. 인퀴지터의 엄한 시선, 단독 전술, 팀워크 보너스 특기를 묵주와 선고로 교환합니다. 주문, 영역, 심판과 파멸을 얻습니다. 거둔 상태이상은 다음 휴식까지 묵주에 남습니다.", "Each lead bead bears the suffering of an ally. Trade stern gaze, solo tactics and all six bonus teamwork feats for the rosary and its sentences. Gain the inquisitor's spells, domain, judgment and bane. A gathered affliction stays in its bead until your next rest.") },
            { "bead.title", ("묵주 · {0}", "Rosary · {0}") },
            { "bead.origin", ("{0}에게서 거둔 {1}.", "{1}, gathered from {0}.") },
            { "rosary.name", ("비탄의 묵주", "Rosary of Sorrows") },
            { "until.rest", ("다음 휴식까지", "Until next rest") },
            { "rosary.desc", ("동료의 상태이상 한 원인을 묵주 한 알에 다음 휴식까지 보관합니다. 1레벨에 한 칸을 얻고 이후 5레벨마다 한 칸씩 늘어나 20레벨에 다섯 칸이 됩니다. 휴식하면 묵주가 비고 거두기 횟수가 회복됩니다.", "Store one affliction per bead until your next rest. Gain one slot at level 1 and another every five levels, up to five. Rest clears the rosary and restores its uses.") },
            { "gather.name", ("비탄 거두기", "Gather Suffering") },
            { "gather.desc", ("표준 행동으로 30피트 안의 다른 동료에게서 효과를 골라 빈 칸에 거둡니다. 하루 3 + 지혜 수정치 회(최소 1회). 여러 효과를 함께 거둬도 한 번만 소모합니다. 능력치 직접 감소, 음의 레벨, 차원 고정은 남습니다.", "As a standard action, gather selected afflictions from another ally within 30 feet. Use 3 + Wisdom modifier times per day (minimum 1). Gathering several afflictions together costs one use. Direct ability loss, negative levels and dimensional anchor stay with the ally.") },
            { "discard.name", ("묵주 살펴보기", "Examine the Rosary") },
            { "discard.desc", ("다음 휴식까지 보관할 효과를 살핍니다. 선택한 알을 비우는 것은 자유 행동입니다.", "Inspect the afflictions held until your next rest. Empty selected beads as a free action.") },
            { "sentence.name", ("비탄의 선고", "Sentence of Suffering") },
            { "sentence.desc", ("표준 행동으로 30피트 안의 적에게 한 효과를 선고합니다. 원래 내성(없으면 의지)을 굴립니다. DC는 10 + 인퀴지터 레벨의 절반 + 지혜 수정치입니다. 지속시간은 거둔 시점의 남은 시간과 3라운드 중 짧은 쪽입니다. 모든 선고는 라운드당 방출 한 번을 공유합니다.", "As a standard action, sentence one foe within 30 feet to one affliction. The foe attempts the original save, or Will if none, at DC 10 + half inquisitor level + Wisdom modifier. Duration is the remaining captured duration, up to 3 rounds. All sentences share one discharge per round.") },
            { "move.name", ("비탄의 선고 · 이동", "Sentence of Suffering (Move)") },
            { "move.desc", ("6레벨부터 이동 행동으로 최대 두 효과를 선고합니다. 11레벨에는 셋, 16레벨에는 넷을 선고합니다. 효과마다 대상 제한과 내성을 따로 판정합니다.", "Starting at level 6, sentence a foe to two afflictions as a move action. This rises to three at level 11 and four at level 16. Check target eligibility and saves separately for each affliction.") },
            { "swift.name", ("비탄의 선고 · 신속", "Sentence of Suffering (Swift)") },
            { "swift.desc", ("이동 선고와 같은 한도로 신속 행동을 선택할 수 있습니다. 칼끝과 일반 선고를 합쳐 라운드마다 한 번 방출합니다.", "Use a swift action with the same limit as the move-action sentence. The blade and both ordinary sentences share one discharge each round.") },
            { "blade.name", ("칼끝의 선고", "Sentence on the Blade") },
            { "blade.desc", ("파멸이 활성화된 동안 자유 행동으로 효과를 싣습니다. 이번 턴에 파멸이 걸린 무기가 적에게 처음 명중하면 방출합니다. 빗나가면 묵주의 알은 남습니다. 5·11·16레벨에 각각 한 개·두 개·세 개를 실을 수 있습니다.", "While bane is active, load beads as a free action. Your first hit on an enemy with a bane-imbued weapon this turn releases them. Misses preserve your beads. Load one at level 5, two at 11, and three at 16.") },
            { "reprisal.name", ("응보", "Reprisal") },
            { "reprisal.desc", ("원래 가해자에게 돌려주는 효과의 선고 DC가 2 오릅니다.", "Increase the save DC by 2 for each affliction returned to its original aggressor.") },
            { "vow.name", ("대신 지는 서원", "Vow to Bear the Burden") },
            { "vow.desc", ("이동 행동으로 30피트 안의 다른 동료를 1라운드 보호합니다. 동료에게 실제로 걸린 첫 적합한 효과를 곧바로 거둡니다. 성공 시 빈 칸과 하루 거두기 횟수 하나를 사용합니다. 한 번에 한 동료에게 서원합니다.", "As a move action, guard one other ally within 30 feet for 1 round. Gather the first eligible affliction that actually lands. Success uses one free slot and one daily gather. You may guard one ally at a time.") },
            { "divine.name", ("비탄의 결산", "Reckoning of Sorrows") },
            { "divine.desc", ("16레벨부터 실제로 적용한 효과마다 신성 피해 2d10을 더합니다. 한 선고의 피해를 합쳐 한 번 굴립니다. 일반 선고 최대 8d10, 칼끝 최대 6d10이며 치명타로 배증하지 않습니다.", "From level 16, deal 2d10 divine damage for each affliction successfully applied. Roll one combined packet, up to 8d10 for an ordinary sentence or 6d10 for the blade. A critical hit does not multiply it.") },
            { "residual.name", ("남은 상흔", "Residual Affliction") },
            { "residual.desc", ("거두기 이후에도 본래 대상에게 남은 효과입니다.", "The portion of an affliction that remains with its original bearer.") },
            { "unavailable", ("남은 묵주 칸, 하루 횟수, 보관한 효과와 이번 라운드의 선고 횟수를 확인하세요.", "Check free slots, daily uses, stored afflictions and this round's discharge.") },
            { "target", ("거두기와 서원은 다른 동료를, 선고는 적을 지정하세요.", "Choose another ally for gathering or a vow, and an enemy for a sentence.") },
            { "counts", ("{0}  |  묵주 {1}/{2}  |  남은 거두기 {3}회  |  선택 한도 {4}", "{0}  |  Beads {1}/{2}  |  Gathers left {3}  |  Selection limit {4}") },
            { "empty", ("고를 수 있는 효과가 없습니다.", "No available afflictions.") },
            { "cancel", ("닫기", "Close") },
            { "confirm", ("확정", "Confirm") },
            { "environment", ("환경", "Environment") },
            { "stage", (" · 경과 주기 {0}", " · ticks elapsed {0}") },
            { "log.gather", ("{0}이(가) {1}에게서 {2}을(를) 거뒀습니다.", "{0} gathered {2} from {1}.") },
            { "log.applied", ("{0}에게 {1} 선고가 적용됐습니다.", "{0} suffers the sentence of {1}.") },
            { "log.saved", ("{0}이(가) {1} 선고의 내성 굴림에 성공했습니다.", "{0} saved against the sentence of {1}.") },
            { "log.immune", ("{0}에게 {1} 선고가 적용되지 않았습니다.", "{0} was unaffected by the sentence of {1}.") },
        };
    }
}
