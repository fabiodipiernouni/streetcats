const express = require('express');
const {
  createCat,
  getCats,
  getCatById,
  updateCat,
  deleteCat,
  searchCats,
  getCatsNearby
} = require('../controllers/cats.controller');
const { authenticate } = require('../middleware/auth.middleware');
const validate = require('../middleware/validation.middleware');
const {
  createCatSchema,
  updateCatSchema,
  searchCatsSchema,
  nearbySearchSchema
} = require('../validators/cats.validator');

const router = express.Router();

// Rotte pubbliche
router.get('/search', validate(searchCatsSchema, 'query'), searchCats);
router.get('/area', validate(nearbySearchSchema, 'query'), getCatsNearby);
router.get('/', getCats);
router.get('/:id', getCatById);

// Rotte protette
router.post('/', authenticate, validate(createCatSchema), createCat);
router.put('/:id', authenticate, validate(updateCatSchema), updateCat);
router.delete('/:id', authenticate, deleteCat);

module.exports = router;