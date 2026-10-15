using Tsvrc.UdonTestKit;

// A spy with a callback named after what your code calls. Test doubles that tests add to
// GameObjects go in this assembly: Unity can't add one from the Edit Mode test assembly.
public class ExampleSpy : CallbackSpy
{
    public void OnExample() => Record();
}
