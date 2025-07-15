const Comment = require('../models/Comment');
const Cat = require('../models/Cat');
const ApiResponse = require('../utils/apiResponse');
const ApiError = require('../utils/apiError');

const createComment = async (req, res, next) => {
  try {
    const { catId } = req.params;
    const { text } = req.body;

    // Verifica che il gatto esista
    const cat = await Cat.findById(catId);
    if (!cat) {
      return next(new ApiError(404, 'Gatto non trovato'));
    }

    const comment = await Comment.create({
      catId,
      userId: req.user._id,
      userName: req.user.username,
      text
    });

    res.status(201).json(new ApiResponse(201, comment, 'Commento creato con successo'));
  } catch (error) {
    next(error);
  }
};

const getComments = async (req, res, next) => {
  try {
    const { catId } = req.params;
    const page = parseInt(req.query.page) || 1;
    const limit = parseInt(req.query.limit) || 10;
    const skip = (page - 1) * limit;

    // Verifica che il gatto esista
    const cat = await Cat.findById(catId);
    if (!cat) {
      return next(new ApiError(404, 'Gatto non trovato'));
    }

    const comments = await Comment.find({ catId })
      .skip(skip)
      .limit(limit)
      .sort({ createdAt: -1 });

    const total = await Comment.countDocuments({ catId });

    res.status(200).json(new ApiResponse(200, {
      comments,
      pagination: {
        page,
        limit,
        total,
        totalPages: Math.ceil(total / limit)
      }
    }, 'Commenti recuperati con successo'));
  } catch (error) {
    next(error);
  }
};

module.exports = {
  createComment,
  getComments
};