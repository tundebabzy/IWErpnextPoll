using Sage.Peachtree.API;
using Serilog;
using System;
using System.Collections.Generic;

namespace IWErpnextPoll
{
    internal class CreatePurchaseOrderHandler : AbstractDocumentHandler, IResourceAddress
    {
        public CreatePurchaseOrderHandler(Company c, ILogger logger, EmployeeInformation employeeInformation=null) : base(c, logger, employeeInformation) { }
        public override object Handle(object request)
        {
            var purchaseOrder = CreateNewPurchaseOrder(request as PurchaseOrderDocument);
            if (GetNext() == null)
            {
                this.SetNext(purchaseOrder != null ? new LogPurchaseOrderHandler(Company, Logger, EmployeeInformation) : null);
            }
            return base.Handle(request);
        }

        private PurchaseOrder CreateNewPurchaseOrder(PurchaseOrderDocument purchaseOrderDocument)
        {
            var purchaseOrder = Company.Factories.PurchaseOrderFactory.Create();
            if (purchaseOrder != null)
            {
                try
                {
                    purchaseOrder.Date = purchaseOrderDocument.TransactionDate;
                    purchaseOrder.GoodThroughDate = purchaseOrderDocument.ScheduleDate;
                    purchaseOrder.ReferenceNumber = purchaseOrderDocument.Name;
                    purchaseOrder.VendorReference = VendorReferences[purchaseOrderDocument.Supplier];
                    purchaseOrder.TermsDescription = purchaseOrderDocument.PaymentTermsTemplate;
                    purchaseOrder.ShipVia = purchaseOrderDocument.ShipMethod;

                    foreach (var line in purchaseOrderDocument.Items)
                    {
                        AddLine(purchaseOrder, line);
                    }

                    purchaseOrder.Save();
                    Logger.Information("Purchase Order - {PurchaseOrderDocument} saved successfully", purchaseOrderDocument.Name);
                }
                catch (KeyNotFoundException)
                {
                    purchaseOrder = null;
                    Logger.Debug("Vendor {@Name} in {@Document} was not found", purchaseOrderDocument.Supplier, purchaseOrderDocument.Name);
                    SetNext(new CreateSupplierHandler(Company, Logger, EmployeeInformation));
                    Logger.Debug("Customer {@name} has been queued for creation in Sage", purchaseOrderDocument.Supplier);
                }
                catch (Sage.Peachtree.API.Exceptions.ValidationException e)
                {
                    Logger.Debug("Validation failed.");
                    Logger.Debug(e.Message);
                    Logger.Debug("{@Name} will be sent back to the queue", purchaseOrderDocument.Name);
                    purchaseOrder = null;
                }
                catch (Sage.Peachtree.API.Exceptions.RecordInUseException)
                {
                    purchaseOrder = null;
                    Logger.Debug("Record is in use. {@Name} will be sent back to the queue", purchaseOrderDocument.Name);
                }
                catch (Exception e)
                {
                    purchaseOrder = null;
                    Logger.Debug(e, e.Message);
                    Logger.Debug("{@E}", e);
                }
            }
            return purchaseOrder;
        }

        private void AddLine(PurchaseOrder purchaseOrderDocument, PurchaseOrderItem line)
        {
            var _ = purchaseOrderDocument.AddLine();
            var itemReference = ItemReferences[line.ItemCode];
            var stockItem = Company.Factories.StockItemFactory.Load(itemReference as EntityReference<StockItem>);
            _.AccountReference = stockItem.COGSAccountReference;
            _.Quantity = line.Qty;
            _.UnitPrice = line.Rate;
            _.Amount = line.Amount;
            _.Description = line.Description;
            _.InventoryItemReference = itemReference;
        }

        public string GetResourceServerAddress()
        {
            throw new NotImplementedException();
        }
    }
}