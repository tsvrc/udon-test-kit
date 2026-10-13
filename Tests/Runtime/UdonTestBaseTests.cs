using NUnit.Framework;
using UnityEngine;

namespace Tsvrc.UdonTestKit.Tests
{
    public class UdonTestBaseTests : UdonTestBase
    {
        [Test]
        public void DisposeUdonHost_AfterCreate_DestroysWhatTheTestCreated()
        {
            // Arrange
            GameObject probe = Udon.Create<UdonProbe>("Probe").gameObject;

            // Act
            DisposeUdonHost();

            // Assert
            Assert.That(probe == null, Is.True);
        }
    }
}
