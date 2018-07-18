using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using NuGet.VisualStudio;

namespace NuGet.PackageManagement.UI
{
    internal class InfiniteScrollListBoxAutomationPeer : ListBoxAutomationPeer
    {
        public InfiniteScrollListBoxAutomationPeer(ListBox owner) : base(owner) { }

        protected override List<AutomationPeer> GetChildrenCore()
        {
            var infiniteScrollListBox = Owner as InfiniteScrollListBox;

            if (infiniteScrollListBox == null)
            {
                return new List<AutomationPeer>();
            }

            return NuGetUIThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await infiniteScrollListBox.ItemsLock.ExecuteAsync(
                    delegate
                    {
                    // Don't return the LoadingStatusIndicator as an AutomationPeer, otherwise narrator will report it as an item in the list of packages, even when not visible
                    var sd = base.GetChildrenCore()?.Where(lbiap => !(((ListBoxItemAutomationPeer)lbiap).Item is LoadingStatusIndicator)).ToList() ?? new List<AutomationPeer>();
                });
            });
        }
    }
}