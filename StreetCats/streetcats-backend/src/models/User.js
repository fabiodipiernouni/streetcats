const mongoose = require('mongoose');
const bcrypt = require('bcryptjs');
const { encryptPassword, decryptPassword } = require('../utils/encryption');

const userSchema = new mongoose.Schema({
  username: {
    type: String,
    required: [true, 'Username è obbligatorio'],
    unique: true,
    trim: true,
    minlength: [3, 'Username deve essere almeno 3 caratteri'],
    maxlength: [50, 'Username non può superare 50 caratteri']
  },
  email: {
    type: String,
    required: [true, 'Email è obbligatoria'],
    unique: true,
    lowercase: true,
    match: [/^\w+([.-]?\w+)*@\w+([.-]?\w+)*(\.\w{2,3})+$/, 'Email non valida']
  },
  fullName: {
    type: String,
    required: [true, 'Nome completo è obbligatorio'],
    trim: true,
    maxlength: [100, 'Nome completo non può superare 100 caratteri']
  },
  password: {
    type: String,
    required: [true, 'Password è obbligatoria'],
    minlength: [6, 'Password deve essere almeno 6 caratteri'],
    select: false
  },
  // Password crittata AES (reversibile) per recovery
  encryptedPassword: {
    type: String,
    select: false
  },
  role: {
    type: String,
    enum: ['user', 'admin'],
    default: 'user'
  },
  isActive: {
    type: Boolean,
    default: true
  }
}, {
  timestamps: true,
  versionKey: false
});

// Hash password con bcrypt E cripta con AES prima del save
userSchema.pre('save', async function(next) {
  if (!this.isModified('password')) return next();
  
  try {
    // Salva password crittata AES (reversibile)
    this.encryptedPassword = encryptPassword(this.password);
    
    // Hash password con bcrypt (per login)
    const salt = await bcrypt.genSalt(12);
    this.password = await bcrypt.hash(this.password, salt);
    
    next();
  } catch (error) {
    next(error);
  }
});

// Metodo per verificare password con bcrypt
userSchema.methods.matchPassword = async function(enteredPassword) {
  return await bcrypt.compare(enteredPassword, this.password);
};

// Metodo per recuperare password in chiaro (solo per admin o recovery)
userSchema.methods.getDecryptedPassword = function() {
  if (!this.encryptedPassword) {
    throw new Error('Password crittata non disponibile');
  }
  return decryptPassword(this.encryptedPassword);
};

// Metodo per cambiare password
userSchema.methods.changePassword = async function(newPassword) {
  this.password = newPassword; // Trigger pre-save hook
  await this.save();
};

// Trasforma output JSON (rimuovi dati sensibili)
userSchema.methods.toJSON = function() {
  const userObject = this.toObject();
  delete userObject.password;
  delete userObject.encryptedPassword;
  return userObject;
};

module.exports = mongoose.model('User', userSchema);