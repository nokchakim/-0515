using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;

using System.Windows.Forms;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows.Threading;
using System.Windows.Media;
using SPX_Weight.DataManager;
using SPX_Weight.Common;
using SPX_Weight.DataModel;
using SPX_Weight.Model;
using System.Reflection;
using System.Data;
using System.Diagnostics;

using MessageBox = System.Windows.Forms.MessageBox;
using DataGridCell = System.Windows.Controls.DataGridCell;
using DataGrid = System.Windows.Controls.DataGrid;
using System.Text.RegularExpressions;

namespace SPX_Weight
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        private BackgroundWorker mThreadDbCommunication = new BackgroundWorker();
        private BackgroundWorker mThreadSerial = new BackgroundWorker();
        private static Object mTransactionLock = new Object();

        private SerialComm[] serialData = new SerialComm[24];

        private DataTable LoTTable = new DataTable();
        private DataTable table = new DataTable();

        private ExportExcel ExcelExport = new ExportExcel();
        
        private double gUSL;
        private double gSL;
        private double gLSL;
        private double gUCL;
        private double gCL;
        private double gLCL;
        public string gMark;
        private double SL_tolerance;

        private string SerialComport;
        private int SerialSpeed;

        //작업 LOT 정보
        private string CurrentProductDate;
        private int plantid;
        private string glineid;

        private string CurrentLot;

        private int Set_Scale;
        private int runScaleCount;
        private int UseScaleCount;
        /// <summary>
        /// 시리얼 입력은 8 12 24 로 나눠져있음 ex)6개의 저울이어도 신호는 8개 들어옴
        /// </summary>
        private int inputDataCount;

        private int glot_seq;
        private int End_qty;
        private int twoway;

        private bool reverseArr;
        private bool CheckCas;
        //디버그 모드 로그 작성
        private int Debuglog = 0;
        private int DebuglogQuery = 0;

        //사이드 나눠졌는지 아닌지 확인
        private bool sideDiv = false;
        /// <summary>
        /// 0데이터없음 2데이터있음 5리셋대기
        /// </summary>
        private int banbookDatainsert;
        private int banbookDatainsert2nd;
        private bool onecyclepass;
        private bool onecyclepass2nd;
        private bool TowScale1Result;
        private bool ignorelast;

        private int currentposindex;

        private string LastImportDate;
        private string LastExportDate;

        private string setlanguage = "KR";

        private string CurrentLoginUserID = "itxtest";

        private bool bQmsdb_Connect = false;
        private bool hasNoSpec = false;

        //결과데이터 쌓을 리스트임 이거 돌려서 QMS에 업데이트 예정
        private List<QMS_SpinWeightResult> WeightResultTemp = new List<QMS_SpinWeightResult>();
        private List<string> NextStepPos = new List<string>();
        private List<string> NextStepSide = new List<string>();
        private List<string> NextStartEnd = new List<string>();
        //엑셀용
        private List<string> NextStartEndForExcel = new List<string>();
        private List<int>  StartEndCount = new List<int>();
        private List<string> LineSetParam = new List<string>();
        private List<string> glineidSet = new List<string>();
        private List<string> SingleEnd = new List<string>();
        private List<string> SingleSide = new List<string>();
        private List<string> SelectSide = new List<string>();
        private List<string> SelectStartEnd = new List<string>();

        private List<System.Windows.Controls.TextBox> listTextbox = new List<System.Windows.Controls.TextBox>();
        private int sideCount = 0;
        private int startPosCount = 0;
        private int sideDivisionexption = 1;

        private List<Double> getSerialData = new List<double>();
        private Dictionary<int, double> dicSerialData = new Dictionary<int, double>();
        private Dictionary<int, double> dicSerialData2nd = new Dictionary<int, double>();
        Dictionary<string, string> SetLotAndEnd = new Dictionary<string, string>();

        private ConcurrentDictionary<int, double> concurdicdata = new ConcurrentDictionary<int, double>();        
        private List<string> Specpanjung = new List<string>();
        private List<TextContents> WeightBox = new List<TextContents>();
        private delegate void updatadelegate(int ScaleCount);

        private readonly object qmslock = new object();
        private Thread _backgroundWorker;
        private Thread threadSendSerial;
        private bool isthreadSendSerialIdle = false;

        private byte[] receivedbuffer;
        private byte[] receivedbuffer2nd;
        public MainWindow()
        {
            InitializeComponent();
            twoway = 1;
            loadSettingIni();
            
            mThreadDbCommunication.DoWork += new DoWorkEventHandler(mThreadSendQmsDb);
            mThreadDbCommunication.RunWorkerCompleted += new RunWorkerCompletedEventHandler(mThreadSendQmsComp);

            QMSDataManager qmsdbdata = QMSDataManager.getInstance();
            LogManager log = LogManager.getInstance();
            log.server_info_Open();
            log.SettingLanguage(setlanguage);
            if ("EN" == setlanguage)
            {
                label_DAY.Content = "Day";
                label_SIDE.Content = "Side";
            }

            qmsdbdata.OpenQMSDB();

            reverseArr = false;            
            CheckCas = false;
            banbookDatainsert = 0;
            banbookDatainsert2nd = 0;
            onecyclepass = false;

            //QMS DB 연결 체크하기
            if (false == qmsdbdata.isOpen())
            {
                MessageBox.Show(log.popupQMSConnectFail);
                bQmsdb_Connect = false;
            }
            else
            {
                bQmsdb_Connect = true;
            }
            
            qmsdbdata.loadINI();
           
            LocalDataManager localdbdata = LocalDataManager.getInstance();
            localdbdata.OpenLocalDB();

            //콤보박스 바인딩
            InitDataGrid(Set_Scale);
            ProductDataPicker.SelectedDate = DateTime.Today;            

            this.LoTTable = GetTable();
            this.dataGrid.ItemsSource = this.LoTTable.DefaultView;

           // comboBox_LOT.Visibility = Visibility.Hidden;
            //이렇게 grid 를 변경할 수 있음
            
            this.TableGrid.SetValue(Grid.RowProperty, 4);
            this.TableGrid.Height = 500;
            this.dataGrid.Height = 490;
            if (12 < Set_Scale)
            {
                if (18 < Set_Scale)
                {
                    this.TableGrid.SetValue(Grid.RowProperty, 6);
                    this.TableGrid.Height = 400;
                    this.dataGrid.Height = 390;
                }
                else
                {
                    this.TableGrid.SetValue(Grid.RowProperty, 5);
                    this.TableGrid.Height = 460;
                    this.dataGrid.Height = 450;                    
                }
                Thickness gridmargin = new Thickness();
                gridmargin.Top = 0;
                gridmargin.Left = 5;
                this.dataGrid.Margin = gridmargin;
            }
            else
            {   
                if(twoway == 2)
                {
                    int tempscale = Set_Scale * 2;
                    if (18 < tempscale)
                    {
                        this.TableGrid.SetValue(Grid.RowProperty, 6);
                        this.TableGrid.Height = 400;
                        this.dataGrid.Height = 390;
                    }
                    else
                    {
                        this.TableGrid.SetValue(Grid.RowProperty, 5);
                        this.TableGrid.Height = 460;
                        this.dataGrid.Height = 450;

                    }
                    Thickness gridmargin = new Thickness();
                    gridmargin.Top = 0;
                    gridmargin.Left = 5;
                    this.dataGrid.Margin = gridmargin;
                }
            }
            int scaleocnt = 1;
            if (CheckCas == true) scaleocnt = Set_Scale;
            if (twoway == 2) scaleocnt = 2;
            else label_Conncet2.Visibility = Visibility.Hidden;


            for (int i = 0; i < scaleocnt; i ++)
            {
                int portNumber = i + 1;
                serialData[i] = new SerialComm();
                //시리얼 연결 포트 순서 확인
                if (i == 0) label_Conncet.Background = new SolidColorBrush(Colors.Blue);
                else if (i == 1) label_Conncet2.Background = new SolidColorBrush(Colors.Blue);

                int com = Convert.ToInt32(SerialComport);
                bool rt = serialData[i].OpenComm(com + i, 9600, 8, System.IO.Ports.StopBits.One, System.IO.Ports.Parity.None, System.IO.Ports.Handshake.XOnXOff);
                Thread.Sleep(1000);
                if(false == rt)
                {
                    if(i == 0 ) label_Conncet.Background = new SolidColorBrush(Colors.IndianRed);
                    else if (i == 1) label_Conncet2.Background = new SolidColorBrush(Colors.IndianRed);
                }
                LogManager.getInstance().writeLog(string.Format("serial connect " + rt + " PORT " + (com + i)));
            }

            serialData[0].DataReceivedHandler += DataReceivedHandler;
            serialData[0].DisconnectedHandler += DisconnectedHandler;

            if(2 == twoway)
            {
                serialData[1].DataReceivedHandler += DataReceivedHandler2ndPort;
                serialData[1].DisconnectedHandler += DisconnectedHandler;
            }

            //dof 는 30개 까지 
            SetDofComboBox();
            //받는 시리얼 버퍼 
            //시리얼은 저울 수랑 약간 다름 그래서 inputdatacount를 사용
            int setbufferlength = 2 + 2 + (4 * inputDataCount) + 1;
            receivedbuffer = new byte[setbufferlength];
            receivedbuffer2nd = new byte[setbufferlength];

            //스펙 입력 일반일때는 readonly로 진행
            Text_WeightMin.IsReadOnly = true;
            Text_WeightMax.IsReadOnly = true;

            //QMS 땡겨오기
            if (true == bQmsdb_Connect)
            {
                LocalDataManager dbdata = LocalDataManager.getInstance();
                LogManager.getInstance().writeLog("QMS DB Load of initialize");
                Set_ProductPlan();
                LogManager.getInstance().writeLog("set plan of initialize");
                dbdata.Truncatetable("WEIGHT_SPEC");
                Set_Weight_SPEC();
            }
        }

        private void InitComboBox()
        {
            //comboBox_LOT.Items.Add("LOT 를 선택");
            comboBox_HO.Items.Clear();
            comboBox_POS.Items.Clear();
            comboBox_DOF.Items.Clear();
            comboBox_SIDE.Items.Clear();
        }
        
        public void undate_language(int language)
        {
            switch (language)
            {
                case 0:
                    ResourceDictionary ko = new ResourceDictionary();
                    ko.Source = new Uri("Ko.xaml", UriKind.Relative);
                    App.Current.Resources.MergedDictionaries.Add(ko);
                    //((MainWindow)this.DataContext).BindingUpdate();
                    break;
                case 1:
                    ResourceDictionary en = new ResourceDictionary();
                    en.Source = new Uri("En.xaml", UriKind.Relative);
                    App.Current.Resources.MergedDictionaries.Add(en);
                    //((MainWindow_ViewModel)this.DataContext).BindingUpdate();
                    break;
                case 2:
                    ResourceDictionary zh = new ResourceDictionary();
                    zh.Source = new Uri("Ch.xaml", UriKind.Relative);
                    App.Current.Resources.MergedDictionaries.Add(zh);
                    //((MainWindow_ViewModel)this.DataContext).BindingUpdate();
                    break;
            }
        }
        /// <summary>
        /// qms 업로드 백그라운드 동작 쓰레드
        /// </summary>        
        private void mThreadSendQmsDb(object sender, DoWorkEventArgs arg)
        {          
            lock (qmslock)
            {
                Thread.Sleep(1);
                LocalDataManager dbdata = LocalDataManager.getInstance();
                QMSDataManager qmsdata = QMSDataManager.getInstance();
                if (true == qmsdata.isOpen())
                {
                    DataSet specdata = dbdata.GetSpinWeightResult();
                    int insertcount = qmsdata.InsertSpinWeightResultToQMS(specdata);
                    string temp = string.Format("QMS DB Insert {0} EA", insertcount);
                    LogManager.getInstance().writeLog(temp);
                }
                else
                {
                    LogManager.getInstance().writeLog("QMS Disconnect SEND DATA FAIL");
                }
            }   
        }

        /// <summary>
        /// QMS 업로드 스레드 완료 콜백
        /// </summary>
        private void mThreadSendQmsComp(object sender, RunWorkerCompletedEventArgs arg)
        {
            //QMS 업로드 완료하면 호출됨
            if (arg.Cancelled)
            {

            }
            else if (arg.Error != null)
            {

            }
            else
            {
                QMSDataManager qmsdata = QMSDataManager.getInstance();
                LocalDataManager dbdata = LocalDataManager.getInstance();
                //여기서 Ui 갱신넣기
                if (true == qmsdata.isOpen())
                {
                    DataSet specdata = dbdata.GetSpinWeightResult();
                    int insertcount = qmsdata.InsertSpinWeightResultToQMS(specdata);
                    string temp = string.Format("QMSCOMP DB Insert {0} EA", insertcount);
                    LogManager.getInstance().writeLog(temp);
                }
                else
                {
                    LogManager.getInstance().writeLog("QMS Disconnect Upload Data Fail");
                }
            }
        }

        private object lockObject = new object();
        private object lockObject2 = new object();

        int currentbuff = 0;
        int checkinputfirst = 0;
        int getalldataTwoWay = 0;
      
        /// <summary>
        /// serial receive event handler
        /// </summary>
        /// <param name="receiveData"></param>
        private void DataReceivedHandler(byte[] receiveData, int portN)
        {
            lock (lockObject)
            {
                dicSerialData.Clear();
                Array.Clear(receivedbuffer, 0, receivedbuffer.Length);

                //2way 경우에는 ini에 등록된  port를 지금 들어온  portN과 비교하여  1번인지 2번인지 확인합니다. 
                int set2line = 0;
                int com = Convert.ToInt32(SerialComport);
                if (twoway == 2)
                {
                    if (portN == com) set2line = 0;
                    else set2line = 1;
                }

                if(receiveData.Length <= 20)
                {
                    LogManager.getInstance().writeLog("입력 zero");
                    return;
                }

                string hex = BitConverter.ToString(receiveData);
                  
                LogManager.getInstance().writeLog(hex, DebuglogQuery, Debuglog);
                if (CheckCas == true)
                {
                    string temp = Encoding.Default.GetString(receiveData, 10, 8);
                    double value = Convert.ToDouble(temp);
                    if (!dicSerialData.ContainsKey(portN))
                    {
                        dicSerialData.Add(portN, value * 1000);
                    }
                }
                else
                {
                  
                    int setlength = 2 + 2 + (4 * Set_Scale);
                    LogManager.getInstance().writeLog(string.Format("receive lenght " + receiveData.Length), DebuglogQuery, Debuglog);

                    //다이아퍼 미쳐가지고 8개로 들어옴
                    if ((receiveData.Length == setlength) || (receiveData.Length == (setlength + 1)))
                    {
                        int bbtt = receiveData[0];
                        //LogManager.getInstance().writeLog("입력 1");
                        LogManager.getInstance().writeLog(string.Format("0번 바이트 " + bbtt.ToString()), DebuglogQuery, Debuglog);

                        if (receiveData[0] == 48)
                        {
                          //  LogManager.getInstance().writeLog("입력 2");
                            currentbuff = receiveData.Length;
                            Array.Copy(receiveData, 0, receivedbuffer, 0, receiveData.Length);
                                            
                          //  LogManager.getInstance().writeLog("입력 바이트_ " + hex);
                          LogManager.getInstance().writeLog("입력 길이_ " + currentbuff, DebuglogQuery, Debuglog);
                        }                        
                    }
                    else
                    {                      
                        //banbookDatainsert = 0;
                        //onecyclepass = false;
                        return;
                    }                  
                    int minus = 1;

                    if (receivedbuffer[receivedbuffer.Length - minus - 1] == 13)
                    {
                        try
                        {
                            if (receivedbuffer[receivedbuffer.Length - minus] == 10)
                            {                                

                                if (twoway == 2)
                                {
                                    LogManager.getInstance().writeLog("INPUT PORT " + portN.ToString());
                                }
                                else
                                {
                                    LogManager.getInstance().writeLog("입력 3", DebuglogQuery, Debuglog);
                                }
                                
                                int ReceiveBufflength = 2 + 2 + (4 * inputDataCount);
                                int weightDatalength = (inputDataCount);

                                int okvalue = 0;

                                if (receivedbuffer.Length == ReceiveBufflength || receivedbuffer.Length == (ReceiveBufflength + 1))
                                {
                                    LogManager.getInstance().writeLog("입력 4", DebuglogQuery, Debuglog);
                                    currentbuff = 0;
                                    double[] realData = new double[weightDatalength];
                                    int s = 2;
                                    for (int i = 0; i < weightDatalength; i++)
                                    {
                                        int datahigh = s + (i * 4);
                                        int datamiddle = s + (i * 4) + 1;
                                        int datalow = s + (i * 4) + 2;
                                        int config = s + (i * 4) + 3;
                                       // LogManager.getInstance().writeLog(string.Format("datahigh" + datahigh + "middle" + datamiddle + "datalow" + datalow + "config" + config), DebuglogQuery, Debuglog);
                                       // LogManager.getInstance().writeLog(string.Format("2 " + receivedbuffer[config]), DebuglogQuery, Debuglog);
                                        var ba1 = new BitArray(receivedbuffer[config]);
                                        string sbit = Convert.ToString(receivedbuffer[config], 2).PadLeft(8, '0');

                                        // LogManager.getInstance().writeLog(string.Format("bit" + sbit));
                                        int boolInt0 = Convert.ToInt32(sbit.Substring(0, 1));
                                        int boolInt1 = Convert.ToInt32(sbit.Substring(1, 1));
                                        int boolInt2 = Convert.ToInt32(sbit.Substring(2, 1));

                                      //  LogManager.getInstance().writeLog(string.Format("안정판정" + Convert.ToInt32(sbit.Substring(5, 1))), DebuglogQuery, Debuglog);
                                      //  LogManager.getInstance().writeLog(string.Format("boolInt0 " + boolInt0 + boolInt1 + boolInt2), DebuglogQuery, Debuglog);
                                        int q = boolInt0 + (boolInt1 * 2) + (boolInt2 * 4);

                                        double rehi = receivedbuffer[datahigh];
                                        double remi = receivedbuffer[datamiddle];
                                        double relo = receivedbuffer[datalow];

                                        realData[i] = (((rehi - 20) * 10000)
                                                     + ((remi - 20) * 100)
                                                     + (relo - 20));
                                        realData[i] = realData[i] * rtConfigData(q); 

                                        BitArray sum;
                                        BitArray bcc;
                                        if (receivedbuffer.Length == ReceiveBufflength)            bcc = new BitArray(receivedbuffer[ReceiveBufflength - 3]);
                                        else if (receivedbuffer.Length == (ReceiveBufflength + 1)) bcc = new BitArray(receivedbuffer[ReceiveBufflength - 4]);
                                    }
                                    Array.Clear(receivedbuffer, 0, receivedbuffer.Length);
                                   // LogManager.getInstance().writeLog(string.Format("리시브 함수 콜 data hex " + hex), DebuglogQuery, Debuglog);
                                    for (int j = 0; j < realData.Length; j++)
                                    {                             
                                        dicSerialData.Add(j, realData[j]);
                                        //입력값 50이하면 무시하는 값으로
                                        if (realData[j] >= 50)
                                        {
                                            okvalue = okvalue + 1;
                                        }
                                    }
                                   LogManager.getInstance().writeLog("입력 4_2", DebuglogQuery, Debuglog);
                                }
                                //절반 이상 아니면 무시 리턴                                
                                if (okvalue < (int)(Set_Scale / 2))
                                {
                                    LogManager.getInstance().writeLog("유효 데이터 수량 미달로 초기화", DebuglogQuery, Debuglog);
                                    Array.Clear(receivedbuffer, 0, receivedbuffer.Length);
                                    banbookDatainsert = 0;
                                    onecyclepass = false;
                                    return;
                                }                                
                                else
                                {
                                    if (banbookDatainsert == 0 && onecyclepass == true)
                                    {
                                        LogManager.getInstance().writeLog("입력 5", DebuglogQuery, Debuglog);
                                        //한번은 넘기고 늦게들어오는게 생깁니다.                               
                                        banbookDatainsert += 1;
                                    }
                                    else
                                    {
                                        //한번은 넘기고 onecyclepass를 켜준다음에.    
                                        LogManager.getInstance().writeLog("입력 4_3", DebuglogQuery, Debuglog);                              
                                        onecyclepass = true;
                                        return;
                                    }
                                }
                            }
                            else
                            {
                                banbookDatainsert = 0;
                                onecyclepass = false;
                                Array.Clear(receivedbuffer, 0, receivedbuffer.Length);
                            }
                        }
                        catch
                        {
                            banbookDatainsert = 0;
                            onecyclepass = false;
                            Array.Clear(receivedbuffer, 0, receivedbuffer.Length);
                        }                        
                    }
                    else
                    {
                        banbookDatainsert = 0;
                        onecyclepass = false;
                        Array.Clear(receivedbuffer, 0, receivedbuffer.Length);
                    }
                }
                //211126 카스 관련해서 카스는 딱 맞으면 한번만에 들어가도록 수정
                if ((dicSerialData.Count == inputDataCount && banbookDatainsert == 1) ||
                    (dicSerialData.Count == inputDataCount && CheckCas == true))
                {
                    LogManager.getInstance().writeLog("입력 6", DebuglogQuery, Debuglog);
                    banbookDatainsert = 5;
                  
                    if (reverseArr == true)
                    {
                        LogManager.getInstance().writeLog(string.Format("11-1"), DebuglogQuery, Debuglog);
                        dicSerialData = dicSerialData.OrderByDescending(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
                        // dicSerialData.Reverse();
                    }
                    //아래에서 getserialdata claer하는데 어쩌지?
                    //serialDataDictoList(dicSerialData);
                    //여기서 set2line 에 따라서 앞에 12개에 데이터 넣을지 뒤에 12개에 넣을지 변경해줘야 함
                    //if (2 == twoway)
                    //{
                    //    SwapSerialData(dicSerialData, set2line);
                    //    getSerialData = new List<double>(dicSerialData.Values);
                    //}
                    //else
                    //{
                    //    getSerialData = new List<double>(dicSerialData.Values);
                    //}
                    getSerialData = new List<double>(dicSerialData.Values);
                    string arraydata = string.Join(",", getSerialData);
                    LogManager.getInstance().writeLog(string.Format((portN + 1) + "라인" + "데이터 갯수 " + runScaleCount));
                    LogManager.getInstance().writeLog(string.Format((portN + 1) + "라인" + "데이터 " + arraydata));

                    getalldataTwoWay += 1;
                    //여기서 라인 1 라인 2 들어온거 계산해서 두개 다 들어온 데이터 쌓일 경우에만 넣기
                    //runscalecount 는 두개 이니까 *2로 해줘서 넣어야함
                    ///220704 serial received 를 나눠서 정리하는 테스트 중
                    /*if (twoway == 2 && getalldataTwoWay == 2)
                    {
                        int WayScale = runScaleCount * 2;
                        // set2line 를 빼고 data가 풀로 샇이면 한번에 처리 하자
                        UpdataSerialDataWightScale(WayScale, 0);
                        LogManager.getInstance().writeLog(("2열 데이터 입력 TEXT Box updata" + arraydata));

                    }
                    else if (twoway == 1)
                    {
                        UpdataSerialDataWightScale(runScaleCount, 0);
                    }
                    else
                    {
                        LogManager.getInstance().writeLog(string.Format("한개 라인 입력 완료 후 리턴  " + getSerialData.Count().ToString()));
                        return;
                    }
                    */
                    ///220704 serial received 를 나눠서 정리하는 테스트 중

                    UpdataSerialDataWightScale(runScaleCount, 0);

                    /*
                    if (twoway == 2 && getalldataTwoWay == 2)
                    {
                        int WayScale = runScaleCount * 2;
                        // set2line 를 빼고 data가 풀로 샇이면 한번에 처리 하자
                        UpdataSerialDataWightScale(WayScale, 0);
                        LogManager.getInstance().writeLog(("2열 데이터 입력 TEXT Box updata" + arraydata));

                    }
                    else if(twoway == 1)
                    {
                        UpdataSerialDataWightScale(runScaleCount, 0);
                    }
                    else {
                        LogManager.getInstance().writeLog(string.Format("한개 라인 입력 완료 후 리턴  " + getSerialData.Count().ToString()));
                        return;
                    }
                    */

                   // LogManager.getInstance().writeLog(string.Format("데이터 총량  " + getSerialData.Count().ToString()));
                                           
                    if (twoway == 2) set2line = 0;
                    this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate 
                    {
                        UPdataSeiralDataToGridAndDB(set2line);
                    }));
                    //1번 포트 다 들어와서 끝났다!
                    checkinputfirst = 1;
                    LogManager.getInstance().writeLog(string.Format("END"), DebuglogQuery, Debuglog);
                    dicSerialData.Clear();
                    //여기서 시간을 좀 끌어줘야할 필요가 있음
                    Thread.Sleep(200);                    
                    getalldataTwoWay = 0;
                }
            }            
        }

        private void DataReceivedHandler2ndPort(byte[] receiveData, int portN)
        {
            lock (lockObject2)
            {              
                dicSerialData2nd.Clear();
                Array.Clear(receivedbuffer2nd, 0, receivedbuffer2nd.Length);
                //일단 com1에서 다 들어오면 2 진행 할 수 있도록 하자
                if (1 != checkinputfirst) 
                {
                    banbookDatainsert2nd = 0;
                    onecyclepass2nd = false;
                    return;
                }
                //2way 경우에는 ini에 등록된  port를 지금 들어온  portN과 비교하여  1번인지 2번인지 확인합니다. 
                int set2line = 0;
                int com = Convert.ToInt32(SerialComport);
                if (twoway == 2)
                {
                    if (portN == com) set2line = 0;
                    else set2line = 1;
                }

                if (receiveData.Length <= 20)
                {
                    LogManager.getInstance().writeLog("입력 zero");
                    return;
                }

                string hex = BitConverter.ToString(receiveData);

                LogManager.getInstance().writeLog(hex, DebuglogQuery, Debuglog);
                if (CheckCas == true)
                {
                    string temp = Encoding.Default.GetString(receiveData, 10, 8);
                    double value = Convert.ToDouble(temp);
                    if (!dicSerialData2nd.ContainsKey(portN))
                    {
                        dicSerialData2nd.Add(portN, value * 1000);
                    }
                }
                else
                {

                    int setlength = 2 + 2 + (4 * Set_Scale);
                    LogManager.getInstance().writeLog(string.Format("receive lenght " + receiveData.Length), DebuglogQuery, Debuglog);

                    //다이아퍼 미쳐가지고 8개로 들어옴
                    if ((receiveData.Length == setlength) || (receiveData.Length == (setlength + 1)))
                    {
                        int bbtt = receiveData[0];
                        //LogManager.getInstance().writeLog("입력 1");
                        LogManager.getInstance().writeLog(string.Format("0번 바이트 " + bbtt.ToString()), DebuglogQuery, Debuglog);

                        if (receiveData[0] == 48)
                        {
                            //  LogManager.getInstance().writeLog("입력 2");
                            currentbuff = receiveData.Length;
                            Array.Copy(receiveData, 0, receivedbuffer2nd, 0, receiveData.Length);
                            //  LogManager.getInstance().writeLog("입력 바이트_ " + hex);
                            LogManager.getInstance().writeLog("입력 길이_ " + currentbuff, DebuglogQuery, Debuglog);
                        }
                    }
                    else
                    {
                        //banbookDatainsert = 0;
                        //onecyclepass = false;
                        return;
                    }
                    int minus = 1;

                    if (receivedbuffer2nd[receivedbuffer2nd.Length - minus - 1] == 13)
                    {
                        try
                        {
                            if (receivedbuffer2nd[receivedbuffer2nd.Length - minus] == 10)
                            {

                                if (twoway == 2)
                                {
                                    LogManager.getInstance().writeLog("INPUT PORT " + portN.ToString());
                                }
                                else
                                {
                                    LogManager.getInstance().writeLog("입력 3", DebuglogQuery, Debuglog);
                                }

                                int ReceiveBufflength = 2 + 2 + (4 * inputDataCount);
                                int weightDatalength = (inputDataCount);

                                int okvalue = 0;

                                if (receivedbuffer2nd.Length == ReceiveBufflength || receivedbuffer2nd.Length == (ReceiveBufflength + 1))
                                {
                                    LogManager.getInstance().writeLog("입력 4", DebuglogQuery, Debuglog);
                                    currentbuff = 0;
                                    double[] realData = new double[weightDatalength];
                                    int s = 2;
                                    for (int i = 0; i < weightDatalength; i++)
                                    {
                                        int datahigh = s + (i * 4);
                                        int datamiddle = s + (i * 4) + 1;
                                        int datalow = s + (i * 4) + 2;
                                        int config = s + (i * 4) + 3;
                                        // LogManager.getInstance().writeLog(string.Format("datahigh" + datahigh + "middle" + datamiddle + "datalow" + datalow + "config" + config), DebuglogQuery, Debuglog);
                                        // LogManager.getInstance().writeLog(string.Format("2 " + receivedbuffer2nd[config]), DebuglogQuery, Debuglog);
                                        var ba1 = new BitArray(receivedbuffer2nd[config]);
                                        string sbit = Convert.ToString(receivedbuffer2nd[config], 2).PadLeft(8, '0');
                                                                          
                                        int boolInt0 = Convert.ToInt32(sbit.Substring(0, 1));
                                        int boolInt1 = Convert.ToInt32(sbit.Substring(1, 1));
                                        int boolInt2 = Convert.ToInt32(sbit.Substring(2, 1));

                                        //  LogManager.getInstance().writeLog(string.Format("안정판정" + Convert.ToInt32(sbit.Substring(5, 1))), DebuglogQuery, Debuglog);
                                        //  LogManager.getInstance().writeLog(string.Format("boolInt0 " + boolInt0 + boolInt1 + boolInt2), DebuglogQuery, Debuglog);
                                        int q = boolInt0 + (boolInt1 * 2) + (boolInt2 * 4);

                                        double rehi = receivedbuffer2nd[datahigh];
                                        double remi = receivedbuffer2nd[datamiddle];
                                        double relo = receivedbuffer2nd[datalow];

                                        realData[i] = (((rehi - 20) * 10000)
                                                     + ((remi - 20) * 100)
                                                     + (relo - 20));
                                        realData[i] = realData[i] * rtConfigData(q);

                                        BitArray sum;
                                        BitArray bcc;
                                        if (receivedbuffer2nd.Length == ReceiveBufflength) bcc = new BitArray(receivedbuffer2nd[ReceiveBufflength - 3]);
                                        else if (receivedbuffer2nd.Length == (ReceiveBufflength + 1)) bcc = new BitArray(receivedbuffer2nd[ReceiveBufflength - 4]);
                                    }
                                    Array.Clear(receivedbuffer2nd, 0, receivedbuffer2nd.Length);
                                    // LogManager.getInstance().writeLog(string.Format("리시브 함수 콜 data hex " + hex), DebuglogQuery, Debuglog);
                                    for (int j = 0; j < realData.Length; j++)
                                    {
                                        dicSerialData2nd.Add(j, realData[j]);
                                        //입력값 50이하면 무시하는 값으로
                                        if (realData[j] >= 50)
                                        {
                                            okvalue = okvalue + 1;
                                        }
                                    }
                                    LogManager.getInstance().writeLog("입력 4_2", DebuglogQuery, Debuglog);
                                }
                                //절반 이상 아니면 무시 리턴
                                //1129 김재환 이거 1개 미만으로 진행 
                                //if (okvalue < (Set_Scale / 3))
                                if (okvalue <= (int)(Set_Scale / 2))
                                {
                                    LogManager.getInstance().writeLog("유효 데이터 수량 미달로 초기화", DebuglogQuery, Debuglog);
                                    Array.Clear(receivedbuffer2nd, 0, receivedbuffer2nd.Length);
                                    banbookDatainsert2nd = 0;
                                    onecyclepass2nd = false;
                                    return;
                                }
                                else
                                {
                                    if (banbookDatainsert2nd == 0 && onecyclepass2nd == true)
                                    {
                                        LogManager.getInstance().writeLog("입력 5", DebuglogQuery, Debuglog);
                                        //한번은 넘기고 늦게들어오는게 생깁니다.                               
                                        banbookDatainsert2nd += 1;
                                    }
                                    else
                                    {
                                        //한번은 넘기고 onecyclepass를 켜준다음에.    
                                        LogManager.getInstance().writeLog("입력 4_3", DebuglogQuery, Debuglog);
                                        onecyclepass2nd = true;
                                        return;
                                    }
                                }
                            }
                            else
                            {
                                banbookDatainsert2nd = 0;
                                onecyclepass2nd = false;
                                Array.Clear(receivedbuffer2nd, 0, receivedbuffer2nd.Length);
                            }
                        }
                        catch
                        {
                            banbookDatainsert2nd = 0;
                            onecyclepass2nd = false;
                            Array.Clear(receivedbuffer2nd, 0, receivedbuffer2nd.Length);
                        }
                    }
                    else
                    {
                        banbookDatainsert2nd = 0;
                        onecyclepass2nd = false;
                        Array.Clear(receivedbuffer2nd, 0, receivedbuffer2nd.Length);
                    }
                }
                //211126 카스 관련해서 카스는 딱 맞으면 한번만에 들어가도록 수정
                if ((dicSerialData2nd.Count == inputDataCount && banbookDatainsert2nd == 1) ||
                    (dicSerialData2nd.Count == inputDataCount && CheckCas == true))
                {
                    LogManager.getInstance().writeLog("입력 6", DebuglogQuery, Debuglog);
                    banbookDatainsert2nd = 5;

                    if (reverseArr == true)
                    {
                        LogManager.getInstance().writeLog(string.Format("11-1"), DebuglogQuery, Debuglog);
                        dicSerialData2nd = dicSerialData2nd.OrderByDescending(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
                        // dicSerialData2nd.Reverse();
                    }
                    //아래에서 getserialdata claer하는데 어쩌지?
                    //serialDataDictoList(dicSerialData2nd);
                    //여기서 set2line 에 따라서 앞에 12개에 데이터 넣을지 뒤에 12개에 넣을지 변경해줘야 함
                    //if (2 == twoway)
                    //{
                    //    SwapSerialData(dicSerialData2nd, set2line);
                    //}
                    //else
                    //{
                    //    getSerialData = new List<double>(dicSerialData2nd.Values);
                    //}
                    getSerialData = new List<double>(dicSerialData2nd.Values);
                    string arraydata = string.Join(",", getSerialData);
                    LogManager.getInstance().writeLog(string.Format((portN + 1) + "라인" + "데이터 갯수 " + runScaleCount));
                    LogManager.getInstance().writeLog(string.Format((portN + 1) + "라인" + "데이터 " + arraydata));

                    getalldataTwoWay += 1;
                    //여기서 라인 1 라인 2 들어온거 계산해서 두개 다 들어온 데이터 쌓일 경우에만 넣기
                    //runscalecount 는 두개 이니까 *2로 해줘서 넣어야함
                    UpdataSerialDataWightScale(Set_Scale, 1);

                    //if (twoway == 2 && getalldataTwoWay == 2)
                    //{
                    //    int WayScale = runScaleCount * 2;
                    //    // set2line 를 빼고 data가 풀로 샇이면 한번에 처리 하자
                    //    UpdataSerialDataWightScale(WayScale, 1);
                    //    LogManager.getInstance().writeLog(("2열 데이터 입력 TEXT Box updata" + arraydata));

                    //}
                    //else if (twoway == 1)
                    //{
                    //    UpdataSerialDataWightScale(runScaleCount, 0);
                    //}
                    //else
                    //{
                    //    LogManager.getInstance().writeLog(string.Format("한개 라인 입력 완료 후 리턴  " + getSerialData.Count().ToString()));
                    //    return;
                    //}

                    LogManager.getInstance().writeLog(string.Format("데이터 총량  " + getSerialData.Count().ToString()));

                    //호출당하는데다 업로드하면안되고 20210416 jaehwan                   
                    if (twoway == 2) set2line = 0;
                    this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate
                    {
                        UPdataSeiralDataToGridAndDB(set2line);
                    }));
                    LogManager.getInstance().writeLog(string.Format("2nd END"), DebuglogQuery, Debuglog);
                    dicSerialData2nd.Clear();
                    //여기서 시간을 좀 끌어줘야할 필요가 있음
                    Thread.Sleep(300);
                    checkinputfirst = 0;
                    getalldataTwoWay = 0;
                }
            }
        }

        private double rtConfigData(int bi)
        {
            double rt = 1;

            if (bi == 1) rt = 1;
            else if (bi == 2) rt = 0.1;
            else if (bi == 3) rt = 0.01;
            else if (bi == 4) rt = 0.001;
            else if (bi == 5) rt = 0.0001;
            else if (bi == 6) rt = 0.1;
            return rt;
        }

        private BitArray makesumbit(BitArray h, BitArray m, BitArray l, BitArray b)
        {
            BitArray q = new BitArray(8);            
            return q;
        }

        private bool BccCheckSum(BitArray sum, byte dcc)
        {
            //BitArray a = new BitArray(sum);
            BitArray a = sum;
            BitArray b = new BitArray(dcc);
            a.Or(b);
            byte ch = Convert.ToByte(a.ToString(), 2);
            return ch == 255 ? true : false;            
        }

        //시리얼 Byte 를 값으로 변환

        private void serialDataDictoList(Dictionary<int, double> temp)
        {            
            var values = from item in temp orderby item.Key select temp.Values;            
            getSerialData.Clear();
            foreach (KeyValuePair<int, double> item in temp)
            {               
                getSerialData.Add(Convert.ToDouble(item.Value));
            }

            if(getSerialData.Count == Set_Scale)
            {
                // 이러면 진행하믄대지 
            }
        }
        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if(e.Cancelled)
            {
                LogManager.getInstance().writeLog("send serial cancelled");
            }
            else if(e.Error != null)
            {
                LogManager.getInstance().writeLog("send serial Error");
            }
            else
            {
                //여기서 Ui 갱신넣기
                LogManager.getInstance().writeLog("send serial Comp");
            }
        }

        private void DisconnectedHandler()
        {   
            Console.WriteLine("serial disconnected");
            label_Conncet.Background = new SolidColorBrush(Colors.IndianRed);
            label_Conncet2.Background = new SolidColorBrush(Colors.IndianRed);
        }


        private object textobj = new object();
        /// <summary>
        /// 시리얼 데이터로 화면 업데이트 
        /// </summary>
        /// <param name="ScaleCount"></param>
        public void UpdataSerialDataWightScale(int ScaleCount, int on2 = 0)
        {
            lock (textobj)
            {
                try
                {
                    int reveropp = 0;
                    //if (reverseArr == true) reveropp = inputDataCount - ScaleCount;
                    //SignalConut 수정
                    if (reverseArr == true) reveropp = 0;
                    if (getSerialData.Count == ScaleCount)
                    {
                        for (int i = 0; i < ScaleCount; i++)
                        {
                            int k = i;
                            if (twoway == 2) k = i + (ScaleCount * on2);

                            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                            {
                                listTextbox[k].Text = getSerialData[i + reveropp].ToString("N1");
                            }));
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogManager.getInstance().writeLog(ex.ToString());
                }
            }
        }

        private object gridobj = new object();
        /// <summary>
        /// 데이터 그리드 갱신
        /// </summary>
        private void UPdataSeiralDataToGridAndDB(int way = 0)
        {
            lock (gridobj)
            {
                try
                {
                    LocalDataManager dbdata = LocalDataManager.getInstance();
                    // 두줄하는데 데이터가 다 없으면 그냥 리턴해보까
                    // 두줄하니까 분별할 수있는 뭔가를 만들어야 함                 
                    if (twoway == 2 && getalldataTwoWay != 2)
                    {
                        //if (way == 0)
                        //{
                        //    getSerialData.Clear();
                        //    return;
                        //}
                    }
                    //사이드가 나눠져잇는지 아닌지 여기서 확인하자 
                    var tempside = SingleSide.Distinct().ToList();
                    if (NextStepSide.Count > tempside.Count)
                    {
                        sideDivisionexption = NextStepSide.Count / tempside.Count;
                    }
                    ///2개의 저울 데이터를 한개로 뭉쳐서 쓰는 경우   && getSerialData.Count >= 4 를 넣었으나 8개로들어오는 제품 발생 
                    if (true == checkBox_2Scale1Result.IsChecked)
                    {
                    }
                    ///
                    //var copyseriallist = getSerialData.ConvertAll(s => s);
                    var copyseriallist = SetTwoWayCountforSerialDatalist(getSerialData, way);
                    Specpanjung.Clear();
                    int reveropp = 0;
                    //if (reverseArr == true) reveropp = Math.Abs(inputDataCount - Set_Scale);
                    //signalCount 수정됨
                    if (reverseArr == true) reveropp = Math.Abs(0);
                    //두줄설비

                    for (int i = 0 + reveropp; i < copyseriallist.Count; i++)
                    {
                        copyseriallist[i] = copyseriallist[i] - Convert.ToDouble(text_jikwan.Text);
                        getSerialData[i] = getSerialData[i] - Convert.ToDouble(text_jikwan.Text);
                        //측정무게가 80g 미만이면 0처리 하도록하자
                        if (80 > copyseriallist[i]) copyseriallist[i] = 0.00;
                        Specpanjung.Add(WeightPanjung(copyseriallist[i]));
                    }

                    if (NextStepSide.Count <= sideCount)
                    {
                        startPosCount += 1;
                        sideCount = 0;
                    }
                    else
                    { }

                    if ("" == ReturnNextPos(startPosCount) && true != checkBox_TestLot.IsChecked)
                    {
                        MessageBox.Show("POS OUT OF RANGE");
                        return;
                    }

                    int checksidecount = NextStepSide.Count / comboBox_SIDE.Items.Count;
                    int setSidecount = 0;

                    if (true == SideChangeLogic())
                    {
                        checksidecount = sideDivisionexption;
                        int setcomboindex = comboBox_SIDE.SelectedIndex;
                        //setSidecount = (checksidecount * setcomboindex) + sideCount;
                        setSidecount = (setcomboindex) + sideCount;
                    }
                    else setSidecount = sideCount;
                    List<double> ReBuildResultArr = new List<double>();
                    if (reverseArr == true)
                    {
                        if (Set_Scale != inputDataCount)
                        {
                            int ccc = Math.Abs(inputDataCount - Set_Scale);
                            for (int i = 0 + ccc; i < inputDataCount; i++)
                            {
                                ReBuildResultArr.Add(getSerialData[i]);
                            }
                            this.LoTTable = GetTable(SetvalueToGrid(ReturnNextPos(startPosCount), NextStepSide[setSidecount], StartEndCount[setSidecount], NextStartEnd[setSidecount], ReBuildResultArr));
                            this.dataGrid.ItemsSource = this.LoTTable.DefaultView;
                            this.dataGrid.ScrollIntoView(dataGrid.Items.GetItemAt(dataGrid.Items.Count - 1));
                        }
                    }
                    else
                    {
                        if (true == checkBox_2Scale1Result.IsChecked) this.LoTTable = GetTable(SetvalueToGrid(ReturnNextPos(startPosCount), NextStepSide[setSidecount], StartEndCount[setSidecount], NextStartEnd[setSidecount], copyseriallist));
                        else this.LoTTable = GetTable(SetvalueToGrid(ReturnNextPos(startPosCount), NextStepSide[setSidecount], StartEndCount[setSidecount], NextStartEnd[setSidecount], copyseriallist));
                        this.dataGrid.ItemsSource = this.LoTTable.DefaultView;
                        this.dataGrid.ScrollIntoView(dataGrid.Items.GetItemAt(dataGrid.Items.Count - 1));
                    }

                    if (!SideChangeLogic()) sideCount += 1;
                    else
                    {
                        if (sideCount < checksidecount - 1)
                        {
                            sideCount += 1;
                        }
                        else if (sideCount == checksidecount - 1)
                        {
                            sideCount = 0;
                            startPosCount += 1;
                        }
                    }
                    //들어온 데이터 가지고 파싱해서
                    //판정(위에 LOT 데이터의 SPEC 가지고 판정을 함) 한 뒤에 그리드에 뿌려주고
                    //ENd 신호가 따로 있는가?
                    if (2 != twoway)
                    {
                        getSerialData.Clear();
                    }
                    else if (2 == twoway && 1 == way)
                    {
                        getSerialData.Clear();
                    }
                    //SIDE 갯수보다 많아지면 POS가 한개 올라가야함
                    // QMS에 데이터 올리고
                }
                catch (Exception ex)
                {
                    LogManager.getInstance().writeLog(ex.ToString());
                }
            }
          
        }


        private void mThreadSerialData(object sender, DoWorkEventArgs arg)
        {          
            //시리얼 프로토콜 확인해봐야함 Rs232
            while (!mThreadSerial.CancellationPending)
            {
                try
                {
                    _backgroundWorker = Thread.CurrentThread;
                    int roof = 1;
                    if (CheckCas == false && twoway == 2) roof = 2;
                    
                    byte[] sendData = new byte[] { 0x30, 0x31, 0x52, 0x44, 0x0A };                    
                    for(int i = 0; i < roof; i++)
                    {
                        serialData[i].Send(sendData);
                    }                    
                  //  LogManager.getInstance().writeLog("Send");
                    Thread.Sleep(200);
                }
                catch(ThreadAbortException ex)
                {
                    var currentmethod = new StackTrace().GetFrame(1).GetMethod();
                    LogManager.getInstance().writeLog(currentmethod.Name + "__" + ex.ToString());
                }
            }
        }

        private void threadsendData()
        {
            int roof = 1;
            if (CheckCas == false && twoway == 2) roof = 2;
            //시리얼 프로토콜 확인해봐야함 Rs232
            while (isthreadSendSerialIdle)
            {
                try
                {
                    Thread.Sleep(250);
                    byte[] sendData = new byte[] { 0x30, 0x31, 0x52, 0x44, 0x0A };
                    for (int i = 0; i < roof; i++)
                    {
                        if (serialData[i].IsOpen)
                        {
                            serialData[i].Send(sendData);
                            //  LogManager.getInstance().writeLog("Send");
                        }
                        else
                        {
                            int com = Convert.ToInt32(SerialComport);
                            LogManager.getInstance().writeLog("Serial Reconnect");
                            serialData[0].OpenComm(com, 9600, 8, System.IO.Ports.StopBits.One, System.IO.Ports.Parity.None, System.IO.Ports.Handshake.XOnXOff);
                        }
                    }
                }
                catch (ThreadAbortException ex)
                {
                    LogManager.getInstance().writeLog(ex.ToString());
                }
            }
        }


        private void InitDataGrid(int set_scale)
        {
            int iWidth = 75;

            if(set_scale > 12)
            {
                iWidth = 55;
            }
            if(set_scale > 18)
            {
                iWidth = 45;
            }

            for (int i = 0; i < set_scale; i++)
            {
                dataGrid.Columns[6+i].Width = iWidth;
            }            

            table.Columns.Add("HXDay");
            table.Columns.Add("HXLine");
            table.Columns.Add("HXDof");
            table.Columns.Add("HXPos");
            table.Columns.Add("HXSide");
            table.Columns.Add("HXRage");
            table.Columns.Add("HXWeight1");
            table.Columns.Add("HXWeight2");
            table.Columns.Add("HXWeight3");
            table.Columns.Add("HXWeight4");
            table.Columns.Add("HXWeight5");
            table.Columns.Add("HXWeight6");
            table.Columns.Add("HXWeight7");
            table.Columns.Add("HXWeight8");
            table.Columns.Add("HXWeight9");
            table.Columns.Add("HXWeight10");
            table.Columns.Add("HXWeight11");
            table.Columns.Add("HXWeight12");
            table.Columns.Add("HXWeight13");
            table.Columns.Add("HXWeight14");
            table.Columns.Add("HXWeight15");
            table.Columns.Add("HXWeight16");
            table.Columns.Add("HXWeight17");
            table.Columns.Add("HXWeight18");
            table.Columns.Add("HXWeight19");
            table.Columns.Add("HXWeight20");
            table.Columns.Add("HXWeight21");
            table.Columns.Add("HXWeight22");
            table.Columns.Add("HXWeight23");
            table.Columns.Add("HXWeight24");

            //text box 컨트롤 넣기
            listTextbox.Add(text_Weight01);
            listTextbox.Add(text_Weight02);
            listTextbox.Add(text_Weight03);
            listTextbox.Add(text_Weight04);
            listTextbox.Add(text_Weight05);
            listTextbox.Add(text_Weight06);
            listTextbox.Add(text_Weight07);
            listTextbox.Add(text_Weight08);
            listTextbox.Add(text_Weight09);
            listTextbox.Add(text_Weight10);
            listTextbox.Add(text_Weight11);
            listTextbox.Add(text_Weight12);
            listTextbox.Add(text_Weight13);
            listTextbox.Add(text_Weight14);
            listTextbox.Add(text_Weight15);
            listTextbox.Add(text_Weight16);
            listTextbox.Add(text_Weight17);
            listTextbox.Add(text_Weight18);
            listTextbox.Add(text_Weight19);
            listTextbox.Add(text_Weight20);
            listTextbox.Add(text_Weight21);
            listTextbox.Add(text_Weight22);
            listTextbox.Add(text_Weight23);
            listTextbox.Add(text_Weight24);
        }

        private void ClearDataGrid()
        {
            this.LoTTable.Rows.Clear();
            startPosCount = 0;
            sideCount = 0;
        }

        private DataTable GetTable(List<string> temp = null)
        {
            try
            {
                if (temp != null)
                {                    
                    table.Rows.Add(temp.ToArray());
                }
                return table;
            }
            catch(Exception ex)
            {
                ex.ToString();
                return table;
            }
            
        }

        //리턴이 true 면 바꾸지 않고 가고 false 면 바꾸고
        private bool SideChangeLogic()
        {
            if (checkBox_OneSide.IsChecked == true)
            {
                return true;
            }
            else if (checkBox_OneSide.IsChecked != true)
            {
                return false;
            }
            return false;
        }
        private List<string> SetvalueToGrid(string pos, string side,  int endcount, string startend, List<double> inputtemp = null)
        {
            List<string> temp = new List<string>();

            //date
            temp.Add(CurrentProductDate);
            //line
            temp.Add(glineid);
            //dof
            temp.Add(comboBox_DOF.SelectedItem.ToString());
            //POS
            temp.Add(pos);
            //SIDE
            if (true == sideDiv) side = side.Substring(0, side.Length -1);
            temp.Add(side);
            //StartEnd
            temp.Add(startend);
            int c = 1;
            foreach(double setdata in inputtemp)
            {                
                if(endcount >= c)
                {                    
                    if (setdata > 30)
                    {
                        temp.Add(setdata.ToString("N1"));
                    }
                    else
                    {
                        temp.Add("");
                    }
                    c++;
                }                
            }
            return temp;
        }

        private void GetQMSDbData()
        {
            QMSDataManager qmsdbdata = QMSDataManager.getInstance();
        }

        private void button_DEVELOP_TEST_Click(object sender, RoutedEventArgs e)
        {
            Random rand = new Random();
            int setscalecountInthisTest = runScaleCount;
            for (int i = 0; i < setscalecountInthisTest; i++)
            {
                getSerialData.Add(500 + rand.Next(0, 9) + rand.NextDouble());
            }
            if (true == checkBox_Reverse.IsChecked) getSerialData.Reverse();
            if (true == checkBox_SideCheck.IsChecked) ApplySelectSide();

            UpdataSerialDataWightScale(setscalecountInthisTest, 1);
            UPdataSeiralDataToGridAndDB(1);
        }

        int settest_k = 0;
        /// <summary>
        /// 테스트 버튼
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_Click_1(object sender, RoutedEventArgs e)
        {    
            Random rand = new Random();
            int setscalecountInthisTest = runScaleCount;
            if (true == checkBox_2Scale1Result.IsChecked ) setscalecountInthisTest = runScaleCount;           
           

            for (int i = 0; i < setscalecountInthisTest; i++)
            {
                getSerialData.Add(500 + rand.Next(0, 9) + rand.NextDouble());
            }
            if (true == checkBox_Reverse.IsChecked) getSerialData.Reverse();
            if (true == checkBox_SideCheck.IsChecked) ApplySelectSide();

            UpdataSerialDataWightScale(setscalecountInthisTest);
            UPdataSeiralDataToGridAndDB(1);
           
           
            //220617 
            if (settest_k >= 1) settest_k = 0;
            else settest_k = 1;
        }

        private void btn_Setting_Click(object sender, RoutedEventArgs e)
        {
            //if (mThreadSerial.IsBusy)
            //{
            //    mThreadSerial.CancelAsync();
            //    button_Start.Content = "START";
            //}
            threadStopforOntherWorker();

            string filePath = System.IO.Directory.GetCurrentDirectory();
          //  MakefileByte(filePath);
            filePath = string.Format(filePath + "\\" + "Setting.ini");
            System.Diagnostics.Process.Start(filePath);
    
            LocalDataManager dbdata = LocalDataManager.getInstance();
            if ("itxtest" == text_UserID_Copy.Text)
            {
                LogManager.getInstance().writeLog(string.Format("Product_PLAN Table Clear"));
                dbdata.Truncatetable("PRODUCT_PLAN");
            }            
        }

        private void btn_Home_Click(object sender, RoutedEventArgs e)
        {
            //0625
            //if (mThreadSerial.IsBusy)
            //{
            //    mThreadSerial.CancelAsync();
            //    button_Start.Content = "START";
            //}
            threadStopforOntherWorker();

            LocalDataManager dbdata = LocalDataManager.getInstance();
            ClearDataGrid();
            banbookDatainsert = 0;
            banbookDatainsert2nd = 0;
            onecyclepass2nd = false;
            onecyclepass = false;

            int check = LocalResultTableReaminDataNotQMS();
            if(1 > check)
            {               
                dbdata.Truncatetable("SPIN_WEIGHT_RESULT");
                MessageBox.Show("RESULT Table Clear");
                LogManager.getInstance().writeLog(string.Format("RESULT Table Clear _ Truncatetable to SPIN_WEIGHT_RESULT"));
            }

            if("itxtest" == text_UserID_Copy.Text)
            {
                LogManager.getInstance().writeLog(string.Format("RESULT Table ALL Clear"));
                dbdata.Truncatetable("SPIN_WEIGHT_RESULT");
                dbdata.Truncatetable("WEIGHT_SPEC");
                dbdata.Truncatetable("LOT_END");
                dbdata.Truncatetable("PRODUCT_PLAN");
            }
        }

        #region 컨트롤 변환 및 갱신

        private void SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProductDataPicker.SelectedDate.HasValue)
            {
                CurrentProductDate = ProductDataPicker.SelectedDate.Value.ToString("yyyyMMdd");
                //여기서 QMS데이터와 PLANTID  가지고 LOT을 지정 해줍시다. 
                InitComboBox();
                LineComboUpdata(CurrentProductDate);
            }
        }

        private void LineComboUpdata(string tempDate)
        {
            //받은 날짜랑 공장ID를 가지고 ProductPlan 의line 정보 부터 가져와야함 
            // 순서가 라인(호기) -> 롯트 -> 포스 ->측 이렇게 되는것
            LocalDataManager dbdata = LocalDataManager.getInstance();
            DataSet dataset = dbdata.GetProductPlanDataForLocal(plantid.ToString(), tempDate);

            if(dataset != null)
            {     
                List<String> strLine = new List<string>();

                foreach (DataTable table in dataset.Tables)
                {
                    foreach (DataRow row in table.Rows)
                    {
                        strLine.Add(row[3].ToString());
                    }
                }
                strLine = strLine.Distinct().ToList();
                comboBox_HO.Items.Clear();
                comboBox_POS.Items.Clear();
                comboBox_SIDE.Items.Clear();
                //comboBox_LOT.Items.Clear();

                strLine.Sort(compare);
                int i = 0;
                foreach (string temp in strLine)
                {                    
                    if (LineSetParam.Count > i)
                    {
                        if (temp == LineSetParam[i])
                        {
                            comboBox_HO.Items.Add(temp);
                            i++;
                        }
                    }
                }
            }
        }

        public int compare(string x, string y)
        {
            return x.CompareTo(y);
        }

        #region LocalDB에 SET 하는 부분들
        private void Set_ProductPlan()
        {
            try
            {
                //날짜를 선택해서 넣어가지고 DB를 가져와야함
                QMSDataManager qmsdbdata = QMSDataManager.getInstance();
                LocalDataManager localdbdata = LocalDataManager.getInstance();

                CurrentProductDate = ProductDataPicker.SelectedDate.Value.ToString("yyyyMMdd");
                DataSet dataset = new DataSet();

                if (true == bQmsdb_Connect)
                {
                    LogManager.getInstance().writeLog("in set productplan");
                    //라스트 import 두달 전으로 진행
                    DateTime dtDate;
                    dtDate = DateTime.ParseExact(LastImportDate, "yyyyMMdd", null);
                    LastImportDate = dtDate.AddDays(-60).ToString("yyyyMMdd");
                    LogManager.getInstance().writeLog("latimportdate " + LastExportDate.ToString());
                    dataset = qmsdbdata.GetProductPlanDataForQMS(plantid.ToString(), CurrentProductDate, LastImportDate);
                    LogManager.getInstance().writeLog("insert local productplan");
                    localdbdata.InsertProductPlan(dataset);
                }
                //DataSet dataset2 = new DataSet();
                //dataset2 = localdbdata.GetProductPlanDataForLocal(plantid.ToString(), CurrentProductDate);
                //dataset 을 이제 localDB에 넣을 차례임
                LastImportDate = CurrentProductDate;

                SetImportQmsTime();
            }
            catch(Exception e)
            {
                LogManager.getInstance().writeLog(e.ToString());
            }
            
        }

        /// <summary>
        /// Data grid를 가지고 결과 테이블에 넣습니다. 
        /// 검사 후 수정 사항을 반영하기 위함
        /// </summary>
        /// <param name="gridview"></param>
        private void Set_ResultTable(DataGrid gridview)
        {

        }

        private void Set_USERID()
        {
            QMSDataManager qmsdbdata = QMSDataManager.getInstance();
            LocalDataManager localdbdata = LocalDataManager.getInstance();

            DataSet dataset = qmsdbdata.GetUserID(plantid.ToString());
            localdbdata.InsertUserID(dataset);
        }

        private void Set_Weight_SPEC()
        {
            QMSDataManager qmsdbdata = QMSDataManager.getInstance();
            LocalDataManager localdbdata = LocalDataManager.getInstance();

            DataSet dataset = qmsdbdata.GetSpinWeightSpec(plantid.ToString(), LastImportDate);
            try
            {
                localdbdata.InsertSpinWeightSpec(dataset);
            }
            catch(Exception ex)
            {
                ex.ToString();
            }            
            LastImportDate = CurrentProductDate;
        }

        #endregion

        private void comboBox_HO_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LocalDataManager localDB = LocalDataManager.getInstance();

            if (comboBox_HO.SelectedItem != null)
            {
                DataSet dataset = localDB.GetLotForLocal(plantid.ToString(), CurrentProductDate, comboBox_HO.SelectedItem.ToString());

                glineid = comboBox_HO.SelectedItem.ToString();
                //comboBox_LOT.Items.Clear();
                comboBox_POS.Items.Clear();
                comboBox_SIDE.Items.Clear();                
                List<string> strLotAdd = new List<string>();
                if (dataset.Tables.Count > 0)
                {
                    foreach (DataRow row in dataset.Tables[0].Rows)
                    {
                        strLotAdd.Add(row[0].ToString());                      
                    }
                    strLotAdd = strLotAdd.Distinct().ToList();
                }
            }
        }

        private void comboBox_LOT_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //  LOT을 바꿔서 저장해주면 LINE을 지정해줘야함(호) 인거 같음
            LocalDataManager localDB = LocalDataManager.getInstance();
        }

        private void comboBox_POS_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            return;
            LocalDataManager localDB = LocalDataManager.getInstance();

            //string tempDate = ProductDataPicker.SelectedDate.Value.ToString("yyyyMMdd");
            string lot_seq = "1";
            if (comboBox_POS.SelectedItem != null)
            {
                if (true == checkBox_TestLot.IsChecked)
                {
                    //End_qty = Set_Scale;
                    //runScaleCount = Set_Scale;
                    End_qty = UseScaleCount;
                    runScaleCount = UseScaleCount;
                    return;
                }
                DataSet dataset = localDB.GetSideForLocal(plantid.ToString(), CurrentProductDate, comboBox_HO.SelectedItem.ToString(), CurrentLot, comboBox_POS.SelectedItem.ToString());                            
                comboBox_SIDE.Items.Clear();
                NextStepSide.Clear();
                NextStartEnd.Clear();
                StartEndCount.Clear();

                List<string> TempSide = new List<string>();
                List<string> TempStartend = new List<string>();
                List<string> tempLotseq = new List<string>();
                List<string> endjust = new List<string>();
                startPosCount = comboBox_POS.SelectedIndex;
                sideCount = 0;
                try
                {
                    if (dataset.Tables.Count > 0)
                    {
                        foreach (DataRow row in dataset.Tables[0].Rows)
                        {
                            comboBox_SIDE.Items.Add(row[0].ToString());
                            TempSide.Add(row[0].ToString());
                            End_qty = Convert.ToInt32(row[1]);
                            TempStartend.Add(row[2].ToString());
                            tempLotseq.Add(row[3].ToString());
                            endjust.Add(row[1].ToString());
                            lot_seq = row[3].ToString();
                        }
                        tempLotseq = tempLotseq.Distinct().ToList();
                        //이러면 LOT SEQ 중복임
                        if (tempLotseq.Count != 1)
                        {
                            string tempmessage = string.Format("Lot seq  YES = {0} NO = {1} \n" +
                                "{0} -  END Count {2}" +
                                "{1} -  END Count {3}" +
                                "관리자 문의 바람", tempLotseq[0], tempLotseq[1], endjust[0], endjust[1]);
                            var yesorno = MessageBox.Show(tempmessage, "YesOrNo", MessageBoxButtons.YesNo);
                            if (System.Windows.Forms.DialogResult.Yes == yesorno)
                            {
                                lot_seq = tempLotseq[0];
                            }
                            else
                            {
                                lot_seq = tempLotseq[1];
                            }
                        }

                        glot_seq = Convert.ToInt32(lot_seq);
                        //end qty 만들기 전에 테이블 만들기
                        MakeLineEndData(lot_seq);
                        SingleEnd.Clear();
                        SingleSide.Clear();
                        SetLotAndEnd.Clear();
                        //LOT_END 에 넣은걸 활용 하자
                        //END_ID, SIDE, END_SIDE 를 가져옴
                        DataSet endData = localDB.GetLotEnd();

                        Dictionary<string, string> endDic = new Dictionary<string, string>();
                        if (endData.Tables.Count > 0)
                        {
                            foreach (DataRow row in endData.Tables[0].Rows)
                            {
                                string dickey = string.Format(row[3].ToString() + row[0].ToString());
                                //키를 END_ID이고 END_SIDE를 가져옴
                                if (false == endDic.ContainsKey(dickey))
                                {
                                    //0705 하나의 POS에 두개 LOT들어간 애들 보면 ENDID가 겹쳐서 딕셔너리 키값으로 중복됨
                                    // 그래서 LOT 까지 만들어서 넣어주고 나중에 ENDID 쓸때 LOT string 파싱해서 짤라쓰자
                                    endDic.Add(dickey, row[1].ToString());
                                    SingleEnd.Add(row[0].ToString());
                                    SingleSide.Add(row[1].ToString());
                                    //두개 한묶음으로 쓸때 필요한 Side랑 LOT 랑 구분하는 딕셔너리 만듬
                                    if (false == SetLotAndEnd.ContainsKey(row[1].ToString()))
                                    {
                                        SetLotAndEnd.Add(row[1].ToString(), row[3].ToString());
                                    }
                                }
                            }
                        }                        
                        var tempend = endDic.Values.Distinct().ToList();

                        //사이드의 갯수 확인
                        int tempsidenum = tempend.Count();
                        comboBox_SIDE.Items.Clear();
                        List<string> strSideAdd = new List<string>();
                        //end 사이드로 분류해서 나누고싶다     
                        for (int i = 0; i < tempsidenum; i++)
                        {
                            List<string> tempside =
                                (from sidetemp in endDic
                                 where sidetemp.Value == tempend[i]
                                 select sidetemp.Key.ToString().Substring(5)
                                 ).ToList();

                            MakeStartEnd(tempside, tempside.Count(), tempend[i]);
                            strSideAdd.Add(tempend[i]);

                            End_qty = tempside.Count();
                        }
                        strSideAdd = strSideAdd.Distinct().ToList();

                        //사이드가 수량으로 나눠져있으면
                        if(NextStepSide.Count > tempsidenum) sideDiv = true;
                        else sideDiv = false;

                        if (localDB.CheckGumiExceptionLot(CurrentLot, out string notthing))
                        {
                            NextStepSide.Sort();
                            NextStartEnd.Sort();                            
                        }

                        foreach (var input in NextStepSide)
                        {
                            comboBox_SIDE.Items.Add(input);
                        }

                        SetDofComboBox();


                        LogManager.getInstance().writeLog("GET SPEC" + plantid.ToString() + "LOT = " + CurrentLot + "LOT_SEQ = " + lot_seq.ToString());
                        DataSet dataspec = localDB.GetSpinWeightSpec_Local(plantid.ToString(), CurrentLot, lot_seq);

                        if (dataspec.Tables.Count > 0)
                        {
                            foreach (DataRow row in dataspec.Tables[0].Rows)
                            {
                                label_WeightMin.Content = Convert.ToDouble(row[2]).ToString("F1");
                                Text_WeightMin.Text = Convert.ToDouble(row[2]).ToString("F1");
                                label_SL.Content = Convert.ToDouble(row[1]).ToString("F1");
                                label_WeightMax.Content = Convert.ToDouble(row[0]).ToString("F1");
                                Text_WeightMax.Text = Convert.ToDouble(row[0]).ToString("F1"); ;
                                label_ErrorRange.Content = row[7]?.ToString() ?? "";

                                gUSL = Convert.ToDouble(row[0]);
                                gSL = Convert.ToDouble(row[1]);
                                gLSL = Convert.ToDouble(row[2]);
                                gUCL = Convert.ToDouble(row[3]);
                                gCL = Convert.ToDouble(row[4]);
                                gLCL = Convert.ToDouble(row[5]);
                                gMark = row[6].ToString();
                               double setpan = gUSL + gSL + gLSL + gCL;
                                if (5 > setpan && 1 == dataspec.Tables.Count)
                                {
                                    MessageBox.Show("QMS has no Spec Data", "YesOrNo", MessageBoxButtons.YesNo);
                                    hasNoSpec = true;
                                }
                                if (gMark == "~") SL_tolerance = 0.0;
                                else SL_tolerance = Convert.ToDouble(row[7].ToString());
                                hasNoSpec = false;
                                
                            }
                        }
                        else
                        {
                            //스펙 등록된 자료가 0이면
                            MessageBox.Show("QMS has no Spec Data", "YesOrNo", MessageBoxButtons.YesNo);
                            hasNoSpec = true;
                        }
                    }
                    currentposindex = comboBox_POS.SelectedIndex;
                    startPosCount = currentposindex;
                }
                catch(Exception ex)
                {
                    LogManager.getInstance().writeLog(ex.ToString());
                    //스펙 등록된 자료가 0이면
                    MessageBox.Show("QMS has no Spec Data", "YesOrNo", MessageBoxButtons.YesNo);
                    hasNoSpec = true;
                }

            }
            //POS 까지 선택하면 grid 를 바꿔보자
            //setGridMax(End_qty);
            setGridMax(runScaleCount);
        }

        private void MakeLineEndData(string lotseqset)
        {
            LocalDataManager localDB = LocalDataManager.getInstance();
            DataSet dataset = localDB.GetPOS_END(plantid.ToString(), CurrentProductDate, comboBox_POS.SelectedItem.ToString(), comboBox_HO.SelectedItem.ToString(), lotseqset, CurrentLot); 
            localDB.InsertLineEndData(dataset);
        }

        private void MakeStartEnd(string startend, int endqty, bool halfcount = false)
        {          
            int i = startend.Length;            
            string startendChek = startend.Substring(0, i - 2);
            int startendNumber = Convert.ToInt32(startend.Substring(i - 2));
            string temp = "";

            if (true == halfcount)
            {            
                int halfqty = endqty / 2;
                temp = string.Format("{0}{1}~{2}{3}", startendChek, startendNumber, startendChek, (startendNumber + halfqty)-1);
                NextStartEnd.Add(temp);

                temp = string.Format("{0}{1}~{2}{3}", startendChek, (startendNumber + halfqty), startendChek, (startendNumber + endqty)-1);
                NextStartEnd.Add(temp);              
            }
            else
            {        
                 temp = string.Format("{0}{1}~{2}{3}", startendChek, startendNumber, startendChek, (startendNumber + endqty-1));
                 NextStartEnd.Add(temp);
            }            
        }

        private void MakeStartEnd(List<string> startend, int endqty, string side)
        {            
            string temp = "";

            //int localsetscale = Set_Scale;
            int localsetscale = UseScaleCount;
            /// 여기서 Ingnore Last 옵션을 적용 해야 할 타이밍인거 같음
            /// 
            if (true == checkBox_IgnoreLast.IsChecked) localsetscale += -1;
            //2Scale 관련해서 측정하는 법
            if (true == checkBox_2Scale1Result.IsChecked) localsetscale = 4 / 2;

            int div = endqty / localsetscale;
            int namuji = endqty % localsetscale;

            if (endqty > localsetscale)
            {
                if(localsetscale >= endqty / 2)
                {
                    div = 2;
                    runScaleCount = endqty / 2;
                    namuji = endqty % runScaleCount;
                }
                else
                {
                    if(localsetscale >= endqty / 3)
                    {
                        div = 3;
                        runScaleCount = endqty / 3;
                        namuji = endqty % runScaleCount;
                    }
                    else if (localsetscale >= endqty / 4)
                    {
                        div = 4;
                        runScaleCount = endqty / 4;
                        namuji = endqty % runScaleCount;
                    }
                }
                //이게 12개짜리 저울에서 16end 제품을 생산할때 8개씩 나눠서 측정 하는것을 적용 하기 위함 
                // 나머지가 나오는 측정 방식은없음 
               // Set_Scale = runScaleCount;
               
                for (int i = 0; i < div; i++)
                {
                    if(i == div - 1)
                    {
                        temp = string.Format("{0}~{1}", startend[i * runScaleCount], startend[((i * runScaleCount) + runScaleCount) - 1]);
                        NextStartEnd.Add(temp);
                        StartEndCount.Add(runScaleCount);
                        NextStepSide.Add(side + (i+1).ToString());

                        //if (0 < namuji)
                        //{
                        //    temp = string.Format("{0}~{1}", startend[((i * runScaleCount) + runScaleCount)], startend[startend.Count() - 1]);
                        //    NextStartEnd.Add(temp);
                        //    StartEndCount.Add(namuji);
                        //    NextStepSide.Add(side + (i+1).ToString());
                        //}
                    }
                    else
                    {
//                        temp = string.Format("{0}~{1}", startend[i * localsetscale], startend[((i * localsetscale) + localsetscale) - 1]);

                        temp = string.Format("{0}~{1}", startend[i * runScaleCount], startend[((i * runScaleCount) + runScaleCount) - 1]);
                        NextStartEnd.Add(temp);
                        StartEndCount.Add(runScaleCount);
                        NextStepSide.Add(side + (i+1).ToString());
                    }                    
                }
            }
            else if(endqty <= localsetscale)
            {
                temp = string.Format("{0}~{1}", startend[0], startend[startend.Count() - 1]);
                NextStartEnd.Add(temp);
                StartEndCount.Add(endqty);
                NextStepSide.Add(side);
                
                runScaleCount = namuji;
                if (endqty == localsetscale)  runScaleCount = localsetscale;
            }

            //if (true == halfcount)
            //{
            //    int halfqty = endqty / 2;
            //    temp = string.Format("{0}~{1}", startend[0], startend[(startend.Count() / 2) - 1]);
            //    NextStartEnd.Add(temp);

            //    temp = string.Format("{0}~{1}", startend[(startend.Count() / 2)], startend[startend.Count()- 1]);
            //    NextStartEnd.Add(temp);
            //}
            //else
            //{
              
            //}
        }
        /// <summary>
        /// 컴포넌트 initialize
        /// </summary>
        private void SetDofComboBox()
        {
            for (int i = 1; i < 81; i++)
            {
                comboBox_DOF.Items.Add(i.ToString()); ;
            }
        }
        #endregion

        private void loadSettingIni()
        {
            string iniFileFullPath = System.IO.Directory.GetCurrentDirectory() + "\\Setting.ini";
            try
            {
                if (System.IO.File.Exists(iniFileFullPath))
                {
                    SerialComport = GetIniValue(iniFileFullPath, "RS232C SETTING", "CommPort");
                    SerialSpeed = Convert.ToInt32(GetIniValue(iniFileFullPath, "RS232C SETTING", "Speed"));
                    plantid = Convert.ToInt32(GetIniValue(iniFileFullPath, "PLANT_INFO", "PlantID"));

                    for(int i = 0; i < 25; i++)
                    {
                        string Lineset = string.Format("Line{0}", i + 1);                        
                        glineidSet.Add(GetIniValue(iniFileFullPath, "PLANT_INFO", Lineset));
                        LineSetParam.Add(GetIniValue(iniFileFullPath, "PLANT_INFO", Lineset));
                    }                    
                    
                    Set_Scale = Convert.ToInt32(GetIniValue(iniFileFullPath, "PLANT_INFO", "ScaleCount"));
                    UseScaleCount = Convert.ToInt32(GetIniValue(iniFileFullPath, "PLANT_INFO", "UseScaleCount"));
                    twoway = Convert.ToInt32(GetIniValue(iniFileFullPath, "PLANT_INFO", "WeightLine"));
                    Debuglog = Convert.ToInt32(GetIniValue(iniFileFullPath, "PLANT_INFO", "DebugLog"));
                    inputDataCount = Set_Scale;
                    //SignalCount 6개저울 8개 들어오는게 하나가 아니어서 Signalcount를 만들어서 관리함 
                   // if (Set_Scale == 6) inputDataCount = 8;
                                        
                    if (0 == Convert.ToInt32(GetIniValue(iniFileFullPath, "PLANT_INFO", "Cas"))) CheckCas = false;
                    else CheckCas = true;

                    LastImportDate = GetIniValue(iniFileFullPath, "LAST DB DATA", "Import");
                    LastExportDate = GetIniValue(iniFileFullPath, "LAST DB DATA", "Export");

                    setlanguage = GetIniValue(iniFileFullPath, "Language", "SET");
                }
            }
            catch (Exception e)
            {
                LogManager.getInstance().writeLog(e.ToString()  + "loadSettingini");               
            }
        }

        private void SetImportQmsTime()
        {
            string iniFileFullPath = System.IO.Directory.GetCurrentDirectory() + "\\Setting.ini";

            try
            {
                if (System.IO.File.Exists(iniFileFullPath))
                {
                    SetIniValue(iniFileFullPath, "LAST DB DATA", "Import", LastImportDate);
                }
            }
            catch (Exception e)
            {
            }
        }

        private void SetExportQmsTime()
        {
            string iniFileFullPath = System.IO.Directory.GetCurrentDirectory() + "\\Setting.ini";

            try
            {
                if (System.IO.File.Exists(iniFileFullPath))
                {
                    SetIniValue(iniFileFullPath, "LAST DB DATA", "Export", LastExportDate);
                }
            }
            catch (Exception e)
            {
            }
        }

        [System.Runtime.InteropServices.DllImport("kernel32")]
        public static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        [System.Runtime.InteropServices.DllImport("kernel32")]
        public static extern int WritePrivateProfileString(string section, string key, string val, string filePath);

        public static String GetIniValue(string path, String Section, String Key)
        {
            StringBuilder temp = new StringBuilder();
            int i = GetPrivateProfileString(Section, Key, string.Empty, temp, 255, path);
            return temp.ToString();
        }

        public static void SetIniValue(string path, string Section, string Key, string value)
        {
            WritePrivateProfileString(Section, Key, value, path);
        }

        #region QMS 에서 SPEC 가져오기
        private void QMS_ImportData()
        {
            if (true == bQmsdb_Connect)
            {
                Set_ProductPlan();
                Set_Weight_SPEC();
                Set_USERID();
            }
        }

        private void GetWeightSpec_localDB(string PLANT_ID, string LOT, string LOT_SEQ)
        {
            //여기에 있는 정보로 로컬디비에서 쿼리날려서 셋팅값 가져오기
            LocalDataManager dbdata = LocalDataManager.getInstance();
            //LSL, USL, SL_TOLERANCE, SL
            DataSet specdata = dbdata.GetSpinWeightSpec_Local(PLANT_ID, LOT, LOT_SEQ);

            if (specdata.Tables.Count != 1)            {
                //이상하게 들어온것이야 
                foreach (DataTable table in specdata.Tables)
                {
                    foreach (DataRow row in table.Rows)
                    {
                        //설정하기 지금 검사하는 제품의 SPEC
                        //usl,sl,lsl,ucl,cl,lcl,mark   
                    }
                }
            }

            else if (specdata.Tables.Count == 0)
            {
                //결과가 확인이 안된것
            }
            else if (specdata.Tables.Count > 1)
            {
                //결과가 많이 나온것
            }
        }
        #endregion

        #region 데이터 가지고 판정하기 
        private string WeightPanjung(double weightdata)
        {
            string temp = "";
            //테스트 진행일때는 판정 여기서 바꿔버려
            if(true == checkBox_TestLot.IsChecked)
            {                
                if (Convert.ToDouble(Text_WeightMin.Text) <= weightdata && weightdata <= Convert.ToDouble(Text_WeightMax.Text)) temp = "AA";
                else temp = "AB";
                return temp;
            }

            //+- 일때
            if(gMark == "±")
            {
                if (gLSL <= weightdata && weightdata <= gUSL) temp = "AA";
                else temp = "AB";
            }
            else if (gMark == "↑")
            {
                //UP일때
                if (gSL <= weightdata) temp = "AA";
                else temp = "AB";
            }
            else if( gMark == "↓")
            {
                //DOWN일떄
                if (gUSL >= weightdata) temp = "AA";
                else temp = "AB";
            }
            else if (gMark == "")
            {
                temp = "AA";
            }
            else if (gMark == "~")
            {
                if (gUSL >= weightdata && gLSL <= weightdata) temp = "AA";
                else temp = "AB";
            }
            //스펙이없지? 그럼 AA야
            if (true == hasNoSpec) temp = "AA";
            
            return temp;
        }
        #endregion

        /// <summary>
        /// 그리드 뷰 index 가지고 columns 조절하기
        /// </summary>
        /// <param name="endqty"></param>
        private void setGridMax(int endqty)
        {
            //앞에 컬럼들 빼고
            int Count = dataGrid.Columns.Count;
            int setwidth = 56;
            if (endqty >= 16 && endqty < 12) setwidth = 55;
            //16개면 8개로 나눠야함
            if(endqty > Set_Scale)
            {
                if (endqty == 16)
                {
                    endqty = 8;
                }
            }            
            endqty = endqty + 6;
            for (int i = 0; i < Count; i ++)
            {
                dataGrid.Columns[i].Visibility = Visibility.Visible;
                dataGrid.Columns[i].Width = setwidth;
            }

            for (int i = 0; i < (Count - endqty);  i++)
            {
                dataGrid.Columns[(Count - 1)  - i].Visibility = Visibility.Hidden;
            }            
        }
        private string ReturnNextPos(int index)
        {
            if(NextStepPos.Count > index)
            {
                return NextStepPos[index].ToString();
            }
            else
            {

            }
            return "";
        }      

        #region Excel Export 함수 구간
        private void InitExcelDocumentData()
        {
            //현재 데이터를 가져옴
          
            //double rangeMax = double.Parse(label_WeightMax.Content.ToString());
            //double rangeMin = double.Parse(label_WeightMin.Content.ToString());
            double rangeMax = double.Parse(Text_WeightMax.ToString());
            double rangeMin = double.Parse(Text_WeightMin.ToString());
            double rangeEr = double.Parse(label_ErrorRange.Content.ToString());

            string MinMaxRange = string.Format("{0:.0}g ~ {1:.0}g", rangeMax, rangeMin);
            string ErrRange = string.Format("{0}", rangeEr);
            
            ExcelExport.ClearCommonData();

            //현재 작업중인 DATA를 가져옴
            ExcelExport.SetDataTableInfo(CurrentLot, MinMaxRange, ErrRange);
        }

        #endregion

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            this.LoTTable.Rows.Clear();
            this.dataGrid.ItemsSource = this.LoTTable.DefaultView;      
        }

        private void TextboxText()
        {         

        }
        
        private TextContents SetWeightValueTextbox(string tetx)
        {
            TextContents textitem = new TextContents();
            textitem.Contents = tetx;
            return textitem;
        }


        private void button_exportQMS_Click(object sender, RoutedEventArgs e)
        {
            //QMS 연결 안되어있어도 로컬 디비 가지고 spec 을 올려가지고 옵시다. 
            if (true == bQmsdb_Connect)
            {      
                LogManager.getInstance().writeLog("QMS DB Load");
                Set_ProductPlan();
                LogManager.getInstance().writeLog("set plan");
                Set_Weight_SPEC();
                //LogManager.getInstance().writeLog("end set spec");
                //Set_USERID();
            }
        }

        private void text_UserID_LostFocus(object sender, RoutedEventArgs e)
        {
            string temp = text_UserID_Copy.Text;

            CurrentLoginUserID = text_UserID_Copy.Text;
        }

        private void button_Start_Click(object sender, RoutedEventArgs e)
        {
            //inputDataCount = 0;
            banbookDatainsert = 0;
            banbookDatainsert2nd = 0;
            checkinputfirst = 0;
            if (true == checkBox_SideCheck.IsChecked) ApplySelectSide();
            runsendthread();
        }
        /// <summary>
        /// 엑셀 출력 버튼
        /// </summary>
        private void button_ExcelExport_Click(object sender, RoutedEventArgs e)
        {
            //0625
            //if (mThreadSerial.IsBusy)
            //{
            //    mThreadSerial.CancelAsync();
            //    button_Start.Content = "START";
            //}
            threadStopforOntherWorker();

            var result = MessageBox.Show(LogManager.getInstance().PopSaveNow, "notice", MessageBoxButtons.YesNo);
            
            if (System.Windows.Forms.DialogResult.Yes == result)
            {
                try
                {
                    ExportExcel export = new ExportExcel();
                    MakeExcelCommonData commondata = new MakeExcelCommonData();

                    if (true == checkBox_TestLot.IsChecked) CurrentLot = string.Format(text_Lotinput.Text + "_TEST");
                    
                    //line range lot data 를 가지고 만듬                               
                    string strrag = string.Format("{0}±{1}", label_SL.Content.ToString(), label_ErrorRange.Content.ToString());

                    //string strmax = label_WeightMax.Content.ToString();
                    //string strmin = label_WeightMin.Content.ToString();
                    string strmax = Text_WeightMax.Text;
                    string strmin = Text_WeightMin.Text;

                    var tempside = SingleSide.Distinct().ToList();
                    var endcount = Set_Scale;
                    
                    commondata.makeCommonData(glineid, strrag, CurrentLot, CurrentProductDate, comboBox_DOF.SelectedItem.ToString(), strmin, strmax, NextStartEndForExcel, Set_Scale, SideChangeLogic(), SingleSide, glot_seq);

                    List<string> temppos = new List<string>();
                    foreach(var item in comboBox_POS.Items)
                    {
                        temppos.Add(item.ToString());
                    }
                    
                    export.Export_Excel(table, Set_Scale, runScaleCount, commondata, tempside, temppos, endcount, CurrentProductDate);
                }
                catch(Exception ex)
                {
                    LogManager.getInstance().writeLog(ex.ToString());
                }               
            }     
        }

        private bool CheckComboboxData()
        {
            bool rt = true;
            if (-1 == comboBox_HO.SelectedIndex) return false;
            if (-1 == comboBox_POS.SelectedIndex) return false;
            if (-1 == comboBox_SIDE.SelectedIndex) return false;
            if (-1 == comboBox_DOF.SelectedIndex) return false;
            return rt;
        }
        /// <summary>
        /// 종료 버튼
        /// </summary>
        private void MainWindowView_Closed(object sender, EventArgs e)
        {            
            if (isthreadSendSerialIdle)
            {
                runsendthread();
                threadSendSerial.Join();
                Thread.Sleep(1000);
            }

            int scaleocnt = 1;
            if (CheckCas == true) scaleocnt = Set_Scale;
            if (twoway == 2) scaleocnt = 2;
            for (int i = 0; i < scaleocnt; i++)
            {
                serialData[i].CloseComm();
            }

            QMSDataManager QMSdb = QMSDataManager.getInstance();
            QMSdb.SetCloseQMSDB();
            Thread.Sleep(100);
            LocalDataManager LOCALdb = LocalDataManager.getInstance();
            LOCALdb.SetCloseLocalDB();
            Thread.Sleep(100);         
            this.Close();
            //Environment.Exit(0);
            System.Diagnostics.Process.GetCurrentProcess().Kill();

        }        

        /// <summary>
        /// Local Result에 입력하는 부분
        /// Excel export 전에 Datagrid 에서 데이터 가져다가 넣어야 함
        /// </summary>
        private void UploadLocalDbResultTable()
        {
            LocalDataManager localdbdata = LocalDataManager.getInstance();
            /// DataGrid에서 자료 긁어오기             /// 
            /// 
            int icount = 0;
            try
            {
                DataTable dt = new DataTable();
                dt = ((DataView)dataGrid.ItemsSource).ToTable();

                for (int i = 0; i < dataGrid.Items.Count; i++)
                {
                    DataRowView item = this.dataGrid.Items[i] as DataRowView;
                    if (item != null)
                    {
                        QMS_SpinWeightResult inputresut = new QMS_SpinWeightResult();
                        //공통 부분은 한줄에 만들어놓고 
                        // Data 값을 바꿔가면서 insert 하는 방식으로 구현
                        inputresut.Product_Date = item.Row.ItemArray[0].ToString();
                        inputresut.Plant_Id = plantid.ToString();
                        inputresut.Line_Id = item.Row.ItemArray[1].ToString();
                        inputresut.Doff = Convert.ToInt32(item.Row.ItemArray[2]);
                        inputresut.Pos = item.Row.ItemArray[3].ToString();
                        inputresut.Lot = CurrentLot;
                        string templot = "";
                        if(localdbdata.CheckGumiExceptionLot(CurrentLot, out templot))
                        {
                            //딕셔너리에서 키값으로 LOT 뽑아내기
                            SetLotAndEnd.TryGetValue(item.Row.ItemArray[4].ToString(), out templot);
                            inputresut.Lot = templot;
                        }
                        inputresut.Lot_seq = glot_seq.ToString();
                        inputresut.Side = item.Row.ItemArray[4].ToString();
                        inputresut.Spec_color = "";
                        inputresut.Created_by = CurrentLoginUserID;
                        inputresut.Modified_by = CurrentLoginUserID;
                        inputresut.Usl = Convert.ToDecimal(gUSL);
                        inputresut.Sl = Convert.ToDecimal(gSL);
                        inputresut.Lsl = Convert.ToDecimal(gLSL);
                        inputresut.Ucl = Convert.ToDecimal(gUCL);
                        inputresut.Cl = Convert.ToDecimal(gCL);
                        inputresut.Lcl = Convert.ToDecimal(gLCL);
                        inputresut.Mark = gMark;                        
                        string[] endid = item.Row.ItemArray[5].ToString().Split('~');
                        int endindex = Convert.ToInt32(endid[0].Substring((endid[0].Length - 2), (endid[0].Length - 1)));
                        int gridlenght = 6 + Set_Scale;

                        if (true == checkBox_IgnoreLast.IsChecked) gridlenght += -1;

                        for (int j = 6; j < gridlenght; j++)
                        {
                            if(!string.IsNullOrEmpty(item.Row.ItemArray[j].ToString()))
                            {
                                inputresut.Value = Convert.ToDecimal(item.Row.ItemArray[j]);
                                inputresut.Decision_id = WeightPanjung(Convert.ToDouble(item.Row.ItemArray[j]));
                                if (null != endid[0])
                                {
                                    inputresut.End_Id = string.Format("{0}{1}", endid[0].Substring(0, (endid[0].Length - 2)), (endindex + (j - 6)).ToString("D2"));
                                }
                                if (true == localdbdata.InsertSpinWeightResult(inputresut, 0)) icount += 1;
                            }                            
                        }
                    }
                }

                LogManager.getInstance().writeLog(string.Format("LocalDataBase Updata {0}EA",icount));
            }
            catch(Exception ex)
            {
                LogManager.getInstance().writeLog(ex.ToString());
            }

        }

        /// <summary>
        /// Datagrid 에 값 갱신 할때 판정에 따른 색 변경을 위한 이벤트
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            FrameworkElement el;
            int indexno = e.Row.GetIndex();
            DataRowView item = this.dataGrid.Items[indexno] as DataRowView;
            var GridrowStyle = new Style { TargetType = typeof(DataGridRow) };
            SolidColorBrush brush = new SolidColorBrush(Colors.Black);

            int gridlenght = 6 + Set_Scale;

            if (true == checkBox_IgnoreLast.IsChecked) gridlenght += -1;
            try
            {
                if (item != null)
                {
                    if (item.Row.ItemArray.Length > 5)
                    {
                        for (int i = 6; i < gridlenght; i++)
                        {
                            el = this.dataGrid.Columns[i].GetCellContent(e.Row);               
                            if (el == null) return;
                            DataGridCell changeCell = GetParent(el, typeof(DataGridCell)) as DataGridCell;

                            double d1 = Convert.ToDouble(item.Row.ItemArray[i]);
                            string temp = WeightPanjung(d1);
                            if ("AA" != temp)
                            {
                                brush = new SolidColorBrush(Colors.Red);
                                changeCell.Background = brush;
                                brush = new SolidColorBrush(Colors.White);
                                changeCell.Foreground = brush;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
        }

        private FrameworkElement GetParent(FrameworkElement child, Type targetType)
        {
            object parent = child.Parent;

            if (parent != null)
            {
                if (parent.GetType() == targetType)
                {
                    return (FrameworkElement)parent;
                }
                else
                {
                    return GetParent((FrameworkElement)parent, targetType);
                }
            }
            return null;
        }

        private void comboBox_SIDE_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBox_SIDE.SelectedItem != null)
            {
                string strside = comboBox_SIDE.SelectedItem.ToString();
                if(checkBox_SideCheck.IsChecked == true)
                {
                    //string strtemp = label_SideList.Content.ToString();
                    //if(strtemp == "")
                    //{
                    //    strtemp = string.Format(strside);
                    //}
                    //else
                    //{
                    //    strtemp = string.Format(strtemp + "," + strside);
                    //}                    
                    //label_SideList.Content = strtemp;
                }
               // startPosCount = 0;
                sideCount = 0;
            }
        }
        /// <summary>
        /// 2개의 저울에서 한개의 출력값으로만 사용하게
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void checkBox_2Scale1Result_Checked(object sender, RoutedEventArgs e)
        {

        }

        /// <summary>
        /// 마지막 한개는 입력값 사용안하도록 조절
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void checkBox_IgnoreLast_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void checkBox_TestLot_Checked(object sender, RoutedEventArgs e)
        {
            if (true == checkBox_TestLot.IsChecked)
            {
               // SetLotId_GetTestLot();
                Text_WeightMin.IsReadOnly = false;
                Text_WeightMax.IsReadOnly = false;
            }            
            else
            {
                Text_WeightMin.IsReadOnly = true;
                Text_WeightMax.IsReadOnly = true;
            }
        }

        /// <summary>
        /// QMS에 업로드 되지 않고 남아있는 LOCAL RESULT 들의 갯수 
        /// </summary>
        /// <returns></returns>
        private int LocalResultTableReaminDataNotQMS()
        {
            int rt = 0;
            LocalDataManager localdbdata = LocalDataManager.getInstance();
            DataSet data = localdbdata.GetLocalResultTableRemainData();            
            if(data.Tables.Count > 0)
            {
                rt = data.Tables[0].Rows.Count;
            }            
            return rt;
        }

        private void checkBox_Reverse_Checked(object sender, RoutedEventArgs e)
        {
            if (checkBox_Reverse.IsChecked == true)
            {
                reverseArr = true;
            }
            else
            {
                reverseArr = false;
            }
        }
        /// <summary>
        /// QMS 업로드 버튼
        /// </summary>
        private void button_QMS_Upload_Click(object sender, RoutedEventArgs e)
        {
            if (true == checkBox_TestLot.IsChecked) return;

            LocalDataManager dbdata = LocalDataManager.getInstance();
            QMSDataManager qmsdata = QMSDataManager.getInstance();

            //0625
            //if (mThreadSerial.IsBusy)
            //{
            //    mThreadSerial.CancelAsync();
            //    button_Start.Content = "START";
            //}
            threadStopforOntherWorker();

            UploadLocalDbResultTable();

            if (!mThreadDbCommunication.IsBusy)
            {
                mThreadDbCommunication.RunWorkerAsync();
            }
            
            //LSL, USL, SL_TOLERANCE, SL
            if (true == qmsdata.isOpen())
            {
                MessageBox.Show("QMS DATA UPLOAD");
                //DataSet specdata = dbdata.GetSpinWeightResult();
                //int insertcount = qmsdata.InsertSpinWeightResultToQMS(specdata);                
                //string temp = string.Format("QMS DB Insert {0} EA", insertcount);
                //LogManager.getInstance().writeLog(temp);
            }
            else
            {
                MessageBox.Show(LogManager.getInstance().PopQmsDisconnected);
            }
        }
        /// <summary>
        /// ROW delete 버튼
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_DeleteROW_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Delete", "notice", MessageBoxButtons.YesNo);
            if (System.Windows.Forms.DialogResult.Yes == result)
            {
                dataGrid.ItemsSource = table.DefaultView;
                DataRowView dr = dataGrid.SelectedItem as DataRowView;
                DataRow dr1 = dr.Row;
                table.Rows.Remove(dr1);

                //무조건 마지막 열에서만 삭제한다고 가정해야한다
                if (sideCount == 0)
                {
                    startPosCount = startPosCount - 1;
                    sideCount = NextStepSide.Count;
                }
                else
                {
                    sideCount = sideCount - 1;
                }
            }            
        }

        private void button_Skip_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Skip", "notice", MessageBoxButtons.YesNo);
            if (System.Windows.Forms.DialogResult.Yes == result)
            {
            
                    for (int i = 0; i < runScaleCount; i++)
                    {
                        getSerialData.Add(0.00);
                    }
                    UPdataSeiralDataToGridAndDB();
             
                //if (!SideChangeLogic())
                //{
                //    sideCount += 1;
                //    if(NextStepSide.Count < sideCount)
                //    {
                //        sideCount = 1;
                //    }
                //}
                //else
                //{
                //    int checksidecount = NextStepSide.Count / comboBox_SIDE.Items.Count;

                //    if (sideCount < checksidecount - 1)
                //    {
                //        sideCount += 1;
                //    }
                //    else if (sideCount == checksidecount - 1)
                //    {
                //        sideCount = 0;
                //        startPosCount += 1;
                //    }
                //}
            }
        }

        private void checkBox_SideCheck_Checked(object sender, RoutedEventArgs e)
        {
            if(checkBox_SideCheck.IsChecked == false)
            {
                textBox_sideSelect.Text = "";
                SelectSide.Clear();
                SelectStartEnd.Clear();
            }
            else
            {
                //label_SideList.Content = "";
                textBox_sideSelect.Text = "";
                SelectSide.Clear();
                SelectStartEnd.Clear();
            }
        }
        /// <summary>
        /// 사이드 선택해서 하는 것을 START누를때 해야 하나
        /// </summary>
        private void ApplySelectSide()
        {
            NextStepSide.Clear();
            NextStepSide.AddRange(SelectSide);

            //기존의 nextStartEnd 에서 index를 가지고 복사 해놓기
            //Index 는 select 한 순서 대로 
            NextStartEnd.Clear();
            NextStartEnd.AddRange(SelectStartEnd);
    
        }

        private void text_Lotinput_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            string nlotiD = "";
            if(e.Key == System.Windows.Input.Key.Enter)
            {
                if (text_Lotinput.Text == "")
                {
                    MessageBox.Show("LOT");
                }
                else if( text_Lotinput.Text == "TEST")
                {
                    SetLotId_GetTestLot();
                }
                else
                {
                    if (null == comboBox_HO.SelectedItem) 
                    {
                        MessageBox.Show(LogManager.getInstance().PopErrorLine);
                        return;
                    }
                    LocalDataManager localDB = LocalDataManager.getInstance();
                    DataSet Lotdataset = localDB.GetLotForLocal(plantid.ToString(), CurrentProductDate, comboBox_HO.SelectedItem.ToString());
                    /// 입력된 값으로 실제 불러온 lot 화 정합성 확인 진행
                    if (null != Lotdataset)
                    {
                        if (Lotdataset.Tables.Count > 0)
                        {
                            if (Lotdataset.Tables[0].Rows.Count >= 1)
                            {
                                foreach (DataRow row in Lotdataset.Tables[0].Rows)
                                {
                                    if (text_Lotinput.Text == row[0].ToString())
                                    {
                                        nlotiD = text_Lotinput.Text;
                                        CurrentLot = nlotiD;
                                        SetLotId_GetTextBox(nlotiD);
                                    }
                                }
                                if("" == nlotiD) { MessageBox.Show(LogManager.getInstance().PopErrorLot); return; }
                            }
                            else {  MessageBox.Show(LogManager.getInstance().popupLot); }
                        }
                        else { MessageBox.Show(LogManager.getInstance().PopErrorLot); }
                    }         
                }
            }
        }

        /// <summary>
        /// 로트 아이디 로컬에서 가지고 오기 
        /// </summary>
        public void SetLotId_GetTextBox(string lotid)
        {
            LocalDataManager localDB = LocalDataManager.getInstance();
            DataSet dataset = localDB.GetPosForLocal(plantid.ToString(), CurrentProductDate, comboBox_HO.SelectedItem.ToString(), lotid);
            comboBox_POS.Items.Clear();
            comboBox_SIDE.Items.Clear();
            NextStepPos.Clear();
            CurrentLot = lotid;
            List<string> strPosAdd = new List<string>();
            if (dataset.Tables.Count > 0)
            {
                foreach (DataRow row in dataset.Tables[0].Rows)
                {
                    strPosAdd.Add(row[0].ToString());
                    NextStepPos.Add(row[0].ToString());
                }
                strPosAdd = strPosAdd.Distinct().ToList();
                NextStepPos = NextStepPos.Distinct().ToList();
                foreach (var input in strPosAdd)
                {
                    comboBox_POS.Items.Add(input);
                }
            }
        }

        /// <summary>
        /// TEST LOT 입력했을대 진행 하게 하는 부분
        /// </summary>
        public void SetLotId_GetTestLot()
        {
            comboBox_POS.Items.Clear();
            comboBox_SIDE.Items.Clear();
            NextStepPos.Clear();
            CurrentLot = "TEST";           

            for(int i = 1;  i < 31; i++)
            {
                comboBox_POS.Items.Add(i.ToString());
            }
            comboBox_SIDE.Items.Add("L");
            comboBox_SIDE.Items.Add("R");
            comboBox_SIDE.Items.Add("LL");
            comboBox_SIDE.Items.Add("LR");
            comboBox_SIDE.Items.Add("RL");
            comboBox_SIDE.Items.Add("RR");
        }

        /// <summary>
        /// 파일을 DB로 넘길 수 있도록 바이트 배열로 전환하기
        /// </summary>
        /// <param name="filepath"></param>
        public void MakefileByte(string filepath)
        {
            string temp = string.Format(filepath + "\\" + "SPX_Weight.exe");
            byte[] filebytes = null;
            var base64en = "";

            //filebytes = File.ReadAllBytes(temp);
            //base64en = Convert.ToBase64String(filebytes);
            //byte[] UFilebyte = new byte[0];
            //UFilebyte = Convert.FromBase64String(base64en);

            //FileStream fs = new FileStream(filepath + "\\" + "TEMP.exe", FileMode.OpenOrCreate, FileAccess.Write);
            //fs.Write(UFilebyte, 0, UFilebyte.Length);
            //fs.Close();
            DirectoryInfo di = new DirectoryInfo("C:\\Program Files (x86)");
            FileVersionInfo myFileVersionInfo;
            if (di.Exists)
            {
                myFileVersionInfo = FileVersionInfo.GetVersionInfo("C:\\Program Files (x86)\\HyosungITX\\ITX_WEIGHT\\SPX_WEIGHT.EXE");
            }
            else
            {
                myFileVersionInfo = FileVersionInfo.GetVersionInfo("C:\\Program Files\\HyosungITX\\ITX_WEIGHT\\SPX_WEIGHT.EXE");
            }            
            string temp2 = myFileVersionInfo.FileVersion;
        }
        /// <summary>
        /// 2줄 서비 관련해서 twoway 받아서 시리얼 앞에거만 자르고 뒤에거 남기고 하는것들에 대한 플래그
        /// </summary>
        /// <returns></returns>
        private dynamic SetTwoWayCountforSerialDatalist(List<double> serialdata, int loopcount)
        {
            var rt = new List<double>();
            int par = getSerialData.Count / 2;
            try
            {
                rt = getSerialData.ConvertAll(s => s);
                /*
                if (2 != twoway)
                {
                    rt = getSerialData.ConvertAll(s => s);
                }
                else
                {
                    rt = getSerialData.GetRange((par * loopcount), par);
                }
                */
                return rt;
            }
            catch (Exception e)
            {
                var t = e.ToString();
            }
            return rt;
        }

        private object objswap = new object();
        /// <summary>
        /// 들어온 데이터가 앞에들어갈 데이터인지 뒤에 들어갈 데이터 인지 봐서 스왑함       
        /// </summary>
        /// <param name="serialdata"></param>
        /// <param name="e"></param>
        private void SwapSerialData(Dictionary<int, double> serialdata, int check2ndLineData)
        {
            return;
            lock (objswap)
            {
                List<double> rt = new List<double>(serialdata.Values);
                if (getSerialData.Count == 0)
                {
                    getSerialData = new List<double>(dicSerialData.Values);
                }
                else if (getSerialData.Count != 0)
                {
                    if (0 == check2ndLineData)
                    {
                        //1번라인 데이터일 경우
                        rt.Reverse();
                        foreach (var element in rt)
                        {
                            getSerialData.Insert(0, element);
                        }
                    }
                    else if (1 == check2ndLineData)
                    {
                        //2번라인 데이터일 경우 1번이 있으면 뒤에넣고, 기존데이터가 있으면 지우고 넣기 
                        if (getSerialData.Count != 0)
                        {
                            getSerialData.AddRange(rt);
                        }
                        else if (getSerialData.Count == 0)
                        {
                            getSerialData.AddRange(rt);
                        }
                    }
                }
            }
        }

        private void label_Conncet_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (threadSendSerial.IsAlive)
                {
                    threadSendSerial.Abort();
          
                    Thread.Sleep(300);                    
                    MessageBox.Show(LogManager.getInstance().PopSerialThreadReset);                    
                }
            }
            catch(Exception ex)
            {
                LogManager.getInstance().writeLog(ex.ToString());
            }

            LogManager.getInstance().writeLog("connect label run sinc");

            int scaleocnt = 1;
            if (CheckCas == true) scaleocnt = Set_Scale;
            if (twoway == 2) scaleocnt = 2;
            LogManager.getInstance().writeLog("Close Comm");
            for (int i = 0; i < scaleocnt; i++)
            {
                serialData[i].CloseComm();
            }
        }

        public void runsendthread()
        {
            if(!isthreadSendSerialIdle)
            {
                isthreadSendSerialIdle = true;
                threadSendSerial = new Thread(new ThreadStart(threadsendData));
                LogManager.getInstance().writeLog("Start btn Start thread");
                button_Start.Content = "STOP";
                threadSendSerial.Start();
            }
            else
            {
                isthreadSendSerialIdle = false;
                LogManager.getInstance().writeLog("Start btn STOP thread");
                button_Start.Content = "START";
                if (Thread.CurrentThread != threadSendSerial)
                    threadSendSerial.Join();
            }
        }

        public void threadStopforOntherWorker()
        {
            if (isthreadSendSerialIdle)
            {
                isthreadSendSerialIdle = false;
                LogManager.getInstance().writeLog("threadStopforOntherWorker STOP thread");
                button_Start.Content = "START";
                if (Thread.CurrentThread != threadSendSerial)
                    threadSendSerial.Join();
            }
        }

        private void text_jikwan_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9.]+");
    
            e.Handled = regex.IsMatch(e.Text);
        }

        private void button_sideSet_Click(object sender, RoutedEventArgs e)
        {
            //NextStartEnd
            //체크 박스 트루 일때만
            if (true == checkBox_SideCheck.IsChecked)
            {
                string tempside = comboBox_SIDE.SelectedItem.ToString();
                int index = comboBox_SIDE.SelectedIndex;
                string tempend = NextStartEnd[index];
                SelectSide.Add(tempside);
                SelectStartEnd.Add(tempend);
                string makeside = "";
                textBox_sideSelect.Text = "";
                foreach (string side in SelectSide)
                {
                    makeside = string.Join(",", SelectSide.ToArray());
                }
                textBox_sideSelect.Text = makeside;
            }
        }
        /// <summary>
        /// Side Clear 버튼 초기화
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_SideClear_Click(object sender, RoutedEventArgs e)
        {
            var yesorno = MessageBox.Show("Clear", "YesOrNo", MessageBoxButtons.YesNo);
            if (System.Windows.Forms.DialogResult.Yes == yesorno)
            {             
                comboBox_SIDE.Items.Clear();
                textBox_sideSelect.Clear();
                SelectSide.Clear();
            }
            else
            {
                
            }
        }

        private void Text_WeightMin_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9.]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void Text_WeightMax_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9.]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        /// <summary>
        /// select Change 를 Drop 로 변경함 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void comboBox_POS_DropDownClosed(object sender, EventArgs e)
        {
            LocalDataManager localDB = LocalDataManager.getInstance();

            ///clr 
            comboBox_SIDE.Items.Clear();
            textBox_sideSelect.Clear();
            SelectSide.Clear();
            ///체크박스 해제
            checkBox_SideCheck.IsChecked = false;
            //string tempDate = ProductDataPicker.SelectedDate.Value.ToString("yyyyMMdd");
            string lot_seq = "1";
            if (comboBox_POS.SelectedItem != null)
            {
                if (true == checkBox_TestLot.IsChecked)
                {
                    //End_qty = Set_Scale;
                    //runScaleCount = Set_Scale;
                    End_qty = UseScaleCount;
                    runScaleCount = UseScaleCount;
                    return;
                }
                DataSet dataset = localDB.GetSideForLocal(plantid.ToString(), CurrentProductDate, comboBox_HO.SelectedItem.ToString(), CurrentLot, comboBox_POS.SelectedItem.ToString());
                comboBox_SIDE.Items.Clear();
                NextStepSide.Clear();
                NextStartEnd.Clear();
                StartEndCount.Clear();

                List<string> TempSide = new List<string>();
                List<string> TempStartend = new List<string>();
                List<string> tempLotseq = new List<string>();
                List<string> endjust = new List<string>();
                startPosCount = comboBox_POS.SelectedIndex;
                sideCount = 0;
                try
                {
                    if (dataset.Tables.Count > 0)
                    {
                        foreach (DataRow row in dataset.Tables[0].Rows)
                        {
                            comboBox_SIDE.Items.Add(row[0].ToString());
                            TempSide.Add(row[0].ToString());
                            End_qty = Convert.ToInt32(row[1]);
                            TempStartend.Add(row[2].ToString());
                            tempLotseq.Add(row[3].ToString());
                            endjust.Add(row[1].ToString());
                            lot_seq = row[3].ToString();
                        }
                        tempLotseq = tempLotseq.Distinct().ToList();
                        //이러면 LOT SEQ 중복임
                        if (tempLotseq.Count != 1)
                        {
                            string tempmessage = string.Format("Lot seq  YES = {0} NO = {1} \n" +
                                "{0} -  END Count {2}" +
                                "{1} -  END Count {3}" +
                                "관리자 문의 바람", tempLotseq[0], tempLotseq[1], endjust[0], endjust[1]);
                            var yesorno = MessageBox.Show(tempmessage, "YesOrNo", MessageBoxButtons.YesNo);
                            if (System.Windows.Forms.DialogResult.Yes == yesorno)
                            {
                                lot_seq = tempLotseq[0];
                            }
                            else
                            {
                                lot_seq = tempLotseq[1];
                            }
                        }

                        glot_seq = Convert.ToInt32(lot_seq);
                        //end qty 만들기 전에 테이블 만들기
                        MakeLineEndData(lot_seq);
                        SingleEnd.Clear();
                        SingleSide.Clear();
                        SetLotAndEnd.Clear();
                        //LOT_END 에 넣은걸 활용 하자
                        //END_ID, SIDE, END_SIDE 를 가져옴
                        DataSet endData = localDB.GetLotEnd();

                        Dictionary<string, string> endDic = new Dictionary<string, string>();
                        if (endData.Tables.Count > 0)
                        {
                            foreach (DataRow row in endData.Tables[0].Rows)
                            {
                                string dickey = string.Format(row[3].ToString() + row[0].ToString());
                                //키를 END_ID이고 END_SIDE를 가져옴
                                if (false == endDic.ContainsKey(dickey))
                                {
                                    //0705 하나의 POS에 두개 LOT들어간 애들 보면 ENDID가 겹쳐서 딕셔너리 키값으로 중복됨
                                    // 그래서 LOT 까지 만들어서 넣어주고 나중에 ENDID 쓸때 LOT string 파싱해서 짤라쓰자
                                    endDic.Add(dickey, row[1].ToString());
                                    SingleEnd.Add(row[0].ToString());
                                    SingleSide.Add(row[1].ToString());
                                    //두개 한묶음으로 쓸때 필요한 Side랑 LOT 랑 구분하는 딕셔너리 만듬
                                    if (false == SetLotAndEnd.ContainsKey(row[1].ToString()))
                                    {
                                        SetLotAndEnd.Add(row[1].ToString(), row[3].ToString());
                                    }
                                }
                            }
                        }
                        var tempend = endDic.Values.Distinct().ToList();

                        //사이드의 갯수 확인
                        int tempsidenum = tempend.Count();
                        comboBox_SIDE.Items.Clear();
                        List<string> strSideAdd = new List<string>();
                        //end 사이드로 분류해서 나누고싶다     
                        for (int i = 0; i < tempsidenum; i++)
                        {
                            List<string> tempside =
                                (from sidetemp in endDic
                                 where sidetemp.Value == tempend[i]
                                 select sidetemp.Key.ToString().Substring(5)
                                 ).ToList();

                            MakeStartEnd(tempside, tempside.Count(), tempend[i]);
                            strSideAdd.Add(tempend[i]);

                            End_qty = tempside.Count();
                        }
                        strSideAdd = strSideAdd.Distinct().ToList();

                        //사이드가 수량으로 나눠져있으면
                        if (NextStepSide.Count > tempsidenum) sideDiv = true;
                        else sideDiv = false;

                        if (localDB.CheckGumiExceptionLot(CurrentLot, out string notthing))
                        {
                            NextStepSide.Sort();
                            NextStartEnd.Sort();
                        }

                        foreach (var input in NextStepSide)
                        {
                            comboBox_SIDE.Items.Add(input);
                        }

                        SetDofComboBox();


                        LogManager.getInstance().writeLog("GET SPEC" + plantid.ToString() + "LOT = " + CurrentLot + "LOT_SEQ = " + lot_seq.ToString());
                        DataSet dataspec = localDB.GetSpinWeightSpec_Local(plantid.ToString(), CurrentLot, lot_seq);

                        if (dataspec.Tables.Count > 0)
                        {
                            foreach (DataRow row in dataspec.Tables[0].Rows)
                            {
                                label_WeightMin.Content = Convert.ToDouble(row[2]).ToString("F1");
                                Text_WeightMin.Text = Convert.ToDouble(row[2]).ToString("F1");
                                label_SL.Content = Convert.ToDouble(row[1]).ToString("F1");
                                label_WeightMax.Content = Convert.ToDouble(row[0]).ToString("F1");
                                Text_WeightMax.Text = Convert.ToDouble(row[0]).ToString("F1"); ;
                                label_ErrorRange.Content = row[7]?.ToString() ?? "";

                                gUSL = Convert.ToDouble(row[0]);
                                gSL = Convert.ToDouble(row[1]);
                                gLSL = Convert.ToDouble(row[2]);
                                gUCL = Convert.ToDouble(row[3]);
                                gCL = Convert.ToDouble(row[4]);
                                gLCL = Convert.ToDouble(row[5]);
                                gMark = row[6].ToString();
                                double setpan = gUSL + gSL + gLSL + gCL;
                                if (5 > setpan && 1 == dataspec.Tables.Count)
                                {
                                    MessageBox.Show("QMS has no Spec Data", "YesOrNo", MessageBoxButtons.YesNo);
                                    hasNoSpec = true;
                                }
                                if (gMark == "~") SL_tolerance = 0.0;
                                else SL_tolerance = Convert.ToDouble(row[7].ToString());
                                hasNoSpec = false;

                            }
                        }
                        else
                        {
                            //스펙 등록된 자료가 0이면
                            MessageBox.Show("QMS has no Spec Data", "YesOrNo", MessageBoxButtons.YesNo);
                            hasNoSpec = true;
                        }
                    }         
                    currentposindex = comboBox_POS.SelectedIndex;
                    startPosCount = currentposindex;
                }
                catch (Exception ex)
                {
                    LogManager.getInstance().writeLog(ex.ToString());
                    //스펙 등록된 자료가 0이면
                    MessageBox.Show("QMS has no Spec Data", "YesOrNo", MessageBoxButtons.YesNo);
                    hasNoSpec = true;
                }

            }
            //POS 까지 선택하면 grid 를 바꿔보자
            //setGridMax(End_qty);
            NextStartEndForExcel.Clear();
            currentposindex = comboBox_POS.SelectedIndex;
            startPosCount = currentposindex;
   
            foreach(string temp in NextStartEnd)
            {
                NextStartEndForExcel.Add(temp);
            }
            setGridMax(runScaleCount);
        }
    }

    public class NameToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
           double input = (double)value;
           if(502 > input)
           {
                return Brushes.LightGreen;
           }
           else
           {
                return DependencyProperty.UnsetValue;
           }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

}

