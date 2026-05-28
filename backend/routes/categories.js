const { Book, Category } = require("../models");

var express = require("express");
var router = express.Router();

router.get("/", async function (req, res, next) {
  try {
    const categories = await Category.findAll();
    res.json(categories);
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
});

router.get("/:category_id", async function (req, res, next) {
  try {
    const category = await Category.findOne({
      where: { category_id: req.params.category_id },
    });
    if (!category) {
      return res.status(404).json({ error: "Category not found" });
    }
    const books = await category.getBooks();
    res.json(books);
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
});

router.post("/:book_id", async function (req, res, next) {
  try {
    const { category_name } = req.body;
    const { book_id } = req.params;
    const book = await Book.findOne({ where: { book_id } });
    if (!book) {
      return res.status(404).json({ error: "Book not found" });
    }
    let category = await Category.findOne({ where: { name: category_name } });
    if (!category) {
      category = await Category.create({ name: category_name });
    }
    await book.addCategory(category);
    return res.json(category);
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
});

router.put("/:book_id", async function (req, res, next) {
  try {
    const { category_name } = req.body;
    const { book_id } = req.params;
    const book = await Book.findOne({ where: { book_id } });
    if (!book) {
      return res.status(404).json({ error: "Book not found" });
    }
    let category = await Category.findOne({ where: { name: category_name } });
    if (!category) {
      category = await Category.create({ name: category_name });
    }
    await book.setCategories([category]);
    return res.json(category);
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
});

router.delete("/:book_id/remove", async function (req, res, next) {
  try {
    const { category_name } = req.body;
    const { book_id } = req.params;
    const book = await Book.findOne({ where: { book_id } });
    if (!book) return res.status(404).json({ error: "Book not found" });
    const category = await Category.findOne({ where: { name: category_name } });
    if (!category) return res.status(404).json({ error: "Category not found" });
    await book.removeCategory(category);
    return res.json({ message: "Tag removed" });
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
});

router.delete("/:category_id", async function (req, res, next) {
  try {
    const { category_id } = req.params;
    const category = await Category.findOne({ where: { category_id } });
    if (!category) {
      return res.status(404).json({ error: "Category not found" });
    }
    await category.setBooks([]);
    await category.destroy();
    return res.json({ message: "Category deleted" });
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
});

module.exports = router;
