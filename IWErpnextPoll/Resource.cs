﻿using RestSharp;
using System;

namespace IWErpnextPoll
{
    class Resource
    {
        private readonly IRestClient _restClient;
        private readonly string _apiSecret = "78e5615db2830e3";
        private readonly string _apiToken = "4e7b90c8fc265ac";
        public string BaseUrl { get; set; }
        public string Doctype { get; set; }
        public Resource(string baseUrl)
        {
            this.BaseUrl = baseUrl;
            _restClient = new RestClient(baseUrl);
            _restClient.AddDefaultHeader("Authorization", string.Format("token {0}:{1}", _apiToken, _apiSecret));
        }

        /**
         * Makes a request to the given ERPNext server and pulls the data in JSON format
         */
        public IRestResponse<SalesOrderResponse> GetSalesOrderList()
        {
            RestRequest request = new RestRequest(Method.GET);
            IRestResponse<SalesOrderResponse> response = _restClient.Execute<SalesOrderResponse>(request);
            return response;
        }

        public IRestResponse<PurchaseOrderResponse> GetPurchaseOrderList()
        {
            RestRequest request = new RestRequest(Method.GET);
            IRestResponse<PurchaseOrderResponse> response = _restClient.Execute<PurchaseOrderResponse>(request);
            return response;
        }

        public IRestResponse LogSalesOrder(SalesOrderDocument document)
        {
            Log log = new Log
            {
                document_name = document.Name,
                export_date = DateTime.Now.ToString("yyyy-MM-dd"),
                document_date = document.TransactionDate.ToString("yyyy-MM-dd"),
                document_type = "Sales Order"
            };
            RestRequest request = new RestRequest(Method.POST);
            request.AddJsonBody(log);
            IRestResponse response = _restClient.Execute(request);
            return response;
        }

        public IRestResponse LogPurchaseOrder(PurchaseOrderDocument document)
        {
            Log log = new Log
            {
                document_name = document.Name,
                export_date = DateTime.Now.ToString("yyyy-MM-dd"),
                document_date = document.TransactionDate.ToString("yyyy-MM-dd"),
                document_type = "Purchase Order"
            };
            RestRequest request = new RestRequest(Method.POST);
            request.AddJsonBody(log);
            IRestResponse response = _restClient.Execute(request);
            return response;
        }
    }
}
