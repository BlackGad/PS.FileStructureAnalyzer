using System;
using System.IO;
using System.Linq;
using PS.FileStructureAnalyzer.Extensions;
using PS.FileStructureAnalyzer.Source;

namespace PS.FileStructureAnalyzer
{
    class Program
    {
        #region Static members

        static void Main(string[] args)
        {
            try
            {
                var filePath = args.FirstOrDefault();
                //filePath = filePath ?? @"d:\Projects\License\LicenseEditor\LicenseEditorData\bin\x64\Debug\LicenseEditorData.dll";
                filePath = filePath ?? @"d:\Projects\License\LicenseEditor\LicenseEditorData\bin\x86\Debug\LicenseEditorData.dll";
                //filePath = filePath ?? @"d:\Projects\License\LicenseEditor\LicenseReader\Bin\Win32\Release\LicenseReader.dll";
                //filePath = filePath ?? @"d:\Projects\License\LicenseEditor\LicenseReader\Bin\x64\Release\LicenseReader.dll";
                if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("Invalid source file path");
                var file = new FileInfo(filePath);
                Console.WriteLine("Detailed method");
                PrintCompilationMode(file.GetCompilationModeDetailed());
                Console.WriteLine("-----------");
                Console.WriteLine("Truncated with explanation method");
                PrintCompilationMode(file.GetCompilationModeTruncatedWithExplanation());
                Console.WriteLine("-----------");
                Console.WriteLine("Truncated method");
                PrintCompilationMode(file.GetCompilationModeTruncated());
            }
            catch (Exception e)
            {
                Console.WriteLine($"ERROR: {e.GetBaseException().Message}");
            }
            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();
        }

        private static void PrintCompilationMode(CompilationMode mode)
        {
            if (mode == CompilationMode.Invalid) Console.WriteLine("INFO: Unknown compilation mode");
            else
            {
                if (mode.HasFlag(CompilationMode.CLR)) Console.WriteLine("Image: CLR");
                if (mode.HasFlag(CompilationMode.Native)) Console.WriteLine("Image: Native");
                if (mode.HasFlag(CompilationMode.Bit32)) Console.WriteLine("Bitness: 32-bit");
                if (mode.HasFlag(CompilationMode.Bit64)) Console.WriteLine("Bitness: 64-bit");
            }
        }

        #endregion
    }
}