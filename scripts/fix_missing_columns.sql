-- SQL Script to add missing columns for tenant schema
-- Run this script against your PostgreSQL database

-- First, find your tenant's schema name
-- SELECT "Id", "Name", "Slug", "SchemaName" FROM shared."Tenants";

-- For tenant c0c887f9-50f0-40d5-9119-24ace85327d0, set the schema below:
-- Replace 'tenant_yourslug' with your actual tenant schema name from the query above

-- Option 1: If you know your schema name, set it here and run:
DO $$
DECLARE
    tenant_schema TEXT;
BEGIN
    -- Get the schema name for the specific tenant
    SELECT "SchemaName" INTO tenant_schema
    FROM shared."Tenants"
    WHERE "Id" = 'c0c887f9-50f0-40d5-9119-24ace85327d0';

    IF tenant_schema IS NULL THEN
        RAISE EXCEPTION 'Tenant not found';
    END IF;

    RAISE NOTICE 'Applying migrations to schema: %', tenant_schema;

    -- Set search path to tenant schema
    EXECUTE format('SET search_path TO %I', tenant_schema);

    -- Add ExternalOrderId column if it doesn't exist
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = tenant_schema
        AND table_name = 'shipments'
        AND column_name = 'ExternalOrderId'
    ) THEN
        EXECUTE format('ALTER TABLE %I."shipments" ADD COLUMN "ExternalOrderId" VARCHAR(50) NULL', tenant_schema);
        RAISE NOTICE 'Added ExternalOrderId column';
    ELSE
        RAISE NOTICE 'ExternalOrderId column already exists';
    END IF;

    -- Add ExternalShipmentId column if it doesn't exist
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = tenant_schema
        AND table_name = 'shipments'
        AND column_name = 'ExternalShipmentId'
    ) THEN
        EXECUTE format('ALTER TABLE %I."shipments" ADD COLUMN "ExternalShipmentId" VARCHAR(50) NULL', tenant_schema);
        RAISE NOTICE 'Added ExternalShipmentId column';
    ELSE
        RAISE NOTICE 'ExternalShipmentId column already exists';
    END IF;

    -- Create indexes
    EXECUTE format('CREATE INDEX IF NOT EXISTS "IX_shipments_ExternalOrderId" ON %I."shipments" ("ExternalOrderId")', tenant_schema);
    EXECUTE format('CREATE INDEX IF NOT EXISTS "IX_shipments_ExternalShipmentId" ON %I."shipments" ("ExternalShipmentId")', tenant_schema);

    RAISE NOTICE 'Created indexes';

    -- Create chat_conversations table if it doesn't exist
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.tables
        WHERE table_schema = tenant_schema
        AND table_name = 'chat_conversations'
    ) THEN
        EXECUTE format('
            CREATE TABLE %I."chat_conversations" (
                "Id" uuid NOT NULL PRIMARY KEY,
                "Title" VARCHAR(100) NOT NULL,
                "UserId" uuid NOT NULL,
                "Status" VARCHAR(20) NOT NULL,
                "MessageCount" INTEGER NOT NULL DEFAULT 0,
                "TotalTokensUsed" INTEGER NOT NULL DEFAULT 0,
                "LastMessageAt" TIMESTAMP WITH TIME ZONE,
                "Metadata" jsonb,
                "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL,
                "CreatedBy" uuid,
                "UpdatedAt" TIMESTAMP WITH TIME ZONE,
                "UpdatedBy" uuid
            )', tenant_schema);
        RAISE NOTICE 'Created chat_conversations table';
    ELSE
        RAISE NOTICE 'chat_conversations table already exists';
    END IF;

    -- Create chat_messages table if it doesn't exist
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.tables
        WHERE table_schema = tenant_schema
        AND table_name = 'chat_messages'
    ) THEN
        EXECUTE format('
            CREATE TABLE %I."chat_messages" (
                "Id" uuid NOT NULL PRIMARY KEY,
                "ConversationId" uuid NOT NULL REFERENCES %I."chat_conversations"("Id") ON DELETE CASCADE,
                "Role" VARCHAR(20) NOT NULL,
                "Content" TEXT NOT NULL,
                "Sequence" INTEGER NOT NULL,
                "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL,
                "TokenCount" INTEGER,
                "ToolCallId" VARCHAR(100),
                "ToolName" VARCHAR(100),
                "ToolCalls" jsonb,
                "Metadata" jsonb
            )', tenant_schema, tenant_schema);
        RAISE NOTICE 'Created chat_messages table';
    ELSE
        RAISE NOTICE 'chat_messages table already exists';
    END IF;

    -- Create chat indexes
    EXECUTE format('CREATE INDEX IF NOT EXISTS "IX_chat_conversations_UserId" ON %I."chat_conversations" ("UserId")', tenant_schema);
    EXECUTE format('CREATE INDEX IF NOT EXISTS "IX_chat_conversations_Status" ON %I."chat_conversations" ("Status")', tenant_schema);
    EXECUTE format('CREATE INDEX IF NOT EXISTS "IX_chat_conversations_CreatedAt" ON %I."chat_conversations" ("CreatedAt")', tenant_schema);
    EXECUTE format('CREATE INDEX IF NOT EXISTS "IX_chat_conversations_LastMessageAt" ON %I."chat_conversations" ("LastMessageAt")', tenant_schema);
    EXECUTE format('CREATE INDEX IF NOT EXISTS "IX_chat_conversations_UserId_Status" ON %I."chat_conversations" ("UserId", "Status")', tenant_schema);
    EXECUTE format('CREATE INDEX IF NOT EXISTS "IX_chat_messages_ConversationId" ON %I."chat_messages" ("ConversationId")', tenant_schema);
    EXECUTE format('CREATE INDEX IF NOT EXISTS "IX_chat_messages_ConversationId_Sequence" ON %I."chat_messages" ("ConversationId", "Sequence")', tenant_schema);
    EXECUTE format('CREATE INDEX IF NOT EXISTS "IX_chat_messages_CreatedAt" ON %I."chat_messages" ("CreatedAt")', tenant_schema);

    RAISE NOTICE 'Created chat indexes';
    RAISE NOTICE 'Migration completed successfully for schema: %', tenant_schema;
END $$;

-- Verify the changes
SELECT
    table_schema,
    table_name,
    column_name,
    data_type
FROM information_schema.columns
WHERE table_name = 'shipments'
AND column_name IN ('ExternalOrderId', 'ExternalShipmentId')
ORDER BY table_schema, column_name;
