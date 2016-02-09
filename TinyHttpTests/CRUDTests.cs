﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TinyHttp;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace TinyHttpTests
{
    [TestClass]
    public class CRUDTests
    {
        public static readonly string BaseUrl = "http://jsonplaceholder.typicode.com";


        [TestMethod]
        public async Task TestCreate()
        {
            dynamic data = new DataObject();
            data.title = "Test title";
            data.body = "Test body";
            data.userId = 1;
            HttpResponse<DataObject> response = await HttpClient.GetCreateRequest(BaseUrl + "/posts")
                .WithContent<object>(HttpClient.RequestContentTypes.Json,(object)data)
                .ExecuteAsync<DataObject>();

            Assert.IsTrue(response.ResponseStatus == System.Net.HttpStatusCode.Created);
            data = response.ResponseData;
            Assert.IsTrue(data.id > 0);
            Console.WriteLine("Created data with id: {0}", data.id);
            Console.WriteLine(DataObject.ToJson(response.ResponseData, true));
        }

        [TestMethod]
        public async Task TestUpdate()
        {
            dynamic data = new DataObject();
            data.title = "Test title";
            data.body = "Test body";
            data.userId = 1;
            data.id = 22;
            HttpResponse<DataObject> response = await HttpClient.GetUpdateRequest(BaseUrl + "/posts/22")
                .WithContent<object>(HttpClient.RequestContentTypes.Json, (object)data)
                .ExecuteAsync<DataObject>();

            Assert.IsTrue(response.ResponseStatus == System.Net.HttpStatusCode.OK);
            dynamic result = response.ResponseData;
            Assert.IsTrue(data.id == result.id);
            Console.WriteLine(DataObject.ToJson(response.ResponseData, true));
        }

        [TestMethod]
        public async Task TestDelete()
        {
            HttpResponse<DataObject> response = await HttpClient.GetDeleteRequest(BaseUrl + "/posts/5")
                .WithContent(HttpClient.RequestContentTypes.Json)
                .ExecuteAsync<DataObject>();

            Assert.IsTrue(response.ResponseStatus == System.Net.HttpStatusCode.OK);
            Console.WriteLine(DataObject.ToJson(response.ResponseData, true));
        }

        [TestMethod]
        public async Task TestRead()
        {
            HttpResponse<List<DataObject>> response = await HttpClient.GetReadRequest(BaseUrl + "/posts")
                .WithContent(HttpClient.RequestContentTypes.Json)
                .ExecuteAsync<List<DataObject>>();

            Assert.IsTrue(response.ResponseStatus == System.Net.HttpStatusCode.OK);
            Console.WriteLine(DataObject.ToJson(response.ResponseData, true));
        }

        [TestMethod]
        public async Task TestReadOne()
        {
            HttpResponse<DataObject> response = await HttpClient.GetReadRequest(BaseUrl + "/posts/3")
                .WithContent(HttpClient.RequestContentTypes.Json)
                .ExecuteAsync<DataObject>();

            Assert.IsTrue(response.ResponseStatus == System.Net.HttpStatusCode.OK);
            Console.WriteLine(DataObject.ToJson(response.ResponseData, true));
        }

        [TestMethod]
        public async Task TestReadFilter()
        {
            HttpResponse<List<DataObject>> response = await HttpClient.GetReadRequest(BaseUrl + "/posts",
                new NameValueCollection()
                {
                    {"userId","1" }
                })
                .WithContent(HttpClient.RequestContentTypes.Json)
                .ExecuteAsync<List<DataObject>>();

            Assert.IsTrue(response.ResponseStatus == System.Net.HttpStatusCode.OK);
            Console.WriteLine(DataObject.ToJson(response.ResponseData, true));
        }
    }
}
