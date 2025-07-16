# 🐱 StreetCats - Script di Avvio

Questo repository include diversi script per avviare l'applicazione StreetCats in diverse modalità.

## 📁 Script Disponibili

### 🎯 Script Principale
- **`run.bat`** - Script principale che accetta parametri per l'ambiente

### 🚀 Script Rapidi  
- **`run-dev.bat`** - Avvio rapido modalità Development (Mock)
- **`run-int.bat`** - Avvio rapido modalità Integration (Backend locale)
- **`run-prod.bat`** - Avvio rapido modalità Production (Backend reale)

### 🎪 Script Interattivo
- **`start.bat`** - Menu interattivo per scegliere l'ambiente

## 🔧 Modalità di Uso

### Metodo 1: Script Principale con Parametri
```cmd
# Sviluppo con servizi mock
run.bat Development

# Test con backend locale  
run.bat Integration

# Produzione con backend reale
run.bat Production
```

### Metodo 2: Script Rapidi
```cmd
# Doppio-click sui file o da terminale:
run-dev.bat     # Development
run-int.bat     # Integration  
run-prod.bat    # Production
```

### Metodo 3: Menu Interattivo
```cmd
start.bat
# Segui il menu per scegliere l'ambiente
```

## 🌐 Configurazioni Ambiente

| Ambiente | Mock Services | Backend URL | Descrizione |
|----------|---------------|-------------|-------------|
| **Development** | ✅ Sì | Non richiesto | Demo con dati fittizi |
| **Integration** | ❌ No | `localhost:3000` | Test con backend locale |
| **Production** | ❌ No | `api.streetcats.it` | Backend di produzione |

## 📋 Prerequisiti

- ✅ .NET 8 SDK installato
- ✅ File progetto `StreetCats.Client.csproj` presente
- ✅ Per Integration: backend in esecuzione su porta 3000
- ✅ Per Production: backend reale disponibile

## 🛠️ Funzionalità Script

### ✨ Caratteristiche
- 🎨 Output colorato per Windows 11
- 🔍 Verifica automatica prerequisiti
- 🌐 Test connettività backend (per Integration)
- ❌ Gestione errori completa
- 📊 Log dettagliati di avvio
- ⚡ Timeout e feedback utente

### 🔧 Validazioni Automatiche
- Verifica presenza .NET SDK
- Controllo esistenza file progetto
- Test connessione backend (modalità Integration)
- Validazione parametri ambiente

## 🐛 Risoluzione Problemi

### Errore: ".NET SDK non trovato"
```cmd
# Installa .NET 8 da:
# https://dotnet.microsoft.com/download
```

### Errore: "Backend non raggiungibile" (Integration)
```cmd
# Avvia il backend Node.js su porta 3000
cd ../backend
npm start

# Poi avvia il client
run-int.bat
```

### Errore: "File progetto non trovato"
```cmd
# Assicurati di essere nella directory corretta
cd E:\repos\Unina\techweb\StreetCats\StreetCats.Client\
```

## 💡 Suggerimenti

1. **Per sviluppo rapido UI**: usa `run-dev.bat`
2. **Per test integrazione**: usa `run-int.bat` dopo aver avviato il backend
3. **Per demo**: usa `start.bat` per il menu interattivo
4. **Personalizzazione**: modifica `run.bat` per esigenze specifiche

## 📖 Esempi d'Uso

```cmd
# Sviluppo veloce senza backend
run-dev.bat

# Test completo con backend locale
# Terminale 1: 
cd ../backend && npm start
# Terminale 2:
run-int.bat

# Menu interattivo
start.bat
```

---
🎓 **Per esame universitario**: Questi script dimostrano competenze in automazione, DevOps e gestione ambienti di sviluppo.