using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Guestbook.Services.ElasticSearch
{
    public class ElasticSearchConfig
    {
        private string userName = "elastic";
        private string passWord = "12345678";
        private string Index = "guestbook";
        private string uri = "http://192.168.13.139:9200";
    }
}
