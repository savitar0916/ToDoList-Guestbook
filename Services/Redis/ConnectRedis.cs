using StackExchange.Redis;
using System;

namespace Guestbook.Services.Redis
{
    public class ConnectRedis
    {
        private string RedisConnectionString { get; set;}
        private ConnectionMultiplexer ConnectionMultiplexer { get; set; }
        public IDatabase Database { get; set; }
        public bool IsConnected { get; set; }
        public ConnectRedis()
        {
            RedisConnectionString = "192.168.13.139:6379,password=12345678";
            try 
            {
                ConnectionMultiplexer = ConnectionMultiplexer.Connect(RedisConnectionString);
                IsConnected = ConnectionMultiplexer.IsConnected;
                Database = ConnectionMultiplexer.GetDatabase();
            }
            catch (Exception e)
            {
                throw new Exception("Connect Redis Server Failed : " + e.Message.ToString());
            }
           
        }
    }
}
