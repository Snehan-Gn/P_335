var express = require("express");
var router = express.Router();
const { User, Book } = require("../models");

/* GET users listing. */
router.get("/", async function (req, res, next) {
  try {
    const users = await User.findAll({
      attributes: ["user_id", "username", "email"],
    });
    return res.json(users);
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
});

router.get("/:id", async function (req, res, next) {
  try {
    const user = await User.findOne({
      attributes: ["user_id", "username", "email"],
      where: { user_id: req.params.id },
    });
    if (!user) {
      return res.status(404).json({ error: "User not found" });
    }
    return res.json({ user });
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
});

module.exports = router;
