using RestSharp;

namespace IWErpnextPoll
{
    class SalesOrderCommand
    {
        private readonly Resource _receiver;

        public SalesOrderCommand(string serverURL = "https://portal.electrocomptr.com")
        {
            _receiver = new Resource(serverURL);
        }

        public IRestResponse<SalesOrderResponse> Execute()
        {
            var documents = _receiver.GetSalesOrderList();
            return documents;
        }
    }
}
