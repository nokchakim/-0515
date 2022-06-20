using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Data;
using System.IO;
using SPX_Weight.Common;
using Excel = Microsoft.Office.Interop.Excel;
using MessageBox = System.Windows.Forms.MessageBox;
using Microsoft.Office.Interop.Excel;

namespace SPX_Weight
{
    public class MakeExcelCommonData
    {
        public string strLine = "00";
        public string strRange = "0.0 ~ 0.0";
        public string strmin = "0.0";
        public string strmax = "0.0";
        public string strLotno = "0000";
        public string strDate = "20000101";

        public string slot = "0";
        public string sline = "00";
        public string sDof = "0";
        public int SetScale = 0;
        public int lotseq = 0;
        public List<string> NextStartEnd = new List<string>();
        public List<string> NextStepSide = new List<string>();
        public bool bsideonly = false;



        public void makeCommonData(string line, string range, string lot, string date, string dof, string min, string max, List<string> Sidelist, int scale, bool sideonly, List<string>nextSide, int lotsequency)
        {
            strLine = string.Format("#{0}M/No Scale Check Sheet", line);
            strRange = string.Format(range);
            strLotno = string.Format("LOT {0}", lot);
            strDate = string.Format(date);
            slot = lot;
            sline = line;
            sDof = dof;
            strmin = min;
            strmax = max;
            NextStartEnd = Sidelist;
            SetScale = scale;
            bsideonly = sideonly;
            NextStepSide = nextSide;
            lotseq = lotsequency;
        }
    }

    class ExportExcel
    {
        public Microsoft.Office.Interop.Excel.Application APP = null;
        public Microsoft.Office.Interop.Excel.Workbook WB = null;
        public Microsoft.Office.Interop.Excel.Worksheet WS = null;
        public Microsoft.Office.Interop.Excel.Range Range = null;

        //공통으로title 에 쓰는거라 Common 으로 달앗음
        private string CmOverRange;
        private string CmLotNo;
        private string CmErrorRange;
        private string Cmtitle;
        private string CmDate;


        public void Export_ExcelFile()
        {

        }

