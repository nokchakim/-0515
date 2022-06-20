using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPX_Weight.DataModel
{

    public class DBDataSelectPOS
    {
        public string POS { get; set; }
    }

    public class DBDataSelectLOT
    {
        public string LOT { get; set; }
    }

    public class DBDataSelectLINE
    {
        public string LINE { get; set; }
    }

    public static class DBDataSelectPOST
    {
        public const string TbName = "Mst_Doff";

        public const string _Id = "_Id";
        public const string Doff_Id = "Doff_Id";
        public const string Desc = "Desc";
        public const string Status = "Status";
    }
}
