using System;
using System.Collections.Generic;
using System.Reactive.Subjects;

using FVH.SSHF.Infrastructure.Interfaces;

namespace FVH.SSHF.Infrastructure.Interfaces
{
    //public interface IInvokeShortcuts
    //{
    //    IEnumerable<Shortcuts> GetShortcuts();
    //}

    public interface IBehaviorSubjectGlobalShortcuts
    {
        BehaviorSubject<IEnumerable<FVH.SSHF.KeyboardShortcut>> GetShortcutsAsObservable();
    }
}



