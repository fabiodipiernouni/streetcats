const mongoose = require('mongoose');

const commentSchema = new mongoose.Schema({
  catId: {
    type: mongoose.Schema.Types.ObjectId,
    ref: 'Cat',
    required: true
  },
  userId: {
    type: mongoose.Schema.Types.ObjectId,
    ref: 'User',
    required: true
  },
  userName: {
    type: String,
    required: true,
    trim: true
  },
  text: {
    type: String,
    required: [true, 'Testo commento è obbligatorio'],
    trim: true,
    maxlength: [500, 'Commento non può superare 500 caratteri']
  }
}, {
  timestamps: true,
  versionKey: false
});

// Populate automatico di user
commentSchema.pre(/^find/, function(next) {
  this.populate({
    path: 'userId',
    select: 'username fullName'
  });
  next();
});

module.exports = mongoose.model('Comment', commentSchema);