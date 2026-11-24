using System;
using System.IO;

namespace G940LedPanel.Core
{
    /// <summary>
    /// 诊断文件统一存放处：程序目录 \ diag \。
    ///
    /// 规则只有一条——所有「给人看、用来查问题」的文件都放这里，不再散落在 exe 旁边。
    /// 以前几个诊断文件各写各的路径，结果是谁也说不清"日志在哪"；现在只有一个入口。
    ///
    /// 目录不存在时按需创建。创建失败（只读目录、U 盘拔了、权限不足）不让程序崩，
    /// 由调用方自己决定是否放弃——诊断功能永远不能把主功能带崩。
    /// </summary>
    internal static class DiagPaths
    {
        /// <summary>diag 目录完整路径（不保证已存在）。</summary>
        public static string Dir
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "diag"); }
        }

        /// <summary>diag 目录下某个文件的完整路径（不创建目录）。</summary>
        public static string PathOf(string fileName)
        {
            return Path.Combine(Dir, fileName);
        }

        /// <summary>确保 diag 目录存在。true = 可以往里写。</summary>
        public static bool Ensure()
        {
            try
            {
                if (!Directory.Exists(Dir)) Directory.CreateDirectory(Dir);
                return true;
            }
            catch (Exception) { return false; }
        }
    }
}
