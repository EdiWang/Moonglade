﻿namespace Moonglade.Data.Generated.Entities;

public class PostTagEntity
{
	public Guid PostId { get; set; }
	public int TagId { get; set; }

	public virtual PostEntity Post { get; set; }
	public virtual TagEntity Tag { get; set; }
}