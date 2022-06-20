using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Security.Cryptography;

namespace SPX_Weight
{

    public struct setConfig
    {
        public string Data;

        public setConfig(string data)
        {
            Data = data;
        }
    }

    public class LogManager
    {
        private static LogManager mInstance;
        private static Object mInstanceLock = new Object();
        private static Object mLogLock      = new Object();
        string mLogDirectories;
        string mLogDate;
        int mToday;

        public static string log_PATH = "";
        public static string server_IP = "";
        public static int server_PORT = 0;
		public static string py_PROCESS_p = "";
		public static string py_PROCESS_nm = "";
		public static string py_PROCESS_cmd = "";
        public static string py_PROCESS_arg = "";


        /// <summary>
        /// 여기다가 그냥 스트링 넣기 합시다 언어팩 따로 만들지 말고
        /// </summary>
        ///         
        public string viewLine = "";
        public string viewDay = "";
        public string viewLot = "";
        public string viewPOS = "";
        public string viewSide = "";
        public string viewDoff = "";
        public string viewWeight = "";
        public string viewErrorRange = "";

        public string popupLot = "";
        public string popupID = "";

        /// <summary>
        /// QMS연결 실패
        /// </summary>
        public string popupQMSConnectFail = "";

        /// <summary>
        /// 설정 파일을 저장 하시겠습니까
        /// </summary>
        public string PopSaveConfig = "";

        /// <summary>
        /// 파일이 이미 열려있습니다. 
        /// </summary>
        public string PopExistFileOpen = "";

        /// <summary>
        /// 파일이 이미 있습니다 덮어 쓰시겠습니까?
        /// </summary>
        public string PopFileExistSave = "";

        /// <summary>
        /// LOT 데이터가 없습니다.  
        /// </summary>
        public string PopNoExistLot = "";

        /// <summary>
        /// LOT 데이터를 확인해주세요 
        /// </summary>
        public string PopErrorLot = "";

        /// <summary>
        /// LINE 데이터를 확인해주세요 
        /// </summary>
        public string PopErrorLine = "";

        /// <summary>
        /// QMS 연결 되지 않음
        /// </summary>
        public string PopQmsDisconnected = "";

        /// <summary>
        /// 현재 정보로 저장 하시겠습니까
        /// </summary>
        public string PopSaveNow = "";

        /// <summary>
        /// 통신 상태 리셋 진행 
        /// </summary>
        public string PopSerialThreadReset = "연결 상태를 재 설정 합니다. 다시 시작버튼을 눌러주세요";


        protected LogManager()
        {
            server_info_Open();
            makeLogDirectory();
        }

        //=====================================================================
        public static LogManager getInstance()
        {
            if (mInstance == null)
            {
                lock (mInstanceLock)
                {
                    if (mInstance == null)
                        mInstance = new LogManager();
                }
            }

            return mInstance;
        }

