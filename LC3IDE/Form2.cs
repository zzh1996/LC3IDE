using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace LC3IDE
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            label1.Font = fontDialog1.Font = new Font(Settings.fontfamily,Settings.fontsize);
            textBox1.Text = Settings.defaultcode;
            fontDialog1.Apply += fontDialog1_Apply;
        }

        void fontDialog1_Apply(object sender, EventArgs e)
        {
            label1.Font = fontDialog1.Font;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (fontDialog1.ShowDialog() == DialogResult.OK)
            {
                label1.Font = fontDialog1.Font;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Settings.fontfamily = fontDialog1.Font.FontFamily.Name;
            Settings.fontsize = fontDialog1.Font.Size;
            Settings.defaultcode = textBox1.Text;
            Close();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            MessageBox.Show("编辑配置文件后请重启程序以使新的配置生效！", GlobalVars.IDEname, MessageBoxButtons.OK);
            Process.Start("notepad.exe", AppDomain.CurrentDomain.BaseDirectory + "\\LC3.xshd");
        }
    }
}
