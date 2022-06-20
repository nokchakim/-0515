using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SPX_Weight.DataModel;
using MySql.Data.MySqlClient;
using System.IO;
using System.Data.SqlClient;
using System.Data.Common;
using System.Data;
using System.Diagnostics;
using Microsoft.Win32;
using System.Windows;
using System.Reflection;



namespace SPX_Weight.DataManager
{
    public class QMSDataManager
    {
        private static DateTime LastExportDate; //마지막으로 QMS로 Result 내보낸 시간
        private static DateTime LastImportDate; //마지막으로 QMS에서 테이블정보를 가져온 시간

        private static QMSDataManager mInstance = null;


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

        private const string KEY_CLIENTID = "Client_ID";
        private const string KEY_SERVERIP = "Server_IP";
        private const string KEY_SERVERPORT = "Server_Port";

        private string CLIENT_ID = "";
        private string SERVER_IP = "";
        private string SERVER_PORT = "";

        public const string REGISTRY_KEY_LAST_EXPORT_DB = "LastExportDB";
        public const string REGISTRY_KEY_LAST_IMPORT_DB = "LastImportDB";

        public static string IDT_PRE = "["; // SQL Server Identifier prefix
        public static string IDT_SUF = "]"; // SQL Server Identifier suffix
        public static string VAL_PRE = "'";
        public static string VAL_SUF = "'";


        private string mMyConString;

        private static Object mInstanceLock = new Object();
        private static Object mUsingLock = new Object();
        private static Object mTransactionLock = new Object();


        private SqlConnection mDBConnection = null;
        private SqlTransaction mDBTransaction = null;
        private int mDBTransactionCount = 0;

        //생성
        private QMSDataManager()
        {

        }

        public string ServerIP
        {
            get { return SERVER_IP; }
            set { }
        }

        public string ServerIpPort
        {
            get { return SERVER_IP + "," + SERVER_PORT; }
            set { }
        }

        public string ClientID
        {
            get { return CLIENT_ID; }
            set { }
        }

        public static QMSDataManager getInstance()
        {
            if (mInstance == null)
            {
                lock (mInstanceLock)
                {
                    if (mInstance == null)
                        mInstance = new QMSDataManager();
                }
            }
            return mInstance;
        }

        public bool OpenQMSDB()
        {
            bool rt = true;

            try
            {
                //개발서버
                //db_qms_IP = "10.10.224.101";
                //db_qms_PORT = "53314";
                //QMS
                //db_qms_IP = "10.11.2.110";
                //db_qms_PORT = "61433";
                db_qms_DB = "TNCQMS";
                db_qms_ID = "tncqms";
                db_qms_PWD = "tncqms20n@";
                //db_qms_DB = "TNCQMS";
                //db_qms_ID = "cheadmin";
                //db_qms_PWD = "tncqms20n@";

                mMyConString = String.Format("Server = {0},{1};  Database = {2}; uid = {3}; pwd = {4};", db_qms_IP, db_qms_PORT, db_qms_DB, db_qms_ID, db_qms_PWD);
                //db 연결하기 
                GetConnectionQMSDB();
            }
            catch
            {

            }

            return rt;
        }

        private SqlConnection GetConnectionQMSDB()
        {
            if (isOpen() == false)
            {
                mDBConnection = new SqlConnection(mMyConString);
                mDBConnection.Open();
                // mDBTransaction = mDBConnection.BeginTransaction();                
            }

            return mDBConnection;
        }

        public SqlConnection SetCloseQMSDB()
        {
            if (isOpen() == true)
            {
                mDBConnection.Close();             
            }

            return mDBConnection;
        }

        private SqlTransaction GetTransactionQMSDB()
        {            
            return mDBTransaction;
        }
        
        private DbDataAdapter GetDataAdapterQMSDB(DbCommand command)
        {
            return new SqlDataAdapter(command as SqlCommand);
        }


        public bool isOpen()
        {
            if (mDBConnection != null && mDBConnection.State == System.Data.ConnectionState.Open)
                return true;
            else
                return false;
        }


