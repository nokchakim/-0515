using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace SPX_Weight
{
    public class UtilManager
    {
        public delegate void AfterCallingMethod(bool res, object[] callbackParams);

        private const string ThumbnailSuffix = "_Thumb";

        //=========================================================================================
        //=========================================================================================
        //============= 파일 영역
        //=========================================================================================
        //=========================================================================================
        //=====================================================================
        public static bool CreateEmptyFile(string path)
        {
            try
            {
                CreateDirectory(path);
                FileStream fs = File.Create(path);
                fs.Close();
            }
            catch
            {
                return false;
            }

            return true;
        }

        //=====================================================================
        public static bool CreateDirectory(string path)
        {
            try
            {
                string dirPath = Path.GetDirectoryName(path);
                if (Directory.Exists(dirPath) == false)
                    Directory.CreateDirectory(dirPath);
            }
            catch
            {
                return false;
            }

            return true;
        }

        //=====================================================================
        public static bool CopyFile(string srcFile, string destFile, bool overwrite = true)
        {
            try
            {
                if (File.Exists(srcFile))
                    File.Copy(srcFile, destFile, overwrite);
                else
                    return false;
            }
            catch (Exception e)
            {
                Console.WriteLine("CopyFile : " + e.Message);
                return false;
            }

            return true;
        }

        //=====================================================================
        public static bool CopyFileAsync(string srcFile, string destFile, AfterCallingMethod method = null, object[] callbackParams = null)
        {
            // 동작이 완료된 이후에 등록된 callback method를 호출

            new Thread(() =>
            {
                try
                {
                    if (File.Exists(srcFile))
                    {
                        File.Copy(srcFile, destFile);
                        if (method != null)
                            method(true, callbackParams);
                        return;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("CopyFileAsync : " + e.Message);
                }

                if (method != null)
                    method(false, callbackParams);

            }).Start();

            return true;
        }

        //=====================================================================
        public static bool DeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
                else
                    return false;
            }
            catch (Exception e)
            {
                Console.WriteLine("DeleteFile : " + e.Message);
                return false;
            }

            return true;
        }

        //=====================================================================
        public static bool DeleteDirectory(string path, bool recursive = false)
        {
            try
            {
                if (path == null || path == "")
                    return false;

                if (Directory.Exists(path))
                {
                    if (recursive == true)
                    {
                        DirectoryInfo pathInfo = new DirectoryInfo(path);
                        pathInfo.Delete(true);
                    }
                    else
                    {
                        Directory.Delete(path);
                    }
                }
                else
                    return false;
            }
            catch (Exception e)
            {
                Console.WriteLine("DeleteDirectory : " + e.Message);
                return false;
            }

            return true;
        }

        //=====================================================================
        public static bool RenameFile(string oldPath, string newPath)
        {
            try
            {
                if (File.Exists(oldPath))
                    File.Move(oldPath, newPath);
                else
                    return false;
            }
            catch (Exception e)
            {
                Console.WriteLine("RenameFile : " + e.Message);
                return false;
            }

            return true;
        }

        //=====================================================================
        public static bool RenameDirectory(string oldPath, string newPath)
        {
            try
            {
                if (Directory.Exists(oldPath))
                    Directory.Move(oldPath, newPath);
                else
                    return false;
            }
            catch (Exception e)
            {
                Console.WriteLine("RenameDirectory : " + e.Message);
                return false;
            }

            return true;
        }

        //=====================================================================
        public static bool MoveFile(string srcFile, string destFile)
        {
            try
            {
                if (File.Exists(srcFile))
                    new FileInfo(srcFile).MoveTo(destFile);
                else
                    return false;
            }
            catch (Exception e)
            {
                Console.WriteLine("MoveFile : " + e.Message);
                return false;
            }

            return true;
        }

        //=====================================================================
        public static bool MoveFileAsync(string srcFile, string destFile, AfterCallingMethod method = null, object[] callbackParams = null)
        {
            // 동작이 완료된 이후에 등록된 callback method를 호출
            new Thread(() =>
            {
                try
                {
                    if (File.Exists(srcFile))
                    {
                        File.Move(srcFile, destFile);
                        if (method != null)
                            method(true, callbackParams);
                        return;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("MoveFileAsync : " + e.Message);
                }

                if (method != null)
                    method(false, callbackParams);

            }).Start();

            return true;
        }

        //=====================================================================
        // Root 경로를 변경
        // - Drive만 변경하고 싶은 경우 dirBlock 값을 0으로 지정
        // - 파일 명만 남기고 상위를 다 변경하고 싶은경우 dirBlock 값을 -1 로 지정
        // - dirBlock이 양수이면 앞에서부터 없앰
        //   e.g. C:\\1\\2\\3\\4\\5 and newRootPath=D:\\
        //        dirBlock = 0 = D:\\1\\2\\3\\4\\5
        //        dirBlock = 1 = D:\\2\\3\\4\\5
        //        dirBlock = 2 = D:\\3\\4\\5
        // - dirBlock이 음수이면 뒤에서부터 없앰
        //   e.g. C:\\1\\2\\3\\4\\5 and newRootPath=D:\\
        //        dirBlock = -1 = D:\\5
        //        dirBlock = -2 = D:\\4\\5
        //        dirBlock = -3 = D:\\3\\4\\5
        //=====================================================================
        public static string ModifyFileRootPath(string path, int dirBlock, string newRootPath)
        {
            if (path == null || path == "" || newRootPath == null || newRootPath == "")
                return "";

            path        = path.Replace("/", "\\");
            newRootPath = newRootPath.Replace("/", "\\");

            string[] dirCascade = path.Split('\\');

            if (dirBlock < 0)
            {
                dirBlock = dirCascade.Length + dirBlock - 1;
                if (dirBlock < 0)
                    dirBlock = 0;
            }

            string newPath = newRootPath;
            for (int i = dirBlock + 1; i < dirCascade.Length; ++i)
                newPath = string.Format("{0}\\{1}", newPath, dirCascade[i]);

            newPath = newPath.Replace("\\\\", "\\");

            return newPath;
        }

        //=====================================================================
        public static bool IsFileExist(string path)
        {
            try
            {
                if (File.Exists(path))
                    return true;
                else
                    return false;
            }
            catch (Exception e)
            {
                Console.WriteLine("IsFileExist : " + e.Message);
                return false;
            }
        }

        //=====================================================================
        public static bool IsDirectoryExist(string path)
        {
            try
            {
                if (Directory.Exists(path))
                    return true;
                else
                    return false;
            }
            catch (Exception e)
            {
                Console.WriteLine("IsDirectoryExist : " + e.Message);
                return false;
            }
        }

        //=====================================================================
        public static string FindFile(string path, string keyword)
        {
            try
            {
                if (path == null || path == "")
                    return "";

                if (Directory.Exists(path))
                {
                    foreach (string file in Directory.GetFiles(path, keyword))
                        return file;
                }
                else
                    return "";
            }
            catch (Exception e)
            {
                Console.WriteLine("FindFile : " + e.Message);
                return "";
            }

            return "";
        }

        //=====================================================================
        // depth 값이 높을수록 상위 폴더를 반환
        // e.g. C:/TEMP/IMG/1.jpg
        //      depth = 1 -> C:/TEMP/IMG/
        //      depth = 2 -> C:/TEMP/
        //=====================================================================
        public static string GetDirectoryPath(string path, int depth = 1)
        {
            string subPath = "";
            try
            {
                if (path == null || path == "")
                    return "";

                path = path.Replace("/", "\\");

                string dir = Path.GetDirectoryName(path);
                string[] dirCascade = dir.Split('\\');

                int dirDepth = dirCascade.Length;
                if (dirDepth >= depth && depth >= 1)
                {
                    for (int idx = 0; idx <= dirDepth - depth; ++idx)
                        subPath = string.Format("{0}{1}\\", subPath, dirCascade[idx]);
                }
                
                return subPath;
            }
            catch (Exception e)
            {
                Console.WriteLine("GetDirectoryPath : " + e.Message);
                return subPath;
            }
        }

        //=====================================================================
        public static DriveInfo GetDriveInfo(string driveName)
        {
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady && drive.Name == driveName)
                {
                    return drive;
                }
            }
            return default(DriveInfo);
        }

        //=====================================================================
        public static string GetDriveLetter(string path)
        {
            if (path != null && path.Length > 0)
                return string.Format("{0}:\\", path[0]);
            return "";
        }

        //=====================================================================
        public static string AttachPrefix(string path, string prefix)
        {
            if (path == null || path == "" || prefix == null || prefix == "")
                return "";

            path = path.Replace("/", "\\");

            string[] dirCascade = path.Split('\\');

            string newPath = "";
            for (int i = 0; i < dirCascade.Length - 1; ++i)
                newPath = string.Format("{0}{1}\\", newPath, dirCascade[i]);

            newPath = string.Format("{0}{1}{2}", newPath, prefix, dirCascade[dirCascade.Length - 1]);

            return newPath;
        }

        //=====================================================================
        public static string AttachSuffix(string path, string suffix)
        {
            if (path == null || path == "" || suffix == null || suffix == "")
                return "";

            path = path.Replace("/", "\\");

            string[] dirCascade = path.Split('\\');

            string newPath = "";
            for (int i = 0; i < dirCascade.Length - 1; ++i)
                newPath = string.Format("{0}{1}\\", newPath, dirCascade[i]);

            string ext = GetExtension(path);
            if (ext != null && ext != "")
                newPath = string.Format("{0}{1}{2}{3}", newPath, Path.GetFileNameWithoutExtension(path), suffix, ext);
            else
                newPath = string.Format("{0}{1}{2}", newPath, dirCascade[dirCascade.Length - 1], suffix);

            return newPath;
        }

        //=====================================================================
        public static string GetFileName(string path)
        {
            return Path.GetFileName(path);
        }

        //=====================================================================
        public static string GetExtension(string path)
        {
            return Path.GetExtension(path);
        }

        //=====================================================================
        public static string GetThumbnailPath(string path)
        {
            return UtilManager.AttachSuffix(path, ThumbnailSuffix);
        }

        //=====================================================================
        public static void CreateThumbnail(string path)
        {
            UtilManager.ResizeImage(path, GetThumbnailPath(path), 250, 250, true);
        }

        //=========================================================================================
        //=========================================================================================
        //============= String 영역
        //=========================================================================================
        //=========================================================================================
        //=====================================================================
        public static string ExtractString(string source, string start, string end, string defValue = "")
        {
            try
            {
                int startIdx = (start != "") ? source.IndexOf(start) : 0;
                int endIdx = (end != "") ? source.IndexOf(end) : source.Length;
                int offset = start.Length;
                string valueString = source.Substring(startIdx + offset, endIdx - (startIdx + offset));
                return valueString;
            }
            catch { }
            return defValue;
        }

        //=====================================================================
        public static int ExtractInt(string source, string start, string end, int defValue = 0)
        {
            try
            {
                return int.Parse(ExtractString(source, start, end));
            }
            catch { }
            return defValue;
        }

        //=====================================================================
        public static double ExtractDouble(string source, string start, string end, double defValue = 0)
        {
            try
            {
                return double.Parse(ExtractString(source, start, end));
            }
            catch { }
            return defValue;
        }

        //=====================================================================
        public static bool IsValidEmailAddress(string s)
        {
            return new System.Text.RegularExpressions.Regex(@"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$").IsMatch(s);
        }

        //=====================================================================
        public static bool IsValidId(string s)
        {
            return new System.Text.RegularExpressions.Regex(@"^[a-zA-Z0-9\-_.]{4,20}$").IsMatch(s);
        }

        //=====================================================================
        public static bool IsValidPassword(string s)
        {
            bool rtnVal = false;

            if(new System.Text.RegularExpressions.Regex(@"^[a-zA-Z0-9]{8,16}$").IsMatch(s))
            {
                bool numCheck;
                bool engCheck;

                engCheck = new System.Text.RegularExpressions.Regex(@"^[a-zA-Z]{8,16}$").IsMatch(s);
                numCheck = new System.Text.RegularExpressions.Regex(@"^[0-9]{8,16}$").IsMatch(s);

                if (engCheck == true || numCheck == true)
                {
                    rtnVal = false;
                }
                else
                {
                    rtnVal =  true;
                }
            }
            else
            {
                rtnVal = false;
            }

            return rtnVal;
        }

        //=====================================================================
        public static bool IsValidName(string s)
        {
            return new System.Text.RegularExpressions.Regex(@"^[a-zA-Z가-힣\u4e00-\u9fff]{2,30}$").IsMatch(s);
        }

        //=====================================================================
        public static bool IsValidEmpNo(string s)
        {
            return new System.Text.RegularExpressions.Regex(@"^[a-zA-Z가-힣\u4e00-\u9fff0-9\-_.]{5,20}$").IsMatch(s);
        }

        //=====================================================================
        public static bool IsValidDepart(string s)
        {
            return new System.Text.RegularExpressions.Regex(@"^[a-zA-Z가-힣\u4e00-\u9fff]{2,15}$").IsMatch(s);
        }

        //=====================================================================
        public static bool IsValidTelNo(string s)
        {
            return new System.Text.RegularExpressions.Regex(@"^[0-9\-]{7,20}$").IsMatch(s);
        }

        //=====================================================================
        public static bool IsValidOnlyNumber(string s)
        {
            return new System.Text.RegularExpressions.Regex(@"^[0-9]$").IsMatch(s);
        }

        //=====================================================================
        public static bool IsValidFirstEngAfterNumber(string s)
        {
            return new System.Text.RegularExpressions.Regex(@"^[a-zA-Z][0-9]{0,3}$").IsMatch(s);
        }

        //=====================================================================
        public static bool IsValidFirstSyapAfterNumber(string s)
        {
            return new System.Text.RegularExpressions.Regex(@"^[#][0-9]{0,2}$").IsMatch(s);
        }
        
        //=====================================================================
        public static string FindResource(string resName)
        {
            try
            {
                return (string)System.Windows.Application.Current.FindResource(resName);
            }
            catch 
            {
                LogManager.getInstance().writeLog("Couldn't find resource name of " + resName);
            }

            return "";
        }

        //=========================================================================================
        //=========================================================================================
        //============= 시간 영역
        //=========================================================================================
        //=========================================================================================
        //=====================================================================
        public static string GetTime()
        {
            return GetTime(DateTime.Now);
        }

        //=====================================================================
        public static string GetTime(DateTime sourceDateTime)
        {
            return sourceDateTime.ToString("yyyy-MM-dd HH:mm:ss");
        }

        //=====================================================================
        public static string GetTimeWithMilli()
        {
            return GetTimeWithMilli(DateTime.Now);
        }

        //=====================================================================
        public static string GetTimeWithMilli(DateTime sourceDateTime)
        {
            return sourceDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }

        //=========================================================================================
        //=========================================================================================
        //============= 데이터 변환(bytes <-> type)
        //=========================================================================================
        //=========================================================================================
        public static Byte[] ushortToByte(ushort number)
        {
            byte[] intBytes = BitConverter.GetBytes(number);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(intBytes);
            }
            return intBytes;
        }

        public static Byte[] shortToByte(short number)
        {
            byte[] intBytes = BitConverter.GetBytes(number);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(intBytes);
            }
            return intBytes;
        }


        public static Byte[] IntToByte(int number)
        {
            byte[] intBytes = BitConverter.GetBytes(number);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(intBytes);
            }
            return intBytes;
        }

        public static Byte[] longToByte(long number)
        {
            byte[] intBytes = BitConverter.GetBytes(number);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(intBytes);
            }
            return intBytes;
        }

        public static Byte[] ByteToByte(byte[] bytes)
        {
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return bytes;
        }

        public static string ByteToHexString(byte[] bytes)
        {
            //string hex = BitConverter.ToString(bytes); // AB-CD-EF-01
            //return hex.Replace("-", ""); // ABCDEF01
            return string.Concat(bytes.Select(b => b.ToString("X2")));
        }

        public static bool isNumber(string s)
        {
            long i = 0;
            bool result = long.TryParse(s, out i);
            return result;
        }

        //=========================================================================================
        //=========================================================================================
        //============= 이미지 영역
        //=========================================================================================
        //=========================================================================================
        //=====================================================================
        public static bool ResizeImage(string imageFile, string outputFile, double width, double height, bool keepAspectRatio = false)
        {
            try
            {
                if (IsFileExist(imageFile) == false)
                    return false;

                BitmapSource source = new BitmapImage(new Uri(imageFile));

                double scaleW = width / source.PixelWidth;
                double scaleH = height / source.PixelHeight;

                if (keepAspectRatio)
                {
                    double bRatio = (double)source.PixelWidth / source.PixelHeight;
                    scaleH = (width / bRatio) / source.PixelHeight;
                }
                TransformedBitmap bitmap = new TransformedBitmap(source, new ScaleTransform(scaleW, scaleH));

                BitmapEncoder encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));

                using (var fileStream = new System.IO.FileStream(outputFile, System.IO.FileMode.Create))
                {
                    encoder.Save(fileStream);
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        //=========================================================================================
        //=========================================================================================
        //============= 네트워크 영역
        //=========================================================================================
        //=========================================================================================
        //=========================================================================
        public static string GetLocalIPAddress()
        {
            try
            {
                using (System.Net.Sockets.Socket socket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 65530);
                    System.Net.IPEndPoint endPoint = socket.LocalEndPoint as System.Net.IPEndPoint;
                    if (endPoint != null && endPoint.Address != null)
                        return endPoint.Address.ToString();
                }
            }
            catch (Exception e)
            {
                LogManager.getInstance().writeLog("GetLocalIPAddress() - " + e.Message);
            }

            return "127.0.0.1"; // "127.0.0.1";
        }

        //=========================================================================================
        //=========================================================================================
        //============= 암호화 영역
        //=========================================================================================
        //=========================================================================================
        //=====================================================================
        private const string initVector = "pemgail9uzpgzl88";
        private const int    keysize = 256;
        private const string passPhrase = "_dhqxlakbns_";
        //=====================================================================
        public static string EncryptString(string plainText)
        {
            byte[] initVectorBytes = Encoding.UTF8.GetBytes(initVector);
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            PasswordDeriveBytes password = new PasswordDeriveBytes(passPhrase, null);
            byte[] keyBytes = password.GetBytes(keysize / 8);
            RijndaelManaged symmetricKey = new RijndaelManaged();
            symmetricKey.Mode = CipherMode.CBC;
            ICryptoTransform encryptor = symmetricKey.CreateEncryptor(keyBytes, initVectorBytes);
            MemoryStream memoryStream = new MemoryStream();
            CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
            cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
            cryptoStream.FlushFinalBlock();
            byte[] cipherTextBytes = memoryStream.ToArray();
            memoryStream.Close();
            cryptoStream.Close();
            return Convert.ToBase64String(cipherTextBytes);
        }

        //=====================================================================
        public static string DecryptString(string cipherText)
        {
            byte[] initVectorBytes = Encoding.UTF8.GetBytes(initVector);
            byte[] cipherTextBytes = Convert.FromBase64String(cipherText);
            PasswordDeriveBytes password = new PasswordDeriveBytes(passPhrase, null);
            byte[] keyBytes = password.GetBytes(keysize / 8);
            RijndaelManaged symmetricKey = new RijndaelManaged();
            symmetricKey.Mode = CipherMode.CBC;
            ICryptoTransform decryptor = symmetricKey.CreateDecryptor(keyBytes, initVectorBytes);
            MemoryStream memoryStream = new MemoryStream(cipherTextBytes);
            CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
            byte[] plainTextBytes = new byte[cipherTextBytes.Length];
            int decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
            memoryStream.Close();
            cryptoStream.Close();
            return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
        }

        //=========================================================================================
        //=========================================================================================
        //============= 시스템 영역
        //=========================================================================================
        //=========================================================================================
        //=====================================================================
        public static void ExecuteApplication(string exeFileName, string argument = "")
        {
            argument = string.Format("\"{0}\"", argument);
            System.Diagnostics.ProcessStartInfo Info = new System.Diagnostics.ProcessStartInfo()
            {
                FileName    = exeFileName,
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Maximized,
                Arguments   = argument
            };
            System.Diagnostics.Process.Start(Info);
        }

        //=====================================================================
        public static void ExecuteFile(string filePath)
        {
            System.Diagnostics.Process.Start(filePath);
        }
    }
}
