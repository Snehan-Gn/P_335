var express = require("express");
var router = express.Router();
const crypto = require("crypto");
const bcrypt = require("bcrypt");
const jwt = require("jsonwebtoken");
const { User, Book } = require("../models");

router.post("/register", async function (req, res, next) {
  try {
    const { username, email, password } = req.body;
    if (!process.env.PEPPER) {
      return res.status(500).json({ error: "PEPPER is not defined" });
    }
    const pepperedPassword = crypto
      .createHmac("SHA3-512", process.env.PEPPER)
      .update(password)
      .digest("hex");

    const password_hash = await bcrypt.hash(pepperedPassword, 12);
    const user = await User.create({ username, email, password_hash });
    return res.json(user);
  } catch (error) {
    console.error("Register Error:", error);
    res.status(500).json({ error: error.message });
  }
});

router.post("/login", async function (req, res, next) {
  try {
    const { email, password } = req.body;
    const user = await User.findOne({ where: { email } });
    if (!user) {
      return res.status(401).json({ error: "Invalid email or password" });
    }

    if (!process.env.PEPPER) {
      return res.status(500).json({ error: "PEPPER is not defined" });
    }
    const pepperedPassword = crypto
      .createHmac("SHA3-512", process.env.PEPPER)
      .update(password)
      .digest("hex");

    const result = await bcrypt.compare(pepperedPassword, user.password_hash);

    if (result) {
      const token = jwt.sign(
        { user_id: user.user_id },
        process.env.JWT_SECRET,
        {
          expiresIn: "3h",
        },
      );
      return res.json({ token });
    } else {
      return res.status(401).json({ error: "Invalid email or password" });
    }
  } catch (error) {
    console.error("Login Error:", error);
    res.status(500).json({ error: error.message });
  }
});

module.exports = router;
