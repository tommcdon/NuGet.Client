using System.Threading;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using Microsoft.VisualStudio.Threading;
using NuGet.VisualStudio;

namespace NuGet.PackageManagement.UI
{ 
    internal class InfiniteScrollListBox : ListBox
    {
        public readonly ReentrantSemaphore ItemsLock =
            ReentrantSemaphore.Create(
                initialCount: 1,
                joinableTaskContext: NuGetUIThreadHelper.JoinableTaskFactory.Context,
                mode: ReentrantSemaphore.ReentrancyMode.Stack);

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new InfiniteScrollListBoxAutomationPeer(this);
        }
    }
}