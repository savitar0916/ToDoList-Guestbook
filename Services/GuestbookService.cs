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

namespace Guestbook.Services
{
    public class GuestbookService : GuestbookServer.GuestbookProtoBuf.GuestbookProtoBufBase
    {
        public override Task<CreateGuestbookProtobufResponse> CreateGuestbookProtobuf(CreateGuestbookProtobufRequest createGuestbookProtobufRequest, ServerCallContext context)
        {
            //先定義好要回傳的相關訊息
            var now = DateTimeOffset.Now;
            var nowTimestamp = ProtobufTimestamp.Timestamp.FromDateTimeOffset(now);
            CreateGuestbookProtobufResponse createGuestbookResponse = new CreateGuestbookProtobufResponse
            {
                Message = "",
                CreateAt = nowTimestamp
            };
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
                    createGuestbookResponse.Message = "Error creating index:" + createIndexResponse.DebugInformation;
                    createGuestbookResponse.Status = 500;
                    Console.WriteLine("Error creating index:" + createIndexResponse.DebugInformation);
                    return Task.FromResult(createGuestbookResponse);
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
                createGuestbookResponse.Message = "Document added successfully! ID: " + createDocumentResponse.Id;
                createGuestbookResponse.Status = 400;
            }
            else
            {
                createGuestbookResponse.Message = "Error adding document: " + createDocumentResponse.DebugInformation;
                createGuestbookResponse.Status = 500;
                Console.WriteLine("Error adding document: " + createDocumentResponse.DebugInformation);
            }
            return Task.FromResult(createGuestbookResponse);
        }

        public override Task<GetGuestbookProtobufResponse> GetGuestbookProtobuf(GetGuestbookProtobufRequest getGuestbookProtobufRequest, ServerCallContext context)
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
                        .Field( f => f.Name)
                        .Field( f => f.Title)
                        .Field( f => f.Content)
                        .Query(getGuestbookProtobufRequest.Query)
                        )
                    )
                );
            //回傳到Gateway的PB
            GetGuestbookProtobufResponse getGuestbookProtobufResponse = new GetGuestbookProtobufResponse();
            //將回傳的所有資料Add進要回傳的PB裡面
            foreach (var hit in searchResponse.Hits)
            {
                GuestbookServer.Guestbook guestbook = new GuestbookServer.Guestbook() 
                {
                    Name = hit.Source.Name,
                    Title = hit.Source.Title,
                    Content = hit.Source.Content,
                    Status = hit.Source.Status,
                    Endtime = ProtobufTimestamp.Timestamp.FromDateTimeOffset(hit.Source.Endtime)
                };
                getGuestbookProtobufResponse.Guestbooks.Add(guestbook);
            }
            return Task.FromResult(getGuestbookProtobufResponse);
        }
    }
}
