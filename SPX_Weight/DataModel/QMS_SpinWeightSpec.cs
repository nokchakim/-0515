using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPX_Weight.DataModel
{
    public class QMS_SpinWeightSpec
    {
        public string Plant_Id { get; set; }
        public string Lot { get; set; }
        public string Lot_seq { get; set; }
        public string Apply_Date { get; set; }
        public decimal Usl { get; set; }
        public decimal Sl { get; set; }
        public decimal Lsl { get; set; }
        public decimal Ucl { get; set; }
        public decimal Cl { get; set; }
        public decimal Lcl { get; set; }
        public string Mark { get; set; }
        public string Sl_tolerance { get; set; }
        public string Cl_tolerance { get; set; }
        public string Created_by { get; set; }
        public DateTime Created_On { get; set; }
        public string Modified_by { get; set; }
        public DateTime Modified_On { get; set; }
    }
    public static class QMS_SpinWeightSpecT
    {
        public const string TbName = "SPIN_WEIGHT_SPEC_V";
        //public const string TbName = "SPIN_WEIGHT_SPEC";

        public static string Plant_Id = "PLANT_ID";
        public static string Lot = "LOT";
        public static string Lot_seq = "LOT_SEQ";
        public static string apply_date = "APPLY_DATE";
        public static string Usl = "USL";
        public static string Sl = "SL";
        public static string Lsl = "LSL";
        public static string Ucl = "UCL";
        public static string Cl = "CL";
        public static string Lcl = "LCL";        
        public static string Mark = "MARK";
        public static string Sl_tolerance = "SL_TOLERANCE";
        public static string Cl_tolerance = "CL_TOLERANCE";
        public static string Created_by = "CREATED_BY";
        public static string Created_On = "CREATED_ON";
        public static string Modified_On = "MODIFIED_ON";
        public static string Modified_by = "MODIFIED_BY";
    }

}
