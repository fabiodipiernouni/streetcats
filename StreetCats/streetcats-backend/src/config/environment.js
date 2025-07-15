require('dotenv').config();

const config = {
  NODE_ENV: process.env.NODE_ENV || 'development',
  PORT: process.env.PORT || 3000,
  MONGODB_URI: process.env.MONGODB_URI || 'mongodb://localhost:27017/streetcats',
  JWT_SECRET: process.env.JWT_SECRET || 'fallback-secret-key',
  JWT_EXPIRES_IN: process.env.JWT_EXPIRES_IN || '7d',
  ENCRYPTION_SECRET: process.env.ENCRYPTION_SECRET || 'default-32-char-encryption-key!!',
  ENCRYPTION_IV: process.env.ENCRYPTION_IV || 'default-16-iv-key',
  FRONTEND_URL: process.env.FRONTEND_URL || 'https://localhost:7281',
  LOG_LEVEL: process.env.LOG_LEVEL || 'info'
};

// Validation
if (config.ENCRYPTION_SECRET.length !== 32) {
  console.warn('⚠️ ENCRYPTION_SECRET deve essere di 32 caratteri');
}

if (config.ENCRYPTION_IV.length !== 16) {
  console.warn('⚠️ ENCRYPTION_IV deve essere di 16 caratteri');
}

module.exports = config;