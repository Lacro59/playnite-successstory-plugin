using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CommonPluginsShared.Models
{
    public class TraceInfos
    {
        public string InitialCaller { get; set; }
        public ParameterInfo[] CallerParams { get; set; }

        public string FileName { get; set; }
        public int LineNumber { get; set; }

        public TraceInfos(Exception ex)
        {
            StackTrace Trace = new StackTrace(ex, true);
            StackFrame Frame = Trace.GetFrames()?.LastOrDefault();
            InitialCaller = Frame?.GetMethod()?.Name;
            CallerParams = Frame?.GetMethod()?.GetParameters();
            FileName = Frame.GetFileName().IsNullOrEmpty() ? "???" : Frame.GetFileName();
            LineNumber = Frame.GetFileLineNumber();
        }
    }
}
