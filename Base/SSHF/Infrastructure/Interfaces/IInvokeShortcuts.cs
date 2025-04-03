using System;
using System.Collections.Generic;

using FVH.SSHF.Infrastructure.Interfaces;

namespace FVH.SSHF.Infrastructure.Interfaces
{
    public interface IBehaviorSubjectGlobalShortcuts
    {
        R3.BehaviorSubject<IEnumerable<FVH.SSHF.KeyboardShortcut>> GetShortcutsAsObservable();
    }
}



