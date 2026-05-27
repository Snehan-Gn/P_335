const { User } = require("../models");

async function currentUser(req, res, next) {
  try {
    const headerUserId = req.headers["x-user-id"];
    const requestedId = headerUserId ? Number(headerUserId) : undefined;
    const envDefaultId = process.env.DEFAULT_USER_ID
      ? Number(process.env.DEFAULT_USER_ID)
      : undefined;

    let user = null;

    if (requestedId && Number.isFinite(requestedId)) {
      user = await User.findByPk(requestedId);
      if (!user) {
        return res.status(400).json({
          error:
            "Invalid X-User-Id: user not found. Omit the header to use default user.",
        });
      }
    } else if (envDefaultId && Number.isFinite(envDefaultId)) {
      user = await User.findByPk(envDefaultId);
    }

    if (!user) {
      user = await User.findOne({ order: [["user_id", "ASC"]] });
    }

    if (!user) {
      return res.status(500).json({
        error:
          "No users exist in database. Create at least one user (seed/import SQL).",
      });
    }

    req.user = { user_id: user.user_id };
    next();
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
}

module.exports = currentUser;

