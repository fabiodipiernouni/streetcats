const express = require('express');
const { createComment, getComments } = require('../controllers/comments.controller');
const { authenticate } = require('../middleware/auth.middleware');
const validate = require('../middleware/validation.middleware');
const { 
  createCommentSchema, 
  getCommentsSchema,
  mongoIdSchema 
} = require('../validators/comments.validator');

const router = express.Router();

// GET /api/cats/:catId/comments - Lista commenti (pubblico)
router.get('/cats/:catId/comments', 
  validate(mongoIdSchema, 'params'),
  validate(getCommentsSchema, 'query'),
  getComments
);

// POST /api/cats/:catId/comments - Crea commento (autenticato)
router.post('/cats/:catId/comments', 
  authenticate,
  validate(mongoIdSchema, 'params'),
  validate(createCommentSchema),
  createComment
);

module.exports = router;