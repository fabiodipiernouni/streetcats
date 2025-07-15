const express = require('express');
const cors = require('cors');
const helmet = require('helmet');
const compression = require('compression');
const mongoSanitize = require('express-mongo-sanitize');
const rateLimit = require('express-rate-limit');

const config = require('./config/environment');
const authRoutes = require('./routes/auth.routes');
const catRoutes = require('./routes/cats.routes');
const commentRoutes = require('./routes/comments.routes');
const errorHandler = require('./middleware/error.middleware');

const app = express();

// Trust proxy
app.set('trust proxy', 1);

// Security middleware
app.use(helmet());
app.use(compression());
app.use(mongoSanitize());

// CORS configuration
app.use(cors({
  origin: config.FRONTEND_URL,
  credentials: true,
  methods: ['GET', 'POST', 'PUT', 'DELETE', 'OPTIONS'],
  allowedHeaders: ['Content-Type', 'Authorization']
}));

// Rate limiting
const limiter = rateLimit({
  windowMs: 15 * 60 * 1000, // 15 minuti
  max: 100, // limite di 100 richieste per IP
  message: {
    success: false,
    message: 'Troppe richieste da questo IP, riprova più tardi',
    statusCode: 429
  }
});
app.use('/api/', limiter);

// Body parser
app.use(express.json({ limit: '10mb' }));
app.use(express.urlencoded({ extended: true, limit: '10mb' }));

// Routes
app.use('/api/auth', authRoutes);
app.use('/api/cats', catRoutes);
app.use('/api', commentRoutes);

// Health check
app.get('/health', (req, res) => {
  res.json({
    success: true,
    message: 'Server è operativo',
    timestamp: new Date().toISOString(),
    environment: config.NODE_ENV
  });
});

// 404 handler
app.use('*', (req, res) => {
  res.status(404).json({
    success: false,
    message: 'Endpoint non trovato',
    statusCode: 404,
    timestamp: new Date().toISOString()
  });
});

// Error handling middleware
app.use(errorHandler);

module.exports = app;