using Sage.Peachtree.API;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IWErpnextPoll
{
    class DocumentTypeHandler : AbstractDocumentHandler
    {
        public DocumentTypeHandler(Company c, ILogger logger): base(c, logger) { }
        public override object Handle(object request)
        {
            if ((request as SalesOrderDocument) != null && (request as SalesOrderDocument).Doctype == "Sales Order")
            {
                SetNext(new CreateSalesOrderHandler(Company, Logger));
            }
            else if ((request as PurchaseOrderDocument) != null && (request as PurchaseOrderDocument).Doctype == "Purchase Order")
            {
                SetNext(new CreatePurchaseOrderHandler(Company, Logger));
            }
            else
            {
                SetNext(null);
            }
            return base.Handle(request);
        }
    }
}
