const mongoose = require('mongoose');

const locationSchema = new mongoose.Schema({
  type: {
    type: String,
    enum: ['Point'],
    default: 'Point'
  },
  coordinates: {
    type: [Number],
    required: true,
    validate: {
      validator: function(coords) {
        return coords.length === 2;
      },
      message: 'Coordinates devono essere [longitudine, latitudine]'
    }
  },
  address: {
    type: String,
    required: true,
    trim: true
  },
  city: {
    type: String,
    required: true,
    trim: true
  },
  postalCode: {
    type: String,
    trim: true
  }
}, { _id: false });

const catSchema = new mongoose.Schema({
  name: {
    type: String,
    required: [true, 'Nome del gatto è obbligatorio'],
    trim: true,
    maxlength: [100, 'Nome non può superare 100 caratteri']
  },
  description: {
    type: String,
    required: [true, 'Descrizione è obbligatoria'],
    trim: true,
    maxlength: [500, 'Descrizione non può superare 500 caratteri']
  },
  color: {
    type: String,
    required: [true, 'Colore è obbligatorio'],
    trim: true,
    maxlength: [50, 'Colore non può superare 50 caratteri']
  },
  status: {
    type: String,
    enum: ['sano', 'ferito', 'scomparso', 'adottato', 'malato', 'sterilizzato', 'randagio', 'domestico'],
    default: 'sano'
  },
  photoUrl: {
    type: String,
    validate: {
      validator: function(url) {
        return !url || /^https?:\/\/.+/.test(url);
      },
      message: 'URL foto non valido'
    }
  },
  location: {
    type: locationSchema,
    required: true
  },
  lastSeen: {
    type: Date,
    default: Date.now
  },
  createdBy: {
    type: mongoose.Schema.Types.ObjectId,
    ref: 'User',
    required: true
  }
}, {
  timestamps: true,
  versionKey: false
});

// Indice geospaziale per ricerche per area
catSchema.index({ location: '2dsphere' });

// Populate automatico di createdBy
catSchema.pre(/^find/, function(next) {
  this.populate({
    path: 'createdBy',
    select: 'username fullName'
  });
  next();
});

module.exports = mongoose.model('Cat', catSchema);