        //=====================================================================
        public void writeLog(String text, int queryOn = 0, int debug = 1)
        {
            lock (mLogLock)
            {
                string objName    = "";
                string callerName = "";
                if (debug == 0) return;
                try
                {
                    try
                    {
                        System.Diagnostics.StackFrame stack = new System.Diagnostics.StackFrame(1);
                        objName    = stack.GetMethod().DeclaringType.Name;
                        callerName = stack.GetMethod().Name;
                    }
                    catch { }

                    writeConsole(objName, callerName, text);
                    string logDateTime = checkDateTime();
                    string srcFile = string.Format("{0}LOG_{1}.txt", mLogDirectories, mLogDate);

                    if (1 == queryOn)
                    {
                        srcFile = string.Format("{0}LOG_{1}_Query.txt", mLogDirectories, mLogDate);
                    }
                    
                    System.IO.FileInfo info = new System.IO.FileInfo(srcFile);
                    System.IO.StreamWriter writer = File.Exists(srcFile) ? info.AppendText() : info.CreateText();
                    writer.WriteLine(string.Format("{0} [{1}] [{2}] {3}", logDateTime, objName, callerName, text));
                    writer.Flush();
                    writer.Close();
                }
                catch (Exception e)
                {
                    writeConsole(objName, callerName, e.Message);
                }
            }
        }
        public void writeLog_ping(String text)
        {
            lock (mLogLock)
            {
                string objName = "";
                string callerName = "";

                try
                {
                    try
                    {
                        System.Diagnostics.StackFrame stack = new System.Diagnostics.StackFrame(1);
                        objName = stack.GetMethod().DeclaringType.Name;
                        callerName = stack.GetMethod().Name;
                    }
                    catch { }

                    writeConsole(objName, callerName, text);
                    string logDateTime = checkDateTime();
                    string srcFile = string.Format("{0}LOG_{1}_ping.txt", mLogDirectories, mLogDate);
                    System.IO.FileInfo info = new System.IO.FileInfo(srcFile);
                    System.IO.StreamWriter writer = File.Exists(srcFile) ? info.AppendText() : info.CreateText();
                    writer.WriteLine(string.Format("{0} [{1}] [{2}] {3}", logDateTime, objName, callerName, text));
                    writer.Flush();
                    writer.Close();
                }
                catch (Exception e)
                {
                    writeConsole(objName, callerName, e.Message);
                }
            }
        }

        //=====================================================================
        public void writeConsole(String text)
        {
            string objName    = "";
            string callerName = "";

            try
            {
                System.Diagnostics.StackFrame stack = new System.Diagnostics.StackFrame(1);
                objName    = stack.GetMethod().DeclaringType.Name;
                callerName = stack.GetMethod().Name;
            }
            catch { }

            writeConsole(objName, callerName, text);
        }

        //=====================================================================
        public void writeConsole(String objName, String callerName, String text)
        {
            try
            {
                Console.WriteLine(string.Format("[{0}] [{1}] {2}", objName, callerName, text));
            }
            catch (Exception e)
            {
                // it's somthing very wrong
                string message = e.Message;
            }
        }

        //=====================================================================
        private void makeLogDirectory()
        {
            DateTime dt = DateTime.Now;

            string year = dt.ToString("yyyy");
            string mm   = dt.ToString("MM");
            string dd   = dt.ToString("dd");
            StringBuilder path1 = new StringBuilder();
            path1.Append(log_PATH);
            path1.Append(year + "_" + mm);
            path1.Append("\\");
            if (!Directory.Exists(path1.ToString()))
            {
                try
                {
                    Directory.CreateDirectory(path1.ToString());
                }
                catch
                {

                }
            }
            mLogDirectories = path1.ToString();
            mLogDate = string.Format("{0}_{1}_{2}", year, mm, dd);
            mToday = dt.Day;
        }

        //=====================================================================
        private string checkDateTime()
        {
            DateTime dt = DateTime.Now;

            if (mToday != dt.Day)
                makeLogDirectory();

            return string.Format("[{0:D4}/{1:D2}/{2:D2} {3:D2}:{4:D2}:{5:D2}.{6:D3}]",
                                  dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Millisecond);
        }

