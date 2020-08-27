using IKEYCOMLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Just.iKey
{
    /// <summary>
    /// iKeyEditCtrl.xaml 的交互逻辑
    /// </summary>
    public partial class iKeyEditCtrl : UserControl, Just.Base.IDependency
    {
        public const int IKEY_MAX_LEVEL = 0x8;

        public const int IKEY_NULL = 0x0;
        public const int IKEY_API_VERSION = 0x200;

        public const int IKEY_OPEN_NEXT = 0x0;
        public const int IKEY_OPEN_FIRST = 0x1;
        public const int IKEY_OPEN_CURRENT = 0x2;
        public const int IKEY_OPEN_SPECIFIC = 0x3;
        public const int IKEY_OPEN_MASK = 0x7;
        public const int IKEY_OPEN_BY_NAME = 0x100;
        public const int IKEY_OPEN_BY_GUID = 0x200;

        public const int IKEY_CONFIG_DES_KEY_SIZE = 0x1;
        public const int IKEY_CONFIG_RSA_MOD_SIZE = 0x2;

        public const int IKEY_PROP_CAPABILITIES = 0x0;
        public const int IKEY_PROP_MEM_SIZE = 0x1;
        public const int IKEY_PROP_ROOT_ACCESSINFO = 0x2;
        public const int IKEY_PROP_ACCESSINFO = 0x3;
        public const int IKEY_PROP_APP_NAME = 0x4;
        public const int IKEY_PROP_APP_GUID = 0x5;
        public const int IKEY_PROP_VERSIONINFO = 0x6;
        public const int IKEY_PROP_SERNUM = 0x7;
        public const int IKEY_PROP_LED_ON = 0x8;
        public const int IKEY_PROP_LED_OFF = 0x9;
        public const int IKEY_PROP_CONFIGURATION = 0xA;
        public const int IKEY_PROP_FRIENDLY_NAME = 0xB;

        public const int IKEY_ROOT_DIR = 0x0;
        public const int IKEY_INDEX_FILE = 0xFFFF;
        public const int IKEY_TOKEN_NAME_FILE = 0xFFFE;

        public const int IKEY_DIR_BY_ID = 0x100;
        public const int IKEY_DIR_BY_LONG_ID = 0x200;
        public const int IKEY_DIR_BY_NAME = 0x300;
        public const int IKEY_DIR_BY_GUID = 0x400;
        public const int IKEY_DIR_BY_MASK = 0xF00;

        public const int IKEY_FILETYPE_UNUSED = 0x0;
        public const int IKEY_FILETYPE_DIR = 0x1;
        public const int IKEY_FILETYPE_DATA = 0x2;
        public const int IKEY_FILETYPE_COUNTER = 0x3;
        public const int IKEY_FILETYPE_KEY = 0x4;
        public const int IKEY_FILETYPE_UNKNOWN = 0xFF;

        public const int IKEY_ACCESS_ANYONE = 0x0;
        public const int IKEY_ACCESS_USER = 0x1;
        public const int IKEY_ACCESS_OFFICER = 0x2;
        public const int IKEY_ACCESS_APP = 0x3;
        public const int IKEY_ACCESS_NONE = 0x7;

        public const int IKEY_ACCESS_READ = 0x1;
        public const int IKEY_ACCESS_WRITE = 0x2;
        public const int IKEY_ACCESS_CRYPT = 0x4;

        public const int IKEY_CAPS_MD5HMAC = 0x1;
        public const int IKEY_CAPS_MD5XOR = 0x2;
        public const int IKEY_CAPS_MD5CHAP = 0x4;
        public const int IKEY_CAPS_DES = 0x8;
        public const int IKEY_CAPS_APPAUTH = 0x20;

        public const int IKEY_DIR_FROM_MF = 0x0;
        public const int IKEY_DIR_FROM_CUR_DF = 0x10;
        public const int IKEY_DIR_TO_PARENT = 0x20;

        public const int IKEY_FILE_READ = 0x10;
        public const int IKEY_FILE_WRITE = 0x20;
        public const int IKEY_FILE_CRYPT = 0x40;

        public const int IKEY_CREATE_AUTO_ID = 0x10000;

        public const int IKEY_DELETE_RECURSIVE = 0x10000;

        public const int IKEY_VERIFY_USER_PIN = 0x0;
        public const int IKEY_VERIFY_SO_PIN = 0x1;
        public const int IKEY_VERIFY_APPKEY = 0x2;

        public const int IKEY_CHANGE_USER_PIN = 0x0;
        public const int IKEY_UNBLOCK_USER_PIN = 0x1;
        public const int IKEY_CHANGE_SO_PIN = 0x2;
        public const int IKEY_CHANGE_APP_KEY = 0x3;
        public const int IKEY_CHANGE_MASK = 0xF;

        public const int IKEY_SCOPE_MF = 0x0;
        public const int IKEY_SCOPE_DF = 0x1;

        public const int IKEY_HASH_MD5_XOR = 0x0;
        public const int IKEY_HASH_MD5_HMAC = 0x1;
        public const int IKEY_HASH_MD5_CHAP = 0x2;
        public const int IKEY_HASH_DES_MAC = 0x10;
        public const int IKEY_HASH_TYPE_MASK = 0xFF;

        public const int IKEY_CRYPT_DES = 0x0;
        public const int IKEY_CRYPT_TYPE_MASK = 0xFF;

        public const int IKEY_GENKEY_ENCRYPT = 0x100;
        public const int IKEY_GENKEY_CREATE = 0x200;
        public const int IKEY_DES_ENCRYPT = 0x80;
        public const int IKEY_DES_DECRYPT = 0x40;

        public const int RB_SUCCESS = 0x0;
        public const uint RB_CANNOT_OPEN_DRIVER = 0x80100001;
        public const uint RB_INVALID_DRVR_VERSION = 0x80100002;
        public const int RB_INVALID_COMMAND = 0x3;
        public const int RB_ACCESS_DENIED = 0x4;
        public const int RB_ALREADY_ZERO = 0x5;
        public const int RB_UNIT_NOT_FOUND = 0x6;
        public const int RB_DEVICE_REMOVED = 0x7;
        public const int RB_COMMUNICATIONS_ERROR = 0x8;
        public const int RB_DIR_NOT_FOUND = 0x9;
        public const uint RB_FILE_NOT_FOUND = 0x8010000A;
        public const int RB_MEM_CORRUPT = 0xB;
        public const int RB_INTERNAL_HW_ERROR = 0xC;
        public const int RB_INVALID_RESP_SIZE = 0xD;
        public const int RB_PIN_EXPIRED = 0xE;
        public const int RB_ALREADY_EXISTS = 0xF;
        public const int RB_NOT_ENOUGH_MEMORY = 0x10;
        public const int RB_INVALID_PARAMETER = 0x11;
        public const int RB_ALIGNMENT_ERROR = 0x12;
        public const int RB_INPUT_TOO_LONG = 0x13;
        public const int RB_INVALID_FILE_SELECTED = 0x14;
        public const int RB_DEVICE_IN_USE = 0x15;
        public const int RB_INVALID_API_VERSION = 0x16;
        public const int RB_TIME_OUT_ERROR = 0x17;
        public const int RB_ITEM_NOT_FOUND = 0x18;
        public const int RB_COMMAND_ABORTED = 0x19;
        public const int RB_INVALID_STATUS = 0xFF;

        public iKeyEditCtrl()
        {
            InitializeComponent();
        }
        IKEYCOMLib.CoiKeyClass ikey = new IKEYCOMLib.CoiKeyClass();
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var prop = new byte[32];
            var pin = new byte[8];
            var rand = new byte[8];
            var name = new byte[32];
            var guid = new byte[16];
            var content = new byte[64];
            var path = new long[8];
            long level;
            long size;
            long num;
            long index;
            string temp;
            long libver;
            long drvver;
            byte refByte = 0;

            var serialNum = new byte[8];
            string friendlyName;
            long capability;
            long memSize;
            SAccessInfo accessInfo;
            SVersionInfo versionInfo;
            SSysInfo sysInfo = new SSysInfo();
            SFileInfo fileInfo;
            SDirInfo dirInfo;

            libver = ikey.GetLibVersion();
            drvver = ikey.GetDriverVersion();
            TxtLog.AppendText($"\nLibrary Version: 0x{libver:x2}  Driver Version: 0x{drvver:x2}");

            try
            {
                ikey.CreateContext(IKEY_NULL, IKEY_API_VERSION);
                TxtLog.AppendText("\nCreateContext");

                ikey.OpenDevice(IKEY_OPEN_FIRST, ref refByte);
                TxtLog.AppendText("\nOpenDevice");

                ikey.GetProperty(IKEY_PROP_CAPABILITIES, ref refByte, out prop[0], 4);
                TxtLog.AppendText("\nGetProperty Capabilities");
                capability = BitConverter.ToInt64(prop, 0);
                TxtLog.AppendText($"\nCapability: {capability}");

                ikey.GetProperty(IKEY_PROP_MEM_SIZE, ref refByte, out prop[0], 4);
                TxtLog.AppendText("\nGetProperty Memory Size");
                memSize = BitConverter.ToInt64(prop, 0);
                TxtLog.AppendText($"\nMemory Size: {memSize}");

                ikey.GetProperty(IKEY_PROP_FRIENDLY_NAME, ref refByte, out prop[0], 32);
                TxtLog.AppendText("\nGetProperty Friendly Name");
                friendlyName = Convert.ToString(prop);
                TxtLog.AppendText($"\nFriendly Name: {friendlyName}");

                ikey.GetProperty(IKEY_PROP_SERNUM, ref refByte, out serialNum[0], 8);
                TxtLog.AppendText("\nGetProperty Serial Number");
                temp = HexToString(serialNum);
                TxtLog.AppendText($"\nSerial Number: {temp}");

                ikey.GetVersionInfo(out versionInfo);
                TxtLog.AppendText("\nGetVersionInfo");
                TxtLog.AppendText($"\nDriver Version: 0x{versionInfo.lDriverVersion:x2}");
                TxtLog.AppendText($"\nFirmware Version: {versionInfo.chFwVersionMajor:x2}.{versionInfo.chFwVersionMinor:x2}.{versionInfo.chProductCode:x2}.{versionInfo.chConfiguration:x2}.{versionInfo.chFwVersionMinor:x2}");

                ikey.GetSysInfo(ref sysInfo);
                TxtLog.AppendText("\nGetSysInfo");
                TxtLog.AppendText($"\nFlags: {sysInfo.lFlags}");
                TxtLog.AppendText($"\nFree Space: {sysInfo.lFreeSpace}");
                TxtLog.AppendText($"\nFile System Type: {sysInfo.chFileSysType}");
                TxtLog.AppendText($"\nMaximumm Dir Levels: {sysInfo.chMaxDirLevels}");

                ikey.GetAccessInfo(IKEY_PROP_ACCESSINFO, out accessInfo);
                TxtLog.AppendText("\nGetAccessInfo");
                TxtLog.AppendText($"\nMaxPinRetries: {accessInfo.chMaxPinRetries}");
                TxtLog.AppendText($"\nCurPinCounter: {accessInfo.chCurPinCounter}");
                TxtLog.AppendText($"\nCreateAccess: {accessInfo.chCreateAccess}");
                TxtLog.AppendText($"\nDeleteAccess: {accessInfo.chDeleteAccess}");
                try
                {
                    accessInfo.chMaxPinRetries = 10;
                    accessInfo.chCurPinCounter = 0;
                    accessInfo.chCreateAccess = IKEY_ACCESS_ANYONE;
                    accessInfo.chDeleteAccess = IKEY_ACCESS_ANYONE;

                    //SetAccessInfo sets the access setting for the iKey
                    ikey.SetAccessInfo(IKEY_PROP_ACCESSINFO, accessInfo);
                }
                catch (Exception ex)
                {
                    TxtLog.AppendText($"\nSetAccessInfo Failed: {ex}");
                }
                ikey.SetProperty(IKEY_PROP_LED_OFF, ref refByte, refByte, 0);
                TxtLog.AppendText($"\nSetProperty LED off");
            }
            catch (Exception ex)
            {
                TxtLog.AppendText($"\niKey Operation Failed: {ex}");
            }
        }

        public string HexToString(byte[] hex)
        {
            //高低位转换
            Array.Reverse(hex);
            return BitConverter.ToString(hex).Replace("-", string.Empty);
        }
    }
}
