using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.IO;
using System.Diagnostics;
using System.Windows;
using SPX_Weight.DataModel;
using System.Data;
using System.Reflection;
using System.Data.SqlClient;
using System.Data.Common;





namespace SPX_Weight
{

    class LocalDataManager
    {
        public const int DB_TYPE_MYSQL = 0;
        public const int DB_TYPE_MSSQL = 1;

        private static LocalDataManager mInstance = null;

        public static string db_ID = "";
        public static string db_PWD = "";
        public static string db_DB = "";
        public static string db_IP = "";
        public static string db_PORT = "";
        public static string db_qms_ID = "";
        public static string db_qms_PWD = "";
        public static string db_qms_DB = "";
        public static string db_qms_IP = "";
        public static string db_qms_PORT = "";

        private static string EXCEPT_IDENTITY_COLUMN = "_id";

        private static string QUERY_LAST_INSERTED_ID = "SELECT LAST_INSERT_ID()";
        private static string QUERY_RESET_AUTO_INCREMENT = "ALTER TABLE `{0}` AUTO_INCREMENT = 1";
        string Excelpath = Path.Combine(System.Windows.Forms.Application.StartupPath, "Excelbase\\");

        SqlConnection conn = new SqlConnection();

        private string mMyConString;


        private static Object mInstanceLock = new Object();
        private static Object mUsingLock = new Object();
        private static Object mTransactionLock = new Object();

        private SqlConnection mLocalDBConnection = null;
        private SqlTransaction mLocalDBTransaction = null;
        //로컬 디비의 스펙을 여기다 선언해놓고 window에서 쓰기


        private LocalDataManager()
        {

        }
        public static LocalDataManager getInstance()
        {
            if (mInstance == null)
            {
                lock (mInstanceLock)
                {
                    if (mInstance == null)
                        mInstance = new LocalDataManager();
                }
            }
            return mInstance;
        }

        public bool OpenLocalDB()
        {
            bool rt = true;            
            try
            {
                string path = Path.Combine(System.Windows.Forms.Application.StartupPath, "Database\\LocalWeightData.mdf");
        
                path = string.Format("C:\\SPX_Weight\\Database\\LocalWeightData.mdf");              
                mMyConString = @"Data Source = (LocalDB)\MSSQLLocalDB; AttachDbFilename = C:\SPX_Weight\Database\LocalWeightData.mdf; Integrated Security = True";
                
                if(File.Exists(path))
                {
                    LogManager.getInstance().writeLog(string.Format("GetConnectionLocalDB"));
                    GetConnectionLocalDB();
                }
                else
                {
                    //create 해야합니다.   
                    LogManager.getInstance().writeLog(string.Format("creatDbTest"));

                    mMyConString = @"Server = (LocalDB)\MSSQLLocalDB; Integrated Security = True;";
                    creatDbTest(mMyConString);
                    mMyConString = @"Data Source = (LocalDB)\MSSQLLocalDB; AttachDbFilename = C:\SPX_Weight\Database\LocalWeightData.mdf; Integrated Security = True";
                    CreatTable(mMyConString);                 
                }            
            }
            catch (Exception e)
            {
                LogManager.getInstance().writeLog(e.ToString() + "openlocaldb");
                
            }
            return rt;
        }

        private void creatDbTest(string temp)
        {
            String str;
         
            mLocalDBConnection = new SqlConnection(temp);
            LogManager.getInstance().writeLog(string.Format("creatDbTest using"));
            string targetpath = "C:\\SPX_Weight\\Database";
            if (!Directory.Exists(targetpath))
                Directory.CreateDirectory(targetpath);

            using (mLocalDBConnection)
            {
               // LogManager.getInstance().writeLog(string.Format("creatDbTest mLocalDBConnection.Open();"));
                mLocalDBConnection.Open();
              //  LogManager.getInstance().writeLog(string.Format("creatDbTest mLocalDBConnection.Open() END;"));
                str = "CREATE DATABASE LocalWeightData ON PRIMARY " +
                 "(NAME = LocalWeightData, " +
                 "FILENAME = 'C:\\SPX_Weight\\Database\\LocalWeightData.mdf', " +
                 "SIZE = 3MB, MAXSIZE = 300MB, FILEGROWTH = 10%)" +
                 "LOG ON (NAME = LocalWeightData_log, " +
                 "FILENAME = 'C:\\SPX_Weight\\Database\\LocalWeightData_log.ldf', " +
                 "SIZE = 3MB, MAXSIZE = 300MB, FILEGROWTH = 10%)";
                SqlCommand myCommand = new SqlCommand(str, mLocalDBConnection);
              //  LogManager.getInstance().writeLog(string.Format("    SqlCommand myCommand = new SqlCommand(str, mLocalDBConnection);"));
                try
                {
                    if (mLocalDBConnection.State != ConnectionState.Open)
                    {
                        mLocalDBConnection.Open();
                    }
                 //   LogManager.getInstance().writeLog(string.Format("   myCommand.ExecuteNonQuery(); 1"));
                    myCommand.ExecuteNonQuery();
                 //   LogManager.getInstance().writeLog(string.Format("   myCommand.ExecuteNonQuery(); 2"));

                }
                catch (System.Exception ex)
                {
                    LogManager.getInstance().writeLog(ex.ToString());
                }
                finally
                {
                    if (mLocalDBConnection.State == ConnectionState.Open)
                    {
                        mLocalDBConnection.Close();
                    }
                }
            }
          
        }

        private void testmakedb()
        {
        }


