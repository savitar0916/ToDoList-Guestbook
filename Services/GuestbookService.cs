using ProtobufTimestamp = Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Guestbook.Services.ElasticSearch;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gateway.Models;
using GuestbookServer;
using Microsoft.Extensions.Logging;

namespace Guestbook.Services
{
    public class GuestbookService : GuestbookServer.GuestbookProtoBuf.GuestbookProtoBufBase
    {
        /*private readonly ILogger<GuestbookService> _logger;
        public GuestbookService(ILogger<GuestbookService> logger)
        {
            _logger = logger;
        }*/
        public override Task<CreateGuestbookProtobufResponse> CreateGuestbookProtobuf(CreateGuestbookProtobufRequest createGuestbookProtobufRequest, ServerCallContext context)
        {
            Console.WriteLine("Call " + System.Reflection.MethodBase.GetCurrentMethod().Name + "Function");
            //要回傳的物件
            CreateGuestbookProtobufResponse createGuestbookProtobufResponse = new CreateGuestbookProtobufResponse();
            var now = DateTimeOffset.Now;
            var nowTimestamp = ProtobufTimestamp.Timestamp.FromDateTimeOffset(now);
            try
            {
                ConnectElasticSearch connectElasticSearch = new ConnectElasticSearch();
                var client = connectElasticSearch.elasticClient;
                //檢查Index是否存在 沒有的話就建立
                if (client.Indices.Exists(connectElasticSearch.Index).Exists)
                {
                    Console.WriteLine("Index Already Exists");
                }
                else
                {
                    var createIndexResponse = client.Indices.Create(connectElasticSearch.Index, c => c
                        .Map<GuestbookModel>(m => m
                            .AutoMap()));
                    if (createIndexResponse.IsValid)
                    {
                        Console.WriteLine("Index created successfully!");
                    }
                    else
                    {
                        createGuestbookProtobufResponse.Message = "Error creating index:" + createIndexResponse.DebugInformation;
                        createGuestbookProtobufResponse.Status = 500;
                        Console.WriteLine("Error creating index:" + createIndexResponse.DebugInformation);
                        return Task.FromResult(createGuestbookProtobufResponse);
                    }
                }
                //建立Document
                GuestbookModel guestbookModel = new GuestbookModel()
                {
                    Name = createGuestbookProtobufRequest.Name,
                    Title = createGuestbookProtobufRequest.Title,
                    Content = createGuestbookProtobufRequest.Content,
                    Status = createGuestbookProtobufRequest.Status,
                    Endtime = createGuestbookProtobufRequest.Endtime.ToDateTimeOffset()
                };
                var createDocumentResponse = client.Index(guestbookModel, i => i
                    .Index(connectElasticSearch.Index)
                    );
                if (createDocumentResponse.IsValid)
                {
                    Console.WriteLine("Document added successfully! ID: " + createDocumentResponse.Id);
                    createGuestbookProtobufResponse.Message = "Document added successfully! ID: " + createDocumentResponse.Id;
                    createGuestbookProtobufResponse.Status = 400;
                }
                else
                {
                    createGuestbookProtobufResponse.Message = "Error adding document: " + createDocumentResponse.DebugInformation;
                    createGuestbookProtobufResponse.Status = 500;
                    Console.WriteLine("Error adding document: " + createDocumentResponse.DebugInformation);
                }
                createGuestbookProtobufResponse.CreateAt = nowTimestamp;
                return Task.FromResult(createGuestbookProtobufResponse);
            }
            catch (Exception e)
            {
                createGuestbookProtobufResponse.CreateAt = nowTimestamp;
                createGuestbookProtobufResponse.Message = e.Message.ToString();
                createGuestbookProtobufResponse.Status = 500;
                return Task.FromResult(createGuestbookProtobufResponse);
            }
            
            

            
        }

        public override Task<GetGuestbookProtobufResponse> GetGuestbookProtobuf(GetGuestbookProtobufRequest getGuestbookProtobufRequest, ServerCallContext context)
        {
            //_logger.LogInformation("Call " + System.Reflection.MethodBase.GetCurrentMethod().Name + "Function");
            //回傳到Gateway的PB
            GetGuestbookProtobufResponse getGuestbookProtobufResponse = new GetGuestbookProtobufResponse();
            try
            {
                ConnectElasticSearch connectElasticSearch = new ConnectElasticSearch();
                var client = connectElasticSearch.elasticClient;
                //不管哪個欄位都查
                //var searchResponse = client.Search<GuestbookModel>(s => s
                //    .Index(connectElasticSearch.Index)
                //    .Query(q => q
                //        .Match(m => m
                //            .Query(getGuestbookProtobufRequest.Query))));
                //依據DSL查詢語法來做查詢
                //只查下面那三個欄位
                var searchResponse = client.Search<GuestbookModel>(s => s
                    .Index(connectElasticSearch.Index)
                    .Query(q => q
                        .Match(m => m
                            .Field(f => f.Name)
                            .Field(f => f.Title)
                            .Field(f => f.Content)
                            .Query(getGuestbookProtobufRequest.Query)
                            )
                        )
                    );
                
                //將回傳的所有資料Add進要回傳的PB裡面
                getGuestbookProtobufResponse.Message = "Get Document Success";
                getGuestbookProtobufResponse.Status = 400;

                if (searchResponse.HitsMetadata.Total.Value.Equals(0))
                {
                    getGuestbookProtobufResponse.Message = "The Query Can Not Found Anything";
                    getGuestbookProtobufResponse.Status = 400;
                    return Task.FromResult(getGuestbookProtobufResponse);
                }
                foreach (var hit in searchResponse.Hits)
                {
                    Console.WriteLine(searchResponse.HitsMetadata.Total.Value.ToString() + "see");
         
                    GuestbookServer.Guestbook guestbook = new GuestbookServer.Guestbook()
                    {
                        Id = hit.Id,
                        Name = hit.Source.Name,
                        Title = hit.Source.Title,
                        Content = hit.Source.Content,
                        Status = hit.Source.Status,
                        Endtime = ProtobufTimestamp.Timestamp.FromDateTimeOffset(hit.Source.Endtime)
                    };
                    getGuestbookProtobufResponse.Guestbooks.Add(guestbook);
                }
                
            }
            catch (Exception e) 
            {
                getGuestbookProtobufResponse.Message = e.Message.ToString();
                getGuestbookProtobufResponse.Status = 500;
            }
            return Task.FromResult(getGuestbookProtobufResponse);
        }

