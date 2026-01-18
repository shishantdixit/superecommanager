-- Terminate all connections
SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = 'superecommanager' AND pid <> pg_backend_pid();

-- Drop and recreate database
DROP DATABASE IF EXISTS superecommanager;
CREATE DATABASE superecommanager;
