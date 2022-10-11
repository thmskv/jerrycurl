DROP TABLE IF EXISTS "BlogTagMap";
DROP TABLE IF EXISTS "BlogTag";
DROP TABLE IF EXISTS "BlogPost";
DROP TABLE IF EXISTS "Blog";
DROP TABLE IF EXISTS "BlogAuthor";
DROP TABLE IF EXISTS "BlogCategory";

CREATE TABLE "BlogCategory"
(
    "Id" integer PRIMARY KEY AUTOINCREMENT NOT NULL,
    "ParentId" int NULL,
    "Name" text NOT NULL,
    CONSTRAINT "FK_BlogCategory_BlogCategory" FOREIGN KEY ("ParentId") REFERENCES "BlogCategory"("Id")
);

CREATE TABLE "Blog"
(
    "Id" integer PRIMARY KEY AUTOINCREMENT NOT NULL,
    "Title" text NOT NULL,
    "CategoryId" int NOT NULL,
    CONSTRAINT "FK_Blog_BlogCategory" FOREIGN KEY ("CategoryId") REFERENCES "BlogCategory"("Id")
);


CREATE TABLE "BlogAuthor"
(
    "BlogId" int NOT NULL,
    "Name" text NOT NULL,
    "TwitterUrl" text NULL,
    CONSTRAINT "PK_BlogAuthor" PRIMARY KEY ("BlogId"),
	CONSTRAINT "FK_BlogAuthor_Blog" FOREIGN KEY ("BlogId") REFERENCES "Blog"("Id")
);

CREATE TABLE "BlogPost"
(
    "Id" integer PRIMARY KEY AUTOINCREMENT NOT NULL,
    "BlogId" int NOT NULL,
    "CreatedOn" datetime NOT NULL,
    "Headline" text NOT NULL,
    "Content" text NOT NULL,
    CONSTRAINT "FK_BlogPost_Blog" FOREIGN KEY ("BlogId") REFERENCES "Blog"("Id")
);

CREATE TABLE "BlogTag"
(
    "Id" integer PRIMARY KEY AUTOINCREMENT NOT NULL,
    "Name" nvarchar
);

CREATE TABLE "BlogTagMap"
(
    "BlogPostId" int NOT NULL,
    "BlogTagId" text NOT NULL,
    CONSTRAINT "PK_BlogTagMap" PRIMARY KEY ("BlogPostId", "BlogTagId"),
    CONSTRAINT "FK_BlogTagMap_BlogPost" FOREIGN KEY ("BlogPostId") REFERENCES "BlogPost"("Id"),
    CONSTRAINT "FK_BlogTagMap_BlogTag" FOREIGN KEY ("BlogTagId") REFERENCES "BlogTag"("Id")
);