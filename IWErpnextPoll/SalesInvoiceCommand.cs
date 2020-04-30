using RestSharp;

namespace IWErpnextPoll
{
    class SalesInvoiceCommand
    {
        private readonly Resource _receiver;

        public SalesInvoiceCommand(string serverURL = "https://portal.electrocomptr.com")
        {
            _receiver = new Resource(serverURL);
        }

        public IRestResponse<SalesInvoiceResponse> Execute()
        {
            IRestResponse<SalesInvoiceResponse> documents = _receiver.GetSalesInvoiceList();
            return documents;
        }
    }
}
