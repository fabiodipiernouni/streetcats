const crypto = require('crypto');
const config = require('../config/environment');

const algorithm = 'aes-256-cbc';
const secretKey = config.ENCRYPTION_SECRET.slice(0, 32); // Assicura 32 chars
const iv = config.ENCRYPTION_IV.slice(0, 16); // Assicura 16 chars

/**
 * Cripta una password con AES-256-CBC
 * @param {string} password - Password da crittare
 * @returns {string} Password crittata in base64
 */
const encryptPassword = (password) => {
  try {
    const cipher = crypto.createCipher(algorithm, secretKey);
    let encrypted = cipher.update(password, 'utf8', 'base64');
    encrypted += cipher.final('base64');
    return encrypted;
  } catch (error) {
    console.error('Errore crittografia password:', error);
    throw new Error('Errore durante la crittografia');
  }
};

/**
 * Decripta una password crittata con AES-256-CBC
 * @param {string} encryptedPassword - Password crittata in base64
 * @returns {string} Password in chiaro
 */
const decryptPassword = (encryptedPassword) => {
  try {
    const decipher = crypto.createDecipher(algorithm, secretKey);
    let decrypted = decipher.update(encryptedPassword, 'base64', 'utf8');
    decrypted += decipher.final('utf8');
    return decrypted;
  } catch (error) {
    console.error('Errore decrittografia password:', error);
    throw new Error('Errore durante la decrittografia');
  }
};

/**
 * Genera chiavi casuali per encryption
 */
const generateKeys = () => {
  const encryptionSecret = crypto.randomBytes(16).toString('hex'); // 32 chars
  const encryptionIV = crypto.randomBytes(8).toString('hex'); // 16 chars
  
  return {
    ENCRYPTION_SECRET: encryptionSecret,
    ENCRYPTION_IV: encryptionIV
  };
};

/**
 * Test del sistema di crittografia
 */
const testEncryption = () => {
  const testPassword = 'TestPassword123!';
  console.log('🔐 Test crittografia:');
  console.log('Password originale:', testPassword);
  
  const encrypted = encryptPassword(testPassword);
  console.log('Password crittata:', encrypted);
  
  const decrypted = decryptPassword(encrypted);
  console.log('Password decrittata:', decrypted);
  console.log('Test OK:', testPassword === decrypted ? '✅' : '❌');
};

module.exports = {
  encryptPassword,
  decryptPassword,
  generateKeys,
  testEncryption
};