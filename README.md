# Spotify User History Service

This project is a containerized .NET service that fetches a user’s Spotify listening history from the Spotify API and stores it in a Supabase PostgreSQL database.


## Build & Deployment

### Build and Push to Docker Registry
```bash
# Build the container
docker build -t container-name:latest .

# Tag the container
docker tag container-name:latest username/image-name

# Push to Docker registry
docker push username/image-name
```

### Run on Server
```bash
docker run -d --name container --env-file .env username/image-name
```

### View Container Logs
```bash
docker logs -f container
```

### Stop the Container
```bash
docker stop container
```

---

## Configuration

### Environment Variables
The following environment variables must be set (typically via a `.env` file):

| Variable | Description |
|----------|-------------|
| `TELEGRAM__CHATID` | Chat ID of the recipient of messages |
| `TELEGRAM__HISTORYUPDATETOKEN` | Token for sending simple history summaries every 30 minutes |
| `TELEGRAM__ERRORLOGGERTOKEN` | Token for sending error exception notifications |
| `SPOTIFY__CLIENTID` | Client ID from your registered Spotify Developer Application |
| `SPOTIFY__CLIENTSECRET` | Client Secret from your registered Spotify Developer Application |
| `SUPABASE__URL` | Supabase project URL |
| `SUPABASE__KEY` | Elevated user key for database access |
| `CONNECTIONSTRINGS__DEFAULTCONNECTION` | Supabase database connection string |

---

## Error Handling Notes
- **Transient Failures**: Occasional transient errors (~once a week) are expected and can be ignored.  
- **HTTP Errors**: Errors such as *"Bad Gateway"* occur when the Spotify API is temporarily unresponsive and do not require intervention.  

---

## Development Notes
- Service fetches Spotify history every **30 minutes**.  
- Notifications and errors are pushed via Telegram.  
- Data is persisted in Supabase’s managed PostgreSQL database.  

---
