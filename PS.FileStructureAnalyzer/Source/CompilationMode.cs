using System;

namespace PS.FileStructureAnalyzer.Source
{
    [Flags]
    public enum CompilationMode
    {
        Invalid = 0,
        Native = 0x1,
        CLR = Native << 1,
        Bit32 = CLR << 1,
        Bit64 = Bit32 << 1
    }
}