namespace FVH.SSHF.Infrastructure.Win32
{
    internal enum HSHELL : uint
    {
        GETMINRECT = 5U,
        WINDOWACTIVATED = 4U,
        RUDEAPPACTIVATED = 32772U,
        WINDOWREPLACING = 14U,
        WINDOWREPLACED = 13U,
        WINDOWCREATED = 1U,
        WINDOWDESTROYED = 2U,
        ACTIVATESHELLWINDOW = 3U,
        TASKMAN = 7U,
        REDRAW = 6U,
        FLASH = 32774U,
        ENDTASK = 10U,
        APPCOMMAND = 12U,
        MONITORCHANGED = 16U,
        LANGUAGE = 8U,
        SYSMENU = 9U,
        ACCESSIBILITYSTATE = 11U,
        APPCOMMAND_DELETE = 53U,  // Происходит при появлении окна системного выбора окон => ALT + TAB, WIN + TAB
        APPCOMMAND_DWM_FLIP3D = 54U // Происходит при закрытии окна системного выбора окон => ALT + TAB, WIN + TAB
    }
}