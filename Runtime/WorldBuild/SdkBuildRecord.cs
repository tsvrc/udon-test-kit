using System;
using UnityEditor;
using VRC.SDKBase.Editor;

namespace Tsvrc.UdonTestKit
{
    // What the world exporter of the VRChat SDK keeps about the builds of the user, saved when this is
    // created and put back when it's disposed, so a world built for a test leaves them as they were.
    //
    // The exporter records the world file it builds in EditorPrefs, which every Unity project of the
    // user shares, with the signature of the world: as the current build, and as the last build,
    // which Test Last Build and Reload Last Build in the SDK control panel use. It deletes the world
    // file of the last build since the scripts last reloaded, and the one recorded before the last
    // build, which it keeps in SessionState. So creating this also empties both, and the exporter
    // deletes neither.
    internal sealed class SdkBuildRecord : IDisposable
    {
        internal const string WorldFileKey = "currentBuildingAssetBundlePath";
        internal const string LastBuildKey = "lastVRCPath";
        internal const string SignatureKey = "currentBuildingAssetBundleWorldSignature";
        internal const string BuildBeforeKey = "previousBuildingAssetBundlePath";

        private static readonly string[] EditorPrefsKeys = { WorldFileKey, LastBuildKey, SignatureKey };

        private readonly string[] _editorPrefs = Array.ConvertAll(EditorPrefsKeys, EditorPrefs.GetString);
        private readonly string _buildBefore = SessionState.GetString(BuildBeforeKey, "");
        private readonly string _lastBuild = VRC_SdkBuilder.VRCPath;

        internal SdkBuildRecord()
        {
            SessionState.SetString(BuildBeforeKey, "");
            VRC_SdkBuilder.VRCPath = null;
        }

        public void Dispose()
        {
            for (int i = 0; i < EditorPrefsKeys.Length; i++)
            {
                EditorPrefs.SetString(EditorPrefsKeys[i], _editorPrefs[i]);
            }
            SessionState.SetString(BuildBeforeKey, _buildBefore);
            VRC_SdkBuilder.VRCPath = _lastBuild;
        }
    }
}
