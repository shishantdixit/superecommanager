@echo off
set PGPASSWORD=postgres
"C:\Program Files\PostgreSQL\18\bin\psql.exe" -h localhost -p 5432 -U postgres -d superecommanager -f "d:\Code\repo\superecommanager\insert_migration_history.sql"
echo Exit code: %ERRORLEVEL%
pause
