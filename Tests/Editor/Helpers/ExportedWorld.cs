using System;
using System.IO;
using UnityEditor;

namespace Tsvrc.UdonTestKit.Editor.Tests.Helpers
{
    // For a test that runs the world exporter of the VRChat SDK itself. Creating it saves the record
    // of the builds of the user, as BuiltWorld does; disposing it deletes the world file the test
    // built and puts that record back.
    internal sealed class ExportedWorld : IDisposable
    {
        private readonly SdkBuildRecord _userRecord = new SdkBuildRecord();
        private readonly string _fileBefore = EditorPrefs.GetString(SdkBuildRecord.WorldFileKey);

        public void Dispose()
        {
            string file = EditorPrefs.GetString(SdkBuildRecord.WorldFileKey);
            if (file != _fileBefore && File.Exists(file))
            {
                File.Delete(file);
            }
            _userRecord.Dispose();
        }
    }
}
