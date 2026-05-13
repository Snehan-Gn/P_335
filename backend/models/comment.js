const { Sequelize, DataTypes } = require("sequelize");
var sequelize = require("../db");

const Comment = sequelize.define(
  "Comment",
  {
    // Model attributes are defined here
    comment_id: {
      type: DataTypes.INTEGER,
      autoIncrement: true,
      primaryKey: true,
    },
    title: {
      type: DataTypes.STRING,
      allowNull: false,
    },
    message: {
      type: DataTypes.STRING,
      allowNull: false,
    },
    rating: {
      type: DataTypes.INTEGER,
      allowNull: false,
      validate: {
        min: 0,
        max: 5,
      },
    },
  },
  {
    // Other model options go here
    tableName: "t_comment",
    timestamps: false,
  },
);

module.exports = Comment;
