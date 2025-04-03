using System;

using FVH.SSHF.FastWindowArea;

namespace FVH.SSHF.FastWindowArea
{
    internal class OneFastWindow : IDisposable
    {
        internal bool IsDisposed = false;
        internal OneFastWindow
        (
            FastWindow fastWindow,
            FastWindowViewModelDependencies fastWindowViewModelDependencies,
            FastWindowExternalConditions mainWindowExternalConditions,
            FastWindowCommand fastWindowCommand
        )
        {
            FastWindow = fastWindow;
            FastWindowViewModelDependencies = fastWindowViewModelDependencies;
            MainWindowExternalConditions = mainWindowExternalConditions;
            FastWindowCommand = fastWindowCommand;
        }
        internal FastWindow FastWindow { get; init; }
        internal FastWindowViewModelDependencies FastWindowViewModelDependencies { get; init; }
        internal FastWindowExternalConditions MainWindowExternalConditions { get; init; }
        internal FastWindowCommand FastWindowCommand { get; init; }

        public void Dispose()
        {
            if(IsDisposed is true) return;
            IsDisposed = true;
            FastWindow.Close();
            FastWindowViewModelDependencies.Dispose();
            IsDisposed = true;
        }
    }
}

