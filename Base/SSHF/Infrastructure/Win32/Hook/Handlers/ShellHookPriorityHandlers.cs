namespace FVH.SSHF.Infrastructure.Win32
{
    internal class ShellHookPriorityHandlers 
    {
        readonly ObserverExclusiveMode _observerExclusiveMode;
        readonly ObserverMsScreenClipExecuting _observerMsScreenClipExecuting;

        public ShellHookPriorityHandlers(
            ObserverExclusiveMode win32ObserverExclusiveMode, 
            ObserverMsScreenClipExecuting observerMsScreenClipExecuting)
        {
            _observerExclusiveMode = win32ObserverExclusiveMode;
            _observerMsScreenClipExecuting = observerMsScreenClipExecuting;
        }

        internal void ShellHookHandler(HSHELL wParam, ref nint lParam, ref bool handled)
        {
            switch(wParam)
            {
                case HSHELL.GETMINRECT:
                break;
                case HSHELL.WINDOWACTIVATED:
                break;
                case HSHELL.RUDEAPPACTIVATED:
                      if(lParam is not 0) _observerExclusiveMode.CheckAndSetStateExcusiveMode();
                break;
                case HSHELL.WINDOWREPLACING:
                break;
                case HSHELL.WINDOWREPLACED:
                break;
                case HSHELL.WINDOWCREATED:
                      _observerMsScreenClipExecuting.CheckAndSetStateMsScreenClipExecuting(ref lParam);
                break;
                case HSHELL.WINDOWDESTROYED:
                      _observerExclusiveMode.CheckAndSetStateExcusiveMode();
                break;
                case HSHELL.ACTIVATESHELLWINDOW:
                break;
                case HSHELL.TASKMAN:
                break;
                case HSHELL.REDRAW:
                break;
                case HSHELL.FLASH:
                break;
                case HSHELL.ENDTASK:
                break;
                case HSHELL.APPCOMMAND:
                break;
                case HSHELL.MONITORCHANGED:
                      _observerExclusiveMode.CheckAndSetStateExcusiveMode();
                break;
                case HSHELL.LANGUAGE:
                break;
                case HSHELL.SYSMENU:
                break;
                case HSHELL.ACCESSIBILITYSTATE:
                break;
                case HSHELL.APPCOMMAND_DELETE:
                      _observerExclusiveMode.CheckAndSetStateExcusiveMode();
                break;
                case HSHELL.APPCOMMAND_DWM_FLIP3D:
                      _observerExclusiveMode.CheckAndSetStateExcusiveMode();
                break;
                default:
#if DEBUG
                      System.Diagnostics.Debugger.Break();
#endif
                break;
            }
        }
    }
}