        public void ExportQmsDB(List<DbDataTemp> LocalDbData)
        {
            List<QMS_SpinWeightTemp> weighttemp = new List<QMS_SpinWeightTemp>();

            foreach (DbDataTemp data in LocalDbData)
            {
                QMS_SpinWeightTemp tempdata = new QMS_SpinWeightTemp();

                tempdata.Product_Date = data.Product_Date;
                tempdata.Plant_Id = data.Platn_Id;
                tempdata.Lot = data.Lot;
                tempdata.Lot_seq = data.Lot_Seq;
                tempdata.Line_Id = data.Line_Id;
                tempdata.Pos = data.Pos;
                tempdata.End_Id = data.End_Id;
                tempdata.Doff = data.Doff;

                tempdata.Value = data.Value;

                tempdata.Usl = data.Usl;
                tempdata.Sl = data.Sl;
                tempdata.Lsl = data.Lsl;
                tempdata.Ucl = data.Ucl;
                tempdata.Cl = data.Cl;
                tempdata.Lcl = data.Lcl;

                weighttemp.Add(tempdata);
            }
        }
        private DataSet Select(string tableName, string where = null, string option = null)
        {
            DataSet dataSet = null;

            try
            {
                lock (mUsingLock)
                {
                    DbCommand command = new SqlCommand();
                    command.Connection = GetConnectionQMSDB();
                    command.Transaction = GetTransactionQMSDB();
                    command.CommandText = string.Format("SELECT * FROM {0}{1}{2}", IDT_PRE, tableName, IDT_SUF);
                    if (where != null)
                        command.CommandText = string.Format("{0} WHERE {1}", command.CommandText, where);
                    if (option != null)
                        command.CommandText = string.Format("{0} {1}", command.CommandText, option);

                    DbDataAdapter adapter = GetDataAdapterQMSDB(command);
                    dataSet = new DataSet();
                    adapter.Fill(dataSet, tableName);
                }
            }
            catch (Exception ex)
            {
                dataSet = null;
                LogManager.getInstance().writeLog(string.Format("Select failed. : {0}", ex.Message));
            }

            return dataSet;
        }

        private DataSet Select(string tableName, string Query)
        {
            DataSet dataSet = null;

            try
            {
                lock (mUsingLock)
                {
                    DbCommand command = new SqlCommand();
                    command.Connection = GetConnectionQMSDB();
                    command.Transaction = GetTransactionQMSDB();
                    command.CommandText = Query;

                    DbDataAdapter adapter = GetDataAdapterQMSDB(command);

                    dataSet = new DataSet();
                    adapter.Fill(dataSet, tableName);
                }
            }
            catch (Exception ex)
            {
                dataSet = null;
                LogManager.getInstance().writeLog(string.Format("Select failed. : {0}", ex.Message));
            }

            return dataSet;
        }

        private string MakeQuerySendQmsSpinweightTemp(QMS_SpinWeightTemp tempdata)
        {
            StringBuilder strtemp = new StringBuilder();

            strtemp.Remove(0, strtemp.Length);

            strtemp.AppendFormat("{0}", tempdata.Product_Date);
            strtemp.AppendFormat(", {0}", tempdata.Plant_Id);
            strtemp.AppendFormat(", {0}", tempdata.Line_Id);
            strtemp.AppendFormat(", {0}", tempdata.Pos);
            strtemp.AppendFormat(", {0}", tempdata.End_Id);
            strtemp.AppendFormat(", {0}", tempdata.Lot);
            strtemp.AppendFormat(", {0}", tempdata.Lot_seq);
            strtemp.AppendFormat(", {0}", tempdata.Doff);
            strtemp.AppendFormat(", {0}", tempdata.Side);
            strtemp.AppendFormat(", {0}", tempdata.Usl);
            strtemp.AppendFormat(", {0}", tempdata.Sl);
            strtemp.AppendFormat(", {0}", tempdata.Lsl);
            strtemp.AppendFormat(", {0}", tempdata.Ucl);
            strtemp.AppendFormat(", {0}", tempdata.Cl);
            strtemp.AppendFormat(", {0}", tempdata.Lsl);
            strtemp.AppendFormat(", {0}", tempdata.Value);
            strtemp.AppendFormat(", {0}", tempdata.Mark);
            strtemp.AppendFormat(", {0}", tempdata.Decision_id);
            strtemp.AppendFormat(", {0}", tempdata.Spec_color);
            strtemp.AppendFormat(", {0}", tempdata.Created_by);
            strtemp.AppendFormat(", {0}", tempdata.Created_On);
            strtemp.AppendFormat(", {0}", tempdata.Modified_by);
            strtemp.AppendFormat(", {0}", tempdata.Modified_On);

            return strtemp.ToString();
        }

