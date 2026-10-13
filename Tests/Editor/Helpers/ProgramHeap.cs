using System;
using System.Collections.Generic;
using UdonSharpEditor;
using VRC.Udon.Common.Interfaces;

namespace Tsvrc.UdonTestKit.Editor.Tests.Helpers
{
    internal static class ProgramHeap
    {
        // Every string on the heap of the program of an UdonSharp class, its string constants among
        // them, as UdonSharp last compiled it.
        public static List<string> Strings(Type udonSharpClass)
        {
            IUdonProgram program = UdonSharpEditorUtility.GetUdonSharpProgramAsset(udonSharpClass).SerializedProgramAsset.RetrieveProgram();
            var strings = new List<string>();
            for (uint address = 0; address < program.Heap.GetHeapCapacity(); address++)
            {
                if (program.Heap.GetHeapVariable(address) is string value)
                {
                    strings.Add(value);
                }
            }
            return strings;
        }
    }
}
