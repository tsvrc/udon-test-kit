using UdonSharp;

namespace Tsvrc.UdonTestKit.Tests
{
    // Compiles for the Editor, and fails the compile for upload, where it calls a method Udon doesn't
    // expose, with an error the compile logs as it ends. It has no program asset, so it fails nothing
    // until a test gives it one.
    public class UploadFailingBehaviour : UdonSharpBehaviour
    {
        public const string UploadError = "Method is not exposed to Udon";

        public void _Run()
        {
#if !UNITY_EDITOR
            System.IO.File.Exists("");
#endif
        }
    }
}
