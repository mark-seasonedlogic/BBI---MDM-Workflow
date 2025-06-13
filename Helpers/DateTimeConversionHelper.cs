using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.Helpers
{
    public class DateTimeConversionHelper
    {
        public static DateTime ConvertToDeviceLocalTime(DateTime utcTimestamp, string? timeZoneId)
        {
            if (string.IsNullOrEmpty(timeZoneId)) return utcTimestamp; // fallback: UTC

            try
            {
                var tzInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                return TimeZoneInfo.ConvertTimeFromUtc(utcTimestamp, tzInfo);
            }
            catch (TimeZoneNotFoundException)
            {
                // fallback to UTC if timezone is invalid
                return utcTimestamp;
            }
        }

    }
}
