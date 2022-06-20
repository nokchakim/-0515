using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPX_Weight.DataModel
{

    public class DBDataMstDoff
    {
        public int _Id { get; set; }
        public string Doff_Id { get; set; }
        public string Desc { get; set; }
        public int Status { get; set; }
    }

    public static class DBDataMstDoffT
    {
        public const string TbName = "Mst_Doff";

        public const string _Id = "_Id";
        public const string Doff_Id = "Doff_Id";
        public const string Desc = "Desc";
        public const string Status = "Status";
    }
}
