using System.Text;

namespace BlobStorageExampleApi.Models;

public class TagQuery
{
    public IEnumerable<TagFilter> TagFilters { get; set; }

    public string ToQueryString()
    {
        var isFirst = true;
        var sb = new StringBuilder();
        foreach (var tagFilter in TagFilters)
        {
            var op = tagFilter.TagOperator switch
            {
                TagOperator.GreaterThan => ">",
                TagOperator.GreaterThanEquals => ">=",
                TagOperator.LessThan => "<",
                TagOperator.LessThanEquals => "<=",
                _ => "=",
            };

            if (isFirst)
            {
                isFirst = false;
            }
            else
            {
                sb.Append(" AND ");
            }

            sb.Append($"\"{tagFilter.Key}\" {op} '{tagFilter.Value}'");
        }
        return sb.ToString();
    }
}
