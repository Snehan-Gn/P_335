const { User, Book, Comment } = require("../models");

var express = require("express");
var router = express.Router();

router.get("/", async function (req, res, next) {
  try {
    const user = await User.findOne({ where: { user_id: req.user.user_id } });
    const comments = await user.getComments();
    res.json(comments);
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
});

router.get("/:book_id", async function (req, res, next) {
  try {
    const book = await Book.findOne({ where: { book_id: req.params.book_id } });
    if (!book) {
      return res.status(404).json({ error: "Livre non trouvé" });
    }
    const comment = await book.getComments();
    res.json(comment);
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
});

router.post("/:book_id", async function (req, res, next) {
  try {
    const { title, message, rating } = req.body;
    const { book_id } = req.params;
    const user = await User.findOne({ where: { user_id: req.user.user_id } });
    const book = await Book.findOne({ where: { book_id } });
    if (!book) {
      return res.status(404).json({ error: "Book not found" });
    }
    const comment = await user.createComment({
      title,
      message,
      rating,
      t_book_fk: book.book_id,
    });
    return res.json(comment);
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
});

router.put("/:comment_id", async function (req, res, next) {
  try {
    const { title, message, rating } = req.body;
    const { comment_id } = req.params;
    const user = await User.findOne({ where: { user_id: req.user.user_id } });
    const comments = await user.getComments({
      where: { comment_id },
    });
    if (comments.length === 0) {
      return res.status(404).json({ error: "Comment not found" });
    }
    const comment = comments[0];
    await comment.update({ title, message, rating });
    return res.json(comment);
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
});

router.delete("/:comment_id", async function (req, res, next) {
  try {
    const { comment_id } = req.params;
    const user = await User.findOne({ where: { user_id: req.user.user_id } });
    const comments = await user.getComments({
      where: { comment_id },
    });
    if (comments.length === 0) {
      return res.status(404).json({ error: "Comment not found" });
    }
    const comment = comments[0];
    await comment.destroy();
    return res.json({ message: "Comment deleted successfully" });
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
});

module.exports = router;
