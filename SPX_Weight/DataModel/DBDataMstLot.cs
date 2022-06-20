using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPX_Weight.DataModel
{
    public class DBDataMstLot
    {
        //public int _Id { get; set; }
        public string Plant_Id { get; set; }
        public string Lot { get; set; }
        public string Lot_Seq { get; set; }
        public int Denier { get; set; }
        public int Fila { get; set; }
        public string Erp_Item { get; set; }
        public string Erp_Item_Name { get; set; }
        public string Erp_Lot { get; set; }
        public string Yarn_Type { get; set; }
        public string Yarn_Bright { get; set; }
        public string Old_Erp_Item { get; set; }
        public int Weight { get; set; }
        public int Speed { get; set; }
        public string Array { get; set; }
        public int End_Qty { get; set; }
        public string Oil { get; set; }
        public string Opu { get; set; }
        public string Usage_Id { get; set; }
        public string Il_Type_Id { get; set; }
        public string Rm_Same_Spec { get; set; }
        public string Created_By { get; set; }
        public DateTime Created_On { get; set; }
        public string Modified_By { get; set; }
        public DateTime Modified_On { get; set; }
        public string Lot_Descr { get; set; }

    }

    public static class DBDataMstLotT
    {
        public const string TbName = "MST_LOT";

        //public const string _Id = "_Id";
        public const string Plant_Id = "PLANT_ID";
        public const string Lot = "LOT";
        public const string Lot_Seq = "LOT_SEQ";
        public const string Denier = "DENIER";
        public const string Fila = "FILA";
        public const string Erp_Item = "ERP_ITEM";
        public const string Erp_Item_Name = "ERP_ITEM_NAME";
        public const string Erp_Lot = "ERP_LOT";
        public const string Yarn_Type = "YARN_TYPE";
        public const string Yarn_Bright = "YARN_BRIGHT";
        public const string Old_Erp_Item = "OLD_ERP_ITEM";
        public const string Weight = "WEIGHT";
        public const string Speed = "SPEED";
        public const string Array = "ARRAY";
        public const string End_Qty = "END_QTY";
        public const string Oil = "OIL";
        public const string Opu = "OPU";
        public const string Usage_id = "USAGE_ID";
        public const string Il_Type_Id = "IL_TYPE_ID";
        public const string Rm_Same_Spec = "RM_SAME_SPEC";
        public const string Created_By = "CREATED_BY";
        public const string Created_On = "CREATED_ON";
        public const string Modified_By = "MODIFIED_BY";
        public const string Modified_On = "MODIFIED_ON";
        public const string Lot_Descr = "LOT_DESCR";

    }
}
