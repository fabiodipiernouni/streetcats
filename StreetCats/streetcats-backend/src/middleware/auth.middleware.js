const jwt = require('jsonwebtoken');
const User = require('../models/User');
const ApiError = require('../utils/apiError');
const config = require('../config/environment');

/**
 * Middleware di autenticazione JWT
 * Verifica il token Bearer e carica l'utente nel request
 */
const authenticate = async (req, res, next) => {
    try {
        // 1. Estrai token dall'header Authorization
        const authHeader = req.headers.authorization;

        if (!authHeader) {
            console.log('Header Authorization mancante');
            return next(new ApiError(401, 'Token di accesso richiesto'));
        }

        if (!authHeader.startsWith('Bearer ')) {
            console.log('Formato header Authorization non valido:', authHeader);
            return next(new ApiError(401, 'Formato token non valido. Usa: Bearer <token>'));
        }

        // Estrai il token (rimuovi "Bearer ")
        const token = authHeader.substring(7);

        if (!token || token === 'null' || token === 'undefined') {
            console.log('Token vuoto o non valido');
            return next(new ApiError(401, 'Token non fornito'));
        }

        console.log(`Verifica token per richiesta: ${req.method} ${req.path}`);
        console.log(`Token ricevuto: ${token.substring(0, 20)}...`);

        // 2. Verifica il token JWT
        let decoded;
        try {
            decoded = jwt.verify(token, config.JWT_SECRET);
            console.log(`Token decodificato per utente ID: ${decoded.id}`);
        } catch (jwtError) {
            console.log('Errore verifica JWT:', jwtError.message);

            if (jwtError.name === 'TokenExpiredError') {
                return next(new ApiError(401, 'Token scaduto'));
            } else if (jwtError.name === 'JsonWebTokenError') {
                return next(new ApiError(401, 'Token non valido'));
            } else {
                return next(new ApiError(401, 'Errore verifica token'));
            }
        }

        // 3. Verifica che il token contenga l'ID utente
        if (!decoded.id) {
            console.log('Token non contiene ID utente');
            return next(new ApiError(401, 'Token malformato'));
        }

        // 4. Carica l'utente dal database
        const user = await User.findById(decoded.id);

        if (!user) {
            console.log(`Utente non trovato per ID: ${decoded.id}`);
            return next(new ApiError(401, 'Utente non trovato'));
        }

        if (!user.isActive) {
            console.log(`Utente inattivo: ${user.email}`);
            return next(new ApiError(401, 'Account disattivato'));
        }

        // 5. Aggiungi utente al request per i controller successivi
        req.user = user;
        req.token = token;

        console.log(`Autenticazione riuscita per: ${user.email} (${user.role})`);

        // 6. Continua al prossimo middleware/controller
        next();

    } catch (error) {
        console.error('Errore imprevisto in auth middleware:', error.message);
        next(new ApiError(500, 'Errore interno del server'));
    }
};

/**
 * Middleware per autorizzazione ruoli
 * Verifica che l'utente abbia uno dei ruoli richiesti
 */
const authorize = (...roles) => {
    return (req, res, next) => {
        try {
            if (!req.user) {
                console.log('Authorize chiamato senza utente autenticato');
                return next(new ApiError(401, 'Autenticazione richiesta'));
            }

            if (!roles.includes(req.user.role)) {
                console.log(`Autorizzazione negata per ${req.user.email}: ruolo ${req.user.role} non in [${roles.join(', ')}]`);
                return next(new ApiError(403, 'Autorizzazione insufficiente'));
            }

            console.log(`Autorizzazione riuscita per ${req.user.email}: ruolo ${req.user.role}`);
            next();

        } catch (error) {
            console.error('Errore authorize middleware:', error.message);
            next(new ApiError(500, 'Errore autorizzazione'));
        }
    };
};

/**
 * Middleware di autenticazione opzionale
 * Come authenticate ma non blocca se il token manca/non è valido
 */
const optionalAuth = async (req, res, next) => {
    try {
        const authHeader = req.headers.authorization;

        if (!authHeader || !authHeader.startsWith('Bearer ')) {
            // Nessun token - continua senza utente
            req.user = null;
            return next();
        }

        const token = authHeader.substring(7);

        if (!token || token === 'null' || token === 'undefined') {
            req.user = null;
            return next();
        }

        try {
            const decoded = jwt.verify(token, config.JWT_SECRET);
            const user = await User.findById(decoded.id);

            if (user && user.isActive) {
                req.user = user;
                req.token = token;
                console.log(`Utente opzionale autenticato: ${user.email}`);
            } else {
                req.user = null;
            }
        } catch (jwtError) {
            // Token non valido - continua senza utente
            req.user = null;
            console.log(`Token opzionale non valido: ${jwtError.message}`);
        }

        next();

    } catch (error) {
        console.error('Errore optionalAuth middleware:', error.message);
        req.user = null;
        next();
    }
};

/**
 * Middleware per testing/debug
 * Log dettagliati di headers e token
 */
const debugAuth = (req, res, next) => {
    console.log('\n=== DEBUG AUTH ===');
    console.log(`Request: ${req.method} ${req.path}`);
    console.log(`Headers:`, req.headers);
    console.log(`Authorization:`, req.headers.authorization);
    console.log(`User Agent:`, req.headers['user-agent']);
    console.log(`Origin:`, req.headers.origin);
    console.log('==================\n');

    next();
};

/**
 * Utility per estrarre utente da token (senza middleware)
 */
const getUserFromToken = async (token) => {
    try {
        if (!token) return null;

        const decoded = jwt.verify(token, config.JWT_SECRET);
        const user = await User.findById(decoded.id);

        return user && user.isActive ? user : null;

    } catch (error) {
        console.log(`getUserFromToken fallito: ${error.message}`);
        return null;
    }
};

/**
 * Verifica configurazione JWT
 */
const verifyJWTConfig = () => {
    try {
        if (!config.JWT_SECRET) {
            console.error('JWT_SECRET non configurato');
            return false;
        }

        // Test generazione e verifica token
        const testPayload = { id: 'test123', test: true };
        const testToken = jwt.sign(testPayload, config.JWT_SECRET, { expiresIn: '1m' });
        const decoded = jwt.verify(testToken, config.JWT_SECRET);

        if (decoded.id !== testPayload.id) {
            console.error('Test JWT fallito');
            return false;
        }

        console.log('Configurazione JWT verificata');
        return true;

    } catch (error) {
        console.error('Errore verifica JWT config:', error.message);
        return false;
    }
};

// Verifica configurazione al caricamento
if (config.NODE_ENV === 'development') {
    verifyJWTConfig();
}

module.exports = {
    authenticate,
    authorize,
    optionalAuth,
    debugAuth,
    getUserFromToken,
    verifyJWTConfig
};