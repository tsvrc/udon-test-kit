using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tsvrc.UdonTestKit.Tests.Helpers
{
    public static class Logs
    {
        // The messages logged while act runs. A test that checks the errors themselves ignores failing
        // messages, which Unity Test Framework otherwise fails the test on.
        public static List<(LogType Type, string Message)> During(Action act, bool ignoreFailingMessages = false)
        {
            var logs = new List<(LogType, string)>();
            Application.LogCallback record = (message, stackTrace, type) => logs.Add((type, message));
            Application.logMessageReceived += record;
            LogAssert.ignoreFailingMessages = ignoreFailingMessages;
            try
            {
                act();
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
                Application.logMessageReceived -= record;
            }
            return logs;
        }

        public static IEnumerable<string> Messages(this IEnumerable<(LogType Type, string Message)> logs, LogType type) =>
            logs.Where(log => log.Type == type).Select(log => log.Message);
    }
}
