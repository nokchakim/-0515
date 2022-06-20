using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPX_Weight.DataModel
{
    public class DBDataMstLine
    {
       // public int _Id { get; set; }
        public string Plant_Id { get; set; }
        public string Line_Id { get; set; }
        public string Line_Name { get; set; }
        public int Pos_QTY { get; set; }
        public string Use_Yn { get; set; }
        public int Side_Qty { get; set; }
        public int Side_End_Qty { get; set; }
        public string Project_ID { get; set; }
        public string Created_By { get; set; }        
        public DateTime Created_On { get; set; }
        public string Modified_By { get; set; }
        public DateTime Modified_On { get; set; }

    }

    public static class DBDataMstLineT
    {
        public const string TbName = "MST_LINE";
       // public const string _Id = "_Id";
        public const string Plant_Id = "PLANT_ID";
        public const string Line_Id = "Line_Id";
        public const string Line_Name = "Line_Name";
        public const string Pos_QTY = "Pos_QTY";
        public const string Side_Qty = "SIDE_QTY";
        public const string Side_End_Qty = "SIDE_END_QTY";
        public const string Project_ID = "PROJECT_ID";
        public const string Created_by = "CREATED_BY";
        public const string Created_On = "Created_On";
        public const string Modified_By = "MODIFIED_BY";
        public const string Modified_On = "Modified_On";
    }
}
