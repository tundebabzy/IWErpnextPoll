using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IWErpnextPoll
{
    class CustomerCommand
    {
        private readonly Resource _receiver;
        protected string CustomerName { get; set; }

        public CustomerCommand(string customerName, string serverURL = "https://portal.electrocomptr.com")
        {
            _receiver = new Resource(serverURL);
            CustomerName = customerName;
        }

        public IRestResponse<CustomerResponse> Execute()
        {
            IRestResponse<CustomerResponse> documents = _receiver.GetCustomerDetails();
            return documents;
        }
    }
}
