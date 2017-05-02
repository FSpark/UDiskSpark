using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Configuration;
using System.Threading;
using System.Runtime.InteropServices;
namespace UDiskSpark
{
  
    public partial class MainForm : Form
    {
        bool isCopy = false;
        bool isCopyEnd = false;
        string targetdir = null;
        private IntPtr deviceNotifyHandle;
        private IntPtr deviceEventHandle;
        private IntPtr directoryHandle;
        private DataTable driveInfoDT= new DataTable("DriveInfo");
        public MainForm(){
        InitializeComponent() ;
    }
        public static IntPtr CreateFileHandle(string driveLetter)
        {
            // open the existing file for reading          
            IntPtr handle = Win32.CreateFile(
                  driveLetter,
                  Win32.GENERIC_READ,
                  Win32.FILE_SHARE_READ | Win32.FILE_SHARE_WRITE,
                  0,
                  Win32.OPEN_EXISTING,
                  Win32.FILE_FLAG_BACKUP_SEMANTICS | Win32.FILE_ATTRIBUTE_NORMAL,
                  0);

            if (handle == Win32.INVALID_HANDLE_VALUE)
            {
                return IntPtr.Zero;
            }
            else
            {
                return handle;
            }
        }

        private void RegisterForHandle(char c)
        {
            Win32.DEV_BROADCAST_HANDLE deviceHandle = new Win32.DEV_BROADCAST_HANDLE();
            int size = Marshal.SizeOf(deviceHandle);
            deviceHandle.dbch_size = size;
            deviceHandle.dbch_devicetype = Win32.DBT_DEVTYP_HANDLE;
            directoryHandle = CreateFileHandle(c + ":\\");
            deviceHandle.dbch_handle = directoryHandle;
            IntPtr buffer = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(deviceHandle, buffer, true);
            deviceNotifyHandle = Win32.RegisterDeviceNotification(this.Handle, buffer, Win32.DEVICE_NOTIFY_WINDOW_HANDLE);
            if (deviceNotifyHandle == IntPtr.Zero)
            {
                // TODO handle error
            }
        }



        

        /// <summary>
        /// Registers a window to receive notifications when USB devices are plugged or unplugged.
        /// </summary>
        /// <param name="windowHandle">Handle to the window receiving notifications.</param>
        public  void RegisterUsbDeviceNotification(IntPtr windowHandle)
        {
            DevBroadcastDeviceinterface dbi = new DevBroadcastDeviceinterface
            {
                DeviceType = Win32.DbtDevtypDeviceinterface,
                Reserved = 0,
                ClassGuid = Win32.GUID_IO_MEDIA_ARRIVAL,
                Name = 0
            };

            dbi.Size = Marshal.SizeOf(dbi);
            IntPtr buffer = Marshal.AllocHGlobal(dbi.Size);
            Marshal.StructureToPtr(dbi, buffer, true);

            deviceNotifyHandle = Win32.RegisterDeviceNotification(windowHandle, buffer, 0);
            MessageBox.Show (deviceNotifyHandle.ToString());
        }

        /// <summary>
        /// Unregisters the window for USB device notifications
        /// </summary>
        public  void UnregisterUsbDeviceNotification()
        {
            Win32.UnregisterDeviceNotification(deviceNotifyHandle);
        }
        private void UnregisterHandles()
        {
            if (directoryHandle != IntPtr.Zero)
            {
                Win32.CloseHandle(directoryHandle);
                directoryHandle = IntPtr.Zero;
            }
            if (deviceNotifyHandle != IntPtr.Zero)
            {
                Win32.UnregisterDeviceNotification(deviceNotifyHandle);
                deviceNotifyHandle = IntPtr.Zero;
            }
        }


        [StructLayout(LayoutKind.Sequential)]
        private struct DevBroadcastDeviceinterface
        {
            internal int Size;
            internal int DeviceType;
            internal int Reserved;
            internal Guid ClassGuid;
            internal short Name;
        }
        
