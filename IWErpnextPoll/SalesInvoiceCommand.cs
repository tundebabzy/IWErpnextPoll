using RestSharp;

namespace IWErpnextPoll
{
    class SalesInvoiceCommand
    {
        private readonly Resource _receiver;

        public SalesInvoiceCommand(string serverUrl = "https://portal.electrocomptr.com")
        {
            _receiver = new Resource(serverUrl);
        }

        public IRestResponse<SalesInvoiceResponse> Execute()
        {
            IRestResponse<SalesInvoiceResponse> documents = _receiver.GetSalesInvoiceList();
            return documents;
        }
    }
}
