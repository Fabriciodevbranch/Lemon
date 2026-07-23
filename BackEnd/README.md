# Lemon Writer — backend e API

O backend do Lemon Writer é uma API ASP.NET Core 9 para autenticação, biblioteca de livros, capítulos, rascunhos, histórico de versões, planejamento narrativo e exportação. A solução usa arquitetura em camadas, CQRS com MediatR, PostgreSQL com Entity Framework Core, JWT, gRPC opcional e observabilidade com OpenTelemetry/Prometheus.

## Visão geral

| Projeto | Responsabilidade |
| --- | --- |
| `LemonWriter.API` | Controllers HTTP, autenticação, CORS, Swagger, health checks e composição da aplicação |
| `LemonWriter.Application` | Casos de uso, comandos, consultas, DTOs, validação e interfaces |
| `LemonWriter.Domain` | Entidades, eventos e contratos de persistência |
| `LemonWriter.Infrastructure` | PostgreSQL/EF Core, repositórios, autenticação, exportação, serviços e implementações gRPC |
| `tests/LemonWriter.Application.Tests` | Testes da camada de aplicação e autorização de recursos |

Fluxo típico: `Controller -> MediatR/serviço de aplicação -> repositório/DbContext -> PostgreSQL`.

## Executar localmente

### Docker Compose (stack completa)

Na raiz do repositório:

```bash
docker compose -f scripts/docker-compose.yaml up --build
```

Serviços principais:

- API REST: `http://localhost:8080`
- frontend: `http://localhost:4200`
- PostgreSQL: `localhost:5432`
- Prometheus: `http://localhost:9090`
- Grafana: `http://localhost:3000`
- Jaeger: `http://localhost:16686`
- Loki: `http://localhost:3100`
- cAdvisor: `http://localhost:8081`

O Compose usa uma chave JWT somente para desenvolvimento. Em qualquer ambiente compartilhado, defina `JWT_KEY` com um segredo aleatório de pelo menos 32 caracteres e não habilite `Security__AllowDevelopmentSecrets`.

### Apenas backend

Requisitos: SDK .NET `9.0.308` (ou patch compatível) e PostgreSQL.

```bash
cd BackEnd
dotnet restore
dotnet run --project src/LemonWriter.API
```

No perfil local, a API atende em `http://localhost:51686` e `https://localhost:51685`. Em `Development`, Swagger fica em `/swagger`; fora desse ambiente, a interface Swagger não é publicada.

## Configuração

A configuração aceita `appsettings*.json`, variáveis de ambiente e os demais provedores padrão do ASP.NET Core. Para variáveis, substitua `:` por `__`.

| Chave | Finalidade | Padrão local |
| --- | --- | --- |
| `ConnectionStrings:DefaultConnection` | Conexão PostgreSQL | `Host=localhost;Database=lemondb;Username=lemon;Password=lemon` |
| `Jwt:Key` | Assinatura HMAC-SHA256 dos tokens | deve ser substituída |
| `Jwt:Issuer` | Emissor aceito | `LemonWriter` |
| `Jwt:Audience` | Audiência aceita | `LemonWriterClient` |
| `Jwt:ExpiryMinutes` | Duração do token | `60` (`480` no Compose) |
| `Authentication:Google:ClientId/ClientSecret` | OAuth Google | placeholders |
| `Frontend:BaseUrl` | Destino do callback OAuth | `http://localhost:4200` |
| `Cors:AllowedOrigins` | Origens permitidas | frontend local |
| `Grpc:Enabled` | Publica serviços gRPC | `false` no Compose |
| `OpenTelemetry:Endpoint` | Coletor OTLP gRPC | `http://localhost:4317` |

Em desenvolvimento o banco é inicializado com `EnsureCreated`; nos demais ambientes, migrations são aplicadas automaticamente na inicialização.

## Autenticação e convenções

Somente registro, login, início/callback OAuth e endpoints operacionais são públicos. As demais rotas requerem:

```http
Authorization: Bearer <token>
```

Os tokens contêm o ID, e-mail e nome do usuário. Recursos pertencentes a outro usuário normalmente respondem `404`, evitando revelar sua existência. JSON usa a política padrão do ASP.NET Core (`camelCase`). Erros de negócio aparecem geralmente como `{ "error": "..." }` ou `{ "message": "..." }`; não há ainda um envelope de erro único.

Exemplo de login:

```bash
curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"writer@example.com","password":"secret"}'
```

Resposta:

```json
{
  "token": "<jwt>",
  "user": {
    "id": "<uuid>",
    "email": "writer@example.com",
    "displayName": "Writer",
    "avatarUrl": null,
    "createdAt": "2026-07-21T12:00:00Z"
  }
}
```

## Referência REST

Todas as rotas abaixo são relativas à URL da API. `🔒` indica JWT obrigatório.

### Autenticação

| Método | Rota | Corpo/entrada | Resultado principal |
| --- | --- | --- | --- |
| `POST` | `/api/auth/register` | `{ email, displayName, password }` | `200` com token e usuário; `409` se já existir |
| `POST` | `/api/auth/login` | `{ email, password }` | `200` com token e usuário; `401` se inválido |
| `GET` | `/api/auth/oauth/google` | — | inicia o desafio OAuth Google |
| `GET` | `/api/auth/oauth/google/complete` | callback do provedor | redireciona ao frontend com token e usuário |

No registro, `email` deve ser válido e ter no máximo 256 caracteres; `displayName` é obrigatório e aceita até 200 caracteres.

### Livros 🔒

| Método | Rota | Corpo/entrada | Resultado principal |
| --- | --- | --- | --- |
| `GET` | `/api/books` | `authorId` é aceito, mas a implementação usa o usuário do token | lista livros com `chapterCount`, `wordCount` e `progress` |
| `GET` | `/api/books/{id}` | UUID do livro | `BookDto` |
| `POST` | `/api/books` | `CreateBookRequest` | `201` com livro criado |
| `PUT` | `/api/books/{id}` | `UpdateBookRequest` | livro atualizado |
| `DELETE` | `/api/books/{id}` | — | `204` |
| `POST` | `/api/books/{id}/cover` | `multipart/form-data`, campo `cover` | `{ coverImageUrl }` |

Criação:

```json
{
  "title": "Meu livro",
  "description": "Sinopse",
  "authorId": "00000000-0000-0000-0000-000000000000",
  "authorName": "Nome público",
  "isbn": null,
  "inbr": null,
  "coverImageUrl": null,
  "isSeries": false,
  "seriesVolume": null,
  "seriesName": null
}
```

O `authorId` enviado na criação é ignorado: a autoria é obtida do JWT. `title` aceita até 500 caracteres e `authorName`, até 200. Capas aceitas: JPEG, PNG, GIF ou WebP, no máximo 5 MiB; o conteúdo é armazenado como data URL Base64.

### Capítulos 🔒

| Método | Rota | Corpo/entrada | Resultado principal |
| --- | --- | --- | --- |
| `GET` | `/api/chapters?bookId={uuid}` | livro | lista `ChapterDto` |
| `GET` | `/api/chapters/{id}` | capítulo | `ChapterDto` |
| `POST` | `/api/chapters` | `{ bookId, title, order }` | `201` |
| `PUT` | `/api/chapters/{id}` | `{ title, content, order }` | capítulo atualizado |
| `DELETE` | `/api/chapters/{id}` | — | `204` |

`title` aceita até 500 caracteres e `order` deve ser zero ou positivo.

### Rascunhos 🔒

| Método | Rota | Corpo/entrada | Resultado principal |
| --- | --- | --- | --- |
| `GET` | `/api/drafts?chapterId={uuid}` | capítulo | lista `DraftDto` |
| `GET` | `/api/drafts/{id}` | rascunho | `DraftDto` |
| `POST` | `/api/drafts` | `{ chapterId, title, initialContent? }` | `201` |
| `PUT` | `/api/drafts/{id}` | `{ title, content }` | rascunho atualizado |
| `POST` | `/api/drafts/{id}/publish` | `{ publishMessage }` | publica e cria snapshot |
| `DELETE` | `/api/drafts/{id}` | — | `204` |

Títulos aceitam até 500 caracteres. `publishMessage` é obrigatório e aceita até 1.000.

### Snapshots 🔒

| Método | Rota | Corpo/entrada | Resultado principal |
| --- | --- | --- | --- |
| `GET` | `/api/snapshots/{id}` | snapshot | `SnapshotDto` |
| `GET` | `/api/snapshots/timeline/{chapterId}` | capítulo | linha do tempo de snapshots |
| `POST` | `/api/snapshots` | `{ chapterId, content, snapshotMessage }` | snapshot criado |
| `POST` | `/api/snapshots/restore` | `{ chapterId, snapshotId }` | capítulo restaurado |
| `POST` | `/api/snapshots/grab-content` | `{ targetChapterId, snapshotId }` | conteúdo aplicado ao capítulo-alvo |
| `GET` | `/api/snapshots/compare?baseId={uuid}&compareId={uuid}` | dois snapshots | snapshots e diferenças |