        private void MainForm_Load(object sender, EventArgs e)
        {
            DataColumn[] cols ={
                                  new DataColumn("Name",typeof(String)),
                                  new DataColumn("DriveFormat",typeof(String)),
                                  new DataColumn("DriveType",typeof(String)),
                                  new DataColumn("IsReady",typeof(String)),
                                  new DataColumn("RootDirectory",typeof(String)),
                               
                                  new DataColumn("AvailableFreeSpace",typeof(long)),
                                  new DataColumn("TotalFreeSpace",typeof(long)),
                                  new DataColumn("TotalSize",typeof(long)),
                                  new DataColumn("VolumeLabel",typeof(String)),
                                  
                              };
            driveInfoDT.Columns.AddRange(cols);

            driveInfoDT.PrimaryKey = new DataColumn[] { driveInfoDT.Columns["Name"] };
            DriveInfo[] s = DriveInfo.GetDrives();
            foreach (DriveInfo drive in s)
            {
                if (drive.DriveType != DriveType.CDRom)
                {
                    driveInfoDT.Rows.Add(new Object[]{drive.Name,drive.DriveFormat,drive.DriveType,drive.IsReady,
                                     drive.RootDirectory,drive.AvailableFreeSpace , drive.TotalFreeSpace,drive.TotalSize,drive.VolumeLabel});
                }
            }
            RegisterUsbDeviceNotification(this.Handle);
        }
        private static void ShowTable(DataTable table)
        {
            foreach (DataColumn col in table.Columns)
            {
                System.Diagnostics.Debug.Write(string.Format("{0,-14}", col.ColumnName));
            }
            System.Diagnostics.Debug.WriteLine("");

            foreach (DataRow row in table.Rows)
            {
                foreach (DataColumn col in table.Columns)
                {
                    if (col.DataType.Equals(typeof(DateTime)))
                    {
                        System.Diagnostics.Debug.Write(string.Format("{0,-14:d}", row[col]));
                       
                    }
                    else if (col.DataType.Equals(typeof(Decimal)))
                    {
                        System.Diagnostics.Debug.Write(string.Format("{0,-14:C}", row[col]));
                    }
                    else
                    {
                        System.Diagnostics.Debug.Write(string.Format("{0,-14}", row[col]));
                    }
                }
                System.Diagnostics.Debug.WriteLine("");
            }
            System.Diagnostics.Debug.WriteLine("");
        }
        protected override void WndProc(ref Message m)
        {
         
           
                
            //try
            //{
                if (m.Msg == Win32.WM_DEVICECHANGE)
                {
                    
                    switch (m.WParam.ToInt32())
                    {
                        case Win32.WM_DEVICECHANGE:
                            break;
                        case Win32.DBT_DEVICEARRIVAL://U盘插入
                            
                                MessageBox.Show("w:" + m.WParam.ToString());
                                MessageBox.Show("L:" + m.LParam.ToString());
                             var devType = Marshal.ReadInt32(m.LParam, 4);  
                        if (devType == Win32.DBT_DEVTYP_VOLUME)  
                        {
                            MessageBox.Show("L:" + GetDrive(m.LParam));
                        }  

                            DataTable driveInfoDT2 = driveInfoDT.Clone();
                            DriveInfo[] s = DriveInfo.GetDrives();
                            foreach (DriveInfo drive in s)
                            {
                                if  ( drive.DriveType != DriveType.CDRom)
                                {
                                    driveInfoDT2.Rows.Add(new Object[]{drive.Name,drive.DriveFormat,drive.DriveType,drive.IsReady,
                                     drive.RootDirectory,drive.AvailableFreeSpace , drive.TotalFreeSpace,drive.TotalSize,drive.VolumeLabel});
                                    
                                }
                              
                                
                            }
                            if (!driveInfoDT2.Equals(driveInfoDT))
                            {
                                foreach (DataRow row in driveInfoDT2.Rows)
                                {
                                    if (!driveInfoDT.Rows.Contains(row["Name"]) & row["DriveType"].ToString() == "Removable")
                                    {

                                        RegisterForHandle(row["Name"].ToString().ToCharArray()[0]);
                                        listBox1.Items.Add(DateTime.Now.ToString() + "--> U盘已插入，盘符为:" + row["Name"]);
                                    }
                                }
                                
                                // Thread.Sleep(1000);
                                //if (!isCopyEnd)
                                //{
                                //    isCopy = true;

                                //}
                                //break;

                            }
                            ShowTable(driveInfoDT2);
                            driveInfoDT.Clear();
                            driveInfoDT = driveInfoDT2;
                            break;
                        case Win32.DBT_CONFIGCHANGECANCELED:
                            break;
                        case Win32.DBT_CONFIGCHANGED:
                            break;
                        case Win32.DBT_CUSTOMEVENT:
                            break;
                        case Win32.DBT_DEVICEQUERYREMOVE:
                             MessageBox.Show("w:" + m.WParam.ToString());
                                MessageBox.Show("L:" + m.LParam.ToString());
                            MessageBox.Show("haah");
                            s = DriveInfo.GetDrives();
                            foreach (DriveInfo drive in s)
                            {
                                if (drive.DriveType == DriveType.Removable)
                                {
                                    listBox1.Items.Add(DateTime.Now.ToString() + "--> U盘请求拔出，盘符为:" + drive.Name.ToString());
                                    
                                    break;
                                }
                            }
                            break;
                        case Win32.DBT_DEVICEQUERYREMOVEFAILED:
                            break;
                        case Win32.DBT_DEVICEREMOVECOMPLETE: //U盘卸载
                             MessageBox.Show("w:" + m.WParam.ToString());
                                MessageBox.Show("L:" + m.LParam.ToString());
                            DataTable driveInfoDT3 = driveInfoDT.Clone();
                            DriveInfo[] ss = DriveInfo.GetDrives();
                            foreach (DriveInfo drive in ss)
                            {
                                if  ( drive.DriveType != DriveType.CDRom)
                                {
                                    driveInfoDT3.Rows.Add(new Object[]{drive.Name,drive.DriveFormat,drive.DriveType,drive.IsReady,
                                     drive.RootDirectory,drive.AvailableFreeSpace , drive.TotalFreeSpace,drive.TotalSize,drive.VolumeLabel});
                                    
                                }
                              
                                
                            }
                            if (!driveInfoDT3.Equals(driveInfoDT))
                            {
                                foreach (DataRow row in driveInfoDT.Rows)
                                {
                                    if (!driveInfoDT3.Rows.Contains(row["Name"]) & row["DriveType"].ToString() == "Removable")
                                    {
                                        listBox1.Items.Add(DateTime.Now.ToString() + "--> U盘已卸载，盘符为:" + row["Name"]);
                                    }
                                }
                                
                                // Thread.Sleep(1000);
                                //if (!isCopyEnd)
                                //{
                                //    isCopy = true;

                                //}
                                //break;

                            }
                            ShowTable(driveInfoDT3);
                            driveInfoDT.Clear();
                            driveInfoDT = driveInfoDT3;
                            
                            
                           
                            break;
                        case Win32.DBT_DEVICEREMOVEPENDING:
                            break;
                        case Win32.DBT_DEVICETYPESPECIFIC:
                            break;
                        case Win32.DBT_DEVNODES_CHANGED:
                            break;
                        case Win32.DBT_QUERYCHANGECONFIG:
                            break;
                        case Win32.DBT_USERDEFINED:
                            break;
                        default:
                            break;
                    }
                }
           // }
            //catch (Exception ex)
            //{
            //    MessageBox.Show(ex.Message);
            //}
            base.WndProc(ref m);
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            UnregisterUsbDeviceNotification();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            UnregisterHandles();
        }

