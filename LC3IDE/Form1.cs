using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Windows.Media;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System.Xml;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;


namespace LC3IDE
{
    public partial class Form1 : Form
    {
        public static LC3File file;
        public static TextEditor editor;
        private Process simulatorProcess;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            editor = new TextEditor();
            elementHost1.Child = editor;

            string[] args = Environment.GetCommandLineArgs();
            if (args.Length >= 2)
                file = new LC3File(editor, openFileDialog1, saveFileDialog1, args[1]);
            else
                file = new LC3File(editor, openFileDialog1, saveFileDialog1);

            file.TitleTextChanged += new LC3File.TitleTextChangedHandler(file_TitleTextChanged);

            file.RefreshTitleText();

            editor.ShowLineNumbers = true;
            XmlTextReader reader = new XmlTextReader(AppDomain.CurrentDomain.BaseDirectory+"\\LC3.xshd");
            editor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
            
            ExtProgConf assemblerConf = new ExtProgConf(AppDomain.CurrentDomain.BaseDirectory + "\\assembler.conf");
            assemblerConf.ShowOn(toolStripComboBox1);
            ExtProgConf simulatorConf = new ExtProgConf(AppDomain.CurrentDomain.BaseDirectory + "\\simulator.conf");
            simulatorConf.ShowOn(toolStripComboBox2);

            editor.FontFamily = new FontFamily(Settings.fontfamily);
            editor.FontSize = Settings.fontsize;
        }

        void file_TitleTextChanged(object sender, LC3File.TitleTextChangeEventArgs e)
        {
            Text = e.TitleText + " - " + GlobalVars.IDEname;
        }

        private bool ASM()
        {
            if (file.Changed || file.Untitled)
            {
                if (!file.Save()) return false;
            }
            string cmd = ((ExtProg)toolStripComboBox1.SelectedItem).exec;
            string exec = cmd.Substring(0, cmd.IndexOf(' '));
            string args = cmd.Substring(cmd.IndexOf(' ')+1);
            exec=AppDomain.CurrentDomain.BaseDirectory + "\\"+exec;
            args=args.Replace("{file}", "\""+file.FileName+"\"");
            string outfile = Path.ChangeExtension(file.FileName, "obj");
            args = args.Replace("{outfile}", "\"" + outfile + "\"");

            Process process = new Process();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.FileName = exec;
            process.StartInfo.Arguments = args;
            process.StartInfo.WorkingDirectory = Path.GetDirectoryName(file.FileName);

            listBox1.Items.Clear();
            listBox1.Items.Add("正在汇编，请稍候……");
            Application.DoEvents();

            process.Start();
            process.WaitForExit();

            listBox1.Items.Add("汇编器返回值：" + process.ExitCode.ToString()+(process.ExitCode==0?"(成功)":"(失败)"));

            string output=process.StandardOutput.ReadToEnd();
            string[] lines=Regex.Split(output, "\r\n|\r|\n");
            foreach(string line in lines){
                listBox1.Items.Add(line);
            }

            if (process.ExitCode != 0)
            {
                LocateErrorLine(output);
            }

            return process.ExitCode == 0;
        }

        private void LocateErrorLine(string AssemblerOutput)
        {
            Regex r=new Regex("line\\s?#?(\\d+)",RegexOptions.IgnoreCase);
            Match match = r.Match(AssemblerOutput);
            if(match.Success){
                int LineNumber = int.Parse(match.Groups[1].Value);
                if (LineNumber > 0 && LineNumber <= editor.LineCount){
                    editor.Focus();
                    editor.ScrollToLine(LineNumber);
                }
                
                //MessageBox.Show(LineNumber.ToString());
            }
        }

        private void RUN()
        {
            if (file.Untitled || !File.Exists(Path.ChangeExtension(file.FileName, "obj")))
            {
                MessageBox.Show("代码未汇编，请先汇编！", GlobalVars.IDEname, MessageBoxButtons.OK);
                return;
            }
            string cmd = ((ExtProg)toolStripComboBox2.SelectedItem).exec;
            string exec = cmd.Substring(0, cmd.IndexOf(' '));
            string args = cmd.Substring(cmd.IndexOf(' ') + 1);
            exec = AppDomain.CurrentDomain.BaseDirectory + "\\" + exec;
            string objfile = Path.ChangeExtension(file.FileName, "obj");
            args = args.Replace("{file}", objfile);

            simulatorProcess = new Process();
            simulatorProcess.StartInfo.FileName = exec;
            simulatorProcess.StartInfo.Arguments = args;
            simulatorProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(file.FileName);

            simulatorProcess.Start();
        }

