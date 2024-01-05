using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using BlobStorageExampleApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace BlobStorageExampleApi.Controllers;

// StorageAccount -> Container(s) -> Blob(s)
[ApiController]
[Route("[controller]")]
public class StorageController : ControllerBase
{
    private const string ConnectionString = "DefaultEndpointsProtocol=https;AccountName=blobexamplestorage;AccountKey=wI9yCEUQCPnS/vIB+vqHifgFc/0qy31aIwmKkCmz1SYr4bTyaH03zVahF518BdRGHGzwmHxMZ7jN+AStKmLqXw==;EndpointSuffix=core.windows.net";
    private const string Containers = "Containers";
    private const string Properties = "Properties";
    private const string Blobs = "Blobs";

    [HttpGet(Properties)]
    public async Task<IActionResult> GetPropertiesAsync()
    {
        var blobServiceClient = new BlobServiceClient(ConnectionString);
        var response = await blobServiceClient.GetPropertiesAsync();
        return Ok(response.Value);
    }

    [HttpGet(Containers)]
    public async Task<IActionResult> GetContainersAsync()
    {
        var blobServiceClient = new BlobServiceClient(ConnectionString);
        var response = blobServiceClient.GetBlobContainersAsync();
        var blobContainerItems = new List<BlobContainerItem>();
        await foreach (var blobContainerItem in response)
        {
            blobContainerItems.Add(blobContainerItem);
        }
        return Ok(blobContainerItems);
    }

    [HttpPost(Containers)]
    public async Task<IActionResult> AddContainerAsync(Container container)
    {
        if (string.IsNullOrWhiteSpace(container.Name))
        {
            throw new NullReferenceException($"Container name cannot be null.");
        }
        var blobContainerClient = new BlobContainerClient(ConnectionString, container.Name);
        var response = await blobContainerClient.CreateIfNotExistsAsync();
        if (response == null)
        {
            return Problem($"{container.Name} already exists.");
        }
        return Ok(response.Value);
    }

    [HttpGet($"{Containers}/{Blobs}/{{container}}")]
    public async Task<IActionResult> GetBlobsAsync(string container)
    {
        var blobContainerClient = new BlobContainerClient(ConnectionString, container);
        var pageableBlobItems = blobContainerClient.GetBlobsAsync();
        var blobItems = new List<BlobItem>();
        await foreach (var blobItem in pageableBlobItems)
        {
            blobItems.Add(blobItem);
        }
        return Ok(blobItems);
    }

    [HttpGet($"{Containers}/{Blobs}/{{container}}/{{blob}}")]
    public async Task<IActionResult> GetBlobAsync(string container, string blob)
    {
        var blobContainerClient = new BlobContainerClient(ConnectionString, container);
        var blobClient = blobContainerClient.GetBlobClient(blob);
        var stream = new MemoryStream();
        var response = await blobClient.DownloadToAsync(stream);
        if (response.IsError)
        {
            return Problem($"Error retrieving file from blob {blob}");
        }
        stream.Position = 0;
        var buffer = stream.ToArray();
        return new FileContentResult(buffer, "application/octet-stream");
    }

    [HttpPut($"{Containers}/{Blobs}/{{container}}/{{blob}}")]
    public async Task<IActionResult> AddTagsAsync(Tag tag, string container, string blob)
    {
        var blobContainerClient = new BlobContainerClient(ConnectionString, container);
        var containerExistsResponse = await blobContainerClient.ExistsAsync();

        if (!containerExistsResponse.Value)
        {
            await blobContainerClient.CreateAsync();
        }

        var blobClient = blobContainerClient.GetBlobClient(blob);
        if (blobClient != null)
        {
            var uploadResponse = await blobClient.SetTagsAsync(tag.KeyValues.ToDictionary(x => x.Key, x => x.Value));

            if (uploadResponse != null && !uploadResponse.IsError)
            {
                return Ok();
            }
            else
            {
                return Problem($"Tags could not be added to {blob} blob.");
            }
        }
        else
        {
            return Problem($"{blob} blob was not found.");
        }
    }

    [HttpPut($"{Containers}/{Blobs}/{{container}}")]
    public async Task<IActionResult> FindBlobsByTagsAsync(string container, TagQuery tagQuery)
    {
        var taggedBlobItems = new List<TaggedBlobItem>();
        var blobContainerClient = new BlobContainerClient(ConnectionString, container);

        // "Type" = 'txt'
        // "Date" >= '2020-01-01' AND "Date" < '2021-12-01'
        await foreach (TaggedBlobItem taggedBlobItem in blobContainerClient.FindBlobsByTagsAsync(tagQuery.ToQueryString()))
        {
            taggedBlobItems.Add(taggedBlobItem);
        }

        return Ok(taggedBlobItems);
    }

    [HttpPost($"{Containers}/{Blobs}/{{container}}/{{blob}}")]
    public async Task<IActionResult> UploadFileAsync(IFormFile formFile, string container, string blob)
    {
        using var stream = new MemoryStream();
        await formFile.CopyToAsync(stream);
        stream.Position = 0;
        var blobContainerClient = new BlobContainerClient(ConnectionString, container);
        var containerExistsResponse = await blobContainerClient.ExistsAsync();

        if (!containerExistsResponse.Value)
        {
            await blobContainerClient.CreateAsync();
        }

        var blobClient = blobContainerClient.GetBlobClient(blob);
        if (blobClient != null)
        {
            var uploadTags = new Dictionary<string, string>
            {
                ["FileName"] = formFile.FileName
            };

            var uploadResponse = await blobClient.UploadAsync(stream, new BlobUploadOptions
            {
                Metadata = uploadTags,
                Tags = uploadTags
            });

            if (uploadResponse != null)
            {
                return Ok(uploadResponse.Value);
            }
            else
            {
                return Problem($"{formFile.Name} file could not be added.");
            }
        }
        else
        {
            return Problem($"{blob} blob could not be added.");
        }
    }

}
