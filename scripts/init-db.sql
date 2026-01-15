-- SuperEcomManager Database Initialization Script
-- This script runs when the PostgreSQL container starts for the first time

-- Create the shared schema for cross-tenant data
CREATE SCHEMA IF NOT EXISTS shared;

-- Grant permissions
GRANT ALL PRIVILEGES ON SCHEMA shared TO postgres;
GRANT ALL PRIVILEGES ON SCHEMA public TO postgres;

-- Create extension for UUID generation
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- Function to create tenant schema
CREATE OR REPLACE FUNCTION create_tenant_schema(schema_name VARCHAR)
RETURNS VOID AS $$
BEGIN
    EXECUTE format('CREATE SCHEMA IF NOT EXISTS %I', schema_name);
    EXECUTE format('GRANT ALL PRIVILEGES ON SCHEMA %I TO postgres', schema_name);
END;
$$ LANGUAGE plpgsql;

-- Log initialization
DO $$
BEGIN
    RAISE NOTICE 'SuperEcomManager database initialized successfully';
END $$;
