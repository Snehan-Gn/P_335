var express = require("express");
var router = express.Router();
var sequelize = require("../db");

/* GET home page. */
router.get("/", async function (req, res, next) {
  try {
    await sequelize.authenticate();
    res.send("Connection has been established successfully.");
  } catch (error) {
    res.send("Unable to connect to the database: " + error);
  }
  //res.render("index", { title: "Express" });
});

module.exports = router;
