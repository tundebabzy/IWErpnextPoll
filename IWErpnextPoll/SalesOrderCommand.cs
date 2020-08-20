using RestSharp;

namespace IWErpnextPoll
{
    class SalesOrderCommand
    {
        private readonly Resource _receiver;

        public SalesOrderCommand(string serverUrl = "https://portal.electrocomptr.com")
        {
            _receiver = new Resource(serverUrl);
        }

        public IRestResponse<SalesOrderResponse> Execute()
        {
            var documents = _receiver.GetSalesOrderList();
            return documents;
        }
    }
}
