using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ITV2ADI_Engine.ITV2ADI_Workers
{
    public class ITVConversionFunctions
    {
        private const string Digits = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        private const string Min = "-1Y2P0IJ32E8E8";

        private const string Max = "1Y2P0IJ32E8E7";

        public string GetBillingId(string paid)
        {
            ///taken from https://github.com/tallesl/net-36/blob/master/Library/Base36.cs
            ///however i didnt want the library or reinvent the wheel :)
            long value = Convert.ToInt32(paid.TrimStart('0'));

            if (value == long.MinValue)
                return Min;

            var negative = value < 0;
            value = Math.Abs(value);
            var encoded = string.Empty;

            do
            {
                encoded = Digits[(int)(value % Digits.Length)] + encoded;
            }
            while ((value /= Digits.Length) != 0);

            return negative ? "-" + encoded : encoded;
        }
    }
}
