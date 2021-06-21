using System.Collections.Generic;

namespace Moonglade.Web.Models
{
    // credits: https://datatables.net/forums/discussion/40690/sample-implementation-of-serverside-processing-in-c-mvc-ef-with-paging-sorting-searching

    public class DataTableRequest
    {
        public int Draw { get; set; }
        public int Start { get; set; }
        public int Length { get; set; }
        public List<DataColumn> Columns { get; set; }
        public SearchRequest Search { get; set; }
        public List<OrderInfo> Order { get; set; }
    }

    public class DataColumn
    {
        public string Data { get; set; }
        public string Name { get; set; }
        public bool Searchable { get; set; }
        public bool Orderable { get; set; }
        public SearchRequest Search { get; set; }
    }

    public class SearchRequest
    {
        public string Value { get; set; }
        public string Regex { get; set; }
    }

    public class OrderInfo
    {
        public int Column { get; set; }
        public string Dir { get; set; }
    }

    public class JqDataTable<T>
    {
        public int Draw { get; set; }
        public int RecordsTotal { get; set; }
        public int RecordsFiltered { get; set; }
        public IReadOnlyList<T> Data { get; set; }
    }
}