        private void ASMAndRUN()
        {
            if(ASM())
                RUN();
        }

        private void STOP()
        {
            if (simulatorProcess!=null&&!simulatorProcess.HasExited)
                simulatorProcess.Kill();
        }

        private void OpenSettingsForm()
        {
            Form2 frm = new Form2();
            frm.ShowDialog();
        }

        private void 新建NToolStripMenuItem_Click(object sender, EventArgs e)
        {
            file.New();
        }

        private void 打开OToolStripMenuItem_Click(object sender, EventArgs e)
        {
            file.Open();
        }

        private void 保存SToolStripMenuItem_Click(object sender, EventArgs e)
        {
            file.Save();
        }

        private void 另存为AToolStripMenuItem_Click(object sender, EventArgs e)
        {
            file.SaveAs();
        }

        private void 退出XToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!file.Close())
            {
                e.Cancel = true;
            }
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            file.New();
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            file.Open();
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            file.Save();
        }

        private void 撤销UToolStripMenuItem_Click(object sender, EventArgs e)
        {
            editor.Undo();
        }

        private void 重做RToolStripMenuItem_Click(object sender, EventArgs e)
        {
            editor.Redo();
        }

        private void 剪切TToolStripMenuItem_Click(object sender, EventArgs e)
        {
            editor.Cut();
        }

        private void 复制CToolStripMenuItem_Click(object sender, EventArgs e)
        {
            editor.Copy();
        }

        private void 粘贴VToolStripMenuItem_Click(object sender, EventArgs e)
        {
            editor.Paste();
        }

        private void 全选AToolStripMenuItem_Click(object sender, EventArgs e)
        {
            editor.SelectAll();
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            editor.Cut();
        }

        private void toolStripButton5_Click(object sender, EventArgs e)
        {
            editor.Copy();
        }

        private void toolStripButton6_Click(object sender, EventArgs e)
        {
            editor.Paste();
        }

        private void toolStripButton7_Click(object sender, EventArgs e)
        {
            editor.Undo();
        }

        private void toolStripButton8_Click(object sender, EventArgs e)
        {
            editor.Redo();
        }

        private void 关于AToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                GlobalVars.IDEname+" Ver"+Application.ProductVersion.ToString()+"\n"+
                "作者：负一的平方根\n"+
                "zzh1996@mail.ustc.edu.cn\n"+
                "2015年9月"
                , GlobalVars.IDEname, MessageBoxButtons.OK);
        }

        private void 汇编AToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ASM();
        }

        private void 在模拟器中运行RToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RUN();
        }

        private void 设置SToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenSettingsForm();
        }

        private void toolStripButton13_Click(object sender, EventArgs e)
        {
            OpenSettingsForm();
        }

        private void 汇编并运行ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ASMAndRUN();
        }

        private void toolStripButton10_Click(object sender, EventArgs e)
        {
            ASMAndRUN();
        }

        private void 终止运行SToolStripMenuItem_Click(object sender, EventArgs e)
        {
            STOP();
        }

        private void toolStripButton12_Click(object sender, EventArgs e)
        {
            STOP();
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (!file.Untitled)
                Process.Start("explorer.exe", Path.GetDirectoryName(file.FileName));
        }
    }

    public partial class LC3File
    {
        private string fileName;
        private bool changed;
        private bool untitled;
        private TextEditor editor;
        private OpenFileDialog openFileDialog;
        private SaveFileDialog saveFileDialog;

        public delegate void TitleTextChangedHandler(object sender, TitleTextChangeEventArgs e);
        public event TitleTextChangedHandler TitleTextChanged;

        public LC3File(TextEditor editor, OpenFileDialog openFileDialog, SaveFileDialog saveFileDialog)
        {
            changed = false;

            this.editor = editor;
            this.openFileDialog = openFileDialog;
            this.saveFileDialog = saveFileDialog;

            editor.TextChanged += editor_TextChanged;

            New();
        }

        public LC3File(TextEditor editor, OpenFileDialog openFileDialog, SaveFileDialog saveFileDialog, string fileName):this(editor,openFileDialog,saveFileDialog)
        {
            FileName = fileName;
            ReadFile();
            Changed = false;
            untitled = false;
        }

        public bool Changed
        {
            get { return changed; }
            private set { changed = value; RefreshTitleText(); }
        }

        public string FileName
        {
            get { return fileName; }
            private set { fileName = value; RefreshTitleText(); }
        }

        public bool Untitled
        {
            get { return untitled; }
        }

        public class TitleTextChangeEventArgs : EventArgs
        {
            private string titleText;
            public TitleTextChangeEventArgs(string titleText)
            {
                this.titleText = titleText;
            }
            public string TitleText
            {
                get { return titleText; }
            }
        }

        void editor_TextChanged(object sender, EventArgs e)
        {
            Changed = true;
        }

        string TitleText()
        {
            return (changed ? "* " : "") + Path.GetFileName(FileName);
        }

        public bool Save()
        {
            if (untitled)
            {
                return SaveAs();
            }
            else
            {
                WriteFile();
                Changed = false;
                return true;
            }
        }

        public bool SaveAs()
        {
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                FileName = saveFileDialog.FileName;
                WriteFile();
                Changed = false;
                untitled = false;
                return true;
            }
            else
            {
                return false;
            }
        }

        public void New()
        {
            if (Close())
            {
                editor.Clear();
                editor.Text = Settings.defaultcode;
                FileName = "Untitled";
                untitled = true;
                Changed = false;
            }
        }

        public void Open()
        {
            if (Close())
            {
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    FileName = openFileDialog.FileName;
                    ReadFile();
                    untitled = false;
                    Changed = false;
                }
            }
        }

        public bool Close()
        {
            if (Changed)
            {
                switch (MessageBox.Show("是否保存更改？", GlobalVars.IDEname, MessageBoxButtons.YesNoCancel))
                {
                    case DialogResult.Cancel:
                        return false;
                    case DialogResult.No:
                        Changed = false;
                        return true;
                    case DialogResult.Yes:
                        Save();
                        return true;
                }
            }
            else
            {
                return true;
            }
            return false;
        }

        private void WriteFile()
        {
            File.WriteAllText(FileName, editor.Text);
        }

        private void ReadFile()
        {
            editor.Text = File.ReadAllText(FileName);
        }

        public void RefreshTitleText()
        {
            if (TitleTextChanged != null)
                TitleTextChanged(this, new TitleTextChangeEventArgs(TitleText()));
        }

    }

    public class ExtProg
    {
        public string name;
        public string exec;
        public ExtProg(string name, string exec)
        {
            this.name = name;
            this.exec = exec;
        }
        public override string ToString()
        {
            return name;
        }
    }

    public class ExtProgConf
    {
        List<ExtProg> progs;

        public ExtProgConf(string filename)
        {
            progs = new List<ExtProg>();
            string[] lines = File.ReadAllLines(filename,Encoding.UTF8);
            for (int i = 0; i < lines.Length; i+=2)
            {
                progs.Add(new ExtProg(lines[i], lines[i + 1]));
            }
        }

        public void ShowOn(ToolStripComboBox comboBox){
            comboBox.Items.Clear();
            foreach (ExtProg i in progs)
            {
                comboBox.Items.Add(i);
            }
            comboBox.SelectedIndex = 0;
        }
    }

    public class Settings
    {
        public static string fontfamily
        {
            get { 
                return Properties.Settings.Default.fontfamily;
            }
            set {
                Form1.editor.FontFamily = new FontFamily(value);
                Properties.Settings.Default.fontfamily = value;
                Properties.Settings.Default.Save();
            }
        }

        public static float fontsize
        {
            get
            {
                return Properties.Settings.Default.fontsize;
            }
            set
            {
                Form1.editor.FontSize = value;
                Properties.Settings.Default.fontsize = value;
                Properties.Settings.Default.Save();
            }
        }

        public static string defaultcode
        {
            get
            {
                return Properties.Settings.Default.defaultcode;
            }
            set
            {
                Properties.Settings.Default.defaultcode = value;
                Properties.Settings.Default.Save();
            }
        }
    }

    public class GlobalVars
    {
        public const string IDEname = "LC3IDE";
    }
}
