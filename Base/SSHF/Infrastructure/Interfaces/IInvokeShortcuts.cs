using System.Collections.Generic;

namespace FVH.SSHF.Infrastructure.Interfaces
{
    public interface IInvokeShortcuts
    {
        IEnumerable<Shortcuts> GetShortcuts();
    }
}
