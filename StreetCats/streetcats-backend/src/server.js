const app = require('./app');
const connectDB = require('./config/database');
const config = require('./config/environment');

// Handle uncaught exceptions
process.on('uncaughtException', (err) => {
  console.error('Uncaught Exception:', err);
  process.exit(1);
});

// Connect to database
connectDB();

const server = app.listen(config.PORT, () => {
  console.log(`
🚀 Server STREETCATS avviato!
📍 Porta: ${config.PORT}
🌍 Ambiente: ${config.NODE_ENV}
📊 Database: ${config.MONGODB_URI}
🔗 Frontend: ${config.FRONTEND_URL}
  `);
});

// Handle unhandled promise rejections
process.on('unhandledRejection', (err) => {
  console.error('Unhandled Rejection:', err);
  server.close(() => {
    process.exit(1);
  });
});

// Graceful shutdown
process.on('SIGTERM', () => {
  console.log('SIGTERM received, shutting down gracefully');
  server.close(() => {
    console.log('Process terminated');
  });
});