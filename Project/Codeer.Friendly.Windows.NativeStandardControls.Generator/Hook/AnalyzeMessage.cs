﻿using System;

namespace Codeer.Friendly.Windows.NativeStandardControls.Generator.Hook
{
    /// <summary>
    /// メッセージ解析。
    /// </summary>
    /// <param name="handle">ハンドル。</param>
    /// <param name="message">メッセージ。</param>
    /// <param name="wparam">wparam。</param>
    /// <param name="lparam">lparam。</param>
    public delegate void AnalyzeMessage(IntPtr handle, int message, IntPtr wparam, IntPtr lparam);
}
