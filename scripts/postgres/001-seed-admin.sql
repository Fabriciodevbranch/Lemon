INSERT INTO "Users" ("Id", "Email", "Name", "OAuthProvider", "OAuthProviderId", "PasswordHash", "CreatedAt")
VALUES (
  '00000000-0000-0000-0000-000000000001',
  'admin@admin.com',
  'Administrator',
  NULL,
  NULL,
  'TGVtb25Xcml0ZXJBZG1pbg==.T7T1DR72mS7fSvNS6LD19Ig5tJxAvNtLyMHtyexwXF0=',
  NOW()
)
ON CONFLICT ("Email") DO NOTHING;
