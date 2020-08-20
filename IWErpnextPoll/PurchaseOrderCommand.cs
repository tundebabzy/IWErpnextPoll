using RestSharp;

namespace IWErpnextPoll
{
    class PurchaseOrderCommand
    {
        private readonly Resource _receiver;

        public PurchaseOrderCommand(string serverUrl = "https://portal.electrocomptr.com")
        {
            _receiver = new Resource(serverUrl);
        }

        public IRestResponse<PurchaseOrderResponse> Execute()
        {
            IRestResponse<PurchaseOrderResponse> documents = _receiver.GetPurchaseOrderList();
            return documents;
        }
    }
}
