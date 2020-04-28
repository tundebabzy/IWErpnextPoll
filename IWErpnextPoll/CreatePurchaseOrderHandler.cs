using Sage.Peachtree.API;
using Serilog;
using System;
using System.Collections.Generic;

namespace IWErpnextPoll
{
    internal class CreatePurchaseOrderHandler : AbstractDocumentHandler
    {
        public CreatePurchaseOrderHandler(Company c, ILogger logger) : base(c, logger) { }
        public override object Handle(object request)
        {
            PurchaseOrder purchaseOrder = CreateNewPurchaseOrder(request as PurchaseOrderDocument);
            this.SetNext(purchaseOrder != null ? new LogPurchaseOrderHandler(Company, Logger) : null);
            return base.Handle(request);
        }

        private PurchaseOrder CreateNewPurchaseOrder(PurchaseOrderDocument purchaseOrderDocument)
        {
            PurchaseOrder purchaseOrder = Company.Factories.PurchaseOrderFactory.Create();
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
#if DEBUG
                    Logger.Information("Purchase Order - {PurchaseOrderDocument} saved successfully", purchaseOrderDocument.Name);
#endif
                }
                catch (KeyNotFoundException e)
                {
                    purchaseOrder = null;
                    Logger.Debug(e, e.Message);
                    Logger.Debug("@{E}", e);
                }
                catch (Sage.Peachtree.API.Exceptions.ValidationException e)
                {
                    // among others, it could be a duplicate
                    Logger.Debug(e, e.Message);
                    Logger.Debug("@{E}", e);
                    Logger.Debug("@{PurchaseOrderDocument}", purchaseOrderDocument.Name);
                }
                catch (Sage.Peachtree.API.Exceptions.RecordInUseException e)
                {
                    purchaseOrder = null;
                    Logger.Debug(e, e.Message);
                    Logger.Debug("@{E}", e);
                    Logger.Debug("@{PurchaseOrderDocument} will be sent back to the queue", purchaseOrderDocument.Name);
                }
                catch (Exception e)
                {
                    purchaseOrder = null;
                    Logger.Debug(e, e.Message);
                    Logger.Debug("@{E}", e);
                }
            }
            return purchaseOrder;
        }

        private void AddLine(PurchaseOrder purchaseOrderDocument, PurchaseOrderItem line)
        {
            PurchaseOrderLine _ = purchaseOrderDocument.AddLine();
            EntityReference itemReference = ItemReferences[line.ItemCode];
            StockItem stockItem = Company.Factories.StockItemFactory.Load(itemReference as EntityReference<StockItem>);
            _.AccountReference = stockItem.COGSAccountReference;
            _.Quantity = line.Qty;
            _.UnitPrice = line.Rate;
            _.Amount = line.Amount;
            _.Description = line.Description;
            _.InventoryItemReference = itemReference;
        }
    }
}