        private void button1_Click(object sender, EventArgs e)
        {
                    RegisterForHandle("J".ToCharArray()[0]);
        }

        private void button3_Click(object sender, EventArgs e)
        {

        }
        private static string GetDrive(IntPtr lParam)
        {
            var volume = (Win32.DEV_BROADCAST_VOLUME)Marshal.PtrToStructure(lParam, typeof(Win32.DEV_BROADCAST_VOLUME));
            var letter = GetLetter(volume.dbcv_unitmask);
            return string.Format("{0}:\\", letter);
        }
        /// <summary>  
        /// 获得盘符  
        /// </summary>  
        /// <param name="dbcvUnitmask">  
        /// 1 = A  
        /// 2 = B  
        /// 4 = C...  
        /// </param>  
        /// <returns>结果是A~Z的任意一个字符或者为'?'</returns>  
        private static char GetLetter(uint dbcvUnitmask)
        {
            const char nona = '?';
            const string drives = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            if (dbcvUnitmask == 0) return nona;
            var i = 0;
            var pom = dbcvUnitmask >> 1;
            while (pom != 0)
            {
                pom = pom >> 1;
                i++;
            }
            if (i < drives.Length)
                return drives[i];
            return nona;
        }
        /*   
          private static void GetLetterTest() 
          { 
              for (int i = 0; i < 67108864; i++) 
              { 
                  Console.WriteLine("{0} - {1}", i, GetLetter((uint)i)); 
                  i = i << 1; 
              } 
    //0 - ? 
    //1 - A 
    //3 - B 
    //7 - C 
    //15 - D 
    //31 - E 
    //63 - F 
    //127 - G 
    //255 - H 
    //511 - I 
    //1023 - J 
    //2047 - K 
    //4095 - L 
    //8191 - M 
    //16383 - N 
    //32767 - O 
    //65535 - P 
    //131071 - Q 
    //262143 - R 
    //524287 - S 
    //1048575 - T 
    //2097151 - U 
    //4194303 - V 
    //8388607 - W 
    //16777215 - X 
    //33554431 - Y 
    //67108863 - Z 
          }*/  
      
    }

