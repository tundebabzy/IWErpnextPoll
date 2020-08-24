using RestSharp;

namespace IWErpnextPoll
{
    internal class CustomerCommand
    {
        private readonly Resource _receiver;
        // private string CustomerName { get; set; }

        public CustomerCommand(string customerName, string serverUrl = "https://portal.electrocomptr.com")
        {
            _receiver = new Resource(serverUrl, customerName);
        }

        public IRestResponse<CustomerResponse> Execute()
        {
            var documents = _receiver.GetCustomerDetails();
            return documents;
        }
    }
}
