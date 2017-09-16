/* 
    PostgeSQL setup script for Exceptional
    Run this script for creating the exceptions table
    It will also upgrade a V1 schema to V2, just run the full script.
*/
CREATE TABLE IF NOT EXISTS "public"."Errors" (
    "Id" serial8 NOT NULL,
    "GUID" uuid NOT NULL,
    "ApplicationName" varchar(50) NOT NULL,
    "MachineName" varchar(50) NOT NULL,
    "CreationDate" timestamp NOT NULL,
    "Type" varchar(100) NOT NULL,
    "IsProtected" bool DEFAULT False NOT NULL,
    "Host" varchar(100),
    "Url" varchar(500),
    "HTTPMethod" varchar(10),
    "IPAddress" varchar(40),
    "Source" varchar(100),
    "Message" varchar(1000),
    "Detail" text,
    "StatusCode" int4,
    "DeletionDate" timestamp,
    "FullJson" text,
    "ErrorHash" int4,
    "DuplicateCount" int4 DEFAULT 1 NOT NULL,
    "LastLogDate" timestamp,
    "Category" varchar(100),
    PRIMARY KEY ("Id")
) WITH (OIDS=FALSE);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Exceptions_GUID_ApplicationName_DeletionDate_CreationDate" ON "public"."Errors" ("GUID", "ApplicationName", "CreationDate" DESC, "DeletionDate");

CREATE INDEX IF NOT EXISTS "IX_Exceptions_ErrorHash_ApplicationName_CreationDate_DeletionDa" ON "public"."Errors" ("ApplicationName", "CreationDate" DESC, "DeletionDate", "ErrorHash");

CREATE INDEX IF NOT EXISTS "IX_Exceptions_ApplicationName_DeletionDate_CreationDate_Filtere" ON "public"."Errors" ("ApplicationName", "CreationDate" DESC, "DeletionDate") WHERE "DeletionDate" IS NULL;

/* Begin V2 Schema changes */

ALTER TABLE "public"."Errors" ADD COLUMN IF NOT EXISTS "LastLogDate" timestamp;

ALTER TABLE "public"."Errors" ADD COLUMN IF NOT EXISTS "Category" varchar(100);

ALTER TABLE "public"."Errors" DROP COLUMN IF EXISTS "SQL";