        public void Export_Excel(System.Data.DataTable table, int scale, int runcount, MakeExcelCommonData common, List<string> side, List<string> pos, int endcount, string CurrentProductDate)
        {
            string filepath = GetExcelformatFile(runcount, common.slot, common.sline, common.sDof, CurrentProductDate, common.lotseq);

            List<double> rowvlaue = new List<double>();
            List<double> rowvlaueEtc = new List<double>();

            int columnscount = runcount + 6;
            try
            {
                for (var i = 0; i < table.Rows.Count; i++)
                {
                    for (var j = 6; j < columnscount; j++)
                    {
                        if (!string.IsNullOrEmpty(table.Rows[i][j].ToString()))
                        {
                            rowvlaue.Add(Convert.ToDouble(table.Rows[i][j]));
                        }                           
                    }
                    //한줄의 평균 최대 최소 갭 계산해서 리턴줌
                    //rowvlaueEtc = MinMaxAvgCalculate(rowvlaue);
                }
            }
            catch(Exception ex)
            {
                LogManager.getInstance().writeLog(ex.ToString());
            }

            //RLM 때문에 변경 
            //WriteFileData(table, filepath, common, side, pos, endcount);
            WriteFileData(table, filepath, common, common.NextStepSide, pos, endcount);
            System.Diagnostics.Process.Start(filepath);
                     
        }
        /// <summary>
        /// 엑셀 파일 내용 채우기 
        /// </summary>        
        /// <returns></returns>
        private bool WriteFileData(System.Data.DataTable ds, string filepath, MakeExcelCommonData common, List<string> side, List<string> pos, int endcount)
        {
            var excelapp = new Excel.Application();
            WB = excelapp.Workbooks.Open(filepath);
            excelapp.DisplayAlerts = false;
            try
            {               
                //excelapp.Workbooks.Add();

                Excel._Worksheet worksheet = WB.ActiveSheet;

                worksheet.Cells[4, 6] = common.strRange;
                worksheet.Cells[3, 6] = common.strLotno;
                worksheet.Cells[1, 1] = common.strLine;
                worksheet.Cells[4, 1] = common.strDate;
                worksheet.Cells[5, 6] = common.strmin;
                worksheet.Cells[5, 7] = common.strmax;

                int currentarrchk = 0;
                int beforepos = 0;
                int startPosposition = 0;
                int posMergecount = 0;
                string temppos = "";
                //여기서 side랑 pos가지고 위치를 다시 정해줘야 합니다. 
                for (var i = 0; i < ds.Rows.Count; i++)
                {
                    //POS의 전체를 가지고 와서 지금 들어가는 위치가 어딘지 확인 하기
                    //POS 시작 포스 * 사이드 갯수 + 8 
                    int endc = common.NextStartEnd.Count;
                    Int32 startpos = ReturnPosPosition(Convert.ToInt32(ds.Rows[i][3]), pos);
                                      
                    //End Range
                    string[] tempside = ds.Rows[i][5].ToString().Split('~');
                    //한 POS에 LOT걸린애들 분류 해야함 startEnd가 같거든
                    //int sidec = common.NextStartEnd.IndexOf(ds.Rows[i][5].ToString());
                    int sidec = side.IndexOf(ds.Rows[i][4].ToString());
                    if(sidec < 0)
                    {
                        sidec = common.NextStepSide.IndexOf(ds.Rows[i][4].ToString());
                    }
                    string temptemp = tempside[0]?.Substring(0, tempside[0].Length - 2);

                    temptemp = ds.Rows[i][4].ToString();
                    int currentpos = Convert.ToInt32(ds.Rows[i][3]);
                    //POS
                    worksheet.Cells[(startpos * endc) + sidec + 8, 1] = ds.Rows[i][3];
                    if(i == 0) startPosposition = (startpos * endc) + sidec + 8;
                    //DOF
                    worksheet.Cells[(startpos * endc) + sidec + 8, 3] = ds.Rows[i][2];
                    worksheet.Cells[(startpos * endc) + sidec + 8, 2] = temptemp;
                    worksheet.Cells[(startpos * endc) + sidec + 8, 5] = ds.Rows[i][4].ToString();
                    for (var j = 6; j < 6 + common.SetScale; j++)
                    {
                        int r = j - 6;
                        worksheet.Cells[(startpos * endc) + sidec + 8, r + 10] = ds.Rows[i][j];
                    }
                    InFomula(worksheet, (startpos * endc) + sidec + 8, 4, 0, endcount);

                    if (common.bsideonly == false)
                    {
                        if ((ds.Rows[i][3].ToString() != temppos && temppos != "") || i == ds.Rows.Count - 1 || 1 == endc)
                        {
                            int k = posMergecount - 1;
                            if (1 == endc)
                            {
                                k = 0;
                                startPosposition = (startpos * endc) + sidec + 8;
                            }
                            else if (i == ds.Rows.Count - 1) k = posMergecount;
                            worksheet.Range[worksheet.Cells[startPosposition, 4], worksheet.Cells[startPosposition + k, 4]].Merge();
                            worksheet.Range[worksheet.Cells[startPosposition, 1], worksheet.Cells[startPosposition + k, 1]].Merge();
                            worksheet.Range[worksheet.Cells[startPosposition, 3], worksheet.Cells[startPosposition + k, 3]].Merge();
                            InFomula(worksheet, startPosposition, 4, k, endcount);
                            posMergecount = 0;
                            startPosposition = (startpos * endc) + sidec + 8;
                        }
                        posMergecount += 1;
                        temppos = ds.Rows[i][3].ToString();

                    }                                        
                }
                /// sideonly 옵션으로 일단 다 쓰고 나서 합치는걸 하자
                if (common.bsideonly == true)
                {
                    int mergerange = 0;
                    List<string> sideonlypos = new List<string>();
                    Range range = worksheet.UsedRange;
                    // 사용중인 셀 범위를 가져오기
                    string samecheck = "";
                    int samestart = 0;
                    for (int row = 8; row <= 111; row++) 
                    {
                        var str = Convert.ToString((range.Cells[row, 1] as Excel.Range).Value2);
                        if(!string.IsNullOrEmpty(str))
                        {
                            if(samecheck == "")
                            {
                                samecheck = str;
                                samestart = row;
                            }
                            else if(samecheck == str)
                            {
                                mergerange += 1;
                            }
                            else if(samecheck != str)
                            {
                                worksheet.Range[worksheet.Cells[samestart, 1], worksheet.Cells[samestart + mergerange, 1]].Merge();
                                worksheet.Range[worksheet.Cells[samestart, 3], worksheet.Cells[samestart + mergerange, 3]].Merge();
                                worksheet.Range[worksheet.Cells[samestart, 4], worksheet.Cells[samestart + mergerange, 4]].Merge();
                                InFomula(worksheet, samestart, 4, mergerange, endcount);
                                mergerange = 0;
                                samestart = row;
                                samecheck = str;
                            }
                        }
                        else
                        {                       
                            if(mergerange >= 1)
                            {
                                worksheet.Range[worksheet.Cells[samestart, 1], worksheet.Cells[samestart + mergerange, 1]].Merge();
                                worksheet.Range[worksheet.Cells[samestart, 3], worksheet.Cells[samestart + mergerange, 3]].Merge();
                                worksheet.Range[worksheet.Cells[samestart, 4], worksheet.Cells[samestart + mergerange, 4]].Merge();
                                InFomula(worksheet, samestart, 4, mergerange, endcount);
                            }
                            samecheck = "";
                            mergerange = 0;
                        }
                    } 
                }
                //worsheet.SaveAs();
                excelapp.DisplayAlerts = true;
                WB.Close(true);
                WB = null;
                excelapp.Quit();
            }
            catch (Exception ex)
            {
                LogManager.getInstance().writeLog(ex.ToString());
                WB.Close(true);
                WB = null;
                excelapp.Quit();
            }
         
            return false;
        }

