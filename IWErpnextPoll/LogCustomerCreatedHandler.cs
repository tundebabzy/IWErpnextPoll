using Sage.Peachtree.API;
using Serilog;
using System;

namespace IWErpnextPoll
{
    internal class LogCustomerCreatedHandler : AbstractDocumentHandler
    {
        public LogCustomerCreatedHandler(Company company, ILogger logger, EmployeeInformation employeeInformation) : base(company, logger, employeeInformation) { }

        public override object Handle(object request)
        {
            LogCustomer(request as CustomerDocument);
            // no new handler as we have reached the end of the chain.
            return base.Handle(request);
        }

        private void LogCustomer(CustomerDocument customerDocument)
        {
            string url = "https://portal.electrocomptr.com/api/resource/Sage 50 Export Log";
            Resource resource = new Resource(url);
            resource.LogCustomer(customerDocument);
        }
    }
}