﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;

namespace Axe.Windows.SystemAbstractions
{
    internal class SystemEnvironment : ISystemEnvironment
    {
        public string CurrentDirectory => Environment.CurrentDirectory;
    } // class
} // namespace
