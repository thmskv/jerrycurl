using global::System;
using global::Jerrycurl.Cqs.Metadata.Annotations;

namespace Jerrycurl.Test.Models.Database
{
	[Table("dbo", "Blog")]
	public class Blog	
	{
		[Id, Key("PK_Blog", 1)]
		public int Id { get; set; }
		public string Title { get; set; }
		[Ref("PK_BlogCategory", 1, "FK_Blog_BlogCategory")]
		public int? CategoryId { get; set; }
	}
	
	[Table("dbo", "BlogAuthor")]
	public class BlogAuthor	
	{
		[Key("PK_BlogAuthor", 1), Ref("PK_Blog", 1, "FK_BlogAuthor_Blog")]
		public int BlogId { get; set; }
		public string Name { get; set; }
		public string TwitterUrl { get; set; }
	}
	
	[Table("dbo", "BlogCategory")]
	public class BlogCategory	
	{
		[Id, Key("PK_BlogCategory", 1)]
		public int Id { get; set; }
		[Ref("PK_BlogCategory", 1, "FK_BlogCategory_BlogCategory")]
		public int? ParentId { get; set; }
		public string Name { get; set; }
	}
	
	[Table("dbo", "BlogPost")]
	public class BlogPost	
	{
		[Id, Key("PK_BlogPost", 1)]
		public int Id { get; set; }
		[Ref("PK_Blog", 1, "FK_BlogPost_Blog")]
		public int BlogId { get; set; }
		public DateTime CreatedOn { get; set; }
		public string Headline { get; set; }
		public string Content { get; set; }
	}
	
	[Table("dbo", "BlogTag")]
	public class BlogTag	
	{
		[Key("PK_BlogTag", 1)]
        public int Id { get; set; }
		public string Name { get; set; }
	}
	
	[Table("dbo", "BlogTagMap")]
	public class BlogTagMap	
	{
		[Key("PK_BlogTagMap", 1), Ref("PK_BlogPost", 1, "FK_BlogTagMap_BlogPost")]
		public int BlogPostId { get; set; }
		[Key("PK_BlogTagMap", 2), Ref("PK_BlogTag", 1, "FK_BlogTagMap_BlogTag")]
		public int BlogTagId { get; set; }
	}
	
}
