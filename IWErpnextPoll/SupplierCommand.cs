using RestSharp;

namespace IWErpnextPoll
{
    internal class SupplierCommand
    {
        private readonly Resource _receiver;
        protected string SupplierName { get; set; }

        public SupplierCommand(string supplierName, string serverURL = "https://portal.electrocomptr.com")
        {
            _receiver = new Resource(serverURL);
            SupplierName = supplierName;
        }

        public IRestResponse<SupplierResponse> Execute()
        {
            IRestResponse<SupplierResponse> documents = _receiver.GetSupplierDetails();
            return documents;
        }
    }
}