using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTDataSQLiteConverter.Entities
{
    public class TableColumn
    {
        public string Name { get; set; }
        public DBColumnType Type { get; set; }
        public long Offset { get; set; }

        public override string ToString()
        {
            return $"{Name} ({Type} at {Offset:X8})";
        }
    }

    public enum DBColumnType
    {
        Unknown,
        Byte,
        Short,
        Int,
        Float,
        Int64,
        Double,
        Id,
        String,
        Unicode,
    }
}
