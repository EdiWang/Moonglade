using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moonglade.Data.Entities;

public class BlogAssetEntity
{
    public Guid Id { get; set; }

    public string Base64Data { get; set; }

    public DateTime LastModifiedTimeUtc { get; set; }
}