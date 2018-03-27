using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using PS.FileStructureAnalyzer.Source;
using PS.FileStructureAnalyzer.Source.PInvoke;

namespace PS.FileStructureAnalyzer.Extensions
{
    public static class FileInfoExtensions
    {
        #region Static members

        public static CompilationMode GetCompilationModeDetailed(this FileInfo info)
        {
            if (!info.Exists) throw new ArgumentException($"{info.FullName} does not exist");

            var intPtr = IntPtr.Zero;
            try
            {
                uint unmanagedBufferSize = 4096;
                intPtr = Marshal.AllocHGlobal((int)unmanagedBufferSize);

                using (var stream = new FileReaderStream(info.FullName))
                {
                    stream.Read(intPtr, unmanagedBufferSize);
                }

                // Check DOS header structure
                var dosStructure = Marshal.PtrToStructure<IMAGE_DOS_HEADER>(intPtr);
                if (dosStructure.e_magic != Constants.PEMagic) return CompilationMode.Invalid;

                // This will get the address for the WinNT header  
                var ntHeaderAddressOffset = dosStructure.e_lfanew;
                var ntHeaderAddress = intPtr + ntHeaderAddressOffset;
                // Determine WinNT header signature number address
                var signatureAddressOffset = Marshal.OffsetOf<IMAGE_NT_HEADERS32>(nameof(IMAGE_NT_HEADERS32.Signature)).ToInt32();
                // Check WinNT header signature
                var signature = Marshal.ReadInt32(ntHeaderAddress + signatureAddressOffset);
                if (signature != Constants.NTHeaderSignature) return CompilationMode.Invalid;

                //Determine file bitness by reading magic number from IMAGE_OPTIONAL_HEADER
                var optionalHeaderAddressOffset = Marshal.OffsetOf<IMAGE_NT_HEADERS32>(nameof(IMAGE_NT_HEADERS32.OptionalHeader)).ToInt32();
                var magicValueAddressOffset = Marshal.OffsetOf<IMAGE_OPTIONAL_HEADER32>(nameof(IMAGE_OPTIONAL_HEADER32.Magic)).ToInt32();
                var magic = (MagicType)Marshal.ReadInt32(ntHeaderAddress + optionalHeaderAddressOffset + magicValueAddressOffset);
                //Check magic value is one of known MagicType values
                if (Enum.GetValues(typeof(MagicType)).OfType<MagicType>().All(v => v != magic))
                {
                    //Invalid magic
                    return CompilationMode.Invalid;
                }

                var result = CompilationMode.Invalid;
                uint clrHeaderSize = 0;
                switch (magic)
                {
                    case MagicType.IMAGE_NT_OPTIONAL_HDR32_MAGIC:
                    {
                        result |= CompilationMode.Bit32;
                        clrHeaderSize = Marshal.PtrToStructure<IMAGE_NT_HEADERS32>(ntHeaderAddress)
                                               .OptionalHeader
                                               .CLRRuntimeHeader
                                               .Size;
                    }
                        break;
                    case MagicType.IMAGE_NT_OPTIONAL_HDR64_MAGIC:
                    {
                        result |= CompilationMode.Bit64;
                        clrHeaderSize = Marshal.PtrToStructure<IMAGE_NT_HEADERS64>(ntHeaderAddress)
                                               .OptionalHeader
                                               .CLRRuntimeHeader
                                               .Size;
                    }
                        break;
                }

                result |= clrHeaderSize != 0
                    ? CompilationMode.CLR
                    : CompilationMode.Native;

                return result;
            }
            finally
            {
                if (intPtr != IntPtr.Zero) Marshal.FreeHGlobal(intPtr);
            }
        }

