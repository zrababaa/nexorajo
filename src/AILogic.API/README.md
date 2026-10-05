# AILogic API

The API exposes one shared AI conversation flow to the website, Facebook Messenger,
and WhatsApp. The browser never receives the DigitalOcean agent access key.

## Run locally

From the repository root:

```powershell
dotnet run --project src/AILogic.API
```

The project loads `key.txt` by walking up from the API project directory. Environment
variables override file values and should be used in production.

Required agent settings:

```text
DIGITALOCEAN_AGENT_ENDPOINT=https://your-agent.agents.do-ai.run
DIGITALOCEAN_AGENT_KEY=your-agent-endpoint-access-key
```

The key must be an **Agent Endpoint Access Key**, not a DigitalOcean personal access
token. `key.txt` is excluded from Git.

## Meta configuration

Set these environment variables before enabling the Meta webhooks:

```text
META_WEBHOOK_VERIFY_TOKEN=choose-a-private-verification-token
META_MESSENGER_PAGE_ACCESS_TOKEN=your-page-access-token
META_WHATSAPP_ACCESS_TOKEN=your-whatsapp-system-user-token
META_WHATSAPP_PHONE_NUMBER_ID=your-phone-number-id
META_GRAPH_API_VERSION=v22.0
```

Configure Meta to use these public HTTPS callback URLs:

- Messenger: `/api/webhooks/messenger`
- WhatsApp: `/api/webhooks/whatsapp`

Both use `META_WEBHOOK_VERIFY_TOKEN` during webhook verification.

## Endpoints

- `POST /api/chat` — landing-page chatbot
- `GET /api/health` — configuration status without exposing secrets
- `GET|POST /api/webhooks/messenger` — Messenger verification and messages
- `GET|POST /api/webhooks/whatsapp` — WhatsApp verification and messages

Conversation history is currently in memory. Replace `InMemoryConversationStore` with
a database-backed implementation before running multiple API instances or when chat
history must survive restarts.