        private void CreatTable(string strConn)
        {

            //LOT_END
            mLocalDBConnection = new SqlConnection(strConn);
            mLocalDBConnection.Open();

            if (mLocalDBConnection.State == ConnectionState.Open)
            {
                string strLOT_END = "CREATE TABLE LOT_END" +
                 "(" +
                 "[Id] INT IDENTITY (1, 1) NOT NULL," +
                 "[PLANT_ID] NVARCHAR (20) NULL," +
                 "[LINE_ID] NVARCHAR(20) NOT NULL," +
                 "[POS] NVARCHAR(20) NOT NULL," +
                 "[LOT] NVARCHAR(20) NOT NULL," +
                 "[LOT_SEQ] NVARCHAR(20) NOT NULL," +
                 "[END_ID] NVARCHAR(20) NULL," +
                 "[SIDE] NVARCHAR(20) NULL," +
                 "[END_SIDE] NVARCHAR(20) NULL," +
                 "PRIMARY KEY CLUSTERED ([Id] ASC)" +
                 ")";
                SqlCommand myCommand = new SqlCommand(strLOT_END, mLocalDBConnection);
                int rt = myCommand.ExecuteNonQuery();

                //PRODUCT_PLAN
                string strPRODUCT_PLAN = "CREATE TABLE PRODUCT_PLAN" +
                    "(" +
                    "[Id] INT IDENTITY (1, 1) NOT NULL," +                 
                    "[PRODUCT_DATE] CHAR(8) NOT NULL," +
                    "[PLANT_ID] NVARCHAR(20) NOT NULL," +
                    "[LINE_ID] NVARCHAR(20) NOT NULL," +
                    "[POS] NVARCHAR(20) NOT NULL," +
                    "[LOT] NVARCHAR(20) NOT NULL," +
                    "[LOT_SEQ] NVARCHAR(20) NOT NULL," +
                    "[START_END] NVARCHAR(10) NOT NULL," +
                    "[SIDE] NVARCHAR(20) NULL," +
                    "[END_END] NVARCHAR(20) NULL," +
                    "[END_QTY] NVARCHAR(20) NULL," +
                    "[INSPECT_END] NVARCHAR(20) NULL," +
                    "[CANCEL_YN] NVARCHAR(20) NOT NULL," +
                    "[END_DATA] CHAR(8) NULL," +      
                    "[CREATED_ON] DATETIME NULL," +   
                    "[MODIFIED_ON] DATETIME NULL," +
                    "PRIMARY KEY CLUSTERED([Id] ASC)" +
                    ")";
                myCommand = new SqlCommand(strPRODUCT_PLAN, mLocalDBConnection);
                rt = myCommand.ExecuteNonQuery();

                //SPIN_WEIGHT_RESULT
                string strSPIN_WEIGHT_RESULT = "CREATE TABLE SPIN_WEIGHT_RESULT" +
                    "(" +
                    "[Id] INT IDENTITY (1, 1) NOT NULL," +
                    "[PRODUCT_DATE] CHAR(8) NOT NULL," +
                    "[PLANT_ID] NVARCHAR(20) NOT NULL," +
                    "[LINE_ID] NVARCHAR(20) NOT NULL," +
                    "[POS] NVARCHAR(20) NOT NULL," +
                    "[LOT] NVARCHAR(20) NOT NULL," +
                    "[LOT_SEQ] NVARCHAR(20) NOT NULL," +
                    "[END_ID] NVARCHAR(20) NOT NULL," +
                    "[DOFF] INT NOT NULL," +
                    "[SIDE] NCHAR(20) NULL," +
                    "[USL] NUMERIC(15, 5) NULL," +
                    "[SL] NUMERIC(15, 5) NULL," +
                    "[LSL] NUMERIC(15, 5) NULL," +
                    "[UCL] NUMERIC(15, 5) NULL," +
                    "[CL] NUMERIC(15, 5) NULL," +
                    "[LCL] NUMERIC(15, 5) NULL," +
                    "[VALUE] NUMERIC(15, 5) NULL," +
                    "[MARK] NVARCHAR(20) NULL," +
                    "[DECISION_ID] NVARCHAR(20) NULL," +
                    "[SPEC_COLOR] NVARCHAR(10) NULL," +
                    "[CREATED_BY] NVARCHAR(20) NULL," +
                    "[CREATED_ON] DATETIME NULL," +
                    "[MODIFIED_BY] NVARCHAR(20) NULL," +
                    "[MODIFIED_ON] DATETIME NULL," +
                    "[SEND_QMS] NVARCHAR(1)  NULL," +
                    "PRIMARY KEY CLUSTERED([Id] ASC)" +
                    ")";
                myCommand = new SqlCommand(strSPIN_WEIGHT_RESULT, mLocalDBConnection);
                rt = myCommand.ExecuteNonQuery();


                //WEIGHT_SPEC
                string strWEIGHT_SPEC = "CREATE TABLE WEIGHT_SPEC" +
                    "(" +
                    "[Id] INT IDENTITY (1, 1) NOT NULL," +
                    "[PLANT_ID] NVARCHAR(20) NOT NULL," +
                    "[LOT] NVARCHAR(20) NOT NULL," +
                    "[LOT_SEQ] NVARCHAR(20) NOT NULL," +
                    "[APPLY_DATE] CHAR(8) NOT NULL," +
                    "[USL] NUMERIC(15, 5) NULL," +
                    "[SL] NUMERIC(15, 5) NULL," +
                    "[LSL] NUMERIC(15, 5) NULL," +
                    "[UCL] NUMERIC(15, 5) NULL," +
                    "[CL] NUMERIC(15, 5) NULL," +
                    "[LCL] NUMERIC(15, 5) NULL," +
                    "[MARK] NVARCHAR(20) NULL," +
                    "[SL_TOLERANCE] NVARCHAR(10) NULL," +
                    "[CL_TOLERANCE] NVARCHAR(10) NULL," +
                    "PRIMARY KEY CLUSTERED ([Id] ASC)" +
                    ")";
                myCommand = new SqlCommand(strWEIGHT_SPEC, mLocalDBConnection);
                rt = myCommand.ExecuteNonQuery();

                //user mast
                string struserMst = "CREATE TABLE txmUserMast" +
                    "(" +
                    "[Id] INT IDENTITY (1, 1) NOT NULL," +
                    "[PLANT_ID] NVARCHAR(20) NOT NULL," +
                    "[USER_ID] NVARCHAR(10) NOT NULL," +
                    "[USE_YN] NCHAR(1) NOT NULL," +
                    "PRIMARY KEY CLUSTERED ([Id] ASC)" +
                    ")";
                myCommand = new SqlCommand(struserMst, mLocalDBConnection);
                rt = myCommand.ExecuteNonQuery();
            }
        }

        private void CreateDataBase()
        {
            //mMyConString = @"Data Source = (LocalDB)\MSSQLLocalDB; AttachDbFilename = C:\SPX_Weight\Database\LocalWeightData.mdf; Integrated Security = True";
            ////여기서 Table 만들어 주어야 합니다. 
            //string sqlcreateDbQuery = "CREATE DATABASE LocalWeightData On PRIMARY" +
            //                            "(NAME = '" +        
            mMyConString = @"Data Source = (localdb)\MSSQLLocalDB; Initial Catalog = Arbitrary; Integrated Security = True; MultipleActiveResultSets = True; AttachDbFilename = C:\SPX_Weight\Arbitrary.mdf";
            mLocalDBConnection = new SqlConnection(mMyConString);
            mLocalDBConnection.Open();

        }

