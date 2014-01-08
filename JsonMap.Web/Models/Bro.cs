using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JsonMap.Web.Models
{
    public class Bro
    {
        public int BroId { get; set; }

        public string Name { get; set; }

        public List<Bro> Bros { get; set; }

        public Bro BestBro { get; set; }

        public Bro()
        {
            Bros = new List<Bro>();
        }
    }
}