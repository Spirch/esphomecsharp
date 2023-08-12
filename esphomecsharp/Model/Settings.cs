﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace esphomecsharp.Model
{
    public sealed class Settings
    {
        public string DBFileName { get; init; }
        public Dictionary<string, string> Pages { get; init; }
    }
}