        List<setConfig> set_list = new List<setConfig>();
        public void server_info_Open()
        {
            Byte[] dummyUniByte = new Byte[8192];
            Array.Clear(dummyUniByte, 0, dummyUniByte.Length);
            string s1 = "";
            //byte[] buffer = new byte[128];
            //FileStream fs = new FileStream(System.Windows.Forms.Application.StartupPath + @".\dbconn.ini", FileMode.Open, FileAccess.Read);
            try
            {
                FileStream fs = new FileStream(System.IO.Directory.GetCurrentDirectory() + @".\config.ini", FileMode.Open, FileAccess.Read);
            fs.Read(dummyUniByte, 0, dummyUniByte.Length);
            fs.Close();
                s1 = Encoding.UTF8.GetString(dummyUniByte).Trim('\0');
            }
            catch (FileNotFoundException)
            {             
                FileStream fs = new FileStream(System.IO.Directory.GetCurrentDirectory() + @".\config.ini", FileMode.Create, FileAccess.Write);
                StreamWriter sw = new StreamWriter(fs);
                sw.WriteLine(s1);
                sw.Close();
                fs.Close();
            }

            //pubkey();

            //string[] s9 = s1.Split('\r');
            string[] s5 = s1.Split(new string[] { "\r\n" }, StringSplitOptions.None);

            set_list.Clear();
            for (int i = 0; i < s5.Length; i++)
            {
                Debug.WriteLine(s5[i].ToString());
                set_list.Add(new setConfig(
                    s5[i].ToString()
                ));

            }
            for (int i = 0; i < set_list.Count; i++)
            {
                setConfig t1 = set_list[i];
                string s9 = t1.Data.Trim();
                if (s9.IndexOf("#", 0) != 0)
                {
                    string[] s10 = s9.Split(new string[] { "$" }, StringSplitOptions.None);
                    if (s10.Length == 2)
                    {                     
                        if (s10[0].Trim().ToLower() == "db_qms_id")
                            DataManager.QMSDataManager.db_qms_ID = s10[1].Trim();
                        else if (s10[0].Trim().ToLower() == "db_qms_passwd")
                            DataManager.QMSDataManager.db_qms_PWD = s10[1].Trim();
                        else if (s10[0].Trim().ToLower() == "db_qms_dbname")
                            DataManager.QMSDataManager.db_qms_DB = s10[1].Trim();
                        else if (s10[0].Trim().ToLower() == "db_qms_ip")
                            DataManager.QMSDataManager.db_qms_IP = s10[1].Trim();
                        else if (s10[0].Trim().ToLower() == "db_qms_port")
                            DataManager.QMSDataManager.db_qms_PORT = s10[1].Trim();                
                    }
                }
            }

        //    DataManager.QMSDataManager.db_PWD = RSADecrypt(DataManager.QMSDataManager.db_PWD, privateKey1);
         //   DataManager.QMSDataManager.db_qms_PWD = RSADecrypt(DataManager.QMSDataManager.db_qms_PWD, privateKey1);
            
        }

