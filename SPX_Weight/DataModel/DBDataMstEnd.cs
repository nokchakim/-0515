using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPX_Weight.DataModel
{
    public class DBDataMstEnd
    {
        //public int _Id { get; set; }
        public string Plant_Id { get; set; }
        public string End_Id { get; set; }
        public string End_Name { get; set; }
        public decimal Prt_Order { get; set; }
        public string Created_by { get; set; }
        public string Modified_by { get; set; }
        public DateTime Created_On { get; set; }
        public DateTime Modified_On { get; set; }

    }

    public static class DBDataMstEndT
    {
        public const string TbName = "MST_END";

        //public const string _Id = "_Id";
        public const string Plant_Id = "PLANT_ID";
        public const string End_Id = "END_ID";
        public const string End_Name = "END_NAME";
        public const string Prt_Order = "PRT_ORDER";        
        public const string Created_by = "CREATED_BY";
        public const string Created_On = "CREATED_ON";
        public const string Modified_On = "MODIFIED_BY";
        public const string Modified_by = "MODIFIED_ON";

    }
}

