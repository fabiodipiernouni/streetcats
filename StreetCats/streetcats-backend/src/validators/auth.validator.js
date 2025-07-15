const Joi = require('joi');

const registerSchema = Joi.object({
  username: Joi.string()
    .alphanum()
    .min(3)
    .max(50)
    .required()
    .messages({
      'string.alphanum': 'Username deve contenere solo caratteri alfanumerici',
      'string.min': 'Username deve essere almeno 3 caratteri',
      'string.max': 'Username non può superare 50 caratteri',
      'any.required': 'Username è obbligatorio'
    }),
  
  email: Joi.string()
    .email()
    .required()
    .messages({
      'string.email': 'Email non valida',
      'any.required': 'Email è obbligatoria'
    }),
  
  fullName: Joi.string()
    .min(2)
    .max(100)
    .required()
    .messages({
      'string.min': 'Nome completo deve essere almeno 2 caratteri',
      'string.max': 'Nome completo non può superare 100 caratteri',
      'any.required': 'Nome completo è obbligatorio'
    }),
  
  password: Joi.string()
    .min(6)
    .required()
    .messages({
      'string.min': 'Password deve essere almeno 6 caratteri',
      'any.required': 'Password è obbligatoria'
    })
});

const loginSchema = Joi.object({
  email: Joi.string()
    .email()
    .required()
    .messages({
      'string.email': 'Email non valida',
      'any.required': 'Email è obbligatoria'
    }),
  
  password: Joi.string()
    .required()
    .messages({
      'any.required': 'Password è obbligatoria'
    })
});

module.exports = {
  registerSchema,
  loginSchema
};