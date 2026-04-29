using System;
using System.Collections.Generic;

namespace MonitoringAndEvaluationPlatform.Helpers
{
    public static class ArabicNumberHelper
    {
        public static string ToArabicWords(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return string.Empty;
            }

            long number = (long)Math.Floor(Math.Abs(value));

            if (number < 1000)
            {
                return number.ToString();
            }

            long billions = number / 1_000_000_000;
            long afterBillions = number % 1_000_000_000;
            long millions = afterBillions / 1_000_000;
            long afterMillions = afterBillions % 1_000_000;
            long thousands = afterMillions / 1_000;
            long remainder = afterMillions % 1_000;

            var parts = new List<string>();

            if (billions > 0)
            {
                parts.Add(billions == 1 ? "مليار" : billions + " مليار");
            }
            if (millions > 0)
            {
                parts.Add(millions == 1 ? "مليون" : millions + " مليون");
            }
            if (thousands > 0)
            {
                if (thousands == 1)
                {
                    parts.Add("ألف");
                }
                else if (thousands == 10)
                {
                    parts.Add("عشرة آلاف");
                }
                else
                {
                    parts.Add(thousands + " ألف");
                }
            }
            if (remainder > 0)
            {
                parts.Add(remainder.ToString());
            }

            return string.Join(" و", parts);
        }
    }
}
