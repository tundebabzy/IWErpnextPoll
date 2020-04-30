using Sage.Peachtree.API;
using Serilog;
using System;
using System.Collections.Generic;

namespace IWErpnextPoll
{
    abstract class AbstractDocumentHandler : IDocumentHandler
    {
        private IDocumentHandler _nextHandler;

        protected Company Company { get; set; }

        protected Dictionary<string, EntityReference<Customer>> CustomerReferences { get; set; }

        protected Dictionary<string, EntityReference> ItemReferences { get; set; }

        protected ILogger Logger { get; set; }

        protected Dictionary<string, EntityReference<Vendor>> VendorReferences { get; set; }


        public AbstractDocumentHandler(Company c, ILogger logger)
        {
            Company = c;
            Logger = logger;
            CustomerReferences = CustomerToReferenceDictionary();
            VendorReferences = VendorToReferenceDictionary();
            ItemReferences = InventoryItemToReferenceDictionary();
        }

        private Dictionary<string, EntityReference<Vendor>> VendorToReferenceDictionary()
        {
            Dictionary<string, EntityReference<Vendor>> dictionary = new Dictionary<string, EntityReference<Vendor>>();
            VendorList vendorList = Company.Factories.VendorFactory.List();
            vendorList.Load();
            foreach (var vendor in vendorList)
            {
                try
                {
                    dictionary.Add(vendor.Name, vendor.Key);
                }
                catch (ArgumentException e)
                {
                    Logger.Debug(e, e.Message);
                }
            }

            return dictionary;
        }

        private Dictionary<string, EntityReference> InventoryItemToReferenceDictionary()
        {
            Dictionary<string, EntityReference> keyValuePairs = new Dictionary<string, EntityReference>();
            InventoryItemList inventoryItems = Company.Factories.InventoryItemFactory.List();
            inventoryItems.Load();
            foreach (var item in inventoryItems)
            {
                keyValuePairs.Add(item.ID, item.Key);
            }
            return keyValuePairs;
        }

        public Dictionary<string, EntityReference<Customer>> CustomerToReferenceDictionary()
        {
            Dictionary<string, EntityReference<Customer>> keyValuePairs = new Dictionary<string, EntityReference<Customer>>();
            CustomerList customers = Company.Factories.CustomerFactory.List();
            customers.Load();
            foreach (var customer in customers)
            {
                try
                {
                    keyValuePairs.Add(customer.Name, customer.Key);
                }
                catch (ArgumentException e)
                {
                    // already in queue so pass
                    Logger.Debug(e, e.Message);
                }
            }
            return keyValuePairs;
        }

        public IDocumentHandler SetNext(IDocumentHandler handler)
        {
            this._nextHandler = handler;
            return handler;
        }

        public virtual object Handle(object request)
        {
            if (this._nextHandler != null)
            {
                return this._nextHandler.Handle(request);
            }
            else
            {
                return null;
            }
        }
    }
}
