using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Guestbook.Services.ElasticSearch
{
    public class ConnectElasticSearch
    {
        private readonly string userName;
        private readonly string passWord;
        private readonly string _Index = "guestbook";
        public string Index 
        { 
            get 
            {
                return _Index;
            }
        }
        private readonly string uri;
        public ConnectionSettings connectionSettings;
        public ElasticClient elasticClient;
        public ConnectElasticSearch()
        {
            this.userName = "elastic";
            this.passWord = "12345678";
            this.uri = "http://192.168.13.139:9200";
            this.connectionSettings = new ConnectionSettings(new Uri(uri)).BasicAuthentication(userName, passWord);
            this.elasticClient = new ElasticClient(this.connectionSettings);
        }
    }
}
