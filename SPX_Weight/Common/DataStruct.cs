using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Text;


namespace SPX_Weight.Common
{
    class DataStruct
    {
    }

    public enum RESULT
    {
        NOT_TESTED = 0,
        PASS = 1,
        FAIL = 2
    }

    public enum ServerStatus
    {
        NONE = 0,
        INSPECTING = 1,
        QMSDATA_IMPORT = 2,
        QMSDATA_EXPORT = 3
    }

    public enum ERRORCODE
    {
        CLIENT_ERROR = 0,
        IMAGE_SAVE_ERROR = 1,
        IMAGE_PATH_ERROR = 2,
        EMPTY_RESERVED = 3,
        IMPORT_TYPE_ERROR = 4,
        EXPORT_TYPE_ERROR = 5,
    }
}
