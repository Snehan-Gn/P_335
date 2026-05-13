const { Sequelize, DataTypes } = require("sequelize");
var sequelize = require("../db");

// https://sequelize.org/docs/v6/core-concepts/model-basics/
const Book = sequelize.define(
  "Book",
  {
    // Model attributes are defined here
    book_id: {
      type: DataTypes.INTEGER,
      autoIncrement: true,
      primaryKey: true,
    },
    title: {
      type: DataTypes.STRING,
      allowNull: false,
    },
    author: {
      type: DataTypes.STRING,
      allowNull: true,
    },
    description: {
      type: DataTypes.STRING,
      allowNull: true,
    },
    publish_date: {
      type: DataTypes.DATE,
      allowNull: true,
    },
    language_: {
      type: DataTypes.STRING,
      allowNull: true,
    },
    isbn: {
      type: DataTypes.STRING,
      allowNull: true,
    },
    url: {
      type: DataTypes.STRING,
      allowNull: true,
    },
    cover_image_url: {
      type: DataTypes.STRING,
      allowNull: true,
    },
    // https://sequelize.org/docs/v6/core-concepts/getters-setters-virtuals/
    average_rating: {
      type: DataTypes.VIRTUAL,
      get() {
        if (this.Comments && this.Comments.length > 0) {
          const sum = this.Comments.reduce(
            (acc, comment) => acc + comment.rating,
            0,
          );
          return (sum / this.Comments.length).toFixed(1);
        }
        return null;
      },
    },
  },
  {
    // Other model options go here
    tableName: "t_book",
    timestamps: false,
  },
);

module.exports = Book;
