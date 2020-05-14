﻿using Sage.Peachtree.API;
using Serilog;
using System;
using System.Collections.Generic;

namespace IWErpnextPoll
{
    internal class CreateSalesInvoiceHandler : AbstractDocumentHandler
    {
        public CreateSalesInvoiceHandler(Company company, ILogger logger) : base(company, logger) { }

        public override object Handle(object request)
        {
            SalesInvoice salesInvoice = CreateNewSalesInvoice(request as SalesInvoiceDocument);
            this.SetNext(salesInvoice != null ? new LogSalesInvoiceHandler(Company, Logger) : null);
            return base.Handle(request);
        }

        private SalesInvoice CreateNewSalesInvoice(SalesInvoiceDocument document)
        {
            SalesInvoice salesInvoice = Company.Factories.SalesInvoiceFactory.Create();
            if (salesInvoice != null)
            {
                try
                {
                    // TODO: customer ID is a more reliable way to get Sage Customers
                    salesInvoice.CustomerReference = CustomerReferences[document.Customer];
                    salesInvoice.CustomerPurchaseOrderNumber = document.PoNo;
                    salesInvoice.CustomerNote = document.NotesOrSpecialInstructions;
                    salesInvoice.Date = document.PostingDate;
                    salesInvoice.DateDue = document.DueDate;
                    salesInvoice.DiscountAmount = document.DiscountAmount;
                    salesInvoice.ReferenceNumber = document.Name;
                    salesInvoice.ShipVia = document.ShippingMethod;
                    salesInvoice.TermsDescription = document.PaymentTermsTemplate;

                    foreach (var line in document.Items)
                    {
                        AddLine(salesInvoice, line);
                    }

                    salesInvoice.Save();
                    Logger.Information("Sales Invoice - {0} was saved successfully", document.Name);
                }
                catch (KeyNotFoundException)
                {
                    Logger.Debug("Customer {@name} in {@Document} was not found in Sage.", document.Customer, document.Name);
                    salesInvoice = null;
                    SetNext(new CreateCustomerHandler(Company, Logger));
                    Logger.Debug("Customer {@name} has been queued for creation in Sage", document.Customer);
                }
                catch (Sage.Peachtree.API.Exceptions.RecordInUseException)
                {
                    // abort. The unsaved data will eventually be re-queued
                    salesInvoice = null;
                    Logger.Debug("Record is in use. {@Name} will be sent back to the queue", document.Name);
                }
                catch (Sage.Peachtree.API.Exceptions.ValidationException e)
                {
                    Logger.Debug("Validation failed.");
                    Logger.Debug(e.Message);
                    Logger.Debug("{@Name} will be sent back to the queue", document.Name);
                    salesInvoice = null;
                }
                catch (Exception e)
                {
                    salesInvoice = null;
                    Logger.Debug(e, e.Message);
                    Logger.Debug("{@E}", e);
                }
            }
            return salesInvoice;
        }

        private void AddLine(SalesInvoice salesInvoice, SalesInvoiceItem line)
        {
            if (line.ForFreight == 1)
            {
                salesInvoice.FreightAmount = line.Amount;
            }
            else if (line.ForHandling != 1)
            {
                SalesInvoiceSalesLine _ = salesInvoice.AddSalesLine();
                EntityReference itemReference = ItemReferences[line.ItemCode];
                ServiceItem item = Company.Factories.ServiceItemFactory.Load(itemReference as EntityReference<ServiceItem>);
                _.AccountReference = item.SalesAccountReference;
                _.Quantity = line.Qty;
                _.UnitPrice = line.Rate;
                _.Amount = line.Amount;
                _.Description = line.Description;
                _.InventoryItemReference = itemReference;
            }
        }
    }
}