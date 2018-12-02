using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IEXTrading.Models
{
    public class CompanyQuote
    {
        public string symbol { get; set; }
        public string companyName { get; set; }
        public float? close { get; set; }
        public float? week52High { get; set; }
        public float? week52Low { get; set; }

        //property to store calculated value from 52-week strategy
        public float? calculatedValue { get; set; }
    }
}
