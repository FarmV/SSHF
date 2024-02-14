﻿using System.Threading;
using System.Threading.Tasks;

namespace SSHF.Infrastructure.Interfaces
{
    public interface IWindowPositionUpdater
    {
        bool IsUpdateWindow { get; }
        Task UpdateWindowPos(CancellationToken token);
        Task DragMove();
    }
}

