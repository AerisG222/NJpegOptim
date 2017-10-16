using System.IO;


namespace NJpegOptim
{
    public class Result
    {
        public Stream OutputStream { get; set; }
        public bool Success { get; set; }
        public string ErrorLine { get; set; }
        public string SourceFile { get; set; }
        public string Resolution { get; set; }
        public string ColorDepth { get; set; }
        public string NormalOrProgressive { get; set; }
        public int SourceSize { get; set; }
        public int OptimizedSize { get; set; }
        public float PercentImprovement { get; set; }
        public bool WasOptimized { get; set; }
    }
}
