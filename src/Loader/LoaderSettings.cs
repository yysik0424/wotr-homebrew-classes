using System.Collections.Generic;
using UMM = UnityModManagerNet.UnityModManager;

namespace HomebrewClasses.Loader
{
    public sealed class LoaderSettings : UMM.ModSettings
    {
        public List<string> DisabledClasses = new List<string>();

        internal bool IsEnabled(string id) { return !DisabledClasses.Contains(id); }
        internal void SetEnabled(string id, bool enabled)
        {
            if (enabled) DisabledClasses.RemoveAll(item => item == id);
            else if (IsEnabled(id)) DisabledClasses.Add(id);
        }

        public override void Save(UMM.ModEntry entry) { Save(this, entry); }
    }
}
