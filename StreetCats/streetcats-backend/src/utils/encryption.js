const crypto = require('crypto');

/**
 * 🔐 StreetCats - Encryption Utilities
 * Aggiornato per Node.js 18+ (sostituisce API deprecate)
 */

// Configurazione algoritmi
const ALGORITHM = 'aes-256-cbc';
const HASH_ALGORITHM = 'sha256';

/**
 * Ottiene la chiave di crittografia dall'environment
 */
const getEncryptionKey = () => {
    const secret = process.env.ENCRYPTION_SECRET;
    if (!secret) {
        throw new Error('ENCRYPTION_SECRET non configurato nel file .env');
    }

    // Crea chiave a 32 byte (256 bit) dalla secret
    return crypto.createHash(HASH_ALGORITHM).update(secret).digest();
};

/**
 * Genera un IV (Initialization Vector) casuale
 */
const generateIV = () => {
    return crypto.randomBytes(16); // 16 bytes per AES-256-CBC
};

/**
 * Cripta una password usando AES-256-CBC
 * @param {string} password - Password in chiaro
 * @returns {string} Password crittografata (formato: iv:encrypted)
 */
const encryptPassword = (password) => {
    try {
        if (!password) {
            throw new Error('Password non può essere vuota');
        }

        const key = getEncryptionKey();
        const iv = generateIV();

        // Crea cipher con chiave e IV
        const cipher = crypto.createCipheriv(ALGORITHM, key, iv);

        let encrypted = cipher.update(password, 'utf8', 'hex');
        encrypted += cipher.final('hex');

        // Combina IV e dati crittografati (separati da :)
        const result = iv.toString('hex') + ':' + encrypted;

        console.log('🔐 Password crittografata con successo');
        return result;

    } catch (error) {
        console.error('❌ Errore crittografia password:', error.message);
        throw new Error('Errore durante la crittografia');
    }
};

/**
 * Decripta una password
 * @param {string} encryptedPassword - Password crittografata (formato: iv:encrypted)
 * @returns {string} Password in chiaro
 */
const decryptPassword = (encryptedPassword) => {
    try {
        if (!encryptedPassword || !encryptedPassword.includes(':')) {
            throw new Error('Password crittografata non valida');
        }

        const key = getEncryptionKey();

        // Separa IV e dati crittografati
        const [ivHex, encrypted] = encryptedPassword.split(':');
        const iv = Buffer.from(ivHex, 'hex');

        // Crea decipher con chiave e IV
        const decipher = crypto.createDecipheriv(ALGORITHM, key, iv);

        let decrypted = decipher.update(encrypted, 'hex', 'utf8');
        decrypted += decipher.final('utf8');

        console.log('🔓 Password decrittografata con successo');
        return decrypted;

    } catch (error) {
        console.error('❌ Errore decrittografia password:', error.message);
        throw new Error('Errore durante la decrittografia');
    }
};

/**
 * Cripta dati generici (non password)
 * @param {string} text - Testo da crittografare
 * @returns {string} Testo crittografato
 */
const encryptData = (text) => {
    try {
        if (!text) return '';

        const key = getEncryptionKey();
        const iv = generateIV();

        const cipher = crypto.createCipheriv(ALGORITHM, key, iv);

        let encrypted = cipher.update(text, 'utf8', 'hex');
        encrypted += cipher.final('hex');

        return iv.toString('hex') + ':' + encrypted;

    } catch (error) {
        console.error('❌ Errore crittografia dati:', error.message);
        throw new Error('Errore durante la crittografia dei dati');
    }
};

/**
 * Decripta dati generici
 * @param {string} encryptedText - Testo crittografato
 * @returns {string} Testo in chiaro
 */
const decryptData = (encryptedText) => {
    try {
        if (!encryptedText || !encryptedText.includes(':')) {
            return encryptedText; // Ritorna il testo se non è crittografato
        }

        const key = getEncryptionKey();

        const [ivHex, encrypted] = encryptedText.split(':');
        const iv = Buffer.from(ivHex, 'hex');

        const decipher = crypto.createDecipheriv(ALGORITHM, key, iv);

        let decrypted = decipher.update(encrypted, 'hex', 'utf8');
        decrypted += decipher.final('utf8');

        return decrypted;

    } catch (error) {
        console.error('❌ Errore decrittografia dati:', error.message);
        throw new Error('Errore durante la decrittografia dei dati');
    }
};

/**
 * Genera hash sicuro per password (usando bcrypt sarebbe meglio)
 * @param {string} password - Password in chiaro
 * @param {string} salt - Salt per l'hash
 * @returns {string} Hash della password
 */
const hashPassword = (password, salt = '') => {
    try {
        const saltedPassword = password + salt + process.env.ENCRYPTION_SECRET;
        return crypto.createHash(HASH_ALGORITHM).update(saltedPassword).digest('hex');
    } catch (error) {
        console.error('❌ Errore hash password:', error.message);
        throw new Error('Errore durante l\'hash della password');
    }
};

/**
 * Genera salt casuale
 * @param {number} length - Lunghezza del salt in byte
 * @returns {string} Salt esadecimale
 */
const generateSalt = (length = 16) => {
    return crypto.randomBytes(length).toString('hex');
};

/**
 * Verifica configurazione encryption
 * @returns {boolean} True se la configurazione è valida
 */
const verifyEncryptionConfig = () => {
    try {
        const secret = process.env.ENCRYPTION_SECRET;
        if (!secret) {
            console.error('❌ ENCRYPTION_SECRET non configurato');
            return false;
        }

        if (secret.length < 8) {
            console.error('❌ ENCRYPTION_SECRET troppo corto (minimo 8 caratteri)');
            return false;
        }

        // Test di crittografia/decrittografia
        const testString = 'test_encryption_' + Date.now();
        const encrypted = encryptData(testString);
        const decrypted = decryptData(encrypted);

        if (testString !== decrypted) {
            console.error('❌ Test crittografia/decrittografia fallito');
            return false;
        }

        console.log('✅ Configurazione encryption verificata');
        return true;

    } catch (error) {
        console.error('❌ Errore verifica configurazione encryption:', error.message);
        return false;
    }
};

/**
 * Migra password da formato legacy a nuovo formato
 * @param {string} legacyPassword - Password nel vecchio formato
 * @returns {string} Password nel nuovo formato
 */
const migrateLegacyPassword = (legacyPassword) => {
    try {
        // Se la password contiene già il separatore ':', è già nel nuovo formato
        if (legacyPassword.includes(':')) {
            return legacyPassword;
        }

        // Altrimenti, ricrittografa usando il nuovo metodo
        console.log('🔄 Migrazione password da formato legacy');
        return encryptPassword(legacyPassword);

    } catch (error) {
        console.error('❌ Errore migrazione password legacy:', error.message);
        throw new Error('Errore durante la migrazione della password');
    }
};

module.exports = {
    encryptPassword,
    decryptPassword,
    encryptData,
    decryptData,
    hashPassword,
    generateSalt,
    verifyEncryptionConfig,
    migrateLegacyPassword,

    // Costanti utili
    ALGORITHM,
    HASH_ALGORITHM
};