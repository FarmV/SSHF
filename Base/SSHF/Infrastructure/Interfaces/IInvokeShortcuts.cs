using System.Collections.Generic;

namespace SSHF.Infrastructure.Interfaces
{
    public interface IInvokeShortcuts
    {
        IEnumerable<Shortcuts> GetShortcuts();
    }
}
