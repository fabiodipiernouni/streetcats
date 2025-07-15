const Joi = require('joi');

const createCatSchema = Joi.object({
  name: Joi.string()
    .min(1)
    .max(100)
    .required()
    .messages({
      'string.min': 'Nome del gatto è obbligatorio',
      'string.max': 'Nome non può superare 100 caratteri',
      'any.required': 'Nome del gatto è obbligatorio'
    }),
  
  description: Joi.string()
    .min(10)
    .max(500)
    .required()
    .messages({
      'string.min': 'Descrizione deve essere almeno 10 caratteri',
      'string.max': 'Descrizione non può superare 500 caratteri',
      'any.required': 'Descrizione è obbligatoria'
    }),
  
  color: Joi.string()
    .min(1)
    .max(50)
    .required()
    .messages({
      'string.min': 'Colore è obbligatorio',
      'string.max': 'Colore non può superare 50 caratteri',
      'any.required': 'Colore è obbligatorio'
    }),
  
  status: Joi.string()
    .valid('sano', 'ferito', 'scomparso', 'adottato', 'malato', 'sterilizzato', 'randagio', 'domestico')
    .default('sano'),
  
  photoUrl: Joi.string()
    .uri()
    .optional()
    .messages({
      'string.uri': 'URL foto non valido'
    }),
  
  location: Joi.object({
    coordinates: Joi.array()
      .items(Joi.number())
      .length(2)
      .required()
      .messages({
        'array.length': 'Coordinate devono essere [longitudine, latitudine]',
        'any.required': 'Coordinate sono obbligatorie'
      }),
    address: Joi.string()
      .min(5)
      .max(200)
      .required()
      .messages({
        'string.min': 'Indirizzo deve essere almeno 5 caratteri',
        'string.max': 'Indirizzo non può superare 200 caratteri',
        'any.required': 'Indirizzo è obbligatorio'
      }),
    city: Joi.string()
      .min(2)
      .max(100)
      .required()
      .messages({
        'string.min': 'Città deve essere almeno 2 caratteri',
        'string.max': 'Città non può superare 100 caratteri',
        'any.required': 'Città è obbligatoria'
      }),
    postalCode: Joi.string()
      .optional()
      .max(20)
      .messages({
        'string.max': 'Codice postale non può superare 20 caratteri'
      })
  }).required(),
  
  lastSeen: Joi.date()
    .optional()
    .max('now')
    .messages({
      'date.max': 'Data ultimo avvistamento non può essere nel futuro'
    })
});

const updateCatSchema = Joi.object({
  name: Joi.string().min(1).max(100).optional(),
  description: Joi.string().min(10).max(500).optional(),
  color: Joi.string().min(1).max(50).optional(),
  status: Joi.string().valid('sano', 'ferito', 'scomparso', 'adottato', 'malato', 'sterilizzato', 'randagio', 'domestico').optional(),
  photoUrl: Joi.string().uri().optional().allow(''),
  location: Joi.object({
    coordinates: Joi.array().items(Joi.number()).length(2).optional(),
    address: Joi.string().min(5).max(200).optional(),
    city: Joi.string().min(2).max(100).optional(),
    postalCode: Joi.string().max(20).optional().allow('')
  }).optional(),
  lastSeen: Joi.date().max('now').optional()
});

const searchCatsSchema = Joi.object({
  q: Joi.string().min(1).max(100).optional(),
  status: Joi.string().valid('sano', 'ferito', 'scomparso', 'adottato', 'malato', 'sterilizzato', 'randagio', 'domestico').optional(),
  page: Joi.number().integer().min(1).default(1),
  limit: Joi.number().integer().min(1).max(100).default(10)
});

const nearbySearchSchema = Joi.object({
  lat: Joi.number().min(-90).max(90).required(),
  lng: Joi.number().min(-180).max(180).required(),
  radius: Joi.number().min(100).max(50000).default(5000),
  page: Joi.number().integer().min(1).default(1),
  limit: Joi.number().integer().min(1).max(100).default(10)
});

module.exports = {
  createCatSchema,
  updateCatSchema,
  searchCatsSchema,
  nearbySearchSchema
};