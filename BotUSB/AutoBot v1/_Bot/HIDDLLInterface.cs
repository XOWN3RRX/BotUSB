using Microsoft.VisualBasic.CompilerServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutoBot_v1._Bot
{
    internal sealed class HIDDLLInterface
    {
        // Token: 0x06000085 RID: 133
        [DllImport("HHID.dll", CharSet = CharSet.Ansi, EntryPoint = "Connect", ExactSpelling = true, SetLastError = true)]
        public static extern bool hidConnect(int pHostWin);

        // Token: 0x06000086 RID: 134
        [DllImport("HHID.dll", CharSet = CharSet.Ansi, EntryPoint = "Disconnect", ExactSpelling = true, SetLastError = true)]
        public static extern bool hidDisconnect();

        // Token: 0x06000087 RID: 135
        [DllImport("HHID.dll", CharSet = CharSet.Ansi, EntryPoint = "GetItem", ExactSpelling = true, SetLastError = true)]
        public static extern int hidGetItem(int pIndex);

        // Token: 0x06000088 RID: 136
        [DllImport("HHID.dll", CharSet = CharSet.Ansi, EntryPoint = "GetItemCount", ExactSpelling = true, SetLastError = true)]
        public static extern int hidGetItemCount();

        // Token: 0x06000089 RID: 137
        [DllImport("HHID.dll", CharSet = CharSet.Ansi, EntryPoint = "Read", ExactSpelling = true, SetLastError = true)]
        public static extern bool hidRead(int pHandle, ref byte pData);

        // Token: 0x0600008A RID: 138
        [DllImport("HHID.dll", CharSet = CharSet.Ansi, EntryPoint = "Write", ExactSpelling = true, SetLastError = true)]
        public static extern bool hidWrite(int pHandle, ref byte pData);

        // Token: 0x0600008B RID: 139
        [DllImport("HHID.dll", CharSet = CharSet.Ansi, EntryPoint = "ReadEx", ExactSpelling = true, SetLastError = true)]
        public static extern bool hidReadEx(int pVendorID, int pProductID, ref byte pData);

        // Token: 0x0600008C RID: 140
        [DllImport("HHID.dll", CharSet = CharSet.Ansi, EntryPoint = "WriteEx", ExactSpelling = true, SetLastError = true)]
        public static extern bool hidWriteEx(int pVendorID, int pProductID, ref byte pData);

        // Token: 0x0600008D RID: 141
        [DllImport("HHID.dll", CharSet = CharSet.Ansi, EntryPoint = "GetHandle", ExactSpelling = true, SetLastError = true)]
        public static extern int hidGetHandle(int pVendoID, int pProductID);

        // Token: 0x0600008E RID: 142
        [DllImport("HHID.dll", CharSet = CharSet.Ansi, EntryPoint = "GetVendorID", ExactSpelling = true, SetLastError = true)]
        public static extern int hidGetVendorID(int pHandle);

        // Token: 0x0600008F RID: 143
        [DllImport("HHID.dll", CharSet = CharSet.Ansi, EntryPoint = "GetProductID", ExactSpelling = true, SetLastError = true)]
        public static extern int hidGetProductID(int pHandle);

        // Token: 0x06000090 RID: 144
        [DllImport("HHID.dll", CharSet = CharSet.Ansi, EntryPoint = "GetVersion", ExactSpelling = true, SetLastError = true)]
        public static extern int hidGetVersion(int pHandle);

        // Token: 0x06000091 RID: 145
        [DllImport("HHID.dll", CharSet = CharSet.Ansi, EntryPoint = "GetVendorName", ExactSpelling = true, SetLastError = true)]
        public static extern int hidGetVendorName(int pHandle, [MarshalAs(UnmanagedType.VBByRefStr)] ref string pText, int pLen);

        // Token: 0x06000092 RID: 146
        [DllImport("HHID.dll", CharSet = CharSet.Ansi, EntryPoint = "GetProductName", ExactSpelling = true, SetLastError = true)]
        public static extern int hidGetProductName(int pHandle, [MarshalAs(UnmanagedType.VBByRefStr)] ref string pText, int pLen);

        // Token: 0x06000093 RID: 147
        [DllImport("HHID.dll", CharSet = CharSet.Ansi, EntryPoint = "GetSerialNumber", ExactSpelling = true, SetLastError = true)]
        public static extern int hidGetSerialNumber(int pHandle, [MarshalAs(UnmanagedType.VBByRefStr)] ref string pText, int pLen);

        // Token: 0x06000094 RID: 148
        [DllImport("HHID.dll", CharSet = CharSet.Ansi, EntryPoint = "GetInputReportLength", ExactSpelling = true, SetLastError = true)]
        public static extern int hidGetInputReportLength(int pHandle);

        // Token: 0x06000095 RID: 149
        [DllImport("HHID.dll", CharSet = CharSet.Ansi, EntryPoint = "GetOutputReportLength", ExactSpelling = true, SetLastError = true)]
        public static extern int hidGetOutputReportLength(int pHandle);

        // Token: 0x06000096 RID: 150
        [DllImport("HHID.dll", CharSet = CharSet.Ansi, EntryPoint = "SetReadNotify", ExactSpelling = true, SetLastError = true)]
        public static extern void hidSetReadNotify(int pHandle, bool pValue);

        // Token: 0x06000097 RID: 151
        [DllImport("HHID.dll", CharSet = CharSet.Ansi, EntryPoint = "IsReadNotifyEnabled", ExactSpelling = true, SetLastError = true)]
        public static extern bool hidIsReadNotifyEnabled(int pHandle);

        // Token: 0x06000098 RID: 152
        [DllImport("HHID.dll", CharSet = CharSet.Ansi, EntryPoint = "IsAvailable", ExactSpelling = true, SetLastError = true)]
        public static extern bool hidIsAvailable(int pVendorID, int pProductID);

        // Token: 0x06000099 RID: 153
        [DllImport("user32", CharSet = CharSet.Ansi, EntryPoint = "CallWindowProcA", ExactSpelling = true, SetLastError = true)]
        public static extern int CallWindowProc(int lpPrevWndFunc, int hwnd, int Msg, int wParam, int lParam);

        // Token: 0x0600009A RID: 154
        [DllImport("user32", CharSet = CharSet.Ansi, EntryPoint = "SetWindowLongA", ExactSpelling = true, SetLastError = true)]
        public static extern int SetWindowLong(int hwnd, int nIndex, int dwNewLong);

        // Token: 0x0600009B RID: 155
        [DllImport("USER32.DLL", CharSet = CharSet.Ansi, EntryPoint = "SetWindowLongA", ExactSpelling = true, SetLastError = true)]
        public static extern int DelegateSetWindowLong(int hwnd, int attr, HIDDLLInterface.SubClassProcDelegate lval);

        // Token: 0x0600009C RID: 156 RVA: 0x00029BAC File Offset: 0x00027DAC
        public static void ConnectToHID(ref Form targetForm)
        {
            int fwinHandle = targetForm.Handle.ToInt32();
            HIDDLLInterface.FWinHandle = fwinHandle;
            fwinHandle = ((((HIDDLLInterface.hidConnect(HIDDLLInterface.FWinHandle) == true) ? true : false)) ? 0 : 1);
            HIDDLLInterface.FPrevWinProc = HIDDLLInterface.DelegateSetWindowLong(HIDDLLInterface.FWinHandle, -4, HIDDLLInterface.Ref_WinProc);
            HIDDLLInterface.HostForm = targetForm;
        }

        // Token: 0x0600009D RID: 157 RVA: 0x00029BFC File Offset: 0x00027DFC
        public static bool DisconnectFromHID()
        {
            HIDDLLInterface.SetWindowLong(HIDDLLInterface.FWinHandle, -4, HIDDLLInterface.FPrevWinProc);
            return true;
        }

        // Token: 0x0600009E RID: 158 RVA: 0x00029C1C File Offset: 0x00027E1C
        private static int WinProc(int pHWnd, int pMsg, int wParam, int lParam)
        {
            if (decimal.Compare(new decimal(pMsg), 32968m) == 0)
            {
                switch (wParam)
                {
                    case 1:
                        {
                            object hostForm = HIDDLLInterface.HostForm;
                            Type type = null;
                            string memberName = "OnPlugged";
                            object[] array = new object[]
                            {
                                lParam
                            };
                            object[] arguments = array;
                            string[] argumentNames = null;
                            Type[] typeArguments = null;
                            bool[] array2 = new bool[]
                            {
                        true
                            };
                            NewLateBinding.LateCall(hostForm, type, memberName, arguments, argumentNames, typeArguments, array2, true);
                            if (array2[0])
                            {
                                lParam = (int)Conversions.ChangeType(RuntimeHelpers.GetObjectValue(array[0]), typeof(int));
                            }
                            break;
                        }
                    case 2:
                        {
                            object hostForm2 = HIDDLLInterface.HostForm;
                            Type type2 = null;
                            string memberName2 = "OnUnplugged";
                            object[] array3 = new object[]
                            {
                                lParam
                            };
                            object[] arguments2 = array3;
                            string[] argumentNames2 = null;
                            Type[] typeArguments2 = null;
                            bool[] array2 = new bool[]
                            {
                        true
                            };
                            NewLateBinding.LateCall(hostForm2, type2, memberName2, arguments2, argumentNames2, typeArguments2, array2, true);
                            if (array2[0])
                            {
                                lParam = (int)Conversions.ChangeType(RuntimeHelpers.GetObjectValue(array3[0]), typeof(int));
                            }
                            break;
                        }
                    case 3:
                        NewLateBinding.LateCall(HIDDLLInterface.HostForm, null, "OnChanged", new object[0], null, null, null, true);
                        break;
                    case 4:
                        {
                            object hostForm3 = HIDDLLInterface.HostForm;
                            Type type3 = null;
                            string memberName3 = "OnRead";
                            object[] array3 = new object[]
                            {
                                lParam
                            };
                            object[] arguments3 = array3;
                            string[] argumentNames3 = null;
                            Type[] typeArguments3 = null;
                            bool[] array2 = new bool[]
                            {
                                true
                            };
                            NewLateBinding.LateCall(hostForm3, type3, memberName3, arguments3, argumentNames3, typeArguments3, array2, true);
                            if (array2[0])
                            {
                                lParam = (int)Conversions.ChangeType(RuntimeHelpers.GetObjectValue(array3[0]), typeof(int));
                            }
                            break;
                        }
                }
            }
            return HIDDLLInterface.CallWindowProc(HIDDLLInterface.FPrevWinProc, pHWnd, pMsg, wParam, lParam);
        }

        // Token: 0x0400003B RID: 59
        public const int WM_APP = 32768;

        // Token: 0x0400003C RID: 60
        public const short GWL_WNDPROC = -4;

        // Token: 0x0400003D RID: 61
        private const decimal WM_HID_EVENT = 32968m;

        // Token: 0x0400003E RID: 62
        private const short NOTIFY_PLUGGED = 1;

        // Token: 0x0400003F RID: 63
        private const short NOTIFY_UNPLUGGED = 2;

        // Token: 0x04000040 RID: 64
        private const short NOTIFY_CHANGED = 3;

        // Token: 0x04000041 RID: 65
        private const short NOTIFY_READ = 4;

        // Token: 0x04000042 RID: 66
        private static int FPrevWinProc;

        // Token: 0x04000043 RID: 67
        private static int FWinHandle;

        // Token: 0x04000044 RID: 68
        private static HIDDLLInterface.SubClassProcDelegate Ref_WinProc = new HIDDLLInterface.SubClassProcDelegate(HIDDLLInterface.WinProc);

        // Token: 0x04000045 RID: 69
        private static object HostForm;

        // Token: 0x0200000A RID: 10
        // (Invoke) Token: 0x060000A2 RID: 162
        public delegate int SubClassProcDelegate(int hwnd, int msg, int wParam, int lParam);
    }
}