        private SqlConnection GetConnectionLocalDB()
        {
            if (isOpen() == false)
            {
                mLocalDBConnection = new SqlConnection(mMyConString);
                mLocalDBConnection.Open();
            }
            return mLocalDBConnection;
        }
        public SqlConnection SetCloseLocalDB()
        {
            if (isOpen() == true)
            {
                mLocalDBConnection.Close();
            }

            return mLocalDBConnection;
        }

        private SqlTransaction GetTransactionLocalDB()
        {
            return mLocalDBTransaction;
        }

        public bool isOpen()
        {
            if (mLocalDBConnection != null && mLocalDBConnection.State == System.Data.ConnectionState.Open)
                return true;
            else
                return false;
        }

        #region 로컬 DB에서 생산 데이터 가져오기 
        public DataSet GetProductPlanDataForLocal(string PlantId, string Date)
        {
            string select = string.Format("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}",
                  DBDataProductPlanT.Product_Date,
                  DBDataProductPlanT.Plant_Id,
                  DBDataProductPlanT.Line_Id,
                  DBDataProductPlanT.Pos,
                  DBDataProductPlanT.Lot,
                  DBDataProductPlanT.Lot_Seq,
                  DBDataProductPlanT.Side,
                  DBDataProductPlanT.End_Date,
                  DBDataProductPlanT.Created_On,
                  DBDataProductPlanT.Modified_On);
            string productPlanSelectQuery = string.Format("SELECT * from PRODUCT_PLAN" +
                " WHERE {0} = '{1}' " +
                " AND CANCEL_YN = 'N'" +
                "AND PRODUCT_DATE <= '{2}'",
                //                " WHERE {0} = '{1}' " +
                //" AND CANCEL_YN = 'N'" +
                //"AND ( ( ISNULL(END_DATE,'') = '' AND PRODUCT_DATE <= '{2}')" +
                //" OR ((ISNULL(END_DATE,'') <> '' AND PRODUCT_DATE <= '{3}') " +
                //") )",
                DBDataProductPlanT.Plant_Id,
                PlantId,  
                Date);

            return ReturnSingleQuery(productPlanSelectQuery);
        }

        public DataSet GetLotForLocal(string PlantId, string Date, string Hogi)
        {
            string productPlanSelectQuery = string.Format("SELECT DISTINCT LOT, LOT_SEQ from PRODUCT_PLAN" +
                " WHERE {0} = '{1}' AND {2} = '{3}'" +
                " AND CANCEL_YN = 'N'" +
                "AND  PRODUCT_DATE <= '{4}'" + 
                "AND  (END_DATA >= '{5}' OR END_DATA is null OR END_DATA ='')",
                //    " WHERE {0} = '{1}' AND {2} = '{3}'" +
                //" AND CANCEL_YN = 'N'" +
                //"AND ( ( ISNULL(END_DATE,'') = '' AND PRODUCT_DATE <= '{4}')" +
                //" OR ((ISNULL(END_DATE,'') <> '' AND PRODUCT_DATE <= '{5}') " +
                //") )",
                DBDataProductPlanT.Plant_Id,
                PlantId,              
                DBDataProductPlanT.Line_Id,
                Hogi,
                Date,
                Date
                );

            return ReturnSingleQuery(productPlanSelectQuery);
        }
        /// <summary>
        /// LOT를 가지고오는데 해당 LOT를 구분해서 string 을 따로 만들어야 할거 같다 
        /// OR 조건 넣어서 두개 가지고 와야해
        /// </summary>                
        public DataSet GetPosForLocal(string PlantId, string Date, string Hogi, string Lot)
        {
            string productPlanSelectQuery = "";
            string combilot = "";
            if (CheckGumiExceptionLot(Lot, out combilot))
            {
                //여기서 랏을 두개 쓰는 상황이 오면 진행 해야함
                productPlanSelectQuery = string.Format("SELECT DISTINCT POS, END_QTY, END_DATA from PRODUCT_PLAN" +
                " WHERE {0} = '{1}' AND {2} = '{3}' AND ({4} = '{5}' OR {6} = '{7}')" +
                " AND CANCEL_YN = 'N'" +
                "AND PRODUCT_DATE <= '{8}'" +
                "AND (END_DATA >= '{9}' OR END_DATA is Null OR END_DATA = '')",
                DBDataProductPlanT.Plant_Id,
                PlantId,
                DBDataProductPlanT.Line_Id,
                Hogi,
                DBDataProductPlanT.Lot,
                Lot,
                DBDataProductPlanT.Lot,
                combilot,
                Date,
                Date
                );
            }
            else
            {
                productPlanSelectQuery = string.Format("SELECT DISTINCT POS, END_QTY from PRODUCT_PLAN" +
                " WHERE {0} = '{1}' AND {2} = '{3}' AND {4} = '{5}'" +
                " AND CANCEL_YN = 'N'" +
                "AND PRODUCT_DATE <= '{6}'" +
                "AND (END_DATA >= '{7}' OR END_DATA is Null OR END_DATA = '')",
                DBDataProductPlanT.Plant_Id,
                PlantId,
                DBDataProductPlanT.Line_Id,
                Hogi,
                DBDataProductPlanT.Lot,
                Lot,
                Date,
                Date
                );
            }

            return ReturnSingleQuery(productPlanSelectQuery);
        }

        public bool CheckGumiExceptionLot(string Lot, out string combiLot)
        {
            bool rt = false;
            combiLot = "";
            //ExportExcel excel = new ExportExcel();
            //combiLot = excel.DualLot(Lot);
            switch (Lot)
            {
                case "Z2712":
                    rt = true;
                    combiLot = "Z2732";
                    break;
                case "Z2732":
                    rt = true;
                    combiLot = "Z2712";
                    break;
                case "Z4653":
                    rt = true;
                    combiLot = "Z4658";
                    break;
                case "Z2620":
                    rt = true;
                    combiLot = "Z2621";
                    break;
            }                        
            return rt;

        }

