using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SPX_Weight.DataManager;
using SPX_Weight.DataModel;
using System.Runtime.InteropServices;

namespace SPX_Weight
{
    public partial class ConfigForm : Form
    {
        public List<String> strPlantId = new List<string>();
        
        private string setplantid;
        private string setlineid;

        public ConfigForm()
        {
            InitializeComponent();
            SetControl();
        }

        public void setConfigdata(string plantid, List<string> lineid, int ScaleSet)
        {
            combo_Plant_ID.Text = plantid;
            setplantid = plantid;

         
            textBox_LINEID_1.Text = lineid[0];
            textBox_LINEID_2.Text = lineid[1];
            textBox_LINEID_3.Text = lineid[2];

            textBox_Scale.Text = ScaleSet.ToString();

        }

        public string getconfigPlantID()
        {
            return setplantid;
        }

        public string getconfigLine()
        {
            return setlineid;
        }

        public void getPlantId()
        {
            QMSDataManager qmsData = QMSDataManager.getInstance();

            List<DBDataPlanTID> PlantId = qmsData.GetPlantId();

            if (PlantId != null && PlantId.Count > 0)
            {
                for (int i = 0; i < PlantId.Count; i++)
                {
                    strPlantId.Add(PlantId[i].Plant_Id);
                }
            }
        }

        public void SetControl()
        {
            SetPlantCombo();
        }

        public void SetPlantCombo()
        {
            getPlantId();

            foreach (string temp in strPlantId)
            {
                combo_Plant_ID.Items.Add(temp);
            }
        }

        private void textBox_LINEID_TextChanged(object sender, EventArgs e)
        {
           //string Text =  this.textBox_LINEID_1
        }

        private void btn_SAVE_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(LogManager.getInstance().PopSaveConfig, "notice", MessageBoxButtons.YesNo);

            List<string> linetemp = new List<string>();
            linetemp.Add(textBox_LINEID_1.Text);
            linetemp.Add(textBox_LINEID_2.Text);
            linetemp.Add(textBox_LINEID_3.Text);

            if (result == DialogResult.Yes)
            {
                this.DialogResult = DialogResult.OK;

                SetIni(setplantid, linetemp);

                this.Close();
            }
        }

        [System.Runtime.InteropServices.DllImport("kernel32")]
        public static extern int WritePrivateProfileString(string section, string key, string val, string filePath);


        public static void SetIniValue(string path, string Section, string Key, string value)
        {
           int i =  WritePrivateProfileString(Section, Key, value, path);
        }

        private void SetIni(string plantid, List<string> lineid)
        {
            string iniFileFullPath = System.IO.Directory.GetCurrentDirectory() + "\\Setting.ini";
            try
            {
                if (System.IO.File.Exists(iniFileFullPath))
                {
                    SetIniValue(iniFileFullPath, "PLANT_INFO", "PlantID", plantid);

                    for(int i =0; i<3; i++)
                    {
                        string Linetemp = string.Format("Line{0}", i + 1);
                        SetIniValue(iniFileFullPath, "PLANT_INFO", Linetemp, lineid[i]);
                    }
                    SetIniValue(iniFileFullPath, "PLANT_INFO", "ScaleCount", textBox_Scale.Text);
                }
            }
            catch
            {

            }
        }

        private void combo_Plant_ID_SelectedIndexChanged(object sender, EventArgs e)
        {
            setplantid = combo_Plant_ID.SelectedItem.ToString();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
