using Sage.Peachtree.API;
using Serilog;

namespace IWErpnextPoll
{
    internal class LogSupplierCreatedHandler : AbstractDocumentHandler
    {
        public LogSupplierCreatedHandler(Company company, ILogger logger): base(company, logger) { }

        public override object Handle(object request)
        {
            LogSupplier(request as SupplierDocument);
            // no new handler as we have reached the end of the chain.
            return base.Handle(request);
        }

        private void LogSupplier(SupplierDocument supplierDocument)
        {
            string url = "https://portal.electrocomptr.com/api/resource/Sage 50 Export Log";
            Resource resource = new Resource(url);
            resource.LogSupplier(supplierDocument);
        }
    }
}