        private List<double> MinMaxAvgCalculate(List<double> value)
        {
            List<double> rtvalue = new List<double>();

            //Min
            rtvalue.Add(value.Min());
            //Max
            rtvalue.Add(value.Max());
            //Avg
            rtvalue.Add(value.Average());
            //R
            rtvalue.Add((rtvalue[1] - rtvalue[0]));

            return rtvalue;
        }

        private string GetExcelformatFile(int scale, string lot, string line, string dof, string CurrentProductDate, int lotseq)
        {
            string fileName = "";
            try
            {
                string folderPath = System.IO.Directory.GetCurrentDirectory();
                folderPath = string.Format(folderPath + "\\BaseExcel\\");
                string basefile = string.Format("Base{0}.xlsx", scale);
                string sourcepath = folderPath + basefile;

                string targetpath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                targetpath = string.Format(targetpath + "\\ExcelReport\\");

                FileInfo file = new FileInfo(sourcepath);

                if (file.Exists)
                {
                    if (!Directory.Exists(targetpath))
                        Directory.CreateDirectory(targetpath);

                    DateTime dt = DateTime.Now;
                    string targetfile = string.Format("{0}-{1}Line-{2}-{3}-Dof({4}).xlsx", CurrentProductDate, line, lot, lotseq, dof);
                    targetpath = string.Format(targetpath + targetfile);
                    FileInfo exitst = new FileInfo(targetpath);

                    if (exitst.Exists)
                    {
                        var result = MessageBox.Show(LogManager.getInstance().PopFileExistSave, "notice", System.Windows.Forms.MessageBoxButtons.YesNo);

                        if (System.Windows.Forms.DialogResult.No == result)
                        {
                            //exitst.Delete();
                            //다른 이름으로 저장하기해서 copy로 진행 함
                            targetfile = string.Format("{0}-{1}Line-{2}-{3}-Dof({4})_Copy.xlsx", CurrentProductDate, line, lot, lotseq, dof);
                        }
                    }
                    else
                    {
                        file.CopyTo(targetpath);
                    }
                    
                    fileName = targetpath;
                }
            }
            catch (Exception ex)
            {
                LogManager.getInstance().writeLog(ex.ToString());
                MessageBox.Show(LogManager.getInstance().PopExistFileOpen);
            }

            return fileName;
        }

