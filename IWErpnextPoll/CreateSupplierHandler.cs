using Sage.Peachtree.API;
using Serilog;

namespace IWErpnextPoll
{
    internal class CreateSupplierHandler : IDocumentHandler
    {
        private Company company;
        private ILogger logger;

        public CreateSupplierHandler(Company company, ILogger logger)
        {
            this.company = company;
            this.logger = logger;
        }
    }
}