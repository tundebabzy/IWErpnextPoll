﻿using Sage.Peachtree.API;
using Serilog;

namespace IWErpnextPoll
{
    internal class LogCustomerCreatedHandler : AbstractDocumentHandler
    {
        public LogCustomerCreatedHandler(Company company, ILogger logger) : base(company, logger) { }

        public override object Handle(object request)
        {
            LogCustomer(request as CustomerDocument);
            // no new handler as we have reached the end of the chain.
            return base.Handle(request);
        }

        private void LogCustomer(CustomerDocument customerDocument)
        {
            var url = $"{Constants.ServerUrl}/api/resource/Sage 50 Export Log";
            var resource = new Resource(url);
            resource.LogCustomer(customerDocument);
        }
    }
}