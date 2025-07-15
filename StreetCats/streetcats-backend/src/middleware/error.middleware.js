const ApiError = require('../utils/apiError');
const config = require('../config/environment');

const errorHandler = (err, req, res, next) => {
  let error = { ...err };
  error.message = err.message;

  // Mongoose bad ObjectId
  if (err.name === 'CastError') {
    error = new ApiError(404, 'Risorsa non trovata');
  }

  // Mongoose duplicate key
  if (err.code === 11000) {
    const message = 'Risorsa già esistente';
    error = new ApiError(400, message);
  }

  // Mongoose validation error
  if (err.name === 'ValidationError') {
    const message = Object.values(err.errors).map(val => val.message);
    error = new ApiError(400, 'Errore di validazione', message);
  }

  // JWT errors
  if (err.name === 'JsonWebTokenError') {
    error = new ApiError(401, 'Token non valido');
  }

  if (err.name === 'TokenExpiredError') {
    error = new ApiError(401, 'Token scaduto');
  }

  res.status(error.statusCode || 500).json({
    success: false,
    message: error.message || 'Errore del server',
    errors: error.errors || [],
    timestamp: new Date().toISOString(),
    ...(config.NODE_ENV === 'development' && { stack: err.stack })
  });
};

module.exports = errorHandler;