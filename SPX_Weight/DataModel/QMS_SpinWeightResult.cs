using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPX_Weight.DataModel
{
    public class QMS_SpinWeightResult
    {
        public string Product_Date { get; set; }
        public string Plant_Id { get; set; }
        public string Line_Id { get; set; }
        public string Pos { get; set; }    
        public string Lot { get; set; }
        public string Lot_seq { get; set; }
        public string End_Id { get; set; }
        public int Doff { get; set; }
        public string Side { get; set; }
        public decimal Usl { get; set; }
        public decimal Sl { get; set; }
        public decimal Lsl { get; set; }
        public decimal Ucl { get; set; }
        public decimal Cl { get; set; }
        public decimal Lcl { get; set; }
        public decimal Value { get; set; }
        public string Mark { get; set; }
        public string Decision_id { get; set; }
        public string Spec_color { get; set; }
        public string Created_by { get; set; }
        public DateTime Created_On { get; set; }
        public string Modified_by { get; set; }
        public DateTime Modified_On { get; set; }
        public string qms_send { get; set; }
    }

    public static class QMS_SpinWeightResultT
    {
        public static string Product_data = "PRODUCT_DATE";
        public static string Plant_Id = "PLANT_ID";
        public static string Line_Id = "LINE_ID";
        public static string Pos = "POS";
        public static string End_Id = "END_ID";
        public static string Lot = "LOT";
        public static string Lot_seq = "LOT_SEQ";
        public static string Doff = "DOFF";
        public static string Side = "SIDE";
        public static string Usl = "USL";
        public static string Sl = "SL";
        public static string Lsl = "LSL";
        public static string Ucl = "UCL";
        public static string Cl = "CL";
        public static string Lcl = "LCL";
        public static string Value = "VALUE";
        public static string Mark = "MARK";
        public static string Decision_id = "DECISION_ID";
        public static string Spec_color = "SPEC_COLOR";
        public static string Created_by = "CREATED_BY";
        public static string Created_On = "CREATED_ON";
        public static string Modified_On = "MODIFIED_ON";
        public static string Modified_by = "MODIFIED_BY";
        public static string Send_QMS = "SEND_QMS";
    }
}
