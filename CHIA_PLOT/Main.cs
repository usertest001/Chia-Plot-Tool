using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CHIA_PLOT
{
    public partial class Main : Form
    {
        #region 全局变量

        private Timer _jobTimer;

        private Timer _uiRefreshTimer;

        private Arguments Arguments { get; set; }

        #endregion

        public Main()
        {
            InitializeComponent();
            gvJobs.AutoGenerateColumns = false;

            _jobTimer = new Timer();
            _jobTimer.Tick += _jobTimer_Tick;
            _jobTimer.Interval = 1;

            _uiRefreshTimer = new Timer();
            _uiRefreshTimer.Tick += _uiRefreshTimer_Tick;
            _uiRefreshTimer.Interval = 60 * 1000;
            _uiRefreshTimer.Enabled = true;

            #region K DataSource

            List<K> kList = new List<K>();

            K k32 = new K();
            k32.Memory = 6750;
            k32.NonBitFieldMemory = 3990;
            k32.KName = "32";
            k32.Thread = 4;
            k32.KSize = (long)(101.4 * 1024 * 1024 * 1024);
            k32.Size = "101.4 GiB";
            k32.TempSize = 239;
            kList.Add(k32);

            K k33 = new K();
            k33.Memory = 7400;
            k33.NonBitFieldMemory = 7400;
            k33.KName = "33";
            k33.Thread = 4;
            k33.KSize = (long)(208.8 * 1024 * 1024 * 1024);
            k33.Size = "208.8 GiB";
            k33.TempSize = 239;
            kList.Add(k33);

            K k34 = new K();
            k34.Memory = 14800;
            k34.NonBitFieldMemory = 14800;
            k34.KName = "34";
            k34.Thread = 4;
            k34.KSize = (long)(429.8 * 1024 * 1024 * 1024);
            k34.Size = "429.8 GiB";
            k34.TempSize = 1041;
            kList.Add(k34);

            K k35 = new K();
            k35.Memory = 29600;
            k35.NonBitFieldMemory = 29600;
            k35.KName = "35";
            k35.Thread = 4;
            k35.KSize = (long)(884.1 * 1024 * 1024 * 1024);
            k35.Size = "884.1 GiB";
            k35.TempSize = 2175;
            kList.Add(k35);

            #endregion

            Arguments = new Arguments();
            Arguments.R = 4;
            Arguments.B = 3390;
            Arguments.Parallel = 20;
            Arguments.Delay = 30;
            Arguments.F = "b7ae8222a7d368c5917166b68efa1fd702f65db07db8a68f6aa6bbca6f1bec543932ae41e81311189aa74f43e833c70b";
            Arguments.P = "ab628afd0de0887f74af6dd9a3a8d74a99e1a9a2fd86865277374638a56c2c5752654ee17b6c1e2aa1fd5a1e92fb8d12";
            Arguments.E = true;
            Arguments.U = 128;
            Arguments.K = k32;

            Arguments.Directories = new BindingList<ChiaDirectory>();
            Arguments.Temp1Directories = new BindingList<ChiaDirectory>();
            Arguments.Temp2Directories = new BindingList<ChiaDirectory>();
            Arguments.Messages = new BindingList<Messages>();
            Arguments.Jobs = new BindingList<Job>();


            //绑定数据源
            kBindingSource.DataSource = kList;
            argumentsBindingSource.DataSource = Arguments;
            jobsBindingSource.DataSource = Arguments.Jobs;
            //messagesBindingSource.DataSource = Arguments.Messages;
            directoriesBindingSource.DataSource = Arguments.Directories;
            temp1DirectoriesBindingSource.DataSource = Arguments.Temp1Directories;
            temp2DirectoriesBindingSource.DataSource = Arguments.Temp2Directories;

            cblT.DisplayMember = nameof(ChiaDirectory.DriveName);
            cblT.ValueMember = nameof(ChiaDirectory.Checked);
            cblT.DataSource = temp1DirectoriesBindingSource;

            cbl2.DisplayMember = nameof(ChiaDirectory.DriveName);
            cbl2.ValueMember = nameof(ChiaDirectory.Checked);
            cbl2.DataSource = temp2DirectoriesBindingSource;

            cblD.DisplayMember = nameof(ChiaDirectory.DriveName);
            cblD.ValueMember = nameof(ChiaDirectory.Checked);
            cblD.DataSource = directoriesBindingSource;


            //foreach (DataGridViewColumn column in gvJobs.Columns)
            //{
            //    if (column.Frozen == false)
            //        column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            //    column.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            //}

            //foreach (DataGridViewColumn column in gvMessages.Columns)
            //{
            //    if (column.Frozen == false)
            //        column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            //    column.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            //}

        }

        private void _uiRefreshTimer_Tick(object sender, EventArgs e)
        {
            GetLocalDisk(Arguments.Directories, Arguments.Temp1Directories, Arguments.Temp2Directories);
            for (int i = 0; i < gvJobs.Rows.Count; i++)
            {
                gvJobs.UpdateCellValue(3, i);
            }

        }

        private void _jobTimer_Tick(object sender, EventArgs e)
        {
            _jobTimer.Interval = Arguments.Delay * 60 * 1000;
            string d = string.Empty;
            var t = Arguments.Temp1Directories.FirstOrDefault(c => c.Checked)?.DriveName;
            var t2 = Arguments.Temp2Directories.FirstOrDefault(c => c.Checked)?.DriveName;

            if (string.IsNullOrEmpty(t))
            {
                SetMessage($"请选择缓存盘", MessageType.Normal);
                return;
            }
            else
            {
                t = Path.Combine(t, "t");
                if (!Directory.Exists(t))
                {
                    Directory.CreateDirectory(t);
                }
            }
            if (string.IsNullOrEmpty(t2))
            {
                SetMessage($"请选择第二缓存盘", MessageType.Normal);
                return;
            }
            else
            {
                t2 = Path.Combine(t2, "t2");
                if (!Directory.Exists(t2))
                {
                    Directory.CreateDirectory(t2);
                }
            }


            #region 选择磁盘
            foreach (ChiaDirectory chiaDirectory in Arguments.Directories.Where(c => c.Checked))
            {
                //计算当前盘符运行中的容量
                long runningChiaSize = Arguments.Jobs.Count(c => Path.GetPathRoot(c.Directory) == chiaDirectory.DriveName) * Arguments.K.KSize;
                var driveInfo = DriveInfo.GetDrives().First(c => c.Name == chiaDirectory.DriveName);
                if (driveInfo.TotalFreeSpace >= runningChiaSize + Arguments.K.KSize)
                {
                    d = Path.Combine(driveInfo.Name, "Farm");
                    if (!Directory.Exists(d))
                    {
                        Directory.CreateDirectory(d);
                    }
                    break;
                }
            }

            if (string.IsNullOrEmpty(d))
            {
                SetMessage($"HDD没有可用的磁盘空间,下一个轮回（{Arguments.Delay}分钟后）将再次尝试。", MessageType.Normal);
                return;
            }

            if (Arguments.Jobs.Count < Convert.ToInt32(Arguments.Parallel))
            {
                //Action action = () =>
                //{
                //string command = $"Start-Process {chiaFileName}  -ArgumentList \"plots create -k 32 -n 1 -t {t} -d {d} -r {txtR.Text.Trim()} -b {txtB.Text.Trim()}  -f {txtF.Text.Trim()} -p {txtP.Text.Trim()}\"  ";
                string command = $"plots create -k {Arguments.K.KName} -n 1 -t {t} -2 {t2} -d {d} -r {Arguments.R} -b {Arguments.B} -u {Arguments.U} -f {Arguments.F} -p {Arguments.P} ";
                if (!Arguments.E)
                {
                    command += " -e";
                }



                Job job = new Job(Arguments);
                job.Id = Arguments.JobId++;

                Process process = new Process();
                process.StartInfo.FileName = Arguments.ChiaFileName;
                process.StartInfo.Arguments = command;
                process.StartInfo.UseShellExecute = false;
                process.EnableRaisingEvents = true;
                process.Exited += Process_Exited;
                if (Arguments.NoWindow)
                {
                    string path = Path.Combine(Application.StartupPath, "Plot_Log");
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }

                    job.LogFileName = Path.Combine(path, $"{job.Id}.txt");
                    //command += $" > \"{path}\"";
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    //process.StartInfo.RedirectStandardInput = true;
                    process.OutputDataReceived += Process_OutputDataReceived;
                    process.ErrorDataReceived += Process_ErrorDataReceived;
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                }
                else
                {
                    process.Start();
                }


                job.ProcessId = process.Id;
                job.StartTime = process.StartTime;
                job.Directory = d;
                Arguments.Jobs.Add(job);

                SetMessage($"任务{job.Id} 使用命令'{command}'成功创建任务。{Arguments.Jobs.Count}/{Arguments.Parallel}", MessageType.Start);
            }
            else
            {
                SetMessage($"当前已经运行了{Arguments.Jobs.Count}/{Arguments.Parallel}个任务，不需要启动新任务。", MessageType.Normal);
            }
            #endregion
        }

        private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            DataRecevied(sender, e);
        }

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            DataRecevied(sender, e);
        }

        private void DataRecevied(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Process process = (Process)sender;
                Job job = Arguments.Jobs.FirstOrDefault(c => c.ProcessId == process.Id);
                if (job != null)
                {
                    using (StreamWriter streamWriter = new StreamWriter(job.LogFileName, true))
                    {
                        streamWriter.WriteLine(e.Data);
                    }
                }
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            //获取chia进程信息
            var chiaProcesses = Process.GetProcessesByName("chia");
            foreach (Process oldProcess in chiaProcesses)
            {
                if (oldProcess.MainModule.FileName == txtChiaFileName.Text)
                {
                    oldProcess.EnableRaisingEvents = true;
                    oldProcess.Exited += Process_Exited;
                    if (Arguments.Jobs.All(c => c.ProcessId != oldProcess.Id))
                    {
                        Job oldJob = new Job(Arguments);
                        oldJob.ProcessId = oldProcess.Id;
                        oldJob.StartTime = oldProcess.StartTime;
                        Arguments.Jobs.Add(oldJob);
                    }
                }
            }

            if (Arguments.Directories.Count(c => c.Checked) == 0)
            {
                SetMessage("请选择最终目录磁盘。", MessageType.Normal);
                return;
            }
            if (Arguments.Temp1Directories.Count(c => c.Checked) == 0)
            {
                SetMessage("请选择缓存目录磁盘。", MessageType.Normal);
                return;
            }
            if (Arguments.Temp2Directories.Count(c => c.Checked) == 0)
            {
                SetMessage("请选择第二缓存目录磁盘。", MessageType.Normal);
                return;
            }
            if (!_jobTimer.Enabled)
            {
                _jobTimer.Start();
            }
        }

        private void SetMessage(string message, MessageType messageType)
        {
            Action action() => () =>
            {
                message = $"{DateTime.Now.ToString("G")}----{message}";
                switch (messageType)
                {
                    case MessageType.Start:
                        AppendTextColorful(message, Color.ForestGreen);
                        break;
                    case MessageType.End:
                        AppendTextColorful(message, Color.Crimson);
                        break;
                    default:
                        AppendText(message);
                        break;
                }
            };

            Invoke(action());
        }

        private void Process_Exited(object sender, EventArgs e)
        {
            Action action() => () =>
            {
                Process process = (Process)sender;
                Job job = Arguments.Jobs.FirstOrDefault(c => c.ProcessId == process.Id);
                if (job != null)
                {
                    Arguments.Jobs.Remove(job);
                    TimeSpan ts = process.ExitTime.Subtract(process.StartTime);
                    string message = $"任务{job.Id},进程{process.Id}已退出,BitField={job.BitField},共耗时{ts.Days}天{ts.Hours}小时{ts.Minutes}分钟";
                    SetMessage(message, MessageType.End);
                }
            };

            Invoke(action());
        }

        private void cbK_SelectedValueChanged(object sender, EventArgs e)
        {
            KOrBitFieldChanged();
        }

        private void KOrBitFieldChanged()
        {
            K k = (K)cbK.SelectedItem;
            if (k == null) return;
            if (Arguments.K.KName != k.KName)
                Arguments.K = k;
            if (Arguments.E)
            {
                Arguments.B = k.Memory;
                Arguments.R = k.Thread;
            }
            else
            {
                Arguments.B = k.NonBitFieldMemory;
                Arguments.R = k.Thread;
            }
            argumentsBindingSource.ResetBindings(true);
        }

        private void cbE_CheckStateChanged(object sender, EventArgs e)
        {
            KOrBitFieldChanged();
        }

        private void btnChiaFileName_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.FileName = "chia.exe";
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            openFileDialog.CheckFileExists = true;
            openFileDialog.CheckPathExists = true;
            openFileDialog.AddExtension = true;
            openFileDialog.Filter = @"daemon下的chia.exe|chia.exe";
            openFileDialog.Title = @"选择Chia所在目录(chia-blockchain\app-1.1.2\resources\app.asar.unpacked\daemon\)";
            if (DialogResult.OK == openFileDialog.ShowDialog())
            {
                txtChiaFileName.Text = openFileDialog.FileName;
            }
        }

        private void btnRefreshDisk_Click(object sender, EventArgs e)
        {
            GetLocalDisk(Arguments.Directories, Arguments.Temp1Directories, Arguments.Temp2Directories);
        }

        private void GetLocalDisk(BindingList<ChiaDirectory> directories, BindingList<ChiaDirectory> temp1Directories, BindingList<ChiaDirectory> temp2Directories)
        {
            foreach (DriveInfo driveInfo in DriveInfo.GetDrives().Where(c => c.DriveType == DriveType.Fixed))
            {
                if (directories.All(c => c.DriveName != driveInfo.Name))
                {
                    ChiaDirectory chiaDirectory = new ChiaDirectory();
                    chiaDirectory.DriveName = driveInfo.Name;
                    chiaDirectory.Checked = true;
                    directories.Add(chiaDirectory);
                }

                if (temp1Directories.All(c => c.DriveName != driveInfo.Name))
                {
                    ChiaDirectory tempDirectory = new ChiaDirectory();
                    tempDirectory.DriveName = driveInfo.Name;
                    temp1Directories.Add(tempDirectory);
                }

                if (temp2Directories.All(c => c.DriveName != driveInfo.Name))
                {
                    ChiaDirectory tempDirectory = new ChiaDirectory();
                    tempDirectory.DriveName = driveInfo.Name;
                    temp2Directories.Add(tempDirectory);
                }
            }
        }

        #region CheckedListBox

        private void cblT_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            SingleChocieCheckedListBox((CheckedListBox)sender, e);
        }

        private void cbl2_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            SingleChocieCheckedListBox((CheckedListBox)sender, e);
        }

        private void SingleChocieCheckedListBox(CheckedListBox checkedListBox, ItemCheckEventArgs e)
        {
            if (e.Index == checkedListBox.SelectedIndex)
            {
                switch (e.NewValue)
                {
                    case CheckState.Checked:
                        ((ChiaDirectory)checkedListBox.Items[checkedListBox.SelectedIndex]).Checked = true;
                        break;
                    default:
                        ((ChiaDirectory)checkedListBox.Items[checkedListBox.SelectedIndex]).Checked = false;
                        break;
                }
            }
            else
            {
                ((ChiaDirectory)checkedListBox.Items[e.Index]).Checked = false;
            }

            if (checkedListBox.CheckedItems.Count > 0)
            {
                for (int i = 0; i < checkedListBox.Items.Count; i++)
                {
                    if (i != e.Index)
                    {
                        checkedListBox.SetItemChecked(i, false);
                    }
                }
            }
        }

        #endregion


        //private void gvMessages_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
        //{
        //    if (e.RowIndex < 0) return;

        //    DataGridViewRow dataGridViewRow = gvMessages.Rows[e.RowIndex];
        //    MessageType messageType = (MessageType)gvMessages.Rows[e.RowIndex].Cells["messageTypeDataGridViewTextBoxColumn"].Value;
        //    switch (messageType)
        //    {
        //        case MessageType.Start:
        //            dataGridViewRow.DefaultCellStyle.Font = new Font(gvMessages.DefaultCellStyle.Font, FontStyle.Bold);
        //            dataGridViewRow.DefaultCellStyle.ForeColor = Color.ForestGreen;
        //            break;
        //        case MessageType.End:
        //            dataGridViewRow.DefaultCellStyle.ForeColor = Color.Crimson;
        //            dataGridViewRow.DefaultCellStyle.Font = new Font(gvMessages.DefaultCellStyle.Font, FontStyle.Bold);
        //            break;
        //    }
        //}

        private void btnStartHarvester_Click(object sender, EventArgs e)
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo();
            processStartInfo.FileName = Arguments.ChiaFileName;
            processStartInfo.Arguments = "start harvester";
            processStartInfo.UseShellExecute = false;

            Process harvesterProcess = new Process();
            harvesterProcess.StartInfo = processStartInfo;
            harvesterProcess.Start();
        }

        private void btnCancelAll_Click(object sender, EventArgs e)
        {
            foreach (Job job in Arguments.Jobs)
            {
                Process jobProcess = Process.GetProcessById(job.ProcessId);
                jobProcess.EnableRaisingEvents = true;
                jobProcess.Exited += Process_Exited;
                jobProcess.Kill();
            }
            if (_jobTimer.Enabled)
                _jobTimer.Stop();
        }

        public void AppendTextColorful(string text, Color color)
        {
            Font font = new Font(rtxMessage.Font, FontStyle.Bold);
            rtxMessage.SelectionStart = rtxMessage.TextLength;
            rtxMessage.SelectionLength = text.Length;
            rtxMessage.SelectionFont = font;
            rtxMessage.SelectionColor = color;
            AppendText(text);
        }

        private void AppendText(string text)
        {
            text += Environment.NewLine;
            rtxMessage.AppendText(text);
        }

        public Color GetRandomColor()
        {
            //随机
            Random RandomNum_First = new Random((int)DateTime.Now.Ticks);
            //  对于C#的随机数，没什么好说的
            System.Threading.Thread.Sleep(RandomNum_First.Next(50));
            Random RandomNum_Sencond = new Random((int)DateTime.Now.Ticks);

            //  为了在白色背景上显示，尽量生成深色
            //  三原色信息     红    绿    蓝      万色之祖（手动滑稽）
            int int_Red = RandomNum_First.Next(256);
            int int_Green = RandomNum_Sencond.Next(256);
            int int_Blue = (int_Red + int_Green > 400) ? 0 : 400 - int_Red - int_Green;
            int_Blue = (int_Blue > 255) ? 255 : int_Blue;
            Color color = Color.FromArgb(int_Red, int_Green, int_Blue);
            return color;
        }

        private void gvJobs_RowEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (e.RowIndex != gvJobs.CurrentRow?.Index)
            {
                string logFileName = gvJobs.Rows[e.RowIndex].Cells["logFileNameDataGridViewTextBoxColumn"].Value?.ToString();
                if (!string.IsNullOrEmpty(logFileName))
                {
                    rtxtPlotLog.Text = File.ReadAllText(logFileName);
                }
            }
        }

        //private void CheckDiskType(BindingList<ChiaDirectory> directories, BindingList<ChiaDirectory> temp1Directories, BindingList<ChiaDirectory> temp2Directories)
        //{
        //    string scope = @"\\.\root\microsoft\windows\storage";
        //    using (ManagementObjectSearcher diskSearcher = new ManagementObjectSearcher(scope, $"SELECT * FROM MSFT_PhysicalDisk where MediaType =4 Or MediaType =3"))
        //    {
        //        foreach (var disk in diskSearcher.Get())
        //        {
        //            var deviceId = disk["DeviceId"];
        //            using (ManagementObjectSearcher partitionSearcher = new ManagementObjectSearcher(scope, $"SELECT * FROM MSFT_Partition where DiskNumber ='{deviceId}' "))
        //            {
        //                foreach (var partition in partitionSearcher.Get())
        //                {
        //                    var driverLetter = partition["DriveLetter"].ToString().Trim('\0');
        //                    if (!string.IsNullOrEmpty(driverLetter))
        //                    {
        //                        switch (disk["MediaType"])
        //                        {
        //                            //HDD
        //                            case (ushort)3:
        //                                if (directories.All(c => c.DriveLetter != driverLetter))
        //                                {
        //                                    ChiaDirectory chiaDirectory = new ChiaDirectory();
        //                                    chiaDirectory.DriveName = $"{driverLetter}:\\";
        //                                    chiaDirectory.Checked = true;
        //                                    chiaDirectory.DriveLetter = driverLetter;
        //                                    directories.Add(chiaDirectory);
        //                                }
        //                                break;
        //                            //SSD
        //                            case (ushort)4:
        //                                if (directories.All(c => c.DriveLetter != driverLetter))
        //                                {
        //                                    ChiaDirectory tempDirectory = new ChiaDirectory();
        //                                    tempDirectory.DriveName = $"{driverLetter}:\\";
        //                                    tempDirectory.DriveLetter = driverLetter;
        //                                    temp1Directories.Add(tempDirectory);
        //                                    temp2Directories.Add(tempDirectory);
        //                                }

        //                                break;
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }

        //}
    }
}
