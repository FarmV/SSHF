﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FVH.Background.Input.Infrastructure.Interfaces
{
    public interface IInvoke
    {
        public Task InvokeFunctions(IEnumerable<IRegFunction> toTaskInvoke);
    }
}
