using Sage.Peachtree.API;
using Serilog;
using System;

namespace IWErpnextPoll
{
    internal class CreateCustomerHandler : AbstractDocumentHandler
    {
        public CreateCustomerHandler(Company company, ILogger logger) : base(company, logger) { }

        public override object Handle(object request)
        {
            Customer customer = CreateNewCustomer(request as CustomerDocument);
            this.SetNext(customer != null ? new LogSalesOrderHandler(Company, Logger) : null);
            return base.Handle(request);
        }

        private Customer CreateNewCustomer(CustomerDocument customerDocument)
        {
            throw new NotImplementedException();
        }
    }
}