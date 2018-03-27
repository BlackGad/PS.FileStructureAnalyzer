using System.IO;

namespace PS.FileStructureAnalyzer.Source.PInvoke
{
    public class SeekDetails
    {
        #region Constructors

        public SeekDetails()
        {
        }

        public SeekDetails(long offset, SeekOrigin seekOrigin = SeekOrigin.Begin)
        {
            Offset = offset;
            SeekOrigin = seekOrigin;
        }

        #endregion

        #region Properties

        public long Offset { get; set; }

        public SeekOrigin SeekOrigin { get; set; }

        #endregion
    }
}