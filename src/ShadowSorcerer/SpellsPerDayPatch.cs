using Kingmaker.UnitLogic;
using Newtonsoft.Json;

namespace ShadowSorcerer
{
    // Kept for saves written before the flat daily-slot penalty was retired.
    public class UnitPartShadowSlotMigration : UnitPart
    {
        [JsonProperty] public bool Applied;
    }
}
