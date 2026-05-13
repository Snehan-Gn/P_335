const { Sequelize, DataTypes } = require("sequelize");
var sequelize = require("../db");

const Category = sequelize.define(
  "Category",
  {
    category_id: {
      type: DataTypes.INTEGER,
      autoIncrement: true,
      primaryKey: true,
    },
    name: {
      type: DataTypes.STRING,
      allowNull: false,
    },
  },
  {
    tableName: "t_category",
    timestamps: false,
  },
);

module.exports = Category;
