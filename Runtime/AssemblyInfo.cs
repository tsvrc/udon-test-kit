using System.Runtime.CompilerServices;

// Lets the kit's own tests reach the seams they prove the workarounds through:
// ClientSimProjectSettingsPreflight's replaceable settings check, and
// UnityEventFilterAllowlistFixup's type list and opt-in check.
[assembly: InternalsVisibleTo("UdonTestKit.Tests.EditMode")]
[assembly: InternalsVisibleTo("UdonTestKit.Tests.PlayMode")]
