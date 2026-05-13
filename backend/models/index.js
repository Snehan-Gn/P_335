const User = require("./user");
const Book = require("./book");
const Comment = require("./comment");
const Category = require("./category");

// Association Many-to-Many (Possession)
User.belongsToMany(Book, {
  through: "possess",
  foreignKey: "t_user_fk",
  otherKey: "t_book_fk",
  timestamps: false,
});

Book.belongsToMany(User, {
  through: "possess",
  foreignKey: "t_book_fk",
  otherKey: "t_user_fk",
  timestamps: false,
});

User.hasMany(Comment, { foreignKey: "t_user_fk" });
Comment.belongsTo(User, { foreignKey: "t_user_fk" });

Book.hasMany(Comment, { foreignKey: "t_book_fk" });
Comment.belongsTo(Book, { foreignKey: "t_book_fk" });

Category.belongsToMany(Book, {
  through: "contains",
  foreignKey: "t_category_fk",
  otherKey: "t_book_fk",
  timestamps: false,
});

Book.belongsToMany(Category, {
  through: "contains",
  foreignKey: "t_book_fk",
  otherKey: "t_category_fk",
  timestamps: false,
});

module.exports = {
  User,
  Book,
  Comment,
  Category,
};
