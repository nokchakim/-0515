using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPX_Weight.DataModel
{
    public class DBDataMstPos
    {
       // public int _Id { get; set; }
        public string Plant_Id { get; set; }
        public string Line_Id { get; set; }
        public string Pos { get; set; }
        public string Pos_Name { get; set; }
        public int End_Qty { get; set; }
        public string Beam { get; set; }
        public string Use_Yn { get; set; }
        public string Created_By { get; set; }
        public DateTime Created_On { get; set; }
        public string Modified_By { get; set; }
        public DateTime Modified_On { get; set; }
        
    }

    public static class DBDataMstPosT
    {
        public const string TbName = "MST_POS";
       // public const string _Id = "_Id";
        public const string Plant_Id = "PLANT_ID";
        public const string Line_Id = "LINE_ID";
        public const string Pos = "POS";
        public const string Pos_Name = "POS_NAME";
        public const string End_Qty = "END_QTY";
        public const string Beam = "BEAM";
        public const string Use_Yn = "USE_YN";
        public const string Created_By = "CREATED_BY";
        public const string Created_On = "CREATED_ON";
        public const string Modified_By = "MODIFIED_BY";
        public const string Modified_On = "MODIFIED_ON";
        
    }

}
