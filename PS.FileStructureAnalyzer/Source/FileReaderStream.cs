using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using PS.FileStructureAnalyzer.Source.PInvoke;

namespace PS.FileStructureAnalyzer.Source
{
    public class FileReaderStream : IDisposable
    {
        #region Static members

        [DllImport("kernel32.dll")]
        private static extern bool ReadFile(IntPtr handle,
                                            IntPtr buffer,
                                            uint numBytesToRead,
                                            out uint lpNumberOfBytesRead,
                                            IntPtr lpOverlapped);

        #endregion

        private FileStream _file;

        #region Constructors

        public FileReaderStream(string filename)
        {
            Open(filename);
        }

        public FileReaderStream()
        {
        }

        #endregion

        #region Properties

        public long Length
        {
            get
            {
                CheckOpen();
                return _file.Length;
            }
        }

        public long Offset { get; set; }

        public SeekOrigin SeekOrigin { get; set; }

        #endregion

        #region IDisposable Members

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Close();
        }

        #endregion

        #region Members

        public void Close()
        {
            if (_file != null)
            {
                _file.Dispose();
                _file = null;
            }
        }

        public void Open(string filename)
        {
            _file = File.OpenRead(filename);
        }

        public uint Read(IntPtr ptr, uint size, SeekDetails details = null)
        {
            CheckOpen();
            if (details != null && !_file.CanSeek) throw new InvalidOperationException("Stream cannot be seeked");

            var oldPosition = _file.Position;
            if (details != null) _file.Seek(details.Offset, details.SeekOrigin);

            if (_file.SafeFileHandle == null) throw new InvalidOperationException();

            uint readSize;
            if (!ReadFile(_file.SafeFileHandle.DangerousGetHandle(), ptr, size, out readSize, IntPtr.Zero))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            if (details != null) _file.Position = oldPosition;

            return readSize;
        }

        private void CheckOpen()
        {
            if (_file == null) throw new InvalidOperationException("Stream is not opened");
        }

        #endregion
    }
}