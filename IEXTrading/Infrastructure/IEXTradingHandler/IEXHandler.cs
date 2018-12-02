using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using IEXTrading.Models;
using Newtonsoft.Json;

namespace IEXTrading.Infrastructure.IEXTradingHandler
{
    public class IEXHandler
    {
        static string BASE_URL = "https://api.iextrading.com/1.0/"; //This is the base URL to which  method specific URL is appended.
        HttpClient httpClient;

        public IEXHandler()
        {
            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        }

        /****
         * This method calls the IEX reference API to get the list of existing symbols. 
        ****/
        public List<Company> GetSymbols()
        {
            string IEXTrading_API_PATH = BASE_URL + "ref-data/symbols";
            string companyList = "";

            List<Company> companies = null;

            httpClient.BaseAddress = new Uri(IEXTrading_API_PATH);
            HttpResponseMessage response = httpClient.GetAsync(IEXTrading_API_PATH).GetAwaiter().GetResult();
            if (response.IsSuccessStatusCode)
            {
                companyList = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }

            if (!companyList.Equals(""))
            {
                companies = JsonConvert.DeserializeObject<List<Company>>(companyList);
               
            }
            return companies;
        }

        public List<CompanyQuote> GetCompanyQuotes(List<Company> companyList)
        {
            string symbols = "";
            // Create quotelist to store the list of stocks and their data
            List<CompanyQuote> quoteList = new List<CompanyQuote>();
            //quoteDict variable created to store the output of json.convert utility method 
            Dictionary<string, Dictionary<string, CompanyQuote>> quoteDict = null;
            int Start = 0;
            int End = 100;
            int Count = 100;
           
            while (End <= companyList.Count)
            {
                int count = 0;
                symbols = "";

                foreach (var company in companyList.GetRange(Start, Count))
                {
                    count++;
                    symbols = symbols + company.symbol + ",";
                }


                string IEXTrading_API_PATH = BASE_URL + "stock/market/batch?symbols=" + symbols + "&types=quote";
                string quoteResponse = "";

                
                HttpResponseMessage response = httpClient.GetAsync(IEXTrading_API_PATH).GetAwaiter().GetResult();
                if (response.IsSuccessStatusCode)
                {
                    quoteResponse = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                }
                quoteDict = new Dictionary<string, Dictionary<string, CompanyQuote>>();
                if (!string.IsNullOrEmpty(quoteResponse))
                {
                    quoteDict = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, CompanyQuote>>>(quoteResponse);
                }
                // To extract the stock details of stock, create a list of stock quotes and store it as squoteList
                foreach (var quoteItem in quoteDict)
                {

                    foreach (var quote in quoteItem.Value)
                    {
                        if (quote.Value != null)
                        {
                            quoteList.Add(quote.Value);
                        }
                    }
                }

               // Logic to extract the last batch of stocks from the companyList
               Start = End;
               End = End + 100;
                if (End > companyList.Count)
                {
                    Count =End - companyList.Count;
                }
            }
            
            return quoteList;
        }

        //function to compute best 5 stocks
        public List<CompanyQuote> GetBest5Picks(List<Company> companies,int n=5)
        {
            List<CompanyQuote> quoteList = new List<CompanyQuote>();
            quoteList = GetCompanyQuotes(companies);
            List<CompanyQuote> bestPickList = new List<CompanyQuote>();
            //to calculate the stockpotential using the current price minus 52-week low divided by 52-week high minus 52-week low. 
            foreach (var quote in quoteList)
            {
                if ((quote.week52High - quote.week52Low) != 0)
                {
                    quote.calculatedValue = (100*(quote.close - quote.week52Low) / (quote.week52High - quote.week52Low));
                }
                bestPickList.Add(quote);
            }
            //order the stocks and select top 5 values
            return bestPickList.OrderByDescending(a => a.calculatedValue).Take(n).ToList();
        }

        /****
         * This method calls the IEX stock API to get Annual chart for the provided symbol. 
        ****/
        public List<Equity> GetChart(string symbol)
        {
            

            string IEXTrading_API_PATH = BASE_URL + "stock/" + symbol + "/batch?types=chart&range=1y";

            string charts = "";
            List<Equity> Equities = new List<Equity>();
            httpClient.BaseAddress = new Uri(IEXTrading_API_PATH);
            HttpResponseMessage response = httpClient.GetAsync(IEXTrading_API_PATH).GetAwaiter().GetResult();
            if (response.IsSuccessStatusCode)
            {
                charts = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }
            if (!charts.Equals(""))
            {
                ChartRoot root = JsonConvert.DeserializeObject<ChartRoot>(charts, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                Equities = root.chart.ToList();
            }
            
            foreach (Equity Equity in Equities)
            {
                Equity.symbol = symbol;
            }

            return Equities;
        }
    }
}
