# 🌐 STREETCATS - Integrazione API REST

Guida per l'uso dei servizi reali API REST nel progetto STREETCATS.

## 📋 Panoramica

Il progetto ora supporta due modalità di funzionamento:
- **🧪 MOCK (Sviluppo)**: Servizi simulati per sviluppo e test
- **🏭 REAL (Produzione)**: Servizi reali che comunicano con API REST

## 🔧 Configurazione

### Passaggio da Mock a Servizi Reali

Modifica il file `wwwroot/appsettings.json`:

```json
{
  "ApiSettings": {
    "UseMockServices": false,  // ← Cambia da true a false
    "BaseUrl": "https://localhost:3000/api"  // ← URL del tuo backend
  }
}
```

### Configurazione Avanzata

Per configurazioni più dettagliate, modifica tutti i parametri in `appsettings.json`:

```json
{
  "ApiSettings": {
    "UseMockServices": false,
    "BaseUrl": "https://api.streetcats.it/v1",
    "TimeoutSeconds": 30,
    "MaxRetries": 3,
    "InitialRetryDelayMs": 500,
    "BackoffMultiplier": 2.0,
    "Logging": {
      "EnableRequestLogging": false,     // Disabilita in produzione
      "EnableResponseLogging": false,    // Disabilita in produzione
      "EnableErrorLogging": true,
      "EnablePerformanceLogging": true,
      "SlowRequestThresholdMs": 3000
    }
  }
}
```

## 🔗 Endpoint API Richiesti

Il backend deve implementare questi endpoint:

### Autenticazione
```
POST /api/auth/login          # Login utente
POST /api/auth/register       # Registrazione utente  
GET  /api/auth/me            # Profilo utente corrente
```

### Gatti
```
GET    /api/cats             # Lista tutti i gatti
GET    /api/cats/{id}        # Dettagli gatto specifico
POST   /api/cats             # Crea nuovo gatto
PUT    /api/cats/{id}        # Aggiorna gatto
DELETE /api/cats/{id}        # Elimina gatto
GET    /api/cats/search?q=   # Ricerca gatti
GET    /api/cats/area?lat=&lng=&radius=  # Gatti in area
```

### Commenti
```
GET  /api/cats/{catId}/comments    # Lista commenti gatto
POST /api/cats/{catId}/comments    # Aggiungi commento
```

### Upload (opzionale)
```
POST /api/upload               # Upload file generico
POST /api/upload/images        # Upload immagini
```

## 📊 Struttura Dati JSON

### Cat Model
```json
{
  "id": "guid",
  "name": "string",
  "description": "string",
  "color": "string", 
  "status": "Avvistato|InCura|Adottato|Disperso",
  "photoUrl": "string",
  "location": {
    "type": "Point",
    "coordinates": [longitude, latitude],
    "address": "string",
    "city": "string",
    "postalCode": "string"
  },
  "comments": [],
  "createdAt": "datetime",
  "lastSeen": "datetime",
  "createdBy": "guid",
  "createdByName": "string"
}
```

### User Model
```json
{
  "id": "guid",
  "username": "string",
  "email": "string", 
  "fullName": "string",
  "createdAt": "datetime",
  "isActive": true,
  "role": "User|Moderator|Admin"
}
```

### Comment Model
```json
{
  "id": "guid",
  "catId": "guid",
  "userId": "guid", 
  "userName": "string",
  "text": "string",
  "createdAt": "datetime"
}
```

## 🔐 Autenticazione JWT

### Request Login
```json
POST /api/auth/login
{
  "username": "string",
  "password": "string"
}
```

### Response Login
```json
{
  "success": true,
  "data": {
    "token": "jwt-token-string",
    "refreshToken": "refresh-token",
    "expiresAt": "datetime",
    "user": { /* User object */ }
  }
}
```

Il client invierà automaticamente il token JWT in tutte le richieste:
```
Authorization: Bearer <jwt-token>
```

