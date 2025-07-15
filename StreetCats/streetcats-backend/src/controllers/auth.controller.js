const jwt = require('jsonwebtoken');
const User = require('../models/User');
const ApiResponse = require('../utils/apiResponse');
const ApiError = require('../utils/apiError');
const config = require('../config/environment');

const generateToken = (id) => {
  return jwt.sign({ id }, config.JWT_SECRET, {
    expiresIn: config.JWT_EXPIRES_IN
  });
};

const register = async (req, res, next) => {
  try {
    const { username, email, fullName, password } = req.body;

    // Verifica se utente già esiste
    const existingUser = await User.findOne({
      $or: [{ email }, { username }]
    });

    if (existingUser) {
      return next(new ApiError(400, 'Utente già registrato'));
    }

    // Crea nuovo utente
    const user = await User.create({
      username,
      email,
      fullName,
      password
    });

    // Genera token
    const token = generateToken(user._id);

    res.status(201).json(new ApiResponse(201, {
      user: {
        _id: user._id,
        username: user.username,
        email: user.email,
        fullName: user.fullName,
        role: user.role,
        isActive: user.isActive,
        createdAt: user.createdAt
      },
      token
    }, 'Registrazione completata con successo'));
  } catch (error) {
    next(error);
  }
};

const login = async (req, res, next) => {
  try {
    const { email, password } = req.body;

    // Cerca utente e include password
    const user = await User.findOne({ email }).select('+password');
    
    if (!user || !user.isActive) {
      return next(new ApiError(401, 'Credenziali non valide'));
    }

    // Verifica password
    const isPasswordValid = await user.matchPassword(password);
    if (!isPasswordValid) {
      return next(new ApiError(401, 'Credenziali non valide'));
    }

    // Genera token
    const token = generateToken(user._id);

    res.status(200).json(new ApiResponse(200, {
      user: {
        _id: user._id,
        username: user.username,
        email: user.email,
        fullName: user.fullName,
        role: user.role,
        isActive: user.isActive,
        createdAt: user.createdAt
      },
      token
    }, 'Login effettuato con successo'));
  } catch (error) {
    next(error);
  }
};

const me = async (req, res, next) => {
  try {
    const user = await User.findById(req.user._id);
    
    res.status(200).json(new ApiResponse(200, {
      user: {
        _id: user._id,
        username: user.username,
        email: user.email,
        fullName: user.fullName,
        role: user.role,
        isActive: user.isActive,
        createdAt: user.createdAt
      }
    }, 'Profilo utente recuperato'));
  } catch (error) {
    next(error);
  }
};

const refresh = async (req, res, next) => {
  try {
    const { refreshToken } = req.body;
    
    if (!refreshToken) {
      return next(new ApiError(401, 'Refresh token richiesto'));
    }

    // Verifica refresh token (implementazione semplificata)
    const decoded = jwt.verify(refreshToken, config.JWT_SECRET);
    const user = await User.findById(decoded.id);
    
    if (!user || !user.isActive) {
      return next(new ApiError(401, 'Refresh token non valido'));
    }

    // Genera nuovo token
    const newToken = generateToken(user._id);
    
    res.status(200).json(new ApiResponse(200, {
      token: newToken
    }, 'Token rinnovato con successo'));
  } catch (error) {
    next(error);
  }
};

module.exports = {
  register,
  login,
  me,
  refresh
};