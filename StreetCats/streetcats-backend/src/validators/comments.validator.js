const Joi = require('joi');

const createCommentSchema = Joi.object({
  text: Joi.string()
    .min(1)
    .max(500)
    .required()
    .trim()
    .messages({
      'string.min': 'Il commento non può essere vuoto',
      'string.max': 'Il commento non può superare 500 caratteri',
      'string.empty': 'Il commento non può essere vuoto',
      'any.required': 'Il testo del commento è obbligatorio'
    })
});

const updateCommentSchema = Joi.object({
  text: Joi.string()
    .min(1)
    .max(500)
    .optional()
    .trim()
    .messages({
      'string.min': 'Il commento non può essere vuoto',
      'string.max': 'Il commento non può superare 500 caratteri',
      'string.empty': 'Il commento non può essere vuoto'
    })
});

const getCommentsSchema = Joi.object({
  page: Joi.number()
    .integer()
    .min(1)
    .default(1)
    .messages({
      'number.min': 'La pagina deve essere almeno 1',
      'number.integer': 'La pagina deve essere un numero intero'
    }),
  
  limit: Joi.number()
    .integer()
    .min(1)
    .max(100)
    .default(10)
    .messages({
      'number.min': 'Il limite deve essere almeno 1',
      'number.max': 'Il limite non può superare 100',
      'number.integer': 'Il limite deve essere un numero intero'
    }),
  
  sortBy: Joi.string()
    .valid('createdAt', 'updatedAt')
    .default('createdAt')
    .messages({
      'any.only': 'Ordinamento deve essere createdAt o updatedAt'
    }),
  
  sortOrder: Joi.string()
    .valid('asc', 'desc')
    .default('desc')
    .messages({
      'any.only': 'Ordine deve essere asc o desc'
    })
});

// Validazione parametri ID MongoDB
const mongoIdSchema = Joi.object({
  id: Joi.string()
    .pattern(/^[0-9a-fA-F]{24}$/)
    .required()
    .messages({
      'string.pattern.base': 'ID non valido',
      'any.required': 'ID è obbligatorio'
    }),
  
  catId: Joi.string()
    .pattern(/^[0-9a-fA-F]{24}$/)
    .required()
    .messages({
      'string.pattern.base': 'ID gatto non valido',
      'any.required': 'ID gatto è obbligatorio'
    })
});

module.exports = {
  createCommentSchema,
  updateCommentSchema,
  getCommentsSchema,
  mongoIdSchema
};