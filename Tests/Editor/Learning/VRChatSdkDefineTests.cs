using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using Tsvrc.UdonTestKit.Editor.Tests.Helpers;
using UnityEditor;
using UnityEditor.Build;

namespace Tsvrc.UdonTestKit.Editor.Tests.Learning
{
    // The VRChat SDK adds VRC_ENABLE_PLAYER_PERSISTENCE to the scripting defines of the build target
    // once it finds ClientSim's saved data. It looks on a domain load, after the compile, so the
    // first compile of a project with the kit runs without the define, and ClientSim builds its
    // saved data only with it. So ClientSimSession waits for the local player's saved data only
    // with the define, and the kit compiles without it.
    public class VRChatSdkDefineTests
    {
        private const string PersistenceDefine = "VRC_ENABLE_PLAYER_PERSISTENCE";

        [Test]
        public void GetScriptingDefineSymbols_ProjectWithClientSimSavedData_HoldsThePersistenceDefine()
        {
            // Arrange
            NamedBuildTarget target = NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget));

            // Act
            string[] defines = PlayerSettings.GetScriptingDefineSymbols(target).Split(';');

            // Assert
            Assert.That(defines, Has.Member(PersistenceDefine));
        }

        [Test]
        public void Compile_ClientSimWithoutThePersistenceDefine_HasNoPlayerRestoredEvent()
        {
            // Act
            CSharpCompilation clientSim = CompileWithoutDefine.Of("VRC.ClientSim", PersistenceDefine);

            // Assert
            Assert.That(clientSim.GetTypeByMetadataName("VRC.SDK3.ClientSim.ClientSimOnPlayerJoinedEvent"), Is.Not.Null);
            Assert.That(clientSim.GetTypeByMetadataName("VRC.SDK3.ClientSim.ClientSimOnPlayerRestoredEvent"), Is.Null);
        }
    }
}
