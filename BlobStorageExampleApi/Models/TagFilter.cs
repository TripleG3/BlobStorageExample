namespace BlobStorageExampleApi.Models;

public class TagFilter
{
    public string Key { get; set; }
    public TagOperator TagOperator { get; set; }
    public string Value { get; set; }
}
