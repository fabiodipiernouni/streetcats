const express = require('express');
const { 
  uploadSingle,
  uploadCatPhoto, 
  uploadMultiplePhotos,
  deleteCatPhoto,
  getPhotoInfo,
  cleanupOrphanedPhotos,
  handleMulterError
} = require('../controllers/upload.controller');
const { authenticate, authorize } = require('../middleware/auth.middleware');

const router = express.Router();

// Upload singola foto gatto (autenticato)
router.post('/images', 
  authenticate,
  uploadSingle,
  handleMulterError,
  uploadCatPhoto
);

// Upload multiple foto (futuro)
router.post('/images/multiple',
  authenticate,
  handleMulterError,
  uploadMultiplePhotos
);

// Elimina foto (solo proprietario o admin)
router.delete('/images/:fileName',
  authenticate,
  deleteCatPhoto
);

// Info foto
router.get('/images/:fileName/info',
  getPhotoInfo
);

// Cleanup foto orfane (solo admin)
router.post('/cleanup',
  authenticate,
  authorize('admin'),
  cleanupOrphanedPhotos
);

module.exports = router;