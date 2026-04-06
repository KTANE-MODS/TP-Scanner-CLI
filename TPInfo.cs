using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TP_Scanner
{
    internal class TPInfo
    {
        public bool HasTPSupport { get; set; }
        public bool HasAutoSolver { get; set; }

        public TPInfo(bool HasTPSupport, bool HasAutoSolver)
        {
            this.HasTPSupport = HasTPSupport;
            this.HasAutoSolver = HasAutoSolver;
        }

    }
}
