using Sage.Peachtree.API;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using Sage.Peachtree.API.Collections.Generic;

namespace IWErpnextPoll
{
    abstract class AbstractDocumentHandler : IDocumentHandler
    {
        private IDocumentHandler _nextHandler;

        protected Company Company { get; set; }

        protected Dictionary<string, EntityReference> ItemReferences { get; set; }

        protected ILogger Logger { get; set; }

        protected Dictionary<string, EntityReference<Vendor>> VendorReferences { get; set; }

        protected EmployeeInformation EmployeeInformation { get; set; }


        protected AbstractDocumentHandler(Company c, ILogger logger, EmployeeInformation employeeInformation)
        {
            Company = c;
            Logger = logger;
            EmployeeInformation = employeeInformation;
            VendorReferences = VendorToReferenceDictionary();
            ItemReferences = InventoryItemToReferenceDictionary();
        }

        private Dictionary<string, EntityReference<Vendor>> VendorToReferenceDictionary()
        {
            var dictionary = new Dictionary<string, EntityReference<Vendor>>();
            var vendorList = Company.Factories.VendorFactory.List();
            vendorList.Load();
            foreach (var vendor in vendorList)
            {
                try
                {
                    dictionary.Add(vendor.Name, vendor.Key);
                }
                catch (ArgumentException e)
                {
                    Logger.Debug(e.Message);
                    Logger.Debug("Key was -> {0}, value was {1}", vendor.Name, vendor.Key);
                    Logger.Debug("Moving on.");
                }
            }

            return dictionary;
        }

        private Dictionary<string, EntityReference> InventoryItemToReferenceDictionary()
        {
            var keyValuePairs = new Dictionary<string, EntityReference>();
            var inventoryItems = Company.Factories.InventoryItemFactory.List();
            inventoryItems.Load();
            foreach (var item in inventoryItems)
            {
                try
                {
                    keyValuePairs.Add(item.ID, item.Key);
                }
                catch (ArgumentException e)
                {
                    Logger.Debug(e.Message);
                    Logger.Debug("Key was -> {0}, value was {1}", item.ID, item.Key);
                    Logger.Debug("Moving on.");
                }
            }
            return keyValuePairs;
        }

        protected static CustomerDocument GetCustomerFromErpNext(string name)
        {
            var receiver = new CustomerCommand(name, $"{GetResourceServerAddress()}?cn={name}");
            var customerDocument = receiver.Execute();
            return customerDocument.Data.Message;
        }

        private static string GetResourceServerAddress()
        {
            return $"{Constants.ServerUrl}/api/method/electro_erpnext.utilities.customer.get_customer_details";
        }

        protected EntityReference GetItemEntityReference(string itemCode)
        {
            try
            {
                var itemList = Company.Factories.InventoryItemFactory.List();
                var filter = GetPropertyContainsLoadModifiers("InventoryItem.ID", itemCode);
                itemList.Load(filter);
                var entity = itemList.FirstOrDefault((item => item.ID == itemCode));
                return entity?.Key;
            }
            catch (Exception e)
            {
                Logger.Debug($"Could not get item entity reference. @{e.Message}");
                return null;
            }
        }

        protected EntityReference<Customer> GetCustomerEntityReference(string documentOldCustomerId)
        {
            try
            {
                var customerList = Company.Factories.CustomerFactory.List();
                var filter = GetPropertyContainsLoadModifiers("Customer.ID", documentOldCustomerId);
                customerList.Load(filter);

                var entity = customerList.FirstOrDefault((customer => customer.ID == documentOldCustomerId));
                return entity?.Key;
            }
            catch (Exception e)
            {
                Logger.Debug($"Could not get customer entity reference. @{e.Message}");
                return null;
            }
        }

        private static LoadModifiers GetPropertyContainsLoadModifiers(string propertyString, string identifier)
        {
            var filter = LoadModifiers.Create();
            var property = FilterExpression.Property(propertyString);
            var value = FilterExpression.StringConstant(identifier);
            var filterParams = FilterExpression.Contains(property, value);
            filter.Filters = filterParams;
            return filter;
        }

        public IDocumentHandler SetNext(IDocumentHandler handler)
        {
            this._nextHandler = handler;
            return handler;
        }

        protected IDocumentHandler GetNext()
        {
            return _nextHandler;
        }

        public virtual object Handle(object request)
        {
            return _nextHandler?.Handle(request);
        }
    }
}