        public string DualLot(string Lot)
        {
            string comLot = "";
            var excelapp = new Excel.Application();
            
            string filepath = string.Format(System.IO.Directory.GetCurrentDirectory() + "\\BaseExcel\\DualLot.xlsx");
            WB = excelapp.Workbooks.Open(filepath);
            excelapp.DisplayAlerts = false;
            
            try
            {

                return comLot;
            }
            catch(Exception e)
            {
                e.ToString();
                return "";
            }
        }
        public void SetDataTableInfo(string Lot, string Range, string error)
        {
            CmOverRange = Range;
            CmLotNo = Lot;
            CmErrorRange = error;
        }

        private void SetCommonData()
        {

        }



        public void ClearCommonData()
        {
            CmOverRange = "";
            CmLotNo = "";
            CmErrorRange = "";
            Cmtitle = "";
            CmDate = "";
        }

        public void SideStyle(List<string> side, List<string> pos, int endcount, Excel._Worksheet worksheet)
        {            
            //POS 기준으로 쭉 쓰고 
            //SIDE를 순서대로 배치 하고 
                //하면서 endcount랑 setscale 가지고 1/2 로 나눌지 두줄로 쓸지 정해야함
                //SIDE기준으로 셀 병합
            //그리고 다음 SIDE내리면서 반복
            //SIDE 다 하고 POS기준으로 또 병합
        }

        /// <summary>
        /// POS 부분만 합치는걸 기준으로 함
        /// </summary>        
        public void CellMerge(Excel._Worksheet ws, int Y1, int Y2)
        {
            Excel.Range rg1 = ws.Range[ws.Cells[Y1, 4], ws.Cells[Y2, 4]].Merge();
            rg1.Merge(true);        
            //rg1.HorizontalAlignment = Microsoft.Office.Interop.Excel.XlHAlign.xlHAlignCenter;
            //rg1.VerticalAlignment = 2;
            //rg1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlDash;
            rg1.Clear();
        }

        public void InFomula(Excel._Worksheet ws, int y, int x, int range, int count)
        {
            Excel.Range rg = ws.Cells[y, x];
            //rg.Formula = string.Format("=AVERAGE(J{0}:{1}{2})",y, TransCellLocate(10+count-6), y+ range);
            //  ws.Cells[y,x] = string.Format("=AVERAGE(J{0}:{1}{2})", y, TransCellLocate(10 + count - 1), y + range);
            ws.Cells[y, x] = string.Format("=AVERAGE(J{0}:U{1})", y, y + range);

        }

        public string TransCellLocate(int columnNumber)
        {
            int dividend = columnNumber;
            string columnName = String.Empty;
            int modulo;
            while (dividend > 0)
            {
                modulo = (dividend - 1) % 26;
                columnName = Convert.ToChar(65 + modulo).ToString() + columnName;
                dividend = (int)((dividend - modulo) / 26);
            }
            return columnName;
        }

        /// <summary>
        /// 지금 POS를 받아다가 전체 중에 어디에 위치해야 하는지 리턴값을 줌
        /// </summary>
        /// <param name="currentpos"></param>
        /// <returns></returns>
        public Int32 ReturnPosPosition(int currentpos, List<string> Pos)
        {
            Int32 rt = 0;
            string temp = currentpos.ToString("D2");
            rt = Pos.IndexOf(temp);
            return rt;
        }
    }
}
