using System;
using System.Collections.Generic;
using System.Linq;

namespace UdonTestKit
{
    /// <summary>
    /// The ordered list of fixups ClientSimTestBase applies. A consuming world can switch one off
    /// with Disable&lt;TFixup&gt;() without forking the kit, e.g. once a VRChat SDK update makes
    /// it unnecessary.
    /// </summary>
    public static class FixupRegistry
    {
        private static readonly List<IPlayModeEnvironmentFixup> AllFixups = new List<IPlayModeEnvironmentFixup>
        {
            new ClientSimProjectSettingsPreflight(),
            new ClientSimPersistenceLeakFixup(),
        };

        private static readonly HashSet<Type> DisabledTypes = new HashSet<Type>();

        public static IEnumerable<IPlayModeEnvironmentFixup> ActiveFixups =>
            AllFixups.Where(f => !DisabledTypes.Contains(f.GetType()));

        public static void Disable<TFixup>() where TFixup : IPlayModeEnvironmentFixup =>
            DisabledTypes.Add(typeof(TFixup));

        public static void EnableAll() => DisabledTypes.Clear();

        // Lets the kit's own tests add a recording fixup to prove how ClientSimTestBase applies them.
        internal static void Register(IPlayModeEnvironmentFixup fixup) => AllFixups.Add(fixup);

        internal static void Unregister(IPlayModeEnvironmentFixup fixup) => AllFixups.Remove(fixup);
    }
}