        //이하 암호화
        //----------------------------------------------------------------------
        //----------------------------------------------------------------------
        //----------------------------------------------------------------------
        //----------------------------------------------------------------------
        //------------------------------------------------------------------------
        // 암호화 키
        //csi@
        private const string desKey_my = "iPn0FTCsdM8DJNILwHB+6VaFUQ7qq0eF3++z7tqruROt9RE4CP0rezHPdG1j8LyU9vahtt9EYh5NGdZfaLBU92zFWFEwsCitnO994mMOjwjFKRe1KO6PtbrWI7AqC6Fe3Ph3ky3vE9iAKo7MFUCmhM9Wu4qOyjtJimWN30pz0lI=";
        //tncqms20n@
        private const string desKey_ms = "RNZxQYYwzvna8CeLkI6TX6ON70LwSR2dqzCm7neEMfpYpOYhTc+bhUzN8XnkVTiwB0D/ZFUnQu92acba2lcQWz+19yy2WSFOQ7NGG6XUXrSoCCOOmjlJ0UKqr8BpItNFs0DLbU72ETXMfVSTGf4zNeQoGHuTH0IHzL+ynccvN+w=";
        private static string privateKey1 = "<RSAKeyValue><Modulus>vFtQM53hjqoPOEfD28ZTA2/wSGC6xHsU0uBZe/qiUhkRVbUakOH0t9vTUEBOoYffuHk28dFVZ/sVrXxJ3chwedqKHAPa40UhEUhqzLpHgXyyEuOUA8sXO/w/AmV2E/wLH9eYO54xw7zBdvhtE92mdLsD4X4oTWAmaMoyTyBLqDU=</Modulus><Exponent>AQAB</Exponent><P>9pISdYYIUuZKWQfDdXuiYVCDFGupRP7rgukFzMt7pfaASzG7pQ9F8f6VOqXbOP1YDYvEm65Usbm7M58RhMmN4w==</P><Q>w49ToFT9GXtW9eeio0gk92RpFomx3ExxRhbl6fGpl/6SWmJec0qsaUQ2rGncVtsVh76R9Th/wm459C0ZP07NBw==</Q><DP>6qmyb68UFPGfKIQ+/Xyg2cTqO3ELM+L4+SoUnwe5sgWbq/S1BS43/0uvcpWOwfo65wlyIEgVyt9czpBA+ANqyQ==</DP><DQ>qMjnsJYp7PhbUdoesTbvUObFHMKzVCRWD9xri8McUSdTQddtFaz5qdFKLv0fQ4fLyWFdsHyXKETimDDkfZORuQ==</DQ><InverseQ>jICuyxby4BMEVAQc1kookSN0nILumxnQYh56zeXP1MqHo8cZZcGJOkn764JpCz6AycAdKLzZOJX2xKq1uBuB/Q==</InverseQ><D>fQ7/tBW6YdelnU+AyhXmnhyfY97dgoDZ9Z1BrKBfT7UXHlnNVq6/padNqTXZP0SQlNHeWjYLx6sc3H/uJ1Pi4M9b1pc70BcOHBGADkdkx50HgsMHjKKYGwzScYP2g4Uv7PjtZziPb5SU3o3798Is7e/tPkGCu/+shSgpOzEAWu0=</D></RSAKeyValue>";
        private string publicKey1 = "<RSAKeyValue><Modulus>vFtQM53hjqoPOEfD28ZTA2/wSGC6xHsU0uBZe/qiUhkRVbUakOH0t9vTUEBOoYffuHk28dFVZ/sVrXxJ3chwedqKHAPa40UhEUhqzLpHgXyyEuOUA8sXO/w/AmV2E/wLH9eYO54xw7zBdvhtE92mdLsD4X4oTWAmaMoyTyBLqDU=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";
            
        public void pubkey()
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();

            // 개인키 생성(복호화용)
            RSAParameters privateKey = RSA.Create().ExportParameters(true);
            rsa.ImportParameters(privateKey);
            string privateKeyText = rsa.ToXmlString(true);
            Debug.WriteLine(privateKeyText);

            // 공개키 생성(암호화용)
            RSAParameters publicKey = new RSAParameters();
            publicKey.Modulus = privateKey.Modulus;
            publicKey.Exponent = privateKey.Exponent;
            rsa.ImportParameters(publicKey);
            string publicKeyText = rsa.ToXmlString(false);
            Debug.WriteLine(publicKeyText);

            string encodedString = RSAEncrypt("csi@", publicKeyText);
            Debug.WriteLine(encodedString);
            string decodedString = RSADecrypt(encodedString, privateKeyText);
            Debug.WriteLine(decodedString);

            encodedString = RSAEncrypt("tncqms20n@", publicKeyText);
            Debug.WriteLine(encodedString);
            decodedString = RSADecrypt(encodedString, privateKeyText);
            Debug.WriteLine(decodedString);
        }

        // RSA 암호화
        public string RSAEncrypt(string getValue, string pubKey)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(pubKey);

            //암호화할 문자열을 UFT8인코딩
            byte[] inbuf = (new UTF8Encoding()).GetBytes(getValue);

            //암호화
            byte[] encbuf = rsa.Encrypt(inbuf, false);