        private string MakeQuerySendQmsSpinweightResult(QMS_SpinWeightResult tempdata)
        {
            StringBuilder strtemp = new StringBuilder();

            strtemp.Remove(0, strtemp.Length);

            strtemp.AppendFormat("{0}", tempdata.Product_Date);
            strtemp.AppendFormat(", {0}", tempdata.Plant_Id);
            strtemp.AppendFormat(", {0}", tempdata.Line_Id);
            strtemp.AppendFormat(", {0}", tempdata.Pos);
            strtemp.AppendFormat(", {0}", tempdata.End_Id);
            strtemp.AppendFormat(", {0}", tempdata.Lot);
            strtemp.AppendFormat(", {0}", tempdata.Lot_seq);
            strtemp.AppendFormat(", {0}", tempdata.Doff);
            strtemp.AppendFormat(", {0}", tempdata.Side);
            strtemp.AppendFormat(", {0}", tempdata.Usl);
            strtemp.AppendFormat(", {0}", tempdata.Sl);
            strtemp.AppendFormat(", {0}", tempdata.Lsl);
            strtemp.AppendFormat(", {0}", tempdata.Ucl);
            strtemp.AppendFormat(", {0}", tempdata.Cl);
            strtemp.AppendFormat(", {0}", tempdata.Lsl);
            strtemp.AppendFormat(", {0}", tempdata.Value);
            strtemp.AppendFormat(", {0}", tempdata.Mark);
            strtemp.AppendFormat(", {0}", tempdata.Decision_id);
            strtemp.AppendFormat(", {0}", tempdata.Spec_color);
            strtemp.AppendFormat(", {0}", tempdata.Created_by);
            strtemp.AppendFormat(", {0}", tempdata.Created_On);
            strtemp.AppendFormat(", {0}", tempdata.Modified_by);
            strtemp.AppendFormat(", {0}", tempdata.Modified_On);

            return strtemp.ToString();
        }

        private DataSet Inset(string tableName, string insertvalue, string where = null, string option = null)
        {
            DataSet dataSet = null;
            try
            {
                lock (mUsingLock)
                {
                    DbCommand command = new SqlCommand();
                    command.Connection = GetConnectionQMSDB();
                    command.Transaction = GetTransactionQMSDB();
                    //string temp = MakeQuerySendQmsSpinweightTemp();
                    command.CommandText = string.Format("INSERT INTO {0}{1}{2} VALUES({3})", IDT_PRE, tableName, IDT_SUF, insertvalue);
                    if (where != null)
                        command.CommandText = string.Format("{0} WHERE {1}", command.CommandText, where);
                    if (option != null)
                        command.CommandText = string.Format("{0} {1}", command.CommandText, option);

                    DbDataAdapter adapter = GetDataAdapterQMSDB(command);

                    dataSet = new DataSet();
                    adapter.Fill(dataSet, tableName);
                }
            }
            catch (Exception ex)
            {
                dataSet = null;
                LogManager.getInstance().writeLog(string.Format("Select failed. : {0}", ex.Message));
            }

            return dataSet;
        }


        public void testselectQuery()
        {
            Select("");
        }

        public void loadINI()
        {
            string iniFileFullPath = System.IO.Directory.GetCurrentDirectory() + "\\info.ini";

            try
            {
                if (System.IO.File.Exists(iniFileFullPath))
                {

                    CLIENT_ID = GetIniValue(iniFileFullPath, "SET", KEY_CLIENTID);
                    SERVER_IP = GetIniValue(iniFileFullPath, "SET", KEY_SERVERIP);
                    SERVER_PORT = GetIniValue(iniFileFullPath, "SET", KEY_SERVERPORT);
                }
            }
            catch (Exception e)
            {
                LogManager.getInstance().writeLog(e.ToString());
            }
        }

        [System.Runtime.InteropServices.DllImport("kernel32")]
        public static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        public static String GetIniValue(string path, String Section, String Key)
        {
            StringBuilder temp = new StringBuilder();
            int i = GetPrivateProfileString(Section, Key, string.Empty, temp, 255, path);
            return temp.ToString();
        }

        #region CRUD 

        public List<T> selectByQuery<T>(string query) where T : new()
        {
            lock (mTransactionLock)
            {
                DataSet dataSet = SelectByQuery(query);
                if (dataSet == null)
                    return default(List<T>);

                List<T> selectList = convDataSetToInfoList<T>("Table", dataSet);
                return selectList;
            }
        }

