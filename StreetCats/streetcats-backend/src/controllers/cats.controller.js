const Cat = require('../models/Cat');
const ApiResponse = require('../utils/apiResponse');
const ApiError = require('../utils/apiError');

const createCat = async (req, res, next) => {
  try {
    const catData = {
      ...req.body,
      createdBy: req.user._id
    };

    const cat = await Cat.create(catData);
    
    res.status(201).json(new ApiResponse(201, cat, 'Gatto creato con successo'));
  } catch (error) {
    next(error);
  }
};

const getCats = async (req, res, next) => {
  try {
    const page = parseInt(req.query.page) || 1;
    const limit = parseInt(req.query.limit) || 10;
    const skip = (page - 1) * limit;

    const query = {};
    
    if (req.query.status) {
      query.status = req.query.status;
    }

    if (req.query.q) {
      query.$text = { $search: req.query.q };
    }

    const cats = await Cat.find(query)
      .skip(skip)
      .limit(limit)
      .sort({ createdAt: -1 });

    const total = await Cat.countDocuments(query);

    res.status(200).json(new ApiResponse(200, {
      cats,
      pagination: {
        page,
        limit,
        total,
        totalPages: Math.ceil(total / limit)
      }
    }, 'Gatti recuperati con successo'));
  } catch (error) {
    next(error);
  }
};

const getCatById = async (req, res, next) => {
  try {
    const cat = await Cat.findById(req.params.id);
    
    if (!cat) {
      return next(new ApiError(404, 'Gatto non trovato'));
    }

    res.status(200).json(new ApiResponse(200, cat, 'Gatto recuperato con successo'));
  } catch (error) {
    next(error);
  }
};

const updateCat = async (req, res, next) => {
  try {
    const cat = await Cat.findById(req.params.id);
    
    if (!cat) {
      return next(new ApiError(404, 'Gatto non trovato'));
    }

    // Verifica proprietà o admin
    if (cat.createdBy.toString() !== req.user._id.toString() && req.user.role !== 'admin') {
      return next(new ApiError(403, 'Non autorizzato a modificare questo gatto'));
    }

    const updatedCat = await Cat.findByIdAndUpdate(
      req.params.id,
      req.body,
      { new: true, runValidators: true }
    );

    res.status(200).json(new ApiResponse(200, updatedCat, 'Gatto aggiornato con successo'));
  } catch (error) {
    next(error);
  }
};

const deleteCat = async (req, res, next) => {
  try {
    const cat = await Cat.findById(req.params.id);
    
    if (!cat) {
      return next(new ApiError(404, 'Gatto non trovato'));
    }

    // Verifica proprietà o admin
    if (cat.createdBy.toString() !== req.user._id.toString() && req.user.role !== 'admin') {
      return next(new ApiError(403, 'Non autorizzato a eliminare questo gatto'));
    }

    await Cat.findByIdAndDelete(req.params.id);

    res.status(200).json(new ApiResponse(200, null, 'Gatto eliminato con successo'));
  } catch (error) {
    next(error);
  }
};

const searchCats = async (req, res, next) => {
  try {
    const { q, status, page = 1, limit = 10 } = req.query;
    const skip = (page - 1) * limit;

    const query = {};
    
    if (status) {
      query.status = status;
    }

    if (q) {
      query.$text = { $search: q };
    }

    const cats = await Cat.find(query)
      .skip(skip)
      .limit(parseInt(limit))
      .sort({ createdAt: -1 });

    const total = await Cat.countDocuments(query);

    res.status(200).json(new ApiResponse(200, {
      cats,
      pagination: {
        page: parseInt(page),
        limit: parseInt(limit),
        total,
        totalPages: Math.ceil(total / limit)
      },
      searchQuery: q || null
    }, 'Ricerca completata con successo'));
  } catch (error) {
    next(error);
  }
};

const getCatsNearby = async (req, res, next) => {
  try {
    const { lat, lng, radius = 5000, page = 1, limit = 10 } = req.query;
    const skip = (page - 1) * limit;

    if (!lat || !lng) {
      return next(new ApiError(400, 'Latitudine e longitudine sono obbligatorie'));
    }

    const cats = await Cat.aggregate([
      {
        $geoNear: {
          near: {
            type: 'Point',
            coordinates: [parseFloat(lng), parseFloat(lat)]
          },
          distanceField: 'distance',
          maxDistance: parseInt(radius),
          spherical: true
        }
      },
      {
        $lookup: {
          from: 'users',
          localField: 'createdBy',
          foreignField: '_id',
          as: 'createdBy'
        }
      },
      {
        $unwind: '$createdBy'
      },
      {
        $project: {
          'createdBy.password': 0,
          'createdBy.email': 0
        }
      },
      { $skip: skip },
      { $limit: parseInt(limit) }
    ]);

    const total = await Cat.countDocuments({
      location: {
        $geoWithin: {
          $centerSphere: [[parseFloat(lng), parseFloat(lat)], parseInt(radius) / 6378100]
        }
      }
    });

    res.status(200).json(new ApiResponse(200, {
      cats,
      pagination: {
        page: parseInt(page),
        limit: parseInt(limit),
        total,
        totalPages: Math.ceil(total / limit)
      },
      searchCenter: {
        latitude: parseFloat(lat),
        longitude: parseFloat(lng)
      },
      radius: parseInt(radius)
    }, 'Gatti nelle vicinanze trovati'));
  } catch (error) {
    next(error);
  }
};

module.exports = {
  createCat,
  getCats,
  getCatById,
  updateCat,
  deleteCat,
  searchCats,
  getCatsNearby
};