## 🔄 Gestione Errori e Retry

### Retry Automatico
Il client riprova automaticamente per:
- Errori di rete (timeout, connection refused)
- Status codes: 408, 429, 500, 502, 503, 504
- Massimo 3 tentativi con exponential backoff

### Gestione Errori
Tutti i servizi restituiscono `ApiResponse<T>`:

```json
{
  "success": false,
  "message": "Messaggio user-friendly",
  "errors": ["Errore dettagliato 1", "Errore dettagliato 2"],
  "statusCode": 400,
  "isRetryable": false
}
```

## 🏥 Monitoraggio e Debug

### Logging HTTP
Con `EnableRequestLogging: true` in development:
```
🔵 HTTP Request [ab12cd34]
   Method: POST
   URL: https://localhost:3000/api/cats
   Content-Type: application/json
   Body: {"name":"Micio",...}

🟢 HTTP Response [ab12cd34] - 245ms
   Status: 201 Created
   Content-Length: 1024 bytes
   Performance: ⚡ Very Fast
```

### Network Status
Il componente `NetworkStatusIndicator` mostra:
- 🟢 Connesso
- 🔴 Disconnesso  
- 🔄 Riconnessione automatica

## 🧪 Testing

### Test con Mock Services
```bash
# Usa servizi mock per sviluppo rapido
dotnet run
# → UseMockServices: true (default)
```

### Test con API Reali
```bash
# Prima avvia il tuo backend Node.js su porta 3000
node server.js

# Poi configura Blazor per API reali
# Modifica appsettings.json: "UseMockServices": false
dotnet run
```

## 🚀 Deploy Produzione

### Configurazione Produzione
1. Crea `wwwroot/appsettings.Production.json`
2. Imposta `UseMockServices: false`
3. Configura `BaseUrl` per il tuo server di produzione
4. Disabilita logging dettagliato

### Variabili Ambiente (Opzionale)
Per deployment sicuro, puoi sovrascrivere la configurazione:

```bash
export ApiSettings__UseMockServices=false
export ApiSettings__BaseUrl=https://api.streetcats.it/v1
```

## 📚 Servizi Disponibili

### IAuthService (Reale)
- ✅ Login con API REST
- ✅ Registrazione con validazione
- ✅ Gestione token JWT automatica
- ✅ Refresh token (quando implementato)
- ✅ Logout con cleanup

### ICatService (Reale) 
- ✅ CRUD completo via REST API
- ✅ Ricerca e filtri
- ✅ Gestione commenti
- ✅ Validazione dati client-side
- ✅ Gestione errori specifici

### IMapService (Reale)
- ✅ Geolocalizzazione browser
- ✅ Geocoding con Nominatim (gratuito)
- ✅ Calcoli distanza
- ✅ Rate limiting automatico

## ⚠️ Note Importanti

1. **Sicurezza**: Mai includere API keys nei file di configurazione committed
2. **CORS**: Il backend deve configurare CORS per il dominio Blazor
3. **HTTPS**: Usa sempre HTTPS in produzione
4. **Rate Limiting**: Rispetta i limiti di Nominatim (1 req/sec)
5. **Error Handling**: Gestisci sempre i casi di rete instabile

## 🐛 Troubleshooting

### Errore: "Could not connect to backend"
- ✅ Verifica che il backend sia in esecuzione
- ✅ Controlla l'URL in `BaseUrl`
- ✅ Verifica configurazione CORS

### Errore: "Token invalid"  
- ✅ Controlla formato JWT del backend
- ✅ Verifica scadenza token
- ✅ Implementa refresh token

### Errore: "Nominatim rate limited"
- ✅ Implementato automaticamente nel client
- ✅ Aspetta 1 secondo tra richieste
- ✅ Considera cache per geocoding

---

🎓 **Per esame universitario**: Questo sistema dimostra competenze in architettura client-server, API REST, gestione errori e user experience moderna.