const mongoose = require('mongoose');
const config = require('./environment');

const connectDB = async () => {
  try {
    const conn = await mongoose.connect(config.MONGODB_URI, {
      useNewUrlParser: true,
      useUnifiedTopology: true,
      maxPoolSize: 10,
      serverSelectionTimeoutMS: 5000,
      socketTimeoutMS: 45000,
    });

    console.log(`MongoDB Connected: ${conn.connection.host}`);
    
    // Crea indici per performance
    await createIndexes();
    
  } catch (error) {
    console.error('MongoDB connection failed:', error);
    process.exit(1);
  }
};

const createIndexes = async () => {
  try {
    const db = mongoose.connection.db;
    
    console.log('🔍 Creazione indici database...');
    
    // Indici per User - con gestione errori per duplicati
    try {
      await db.collection('users').createIndex({ email: 1 }, { unique: true, background: true });
      console.log('✅ Indice users.email creato');
    } catch (err) {
      if (err.code !== 85) console.log('⚠️ Indice users.email già esistente');
    }
    
    try {
      await db.collection('users').createIndex({ username: 1 }, { unique: true, background: true });
      console.log('✅ Indice users.username creato');
    } catch (err) {
      if (err.code !== 85) console.log('⚠️ Indice users.username già esistente');
    }
    
    // Indici per Cat
    try {
      await db.collection('cats').createIndex({ location: '2dsphere' }, { background: true });
      console.log('✅ Indice cats.location (geospaziale) creato');
    } catch (err) {
      if (err.code !== 85) console.log('⚠️ Indice cats.location già esistente');
    }
    
    try {
      await db.collection('cats').createIndex({ name: 'text', description: 'text' }, { background: true });
      console.log('✅ Indice cats.search (text) creato');
    } catch (err) {
      if (err.code !== 85) console.log('⚠️ Indice cats.search già esistente');
    }
    
    try {
      await db.collection('cats').createIndex({ status: 1, createdAt: -1 }, { background: true });
      console.log('✅ Indice cats.status creato');
    } catch (err) {
      if (err.code !== 85) console.log('⚠️ Indice cats.status già esistente');
    }
    
    try {
      await db.collection('cats').createIndex({ createdBy: 1, createdAt: -1 }, { background: true });
      console.log('✅ Indice cats.createdBy creato');
    } catch (err) {
      if (err.code !== 85) console.log('⚠️ Indice cats.createdBy già esistente');
    }
    
    // Indici per Comment
    try {
      await db.collection('comments').createIndex({ catId: 1, createdAt: -1 }, { background: true });
      console.log('✅ Indice comments.catId creato');
    } catch (err) {
      if (err.code !== 85) console.log('⚠️ Indice comments.catId già esistente');
    }
    
    try {
      await db.collection('comments').createIndex({ userId: 1, createdAt: -1 }, { background: true });
      console.log('✅ Indice comments.userId creato');
    } catch (err) {
      if (err.code !== 85) console.log('⚠️ Indice comments.userId già esistente');
    }
    
    console.log('🎯 Indici database completati');
  } catch (error) {
    console.error('❌ Errore generale creazione indici:', error);
  }
};

module.exports = connectDB;