O conteúdo e a mensagem são obrigatórios; a mensagem aceita até 1.000 caracteres.

### Story Studio 🔒

Base: `/api/books/{bookId}/studio`. Tipos aceitos: `characters`, `places`, `objects`, `timeline`, `goals`, `lore`, `magic`, `research` e `gallery`.

| Método | Rota | Corpo/entrada | Resultado principal |
| --- | --- | --- | --- |
| `GET` | `/{type}` | tipo suportado | lista entradas |
| `POST` | `/{type}` | `CreateStudioEntry` | `201`; limite da requisição: 7 MiB |
| `PATCH` | `/goals/{id}/progress` | `{ progress }` | meta atualizada |
| `PUT` | `/timeline/order` | `{ ids: [uuid, ...] }` | `204` |
| `DELETE` | `/entries/{id}` | — | `204` |
| `GET` | `/relationships` | — | lista relações |
| `POST` | `/relationships` | `{ from, to, label?, tone? }` | relação criada |
| `DELETE` | `/relationships/{id}` | — | `204` |
| `GET` | `/metrics` | — | métricas da história ou objeto desabilitado |

Uma entrada aceita `name` (obrigatório), `summary`, `details`, `motivation`, `plot`, `image`, `eventDate`, `impact`, `characterIds`, `objectIds`, `placeIds`, `goalTarget` e `goalProgress`. Os campos de IDs relacionados são strings com UUIDs separados por vírgula.

### Privacidade 🔒

| Método | Rota | Corpo/entrada | Resultado principal |
| --- | --- | --- | --- |
| `GET` | `/api/privacy/story-metrics` | — | `{ enabled: boolean }` |
| `PUT` | `/api/privacy/story-metrics` | `{ enabled }` | `204` |

As métricas de história são opcionais e desabilitadas por padrão.

### Exportação 🔒

| Método | Rota | Entrada | Resultado principal |
| --- | --- | --- | --- |
| `GET` | `/api/export/{bookId}?format=epub` | `epub` ou `pdf` | arquivo exportado |
| `GET` | `/api/books/{bookId}/export/{format}` | `epub` ou `pdf` | arquivo exportado |

Tipos MIME: `application/epub+zip` e `application/pdf`.

### Operação

| Método | Rota | Uso |
| --- | --- | --- |
| `GET` | `/health/live` | processo da API está ativo |
| `GET` | `/health/ready` | API e PostgreSQL estão prontos |
| `GET` | `/metrics` | scrape Prometheus |
| `GET` | `/swagger` | documentação interativa, somente em Development |

## Contratos de resposta principais

- `BookDto`: `id`, `title`, `description`, `authorId`, `isbn`, `inbr`, `authorName`, `coverImageUrl`, dados de série, `createdAt`, `updatedAt`.
- `ChapterDto`: `id`, `bookId`, `title`, `order`, `currentContent`, timestamps.
- `DraftDto`: `id`, `chapterId`, `title`, `content`, timestamps, `isPublished`, `publishedAt`, `publishedSnapshotId`.
- `SnapshotDto`: `id`, `chapterId`, `content`, `snapshotMessage`, `createdAt`, `authorId`, `parentSnapshotId`.
- `StudioEntryDto`: dados da entrada, imagem, ordenação, vínculos narrativos e progresso de meta.

## gRPC opcional

Com `Grpc:Enabled=true`, a API publica `BooksService` e `ChaptersService`, ambos protegidos por autenticação. Os contratos estão em `proto/books.proto` e `proto/chapters.proto`. O serviço de capítulos também oferece timeline, criação de snapshot e restauração. O Compose mantém gRPC desabilitado por padrão.

## Persistência e observabilidade

O EF Core usa Npgsql e migrations em `src/LemonWriter.Infrastructure/Migrations`. Os repositórios persistem usuários, livros, capítulos, rascunhos e snapshots; serviços especializados cuidam de Story Studio, relações, timeline, métricas e exportação.

Cada requisição é registrada pelo Serilog. Traces e métricas ASP.NET Core/HTTP são enviados por OTLP; `/metrics` expõe métricas Prometheus. O Compose inclui coletor OpenTelemetry, Prometheus e dashboard Grafana provisionado.

## Testes

```bash
cd BackEnd
dotnet test LemonWriter.sln
```

Para validar compilação e contrato Swagger localmente:

```bash
dotnet build LemonWriter.sln
dotnet run --project src/LemonWriter.API
```

Depois abra `/swagger` no endereço informado pelo processo.
