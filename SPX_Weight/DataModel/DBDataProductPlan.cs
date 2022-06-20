using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPX_Weight.DataModel
{
    public class DBDataProductPlan
    {
        //public int _Id { get; set; }
        public string Product_Date { get; set; }
        public string Plant_Id { get; set; }
        public string Line_Id { get; set; }
        public string Pos { get; set; }
        public string Lot { get; set; }
        public string Lot_Seq { get; set; }
        public string Start_End { get; set; }
        public string Side { get; set; }
        public string End_End { get; set; }
        public int End_Qty { get; set; }
        public string Inspect_End { get; set; }
        public string Cancel_Yn { get; set; }    
        public string End_Date { get; set; }        
        public DateTime Created_On { get; set; }        
        public DateTime Modified_On { get; set; }
    }

    public static class DBDataProductPlanT
    {
        public const string TbName = "PRODUCT_PLAN_W_V";
        public const string TbNameLocal = "PRODUCT_PLAN";

        public const string _Id = "_Id";
        public const string Product_Date = "Product_Date";
        public const string Plant_Id = "Plant_Id";
        public const string Line_Id = "Line_Id";
        public const string Pos = "Pos";
        public const string Lot = "Lot";
        public const string Lot_Seq = "Lot_Seq";
        public const string Start_end = "START_END";        
        public const string Side = "Side";
        public const string End_End = "End_End";
        public const string End_Qty = "END_QTY";
        public const string Inspect_End = "INSPECT_END";
        public const string Cancel_Yn = "CANCEL_YN";
        public const string End_Date = "END_DATE";        
        public const string Created_On = "Created_On";        
        public const string Modified_On = "Modified_On";
    }

    public class DBDataPlanTID
    {
        public string Plant_Id { get; set; }
    }
}
