using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using WotrClassMod.Common;

namespace ShadowSorcerer
{
     
    public static class EchoDudLog
    {
        // 키가 없는 사유 = Strings.Resolve 가 던진다(조용한 빈 문구 금지).
        public static string Line(string reason) { return Strings.Resolve(ModContext.StringPrefix + "log.dud") + ": " + Strings.Resolve(ModContext.StringPrefix + "dud." + reason); }

        public static void Log(UnitEntityData caster, string reason, int count)
        {
            for (int i = 0; i < count; i++) ShadowErrors.CountDud(reason);
            string name = caster == null ? "?" : caster.CharacterName;
            ShadowErrors.Dud("echo", name + " x" + count + " " + reason);
            string line = name + " — " + Line(reason) + (count > 1 ? " (x" + count + ")" : "");
            bool delivered = EventBus.RaiseEvent<ILogMessageUIHandler>(h => h.HandleLogMessage(line), true);
            if (!delivered) ModContext.Log("ShadowSorcerer: echo dud (combat log not available): " + line);
        }
    }
}