    public class Win32
    {
        public const int WM_DEVICECHANGE = 0x219;
        public const int DBT_DEVICEARRIVAL = 0x8000;
        public const int DBT_CONFIGCHANGECANCELED = 0x0019;
        public const int DBT_CONFIGCHANGED = 0x0018;
        public const int DBT_CUSTOMEVENT = 0x8006;
        public const int DBT_DEVICEQUERYREMOVE = 0x8001;
        public const int DBT_DEVICEQUERYREMOVEFAILED = 0x8002;
        public const int DBT_DEVICEREMOVECOMPLETE = 0x8004;
        public const int DBT_DEVICEREMOVEPENDING = 0x8003;
        public const int DBT_DEVICETYPESPECIFIC = 0x8005;
        public const int DBT_DEVNODES_CHANGED = 0x0007;
        public const int DBT_QUERYCHANGECONFIG = 0x0017;

        public const int DBT_DEVTYP_DEVICEINTERFACE = 5;
        public const int DBT_DEVTYP_HANDLE = 6;
        public const int DBT_DEVTYP_VOLUME = 0x00000002; // drive type is logical volume  

        public const int DBT_USERDEFINED = 0xFFFF;
        public const int DbtDevtypDeviceinterface = 5;


        public const uint GENERIC_READ = 0x80000000;
        public const uint OPEN_EXISTING = 3;
        public const uint FILE_SHARE_READ = 1;
        public const uint FILE_SHARE_WRITE = 2;
        public const uint FILE_SHARE_DELETE = 4;
        public const uint FILE_ATTRIBUTE_NORMAL = 128;
        public const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
        public static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        public const int DEVICE_NOTIFY_WINDOW_HANDLE = 0;
        public const int DEVICE_NOTIFY_SERVICE_HANDLE = 1;
        public const int DEVICE_NOTIFY_ALL_INTERFACE_CLASSES = 4;

        public static Guid GUID_IO_MEDIA_ARRIVAL = new Guid("A5DCBF10-6530-11D2-901F-00C04FB951ED");
        [StructLayout(LayoutKind.Sequential)]
        public class DEV_BROADCAST_DEVICEINTERFACE
        {
            public int dbcc_size;
            public int dbcc_devicetype;
            public int dbcc_reserved;
            public Guid dbcc_classguid;
            public short dbcc_name;
        }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public class DEV_BROADCAST_DEVICEINTERFACE1
        {
            public int dbcc_size;
            public int dbcc_devicetype;
            public int dbcc_reserved;
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 16)]
            public byte[] dbcc_classguid;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
            public char[] dbcc_name;
        }
        [StructLayout(LayoutKind.Sequential)]
        public class DEV_BROADCAST_HDR
        {
            public int dbcc_size;
            public int dbcc_devicetype;
            public int dbcc_reserved;
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct DEV_BROADCAST_HANDLE
        {
            public int dbch_size;
            public int dbch_devicetype;
            public int dbch_reserved;
            public IntPtr dbch_handle;
            public IntPtr dbch_hdevnotify;
            public Guid dbch_eventguid;
            public long dbch_nameoffset;
            public byte dbch_data;
            public byte dbch_data1;
        }
   [StructLayout(LayoutKind.Sequential)]
        public struct DEV_BROADCAST_VOLUME
        {
            /// DWORD->unsigned int  
            public uint dbcv_size;
            /// DWORD->unsigned int  
            public uint dbcv_devicetype;
            /// DWORD->unsigned int  
            public uint dbcv_reserved;
            /// DWORD->unsigned int  
            public uint dbcv_unitmask;
            /// WORD->unsigned short  
            public ushort dbcv_flags;
        }  

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr RegisterDeviceNotification(IntPtr recipient, IntPtr notificationFilter, int flags);

        [DllImport("user32.dll")]
        public static extern bool UnregisterDeviceNotification(IntPtr handle);

        [DllImport("kernel32.DLL")]
        public static extern int GetLastError();


        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateFile(
              string FileName,                    // file name
              uint DesiredAccess,                 // access mode
              uint ShareMode,                     // share mode
              uint SecurityAttributes,            // Security Attributes
              uint CreationDisposition,           // how to create
              uint FlagsAndAttributes,            // file attributes
              int hTemplateFile                   // handle to template file
              );

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);
    }

}