        public override Task<UpdateGuestbookProtobufResponse> UpdateGuestbookProtobuf(UpdateGuestbookProtobufRequest updateGuestbookProtobufRequest, ServerCallContext context)
        {
            Console.WriteLine("Call " + System.Reflection.MethodBase.GetCurrentMethod().Name + "Function");
            //  同步的寫法
            //  依據需求可以更改為使用異步的寫法
            //  要回傳的物件
            UpdateGuestbookProtobufResponse updateGuestbookProtobufResponse = new UpdateGuestbookProtobufResponse();
            try
            {
                ConnectElasticSearch connectElasticSearch = new ConnectElasticSearch();
                var client = connectElasticSearch.elasticClient;
                // 先找出該ID的文檔
                var searchResponse = client.Search<GuestbookModel>(s => s
                   .Index(connectElasticSearch.Index)
                   .Query(q => q
                       .Match(m => m
                            .Field(f => f.Id)
                            .Query(updateGuestbookProtobufRequest.Id)
                           )
                       )
                   );
                /*if (!searchResponse.IsValid)
                {
                    updateGuestbookProtobufResponse.Id = updateGuestbookProtobufRequest.Id;
                    updateGuestbookProtobufResponse.Message = updateGuestbookProtobufRequest.Id + "is not existed";
                    updateGuestbookProtobufResponse.Status = 500;
                    return Task.FromResult(updateGuestbookProtobufResponse);
                }
                */
                //建立Document
                GuestbookModel guestbookModel = new GuestbookModel()
                {
                    Name = updateGuestbookProtobufRequest.Name,
                    Title = updateGuestbookProtobufRequest.Title,
                    Content = updateGuestbookProtobufRequest.Content,
                    Status = updateGuestbookProtobufRequest.Status,
                    Endtime = updateGuestbookProtobufRequest.Endtime.ToDateTimeOffset()
                };
                var updateResponse = client.Update<GuestbookModel>(updateGuestbookProtobufRequest.Id, u => u
                .Index(connectElasticSearch.Index)
                .Doc(guestbookModel));

                /*if (!updateResponse.IsValid)
                {
                    updateGuestbookProtobufResponse.Id = updateGuestbookProtobufRequest.Id;
                    updateGuestbookProtobufResponse.Message = "Update Document Failed";
                    updateGuestbookProtobufResponse.Status = 500;
                    return Task.FromResult(updateGuestbookProtobufResponse);
                }*/
                updateGuestbookProtobufResponse.Id = updateGuestbookProtobufRequest.Id;
                updateGuestbookProtobufResponse.Message = "Update Document Success";
                updateGuestbookProtobufResponse.Status = 400;
                return Task.FromResult(updateGuestbookProtobufResponse);
            }
            catch (Exception e)
            {
                updateGuestbookProtobufResponse.Id = updateGuestbookProtobufRequest.Id;
                updateGuestbookProtobufResponse.Message = e.Message.ToString();
                updateGuestbookProtobufResponse.Status = 500;
                return Task.FromResult(updateGuestbookProtobufResponse);
            }
        }

        public override Task<DeleteGuestbookProtobufResponse> DeleteGuestbookProtobuf(DeleteGuestbookProtobufRequest deleteGuestbookProtobufRequest,
            ServerCallContext context) 
        {
            Console.WriteLine("Call " + System.Reflection.MethodBase.GetCurrentMethod().Name + "Function");
            DeleteGuestbookProtobufResponse deleteGuestbookProtobufResponse = new DeleteGuestbookProtobufResponse();

            ConnectElasticSearch connectElasticSearch = new ConnectElasticSearch();
            var client = connectElasticSearch.elasticClient;
            var deleteResponse = client.Delete<GuestbookModel>(deleteGuestbookProtobufRequest.Id, d => d
                .Index(connectElasticSearch.Index));
            Console.WriteLine(deleteGuestbookProtobufRequest.Id);
            if (!deleteResponse.IsValid)
            {
                deleteGuestbookProtobufResponse.Status = (int)deleteResponse.ApiCall.HttpStatusCode;
                deleteGuestbookProtobufResponse.Message = "Error Message:" + deleteResponse.ServerError.Error;
                return  Task.FromResult(deleteGuestbookProtobufResponse);
            }
            deleteGuestbookProtobufResponse.Status = 400;
            deleteGuestbookProtobufResponse.Message = "Delete Document Success";
            return  Task.FromResult(deleteGuestbookProtobufResponse);
        }
    }
}
