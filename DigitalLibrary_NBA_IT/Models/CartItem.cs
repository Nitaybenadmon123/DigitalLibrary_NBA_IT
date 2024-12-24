using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DigitalLibrary_NBA_IT.Models
{
    public class CartItem
    {
        public Books Book { get; set; } // הספר עצמו
        public string Type { get; set; } // סוג הפעולה: "borrow" או "buy"
    }
}
