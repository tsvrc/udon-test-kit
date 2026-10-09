using NUnit.Framework;

namespace UdonTestKit
{
    /// <summary>
    /// Base for Play Mode tests that use ClientSim. Start <see cref="Session"/> in a test; it ends
    /// after every test, after the derived class's own teardown.
    /// </summary>
    public abstract class ClientSimTestBase
    {
        protected readonly ClientSimSession Session = new ClientSimSession();

        [TearDown]
        public void EndClientSimSession()
        {
            Session.End();
        }
    }
}
