using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MVS.Models
{
    public class ImportResult
    {
        public ImportResultCode code;
        public string message;

        public ImportResult()
        {
            code = ImportResultCode.OK;
            message = string.Empty;
        }
    }
}
