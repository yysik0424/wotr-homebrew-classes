using System;
using System.Linq;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Mechanics;

namespace SorrowInquisitor
{
    public sealed class BeadStatus : UnitBuffComponentDelegate
    {
        sealed class Display : IUIDataProvider
        {
            readonly Bead bead;
            public Display(Bead bead) { this.bead = bead; }
            public string Name => string.Format(Rosary.Text("bead.title"), bead.Blueprint.Name);
            public string NameForAcronym => Name;
            public string Description => string.Format(Rosary.Text("bead.origin"),bead.AllyName,bead.Blueprint.Name);
            public UnityEngine.Sprite Icon => bead.Blueprint.Icon ?? Blueprints.Stored.Icon;
        }
        internal static void Show(UnitEntityData u, Bead bead)
        {
            var ctx = new MechanicsContext(u,u,Blueprints.Stored,null,u);
            var marker = new Buff(Blueprints.Stored,ctx,TimeSpan.FromSeconds(Rules.StorageSeconds)) { IsNotDispelable = true };
            marker.UIDataProviderOverride = new Display(bead);
            var added = u.Facts.Add(marker);
            if (ReferenceEquals(added,marker)) bead.MarkerId = marker.UniqueId;
        }
        internal static void Remove(UnitEntityData u, Bead bead)
        {
            if (bead.MarkerId == null) return;
            u.Buffs.Enumerable.FirstOrDefault(b => b.UniqueId == bead.MarkerId)?.Remove();
        }
        internal static void RestoreDisplay(UnitEntityData u, Bead bead)
        {
            if (bead.MarkerId == null) return;
            var marker = u.Buffs.Enumerable.FirstOrDefault(b => b.UniqueId == bead.MarkerId);
            if (marker != null) marker.UIDataProviderOverride = new Display(bead);
        }
        protected override void OnTurnOn()
        {
            var bead = Owner.Get<SorrowPart>()?.Beads.FirstOrDefault(b => b.MarkerId == Buff.UniqueId);
            if (bead != null) Buff.UIDataProviderOverride = new Display(bead);
        }
    }
}
