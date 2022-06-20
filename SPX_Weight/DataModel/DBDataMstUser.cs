using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPX_Weight.DataModel
{
    class DBDataMstUser
    {
        public string Plant_Id { get; set; }
        public string User_Id { get; set; }
        public string Use_YN { get; set; }
    }

    public static class DBDataMstUserT
    {
        public const string TbName = "txmUserMast";        
        public const string Plant_Id = "PLANT_ID";
        public const string User_Id = "USER_ID";
        public const string Use_YN = "USE_YN";
    }
}

