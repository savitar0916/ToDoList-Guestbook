using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Gateway.Models
{
    public class GuestbookModel
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Title { get; set; }

        public string Content { get; set; }

        public bool Status { get; set; }

        public DateTimeOffset Endtime { get; set; }
    }
}
