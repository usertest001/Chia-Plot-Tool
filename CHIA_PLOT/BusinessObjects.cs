using System;
using System.ComponentModel;

namespace CHIA_PLOT
{
    public class Arguments
    {
        /// <summary>
        /// thread
        /// </summary>
        public byte R { get; set; }

        /// <summary>
        /// memory
        /// </summary>
        public ushort B { get; set; }

        public byte N { get; set; }

        public byte Parallel { get; set; }

        public byte Delay { get; set; }

        /// <summary>
        /// farmer public key
        /// </summary>
        public string F { get; set; }

        /// <summary>
        /// pool public key
        /// </summary>
        public string P { get; set; }

        public string ChiaFileName { get; set; }

        /// <summary>
        /// bucket
        /// </summary>
        public byte U { get; set; }

        /// <summary>
        /// bitfield
        /// </summary>
        public bool E { get; set; }

        public K K { get; set; }

        /// <summary>
        /// 所有任务Id汇总,累加
        /// </summary>
        public byte JobId { get; set; }

        /// <summary>
        /// 无窗口模式
        /// </summary>
        public bool NoWindow { get; set; }

        public BindingList<ChiaDirectory> Directories { get; set; }

        public BindingList<ChiaDirectory> Temp1Directories { get; set; }

        public BindingList<ChiaDirectory> Temp2Directories { get; set; }

        public BindingList<Job> Jobs { get; set; }

        public BindingList<Messages> Messages { get; set; }

    }

    public class ChiaDirectory
    {
        public string DriveName { get; set; }


        public bool Checked { get; set; }
    }

    public class Job
    {

        public Job(Arguments arguments)
        {
            this.Arguments = arguments;
        }

        [Browsable(false)]
        public Arguments Arguments { get; set; }

        /// <summary>
        /// 顺序
        /// </summary>
        public byte Id { get; set; }

        public int ProcessId { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 运行时间
        /// </summary>
        public string RunningTime
        {
            get
            {
                string result;
                TimeSpan ts = DateTime.Now.Subtract(StartTime);
                if (ts.Days > 0)
                    result = $"{ts.Days}天{ts.Hours}小时{ts.Minutes}分钟";
                else
                    result = $"{ts.Hours}小时{ts.Minutes}分钟";
                return result;
            }
        }

        /// <summary>
        /// 最终文件夹
        /// </summary>
        public string Directory { get; set; }

        public bool BitField => Arguments.E;

        public string LogFileName { get; set; }

    }

    public class Messages
    {
        public DateTime Time { get; set; }

        public string Message { get; set; }

        public MessageType MessageType { get; set; }
    }

    public enum MessageType
    {
        Start,
        End,
        Normal,
        Error
    }

    public class K
    {
        public string KName { get; set; }

        public ushort Memory { get; set; }

        public ushort NonBitFieldMemory { get; set; }

        public byte Thread { get; set; }

        [Browsable(false)]
        public long KSize { get; set; }

        public string Size { get; set; }

        public double TempSize { get; set; }

        /// <summary>
        /// 转换字节大小、长度, 根据字节大小范围返回KB, MB, GB自适长度
        /// </summary>
        /// <param name="length">传入字节大小</param>
        /// <returns></returns>
        private string FileSizeString(long length)
        {
            int byteConversion = 1024;
            double bytes = Convert.ToDouble(length);

            // 超过EB的单位已经没有实际转换意义了, 太大了, 忽略不用
            if (bytes >= Math.Pow(byteConversion, 6)) // EB
            {
                return string.Concat(Math.Round(bytes / Math.Pow(byteConversion, 6), 2), " EB");
            }
            else if (bytes >= Math.Pow(byteConversion, 5)) // PB
            {
                return string.Concat(Math.Round(bytes / Math.Pow(byteConversion, 5), 2), " PB");
            }
            else if (bytes >= Math.Pow(byteConversion, 4)) // TB
            {
                return string.Concat(Math.Round(bytes / Math.Pow(byteConversion, 4), 2), " TB");
            }
            else if (bytes >= Math.Pow(byteConversion, 3)) // GB
            {
                return string.Concat(Math.Round(bytes / Math.Pow(byteConversion, 3), 2), " GB");
            }
            else if (bytes >= Math.Pow(byteConversion, 2)) // MB
            {
                return string.Concat(Math.Round(bytes / Math.Pow(byteConversion, 2), 2), " MB");
            }
            else if (bytes >= byteConversion) // KB
            {
                return string.Concat(Math.Round(bytes / byteConversion, 2), " KB");
            }
            else // Bytes
            {
                return string.Concat(bytes, " Bytes");
            }
        }
    }
}
