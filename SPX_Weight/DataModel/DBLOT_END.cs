using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPX_Weight.DataModel
{

    public class DBLOT_END
    {
        public int _Id { get; set; }
        public string PLANT_ID { get; set; }
        public string LINE_ID { get; set; }
        public string POS { get; set; }
        public string LOT { get; set; }
        public string END_ID { get; set; }
        public string SIDE { get; set; }
        public string LOT_SEQ { get; set; }


    }

    public static class DBLOT_ENDT
    {
        public const string _Id = "_Id";
        public const string PlantID = "PLANT_ID";        
        public const string LineID = "LINE_ID";
        public const string Pos = "POS";
        public const string LOT = "LOT";
        public const string EndId = "END_ID";
        public const string LOT_SEQ = "LOT_SEQ";
        public const string SIDE = "SIDE";
    }
}
