using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPX_Weight.DataModel
{
    public class QMS_SpinWeightTemp
    {
        public string Product_Date { get; set; }
        public string Plant_Id { get; set; }
        public string Line_Id { get; set; }
        public string Pos { get; set; }
        public string End_Id { get; set; }
        public string Lot { get; set; }
        public string Lot_seq { get; set; }
        public string Doff { get; set; }
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

    }

    public static class QMS_SpinWeightTempT
    {
        public const string Product_Date = "PRODUCT_DATE";
        public static string Plant_Id = "PLANT_ID";
        public static string Line_Id = "";
        public static string Pos = "";
        public static string End_Id = "";
        public static string Lot = "LOT";
        public static string Lot_seq = "LOT_SEQ";
        public static string Doff = "";
        public static string Side = "";
        public static string Usl = "";
        public static string Sl = "";
        public static string Lsl = "";
        public static string Ucl = "";
        public static string Cl = "";
        public static string Lcl = "";
        public static string Value = "";
        public static string Mark = "";
        public static string Decision_id = "";
        public static string Spec_color = "";
        public static string Created_by = "";
        public static string Created_On = "";
        public static string Modified_On = "";
        public static string Modified_by = "";
    }

    public class DbDataTemp
    {
        public int _Id { get; set; }
        public string Product_Date { get; set; }
        public string Platn_Id { get; set; }
        public string Lot { get; set; }
        public string Lot_Seq { get; set; }
        public string Line_Id { get; set; }
        public string Pos { get; set; }
        public string End_Id { get; set; }
        public string Doff { get; set; }
        public decimal Value { get; set; }
        public DateTime Inspect_Date { get; set; }
        public decimal Usl { get; set; }
        public decimal Sl { get; set; }
        public decimal Lsl { get; set; }
        public decimal Ucl { get; set; }
        public decimal Cl { get; set; }
        public decimal Lcl { get; set; }
        public decimal B_Usl { get; set; }
        public decimal B_Lsl { get; set; }
        public decimal B_Ucl { get; set; }
        public decimal B_Lcl { get; set; }
        public string Mark { get; set; }
        public decimal Tolerance { get; set; }
        public string Remarks { get; set; }
        public int Status { get; set; }
    }
}