        public DataSet GetSideForLocal(string PlantId, string Date, string LineId, string Lot, string Pos)
        {
            string productPlanSelectQuery = "";
            string combilot = "";
            if (CheckGumiExceptionLot(Lot, out combilot))
            {
                productPlanSelectQuery = string.Format("SELECT DISTINCT SIDE, END_QTY, START_END, LOT_SEQ from PRODUCT_PLAN" +
                " WHERE {0} = '{1}' AND {2} = '{3}' AND ({4} = '{5}' OR {6} = '{7}') AND {8} = '{9}'" +
                " AND CANCEL_YN = 'N'" +
                " AND PRODUCT_DATE <= '{10}'" +
                " AND ( END_DATA >= '{11}' or END_DATA is null or END_DATA = '' )",
                DBDataProductPlanT.Plant_Id,
                PlantId,
                DBDataProductPlanT.Line_Id,
                LineId,
                DBDataProductPlanT.Lot,
                Lot,
                DBDataProductPlanT.Lot,
                combilot,
                DBDataProductPlanT.Pos,
                Pos,
                Date,
                Date
                );
            }
            else
            {
                //productPlanSelectQuery = string.Format("SELECT DISTINCT SIDE, END_QTY, START_END, LOT_SEQ from PRODUCT_PLAN" +
                //" WHERE {0} = '{1}' AND {2} = '{3}' AND {4} = '{5}' AND {6} = '{7}'" +
                //" AND CANCEL_YN = 'N'" +
                //" AND PRODUCT_DATE <= '{8}'" +
                //" AND ( END_DATA >= '{9}' or END_DATA is null or END_DATA = '' )",
                productPlanSelectQuery = string.Format("SELECT DISTINCT SIDE, END_QTY, START_END, LOT_SEQ, Product_Date from PRODUCT_PLAN" +
                " WHERE {0} = '{1}' AND {2} = '{3}' AND {4} = '{5}' AND {6} = '{7}'" +
                " AND CANCEL_YN = 'N'" +
                " AND PRODUCT_DATE <= '{8}'" +
                " AND ( END_DATA >= '{9}' or END_DATA is null or END_DATA = '' ) ORDER BY Product_Date ASC",
                DBDataProductPlanT.Plant_Id,
                PlantId,
                DBDataProductPlanT.Line_Id,
                LineId,
                DBDataProductPlanT.Lot,
                Lot,
                DBDataProductPlanT.Pos,
                Pos,
                Date,
                Date
                );
            }
            

            return ReturnSingleQuery(productPlanSelectQuery);
        }

        public DataSet GetEndDataForLocal(string PlantID, string LineID, string Lot, string Side)
        {
            string productPlanSelectQuery = string.Format("SELECT DISTINCT START_END, END_QTY from PRODUCT_PLAN" +
              " WHERE {0} = '{1}' AND {2} = '{3}' AND {4} = '{5}' AND {6} = '{7}';",
               DBDataProductPlanT.Plant_Id,
               PlantID,             
               DBDataProductPlanT.Line_Id,
               LineID,
               DBDataProductPlanT.Lot,
               Lot,
               DBDataProductPlanT.Side,
               Side);

            return ReturnSingleQuery(productPlanSelectQuery);
        }
        #endregion

        private DataSet ReturnSingleQuery(string Query)
        {
            DataSet dset = SelectByQuery(Query);
            return dset;
        }

