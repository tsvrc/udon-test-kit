namespace UdonTestKit
{
    /// <summary>How long a ClientSimTestBase fixture keeps its TestScene loaded.</summary>
    public enum TestSceneLifetime
    {
        /// <summary>A fresh load for every test, unloaded after it.</summary>
        PerTest,

        /// <summary>One load shared by every test in the fixture, unloaded after the last one.</summary>
        PerFixture,
    }
}
