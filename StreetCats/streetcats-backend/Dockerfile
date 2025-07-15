FROM node:20.9.0-alpine

# Installa dumb-init per gestione segnali
RUN apk add --no-cache dumb-init

# Crea directory app
WORKDIR /usr/src/app

# Copia package files
COPY package*.json ./

# Installa dipendenze
RUN npm ci --only=production && npm cache clean --force

# Copia codice app
COPY . .

# Crea utente non-root
RUN addgroup -g 1001 -S nodejs && \
    adduser -S nodejs -u 1001

# Cambia ownership
RUN chown -R nodejs:nodejs /usr/src/app
USER nodejs

# Esponi porta
EXPOSE 3000

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD node -e "require('http').get('http://localhost:3000/health', (res) => { process.exit(res.statusCode === 200 ? 0 : 1) })"

# Usa dumb-init come entrypoint
ENTRYPOINT ["dumb-init", "--"]
CMD ["node", "src/server.js"]