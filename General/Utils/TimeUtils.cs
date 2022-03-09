using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Net.General.Utils
{
    public static class TimeUtils
    {
        public static readonly DateTime UTC_TIME = new DateTime(1970, 1, 1);

        /// <summary>
        /// 转换成时间戳
        /// </summary>
        /// <param name="timeStamp"></param>
        /// <returns></returns>
        public static DateTime ConvertTime(long timeStamp)
        {
            var startTime = TimeZoneInfo.ConvertTimeFromUtc(UTC_TIME, TimeZoneInfo.Local);
            return startTime.AddMilliseconds(timeStamp);
        }

        /// <summary>
        /// 转换成时间
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static long ConvertTime(DateTime dateTime)
        {
            var startTime = TimeZoneInfo.ConvertTimeFromUtc(UTC_TIME, TimeZoneInfo.Local);
            return (long)(dateTime - startTime).TotalMilliseconds;
        }

        public static uint ConvertTimeToUInt(DateTime time)
        {
            return (uint)(Convert.ToInt64(time.Subtract(UTC_TIME).TotalMilliseconds) & 0xffffffff);
        }

        /// <summary>
        /// 获得明日零点时间戳 
        /// </summary>
        /// <returns></returns>
        public static long GetNextDayTimeSpan(DateTime timeSpan)
        {
            return ConvertTime(timeSpan.Date.AddDays(1));
        }

        /// <summary>
        /// 获取一个时间到现在经过了多少天
        /// </summary>
        /// <param name="timeSpan"></param>
        /// <returns></returns>
        public static int GetAfterDay(DateTime timeSpan)
        {
            return (DateTime.UtcNow.Date - timeSpan.Date).Days;
        }
    }
}
