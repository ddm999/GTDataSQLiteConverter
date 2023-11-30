using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTDataSQLiteConverter.Entities
{
    public class TableRow
    {
        public List<object> Cells { get; set; } = new List<object>();
    }
}
