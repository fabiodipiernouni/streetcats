const jwt = require('jsonwebtoken');
const User = require('../models/User');
const ApiError = require('../utils/apiError');
const config = require('../config/environment');

const authenticate = async (req, res, next) => {
  try {
    const authHeader = req.headers.authorization;
    
    if (!authHeader || !authHeader.startsWith('Bearer ')) {
      return next(new ApiError(401, 'Token di accesso mancante'));
    }

    const token = authHeader.split(' ')[1];
    
    const decoded = jwt.verify(token, config.JWT_SECRET);
    
    const user = await User.findById(decoded.id);
    if (!user || !user.isActive) {
      return next(new ApiError(401, 'Utente non trovato o non attivo'));
    }

    req.user = user;
    next();
  } catch (error) {
    if (error.name === 'TokenExpiredError') {
      return next(new ApiError(401, 'Token scaduto'));
    }
    if (error.name === 'JsonWebTokenError') {
      return next(new ApiError(401, 'Token non valido'));
    }
    return next(new ApiError(401, 'Errore di autenticazione'));
  }
};

const authorize = (...roles) => {
  return (req, res, next) => {
    if (!req.user) {
      return next(new ApiError(401, 'Utente non autenticato'));
    }
    
    if (!roles.includes(req.user.role)) {
      return next(new ApiError(403, 'Accesso negato'));
    }
    
    next();
  };
};

module.exports = {
  authenticate,
  authorize
};