            //암호화된 문자열 Base64인코딩
            return System.Convert.ToBase64String(encbuf);
        }

        // RSA 복호화
        public string RSADecrypt(string getValue, string priKey)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(priKey);

            //sValue문자열을 바이트배열로 변환
            byte[] srcbuf = System.Convert.FromBase64String(getValue);

            //바이트배열 복호화
            byte[] decbuf = rsa.Decrypt(srcbuf, false);

            //복호화 바이트배열을 문자열로 변환
            string sDec = (new UTF8Encoding()).GetString(decbuf, 0, decbuf.Length);
            return sDec;
        }
        public void SetLanguage()
        {
            viewLine = "#Line";
            viewDay = "DAY";
            viewLot = "LOT";
            viewPOS = "POS";
            viewSide = "SIDE";
            viewDoff = "DOFF";
            viewWeight = "Weight";
            viewErrorRange = "ErrorRange";                  
            popupQMSConnectFail = "QMS DB Connect Fail";
            popupLot = "Check Lot Data";
            popupID = "Check ID Data";
            PopSaveConfig = "do you want save config ?";
            PopExistFileOpen = "File is already open";
            PopFileExistSave = "File exists Do you want to Overwrite";
            PopNoExistLot = "No LOT data found";
            PopErrorLot = "No LOT data found";
            PopErrorLine = "No LINE data found";
            PopQmsDisconnected = "QMS DisConnected";
            PopSaveNow = "Do you want save File?";
        }

        public void SettingLanguage(string country)
        {
            string iniFileFullPath = System.IO.Directory.GetCurrentDirectory() + "\\Language.ini";
            try
            {
                if (System.IO.File.Exists(iniFileFullPath))
                {                    
                    viewLine = GetIniValue(iniFileFullPath, country, "viewLine");
                    viewDay = GetIniValue(iniFileFullPath, country, "viewDay");
                    viewLot = GetIniValue(iniFileFullPath, country, "viewLot");
                    viewPOS = GetIniValue(iniFileFullPath, country, "viewPOS");
                    viewSide = GetIniValue(iniFileFullPath, country, "viewSide");
                    viewDoff = GetIniValue(iniFileFullPath, country, "viewDoff");
                    viewWeight = GetIniValue(iniFileFullPath, country, "viewWeight");
                    viewErrorRange = GetIniValue(iniFileFullPath, country, "viewErrorRange");
                    popupQMSConnectFail = GetIniValue(iniFileFullPath, country, "popupQMSConnectFail");
                    popupLot = GetIniValue(iniFileFullPath, country, "popupLot");
                    popupID = GetIniValue(iniFileFullPath, country, "popupID");
                    PopSaveConfig = GetIniValue(iniFileFullPath, country, "PopSaveConfig");
                    PopExistFileOpen = GetIniValue(iniFileFullPath, country, "PopExistFileOpen");
                    PopFileExistSave = GetIniValue(iniFileFullPath, country, "PopFileExistSave");
                    PopNoExistLot = GetIniValue(iniFileFullPath, country, "PopNoExistLot");
                    PopErrorLot = GetIniValue(iniFileFullPath, country, "PopErrorLot");
                    PopErrorLine = GetIniValue(iniFileFullPath, country, "PopErrorLine");
                    PopQmsDisconnected = GetIniValue(iniFileFullPath, country, "PopQmsDisconnected");
                    PopSaveNow = GetIniValue(iniFileFullPath, country, "PopSaveNow");
                }
            }
            catch(Exception ex)
            {

            }
        }
        public static string RSADecrypt(string getValue)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(privateKey1);

            //sValue문자열을 바이트배열로 변환
            byte[] srcbuf = System.Convert.FromBase64String(getValue);

            //바이트배열 복호화
            byte[] decbuf = rsa.Decrypt(srcbuf, false);

            //복호화 바이트배열을 문자열로 변환
            string sDec = (new UTF8Encoding()).GetString(decbuf, 0, decbuf.Length);
            return sDec;
        }

        [System.Runtime.InteropServices.DllImport("kernel32")]
        public static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);


        public static String GetIniValue(string path, String Section, String Key)
        {
            StringBuilder temp = new StringBuilder();
            int i = GetPrivateProfileString(Section, Key, string.Empty, temp, 255, path);
            return temp.ToString();
        }
    }
}
