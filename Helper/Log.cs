#region Revision info
/*
 * $Author: millz $
 * $Date: 2013-04-27 10:02:17 +0200 (Sat, 27 Apr 2013) $
 * $ID: $
 * $Revision: 21 $
 * $URL: http://subversion.assembla.com/svn/iwantmovement/trunk/IWantMovement/Helper/Log.cs $
 * $LastChangedBy: millz $
 * $ChangesMade: $
 */
#endregion

using System.Windows.Media;
using Styx.Common;

namespace IWantMovement.Helper
{
    class Log
    {

        public static void Info(string logText, params object[] args)
        {
            if (logText == null) return;
            Logging.Write(LogLevel.Normal, Colors.LawnGreen, "[IWM]: {0}", string.Format(logText, args));
        }

        public static void Warning(string logText, params object[] args)
        {
            if (logText == null) return;
            Logging.Write(LogLevel.Normal, Colors.Fuchsia, "[IWM Warning]: {0}", string.Format(logText, args));
        }

        public static void Debug(string logText, params object[] args)
        {
            if (logText == null) return;
            Logging.Write(LogLevel.Diagnostic, Colors.Aqua, "[IWM Debug]: {0}", string.Format(logText, args));
        }
    }
}
