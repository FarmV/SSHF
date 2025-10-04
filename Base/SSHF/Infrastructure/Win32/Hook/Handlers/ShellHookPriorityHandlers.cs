using System.Threading.Tasks;

namespace FVH.SSHF.Infrastructure.Win32
{
    internal class ShellHookPriorityHandlers(ObserverExclusiveMode win32ObserverExclusiveMode, MsScreenClip msScreenClip)
    {
        readonly       ObserverExclusiveMode         _observerExclusiveMode = win32ObserverExclusiveMode;
        readonly       MsScreenClip                  _msScreenClip          = msScreenClip;
        private  const int                           osFirstBuildWin11       = 22000;
        internal void ShellHookHandler(HSHELL wParam, ref nint lParam, ref bool handled)
        {
            switch(wParam)
            {
                case HSHELL.GETMINRECT:                                                                                                                                                                     break;
                case HSHELL.WINDOWACTIVATED:                                                                                                                                                                break;
                case HSHELL.RUDEAPPACTIVATED:      if(App.OperatingSystem.Version.Build < osFirstBuildWin11)  _ = Task.Run(_observerExclusiveMode.CheckAndSetStateExcusiveModeAsync).ConfigureAwait(false); break;
                case HSHELL.WINDOWREPLACING:                                                                                                                                                                break;
                case HSHELL.WINDOWREPLACED:                                                                                                                                                                 break;
                case HSHELL.WINDOWCREATED:         _msScreenClip.SetStateIfWindowScreenclip(lParam);                                                                                                        break;
                case HSHELL.WINDOWDESTROYED:       if(App.OperatingSystem.Version.Build < osFirstBuildWin11) _ = Task.Run(_observerExclusiveMode.CheckAndSetStateExcusiveModeAsync).ConfigureAwait(false);  break;
                case HSHELL.ACTIVATESHELLWINDOW:                                                                                                                                                            break;
                case HSHELL.TASKMAN:                                                                                                                                                                        break;
                case HSHELL.REDRAW:                                                                                                                                                                         break;
                case HSHELL.FLASH:                                                                                                                                                                          break;
                case HSHELL.ENDTASK:                                                                                                                                                                        break;
                case HSHELL.APPCOMMAND:                                                                                                                                                                     break;
                case HSHELL.MONITORCHANGED:        if(App.OperatingSystem.Version.Build < osFirstBuildWin11) _ = Task.Run(_observerExclusiveMode.CheckAndSetStateExcusiveModeAsync).ConfigureAwait(false);  break;
                case HSHELL.LANGUAGE:                                                                                                                                                                       break;
                case HSHELL.SYSMENU:                                                                                                                                                                        break;
                case HSHELL.ACCESSIBILITYSTATE:                                                                                                                                                             break;
                case HSHELL.APPCOMMAND_DELETE:     if(App.OperatingSystem.Version.Build < osFirstBuildWin11) _ = Task.Run(_observerExclusiveMode.CheckAndSetStateExcusiveModeAsync).ConfigureAwait(false);  break;
                case HSHELL.APPCOMMAND_DWM_FLIP3D: if(App.OperatingSystem.Version.Build < osFirstBuildWin11) _ = Task.Run(_observerExclusiveMode.CheckAndSetStateExcusiveModeAsync).ConfigureAwait(false);  break;
                default:
#if DEBUG
                      System.Diagnostics.Debugger.Break();
#endif
                break;
            }
        }
    }
}