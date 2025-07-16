require('dotenv').config();

/**
 * StreetCats - Environment Configuration
 */

const config = {
    // Server Configuration
    NODE_ENV: process.env.NODE_ENV || 'development',
    PORT: parseInt(process.env.PORT) || 3000,

    // Database Configuration
    MONGODB_URI: process.env.MONGODB_URI || 'mongodb://localhost:27017/streetcats',
    MONGODB_URI_ATLAS: process.env.MONGODB_URI_ATLAS || null,

    // Security Configuration  
    JWT_SECRET: process.env.JWT_SECRET || 'fallback-jwt-secret-key-for-development',
    JWT_EXPIRES_IN: process.env.JWT_EXPIRES_IN || '24h',
    REFRESH_TOKEN_SECRET: process.env.REFRESH_TOKEN_SECRET || process.env.JWT_SECRET,

    // Encryption Configuration (AES-256-CBC)
    ENCRYPTION_SECRET: process.env.ENCRYPTION_SECRET || 'default-encryption-secret-key-32chars',

    // CORS Configuration
    FRONTEND_URL: process.env.FRONTEND_URL || 'http://localhost:5045',
    ALLOWED_ORIGINS: process.env.ALLOWED_ORIGINS ?
        process.env.ALLOWED_ORIGINS.split(',') :
        ['http://localhost:5045', 'http://127.0.0.1:5045'],

    // File Upload Configuration
    UPLOAD_MAX_SIZE: parseInt(process.env.UPLOAD_MAX_SIZE) || 5242880, // 5MB
    UPLOAD_ALLOWED_TYPES: process.env.UPLOAD_ALLOWED_TYPES ?
        process.env.UPLOAD_ALLOWED_TYPES.split(',') :
        ['image/jpeg', 'image/png', 'image/webp'],
    UPLOAD_PATH: process.env.UPLOAD_PATH || './uploads',

    // Rate Limiting Configuration
    RATE_LIMIT_WINDOW_MS: parseInt(process.env.RATE_LIMIT_WINDOW_MS) || 900000, // 15 min
    RATE_LIMIT_MAX_REQUESTS: parseInt(process.env.RATE_LIMIT_MAX_REQUESTS) || 100,

    // Logging Configuration
    LOG_LEVEL: process.env.LOG_LEVEL || 'info',
    LOG_FILE: process.env.LOG_FILE || './logs/app.log',

    // Development Configuration
    ENABLE_CORS: process.env.ENABLE_CORS !== 'false',
    ENABLE_RATE_LIMITING: process.env.ENABLE_RATE_LIMITING !== 'false',
    ENABLE_REQUEST_LOGGING: process.env.NODE_ENV === 'development'
};

/**
 * Validazione configurazione con controlli aggiornati
 */
const validateConfiguration = () => {
    const errors = [];
    const warnings = [];

    // Validazione JWT Secret
    if (!config.JWT_SECRET || config.JWT_SECRET === 'fallback-jwt-secret-key-for-development') {
        if (config.NODE_ENV === 'production') {
            errors.push('JWT_SECRET deve essere configurato in produzione');
        } else {
            warnings.push('JWT_SECRET usa valore di default (ok per development)');
        }
    }

    if (config.JWT_SECRET.length < 32) {
        warnings.push('JWT_SECRET dovrebbe essere di almeno 32 caratteri per maggiore sicurezza');
    }

    // Validazione Encryption Secret (AGGIORNATA)
    if (!config.ENCRYPTION_SECRET) {
        errors.push('ENCRYPTION_SECRET è obbligatorio');
    } else if (config.ENCRYPTION_SECRET.length < 16) {
        // ✅ NUOVO: Minimo 16 caratteri (verrà hashato a 32 byte)
        errors.push('ENCRYPTION_SECRET deve essere di almeno 16 caratteri');
    }

    // ❌ RIMOSSO: Controllo ENCRYPTION_IV (non più usato)
    // L'IV viene ora generato casualmente per ogni crittografia

    // Validazione Database
    if (!config.MONGODB_URI) {
        errors.push('MONGODB_URI è obbligatorio');
    }

    // Validazione Production
    if (config.NODE_ENV === 'production') {
        if (config.MONGODB_URI.includes('localhost')) {
            warnings.push('MONGODB_URI usa localhost in produzione - considera MongoDB Atlas');
        }

        if (!config.MONGODB_URI.startsWith('mongodb+srv://')) {
            warnings.push('Considera di usare MongoDB Atlas per produzione');
        }
    }

    // Validazione Upload
    if (config.UPLOAD_MAX_SIZE > 10485760) { // 10MB
        warnings.push('UPLOAD_MAX_SIZE è molto grande (>10MB) - considera performance');
    }

    // Log risultati validazione
    if (errors.length > 0) {
        console.error('ERRORI CONFIGURAZIONE:');
        errors.forEach(error => console.error(`   • ${error}`));

        if (config.NODE_ENV === 'production') {
            console.error('🚨 PRODUZIONE BLOCCATA - Correggi errori configurazione');
            process.exit(1);
        }
    }

    if (warnings.length > 0) {
        console.warn('AVVISI CONFIGURAZIONE:');
        warnings.forEach(warning => console.warn(`   • ${warning}`));
    }

    if (errors.length === 0 && warnings.length === 0) {
        console.log('Configurazione validata con successo');
    }
};

/**
 * Mostra configurazione attiva (senza secrets)
 */
const logConfiguration = () => {
    if (config.NODE_ENV === 'development') {
        console.log('CONFIGURAZIONE ATTIVA:');
        console.log(`   Environment: ${config.NODE_ENV}`);
        console.log(`   Port: ${config.PORT}`);
        console.log(`   Database: ${config.MONGODB_URI}`);
        console.log(`   Frontend URL: ${config.FRONTEND_URL}`);
        console.log(`   Upload Max Size: ${(config.UPLOAD_MAX_SIZE / 1024 / 1024).toFixed(1)}MB`);
        console.log(`   JWT Expires: ${config.JWT_EXPIRES_IN}`);
        console.log(`   Encryption: ${config.ENCRYPTION_SECRET ? 'Configurato' : 'Non configurato'}`);
        console.log(`   Rate Limiting: ${config.ENABLE_RATE_LIMITING ? 'Abilitato' : 'Disabilitato'}`);
    }
};

/**
 * Test configurazione encryption
 */
const testEncryptionConfig = () => {
    try {
        const crypto = require('crypto');

        // Test che la secret possa essere usata per generare una chiave
        const key = crypto.createHash('sha256').update(config.ENCRYPTION_SECRET).digest();

        if (key.length !== 32) {
            throw new Error('Chiave generata non valida');
        }

        console.log('🔐 Test encryption configuration: SUCCESSO');
        return true;

    } catch (error) {
        console.error('❌ Test encryption configuration: FALLITO', error.message);
        return false;
    }
};

// Esegui validazione al caricamento
validateConfiguration();

// Log configurazione in development
if (config.NODE_ENV === 'development') {
    logConfiguration();
}

/* Test encryption in development
if (config.NODE_ENV === 'development') {
    testEncryptionConfig();
}*/

module.exports = config;