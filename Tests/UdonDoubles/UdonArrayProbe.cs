using System;
using UdonSharp;
using VRC.SDK3.Data;

namespace Tsvrc.UdonTestKit.Tests.Learning
{
    // Runs System.Array members as Udon. A test sets its fields, calls a method by name and reads the
    // result from its public fields.
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class UdonArrayProbe : UdonSharpBehaviour
    {
        public UdonSharpBehaviour Target;

        public string[] Names;
        public string SearchName;
        public int SearchStart;
        public int Found;

        public int GrownLength;
        public bool GrownKeptTarget;
        public string[] GrownNames;
        public DataToken[] GrownTokens;

        public void _FindName()
        {
            Found = Array.IndexOf(Names, SearchName, SearchStart, Names.Length - SearchStart);
        }

        // The same search as _FindName, as a loop written in UdonSharp.
        public void _FindNameByLoop()
        {
            Found = -1;
            for (int i = SearchStart; i < Names.Length; i++)
            {
                if (Names[i] == SearchName)
                {
                    Found = i;
                    return;
                }
            }
        }

        // Moves every name after the first down one place and clears the last.
        public void _RemoveFirstName()
        {
            Array.Copy(Names, 1, Names, 0, Names.Length - 1);
            Names[Names.Length - 1] = null;
        }

        // Each _Grow method makes an array of two items, of one element type, twice as long.
        public void _GrowWithTarget()
        {
            UdonSharpBehaviour[] behaviours = new UdonSharpBehaviour[] { this, Target };
            Array grown = Array.CreateInstance(behaviours.GetType().GetElementType(), behaviours.Length * 2);
            Array.Copy(behaviours, grown, behaviours.Length);
            UdonSharpBehaviour[] result = (UdonSharpBehaviour[])grown;
            GrownLength = result.Length;
            GrownKeptTarget = result[1] == Target;
        }

        public void _GrowNames()
        {
            string[] names = new string[] { "A", "B" };
            Array grown = Array.CreateInstance(names.GetType().GetElementType(), names.Length * 2);
            Array.Copy(names, grown, names.Length);
            GrownNames = (string[])grown;
        }

        public void _GrowTokens()
        {
            DataToken[] tokens = new DataToken[] { 1, 2 };
            Array grown = Array.CreateInstance(tokens.GetType().GetElementType(), tokens.Length * 2);
            Array.Copy(tokens, grown, tokens.Length);
            GrownTokens = (DataToken[])grown;
        }
    }
}
