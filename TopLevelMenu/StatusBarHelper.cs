using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Packaging;
using System.Drawing;
using System.Windows.Forms;

namespace TopLevelMenu
{
    internal class StatusBarHelper
    {
        static public void SetOperationOn(AsyncPackage package, string message) {
#pragma warning disable VSTHRD101 // Avoid unsupported async delegates
            var t = new Task(async () => {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
                IVsStatusbar statusBar = await package.GetServiceAsync(typeof(SVsStatusbar)) as IVsStatusbar;
                object icon = (short)Microsoft.VisualStudio.Shell.Interop.Constants.SBAI_Find;
                int frozen;
                statusBar.IsFrozen(out frozen);
                if (frozen != 0)
                {   //Unfreeze status bar
                    statusBar.FreezeOutput(0);
                }
                statusBar.SetText(message);
                statusBar.Animation(1, ref icon);
                statusBar.FreezeOutput(1);
            }
#pragma warning restore VSTHRD101 // Avoid unsupported async delegates
            );
            t.Start();
            t.Wait( 1000 );
        }

        static public void SetOperationOff(AsyncPackage package)
        {
#pragma warning disable VSTHRD101 // Avoid unsupported async delegates
            var t = new Task(async () => {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
                IVsStatusbar statusBar = await package.GetServiceAsync(typeof(SVsStatusbar)) as IVsStatusbar;
                statusBar.FreezeOutput(0); 
                statusBar.SetText("");
                statusBar.Animation(0, null);
                statusBar.Clear();
            }
#pragma warning restore VSTHRD101 // Avoid unsupported async delegates
            );
            t.Start();
            t.Wait( 1000 );
        }
    }
}
