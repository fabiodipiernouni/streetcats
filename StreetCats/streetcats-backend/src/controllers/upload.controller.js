const multer = require('multer');
const path = require('path');
const fs = require('fs').promises;
const crypto = require('crypto');
const sharp = require('sharp');
const ApiResponse = require('../utils/apiResponse');
const ApiError = require('../utils/apiError');

// Configurazione Multer per upload
const storage = multer.memoryStorage(); // Usa memoria per elaborazione con Sharp

const fileFilter = (req, file, cb) => {
  // Solo immagini
  if (file.mimetype.startsWith('image/')) {
    cb(null, true);
  } else {
    cb(new ApiError(400, 'Solo file immagine sono consentiti'), false);
  }
};

const upload = multer({
  storage: storage,
  fileFilter: fileFilter,
  limits: {
    fileSize: 5 * 1024 * 1024, // 5MB max
    files: 1 // Solo 1 file alla volta
  }
});

// Middleware per upload singolo
const uploadSingle = upload.single('photo');

/**
 * Upload foto gatto con resize automatico
 */
const uploadCatPhoto = async (req, res, next) => {
  try {
    // Controlla se file è presente
    if (!req.file) {
      return next(new ApiError(400, 'Nessun file caricato'));
    }

    // Genera nome file unico
    const fileExtension = '.webp'; // Converti sempre in WebP per efficienza
    const fileName = `cat-${crypto.randomUUID()}${fileExtension}`;
    
    // Path assoluto per salvare
    const uploadDir = path.join(__dirname, '../../uploads/cats');
    const filePath = path.join(uploadDir, fileName);
    
    // Crea directory se non esiste
    await ensureDirectoryExists(uploadDir);
    
    // Elabora immagine con Sharp (resize + ottimizzazione)
    await sharp(req.file.buffer)
      .resize(800, 600, { // Max 800x600
        fit: 'inside',
        withoutEnlargement: true
      })
      .webp({ quality: 85 }) // Converti in WebP con qualità 85%
      .toFile(filePath);
    
    // URL pubblico dell'immagine
    const photoUrl = `/uploads/cats/${fileName}`;
    
    // Log dell'upload
    console.log(`📸 Foto caricata: ${fileName} (${req.file.size} bytes → WebP)`);
    
    res.status(200).json(new ApiResponse(200, {
      photoUrl: photoUrl,
      fileName: fileName,
      originalName: req.file.originalname,
      size: req.file.size,
      mimeType: 'image/webp'
    }, 'Foto caricata con successo'));
    
  } catch (error) {
    next(error);
  }
};

/**
 * Upload multiplo (per future funzionalità)
 */
const uploadMultiplePhotos = async (req, res, next) => {
  try {
    const uploadMultiple = upload.array('photos', 5); // Max 5 foto
    
    uploadMultiple(req, res, async (err) => {
      if (err) {
        return next(new ApiError(400, err.message));
      }
      
      if (!req.files || req.files.length === 0) {
        return next(new ApiError(400, 'Nessun file caricato'));
      }
      
      const uploadedFiles = [];
      const uploadDir = path.join(__dirname, '../../uploads/cats');
      await ensureDirectoryExists(uploadDir);
      
      // Elabora ogni file
      for (const file of req.files) {
        const fileName = `cat-${crypto.randomUUID()}.webp`;
        const filePath = path.join(uploadDir, fileName);
        
        await sharp(file.buffer)
          .resize(800, 600, { fit: 'inside', withoutEnlargement: true })
          .webp({ quality: 85 })
          .toFile(filePath);
        
        uploadedFiles.push({
          photoUrl: `/uploads/cats/${fileName}`,
          fileName: fileName,
          originalName: file.originalname
        });
      }
      
      res.status(200).json(new ApiResponse(200, {
        photos: uploadedFiles,
        count: uploadedFiles.length
      }, `${uploadedFiles.length} foto caricate con successo`));
    });
    
  } catch (error) {
    next(error);
  }
};

/**
 * Elimina foto dal filesystem
 */