        public static CompilationMode GetCompilationModeTruncated(this FileInfo info)
        {
            if (!info.Exists) throw new ArgumentException($"{info.FullName} does not exist");

            var intPtr = IntPtr.Zero;
            try
            {
                uint unmanagedBufferSize = 4096;
                intPtr = Marshal.AllocHGlobal((int)unmanagedBufferSize);

                using (var stream = new FileReaderStream(info.FullName))
                {
                    stream.Read(intPtr, unmanagedBufferSize);
                }

                //Check DOS header magic number
                if (Marshal.ReadInt16(intPtr) != Constants.PEMagic) return CompilationMode.Invalid;

                // This will get the address for the WinNT header  
                var ntHeaderAddressOffset = Marshal.ReadInt32(intPtr + 60);

                // Check WinNT header signature
                var signature = Marshal.ReadInt32(intPtr + ntHeaderAddressOffset);
                if (signature != Constants.NTHeaderSignature) return CompilationMode.Invalid;

                //Determine file bitness by reading magic from IMAGE_OPTIONAL_HEADER
                var magic = (MagicType)Marshal.ReadInt32(intPtr + ntHeaderAddressOffset + 24);

                //Check magic value is one of known MagicType values
                if (Enum.GetValues(typeof(MagicType)).OfType<MagicType>().All(v => v != magic))
                    return CompilationMode.Invalid;

                var result = CompilationMode.Invalid;
                uint clrHeaderSize = 0;
                switch (magic)
                {
                    case MagicType.IMAGE_NT_OPTIONAL_HDR32_MAGIC:
                    {
                        clrHeaderSize = (uint)Marshal.ReadInt32(intPtr + ntHeaderAddressOffset + 24 + 208 + 4);
                        result |= CompilationMode.Bit32;
                    }
                        break;
                    case MagicType.IMAGE_NT_OPTIONAL_HDR64_MAGIC:
                    {
                        clrHeaderSize = (uint)Marshal.ReadInt32(intPtr + ntHeaderAddressOffset + 24 + 224 + 4);
                        result |= CompilationMode.Bit64;
                    }
                        break;
                }
                result |= clrHeaderSize != 0
                    ? CompilationMode.CLR
                    : CompilationMode.Native;
                return result;
            }
            finally
            {
                if (intPtr != IntPtr.Zero) Marshal.FreeHGlobal(intPtr);
            }
        }

        public static CompilationMode GetCompilationModeTruncatedWithExplanation(this FileInfo info)
        {
            if (!info.Exists) throw new ArgumentException($"{info.FullName} does not exist");

            var intPtr = IntPtr.Zero;
            try
            {
                uint unmanagedBufferSize = 4096;
                intPtr = Marshal.AllocHGlobal((int)unmanagedBufferSize);

                using (var stream = new FileReaderStream(info.FullName))
                {
                    stream.Read(intPtr, unmanagedBufferSize);
                }

                //Check DOS header magic number
                if (Marshal.ReadInt16(intPtr) != Constants.PEMagic) return CompilationMode.Invalid;

                // This will get the address for the WinNT header  
                var lfanewAddressOffset = Marshal.OffsetOf<IMAGE_DOS_HEADER>(nameof(IMAGE_DOS_HEADER.e_lfanew)).ToInt32();
                var ntHeaderAddressOffset = Marshal.ReadInt32(intPtr + lfanewAddressOffset);
                var ntHeaderAddress = intPtr + ntHeaderAddressOffset;

                // Determine WinNT header signature number address
                var signatureAddressOffset = Marshal.OffsetOf<IMAGE_NT_HEADERS32>(nameof(IMAGE_NT_HEADERS32.Signature)).ToInt32();

                // Check WinNT header signature
                var signature = Marshal.ReadInt32(ntHeaderAddress + signatureAddressOffset);
                if (signature != Constants.NTHeaderSignature) return CompilationMode.Invalid;

                //Determine file bitness by reading magic from IMAGE_OPTIONAL_HEADER
                var optionalHeaderAddressOffset = Marshal.OffsetOf<IMAGE_NT_HEADERS32>(nameof(IMAGE_NT_HEADERS32.OptionalHeader)).ToInt32();
                var magicValueAddressOffset = Marshal.OffsetOf<IMAGE_OPTIONAL_HEADER32>(nameof(IMAGE_OPTIONAL_HEADER32.Magic)).ToInt32();
                var magic = (MagicType)Marshal.ReadInt32(ntHeaderAddress + optionalHeaderAddressOffset + magicValueAddressOffset);

                //Check magic value is one of known MagicType values
                if (Enum.GetValues(typeof(MagicType)).OfType<MagicType>().All(v => v != magic))
                {
                    //Invalid magic
                    return CompilationMode.Invalid;
                }

                var result = CompilationMode.Invalid;
                uint clrHeaderSize = 0;
                switch (magic)
                {
                    case MagicType.IMAGE_NT_OPTIONAL_HDR32_MAGIC:
                    {
                        var optionalHeaderOffset = Marshal.OffsetOf<IMAGE_NT_HEADERS32>(nameof(IMAGE_NT_HEADERS32.OptionalHeader)).ToInt32();
                        var clrRuntimeHeaderOffset =
                            Marshal.OffsetOf<IMAGE_OPTIONAL_HEADER32>(nameof(IMAGE_OPTIONAL_HEADER32.CLRRuntimeHeader)).ToInt32();
                        var clrRuntimeHeaderImageDataDirectorySizeOffset =
                            Marshal.OffsetOf<IMAGE_DATA_DIRECTORY>(nameof(IMAGE_DATA_DIRECTORY.Size)).ToInt32();
                        clrHeaderSize = (uint)Marshal.ReadInt32(ntHeaderAddress +
                                                                optionalHeaderOffset +
                                                                clrRuntimeHeaderOffset +
                                                                clrRuntimeHeaderImageDataDirectorySizeOffset);
                        result |= CompilationMode.Bit32;
                    }
                        break;
                    case MagicType.IMAGE_NT_OPTIONAL_HDR64_MAGIC:
                    {
                        var optionalHeaderOffset = Marshal.OffsetOf<IMAGE_NT_HEADERS64>(nameof(IMAGE_NT_HEADERS64.OptionalHeader)).ToInt32();
                        var clrRuntimeHeaderOffset =
                            Marshal.OffsetOf<IMAGE_OPTIONAL_HEADER64>(nameof(IMAGE_OPTIONAL_HEADER64.CLRRuntimeHeader)).ToInt32();
                        var clrRuntimeHeaderImageDataDirectorySizeOffset =
                            Marshal.OffsetOf<IMAGE_DATA_DIRECTORY>(nameof(IMAGE_DATA_DIRECTORY.Size)).ToInt32();
                        clrHeaderSize = (uint)Marshal.ReadInt32(ntHeaderAddress +
                                                                optionalHeaderOffset +
                                                                clrRuntimeHeaderOffset +
                                                                clrRuntimeHeaderImageDataDirectorySizeOffset);
                        result |= CompilationMode.Bit64;
                    }
                        break;
                }
                result |= clrHeaderSize != 0
                    ? CompilationMode.CLR
                    : CompilationMode.Native;
                return result;
            }
            finally
            {
                if (intPtr != IntPtr.Zero) Marshal.FreeHGlobal(intPtr);
            }
        }