        private DataSet SelectByQuery(string query)
        {
            DataSet dataSet = null;

            try
            {
                lock (mUsingLock)
                {

                    DbCommand command = new SqlCommand();
                    command.Connection = GetConnectionQMSDB();
                    command.Transaction = GetTransactionQMSDB();
                    command.CommandText = query;

                    DbDataAdapter adapter = GetDataAdapterQMSDB(command);

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



        private List<T> convDataSetToInfoList<T>(string tableName, DataSet dataSet) where T : new()
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
                else properties[idx].SetValue(obj, null, null);
            }
        }
        #endregion

        #region GetQMS Data Query
        public void GetEndData(string PlantId)
        {
            //PLAN ID 받아오고
            string endSelectQuery = string.Format("SELECT * FROM [{0}] WHERE {1} = {2}",
                DBDataMstEndT.TbName,
                DBDataMstEndT.Plant_Id,
                PlantId
              );

            List<DBDataMstEnd> dataSet = selectByQuery<DBDataMstEnd>(endSelectQuery);
        }


        public List<DBDataProductPlan> GetProductPlanData(string PlantId, string Date)
        {
            string productPlanSelectQuery = string.Format("SELECT * FROM [{0}] WHERE {1} = {2} AND {3} = {4}",
                DBDataProductPlanT.TbName,
                DBDataProductPlanT.Plant_Id,
                PlantId,
                DBDataProductPlanT.Product_Date,
                Date
                );

            List<DBDataProductPlan> dataSet = selectByQuery<DBDataProductPlan>(productPlanSelectQuery);
            return dataSet;
        }


        public List<DBDataProductPlan> GetProductPlanData(string PlantId, string Date, string LOTtemp)
        {
            string productPlanSelectQuery = string.Format("SELECT POS FROM [{0}] WHERE {1} = '{2}' AND {3} = '{4}' AND {5} = '{6}'",
                DBDataProductPlanT.TbName,
                DBDataProductPlanT.Plant_Id,
                PlantId,
                DBDataProductPlanT.Product_Date,
                Date,
                DBDataProductPlanT.Lot,
                LOTtemp
                );

            List<DBDataProductPlan> dataSet = selectByQuery<DBDataProductPlan>(productPlanSelectQuery);

            return dataSet;
        }

        public List<DBDataProductPlan> GetProductPlanData(string PlantId, string Date, string LOTtemp, string Line)
        {
            string productPlanSelectQuery = string.Format("SELECT * FROM [{0}] (nolock) " +
                "WHERE {1} = '{2}'" +
                " AND {3} = '{4}'" +
                " AND {5} = '{6}' AND {7} = '{8}'",
                DBDataProductPlanT.TbName,
                DBDataProductPlanT.Plant_Id,
                PlantId,
                DBDataProductPlanT.Product_Date,
                Date,
                DBDataProductPlanT.Lot,
                LOTtemp,
                DBDataProductPlanT.Line_Id,
                Line
                );

            List<DBDataProductPlan> dataSet = selectByQuery<DBDataProductPlan>(productPlanSelectQuery);

            return dataSet;
        }

        public DataSet GetProductPlanDataForQMS(string PlantId, string Date, string lastImportDate)
        {
            string select = string.Format("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}",
                  DBDataProductPlanT.Product_Date,
                  DBDataProductPlanT.Plant_Id,
                  DBDataProductPlanT.Line_Id,
                  DBDataProductPlanT.Pos,
                  DBDataProductPlanT.Lot,
                  DBDataProductPlanT.Lot_Seq,
                  DBDataProductPlanT.Start_end,
                  DBDataProductPlanT.Side,
                  DBDataProductPlanT.End_End,
                  DBDataProductPlanT.End_Qty,
                  DBDataProductPlanT.Inspect_End,
                  DBDataProductPlanT.Cancel_Yn,
                  DBDataProductPlanT.End_Date,
                  DBDataProductPlanT.Created_On,
                  DBDataProductPlanT.Modified_On);

            string productPlanSelectQuery = string.Format("SELECT {0} FROM [{1}] (nolock) " +
                "WHERE {2} = '{3}'",            
                //"AND PRODUCT_DATE <= '{4}'" +              
                //"AND MODIFIED_ON >= '{5}'",
                //                "WHERE {2} = '{3}'" +
                //" AND CANCEL_YN = 'N'" +
                //"AND ( ( ISNULL(END_DATE,'') = '' AND PRODUCT_DATE <= '{4}')" +
                //" OR ((ISNULL(END_DATE,'') <> '' AND PRODUCT_DATE <= '{5}') " +
                //") )" +
                //"AND MODIFIED_ON >= '{7}'",
                select,
                DBDataProductPlanT.TbName,
                DBDataProductPlanT.Plant_Id,
                PlantId
                //Date,          
                //lastImportDate
                );            

            return ReturnSingleQuery(productPlanSelectQuery);
        }

        #region 결과데이터 QMS 테이블에 올리기
        public int InsertSpinWeightResultToQMS(DataSet data)
        {
            List<QMS_SpinWeightResult> selectList = convDataSetToInfoList<QMS_SpinWeightResult>("Table", data);
            int icount = 0;
            foreach(QMS_SpinWeightResult resultdata in selectList)
            {
                if(true == InsertSpinWeightResultToQMS(0, resultdata))
                {
                    //트루로 리턴되면 QMS 시그널을 1로 바꿔주기
                    LocalDataManager localdb = LocalDataManager.getInstance();
                    localdb.UpdataSpinWeightResult(resultdata, 1);
                    icount += 1;
                }
            }  
            return icount;                 
        }
      
        private Int64 InsertOnly(string tableName, string values)
        {
            Int64 returnValue = 0;
            try
            {
                lock (mUsingLock)
                {
                    DbCommand command = new SqlCommand();
                    command.Connection = GetConnectionQMSDB();
                    command.Transaction = GetTransactionQMSDB();
               
                    string temp = string.Format("INSERT INTO {0} VALUES({1})", tableName, values);
                   
                    command.CommandText = temp;
                    Console.WriteLine(command.CommandText);
                    returnValue = Convert.ToInt64(command.ExecuteNonQuery());
                }
            }
            catch (Exception e)
            {
                e.ToString();
            }
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
            catch (Exception e)
            {
                e.ToString();
            }
            return returnValue;
        }

        #endregion
        //
        public DataSet GetSpinWeightSpec(string PlantId, string lastImportDate)
        {
            string select = string.Format("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}",
                  QMS_SpinWeightSpecT.Plant_Id,
                  QMS_SpinWeightSpecT.Lot,
                  QMS_SpinWeightSpecT.Lot_seq,
                  QMS_SpinWeightSpecT.apply_date,
                  QMS_SpinWeightSpecT.Usl,
                  QMS_SpinWeightSpecT.Sl,
                  QMS_SpinWeightSpecT.Lsl,
                  QMS_SpinWeightSpecT.Ucl,
                  QMS_SpinWeightSpecT.Cl,
                  QMS_SpinWeightSpecT.Lcl,
                  QMS_SpinWeightSpecT.Mark,
                  QMS_SpinWeightSpecT.Sl_tolerance,
                  QMS_SpinWeightSpecT.Cl_tolerance,
                  QMS_SpinWeightSpecT.Created_by,
                  QMS_SpinWeightSpecT.Created_On,
                  QMS_SpinWeightSpecT.Modified_by,
                  QMS_SpinWeightSpecT.Modified_On
                  );

            string SpinWeightspecSelectQuery = string.Format("SELECT {0} FROM [{1}] (nolock) " +
                "WHERE {2} = '{3}'",
                select,
                QMS_SpinWeightSpecT.TbName,
                QMS_SpinWeightSpecT.Plant_Id,
                PlantId                 
                );
            
            return ReturnSingleQuery(SpinWeightspecSelectQuery);
        }

        public void deleteSTableData()
        {

        }

        public void testPlan()
        {
            string inderquery = string.Format("INSERT INTO LocalWeightData.PRODUCT_PLAN(PRODUCT_DATA, PLANT_ID, LINE_ID, POS, LOT, LOT_SEQ)SELECT title, name, regdate, register, memberid, categoryid FROM TNCQMS.PRODUCT_PLAN");
            ReturnSingleQuery(inderquery);
        }
        public List<DBDataPlanTID> GetPlantId()
        {
            string getPlantId = string.Format("SELECT PLANT_ID FROM MST_PLANT");
            List<DBDataPlanTID> dataSet = selectByQuery<DBDataPlanTID>(getPlantId);
            return dataSet;
        }
        #endregion


        private DataSet ReturnSingleQuery(string Query)
        {
            DataSet dset = SelectByQuery(Query);

            return dset;
        }

        public DataSet GetLotIDData(string PlantId, string Date, string Line)
        {
            string productPlanSelectQuery = string.Format("SELECT DISTINCT LOT FROM [{0}] WHERE {1} = '{2}' AND {3} = '{4}' AND {5} = '{6}'",
               DBDataProductPlanT.TbName,
               DBDataProductPlanT.Plant_Id,
               PlantId,
               DBDataProductPlanT.Product_Date,
               Date,             
               DBDataProductPlanT.Line_Id,
               Line
               );

            return ReturnSingleQuery(productPlanSelectQuery);
        }

        public DataSet GetPOSIdData(string PlantId, string Date, string LOTtemp, string Line)
        {
            string productPlanSelectQuery = string.Format("SELECT DISTINCT POS FROM [{0}] WHERE {1} = '{2}' AND {3} = '{4}' AND {5} = '{6}' AND {7} = '{8}'",
                DBDataProductPlanT.TbName,
                DBDataProductPlanT.Plant_Id,
                PlantId,
                DBDataProductPlanT.Product_Date,
                Date,
                DBDataProductPlanT.Lot,
                LOTtemp,
                DBDataProductPlanT.Line_Id,
                Line
                );

            return ReturnSingleQuery(productPlanSelectQuery);
        }

        public DataSet GetPOS_END(string PlantId, string Date, string Pos, string Line)
        {
            string productPlanSelectQuery = string.Format("SELECT PLANT_ID, LINE_ID, POS, LOT, LOT_SEQ, START_END, END_QTY FROM [{0}] WHERE {1} = '{2}' AND {3} = '{4}' AND {5} = '{6}'" +
                "AND CANCEL_YN = 'N'",
                    DBDataProductPlanT.TbName,
                    DBDataProductPlanT.Plant_Id,
                    PlantId,
                    DBDataProductPlanT.Pos,
                    Pos,
                    DBDataProductPlanT.Line_Id,
                    Line
                    );

            return ReturnSingleQuery(productPlanSelectQuery);
        }

        public DataSet GetUserID(string PlantId)
        {
            string UserIDSelectQuery = string.Format("SELECT {0}, {1}, {2} FROM [{3}] WHERE {4} = '{5}' AND {6} = 'Y'",                
                    DBDataMstUserT.Plant_Id,
                    DBDataMstUserT.User_Id,
                    DBDataMstUserT.Use_YN,
                    DBDataMstUserT.TbName,
                    DBDataMstUserT.Plant_Id,
                    PlantId,
                    DBDataMstUserT.Use_YN                    
                    );

            return ReturnSingleQuery(UserIDSelectQuery);
        }

        public DataSet GetSideData(string PlantId, string Date, string LOTtemp, string Line, string POS)
        {
            string productPlanSelectQuery = string.Format("SELECT DISTINCT SIDE FROM [{0}] WHERE {1} = '{2}' AND {3} = '{4}' AND {5} = '{6}' AND {7} = '{8}' AND {9} = '{10}'",
                DBDataProductPlanT.TbName,
                DBDataProductPlanT.Plant_Id,
                PlantId,
                DBDataProductPlanT.Product_Date,
                Date,
                DBDataProductPlanT.Lot,
                LOTtemp,
                DBDataProductPlanT.Line_Id,
                Line,
                DBDataProductPlanT.Pos,
                POS
                );

            return ReturnSingleQuery(productPlanSelectQuery);
        }

        public bool InsertSpinWeightResultToQMS(int insert, QMS_SpinWeightResult WeightTempData)
        {
            QMS_SpinWeightResult spec = WeightTempData;

            if (spec != null)
            {
                //INSERT에서 FROM DUAL WHERE NOT EXISTS 구분할 WHERER 구문을 만들어 봄
                string where = string.Format("{0} = '{1}' AND {2} = '{3}' AND {4} = '{5}' AND {6} = '{7}' AND {8} = '{9}' AND [{10}] = {11}",
                                          QMS_SpinWeightResultT.Plant_Id, spec.Plant_Id,
                                          QMS_SpinWeightResultT.Lot, spec.Lot,
                                          QMS_SpinWeightResultT.Lot_seq, spec.Lot_seq,
                                          QMS_SpinWeightResultT.Line_Id, spec.Line_Id,
                                          QMS_SpinWeightResultT.End_Id, spec.End_Id,
                                          QMS_SpinWeightResultT.Value, spec.Value                                       
                                         );

                string value = string.Format("'{0}','{1}','{2}','{3}','{4}','{5}',N'{6}','{7}',N'{8}','{9}','{10}','{11}','{12}'" +
                    ",'{13}','{14}','{15}','{16}','{17}','{18}','{19}','{20}','{21}','{22}'",
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
                    UtilManager.GetTimeWithMilli()                  
                    );

                if (1 == InsertOnly("SPIN_WEIGHT_RESULT", value)) return true;
            }
            return false;
        }
    }
}
