using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Domain.Entities
{
    public class Holiday
    {
        public DateTime Date { get; set; }
        public string Day { get; set; } = "";
        public string Reason { get; set; } = "";
        public string Type { get; set; } = "";
    }

}