        private DataSet SelectByQuery(string query)
        {
            DataSet dataSet = null;

            try
            {
                lock (mUsingLock)
                {
                    DbCommand command = new SqlCommand();
                    command.Connection = GetConnectionLocalDB();
                    command.Transaction = GetTransactionLocalDB();
                    command.CommandText = query;

                    DbDataAdapter adapter = GetDataAdapterLocalDB(command);

                    dataSet = new DataSet();
                    adapter.Fill(dataSet);
                    //command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                dataSet = null;
                LogManager.getInstance().writeLog(string.Format("SelectByQuery failed. : {0}", ex.Message));
            }

            return dataSet;
        }
        private SqlConnection GetConnectionQMSDB()
        {
            if (isOpen() == false)
            {
                mLocalDBConnection = new SqlConnection(mMyConString);
                mLocalDBConnection.Open();
            }
            return mLocalDBConnection;
        }

        private SqlTransaction GetTransactionQMSDB()
        {
            return mLocalDBTransaction;
        }
        private DbDataAdapter GetDataAdapterLocalDB(DbCommand command)
        {
            return new SqlDataAdapter(command as SqlCommand);
        }

        public QMS_SpinWeightResult MakeSpinWeightResultTable()
        {
            QMS_SpinWeightResult data = new QMS_SpinWeightResult();
            
            return data;

        }

        public bool InsertProductPlan(DataSet InsertData)
        {
            bool rt = false;
            //  Truncatetable("PRODUCT_PLAN");
            LogManager.getInstance().writeLog("local InsertProductPlan");
            InsertProductPlan(0, InsertData);
            LogManager.getInstance().writeLog("end InsertProductPlan");

            return rt;
        }

        public bool InsertLineEndData(DataSet insertData)
        {
            bool rt = false;
            Truncatetable("LOT_END");
            Insert_Line_End(0, insertData);
            return rt;
        }
        public void Insert_Line_End(int mode, DataSet inData)
        {
            if (inData.Tables.Count > 0)
            {
                List<DBLOT_END> convertData = new List<DBLOT_END>();

                foreach (DataRow row in inData.Tables[0].Rows)
                {
                    DBLOT_END item = new DBLOT_END();
                    int i = row[4].ToString().Length;
                    string endstart = row[4].ToString().Substring(0, i - 2);
                    string endnum = row[4].ToString().Substring(i - 2);
                    int endqty = Convert.ToInt32(row[7]);
                    for (int j = 0; j < endqty; j++)
                    {
                        string setnum = (Convert.ToInt32(endnum) + j).ToString("D2");
                        string MakeEndStart = string.Format("{0}{1}", endstart, setnum);

                        item.PLANT_ID = row[0].ToString();
                        item.LINE_ID = row[1].ToString();
                        item.POS = row[2].ToString();
                        item.LOT = row[3].ToString();
                        item.END_ID = MakeEndStart;
                        item.SIDE = row[5].ToString();
                        item.LOT_SEQ = row[6].ToString();
                        convertData.Add(item);

                        string where = string.Format("{0} = '{1}' AND {2} = '{3}' AND {4} = '{5}' AND {6} = '{7}' AND {8} = '{9}' AND {10} = '{11}' AND {12} = '{13}'",
                                DBLOT_ENDT.PlantID, item.PLANT_ID,
                                DBLOT_ENDT.LineID, item.LINE_ID,
                                DBLOT_ENDT.Pos, item.POS,
                                DBLOT_ENDT.LOT, item.LOT,
                                DBLOT_ENDT.EndId, item.END_ID,
                                DBLOT_ENDT.SIDE, item.SIDE,

                                DBLOT_ENDT.LOT_SEQ, item.LOT_SEQ
                              );

                        string select = string.Format("'{0}','{1}','{2}','{3}','{4}',N'{5}',N'{6}',N'{7}'",
                                item.PLANT_ID,
                                item.LINE_ID,
                                item.POS,
                                item.LOT,
                                item.LOT_SEQ,
                                item.END_ID,
                                item.SIDE,                               
                                endstart
                              );
                        Insert_Exist("LOT_END", select, where);
                    }                    
                }
            }
        }
        public void testsearchLotEnd()
        {
            DataSet templog = ReturnSingleQuery(string.Format("SELECT * FROM LOT_END"));
            foreach(DataRow i in templog.Tables[0].Rows)
            {
                LogManager.getInstance().writeLog(i[0].ToString() + i[2].ToString() + i[3].ToString() + i[4].ToString() + i[5].ToString());
            }      
        }

        public void testinsertLotEnd()
        {
            string where = string.Format("{0} = '{1}' AND {2} = '{3}' AND {4} = '{5}' AND {6} = '{7}' AND {8} = '{9}' AND {10} = '{11}' AND {12} = '{13}'",
            DBLOT_ENDT.PlantID, "1101",
            DBLOT_ENDT.LineID, "12",
            DBLOT_ENDT.Pos, "25",
            DBLOT_ENDT.LOT, "T1101",
            DBLOT_ENDT.EndId, "21",
            DBLOT_ENDT.SIDE, "R",
            DBLOT_ENDT.LOT_SEQ, "5"
            );

            string select = string.Format("'{0}','{1}','{2}','{3}','{4}',N'{5}',N'{6}',N'{7}'",
                    1,
                    2,
                    3,
                    4,
                    5,
                    6,
                    7,
                    0
                  );
            LogManager.getInstance().writeLog("Where_"+ where );
            LogManager.getInstance().writeLog("SELECT_" + select);            
            Insert_Exist("LOT_END", select, where);
        }

        public DataSet GetLotEnd()
        {
            string productPlanSelectQuery = string.Format("SELECT END_ID, SIDE, END_SIDE, LOT from LOT_END;");
            return ReturnSingleQuery(productPlanSelectQuery);           
        }

        public DataSet GetUserID(string ID)
        {
            string productPlanSelectQuery = string.Format("SELECT * FROM txmUserMast WHERE USER_ID = '{0}'" , ID);
            return ReturnSingleQuery(productPlanSelectQuery);
        }

        public DataSet GetLotID(string LOT)
        {
            string productPlanSelectQuery = string.Format("SELECT LOT FROM PRODUCT_PLAN " +
                "WHERE LOT = '{0}' " +
                "AND CANCEL YN = N", LOT);
            return ReturnSingleQuery(productPlanSelectQuery);
        }
        public DataSet GetSpinWeightResult()
        {          
            string productPlanSelectQuery = string.Format("SELECT {0} from SPIN_WEIGHT_RESULT WHERE SEND_QMS = '0';", MakeQuerySendQmsSpinweightResult());
            return ReturnSingleQuery(productPlanSelectQuery);       
        }

        #region Table Insert 구현부
        private int InsertProductPlan(int mode, DataSet inData)
        {

            List<DBDataProductPlan> selectList = convDataSetToInfoList<DBDataProductPlan>("Table", inData);

            if (selectList != null && selectList.Count > 0)
            {
                List<DBDataProductPlan> convertData = new List<DBDataProductPlan>();

                for (int i = 0; i < selectList.Count; i++)
                {
                    DBDataProductPlan item = new DBDataProductPlan();

                    item.Product_Date = selectList[i].Product_Date;
                    item.Plant_Id = selectList[i].Plant_Id;
                    item.Line_Id = selectList[i].Line_Id;
                    item.Pos = selectList[i].Pos;
                    item.Lot = selectList[i].Lot;
                    item.Lot_Seq = selectList[i].Lot_Seq;
                    item.Start_End = selectList[i].Start_End;
                    item.Side = selectList[i].Side;
                    item.End_End = selectList[i].End_End;
                    item.End_Qty = selectList[i].End_Qty;
                    item.Inspect_End = selectList[i].Inspect_End;
                    item.Cancel_Yn = selectList[i].Cancel_Yn;
                    item.End_Date = selectList[i].End_Date;
                    //item.Created_By = selectList[i].Created_By;
                    item.Created_On = selectList[i].Created_On;
                    //item.Modified_By = selectList[i].Modified_By;
                    item.Modified_On = selectList[i].Modified_On;

                    convertData.Add(item);
                }
                int icount = 0;
                foreach (DBDataProductPlan spec in convertData)
                {
                      string where = string.Format("{0} = {1} AND {2} = {3} AND {4} = '{5}' AND {6} = {7} AND {8} = '{9}' AND {10} = {11} AND {12} = '{13}'",
                          DBDataProductPlanT.Product_Date, spec.Product_Date,
                          DBDataProductPlanT.Plant_Id, spec.Plant_Id,
                          DBDataProductPlanT.Lot, spec.Lot,
                          DBDataProductPlanT.Lot_Seq, spec.Lot_Seq,
                          DBDataProductPlanT.Line_Id, spec.Line_Id,
                          DBDataProductPlanT.Pos, spec.Pos,
                          DBDataProductPlanT.Start_end, spec.Start_End

                        );
                    string select = string.Format("'{0}','{1}','{2}','{3}','{4}','{5}',N'{6}',N'{7}',N'{8}',N'{9}','{10}','{11}','{12}','{13}','{14}'",
                            spec.Product_Date,
                            spec.Plant_Id,
                            spec.Line_Id,
                            spec.Pos,
                            spec.Lot,
                            spec.Lot_Seq,
                            spec.Start_End,
                            spec.Side,
                            spec.End_End,
                            spec.End_Qty,
                            spec.Inspect_End,
                            spec.Cancel_Yn,
                            spec.End_Date,
                            spec.Created_On.ToString("yyyy-MM-dd hh:mm:ss"),
                            spec.Modified_On.ToString("yyyy-MM-dd hh:mm:ss")
                          );

                    Insert_Exist("PRODUCT_PLAN", select, where);

                    string value = string.Format("{0} = {1}, {2} = '{3}', {4} = '{5}', {6} = '{7}'",
                        DBDataProductPlanT.Product_Date, spec.Product_Date,
                        DBDataProductPlanT.Cancel_Yn, spec.Cancel_Yn,
                        "END_DATA", spec.End_Date,
                        DBDataProductPlanT.Modified_On, spec.Modified_On.ToString("yyyy-MM-dd hh:mm:ss")
                        );

                    Updata("PRODUCT_PLAN", value, where);
                    //업데이트도 하기
                    Console.WriteLine("{0}", icount);
                    icount++;                        
                }

                LogManager.getInstance().writeLog("InsertProductPlan Count" + icount.ToString());
            } 
            return 0;
        }

    public bool InsertUserID(DataSet UserTempData)
        {
            bool rt = false;
            List<DBDataMstUser> selectList = convDataSetToInfoList<DBDataMstUser>("Table", UserTempData);

            if (selectList != null && selectList.Count > 0)
            {
                List<DBDataMstUser> convertData = new List<DBDataMstUser>();

                for (int i = 0; i < selectList.Count; i++)
                {
                    DBDataMstUser item = new DBDataMstUser();

                    item.Plant_Id = selectList[i].Plant_Id;
                    item.User_Id = selectList[i].User_Id;
                    item.Use_YN = selectList[i].Use_YN;
                    convertData.Add(item);
                }

                foreach (DBDataMstUser spec in convertData)
                {
                    //INSERT에서 FROM DUAL WHERE NOT EXISTS 구분할 WHERER 구문을 만들어 봄
                    string where = string.Format("{0} = '{1}' AND {2} = '{3}' AND {4} = '{5}'",
                                              DBDataMstUserT.Plant_Id, spec.Plant_Id,
                                              DBDataMstUserT.User_Id, spec.User_Id,
                                              DBDataMstUserT.Use_YN, spec.Use_YN
                                             );

                    string value = string.Format("'{0}','{1}','{2}'",
                        spec.Plant_Id, spec.User_Id, spec.Use_YN
                        );

                    Insert_Exist("txmUserMast", value, where);
                }
            }
            return true;
        }
        public bool InsertSpinWeightSpec(DataSet WeightTempData)
        {
            bool rt = false;
            List<QMS_SpinWeightSpec> selectList = convDataSetToInfoList<QMS_SpinWeightSpec>("Table", WeightTempData);
            int countspec = 0;
            if (selectList != null && selectList.Count > 0)
            {
                List<QMS_SpinWeightSpec> convertData = new List<QMS_SpinWeightSpec>();

                for (int i = 0; i < selectList.Count; i++)
                {
                    QMS_SpinWeightSpec item = new QMS_SpinWeightSpec();

                    item.Plant_Id = selectList[i].Plant_Id;
                    item.Lot = selectList[i].Lot;
                    item.Lot_seq = selectList[i].Lot_seq;
                    item.Apply_Date = selectList[i].Apply_Date;
                    item.Usl = selectList[i].Usl;
                    item.Sl = selectList[i].Sl;
                    item.Lsl = selectList[i].Lsl;
                    item.Cl = selectList[i].Cl;
                    item.Lcl = selectList[i].Lcl;
                    item.Mark = selectList[i].Mark;
                    item.Sl_tolerance = selectList[i].Sl_tolerance;
                    item.Cl_tolerance = selectList[i].Cl_tolerance;
                    item.Created_by = selectList[i].Created_by;
                    item.Created_On = selectList[i].Created_On;
                    item.Modified_by = selectList[i].Modified_by;
                    item.Modified_On = selectList[i].Modified_On;

                    convertData.Add(item);
                }

                foreach (QMS_SpinWeightSpec spec in convertData)
                {
                    //INSERT에서 FROM DUAL WHERE NOT EXISTS 구분할 WHERER 구문을 만들어 봄
                    string where = string.Format("{0} = '{1}' AND {2} = '{3}' AND {4} = {5} AND {6} = {7}",
                                              QMS_SpinWeightSpecT.Plant_Id, spec.Plant_Id,
                                              QMS_SpinWeightSpecT.Lot, spec.Lot,
                                              QMS_SpinWeightSpecT.Lot_seq, spec.Lot_seq,
                                              QMS_SpinWeightSpecT.apply_date, spec.Apply_Date
                                            );

                    string value = string.Format("'{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}','{12}'",
                        spec.Plant_Id,
                        spec.Lot,
                        spec.Lot_seq,
                        spec.Apply_Date,
                        spec.Usl,
                        spec.Sl,
                        spec.Lsl,
                        spec.Ucl,
                        spec.Cl,
                        spec.Lcl,                      
                        spec.Mark,
                        spec.Sl_tolerance,
                        spec.Cl_tolerance);

                    Insert_Exist("WEIGHT_SPEC", value, where);

                    string value2 = string.Format("{0} = '{1}', {2} = '{3}', {4} = '{5}', {6} = '{7}'",
                                              QMS_SpinWeightSpecT.Plant_Id, spec.Plant_Id,
                                              QMS_SpinWeightSpecT.Lot, spec.Lot,
                                              QMS_SpinWeightSpecT.Lot_seq, spec.Lot_seq,
                                              QMS_SpinWeightSpecT.apply_date, spec.Apply_Date
                                              );

                    Updata("WEIGHT_SPEC", value2, where);

                    countspec++;
                }
            }
            LogManager.getInstance().writeLog("InsertWeightSPEC Count" + countspec.ToString());
            return rt;
        }

        public Int64 DeleteProductPlanUnderEnddata()
        {
            Int64 returnValue = 0;
            try
            {
                lock (mUsingLock)
                {
                    DbCommand command = new SqlCommand();
                    command.Connection = GetConnectionQMSDB();
                    command.Transaction = GetTransactionQMSDB();
                    string temp = string.Format("Delete * from [dbo].[PRODUCT_PLAN] where END_DATA < GETDATE() - 2;");
                    command.CommandText = temp;
                    returnValue = Convert.ToInt64(command.ExecuteNonQuery());
                }
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
            return returnValue;
        }

        public bool InsertSpinWeightResult(QMS_SpinWeightResult WeightTempData, int send_qms)
        {
            QMS_SpinWeightResult spec = WeightTempData;

            if(spec != null)
            {
                //INSERT에서 FROM DUAL WHERE NOT EXISTS 구분할 WHERER 구문을 만들어 봄
                string where = string.Format("{0} = '{1}' AND {2} = '{3}' AND {4} = {5} AND {6} = '{7}' AND {8} = '{9}' AND [{10}] = {11} AND {12} = '{13}' AND {14} = '{15}'",
                                          QMS_SpinWeightResultT.Plant_Id, spec.Plant_Id,
                                          QMS_SpinWeightResultT.Lot, spec.Lot,
                                          QMS_SpinWeightResultT.Lot_seq, spec.Lot_seq,
                                          QMS_SpinWeightResultT.Line_Id, spec.Line_Id,
                                          QMS_SpinWeightResultT.End_Id, spec.End_Id,
                                          QMS_SpinWeightResultT.Value, spec.Value,
                                          QMS_SpinWeightResultT.Product_data, spec.Product_Date,
                                          QMS_SpinWeightResultT.Pos, spec.Pos
                                         );

                string value = string.Format("'{0}','{1}','{2}','{3}','{4}','{5}',N'{6}','{7}',N'{8}','{9}','{10}','{11}','{12}'" +
                    ",'{13}','{14}','{15}','{16}','{17}','{18}','{19}','{20}','{21}','{22}','{23}'",
                    spec.Product_Date,                   
                    spec.Plant_Id,
                    spec.Line_Id,
                    spec.Pos,
                    spec.Lot,
                    spec.Lot_seq,
                    spec.End_Id,
                    spec.Doff,
                    spec.Side,
                    spec.Usl,
                    spec.Sl,
                    spec.Lsl,
                    spec.Ucl,
                    spec.Cl,
                    spec.Lcl,
                    spec.Value,
                    spec.Mark,
                    spec.Decision_id,
                    spec.Spec_color,
                    spec.Created_by,
                    UtilManager.GetTimeWithMilli(),
                    spec.Modified_by,
                    UtilManager.GetTimeWithMilli(),
                    send_qms
                    );

                if (1 == Insert_Exist("SPIN_WEIGHT_RESULT", value, where)) return true;
                else return false;
            }
                return true;
        }

        public DataSet GetLocalResultTableRemainData()
        {
            string ResultSelectQuery = string.Format("select * from SPIN_WEIGHT_RESULT Where SEND_QMS = '0';");

            return ReturnSingleQuery(ResultSelectQuery);
        }

        public bool UpdataSpinWeightResult(QMS_SpinWeightResult WeightTempData, int send_qms)
        {
            QMS_SpinWeightResult spec = WeightTempData;

            if (spec != null)
            {
                //INSERT에서 FROM DUAL WHERE NOT EXISTS 구분할 WHERER 구문을 만들어 봄
                string where = string.Format("{0} = '{1}' AND {2} = '{3}' AND {4} = {5} AND {6} = '{7}' AND {8} = '{9}' AND {10} = {11}",
                                          QMS_SpinWeightResultT.Plant_Id, spec.Plant_Id,
                                          QMS_SpinWeightResultT.Lot, spec.Lot,
                                          QMS_SpinWeightResultT.Lot_seq, spec.Lot_seq,
                                          QMS_SpinWeightResultT.Line_Id, spec.Line_Id,
                                          QMS_SpinWeightResultT.End_Id, spec.End_Id,
                                          QMS_SpinWeightResultT.Value, spec.Value
                                         );

                string value = string.Format("{0} = '{1}'",
                    QMS_SpinWeightResultT.Send_QMS,                    
                    send_qms
                    );
                //SPIN_WEIGHT_RESULT_TEST 의 임시테이블을 만들어서 거기다가 업로드 하는 것을 테스트 중
                Updata("SPIN_WEIGHT_RESULT", value, where);
            }
            return true;
        }

        private Int64 insert_Test(string temp)
        {
            Int64 returnValue = 0;
            DbCommand command = new SqlCommand();
            command.Connection = GetConnectionQMSDB();
            command.Transaction = GetTransactionQMSDB();

            Console.WriteLine(command.CommandText);
            returnValue = Convert.ToInt64(command.ExecuteNonQuery());

            return returnValue;
        }

        private Int64 Insert_Exist(string tableName, string values, string exitstquery)
        {
            Int64 returnValue = 0;
            try
            {
                lock (mUsingLock)
                {
                    DbCommand command = new SqlCommand();
                    command.Connection = GetConnectionQMSDB();
                    command.Transaction = GetTransactionQMSDB();
                    string exists = string.Format("IF NOT EXISTS(SELECT * FROM {0} WHERE {1})", tableName, exitstquery);
                    string begin = " begin ";
                    string temp = string.Format("INSERT INTO {0} VALUES({1})", tableName, values);
                    string end = " END ";
                    
                    command.CommandText = exists + begin + temp + end;
                    Console.WriteLine(command.CommandText);
                    returnValue = Convert.ToInt64(command.ExecuteNonQuery());
                }
            }
            catch(Exception e)
            {
                e.ToString();
            }
            return returnValue;
        }

        private Int64 Updata(string tableName, string values, string exitstquery)
        {
            Int64 returnValue = 0;
            try
            {
                lock (mUsingLock)
                {
                    DbCommand command = new SqlCommand();
                    command.Connection = GetConnectionQMSDB();
                    command.Transaction = GetTransactionQMSDB();

                    string temp = string.Format("UPDATE {0} SET {1} " +
                        "WHERE {2};",
                        tableName, values, exitstquery );
                    command.CommandText = temp;
                   returnValue = Convert.ToInt64(command.ExecuteNonQuery());
                }
            }
            catch(Exception ex)
            {
                ex.ToString();
            }
            return returnValue;
        }


        #endregion

        public List<T> convDataSetToInfoList<T>(string tableName, DataSet dataSet) where T : new()
        {
            DataTable table = dataSet.Tables[tableName];
            if (table == null)
                return default(List<T>);

            List<T> infoList = new List<T>();
            for (int idx = 0; idx < table.Rows.Count; ++idx)
            {
                try
                {
                    T info = new T();
                    setColumnValuesToObject<T>((T)info, table.Rows[idx]);
                    infoList.Add((T)info);
                }
                catch (Exception e)
                {
                    Console.WriteLine(string.Format("convDataSetToInfoList exception : {0}" + e.Message));
                }
            }

            return infoList;
        }

        private void setColumnValuesToObject<T>(T obj, DataRow row)
        {
            PropertyInfo[] properties = typeof(T).GetProperties();
            for (int idx = 0; idx < row.ItemArray.Length; ++idx)
            {
                if (row[idx] != DBNull.Value)
                    properties[idx].SetValue(obj, row[idx], null);
                else 
                    properties[idx].SetValue(obj, null, null);
            }
        }

        public void Truncatetable(string tablename)
        {
            DbCommand command = new SqlCommand();
            command.Connection = GetConnectionQMSDB();
            command.Transaction = GetTransactionQMSDB();

            string temp = string.Format("TRUNCATE TABLE {0};", tablename);
            command.CommandText = temp;
            int result = command.ExecuteNonQuery();
        }

        #region 결과 데이터 로컬 DB에 입력하기
        private int UpDataSpinWeightSpec(int mode, DataRow data)
        {
            DbCommand command = new SqlCommand();
            command.Connection = GetConnectionQMSDB();
            command.Transaction = GetTransactionQMSDB();

            string temp = string.Format("INSERT INTO SPIN_WEIGHT_TEMP VALUES((SELECT isnull(MAX(id),0)+1 FROM insertTestTable b),'gogogogogo','222222222222222');");

            command.CommandText = temp;
            int result = command.ExecuteNonQuery();

            return 0;
        }

        //결과데이터 입력부분
        public bool InsertSpinWeightTemp(DataRow WeightTempData)
        {
            bool rt = false;

            return rt;
        }
        #endregion

        private int UpDataSpinWeightTemp(int mode, DataSet data)
        {
            return 0;
        }

        public DataSet GetSpinWeightSpec_Local(string PLANT_ID, string LOT, string LOT_SEQ = "1")
        {

            //현업에선 LOT_SEQ를 입력하지 않든데?
            //string productPlanSelectQuery = string.Format("SELECT LSL, USL from WEIGHT_SPEC WHERE {0} = {1} AND {2} = {3} AND {4} = {5};",
            //    specdata.Plant_Id,
            //    PLANT_ID,
            //    specdata.Lot,
            //    LOT,
            //    specdata.Lot_seq,
            //    LOT_SEQ);

            string productPlanSelectQuery = string.Format("SELECT USL, SL, LSL, UCL, CL, LCL, MARK, SL_TOLERANCE from WEIGHT_SPEC WHERE {0} = '{1}' AND {2} = '{3}' AND {4} = '{5}';",
                QMS_SpinWeightSpecT.Plant_Id,
                PLANT_ID,
                QMS_SpinWeightSpecT.Lot,
                LOT,
                QMS_SpinWeightSpecT.Lot_seq,
                LOT_SEQ
                );


            return ReturnSingleQuery(productPlanSelectQuery);
        }


        public DataSet GetPOS_END(string PlantId, string Date, string Pos, string Line, string Lotseq, string lot)
        {

            string productPlanSelectQuery = "";
            string combilot = "";

            if (CheckGumiExceptionLot(lot, out combilot))
            {
                productPlanSelectQuery = string.Format("SELECT PLANT_ID, LINE_ID, POS, LOT, START_END, SIDE ,LOT_SEQ, END_QTY " +
                        "FROM [{0}] WHERE {1} = '{2}' " +
                        "AND {3} = '{4}' AND {5} = '{6}'" +
                        "AND CANCEL_YN = 'N'" +
                        "AND ({7} = '{8}' OR {9} = '{10}')",
                            DBDataProductPlanT.TbNameLocal,
                            DBDataProductPlanT.Plant_Id,
                            PlantId,
                            DBDataProductPlanT.Pos,
                            Pos,
                            DBDataProductPlanT.Line_Id,
                            Line,
                            DBDataProductPlanT.Lot,
                            lot,
                            DBDataProductPlanT.Lot,
                            combilot
                            );
            }
            else
            {
                productPlanSelectQuery = string.Format("SELECT PLANT_ID, LINE_ID, POS, LOT, START_END, SIDE ,LOT_SEQ, END_QTY " +
                        "FROM [{0}] WHERE {1} = '{2}' " +
                        "AND {3} = '{4}' AND {5} = '{6}' AND {7} = '{8}'" +
                        "AND CANCEL_YN = 'N'" +
                        "AND {9} = '{10}'",
                            DBDataProductPlanT.TbNameLocal,
                            DBDataProductPlanT.Plant_Id,
                            PlantId,
                            DBDataProductPlanT.Pos,
                            Pos,
                            DBDataProductPlanT.Line_Id,
                            Line,
                            DBDataProductPlanT.Lot_Seq,
                            Lotseq,
                            DBDataProductPlanT.Lot,
                            lot
                            );
            }

            return ReturnSingleQuery(productPlanSelectQuery);
        }

        public void test_insert_db()
        {
            testinsertLotEnd();
        }

        private string MakeQuerySendQmsSpinweightResult()
        {
            StringBuilder strtemp = new StringBuilder();

            strtemp.Remove(0, strtemp.Length);

            strtemp.AppendFormat("{0}",  QMS_SpinWeightResultT.Product_data);
            strtemp.AppendFormat(", {0}", QMS_SpinWeightResultT.Plant_Id);
            strtemp.AppendFormat(", {0}", QMS_SpinWeightResultT.Line_Id);
            strtemp.AppendFormat(", {0}", QMS_SpinWeightResultT.Pos);            
            strtemp.AppendFormat(", {0}", QMS_SpinWeightResultT.Lot);
            strtemp.AppendFormat(", {0}", QMS_SpinWeightResultT.Lot_seq);
            strtemp.AppendFormat(", {0}", QMS_SpinWeightResultT.End_Id);
            strtemp.AppendFormat(", {0}", QMS_SpinWeightResultT.Doff);
            strtemp.AppendFormat(", {0}", QMS_SpinWeightResultT.Side);
            strtemp.AppendFormat(", {0}", QMS_SpinWeightResultT.Usl);
            strtemp.AppendFormat(", {0}", QMS_SpinWeightResultT.Sl);
            strtemp.AppendFormat(", {0}", QMS_SpinWeightResultT.Lsl);
            strtemp.AppendFormat(", {0}", QMS_SpinWeightResultT.Ucl);
            strtemp.AppendFormat(", {0}", QMS_SpinWeightResultT.Cl);
            strtemp.AppendFormat(", {0}", QMS_SpinWeightResultT.Lsl);
            strtemp.AppendFormat(", {0}", QMS_SpinWeightResultT.Value);
            strtemp.AppendFormat(", {0}", QMS_SpinWeightResultT.Mark);
            strtemp.AppendFormat(", {0}", QMS_SpinWeightResultT.Decision_id);
            strtemp.AppendFormat(", {0}", QMS_SpinWeightResultT.Spec_color);
            strtemp.AppendFormat(", {0}", QMS_SpinWeightResultT.Created_by);
            strtemp.AppendFormat(", {0}", QMS_SpinWeightResultT.Created_On);
            strtemp.AppendFormat(", {0}", QMS_SpinWeightResultT.Modified_by);
            strtemp.AppendFormat(", {0}", QMS_SpinWeightResultT.Modified_On);

            return strtemp.ToString();
        }
    }
}
