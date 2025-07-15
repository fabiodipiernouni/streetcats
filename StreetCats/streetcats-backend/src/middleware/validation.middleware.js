const ApiError = require('../utils/apiError');

const validate = (schema, property = 'body') => {
  return (req, res, next) => {
    const { error } = schema.validate(req[property], { abortEarly: false });
    
    if (error) {
      const errorMessages = error.details.map(detail => ({
        field: detail.path.join('.'),
        message: detail.message
      }));
      
      return next(new ApiError(400, 'Errore di validazione', errorMessages));
    }
    
    next();
  };
};

module.exports = validate;