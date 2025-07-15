class ApiError extends Error {
  constructor(statusCode, message, errors = []) {
    super(message);
    this.statusCode = statusCode;
    this.success = false;
    this.errors = errors;
    this.timestamp = new Date().toISOString();
  }
}

module.exports = ApiError;