﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.IO;

namespace AxeWindowsCLI
{
    class ScanDelay : IScanDelay
    {
        private readonly TextWriter _writer;
        private readonly Action _oneSecondDelay;

        internal ScanDelay(TextWriter writer, Action oneSecondDelay)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            if (oneSecondDelay == null) throw new ArgumentNullException(nameof(oneSecondDelay));

            _writer = writer;
            _oneSecondDelay = oneSecondDelay;
        }

        public void DelayWithCountdown(IOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            if (options.DelayInSeconds > 0)
            {
                ConditionallyWriteMessageWithCount(options, "Delaying {0} second{1} before scanning.", options.DelayInSeconds);
                for (int secondsRemaining = options.DelayInSeconds; secondsRemaining > 0; secondsRemaining--)
                {
                    ConditionallyWriteMessageWithCount(options, "  {0} second{1} before scan.", secondsRemaining);
                    _oneSecondDelay.Invoke();
                }
                ConditionallyWriteMessage(options, "Triggering scan");
            }
        }

        private void ConditionallyWriteMessageWithCount(IOptions options, string format, int count)
        {
            if (options.VerbosityLevel > VerbosityLevel.Quiet)
            {
                _writer.WriteLine(format, count, PluralSuffix(count));
            }
        }

        private void ConditionallyWriteMessage(IOptions options, string message)
        {
            if (options.VerbosityLevel > VerbosityLevel.Quiet)
            {
                _writer.WriteLine(message);
            }
        }

        private static string PluralSuffix(int count)
        {
            return count > 1 ? "s" : "";
        }
    }
}