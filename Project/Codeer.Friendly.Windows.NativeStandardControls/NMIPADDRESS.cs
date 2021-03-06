﻿using System;
using System.Runtime.InteropServices;

namespace Codeer.Friendly.Windows.NativeStandardControls
{
#if ENG
    /// <summary>
    /// Please refer to MSDN's LVCOLUMN.
    /// </summary>
#else
    /// <summary>
    /// MSDNのLVCOLUMNを参照お願いします。
    /// </summary>
#endif
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct NMIPADDRESS
    {
        public NMHDR hdr;
        public int iField;
        public int iValue;
    }
}