        public static CompilationMode GetCompilationModeWithoutAnyDependencies(this FileInfo info)
        {
            if (!info.Exists) throw new ArgumentException($"{info.FullName} does not exist");

            var intPtr = IntPtr.Zero;
            try
            {
                uint unmanagedBufferSize = 4096;
                intPtr = Marshal.AllocHGlobal((int)unmanagedBufferSize);

                using (var stream = File.Open(info.FullName, FileMode.Open, FileAccess.Read))
                {
                    var bytes = new byte[unmanagedBufferSize];
                    stream.Read(bytes, 0, bytes.Length);
                    Marshal.Copy(bytes, 0, intPtr, bytes.Length);
                }

                //Check DOS header magic number
                if (Marshal.ReadInt16(intPtr) != 0x5a4d) return CompilationMode.Invalid;

                // This will get the address for the WinNT header  
                var ntHeaderAddressOffset = Marshal.ReadInt32(intPtr + 60);

                // Check WinNT header signature
                var signature = Marshal.ReadInt32(intPtr + ntHeaderAddressOffset);
                if (signature != 0x4550) return CompilationMode.Invalid;

                //Determine file bitness by reading magic from IMAGE_OPTIONAL_HEADER
                var magic = Marshal.ReadInt32(intPtr + ntHeaderAddressOffset + 24);

                var result = CompilationMode.Invalid;
                uint clrHeaderSize;
                if (magic == 0x10b)
                {
                    clrHeaderSize = (uint)Marshal.ReadInt32(intPtr + ntHeaderAddressOffset + 24 + 208 + 4);
                    result |= CompilationMode.Bit32;
                }
                else if (magic == 0x20b)
                {
                    clrHeaderSize = (uint)Marshal.ReadInt32(intPtr + ntHeaderAddressOffset + 24 + 224 + 4);
                    result |= CompilationMode.Bit64;
                }
                else return CompilationMode.Invalid;

                result |= clrHeaderSize != 0
                    ? CompilationMode.CLR
                    : CompilationMode.Native;

                return result;
            }
            finally
            {
                if (intPtr != IntPtr.Zero) Marshal.FreeHGlobal(intPtr);
            }
        }

        #endregion
    }
}