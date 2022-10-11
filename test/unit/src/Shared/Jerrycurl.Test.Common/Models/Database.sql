DROP TABLE IF EXISTS "BlogTagMap";
DROP TABLE IF EXISTS "BlogTag";
DROP TABLE IF EXISTS "BlogPost";
DROP TABLE IF EXISTS "Blog";
DROP TABLE IF EXISTS "BlogAuthor";
DROP TABLE IF EXISTS "BlogCategory";

CREATE TABLE "BlogCategory"
(
    "Id" int NOT NULL IDENTITY(1, 1),
    "ParentId" int NULL,
    "Name" nvarchar(100) NOT NULL,
    CONSTRAINT "PK_BlogCategory" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_BlogCategory_BlogCategory" FOREIGN KEY ("ParentId") REFERENCES "BlogCategory"("Id")
);

CREATE TABLE "Blog"
(
    "Id" int NOT NULL IDENTITY(1, 1),
    "Title" nvarchar(100) NOT NULL,
    "CategoryId" int NOT NULL,
    CONSTRAINT "PK_Blog" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Blog_BlogCategory" FOREIGN KEY ("CategoryId") REFERENCES "BlogCategory"("Id")
);


CREATE TABLE "BlogAuthor"
(
    "BlogId" int NOT NULL,
    "Name" nvarchar(100) NOT NULL,
    "TwitterUrl" nvarchar(200) NULL,
    CONSTRAINT "PK_BlogAuthor" PRIMARY KEY ("BlogId"),
	CONSTRAINT "FK_BlogAuthor_Blog" FOREIGN KEY ("BlogId") REFERENCES "Blog"("Id"),
);

CREATE TABLE "BlogPost"
(
    "Id" int NOT NULL IDENTITY(1, 1),
    "BlogId" int NOT NULL,
    "CreatedOn" datetime NOT NULL,
    "Headline" nvarchar(100) NOT NULL,
    "Content" nvarchar(MAX) NOT NULL,
    CONSTRAINT "PK_BlogPost" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_BlogPost_Blog" FOREIGN KEY ("BlogId") REFERENCES "Blog"("Id")
);

CREATE TABLE "BlogTag"
(
    "Id" int NOT NULL IDENTITY(1, 1),
    "Name" nvarchar(20),
    CONSTRAINT "PK_BlogTag" PRIMARY KEY ("Name")
);

CREATE TABLE "BlogTagMap"
(
    "BlogPostId" int NOT NULL,
    "BlogTagId" nvarchar(20) NOT NULL,
    CONSTRAINT "PK_BlogTagMap" PRIMARY KEY ("BlogPostId", "BlogTagId"),
    CONSTRAINT "FK_BlogTagMap_BlogPost" FOREIGN KEY ("BlogPostId") REFERENCES "BlogPost"("Id"),
    CONSTRAINT "FK_BlogTagMap_BlogTag" FOREIGN KEY ("BlogTagId") REFERENCES "BlogTag"("Id")
);