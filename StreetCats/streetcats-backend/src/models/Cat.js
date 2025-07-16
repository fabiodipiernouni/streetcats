const mongoose = require('mongoose');
const bcrypt = require('bcryptjs');

const catSchema = new mongoose.Schema({
    name: {
        type: String,
        required: [true, 'Il nome del gatto è obbligatorio'],
        trim: true,
        maxLength: [100, 'Il nome non può superare 100 caratteri']
    },

    description: {
        type: String,
        maxLength: [500, 'La descrizione non può superare 500 caratteri'],
        trim: true
    },

    color: {
        type: String,
        required: [true, 'Il colore del gatto è obbligatorio'],
        trim: true,
        maxLength: [50, 'Il colore non può superare 50 caratteri']
    },

    status: {
        type: String,
        enum: {
            values: ['Avvistato', 'Adottato', 'Disperso', 'InCura', 'Deceduto'],
            message: 'Status deve essere uno tra: Avvistato, Adottato, Disperso, InCura, Deceduto'
        },
        default: 'Avvistato'
    },

    photoUrl: {
        type: String,
        validate: {
            validator: function (v) {
                if (!v || v === '') return true;
                return /^https?:\/\//.test(v) || /^\/uploads\//.test(v);
            },
            message: 'URL foto non valido'
        }
    },

    location: {
        type: {
            type: String,
            enum: ['Point'],
            default: 'Point'
        },
        coordinates: {
            type: [Number],
            required: true,
            validate: {
                validator: function (coords) {
                    return coords.length === 2 &&
                        coords[1] >= -90 && coords[1] <= 90 &&
                        coords[0] >= -180 && coords[0] <= 180;
                },
                message: 'Coordinate non valide [longitudine, latitudine]'
            }
        },
        address: {
            type: String,
            maxLength: [200, 'Indirizzo troppo lungo']
        },
        city: {
            type: String,
            maxLength: [100, 'Nome città troppo lungo']
        },
        postalCode: {
            type: String,
            maxLength: [20, 'CAP troppo lungo']
        }
    },

    createdBy: {
        type: mongoose.Schema.Types.ObjectId,
        ref: 'User',
        required: true
    },

    tags: [{
        type: String,
        maxLength: [30, 'Tag troppo lungo']
    }],

    isPublic: {
        type: Boolean,
        default: true
    },

    priority: {
        type: String,
        enum: ['low', 'medium', 'high', 'urgent'],
        default: 'medium'
    },

    lastSeenAt: {
        type: Date,
        default: Date.now
    },

    reportCount: {
        type: Number,
        default: 1,
        min: 0
    }
}, {
    timestamps: true,
    toJSON: { virtuals: true },
    toObject: { virtuals: true }
});

// Indici per performance
catSchema.index({ location: '2dsphere' });
catSchema.index({ status: 1 });
catSchema.index({ createdBy: 1 });
catSchema.index({ createdAt: -1 });
catSchema.index({ name: 'text', description: 'text', color: 'text' });

// Virtual per calcolare distanza (se necessario)
catSchema.virtual('comments', {
    ref: 'Comment',
    localField: '_id',
    foreignField: 'catId'
});

// Middleware pre-save
catSchema.pre('save', function (next) {
    this.lastSeenAt = Date.now();
    next();
});

module.exports = mongoose.model('Cat', catSchema);