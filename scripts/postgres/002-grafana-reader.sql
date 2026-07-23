SELECT format('CREATE ROLE lemon_grafana LOGIN PASSWORD %L', :'grafana_password')
WHERE NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'lemon_grafana')
\gexec

SELECT format('ALTER ROLE lemon_grafana PASSWORD %L', :'grafana_password')
\gexec

GRANT CONNECT ON DATABASE lemondb TO lemon_grafana;
GRANT USAGE ON SCHEMA public TO lemon_grafana;
GRANT SELECT ON ALL TABLES IN SCHEMA public TO lemon_grafana;
GRANT SELECT ON ALL SEQUENCES IN SCHEMA public TO lemon_grafana;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT ON TABLES TO lemon_grafana;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT ON SEQUENCES TO lemon_grafana;