const deleteCatPhoto = async (req, res, next) => {
  try {
    const { fileName } = req.params;
    
    // Validazione nome file (sicurezza)
    if (!fileName || !fileName.match(/^cat-[a-f0-9-]+\.(webp|jpg|jpeg|png)$/i)) {
      return next(new ApiError(400, 'Nome file non valido'));
    }
    
    const filePath = path.join(__dirname, '../../uploads/cats', fileName);
    
    // Verifica che il file esista
    try {
      await fs.access(filePath);
    } catch {
      return next(new ApiError(404, 'File non trovato'));
    }
    
    // Elimina il file
    await fs.unlink(filePath);
    
    console.log(`🗑️ Foto eliminata: ${fileName}`);
    
    res.status(200).json(new ApiResponse(200, null, 'Foto eliminata con successo'));
    
  } catch (error) {
    next(error);
  }
};

/**
 * Ottieni info foto
 */
const getPhotoInfo = async (req, res, next) => {
  try {
    const { fileName } = req.params;
    const filePath = path.join(__dirname, '../../uploads/cats', fileName);
    
    // Verifica esistenza
    try {
      const stats = await fs.stat(filePath);
      
      res.status(200).json(new ApiResponse(200, {
        fileName: fileName,
        size: stats.size,
        created: stats.birthtime,
        modified: stats.mtime,
        url: `/uploads/cats/${fileName}`
      }, 'Informazioni foto recuperate'));
      
    } catch {
      return next(new ApiError(404, 'File non trovato'));
    }
    
  } catch (error) {
    next(error);
  }
};

/**
 * Cleanup foto orfane (non collegate a gatti)
 */
const cleanupOrphanedPhotos = async (req, res, next) => {
  try {
    const Cat = require('../models/Cat');
    
    // Ottieni tutti i file nella directory uploads
    const uploadDir = path.join(__dirname, '../../uploads/cats');
    const files = await fs.readdir(uploadDir);
    
    // Ottieni tutti gli URL delle foto utilizzate
    const usedPhotos = await Cat.find({ photoUrl: { $exists: true, $ne: '' } })
      .select('photoUrl')
      .lean();
    
    const usedFileNames = usedPhotos
      .map(cat => cat.photoUrl.split('/').pop())
      .filter(Boolean);
    
    // Trova file orfani
    const orphanedFiles = files.filter(file => 
      file.match(/^cat-[a-f0-9-]+\.(webp|jpg|jpeg|png)$/i) &&
      !usedFileNames.includes(file)
    );
    
    // Elimina file orfani
    let deletedCount = 0;
    for (const file of orphanedFiles) {
      try {
        await fs.unlink(path.join(uploadDir, file));
        deletedCount++;
        console.log(`🧹 File orfano eliminato: ${file}`);
      } catch (error) {
        console.error(`❌ Errore eliminazione ${file}:`, error.message);
      }
    }
    
    res.status(200).json(new ApiResponse(200, {
      deletedCount,
      orphanedFiles: orphanedFiles.length,
      totalFiles: files.length
    }, `Cleanup completato: ${deletedCount} file eliminati`));
    
  } catch (error) {
    next(error);
  }
};

/**
 * Assicura che la directory esista
 */
const ensureDirectoryExists = async (dirPath) => {
  try {
    await fs.access(dirPath);
  } catch {
    await fs.mkdir(dirPath, { recursive: true });
    console.log(`📁 Directory creata: ${dirPath}`);
  }
};

// Middleware per gestire errori Multer
const handleMulterError = (err, req, res, next) => {
  if (err instanceof multer.MulterError) {
    if (err.code === 'LIMIT_FILE_SIZE') {
      return next(new ApiError(400, 'File troppo grande (max 5MB)'));
    }
    if (err.code === 'LIMIT_FILE_COUNT') {
      return next(new ApiError(400, 'Troppi file (max 5)'));
    }
    if (err.code === 'LIMIT_UNEXPECTED_FILE') {
      return next(new ApiError(400, 'Campo file non valido'));
    }
  }
  next(err);
};

module.exports = {
  uploadSingle,
  uploadCatPhoto,
  uploadMultiplePhotos,
  deleteCatPhoto,
  getPhotoInfo,
  cleanupOrphanedPhotos,
  handleMulterError
};