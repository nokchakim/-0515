using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace SPX_Weight.Common
{
    public class SerialComm
    {
        public delegate void DataReceivedHandlerFunc(byte[] receiveData, int comnum);
        public DataReceivedHandlerFunc DataReceivedHandler;

        public delegate void DisconnectedHandlerFunc();
        public DisconnectedHandlerFunc DisconnectedHandler;
        public string serialData { get; set; }
        public int portNum;
        private SerialPort serialPort;
        private static Object mSerialLock = new Object();
        // Queue<byte> receviDataQueue = new Queue<byte>();

        public bool IsOpen
        {
            get
            {
                if (serialPort != null) return serialPort.IsOpen;
                return false;
            }
        }

        // serial port check
        private Thread threadCheckSerialOpen;
        private bool isThreadCheckSerialOpen = false;

        public SerialComm()
        {
           // serialPort = new SerialPort();
        }

        public bool OpenComm(int portName, int baudrate, int databits, StopBits stopbits, Parity parity, Handshake handshake)
        {
            try
            {
                serialPort = new SerialPort();
                portNum = portName;
                string strPort = string.Format("COM{0}", portName);
                serialPort.PortName = strPort;
                serialPort.BaudRate = baudrate;
                serialPort.DataBits = databits;
                serialPort.StopBits = stopbits;
                serialPort.Parity = parity;
               // serialPort.Handshake = handshake;

                serialPort.Encoding = new System.Text.ASCIIEncoding();
                serialPort.NewLine = "\r\n";
                serialPort.ErrorReceived += serialPort_ErrorReceived;
                serialPort.DataReceived += serialPort_DataReceived;

                serialPort.Open();

              //  StartCheckSerialOpenThread();
                return true;
            }
            catch (Exception ex)
            {
                LogManager.getInstance().writeLog(ex.ToString());
                
                return false;
            }
        }

        public void CloseComm()
        {
            try
            {
                if (serialPort != null)
                {
                    StopCheckSerialOpenThread();
                    serialPort.Close();
                    serialPort = null;
                }
            }
            catch (Exception ex)
            {
                LogManager.getInstance().writeLog(ex.ToString());
            }
        }

        public bool Send(string sendData)
        {
            try
            {
                if (serialPort != null && serialPort.IsOpen)
                {
                    serialPort.Write(sendData);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
            return false;
        }

        public bool Send(byte[] sendData)
        {
            try
            {
                if (serialPort != null && serialPort.IsOpen)
                {
                    serialPort.Write(sendData, 0, sendData.Length);
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogManager.getInstance().writeLog(ex.ToString());
            }
            return false;
        }

        public bool Send(byte[] sendData, int offset, int count)
        {
            try
            {
                if (serialPort != null && serialPort.IsOpen)
                {              
                    serialPort.Write(sendData, offset, count);                   
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogManager.getInstance().writeLog(ex.ToString());
            }
            return false;
        }

        public byte[] read()
        {
            return ReadSerialByteData();
        }

        private byte[] ReadSerialByteData()
        {
            serialPort.ReadTimeout = 100;
            byte[] bytesBuffer = new byte[serialPort.BytesToRead];
            int bufferOffset = 0;
            int bytesToRead = serialPort.BytesToRead;

            while (bytesToRead > 0)
            {
                try
                {
                    lock (mSerialLock)
                    {
                        int readBytes = serialPort.Read(bytesBuffer, bufferOffset, bytesToRead - bufferOffset);
                        bytesToRead -= readBytes;
                        bufferOffset += readBytes;
                    }                   
                }
                catch (TimeoutException ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
            }

            return bytesBuffer;
        }

        private void serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            Thread.Sleep(100);
            try
            {
                byte[] bytesBuffer = ReadSerialByteData();
                //bytesBuffer.ToList().ForEach(newByte => receviDataQueue.Enqueue(newByte));
                //string strBuffer = Encoding.ASCII.GetString(bytesBuffer);
                //serialData = strBuffer;
                SerialPort port = (SerialPort)sender;
                if (DataReceivedHandler != null)
                    DataReceivedHandler(bytesBuffer, portNum);
                Array.Clear(bytesBuffer, 0, bytesBuffer.Length);
            }
            catch (Exception ex)
            {
                LogManager.getInstance().writeLog(ex.ToString());              
            }
        }

        private void serialPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            LogManager.getInstance().writeLog(e.ToString());
        }

        private void StartCheckSerialOpenThread()
        {
            isThreadCheckSerialOpen = true;
            threadCheckSerialOpen = new Thread(new ThreadStart(ThreadCheckSerialOpen));
            threadCheckSerialOpen.Start();
        }

        private void StopCheckSerialOpenThread()
        {
            if (isThreadCheckSerialOpen)
            {
                isThreadCheckSerialOpen = false;
                if (Thread.CurrentThread != threadCheckSerialOpen)
                    threadCheckSerialOpen.Join();
            }
        }

        private void ThreadCheckSerialOpen()
        {
            while (isThreadCheckSerialOpen)
            {
                Thread.Sleep(100);

                try
                {
                    if (serialPort == null || !serialPort.IsOpen)
                    {
                        Debug.WriteLine("seriaport disconnected");
                        if (DisconnectedHandler != null)
                            DisconnectedHandler();
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
            }
        }
    }
}